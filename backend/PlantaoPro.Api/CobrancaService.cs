using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

// ============================================================================
// R5-B6 - Cobranca SaaS (meio de pagamento por contrato de provider).
// Nenhum provedor externo existe ainda no produto; a entrega e o nucleo
// completo com provider SANDBOX local: checkout sem dados de cartao, webhook
// anonimo autenticado por HMAC-SHA256 do corpo bruto, dedupe atomico pelo
// indice unico (provider, evento) e maquina de estados que nunca rebaixa
// estado terminal nem inventa sucesso. O simulador sandbox assina server-side
// e percorre exatamente o mesmo pipeline do webhook real. Redirect de
// navegador (GET) nunca muta: so le status. Pagamento aprovado aplica os
// mesmos efeitos do marcar-paga canônico (fatura PAGA + pagamento + evento +
// resolucao de alertas), tudo transacional.
// ============================================================================

/// <summary>Meio de cobranca externo abstrato. A unica implementacao atual e o SANDBOX.</summary>
public interface ICobrancaProvider
{
    string Codigo { get; }
    /// <summary>Registra a cobranca no meio de pagamento e devolve referencia opaca + caminho do checkout hospedado.</summary>
    (string Referencia, string CheckoutPath) CriarCobranca(Guid faturaId, decimal valor, DateOnly vencimento);
}

/// <summary>Sandbox deterministico PLANTAOPro: referencia criptograficamente forte (a URL e a credencial do checkout) e nenhum dado de cartao.</summary>
public sealed class SandboxCobrancaProvider : ICobrancaProvider
{
    public string Codigo => "SANDBOX";

    public (string Referencia, string CheckoutPath) CriarCobranca(Guid faturaId, decimal valor, DateOnly vencimento)
    {
        var referencia = "SBX-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        return (referencia, $"/api/cobranca/sandbox/{referencia}/pagina");
    }
}

public sealed record CobrancaProviderDto(string Codigo, string Nome, string Modo, string Status, bool CredencialConfigurada);
public sealed record CobrancaCriadaDto(Guid CobrancaId, string Referencia, string CheckoutPath, decimal Valor, string Status, bool JaExistia);
public sealed record CobrancaPublicaDto(string Referencia, string Provider, string Status, decimal Valor, DateOnly Vencimento, DateOnly Competencia, string ClienteNome, string FaturaStatus, DateTime ExpiraEm);
public sealed record CobrancaEventoDto(string Fonte, string Tipo, string? Resultado, string? Mensagem, DateTime Em);
public sealed record CobrancaFaturaEventosDto(IReadOnlyList<CobrancaResumoDto> Cobrancas, IReadOnlyList<CobrancaEventoDto> Eventos);
public sealed record CobrancaResumoDto(Guid CobrancaId, string Provider, string Referencia, string Status, decimal Valor, DateTime CriadoEm, string CheckoutPath);
public sealed record WebhookRetornoDto(string Resultado, string Mensagem);

public sealed class CobrancaService
{
    /// <summary>Padrao de credenciais da Fiscal: segredo so no pool da API, chave "Cobranca:Credenciais:{CODIGO}".</summary>
    public const string PrefixoCredenciais = "Cobranca:Credenciais";

    private readonly IConfiguration _cfg;
    private readonly IAuditService _audit;
    private readonly IEnumerable<ICobrancaProvider> _providers;
    private readonly ILogger<CobrancaService> _logger;

    public CobrancaService(IConfiguration cfg, IAuditService audit, IEnumerable<ICobrancaProvider> providers, ILogger<CobrancaService> logger)
    {
        _cfg = cfg;
        _audit = audit;
        _providers = providers;
        _logger = logger;
    }

    private string ConnectionString => _cfg.GetConnectionString("Default")!;
    private string? Credencial(string codigo) => _cfg.GetSection(PrefixoCredenciais)[codigo.Trim().ToUpperInvariant()];
    private ICobrancaProvider? Provider(string codigo) => _providers.FirstOrDefault(p => string.Equals(p.Codigo, codigo, StringComparison.OrdinalIgnoreCase));

    // ---------------------------------------------------------------- administracao

