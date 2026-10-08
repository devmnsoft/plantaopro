using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

// ============================================================================
// B4 - Solicitacoes de plano (upgrade/downgrade/cancelamento self-service).
// Sem dead-end: toda SOLICITADO/CANCELAMENTO_SOLICITADO tem leitura (admin +
// cliente) e decisao B2B (aprovar/recusar) com transicao de estado idempotente.
// Aprovar upgrade/downgrade = alterar plano imediato (mesmo efeito do manual
// B2B: UPDATE plano_id + historico ALTERAR_PLANO); aprovar cancelamento =
// CANCELADA com motivo/data. Concorrencia: advisory lock por cliente (mesmo
// padrao da criacao/reativacao de assinatura) + UPDATE condicional por status.
// ============================================================================
public sealed class SolicitacoesPlanosService
{
    private readonly IConfiguration _cfg;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly AssinaturaGuardService _guard;
    private readonly ILogger<SolicitacoesPlanosService> _logger;

    public SolicitacoesPlanosService(
        IConfiguration cfg,
        ICurrentUserService currentUser,
        IAuditService audit,
        AssinaturaGuardService guard,
        ILogger<SolicitacoesPlanosService> logger)
    {
        _cfg = cfg;
        _currentUser = currentUser;
        _audit = audit;
        _guard = guard;
        _logger = logger;
    }

    private NpgsqlConnection Connection() => new(_cfg.GetConnectionString("Default"));

    private static string Tabela(string tipo) =>
        string.Equals(tipo, "UPGRADE", StringComparison.OrdinalIgnoreCase)
            ? "plantaopro.upgrade_solicitacoes"
            : "plantaopro.downgrade_solicitacoes";

    private static string NormalizarTipo(string? tipo, bool cancelamento)
    {
        if (cancelamento) return "CANCELAMENTO";
        var t = (tipo ?? string.Empty).Trim().ToUpperInvariant();
        return t == "DOWNGRADE" ? "DOWNGRADE" : "UPGRADE";
    }

