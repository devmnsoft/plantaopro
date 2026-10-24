using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Security;

/// <summary>
/// R103 (integracoes - API publica com chaves): autenticacao servidor-servidor por header
/// X-Api-Key sobre o hash SHA256 em plantaopro.api_chaves (mesmo hash que a criacao do portal do
/// desenvolvedor grava). Complementa o JWT nas rotas /api/publico: sem o header o esquema devolve
/// NoResult (o pedido segue para os demais esquemas); chave invalida, revogada ou de tenant
/// suspenso vira Fail (desafio 401). Os limites de rate limit viaem dos claims para o filtro
/// EscopoChave aplicar janela de 1 minuto / 1 dia sem nova consulta.
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiChave";
    public const string HeaderName = "X-Api-Key";

    private readonly IConfiguration _cfg;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration cfg)
        : base(options, logger, encoder)
    {
        _cfg = cfg;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var valores) || string.IsNullOrWhiteSpace(valores.ToString()))
            return AuthenticateResult.NoResult();

        var chave = valores.ToString().Trim();
        var hash = Hash(chave);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        var linha = await cn.QueryFirstOrDefaultAsync<ChaveRow>(new CommandDefinition(@"select c.id as ChaveId, coalesce(c.nome,'') as Nome, coalesce(c.escopos,'') as Escopos,
 t.id as TenantId, c.cliente_id as ClienteId, coalesce(t.status,'') as TenantStatus,
 coalesce(l.limite_minuto,120) as LimiteMinuto, coalesce(l.limite_dia,10000) as LimiteDia
from plantaopro.api_chaves c
left join plantaopro.tenants t on t.id=c.tenant_id and t.reg_status='A'
left join plantaopro.api_rate_limits l on l.api_chave_id=c.id and l.reg_status='A'
where c.reg_status='A' and upper(c.status)='ATIVA' and c.api_key_hash=@hash
limit 1", new { hash }, cancellationToken: Context.RequestAborted));
        if (linha is null)
            return AuthenticateResult.Fail("Chave de API não reconhecida ou revogada.");
        if (!linha.TenantId.HasValue || string.Equals(linha.TenantStatus, "SUSPENSO", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Instituição suspensa. Regularize a assinatura para continuar.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, linha.ChaveId.ToString()),
            new("chave_id", linha.ChaveId.ToString()),
            new("chave_nome", linha.Nome),
            new("tenant_id", linha.TenantId.Value.ToString()),
            new("limite_minuto", linha.LimiteMinuto.ToString()),
            new("limite_dia", linha.LimiteDia.ToString())
        };
        if (linha.ClienteId.HasValue)
            claims.Add(new Claim("cliente_id", linha.ClienteId.Value.ToString()));
        foreach (var escopo in linha.Escopos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            claims.Add(new Claim("escopo", escopo));

        var identidade = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.Name, ClaimTypes.Role);
        identidade.AddClaim(new Claim(ClaimTypes.Name, linha.Nome));
        var principal = new ClaimsPrincipal(identidade);
        try
        {
            await cn.ExecuteAsync(new CommandDefinition("update plantaopro.api_chaves set ultimo_uso_em=now() where id=@id", new { id = linha.ChaveId }, cancellationToken: Context.RequestAborted));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Falha ao registrar ultimo_uso_em da chave {ChaveId}", linha.ChaveId);
        }
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private static string Hash(string chave) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chave))).ToLowerInvariant();

    private sealed record ChaveRow(Guid ChaveId, string Nome, string Escopos, Guid? TenantId, Guid? ClienteId, string TenantStatus, int LimiteMinuto, int LimiteDia);
}

