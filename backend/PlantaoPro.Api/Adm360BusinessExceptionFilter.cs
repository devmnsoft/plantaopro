using System.Security.Claims;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api.Models;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Api;

/// <summary>
/// Mapeia erros de negócio CONHECIDOS do módulo Administrativo 360 para o código HTTP
/// correspondente, em vez do 500 genérico do handler global:
///   - Administrativo360BusinessException / ArgumentException → 400 (regra de negócio violada ou entrada inválida);
///   - UnauthorizedAccessException → 403 (acesso negado no domínio);
///   - KeyNotFoundException → 404 (registro inexistente).
/// Falhas técnicas (qualquer outro tipo, inclusive InvalidOperationException sem tipo específico)
/// passam por aqui sem tratamento e chegam ao handler global, que responde 500 e registra log
/// estruturado com correlação. Aplica-se apenas a rotas que contêm "administrativo360".
/// Implementa IAsyncExceptionFilter de propósito: o DefaultFilterProvider despacha para
/// OnExceptionAsync apenas quando o filtro declara a interface assíncrona (senão chama o
/// OnException síncrono por adapter e a persistência abaixo nunca executa).
/// R93 (B5 observabilidade): cada erro de negócio mapeado também é persistido em
/// plantaopro.erros_negocio (best-effort, com tenant/usuário das claims da sessão e timeout
/// curto) para o digest diário em GET /api/observabilidade/digest-diario. A persistência
/// nunca altera a resposta: qualquer falha nela gera apenas log de aviso.
/// </summary>
public sealed class Adm360BusinessExceptionFilter : IExceptionFilter, IAsyncExceptionFilter
{
    private static readonly Regex ParameterNameSuffix = new(@" \(Parameter '[^']*'\)\s*$", RegexOptions.Compiled);
    private const int TimeoutPersistenciaMs = 2000;

    private readonly IConfiguration? _cfg;
    private readonly ILogger<Adm360BusinessExceptionFilter> _logger;

    /// <summary>Único construtor (ambiguidade de ctor quebra a resolução do filtro em tempo de
    /// execução); parâmetros opcionais mantêm a construção nua dos contratos de unidade sem banco.</summary>
    public Adm360BusinessExceptionFilter(IConfiguration? cfg = null, ILogger<Adm360BusinessExceptionFilter>? logger = null)
    {
        _cfg = cfg;
        _logger = logger ?? NullLogger<Adm360BusinessExceptionFilter>.Instance;
    }

    /// <summary>Mapeamento síncrono (mantido para os contratos de unidade sem banco). A persistência entra pelo caminho assíncrono.</summary>
    public void OnException(ExceptionContext context) => Mapear(context, out _, out _, out _);

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (!Mapear(context, out var status, out var categoria, out var mensagem)) return;
        await PersistirAsync(context, status, categoria, mensagem).ConfigureAwait(false);
    }

    /// <summary>Converte exceções conhecidas de negócio em resultado amigável. Retorna se a exceção foi tratada.</summary>
    private bool Mapear(ExceptionContext context, out int status, out string categoria, out string message)
    {
        status = 0;
        categoria = string.Empty;
        message = string.Empty;

        var path = context.HttpContext.Request.Path.Value;
        if (path is null || !path.Contains("/administrativo360", StringComparison.OrdinalIgnoreCase)) return false;

        var ex = context.Exception;
        string mensagemPadrao;
        switch (ex)
        {
            case Administrativo360BusinessException: // regra de negócio explícita do módulo
            case ArgumentException:                  // entrada inválida (inclui ArgumentOutOfRangeException)
                status = StatusCodes.Status400BadRequest;
                categoria = "REGRA_NEGOCIO";
                mensagemPadrao = "Solicitação inválida.";
                message = MensagemUtil(ex.Message);
                break;
            case KeyNotFoundException kfn:           // registro não encontrado
                status = StatusCodes.Status404NotFound;
                categoria = "NAO_ENCONTRADO";
                mensagemPadrao = "Registro não encontrado.";
                // Se a exceção não carrega mensagem significativa (padrão técnico do .NET), usa a amigável em pt-BR.
                message = kfn.Message == new KeyNotFoundException().Message ? mensagemPadrao : MensagemUtil(kfn.Message);
                break;
            case UnauthorizedAccessException uaa:    // acesso negado no domínio
                status = StatusCodes.Status403Forbidden;
                categoria = "ACESSO_NEGADO";
                mensagemPadrao = "Acesso não autorizado.";
                message = uaa.Message == new UnauthorizedAccessException().Message ? mensagemPadrao : MensagemUtil(uaa.Message);
                break;
            default:
                // Falha técnica: mantém o fluxo padrão (handler global → HTTP 500 + log estruturado com correlação).
                return false;
        }

        if (string.IsNullOrWhiteSpace(message)) message = mensagemPadrao;
        context.Result = new JsonResult(ApiResponse<string>.Fail(message, status))
        {
            StatusCode = status
        };
        context.ExceptionHandled = true;
        return true;
    }

    /// <summary>
    /// Centralização (R93): um registro por erro mapeado em plantaopro.erros_negocio com o
    /// tenant/usuário da sessão (claims cliente_id/uid, mesma fonte dos logs de api_error_logs).
    /// Best-effort: timeout curto e isolamento total de falhas — a persistência nunca altera a resposta.
    /// </summary>
    private async Task PersistirAsync(ExceptionContext context, int status, string categoria, string mensagem)
    {
        if (_cfg is null) return;
        var conexao = _cfg.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(conexao)) return;

        var http = context.HttpContext;
        try
        {
            await using var cn = new NpgsqlConnection(conexao);
            var comando = new CommandDefinition(
                @"insert into plantaopro.erros_negocio (tenant_id, usuario_id, modulo, categoria, mensagem, endpoint, reg_date)
                  values (@tenantId, @usuarioId, 'ADMINISTRATIVO360', @categoria, @mensagem, @endpoint, now())",
                new
                {
                    tenantId = GuidClaim(http.User, "cliente_id"),
                    usuarioId = GuidClaim(http.User, "uid") ?? GuidClaim(http.User, ClaimTypes.NameIdentifier),
                    categoria,
                    mensagem,
                    endpoint = http.Request.Path.Value
                },
                commandTimeout: TimeoutPersistenciaMs / 1000);
            await cn.ExecuteAsync(comando);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro de negócio mapeado ({Status}) não persistiu em plantaopro.erros_negocio.", status);
        }
    }

    private static Guid? GuidClaim(ClaimsPrincipal? user, string nome)
    {
        var valor = user?.FindFirst(nome)?.Value;
        return Guid.TryParse(valor, out var g) ? g : null;
    }

    /// <summary>Tira o sufixo técnico "(Parameter 'x')" e espaços; aceita null.</summary>
    private static string MensagemUtil(string? mensagem) =>
        ParameterNameSuffix.Replace(mensagem ?? string.Empty, string.Empty).Trim();
}