    public async Task<ApiResponse<IReadOnlyList<CobrancaProviderDto>>> ListarProvidersAsync(CancellationToken ct)
    {
        try
        {
            await using var cn = new NpgsqlConnection(ConnectionString);
            var rows = await cn.QueryAsync<ProviderRow>(new CommandDefinition(
                @"select codigo as Codigo, nome as Nome, modo as Modo, status as Status
                    from plantaopro.cobranca_providers
                   where reg_status='A' order by codigo", cancellationToken: ct));
            var items = rows.Select(r => new CobrancaProviderDto(r.Codigo, r.Nome, r.Modo, r.Status, !string.IsNullOrWhiteSpace(Credencial(r.Codigo)))).ToList();
            return ApiResponse<IReadOnlyList<CobrancaProviderDto>>.Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar providers de cobranca");
            return ApiResponse<IReadOnlyList<CobrancaProviderDto>>.Fail("Nao foi possivel listar os providers de cobranca.", 500);
        }
    }

    /// <summary>
    /// Gera cobranca para fatura aberta/vencida. Idempotente: cobranca ativa existente
    /// e devolvida sem nova criacao; o indice unico parcial arbitra a concorrencia.
    /// </summary>
    public async Task<ApiResponse<CobrancaCriadaDto>> CobrarAsync(Guid faturaId, string? providerCodigo, string? ip, CancellationToken ct)
    {
        var codigo = (string.IsNullOrWhiteSpace(providerCodigo) ? "SANDBOX" : providerCodigo).Trim().ToUpperInvariant();
        try
        {
            await using var cn = new NpgsqlConnection(ConnectionString);
            await cn.OpenAsync(ct);

            var provider = await cn.QueryFirstOrDefaultAsync<ProviderStateRow>(new CommandDefinition(
                "select modo as Modo, status as Estado from plantaopro.cobranca_providers where codigo=@codigo and reg_status='A'", new { codigo }, cancellationToken: ct));
            if (provider is null || string.IsNullOrEmpty(provider.Estado))
                return ApiResponse<CobrancaCriadaDto>.Fail("Provider de cobranca inexistente.", 404);
            if (!string.Equals(provider.Estado, "ATIVO", StringComparison.OrdinalIgnoreCase))
                return ApiResponse<CobrancaCriadaDto>.Fail("Provider de cobranca inativo. Reative-o antes de cobrar.", 409);
            var providerImpl = Provider(codigo);
            if (providerImpl is null)
                return ApiResponse<CobrancaCriadaDto>.Fail("Provider de cobranca sem implementacao disponivel neste ambiente.", 409);
            if (string.IsNullOrWhiteSpace(Credencial(codigo)))
                return ApiResponse<CobrancaCriadaDto>.Fail($"Credencial de cobranca nao configurada para o provider {codigo}. Configure '{PrefixoCredenciais}:{codigo}' no pool da API.", 503);

            var fatura = await cn.QueryFirstOrDefaultAsync<FaturaCobrarRow>(new CommandDefinition(
                @"select cliente_id as ClienteId, upper(status) as Estado, valor as Valor, vencimento as Vencimento
                    from plantaopro.faturas_saas where id=@faturaId and reg_status='A'", new { faturaId }, cancellationToken: ct));
            if (fatura is null || fatura.ClienteId == Guid.Empty)
                return ApiResponse<CobrancaCriadaDto>.Fail("Fatura nao encontrada.", 404);
            if (fatura.Valor <= 0)
                return ApiResponse<CobrancaCriadaDto>.Fail("Fatura sem valor contratado nao pode ser cobrada online.", 409);
            if (fatura.Estado == "PAGA")
                return ApiResponse<CobrancaCriadaDto>.Fail("Fatura ja paga dispensa nova cobranca.", 409);
            if (fatura.Estado == "CANCELADA")
                return ApiResponse<CobrancaCriadaDto>.Fail("Fatura cancelada nao pode ser cobrada.", 409);
            if (fatura.Estado == "EM_CONTESTACAO")
                return ApiResponse<CobrancaCriadaDto>.Fail("Fatura em contestacao aguarda resposta do financeiro antes de nova cobranca.", 409);

            var existente = await cn.QueryFirstOrDefaultAsync<CobrancaResumoRow>(new CommandDefinition(
                CobrancaAtivaSql + " order by criado_em desc limit 1", new { faturaId }, cancellationToken: ct));
            if (existente is not null)
                return ApiResponse<CobrancaCriadaDto>.Ok(ParaDto(existente, true), "Ja existe uma cobranca ativa para esta fatura.");

            var (referencia, checkoutPath) = providerImpl.CriarCobranca(faturaId, fatura.Valor, fatura.Vencimento);
            await using var tx = await cn.BeginTransactionAsync(ct);
            Guid cobrancaId;
            try
            {
                cobrancaId = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(
                    @"insert into plantaopro.cobranca_cobrancas(fatura_id, cliente_id, provider_codigo, referencia, checkout_url, valor, status)
                      values(@faturaId, @clienteId, @codigo, @referencia, @checkoutPath, @valor, 'INICIADA')
                      returning id",
                    new { faturaId, clienteId = fatura.ClienteId, codigo, referencia, checkoutPath, valor = fatura.Valor }, tx, cancellationToken: ct));
            }
            catch (PostgresException pex) when (pex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await tx.RollbackAsync(ct);
                var corrida = await cn.QueryFirstOrDefaultAsync<CobrancaResumoRow>(new CommandDefinition(
                    CobrancaAtivaSql + " order by criado_em desc limit 1", new { faturaId }, cancellationToken: ct));
                if (corrida is null) throw;
                return ApiResponse<CobrancaCriadaDto>.Ok(ParaDto(corrida, true), "Ja existe uma cobranca ativa para esta fatura.");
            }

            await cn.ExecuteAsync(new CommandDefinition(
                @"insert into plantaopro.cobranca_eventos(id, cliente_id, fatura_id, tipo, mensagem, reg_status, reg_date)
                  values(gen_random_uuid(), @clienteId, @faturaId, 'COBRANCA_CRIADA', @mensagem, 'A', now())",
                new { clienteId = fatura.ClienteId, faturaId, mensagem = $"Cobranca {codigo} iniciada via checkout (referencia {referencia})." }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            await _audit.RegistrarAsync(null, fatura.ClienteId, AuditoriaConstants.Entidades.FaturaSaas, faturaId, AuditoriaConstants.Acoes.Criar, new { faturaId, codigo, referencia }, true, ip, "COBRANCA");
            return ApiResponse<CobrancaCriadaDto>.Ok(new CobrancaCriadaDto(cobrancaId, referencia, checkoutPath, fatura.Valor, "INICIADA", false), "Cobranca criada. Envie o link de checkout ao pagador.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar cobranca da fatura {FaturaId}", faturaId);
            return ApiResponse<CobrancaCriadaDto>.Fail("Nao foi possivel iniciar a cobranca.", 500);
        }
    }

    private const string CobrancaAtivaSql = @"select id as CobrancaId, provider_codigo as Provider, referencia as Referencia, upper(status) as Estado, valor as Valor, criado_em as CriadoEm, coalesce(checkout_url,'') as CheckoutPath
                                                from plantaopro.cobranca_cobrancas
                                               where fatura_id=@faturaId and reg_status='A' and status in ('PENDENTE','INICIADA')";

    private static CobrancaCriadaDto ParaDto(CobrancaResumoRow row, bool jaExistia)
        => new(row.CobrancaId, row.Referencia, row.CheckoutPath, row.Valor, row.Estado, jaExistia);

    /// <summary>Historico consolidado da fatura: tentativas de cobranca + webhooks + trilha financeira.</summary>
    public async Task<ApiResponse<CobrancaFaturaEventosDto>> EventosFaturaAsync(Guid faturaId, CancellationToken ct)
    {
        try
        {
            await using var cn = new NpgsqlConnection(ConnectionString);
            var faturaExiste = await cn.ExecuteScalarAsync<int?>(new CommandDefinition(
                "select 1 from plantaopro.faturas_saas where id=@faturaId and reg_status='A'", new { faturaId }, cancellationToken: ct));
            if (faturaExiste is null) return ApiResponse<CobrancaFaturaEventosDto>.Fail("Fatura nao encontrada.", 404);

            var cobrancas = (await cn.QueryAsync<CobrancaResumoDto>(new CommandDefinition(
                @"select id as ""CobrancaId"", provider_codigo as ""Provider"", referencia as ""Referencia"", upper(status) as ""Status"", valor as ""Valor"", criado_em as ""CriadoEm"", coalesce(checkout_url,'') as ""CheckoutPath""
                    from plantaopro.cobranca_cobrancas where fatura_id=@faturaId and reg_status='A' order by criado_em desc limit 50", new { faturaId }, cancellationToken: ct))).ToList();
            var eventos = (await cn.QueryAsync<CobrancaEventoDto>(new CommandDefinition(
                @"select * from (
                     select 'financeiro' as ""Fonte"", tipo as ""Tipo"", null::varchar as ""Resultado"", coalesce(mensagem,'') as ""Mensagem"", reg_date as ""Em""
                       from plantaopro.cobranca_eventos where fatura_id=@faturaId and reg_status='A'
                     union all
                     select 'webhook' as ""Fonte"", tipo_evento as ""Tipo"", resultado as ""Resultado"", coalesce(mensagem,'') as ""Mensagem"", processado_em as ""Em""
                       from plantaopro.cobranca_webhook_eventos where fatura_id=@faturaId and reg_status='A'
                   ) t order by ""Em"" desc limit 100", new { faturaId }, cancellationToken: ct))).ToList();
            return ApiResponse<CobrancaFaturaEventosDto>.Ok(new CobrancaFaturaEventosDto(cobrancas, eventos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar eventos de cobranca da fatura {FaturaId}", faturaId);
            return ApiResponse<CobrancaFaturaEventosDto>.Fail("Nao foi possivel carregar os eventos de cobranca.", 500);
        }
    }

    // -------------------------------------------------------------- checkout publico (somente leitura)

    /// <summary>Status publico para a pagina de checkout. Somente leitura: abrir/recarregar jamais muta.</summary>
    public async Task<ApiResponse<CobrancaPublicaDto>> ObterPublicaAsync(string referencia, CancellationToken ct)
    {
        try
        {
            await using var cn = new NpgsqlConnection(ConnectionString);
            var row = await cn.QueryFirstOrDefaultAsync<CobrancaPublicaDto>(new CommandDefinition(
                @"select c.referencia as ""Referencia"", c.provider_codigo as ""Provider"", upper(c.status) as ""Status"", c.valor as ""Valor"",
                         f.vencimento as ""Vencimento"", f.competencia as ""Competencia"",
                         coalesce(cli.nome_fantasia, cli.razao_social, '') as ""ClienteNome"", upper(f.status) as ""FaturaStatus"",
                         c.expira_em as ""ExpiraEm""
                    from plantaopro.cobranca_cobrancas c
                    join plantaopro.faturas_saas f on f.id=c.fatura_id
                    join plantaopro.clientes cli on cli.id=c.cliente_id
                   where c.referencia=@referencia and c.reg_status='A'", new { referencia }, cancellationToken: ct));
            return row is null
                ? ApiResponse<CobrancaPublicaDto>.Fail("Cobranca nao encontrada (link invalido ou expirado).", 404)
                : ApiResponse<CobrancaPublicaDto>.Ok(row);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao consultar cobranca publica {Referencia}", referencia);
            return ApiResponse<CobrancaPublicaDto>.Fail("Nao foi possivel consultar a cobranca.", 500);
        }
    }

    /// <summary>
    /// Simulador do meio de pagamento sandbox: monta o payload oficial, assina HMAC
    /// server-side e chama o MESMO pipeline do webhook. Mutacao apenas por POST explicito.
    /// </summary>
    public async Task<ApiResponse<WebhookRetornoDto>> SimularPagamentoAsync(string referencia, bool aprovado, CancellationToken ct)
    {
        var segredo = Credencial("SANDBOX");
        if (string.IsNullOrWhiteSpace(segredo))
            return ApiResponse<WebhookRetornoDto>.Fail($"Credencial de cobranca nao configurada para o provider SANDBOX. Configure '{PrefixoCredenciais}:SANDBOX' no pool da API.", 503);
        try
        {
            await using var cn = new NpgsqlConnection(ConnectionString);
            var estado = await cn.QueryFirstOrDefaultAsync<SimulacaoRow>(new CommandDefinition(
                @"select upper(c.status) as Estado, c.valor as Valor, upper(coalesce(p.modo,'INEXISTENTE')) as Modo
                    from plantaopro.cobranca_cobrancas c
                    left join plantaopro.cobranca_providers p on p.codigo=c.provider_codigo and p.reg_status='A'
                   where c.referencia=@referencia and c.reg_status='A'", new { referencia }, cancellationToken: ct));
            if (estado is null) return ApiResponse<WebhookRetornoDto>.Fail("Cobranca nao encontrada (link invalido ou expirado).", 404);
            if (estado.Modo != "SANDBOX")
                return ApiResponse<WebhookRetornoDto>.Fail("Simulacao disponivel apenas para cobrancas do provider SANDBOX.", 409);
            if (estado.Estado is "PAGA" or "FALHA" or "CANCELADA" or "ESTORNADA")
                return ApiResponse<WebhookRetornoDto>.Fail($"Cobranca ja finalizada ({estado.Estado}). A simulacao nao altera cobrancas concluidas.", 409);

            var payload = new Dictionary<string, object?>
            {
                ["eventoId"] = "SBXEVT-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant(),
                ["tipo"] = aprovado ? "PAGAMENTO_APROVADO" : "PAGAMENTO_NEGADO",
                ["referencia"] = referencia,
                ["valorPago"] = aprovado ? estado.Valor : (decimal?)null,
                ["dataPagamento"] = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
                ["formaPagamento"] = aprovado ? "PIX_SANDBOX" : null
            };
            var corpo = JsonSerializer.SerializeToUtf8Bytes(payload);
            var assinatura = "sha256=" + HexAssinatura(segredo, corpo);
            return await ProcessarWebhookInternoAsync("SANDBOX", corpo, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao simular pagamento sandbox {Referencia}", referencia);
            return ApiResponse<WebhookRetornoDto>.Fail("Nao foi possivel processar a simulacao de pagamento.", 500);
        }
    }

    // -------------------------------------------------------------- webhook autenticado por HMAC

    /// <summary>
    /// Webhook anonimo do provider. Autenticacao = HMAC-SHA256 do corpo bruto
    /// (header "X-Cobranca-Signature: sha256=&lt;hex&gt;", comparacao constant-time).
    /// Provider/segredo/assinatura validam ANTES de qualquer persistencia: um replay
    /// nao assinado jamais consome o dedupe de um evento legitimo futuro.
    /// </summary>
    public async Task<ApiResponse<WebhookRetornoDto>> ProcessarWebhookAsync(string providerCodigo, byte[] corpo, string? headerAssinatura, CancellationToken ct)
    {
        var codigo = (providerCodigo ?? string.Empty).Trim().ToUpperInvariant();
        try
        {
            await using var cn = new NpgsqlConnection(ConnectionString);
            var provider = await cn.QueryFirstOrDefaultAsync<ProviderStateRow>(new CommandDefinition(
                "select modo as Modo, status as Estado from plantaopro.cobranca_providers where codigo=@codigo and reg_status='A'", new { codigo }, cancellationToken: ct));
            if (provider is null || string.IsNullOrEmpty(provider.Estado))
                return ApiResponse<WebhookRetornoDto>.Fail("Provider de cobranca inexistente.", 404);
            if (!string.Equals(provider.Estado, "ATIVO", StringComparison.OrdinalIgnoreCase))
                return ApiResponse<WebhookRetornoDto>.Fail("Provider de cobranca inativo.", 409);
            var segredo = Credencial(codigo);
            if (string.IsNullOrWhiteSpace(segredo))
                return ApiResponse<WebhookRetornoDto>.Fail($"Credencial de cobranca nao configurada para o provider {codigo}.", 503);
            if (!AssinaturaValida(headerAssinatura, segredo, corpo))
                return ApiResponse<WebhookRetornoDto>.Fail("Assinatura do webhook invalida ou ausente.", 401);
            return await ProcessarWebhookInternoAsync(codigo, corpo, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook de cobranca {Provider}", codigo);
            return ApiResponse<WebhookRetornoDto>.Fail("Nao foi possivel processar o webhook de cobranca.", 500);
        }
    }

    /// <summary>Pipeline comum ao webhook real e ao simulador sandbox (mesmo codigo, mesmos efeitos).</summary>
    private async Task<ApiResponse<WebhookRetornoDto>> ProcessarWebhookInternoAsync(string codigo, byte[] corpo, CancellationToken ct)
    {
        string payloadJson = Encoding.UTF8.GetString(corpo);
        string eventoId;
        string tipo;
        string? referencia;
        decimal? valorPago;
        DateOnly dataPagamento;
        string? formaPagamento;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            eventoId = Texto(root, "eventoId", "evento_id")?.Trim() ?? string.Empty;
            tipo = (Texto(root, "tipo") ?? string.Empty).Trim().ToUpperInvariant();
            referencia = Texto(root, "referencia");
            valorPago = Numero(root, "valorPago", "valor_pago");
            var dataTexto = Texto(root, "dataPagamento", "data_pagamento");
            dataPagamento = DateOnly.TryParseExact(dataTexto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dp) ? dp : DateOnly.FromDateTime(DateTime.UtcNow);
            formaPagamento = Texto(root, "formaPagamento", "forma_pagamento");
        }
        catch (JsonException)
        {
            return Responder("RECUSADO", "Corpo do webhook nao e JSON valido; nada foi alterado.");
        }
        if (string.IsNullOrEmpty(eventoId) || string.IsNullOrEmpty(referencia))
            return Responder("RECUSADO", "Webhook sem eventoId ou referencia; nada foi alterado.");
        if (tipo is not ("PAGAMENTO_APROVADO" or "PAGAMENTO_NEGADO" or "ESTORNO"))
            return Responder("RECUSADO", $"Tipo de evento desconhecido: {tipo}; nada foi alterado.");

        await using var cn = new NpgsqlConnection(ConnectionString);
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);
        try
        {
            var cobranca = await cn.QueryFirstOrDefaultAsync<WebhookCobrancaRow>(new CommandDefinition(
                @"select c.id as CobrancaId, c.fatura_id as FaturaId, c.cliente_id as ClienteId, upper(c.status) as Estado, c.valor as Valor
                    from plantaopro.cobranca_cobrancas c
                   where c.referencia=@referencia and c.provider_codigo=@codigo and c.reg_status='A'
                   for update of c", new { referencia, codigo }, tx, cancellationToken: ct));

            string resultado;
            string mensagem;
            Guid? cobrancaDbId = cobranca?.CobrancaId;
            Guid? faturaDbId = cobranca?.FaturaId;

            if (cobranca is null)
            {
                resultado = "IGNORADO";
                mensagem = $"Referencia {referencia} desconhecida para o provider {codigo}; nenhuma cobranca foi alterada.";
            }
            else if (tipo == "ESTORNO")
            {
                var aplicado = await cn.ExecuteAsync(new CommandDefinition(
                    @"update plantaopro.cobranca_cobrancas set status='ESTORNADA', evento_final_id=@eventoId, atualizado_em=now()
                       where id=@cobrancaId and status='PAGA'", new { eventoId, cobrancaId = cobranca.CobrancaId }, tx, cancellationToken: ct));
                resultado = aplicado > 0 ? "APLICADO" : "IGNORADO";
                mensagem = aplicado > 0
                    ? "Estorno registrado na cobranca. A fatura permanece paga ate a decisao do financeiro."
                    : $"Estorno ignorado: cobranca em estado {cobranca.Estado}.";
                if (aplicado > 0)
                    await RegistrarEventoFinanceiroAsync(cn, tx, cobranca.ClienteId, cobranca.FaturaId, "COBRANCA_ESTORNADA", $"Estorno registrado pela cobranca {codigo}; estorno do credito exige acao do financeiro.", ct);
            }
            else if (tipo == "PAGAMENTO_NEGADO")
            {
                var aplicado = await cn.ExecuteAsync(new CommandDefinition(
                    @"update plantaopro.cobranca_cobrancas set status='FALHA', evento_final_id=@eventoId, atualizado_em=now()
                       where id=@cobrancaId and status in ('PENDENTE','INICIADA')", new { eventoId, cobrancaId = cobranca.CobrancaId }, tx, cancellationToken: ct));
                resultado = aplicado > 0 ? "APLICADO" : "IGNORADO";
                mensagem = aplicado > 0
                    ? "Pagamento recusado registrado; a fatura continua em aberto."
                    : $"Recusa ignorada: cobranca em estado {cobranca.Estado}.";
                if (aplicado > 0)
                    await RegistrarEventoFinanceiroAsync(cn, tx, cobranca.ClienteId, cobranca.FaturaId, "COBRANCA_FALHA", $"Pagamento recusado pela cobranca {codigo}; a fatura continua em aberto.", ct);
            }
            else
            {
                // PAGAMENTO_APROVADO
                if (cobranca.Estado is not ("PENDENTE" or "INICIADA"))
                {
                    resultado = "IGNORADO";
                    mensagem = $"Pagamento fora de ordem ignorado: cobranca em estado {cobranca.Estado}.";
                }
                else
                {
                    var fatura = await cn.QueryFirstOrDefaultAsync<WebhookFaturaRow>(new CommandDefinition(
                        "select upper(status) as Estado from plantaopro.faturas_saas where id=@faturaId and reg_status='A' for update", new { faturaId = cobranca.FaturaId }, tx, cancellationToken: ct));
                    var faturaEstado = fatura?.Estado ?? "INEXISTENTE";
                    if (faturaEstado is "PAGA" or "CANCELADA" or "INEXISTENTE")
                    {
                        resultado = "IGNORADO";
                        mensagem = $"Pagamento recebido para fatura {faturaEstado}; nada foi alterado. Cabendo estorno, o financeiro decide.";
                    }
                    else if (valorPago.HasValue && Math.Abs(valorPago.Value - cobranca.Valor) > 0.005m)
                    {
                        resultado = "RECUSADO";
                        mensagem = $"Divergencia de valor: webhook informou {valorPago.Value.ToString("N2", CultureInfo.InvariantCulture)} e a cobranca espera {cobranca.Valor.ToString("N2", CultureInfo.InvariantCulture)}. Fatura mantida sem alteracao.";
                    }
                    else
                    {
                        var aplicado = await cn.ExecuteAsync(new CommandDefinition(
                            @"update plantaopro.cobranca_cobrancas set status='PAGA', evento_final_id=@eventoId, atualizado_em=now()
                               where id=@cobrancaId and status in ('PENDENTE','INICIADA')", new { eventoId, cobrancaId = cobranca.CobrancaId }, tx, cancellationToken: ct));
                        if (aplicado == 0)
                        {
                            resultado = "IGNORADO";
                            mensagem = $"Pagamento ignorado por corrida de estado na cobranca ({cobranca.Estado}).";
                        }
                        else
                        {
                            await AplicarPagamentoCanonicoAsync(cn, tx, cobranca.FaturaId, cobranca.ClienteId, cobranca.Valor,
                                dataPagamento, string.IsNullOrWhiteSpace(formaPagamento) ? $"CHECKOUT_{codigo}" : formaPagamento,
                                $"Pagamento {codigo} confirmado por webhook (evento {eventoId}).", ct);
                            resultado = "APLICADO";
                            mensagem = "Pagamento confirmado e fatura liquidada pelo webhook.";
                        }
                    }
                }
            }

            // Registro do evento em último lugar: violação do único (provider,evento) desfaz
            // a transação inteira — dedupe atômico, nada é aplicado duas vezes.
            await cn.ExecuteAsync(new CommandDefinition(
                @"insert into plantaopro.cobranca_webhook_eventos(provider_codigo, evento_id, tipo_evento, referencia, cobranca_id, fatura_id, resultado, mensagem, payload)
                  values(@codigo, @eventoId, @tipo, @referencia, @cobrancaDbId, @faturaDbId, @resultado, @mensagem, @payload::jsonb)",
                new { codigo, eventoId, tipo, referencia, cobrancaDbId, faturaDbId, resultado, mensagem, payload = payloadJson }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            await _audit.RegistrarAsync(null, null, AuditoriaConstants.Entidades.FaturaSaas, faturaDbId, AuditoriaConstants.Acoes.AlterarStatus, new { provedor = codigo, eventoId, resultado }, resultado == "APLICADO", null, "WEBHOOK_COBRANCA");
            return ApiResponse<WebhookRetornoDto>.Ok(new WebhookRetornoDto(resultado, mensagem), mensagem);
        }
        catch (PostgresException pex) when (pex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync(ct);
            return ApiResponse<WebhookRetornoDto>.Ok(new WebhookRetornoDto("DUPLICADO", "Evento processado anteriormente ou em concorrencia; nenhuma alteracao adicional foi feita."), "Webhook repetido ignorado.");
        }
    }

    /// <summary>Mesmos efeitos do marcar-paga canônico do faturamento SaaS.</summary>
    private static async Task AplicarPagamentoCanonicoAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid faturaId, Guid clienteId, decimal valor, DateOnly dataPagamento, string formaPagamento, string mensagem, CancellationToken ct)
    {
        await cn.ExecuteAsync(new CommandDefinition(
            @"update plantaopro.faturas_saas
                 set status='PAGA', valor_pago=coalesce(valor_pago, @valor), data_pagamento=@dataPagamento, forma_pagamento=@formaPagamento, atualizado_em=now()
               where id=@faturaId and reg_status='A' and upper(status) in ('ABERTA','VENCIDA','EM_CONTESTACAO')",
            new { faturaId, valor, dataPagamento, formaPagamento }, tx, cancellationToken: ct));
        await cn.ExecuteAsync(new CommandDefinition(
            @"insert into plantaopro.pagamentos_saas(id, fatura_id, cliente_id, valor_pago, data_pagamento, forma_pagamento, observacoes, reg_status, reg_date, criado_em)
              select gen_random_uuid(), @faturaId, @clienteId, @valor, @dataPagamento, @formaPagamento, @mensagem, 'A', now(), now()
             where not exists (select 1 from plantaopro.pagamentos_saas where fatura_id=@faturaId and reg_status='A')",
            new { faturaId, clienteId, valor, dataPagamento, formaPagamento, mensagem }, tx, cancellationToken: ct));
        await RegistrarEventoFinanceiroAsync(cn, tx, clienteId, faturaId, "FATURA_PAGA", mensagem, ct);
        await cn.ExecuteAsync(new CommandDefinition(
            @"update plantaopro.cliente_alertas
                 set resolvido=true, reg_update=now()
               where cliente_id=@clienteId and tipo in ('INADIMPLENCIA','COBRANCA','CONTESTACAO') and resolvido=false and reg_status='A'
                 and not exists (select 1 from plantaopro.faturas_saas where cliente_id=@clienteId and status in ('VENCIDA','EM_CONTESTACAO') and reg_status='A')",
            new { clienteId }, tx, cancellationToken: ct));
    }

    private static Task RegistrarEventoFinanceiroAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid clienteId, Guid faturaId, string tipo, string mensagem, CancellationToken ct)
        => cn.ExecuteAsync(new CommandDefinition(
            @"insert into plantaopro.cobranca_eventos(id, cliente_id, fatura_id, tipo, mensagem, reg_status, reg_date)
              values(gen_random_uuid(), @clienteId, @faturaId, @tipo, @mensagem, 'A', now())",
            new { clienteId, faturaId, tipo, mensagem }, tx, cancellationToken: ct));

    private static ApiResponse<WebhookRetornoDto> Responder(string resultado, string mensagem)
        => ApiResponse<WebhookRetornoDto>.Ok(new WebhookRetornoDto(resultado, mensagem), mensagem);

    // -------------------------------------------------------------- assinatura HMAC

    internal static string HexAssinatura(string segredo, byte[] corpo)
        => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(segredo), corpo)).ToLowerInvariant();

    internal static bool AssinaturaValida(string? header, string segredo, byte[] corpo)
    {
        if (string.IsNullOrWhiteSpace(header)) return false;
        var idx = header.IndexOf('=', StringComparison.Ordinal);
        if (idx <= 0) return false;
        if (header[..idx].Trim().ToLowerInvariant() != "sha256") return false;
        var esperado = HexAssinatura(segredo, corpo);
        var recebido = header[(idx + 1)..].Trim().ToLowerInvariant();
        if (recebido.Length != esperado.Length) return false;
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(recebido), Encoding.ASCII.GetBytes(esperado));
    }

    private static string? Texto(JsonElement root, params string[] nomes)
    {
        foreach (var nome in nomes)
        {
            if (root.TryGetProperty(nome, out var el))
            {
                if (el.ValueKind == JsonValueKind.String) return el.GetString();
                if (el.ValueKind != JsonValueKind.Null && el.ValueKind != JsonValueKind.False && el.ValueKind != JsonValueKind.True) return el.ToString();
            }
        }
        return null;
    }

    private static decimal? Numero(JsonElement root, params string[] nomes)
    {
        foreach (var nome in nomes)
        {
            if (root.TryGetProperty(nome, out var el))
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var v)) return v;
                if (el.ValueKind == JsonValueKind.String && decimal.TryParse(el.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var s)) return s;
            }
        }
        return null;
    }

    // -------------------------------------------------------------- linhas internas (Dapper)

    private sealed class ProviderRow
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Modo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    private sealed class ProviderStateRow
    {
        public string Modo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    private sealed class FaturaCobrarRow
    {
        public Guid ClienteId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateOnly Vencimento { get; set; }
    }

    private sealed class CobrancaResumoRow
    {
        public Guid CobrancaId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Referencia { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime CriadoEm { get; set; }
        public string CheckoutPath { get; set; } = string.Empty;
    }

    private sealed class SimulacaoRow
    {
        public string Estado { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Modo { get; set; } = string.Empty;
    }

    private sealed class WebhookCobrancaRow
    {
        public Guid CobrancaId { get; set; }
        public Guid FaturaId { get; set; }
        public Guid ClienteId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }

    private sealed class WebhookFaturaRow
    {
        public string Estado { get; set; } = string.Empty;
    }
}
