using Dapper;
using Npgsql;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Text.Json;

namespace PlantaoPro.Api;

public sealed class V115FaturamentoRegraService
{
    private readonly IConfiguration cfg;
    private readonly ICurrentUserService currentUser;
    private readonly IAuditService audit;
    private readonly ILogger<V115FaturamentoRegraService> logger;

    public V115FaturamentoRegraService(IConfiguration cfg, ICurrentUserService currentUser, IAuditService audit, ILogger<V115FaturamentoRegraService> logger)
    {
        this.cfg = cfg;
        this.currentUser = currentUser;
        this.audit = audit;
        this.logger = logger;
    }

    private NpgsqlConnection Cn() => new NpgsqlConnection(cfg.GetConnectionString("Default"));
    private Guid? TenantId => currentUser.ClienteId ?? currentUser.TenantId;
    private Guid? UserId => currentUser.UserId;
    private bool Global => currentUser.IsGlobalAdmin();
    private string Perfil => string.Join(',', currentUser.Roles);

    public async Task<ApiResponse<IEnumerable<object>>> ListarRegrasAsync(string? tipo = null) => await QueryList(@"select id,codigo,nome,tipo_faturamento as tipoFaturamento,valor_base as valorBase,percentual_desconto as percentualDesconto,percentual_acrescimo as percentualAcrescimo,convenio_id as convenioId,item_faturavel_id as itemFaturavelId,status,created_at as criadoEm from plantaopro.v115_regras_faturamento where reg_status='A' and (@tipo is null or tipo_faturamento=@tipo) and (@isGlobal or @tenantId is null or tenant_id=@tenantId or cliente_id=@tenantId) order by created_at desc", "Regras de faturamento v1.15 carregadas.", ScopeWith(new { tipo = Normalize(tipo) }));

