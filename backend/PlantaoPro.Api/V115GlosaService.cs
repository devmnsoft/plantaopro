using Dapper;
using Npgsql;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class V115GlosaService
{
    private readonly IConfiguration cfg; private readonly ICurrentUserService currentUser; private readonly FinancialTenantContext context; private readonly IAuditService audit; private readonly ILogger<V115GlosaService> logger;
    public V115GlosaService(IConfiguration cfg, ICurrentUserService currentUser, FinancialTenantContext context, IAuditService audit, ILogger<V115GlosaService> logger) { this.cfg = cfg; this.currentUser = currentUser; this.context = context; this.audit = audit; this.logger = logger; }
    private NpgsqlConnection Cn() => new NpgsqlConnection(cfg.GetConnectionString("Default")); private Guid? TenantId => currentUser.ClienteId ?? currentUser.TenantId; private Guid? UserId => currentUser.UserId; private bool Global => currentUser.IsGlobalAdmin(); private string Perfil => string.Join(',', currentUser.Roles);

    public async Task<ApiResponse<IEnumerable<GlosaDto>>> ListarAsync() { if (!context.HasTenant) return context.MissingTenant<IEnumerable<GlosaDto>>(); try { await using var cn=Cn(); var rows=await cn.QueryAsync<GlosaDto>("select id,conta_receber_id as contaReceberId,titulo_id as tituloId,convenio_id as convenioId,motivo,valor_glosado as valorGlosado,coalesce(valor_recuperado,0) as valorRecuperado,prazo_recurso as prazoRecurso,status,lote_item_id as loteItemId,recurso_justificativa as recursoJustificativa,recurso_em as recursoEm from plantaopro.v115_regras_glosa where reg_status='A' and tenant_id=@tenantId order by created_at desc", new { tenantId=context.TenantId }); return ApiResponse<IEnumerable<GlosaDto>>.Ok(rows,"Glosas carregadas."); } catch(Exception ex) { return Fail<IEnumerable<GlosaDto>>(ex,"Falha ao listar glosas."); } }

    public async Task<ApiResponse<object>> RegistrarAsync(V115GlosaRequest request)
    {
        if(!context.HasTenant) return context.MissingTenant<object>();
        try {
            if(request.ContaReceberId is null || request.ConvenioId is null || string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<object>.Fail("Conta, convenio e motivo sao obrigatorios.",422);
            if (request.ValorGlosado <= 0) return ApiResponse<object>.Fail("Valor glosado deve ser informado ou calculado pela regra do convênio.", 400);
            Guid? loteItemId=null;
            var prioridade = (request.PrazoRecurso.HasValue && request.PrazoRecurso.Value <= DateTime.UtcNow.AddDays(2)) ? "ALTA" : "MEDIA";
            await using var cn = Cn();
            if(cn.State != System.Data.ConnectionState.Open) await cn.OpenAsync();
            await using var tx = cn.BeginTransaction();
            try {
                var elegivel=await cn.ExecuteScalarAsync<decimal?>("select valor from plantaopro.v113_faturas where id=@id and tenant_id=@tenantId and reg_status='A'",new{id=request.ContaReceberId,tenantId=context.TenantId},tx);
                if(!elegivel.HasValue){tx.Rollback();return ApiResponse<object>.Fail("Conta nao pertence ao tenant.",404);}
                if(request.ValorGlosado>elegivel.Value){tx.Rollback();return ApiResponse<object>.Fail("Glosa excede valor elegivel.",422);}
                // R8-B7: granularidade por item de lote — quando informado, valida a pertença ao tenant.
                if(request.LoteItemId is not null){ var okItem=await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.v116_faturamento_lote_itens where id=@id and tenant_id=@tenantId and coalesce(reg_status,'ATIVO')<>'EXCLUIDO'",new{id=request.LoteItemId,tenantId=context.TenantId},tx); if(okItem==0){tx.Rollback();return ApiResponse<object>.Fail("Item de lote nao pertence ao tenant.",404);} loteItemId=request.LoteItemId; }
                var id = Guid.NewGuid();
                await cn.ExecuteAsync("insert into plantaopro.v115_regras_glosa(id,cliente_id,tenant_id,conta_receber_id,titulo_id,convenio_id,motivo,valor_glosado,prazo_recurso,lote_item_id,status,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@contaReceberId,@tituloId,@convenioId,@motivo,@valorGlosado,@prazoRecurso,@loteItemId,'ABERTA','A',now(),@userId)", ScopeWith(new { id, contaReceberId = request.ContaReceberId, tituloId = request.TituloId, convenioId = request.ConvenioId, motivo = request.Motivo, valorGlosado = request.ValorGlosado, prazoRecurso = request.PrazoRecurso, loteItemId, userId = UserId }), tx);
                // R8-B7: a glosa reconhecida abate o saldo (coluna que a leitura do saldo ja descontava sem writer).
                await cn.ExecuteAsync("update plantaopro.v113_faturas set glosa_reconhecida=glosa_reconhecida+@valorGlosado where id=@contaReceberId and tenant_id=@tenantId and reg_status='A'", ScopeWith(new { valorGlosado = request.ValorGlosado, contaReceberId = request.ContaReceberId }), tx);
                await cn.ExecuteAsync("insert into plantaopro.v115_alertas_operacionais(id,cliente_id,tenant_id,tipo,prioridade,perfil_responsavel,modulo,entidade_id,cta,rota,prazo,status,origem_regra,reg_status,created_at,created_by) values(gen_random_uuid(),@tenantId,@tenantId,'GLOSA_ANALISAR',@prioridade,'FATURAMENTO_CONVENIO','Faturamento Clínico',@id,'Resolver glosa','/FaturamentoClinico/Glosas',@prazoRecurso,'ABERTA','v115_glosa','A',now(),@userId)", ScopeWith(new { id, prioridade, prazoRecurso = request.PrazoRecurso, userId = UserId }), tx);
                tx.Commit();
                await AuditAsync("v115_glosas", id, "REGISTRAR_GLOSA", new { request.ConvenioId, request.Motivo, loteItemId });
                return ApiResponse<object>.Ok(new { id, status = "ABERTA", request.ValorGlosado }, "Glosa registrada com alerta operacional.");
            } catch { tx.Rollback(); throw; }
        } catch (Exception ex) { return Fail<object>(ex, "Falha ao registrar glosa."); }
    }

    public async Task<ApiResponse<object>> ResolverAsync(Guid id, V115ResolucaoGlosaRequest request)
    {
        if(!context.HasTenant)return context.MissingTenant<object>();
        if(string.IsNullOrWhiteSpace(request.Motivo))return ApiResponse<object>.Fail("Resultado da glosa obrigatorio.",422);
        try {
            await using var cn = Cn();
            if(cn.State != System.Data.ConnectionState.Open) await cn.OpenAsync();
            await using var tx = cn.BeginTransaction();
            try {
                var row = await cn.QueryFirstOrDefaultAsync<dynamic>("select valor_glosado from plantaopro.v115_regras_glosa where id=@id and reg_status='A' and tenant_id=@tenantId", new { id, tenantId=context.TenantId }, tx);
                if(row == null){tx.Rollback();return ApiResponse<object>.Fail("Glosa não encontrada.", 404);}
                var glosado = Convert.ToDecimal(((dynamic)row).valor_glosado);
                if(request.ValorRecuperado.HasValue && (request.ValorRecuperado.Value < 0 || request.ValorRecuperado.Value > glosado)){tx.Rollback();return ApiResponse<object>.Fail("Valor recuperado deve estar entre zero e o valor glosado.", 422);}
                var affected = await cn.ExecuteAsync("update plantaopro.v115_regras_glosa set status='RESOLVIDA',resolucao=@motivo,valor_recuperado=coalesce(@vr,0),updated_at=now(),updated_by=@userId where id=@id and reg_status='A' and tenant_id=@tenantId", ScopeWith(new { id, motivo = request.Motivo, vr=(decimal?)request.ValorRecuperado, userId = UserId }), tx);
                if(affected==0){tx.Rollback();return ApiResponse<object>.Fail("Glosa não encontrada.", 404);}
                // R8-B7: o valor recuperado devolve ao saldo (glosa reconhecida diminui, piso em zero).
                if(request.ValorRecuperado.HasValue && request.ValorRecuperado.Value > 0){
                    var contaId = await cn.ExecuteScalarAsync<Guid?>("select conta_receber_id from plantaopro.v115_regras_glosa where id=@id and tenant_id=@tenantId", new { id, tenantId=context.TenantId }, tx);
                    if(contaId.HasValue) await cn.ExecuteAsync("update plantaopro.v113_faturas set glosa_reconhecida=greatest(0,glosa_reconhecida-@vr) where id=@contaId and tenant_id=@tenantId and reg_status='A'", ScopeWith(new { vr=(decimal)request.ValorRecuperado.Value, contaId }), tx);
                }
                tx.Commit();
                await AuditAsync("v115_glosas", id, "RESOLVER_GLOSA", new { request.Motivo, request.ValorRecuperado });
                return ApiResponse<object>.Ok(new { id, status = "RESOLVIDA", request.ValorRecuperado }, "Glosa resolvida.");
            } catch { tx.Rollback(); throw; }
        } catch (Exception ex) { return Fail<object>(ex, "Falha ao resolver glosa."); }
    }

    public async Task<ApiResponse<object>> RecorrerAsync(Guid id, V115RecursoRequest request)
    {
        if(!context.HasTenant)return context.MissingTenant<object>();
        if(string.IsNullOrWhiteSpace(request.Justificativa))return ApiResponse<object>.Fail("Recurso de glosa exige justificativa.",400);
        var docs = request.Documentos ?? Array.Empty<V115RecursoDocumentoRequest>();
        foreach(var d in docs){ if(string.IsNullOrWhiteSpace(d.NomeArquivo)) return ApiResponse<object>.Fail("Documento de recurso exige nome de arquivo.",422); }
        try {
            await using var cn = Cn();
            if(cn.State != System.Data.ConnectionState.Open) await cn.OpenAsync();
            await using var tx = cn.BeginTransaction();
            try {
                var st = await cn.ExecuteScalarAsync<string?>("select status from plantaopro.v115_regras_glosa where id=@id and reg_status='A' and tenant_id=@tenantId", new { id, tenantId=context.TenantId }, tx);
                if(st is null){tx.Rollback();return ApiResponse<object>.Fail("Glosa não encontrada.",404);}
                if(st != "ABERTA"){tx.Rollback();return ApiResponse<object>.Fail("Glosa ja possui recurso ou foi resolvida.",409);}
                var affected = await cn.ExecuteAsync("update plantaopro.v115_regras_glosa set status='EM_RECURSO',recurso_justificativa=@justificativa,recurso_em=now(),updated_at=now(),updated_by=@userId where id=@id and reg_status='A' and status='ABERTA' and tenant_id=@tenantId", ScopeWith(new { id, justificativa=request.Justificativa, userId=UserId }), tx);
                if(affected==0){tx.Rollback();return ApiResponse<object>.Fail("Glosa ja possui recurso ou foi resolvida.",409);}
                foreach(var d in docs){
                    await cn.ExecuteAsync("insert into plantaopro.v115_recurso_documentos(id,tenant_id,cliente_id,glosa_id,nome_arquivo,tipo_mime,conteudo,tamanho,created_at,created_by,reg_status) values(gen_random_uuid(),@tenantId,@tenantId,@glosaId,@nome,@mime,@conteudo,@tamanho,now(),@userId,'A')", ScopeWith(new { glosaId=id, nome=d.NomeArquivo, mime=d.TipoMime, conteudo=d.ConteudoBase64, tamanho=string.IsNullOrEmpty(d.ConteudoBase64)?0:d.ConteudoBase64.Length, userId=UserId }), tx);
                }
                tx.Commit();
                await AuditAsync("v115_glosas", id, "RECORRER_GLOSA", new { request.Justificativa, documentos=docs.Select(d=>d.NomeArquivo).ToArray() });
                return ApiResponse<object>.Ok(new { id, status="EM_RECURSO" }, "Recurso de glosa registrado.");
            } catch { tx.Rollback(); throw; }
        } catch (Exception ex) { return Fail<object>(ex, "Falha ao recorrer glosa."); }
    }

    public async Task<ApiResponse<IEnumerable<V115RecursoDocumentoDto>>> DocumentosAsync(Guid glosaId)
    {
        if(!context.HasTenant)return context.MissingTenant<IEnumerable<V115RecursoDocumentoDto>>();
        try {
            await using var cn=Cn();
            var existe = await cn.ExecuteScalarAsync<string?>("select status from plantaopro.v115_regras_glosa where id=@id and reg_status='A' and tenant_id=@tenantId", new { id=glosaId, tenantId=context.TenantId });
            if(existe is null) return ApiResponse<IEnumerable<V115RecursoDocumentoDto>>.Fail("Glosa não encontrada.",404);
            var rows = await cn.QueryAsync<V115RecursoDocumentoDto>("select id,nome_arquivo as nomeArquivo,tipo_mime as tipoMime,conteudo as conteudoBase64,tamanho,created_at as criadoEm from plantaopro.v115_recurso_documentos where glosa_id=@id and reg_status='A' and tenant_id=@tenantId order by created_at", new { id=glosaId, tenantId=context.TenantId });
            return ApiResponse<IEnumerable<V115RecursoDocumentoDto>>.Ok(rows,"Documentos carregados.");
        } catch(Exception ex){return Fail<IEnumerable<V115RecursoDocumentoDto>>(ex,"Falha ao listar documentos.");}
    }

    public async Task<ApiResponse<IEnumerable<AlertaFinanceiroDto>>> AlertasAsync() { if(!context.HasTenant)return context.MissingTenant<IEnumerable<AlertaFinanceiroDto>>(); try { await using var cn=Cn(); var rows=await cn.QueryAsync<AlertaFinanceiroDto>("select id,tipo,prioridade,entidade_id as entidadeId,cta,rota,prazo,status from plantaopro.v115_alertas_operacionais where reg_status='A' and tenant_id=@tenantId order by prioridade, prazo nulls last",new{tenantId=context.TenantId}); return ApiResponse<IEnumerable<AlertaFinanceiroDto>>.Ok(rows,"Alertas carregados."); } catch(Exception ex){return Fail<IEnumerable<AlertaFinanceiroDto>>(ex,"Falha ao listar alertas.");} }

    private async Task AuditAsync(string entity, Guid id, string action, object details) => await audit.RegistrarAsync(UserId, TenantId, entity, id, action, details, true, null, Perfil);
    private ApiResponse<T> Fail<T>(Exception ex, string message) { logger.LogError(ex, "V1.15 glosa: {Message}", message); return ApiResponse<T>.Fail(message, 500); }
    private object ScopeWith(object values) { var dict = new Dictionary<string, object?> { { "tenantId", TenantId } }; foreach (var p in values.GetType().GetProperties()) dict[p.Name] = p.GetValue(values); return dict; }
}
public sealed record V115GlosaRequest(Guid? ContaReceberId, Guid? TituloId, Guid? ConvenioId, string Motivo, decimal ValorGlosado, DateTime? PrazoRecurso, Guid? LoteItemId = null);