/// <summary>
/// R103: filtro das rotas da API publica autenticada por chave. Verifica (1) presenca do
/// principal do esquema ApiChave, (2) o escopo exigido pela rota e (3) as janelas de rate limit
/// (1 minuto / 1 dia) contadas sobre a trilha plantaopro.api_chaves_usos. Cada requisicao
/// autenticada deixa trilha na trilha (metodo, rota, status HTTP e IP) — base dos logs do portal
/// do desenvolvedor. Falha de trilha nunca derruba a requisicao (best effort).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class EscopoChaveAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _escopo = string.Empty;

    public EscopoChaveAttribute(string escopo) => _escopo = escopo.Trim().ToLowerInvariant();

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var chaveIdTexto = user.FindFirstValue("chave_id");
        if (string.IsNullOrWhiteSpace(chaveIdTexto))
        {
            context.Result = new ObjectResult(ApiResponse<string>.Fail("Chave de API ausente ou inválida.", 401)) { StatusCode = 401 };
            return;
        }
        if (!Guid.TryParse(chaveIdTexto, out var chaveId))
        {
            context.Result = new ObjectResult(ApiResponse<string>.Fail("Chave de API inválida.", 401)) { StatusCode = 401 };
            return;
        }
        if (_escopo.Length > 0 && !user.Claims.Any(c => c.Type == "escopo" && string.Equals(c.Value, _escopo, StringComparison.OrdinalIgnoreCase)))
        {
            context.Result = new ObjectResult(ApiResponse<string>.Fail($"A chave não possui o escopo {_escopo}.", 403)) { StatusCode = 403 };
            return;
        }

        var cfg = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var limiteMinuto = int.TryParse(user.FindFirstValue("limite_minuto"), out var lm) ? lm : 120;
        var limiteDia = int.TryParse(user.FindFirstValue("limite_dia"), out var ld) ? ld : 10000;
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        var janelas = await cn.QueryFirstOrDefaultAsync<JanelaUso>(new CommandDefinition(@"select (select count(*) from plantaopro.api_chaves_usos where chave_id=@id and reg_status='A' and reg_date > now() - interval '1 minute')::int as Minuto,
(select count(*) from plantaopro.api_chaves_usos where chave_id=@id and reg_status='A' and reg_date > now() - interval '1 day')::int as Dia", new { id = chaveId }, cancellationToken: context.HttpContext.RequestAborted));
        if (janelas is not null && (janelas.Minuto >= limiteMinuto || janelas.Dia >= limiteDia))
        {
            context.Result = new ObjectResult(ApiResponse<string>.Fail("Limite de requisições da chave excedido.", 429)) { StatusCode = 429 };
            return;
        }

        await next();

        try
        {
            int statusHttp = 200;
            switch (context.Result)
            {
                case StatusCodeResult sr: statusHttp = sr.StatusCode; break;
                case ObjectResult resultado when resultado.StatusCode != null: statusHttp = resultado.StatusCode.Value; break;
            }
            var tenantId = Guid.TryParse(user.FindFirstValue("tenant_id"), out var tid) ? tid : (Guid?)null;
            var clienteId = Guid.TryParse(user.FindFirstValue("cliente_id"), out var cid) ? cid : (Guid?)null;
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            await cn.ExecuteAsync(new CommandDefinition(@"insert into plantaopro.api_chaves_usos(id, chave_id, tenant_id, cliente_id, metodo, rota, status_http, ip_origem, reg_date, reg_status)
values(gen_random_uuid(), @chaveId, @tenantId, @clienteId, @metodo, @rota, @status, @ip, now(), 'A')", new
            {
                chaveId,
                tenantId,
                clienteId,
                metodo = context.HttpContext.Request.Method,
                rota = context.HttpContext.Request.Path.Value ?? string.Empty,
                status = statusHttp,
                ip
            }, cancellationToken: context.HttpContext.RequestAborted));
        }
        catch (Exception ex)
        {
            // Trilha de uso e best effort: a resposta ja foi decidida pelo endpoint.
            context.HttpContext.RequestServices.GetRequiredService<ILogger<EscopoChaveAttribute>>().LogWarning(ex, "Falha ao registrar uso da chave {ChaveId}", chaveId);
        }
    }

    private sealed record JanelaUso(int Minuto, int Dia);
}