    public async Task<ApiResponse<IEnumerable<SolicitacaoPlanoDto>>> ListarAsync(string? tipo, string? status, CancellationToken ct)
    {
        try
        {
            await using var cn = Connection();
            var rows = new List<SolicitacaoPlanoDto>();
            foreach (var tabela in new[] { "plantaopro.upgrade_solicitacoes", "plantaopro.downgrade_solicitacoes" })
            {
                var kind = tabela.Contains("upgrade") ? "UPGRADE" : "DOWNGRADE";
                if (!string.IsNullOrWhiteSpace(tipo) && !string.Equals(tipo.Trim(), kind, StringComparison.OrdinalIgnoreCase)
                    && !(string.Equals(tipo.Trim(), "CANCELAMENTO", StringComparison.OrdinalIgnoreCase) && kind == "DOWNGRADE"))
                    continue;
                rows.AddRange(await cn.QueryAsync<SolicitacaoPlanoDto>(new CommandDefinition($@"select s.id as ""Id"",
       case when upper(coalesce(s.status,''))='CANCELAMENTO_SOLICITADO' then 'CANCELAMENTO' else '{kind}' end as ""Tipo"",
       s.tenant_id as ""TenantId"", s.cliente_id as ""ClienteId"", s.assinatura_id as ""AssinaturaId"",
       s.plano_atual_id as ""PlanoAtualId"", coalesce(pa.nome,'') as ""PlanoAtualNome"",
       s.plano_destino_id as ""PlanoDestinoId"", coalesce(pd.nome,'') as ""PlanoDestinoNome"",
       coalesce(s.motivo,'') as ""Motivo"", coalesce(s.status,'') as ""Status"",
       s.reg_date as ""SolicitadoEm"", s.decidido_em as ""DecididoEm"",
       coalesce(s.justificativa_decisao,'') as ""JustificativaDecisao""
  from {tabela} s
  left join plantaopro.planos pa on pa.id=s.plano_atual_id
  left join plantaopro.planos pd on pd.id=s.plano_destino_id
 where s.reg_status='A'
   and (@status='' or upper(coalesce(s.status,''))=upper(@status)
        or (@status='CANCELAMENTO' and upper(coalesce(s.status,''))='CANCELAMENTO_SOLICITADO'))
 order by s.reg_date desc limit 200", new { status = (status ?? string.Empty).Trim().ToUpperInvariant() }, cancellationToken: ct)));
            }
            return ApiResponse<IEnumerable<SolicitacaoPlanoDto>>.Ok(rows.OrderByDescending(r => r.SolicitadoEm));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar solicitacoes de plano");
            return ApiResponse<IEnumerable<SolicitacaoPlanoDto>>.Fail("Não foi possível listar as solicitações.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<MinhaSolicitacaoPlanoDto>>> MinhasAsync(CancellationToken ct)
    {
        try
        {
            var tenantId = _currentUser.TenantId;
            if (!tenantId.HasValue) return ApiResponse<IEnumerable<MinhaSolicitacaoPlanoDto>>.Fail("Selecione uma organização.", 403);
            await using var cn = Connection();
            var rows = new List<MinhaSolicitacaoPlanoDto>();
            foreach (var tabela in new[] { "plantaopro.upgrade_solicitacoes", "plantaopro.downgrade_solicitacoes" })
            {
                var kind = tabela.Contains("upgrade") ? "UPGRADE" : "DOWNGRADE";
                rows.AddRange(await cn.QueryAsync<MinhaSolicitacaoPlanoDto>(new CommandDefinition($@"select s.id as ""Id"",
       case when upper(coalesce(s.status,''))='CANCELAMENTO_SOLICITADO' then 'CANCELAMENTO' else '{kind}' end as ""Tipo"",
       coalesce(pd.nome,'') as ""PlanoDestinoNome"", coalesce(s.status,'') as ""Status"", s.reg_date as ""SolicitadoEm"",
       case upper(coalesce(s.status,''))
         when 'SOLICITADO' then 'Aguardando avaliacao comercial. Nada mudou no seu plano.'
         when 'CANCELAMENTO_SOLICITADO' then 'Aguardando avaliacao comercial. Seu acesso continua ativo.'
         when 'APROVADA' then 'Aprovada e aplicada. Confira os dados atuais da assinatura.'
         when 'RECUSADA' then 'Recusada pela avaliacao comercial. Nada mudou no seu plano.'
         else 'Verifique a situacao com o atendimento comercial.'
       end as ""MensagemEstado""
  from {tabela} s
  left join plantaopro.planos pd on pd.id=s.plano_destino_id
 where s.reg_status='A' and s.tenant_id=@tenantId
 order by s.reg_date desc limit 50", new { tenantId = tenantId.Value }, cancellationToken: ct)));
            }
            return ApiResponse<IEnumerable<MinhaSolicitacaoPlanoDto>>.Ok(rows.OrderByDescending(r => r.SolicitadoEm));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar minhas solicitacoes de plano");
            return ApiResponse<IEnumerable<MinhaSolicitacaoPlanoDto>>.Fail("Não foi possível listar as solicitações.", 500);
        }
    }

    public async Task<ApiResponse<string>> DecidirAsync(string tipo, Guid id, bool aprovar, string? justificativa, string? ip, CancellationToken ct)
    {
        var cancelamento = string.Equals(tipo, "CANCELAMENTO", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tipo, "Cancelar", StringComparison.OrdinalIgnoreCase);
        var tabela = Tabela(tipo);
        if (!aprovar && string.IsNullOrWhiteSpace(justificativa))
            return ApiResponse<string>.Fail("Justificativa obrigatória para recusar.", 400);
        try
        {
            await using var cn = Connection();
            await cn.OpenAsync(ct);
            await using var tx = await cn.BeginTransactionAsync(ct);
            var row = await cn.QueryFirstOrDefaultAsync<SolicitacaoRow>(new CommandDefinition(
                $"select id, tenant_id as \"TenantId\", cliente_id as \"ClienteId\", assinatura_id as \"AssinaturaId\", plano_atual_id as \"PlanoAtualId\", plano_destino_id as \"PlanoDestinoId\", coalesce(motivo,'') as \"Motivo\", coalesce(status,'') as \"Status\" from {tabela} where id=@id and reg_status='A' for update",
                new { id }, tx, cancellationToken: ct));
            if (row is null)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<string>.Fail("Solicitação não encontrada.", 404);
            }
            if (string.Equals(row.Status, "APROVADA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.Status, "RECUSADA", StringComparison.OrdinalIgnoreCase))
            {
                await tx.CommitAsync(ct);
                return ApiResponse<string>.Ok("ok", $"Solicitação já estava {row.Status.ToLowerInvariant()}. Nenhuma alteração aplicada.");
            }
            var pendente = string.Equals(row.Status, "SOLICITADO", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.Status, "CANCELAMENTO_SOLICITADO", StringComparison.OrdinalIgnoreCase);
            if (!pendente)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<string>.Fail($"Solicitação no estado {row.Status} não pode ser decidida.", 409);
            }
            if (cancelamento && !string.Equals(row.Status, "CANCELAMENTO_SOLICITADO", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<string>.Fail("Esta solicitação não é de cancelamento.", 409);
            }
            if (!cancelamento && string.Equals(row.Status, "CANCELAMENTO_SOLICITADO", StringComparison.OrdinalIgnoreCase))
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<string>.Fail("Esta solicitação é de cancelamento: decida pela rota de cancelamento.", 409);
            }
            if (!cancelamento && !row.PlanoDestinoId.HasValue)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<string>.Fail("Solicitação sem plano destino.", 409);
            }

            // Serializa por cliente: a troca/cancelamento de plano usa o mesmo lock
            // da criação/reativação de assinatura (índice único ATIVA/TRIAL arbitra).
            if (row.ClienteId.HasValue)
                await cn.ExecuteAsync(new CommandDefinition("select pg_advisory_xact_lock(hashtextextended('assinaturas:' || @cliente::text, 0))", new { cliente = row.ClienteId.Value }, tx, cancellationToken: ct));

            var assinatura = row.AssinaturaId.HasValue
                ? await cn.QueryFirstOrDefaultAsync<AssinaturaRow>(new CommandDefinition(
                    "select id, cliente_id as \"ClienteId\", plano_id as \"PlanoId\", coalesce(status,'') as \"Status\" from plantaopro.assinaturas where id=@id and reg_status='A' for update",
                    new { id = row.AssinaturaId.Value }, tx, cancellationToken: ct))
                : null;
            if (assinatura is null)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<string>.Fail("Assinatura da solicitação não encontrada.", 404);
            }

            // A natureza da linha manda: linha de downgrade (tabela + SOLICITADO)
            // aprovada por qualquer rota de troca passa pela revalidacao de uso.
            var ehDowngrade = string.Equals(tabela, "plantaopro.downgrade_solicitacoes", StringComparison.Ordinal)
                && string.Equals(row.Status, "SOLICITADO", StringComparison.OrdinalIgnoreCase);
            if (aprovar && !cancelamento)
            {
                var destinoAtivo = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
                    "select exists(select 1 from plantaopro.planos where id=@id and reg_status='A' and upper(status)='ATIVO')",
                    new { id = row.PlanoDestinoId!.Value }, tx, cancellationToken: ct));
                if (!destinoAtivo)
                {
                    await tx.RollbackAsync(ct);
                    return ApiResponse<string>.Fail("Plano destino inativo: a solicitação não pode ser aprovada.", 409);
                }
                if (ehDowngrade)
                {
                    var uso = await _guard.ObterUsoPlanoAsync(assinatura.ClienteId);
                    var destino = await cn.QueryFirstOrDefaultAsync<DestinoRow>(new CommandDefinition(
                        "select limite_medicos as \"LimiteMedicos\", limite_hospitais as \"LimiteHospitais\", limite_plantoes_mes as \"LimitePlantoesMes\", coalesce(limite_usuarios,0) as \"LimiteUsuarios\" from plantaopro.planos where id=@id",
                        new { id = row.PlanoDestinoId!.Value }, tx, cancellationToken: ct));
                    if (uso.Success && uso.Data is not null && destino is not null
                        && ((destino.LimiteMedicos > 0 && uso.Data.MedicosUsados > destino.LimiteMedicos)
                            || (destino.LimiteHospitais > 0 && uso.Data.HospitaisUsados > destino.LimiteHospitais)
                            || (destino.LimitePlantoesMes > 0 && uso.Data.PlantoesMesUsados > destino.LimitePlantoesMes)
                            || (destino.LimiteUsuarios > 0 && uso.Data.UsuariosUsados > destino.LimiteUsuarios)))
                    {
                        await tx.RollbackAsync(ct);
                        return ApiResponse<string>.Fail("Uso atual excede os limites do plano destino: a solicitação não pode ser aprovada.", 409);
                    }
                }
                await cn.ExecuteAsync(new CommandDefinition(
                    "update plantaopro.assinaturas set plano_id=@destino, reg_update=now() where id=@id",
                    new { id = assinatura.Id, destino = row.PlanoDestinoId!.Value }, tx, cancellationToken: ct));
                await cn.ExecuteAsync(new CommandDefinition(
                    @"insert into plantaopro.assinatura_historico(id, assinatura_id, cliente_id, plano_id_anterior, plano_id_novo, acao, justificativa, status_novo, reg_status, reg_date)
values(gen_random_uuid(), @assinatura, @cliente, @anterior, @novo, 'ALTERAR_PLANO', @justificativa, @statusNovo, 'A', now())",
                    new { assinatura = assinatura.Id, cliente = assinatura.ClienteId, anterior = assinatura.PlanoId, novo = row.PlanoDestinoId!.Value, justificativa = $"Aprovacao B2B de {NormalizarTipo(tipo, false).ToLowerInvariant()} ({id}). " + (justificativa ?? string.Empty), statusNovo = assinatura.Status }, tx, cancellationToken: ct));
            }

            if (aprovar && cancelamento)
            {
                await cn.ExecuteAsync(new CommandDefinition(
                    "update plantaopro.assinaturas set status='CANCELADA', motivo_cancelamento=@motivo, data_cancelamento=now(), reg_update=now() where id=@id",
                    new { id = assinatura.Id, motivo = "Cancelamento aprovado na avaliacao comercial: " + row.Motivo }, tx, cancellationToken: ct));
            }

            var novoStatus = aprovar ? "APROVADA" : "RECUSADA";
            var marcadas = await cn.ExecuteAsync(new CommandDefinition(
                $"update {tabela} set status=@novo, decidido_por=@user, decidido_em=now(), justificativa_decisao=@justificativa, reg_update=now() where id=@id and reg_status='A' and upper(status)=upper(@atual)",
                new { id, novo = novoStatus, user = _currentUser.UserId, justificativa = justificativa?.Trim() ?? string.Empty, atual = row.Status }, tx, cancellationToken: ct));
            if (marcadas == 0)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<string>.Fail("Solicitação alterada por outra decisão simultânea.", 409);
            }
            await tx.CommitAsync(ct);
            await _audit.RegistrarAsync(_currentUser.UserId, assinatura.ClienteId, AuditoriaConstants.Entidades.Assinatura, assinatura.Id,
                AuditoriaConstants.Acoes.AlterarStatus, new { solicitacao = id, tipo = NormalizarTipo(tipo, cancelamento), decisao = novoStatus, justificativa }, true, ip, "ADMINISTRADOR_GLOBAL");
            return ApiResponse<string>.Ok("ok", aprovar ? "Solicitação aprovada e aplicada." : "Solicitação recusada. Nada mudou no plano.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao decidir solicitacao de plano {Tipo} {Id}", tipo, id);
            return ApiResponse<string>.Fail("Não foi possível decidir a solicitação.", 500);
        }
    }

    private sealed class SolicitacaoRow
    {
        public Guid Id { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? ClienteId { get; set; }
        public Guid? AssinaturaId { get; set; }
        public Guid? PlanoAtualId { get; set; }
        public Guid? PlanoDestinoId { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    private sealed class AssinaturaRow
    {
        public Guid Id { get; set; }
        public Guid ClienteId { get; set; }
        public Guid PlanoId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private sealed class DestinoRow
    {
        public int LimiteMedicos { get; set; }
        public int LimiteHospitais { get; set; }
        public int LimitePlantoesMes { get; set; }
        public int LimiteUsuarios { get; set; }
    }
}