    public async Task<ApiResponse<V115RegraFaturamentoDto>> SalvarRegraAsync(V115RegraFaturamentoRequest request, Guid? id = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<V115RegraFaturamentoDto>.Fail("Código e nome da regra são obrigatórios.", 400);
            if (request.ValorBase < 0) return ApiResponse<V115RegraFaturamentoDto>.Fail("Valor base não pode ser negativo.", 400);
            await using var cn = Cn();
            var regraId = id ?? Guid.NewGuid();
            if (id.HasValue)
            {
                var affected = await cn.ExecuteAsync(@"update plantaopro.v115_regras_faturamento set codigo=@codigo,nome=@nome,tipo_faturamento=@tipoFaturamento,item_faturavel_id=@itemFaturavelId,convenio_id=@convenioId,valor_base=@valorBase,percentual_desconto=@percentualDesconto,percentual_acrescimo=@percentualAcrescimo,status=@status,updated_at=now(),updated_by=@userId where id=@id and reg_status='A' and (@isGlobal or @tenantId is null or tenant_id=@tenantId or cliente_id=@tenantId)", Params(request, regraId));
                if (affected == 0) return ApiResponse<V115RegraFaturamentoDto>.Fail("Regra de faturamento não encontrada para o tenant.", 404);
            }
            else
            {
                await cn.ExecuteAsync(@"insert into plantaopro.v115_regras_faturamento(id,cliente_id,tenant_id,codigo,nome,tipo_faturamento,item_faturavel_id,convenio_id,valor_base,percentual_desconto,percentual_acrescimo,status,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@codigo,@nome,@tipoFaturamento,@itemFaturavelId,@convenioId,@valorBase,@percentualDesconto,@percentualAcrescimo,@status,'A',now(),@userId)", Params(request, regraId));
            }
            await AuditAsync("v115_regras_faturamento", regraId, id.HasValue ? "ATUALIZAR_REGRA" : "CRIAR_REGRA", new { request.Codigo, request.TipoFaturamento });
            return ApiResponse<V115RegraFaturamentoDto>.Ok(new V115RegraFaturamentoDto(regraId, request.Codigo, request.Nome, Normalize(request.TipoFaturamento) ?? "CONSULTA", request.ValorBase, request.PercentualDesconto, request.PercentualAcrescimo, request.ItemFaturavelId, request.ConvenioId, Normalize(request.Status) ?? "ATIVA"), "Regra de faturamento salva.");
        }
        catch (Exception ex) { return Fail<V115RegraFaturamentoDto>(ex, "Falha ao salvar regra de faturamento."); }
    }

    public async Task<ApiResponse<object>> GerarContaConsultaAsync(Guid consultaId, V115GerarContaRequest? request = null) => await GerarContaAsync(consultaId, "CONSULTA", request);
    public async Task<ApiResponse<object>> GerarContaPlantaoAsync(Guid plantaoId, V115GerarContaRequest? request = null) => await GerarContaAsync(plantaoId, "PLANTAO", request);
    public async Task<ApiResponse<IEnumerable<object>>> ContasReceberAsync() => await QueryList("select id,pedido_id as referenciaId,valor,status,created_at as emitidaEm from plantaopro.v113_faturas where reg_status='A' and (@isGlobal or @tenantId is null or tenant_id=@tenantId or cliente_id=@tenantId) order by created_at desc", "Contas a receber reais carregadas.");

    public async Task<ApiResponse<object>> ReceberAsync(Guid contaId, V115RecebimentoRequest request)
    {
        try
        {
            await using var cn = Cn();
            var valor = await cn.ExecuteScalarAsync<decimal?>("select valor from plantaopro.v113_faturas where id=@contaId and reg_status='A' and (@isGlobal or @tenantId is null or tenant_id=@tenantId or cliente_id=@tenantId)", ScopeWith(new { contaId }));
            if (!valor.HasValue) return ApiResponse<object>.Fail("Conta a receber não encontrada.", 404);
            var recebido = request.ValorRecebido <= 0 ? valor.Value : request.ValorRecebido;
            var id = Guid.NewGuid();
            await cn.ExecuteAsync("insert into plantaopro.v115_recebimentos(id,cliente_id,tenant_id,conta_receber_id,valor_recebido,forma,status,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@contaId,@recebido,@forma,'RECEBIDO','A',now(),@userId); update plantaopro.v113_faturas set status='PAID',updated_at=now(),updated_by=@userId where id=@contaId", ScopeWith(new { id, contaId, recebido, forma = Normalize(request.Forma) ?? "MANUAL_AUDITADO", userId = UserId }));
            await RegistrarEventoAsync(cn, "RECEBIMENTO_REGISTRADO", contaId, new { contaId, recebido });
            return ApiResponse<object>.Ok(new { id, contaId, valorRecebido = recebido, status = "RECEBIDO" }, "Recebimento registrado com auditoria financeira.");
        }
        catch (Exception ex) { return Fail<object>(ex, "Falha ao registrar recebimento."); }
    }

    private async Task<ApiResponse<object>> GerarContaAsync(Guid referenciaId, string tipo, V115GerarContaRequest? request)
    {
        try
        {
            await using var cn = Cn();
            var regra = await cn.QueryFirstOrDefaultAsync<dynamic>(@"select id,codigo,valor_base,percentual_desconto,percentual_acrescimo from plantaopro.v115_regras_faturamento where reg_status='A' and status='ATIVA' and tipo_faturamento=@tipo and (@convenioId is null or convenio_id is null or convenio_id=@convenioId) and (@itemFaturavelId is null or item_faturavel_id is null or item_faturavel_id=@itemFaturavelId) and (@isGlobal or @tenantId is null or tenant_id=@tenantId or cliente_id=@tenantId) order by convenio_id nulls last,item_faturavel_id nulls last,created_at desc limit 1", ScopeWith(new { tipo, convenioId = request == null ? null : request.ConvenioId, itemFaturavelId = request == null ? null : request.ItemFaturavelId }));
            if (regra == null) return ApiResponse<object>.Fail("Regra de faturamento ausente para " + tipo + ". Configure item faturável/convênio antes de gerar conta real.", 400);
            decimal baseValor = regra.valor_base;
            decimal desconto = regra.percentual_desconto;
            decimal acrescimo = regra.percentual_acrescimo;
            var valorCalculado = decimal.Round(baseValor - (baseValor * desconto / 100m) + (baseValor * acrescimo / 100m), 2);
            var contaId = Guid.NewGuid();
            var tituloId = Guid.NewGuid();
            await cn.ExecuteAsync("insert into plantaopro.v113_faturas(id,cliente_id,tenant_id,pedido_id,valor,status,reg_status,created_at,created_by) values(@contaId,@tenantId,@tenantId,@referenciaId,@valor,'ISSUED','A',now(),@userId); insert into plantaopro.v113_titulos(id,cliente_id,tenant_id,fatura_id,valor,status,demo_boleto,vencimento,reg_status,created_at,created_by) values(@tituloId,@tenantId,@tenantId,@contaId,@valor,'OPEN',false,now()+interval '7 days','A',now(),@userId)", ScopeWith(new { contaId, tituloId, referenciaId, valor = valorCalculado, userId = UserId }));
            await RegistrarEventoAsync(cn, "CONTA_REAL_GERADA", contaId, new { referenciaId, tipo, regraId = regra.id, valorCalculado });
            await AuditAsync("v115_contas_receber", contaId, "GERAR_CONTA_" + tipo, new { referenciaId, regraId = regra.id });
            return ApiResponse<object>.Ok(new { contaReceberId = contaId, tituloId, referenciaId, tipoFaturamento = tipo, valorCalculado, regraAplicada = regra.codigo, status = "ISSUED", mensagens = new List<string> { "Valor calculado por regra configurável v1.15." } }, "Conta a receber gerada por regra real.");
        }
        catch (Exception ex) { return Fail<object>(ex, "Falha ao gerar conta por regra real."); }
    }

    public async Task RegistrarEventoAsync(NpgsqlConnection cn, string tipo, Guid entidadeId, object payload)
    {
        await cn.ExecuteAsync("insert into plantaopro.v115_faturamento_eventos(id,cliente_id,tenant_id,tipo,entidade_id,payload,status,reg_status,created_at,created_by) values(gen_random_uuid(),@tenantId,@tenantId,@tipo,@entidadeId,cast(@payload as jsonb),'PENDENTE','A',now(),@userId)", new { tenantId = TenantId, userId = UserId, tipo, entidadeId, payload = JsonSerializer.Serialize(payload) });
        await cn.ExecuteAsync("insert into plantaopro.v113_outbox_eventos(id,cliente_id,tenant_id,tipo,payload_ref,payload,status,reg_status,created_at,created_by) values(gen_random_uuid(),@tenantId,@tenantId,@tipo,@payloadRef,cast(@payload as jsonb),'PENDING','A',now(),@userId)", new { tenantId = TenantId, userId = UserId, tipo, payloadRef = entidadeId.ToString(), payload = JsonSerializer.Serialize(payload) });
    }

    private object Params(V115RegraFaturamentoRequest r, Guid id) => ScopeWith(new { id, userId = UserId, codigo = r.Codigo, nome = r.Nome, tipoFaturamento = Normalize(r.TipoFaturamento) ?? "CONSULTA", itemFaturavelId = r.ItemFaturavelId, convenioId = r.ConvenioId, valorBase = r.ValorBase, percentualDesconto = r.PercentualDesconto, percentualAcrescimo = r.PercentualAcrescimo, status = Normalize(r.Status) ?? "ATIVA" });
    private async Task<ApiResponse<IEnumerable<object>>> QueryList(string sql, string ok, object? args = null) { try { await using var cn = Cn(); var rows = await cn.QueryAsync<object>(sql, args ?? ScopeWith(new { })); return ApiResponse<IEnumerable<object>>.Ok(rows.ToList(), ok); } catch (Exception ex) { return Fail<IEnumerable<object>>(ex, ok); } }
    private async Task AuditAsync(string entity, Guid id, string action, object details) => await audit.RegistrarAsync(UserId, TenantId, entity, id, action, details, true, null, Perfil);
    private ApiResponse<T> Fail<T>(Exception ex, string message) { logger.LogError(ex, "V1.15 faturamento: {Message}", message); return ApiResponse<T>.Fail(message, 500); }
    private object ScopeWith(object values) { var dict = new Dictionary<string, object?> { { "tenantId", TenantId }, { "isGlobal", Global } }; foreach (var p in values.GetType().GetProperties()) dict[p.Name] = p.GetValue(values); return dict; }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}

public sealed record V115RegraFaturamentoDto(Guid Id, string Codigo, string Nome, string TipoFaturamento, decimal ValorBase, decimal PercentualDesconto, decimal PercentualAcrescimo, Guid? ItemFaturavelId, Guid? ConvenioId, string Status);
public sealed record V115RegraFaturamentoRequest(string Codigo, string Nome, string TipoFaturamento, decimal ValorBase, decimal PercentualDesconto, decimal PercentualAcrescimo, Guid? ItemFaturavelId, Guid? ConvenioId, string? Status);
public sealed record V115GerarContaRequest(Guid? ItemFaturavelId, Guid? ConvenioId, Guid? PacienteId, Guid? MedicoId);
public sealed record V115RecebimentoRequest(decimal ValorRecebido, string? Forma, string? Observacao);
