using Dapper;
using Npgsql;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class V115RepasseMedicoService
{
    private readonly IConfiguration cfg; private readonly ICurrentUserService currentUser; private readonly IAuditService audit; private readonly ILogger<V115RepasseMedicoService> logger;
    public V115RepasseMedicoService(IConfiguration cfg, ICurrentUserService currentUser, IAuditService audit, ILogger<V115RepasseMedicoService> logger) { this.cfg = cfg; this.currentUser = currentUser; this.audit = audit; this.logger = logger; }
    private NpgsqlConnection Cn() => new NpgsqlConnection(cfg.GetConnectionString("Default"));
    private Guid? TenantId => currentUser.ClienteId ?? currentUser.TenantId; private Guid? UserId => currentUser.UserId; private bool Global => currentUser.IsGlobalAdmin(); private string Perfil => string.Join(',', currentUser.Roles);

    public async Task<ApiResponse<IEnumerable<object>>> ListarAsync() => await QueryList("select id,referencia_id as referenciaId,medico_id as medicoId,valor_base as valorBase,valor_repasse as valorRepasse,tipo_regra as tipoRegra,status,created_at as criadoEm from plantaopro.v115_regras_repasse where reg_status='A' and (@isGlobal or @tenantId is null or tenant_id=@tenantId or cliente_id=@tenantId) order by created_at desc", "Repasses médicos v1.15 carregados.");

    public async Task<ApiResponse<object>> GerarAsync(Guid referenciaId, V115RepasseRequest request)
    {
        try
        {
            await using var cn = Cn();
            var existente = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.v115_regras_repasse where referencia_id=@referenciaId and medico_id=@medicoId and reg_status='A' and status in ('PENDENTE','CONFIRMADO') limit 1", ScopeWith(new { referenciaId, request.MedicoId }));
            if (existente.HasValue) return ApiResponse<object>.Fail("Repasse já gerado para a referência e médico informados.", 409);
            var regra = await cn.QueryFirstOrDefaultAsync<dynamic>("select tipo_regra,percentual,valor_fixo,convenio_id from plantaopro.v115_regras_repasse where reg_status='A' and status='REGRA_ATIVA' and (@convenioId is null or convenio_id is null or convenio_id=@convenioId) and (@isGlobal or @tenantId is null or tenant_id=@tenantId or cliente_id=@tenantId) order by convenio_id nulls last,created_at desc limit 1", ScopeWith(new { convenioId = request.ConvenioId }));
            if (regra == null) return ApiResponse<object>.Fail("Regra configurável de repasse ausente.", 400);
            var tipo = ((string)regra.tipo_regra).ToUpperInvariant();
            decimal valorRepasse = tipo == "VALOR_FIXO" ? (decimal)regra.valor_fixo : decimal.Round(request.ValorBase * ((decimal)regra.percentual) / 100m, 2);
            var id = Guid.NewGuid();
            await cn.ExecuteAsync("insert into plantaopro.v115_regras_repasse(id,cliente_id,tenant_id,referencia_id,medico_id,convenio_id,valor_base,valor_repasse,tipo_regra,percentual,valor_fixo,status,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@referenciaId,@medicoId,@convenioId,@valorBase,@valorRepasse,@tipo,@percentual,@valorFixo,'PENDENTE','A',now(),@userId)", ScopeWith(new { id, referenciaId, medicoId = request.MedicoId, convenioId = request.ConvenioId, valorBase = request.ValorBase, valorRepasse, tipo, percentual = (decimal)regra.percentual, valorFixo = (decimal)regra.valor_fixo, userId = UserId }));
            await AuditAsync("v115_repasses_medicos", id, "GERAR_REPASSE", new { referenciaId, request.MedicoId });
            return ApiResponse<object>.Ok(new { id, referenciaId, valorRepasse, status = "PENDENTE", regraAplicada = tipo }, "Repasse médico gerado por regra configurável.");
        }
        catch (Exception ex) { return Fail<object>(ex, "Falha ao gerar repasse médico."); }
    }
    public Task<ApiResponse<object>> ContestarAsync(Guid id, V115ContestacaoRequest request) => AlterarStatusAsync(id, "CONTESTADO", "CONTESTAR_REPASSE", request.Motivo);
    public Task<ApiResponse<object>> ResolverAsync(Guid id, V115ContestacaoRequest request) => AlterarStatusAsync(id, "RESOLVIDO", "RESOLVER_REPASSE", request.Motivo);
    public Task<ApiResponse<object>> ConfirmarAsync(Guid id) => AlterarStatusAsync(id, "CONFIRMADO", "CONFIRMAR_PAGAMENTO", "Pagamento confirmado sem expor dados clínicos.");
    private async Task<ApiResponse<object>> AlterarStatusAsync(Guid id, string status, string action, string? motivo) { try { await using var cn = Cn(); var affected = await cn.ExecuteAsync("update plantaopro.v115_regras_repasse set status=@status,contestacao=coalesce(@motivo,contestacao),updated_at=now(),updated_by=@userId where id=@id and reg_status='A' and (@isGlobal or @tenantId is null or tenant_id=@tenantId or cliente_id=@tenantId)", ScopeWith(new { id, status, motivo, userId = UserId })); if (affected == 0) return ApiResponse<object>.Fail("Repasse não encontrado.", 404); await AuditAsync("v115_repasses_medicos", id, action, new { motivo }); return ApiResponse<object>.Ok(new { id, status }, "Repasse atualizado."); } catch (Exception ex) { return Fail<object>(ex, "Falha ao atualizar repasse."); } }
    private async Task<ApiResponse<IEnumerable<object>>> QueryList(string sql, string ok) { try { await using var cn = Cn(); return ApiResponse<IEnumerable<object>>.Ok((await cn.QueryAsync<object>(sql, ScopeWith(new { }))).ToList(), ok); } catch (Exception ex) { return Fail<IEnumerable<object>>(ex, ok); } }
    private async Task AuditAsync(string entity, Guid id, string action, object details) => await audit.RegistrarAsync(UserId, TenantId, entity, id, action, details, true, null, Perfil);
    private ApiResponse<T> Fail<T>(Exception ex, string message) { logger.LogError(ex, "V1.15 repasse: {Message}", message); return ApiResponse<T>.Fail(message, 500); }
    private object ScopeWith(object values) { var dict = new Dictionary<string, object?> { { "tenantId", TenantId }, { "isGlobal", Global } }; foreach (var p in values.GetType().GetProperties()) dict[p.Name] = p.GetValue(values); return dict; }
}
public sealed record V115RepasseRequest(Guid? MedicoId, Guid? ConvenioId, decimal ValorBase);
public sealed record V115ContestacaoRequest(string? Motivo);
