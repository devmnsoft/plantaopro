using Dapper;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class GestaoDashboardRepository : Adm360Repository, IGestaoDashboardRepository
{
    public GestaoDashboardRepository(string connectionString)
        : base(connectionString)
    {
    }

    public async Task<IReadOnlyList<string>> ListarCapacidadesAtivasAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<string>(new CommandDefinition(@"
            SELECT capacidade FROM plantaopro.adm360_capacidades_contratadas
            WHERE tenant_id = @tenantId AND habilitado = true",
            new { tenantId }, cancellationToken: ct));

        return rows.ToList();
    }

    public async Task<DashboardAdm360Dto> ObterDashboardAsync(Guid tenantId, FiltroDashboardDto filtro, CancellationToken ct = default)
    {
        await using var cn = Connection();

        // 1. Indicadores de Cotações
        var cotacoesSql = @"
            SELECT
                COUNT(*) AS total_recebidas,
                COUNT(CASE WHEN status_interno IN ('RECEBIDA', 'EM_RELACIONAMENTO') THEN 1 END) AS aguardando_relacionamento,
                COUNT(CASE WHEN prazo_resposta <= now() + interval '24 hours' AND status_interno NOT IN ('RESPONDIDA', 'CANCELADA', 'EXPIRADA') THEN 1 END) AS proximas_do_prazo,
                COUNT(CASE WHEN orcamento_id IS NOT NULL THEN 1 END) AS orcamentos_gerados,
                COUNT(CASE WHEN status_interno = 'RESPONDIDA' THEN 1 END) AS respostas_aceitas,
                (SELECT COUNT(*) FROM plantaopro.adm360_cotacao_respostas r
                 JOIN plantaopro.adm360_cotacoes c2 ON c2.id = r.cotacao_id AND c2.tenant_id = r.tenant_id
                 WHERE r.tenant_id = @tenantId AND r.status_comercial_externo = 'VENCEDORA') AS propostas_vencedoras,
                (SELECT COUNT(*) FROM plantaopro.adm360_cotacao_respostas r
                 JOIN plantaopro.adm360_cotacoes c2 ON c2.id = r.cotacao_id AND c2.tenant_id = r.tenant_id
                 WHERE r.tenant_id = @tenantId AND r.status_comercial_externo = 'PERDIDA') AS propostas_perdidas,
                (SELECT COUNT(*) FROM plantaopro.adm360_cotacao_respostas r
                 WHERE r.tenant_id = @tenantId AND r.status_transmissao IN ('REJEITADA_PELO_PORTAL', 'RESULTADO_DESCONHECIDO')) AS falhas_integracao,
                COALESCE(AVG(CASE WHEN c.status_interno = 'RESPONDIDA' THEN EXTRACT(EPOCH FROM (c.updated_at - c.capturada_em)) / 3600.0 END), 0.0) AS tempo_medio_horas
            FROM plantaopro.adm360_cotacoes c
            WHERE c.tenant_id = @tenantId
              AND (@estabId::uuid IS NULL OR c.estabelecimento_id = @estabId::uuid)
              AND (@provedor::text IS NULL OR c.provedor = @provedor::text)
              AND (@dataInicio::timestamptz IS NULL OR c.capturada_em >= @dataInicio::timestamptz)
              AND (@dataFim::timestamptz IS NULL OR c.capturada_em <= @dataFim::timestamptz)";

        var cRow = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(cotacoesSql, new
        {
            tenantId,
            estabId = filtro.EstabelecimentoId,
            provedor = filtro.Provedor,
            dataInicio = filtro.DataInicio?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            dataFim = filtro.DataFim?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)
        }, cancellationToken: ct));

        int totalRecebidas = (int)(cRow?.total_recebidas ?? 0);
        int aguardandoRelacionamento = (int)(cRow?.aguardando_relacionamento ?? 0);
        int proximasDoPrazo = (int)(cRow?.proximas_do_prazo ?? 0);
        int orcamentosGerados = (int)(cRow?.orcamentos_gerados ?? 0);
        int respostasAceitas = (int)(cRow?.respostas_aceitas ?? 0);
        int propostasVencedoras = (int)(cRow?.propostas_vencedoras ?? 0);
        int propostasPerdidas = (int)(cRow?.propostas_perdidas ?? 0);
        int falhasIntegracao = (int)(cRow?.falhas_integracao ?? 0);
        double tempoMedio = Convert.ToDouble(cRow?.tempo_medio_horas ?? 0.0);

        int totalFinalizadas = propostasVencedoras + propostasPerdidas;
        decimal taxaSucesso = totalFinalizadas > 0
            ? Math.Round(((decimal)propostasVencedoras / totalFinalizadas) * 100m, 2)
            : (totalRecebidas > 0 ? Math.Round(((decimal)respostasAceitas / totalRecebidas) * 100m, 2) : 0m);

        var cotacoesIndicadores = new CotacoesIndicadoresDto(
            totalRecebidas,
            aguardandoRelacionamento,
            proximasDoPrazo,
            orcamentosGerados,
            respostasAceitas,
            propostasVencedoras,
            propostasPerdidas,
            taxaSucesso,
            Math.Round(tempoMedio, 1),
            falhasIntegracao
        );

        // 2. Indicadores de XML Recebidos
        var xmlSql = @"
            SELECT
                COUNT(*) AS total_recebidos,
                COUNT(CASE WHEN tipo_documento = 'NFE_COMPLETA' THEN 1 END) AS completos,
                COUNT(CASE WHEN tipo_documento = 'RESUMO' THEN 1 END) AS resumos,
                COUNT(CASE WHEN quarentena = true THEN 1 END) AS quarentenados,
                COUNT(CASE WHEN status_conferencia = 'PENDENTE' AND quarentena = false THEN 1 END) AS pendentes_conferencia,
                COUNT(CASE WHEN status_conferencia = 'VINCULADO' THEN 1 END) AS vinculados
            FROM plantaopro.adm360_documentos_recebidos d
            WHERE d.tenant_id = @tenantId
              AND (@estabId::uuid IS NULL OR d.estabelecimento_id = @estabId::uuid)
              AND (@dataInicio::timestamptz IS NULL OR d.data_emissao >= @dataInicio::timestamptz)
              AND (@dataFim::timestamptz IS NULL OR d.data_emissao <= @dataFim::timestamptz)";

        var xRow = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(xmlSql, new
        {
            tenantId,
            estabId = filtro.EstabelecimentoId,
            dataInicio = filtro.DataInicio?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            dataFim = filtro.DataFim?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)
        }, cancellationToken: ct));

        var xmlIndicadores = new XmlIndicadoresDto(
            (int)(xRow?.total_recebidos ?? 0),
            (int)(xRow?.completos ?? 0),
            (int)(xRow?.resumos ?? 0),
            (int)(xRow?.quarentenados ?? 0),
            (int)(xRow?.pendentes_conferencia ?? 0),
            (int)(xRow?.vinculados ?? 0)
        );

        // 3. Indicadores de Integrações
        var integracoesSql = @"
            SELECT
                COUNT(CASE WHEN ativo = true AND status_integracao = 'CONFIGURADA' THEN 1 END) AS contas_ativas,
                COUNT(CASE WHEN status_integracao = 'BLOQUEADA' OR status_integracao = 'ERRO_AUTENTICACAO' THEN 1 END) AS conexoes_bloqueadas,
                MAX(ultima_sincronizacao) AS ultima_sync
            FROM plantaopro.adm360_portal_contas
            WHERE tenant_id = @tenantId";

        var iRow = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(integracoesSql, new { tenantId }, cancellationToken: ct));

        var integracoesIndicadores = new IntegracoesIndicadoresDto(
            (int)(iRow?.contas_ativas ?? 0),
            (int)(iRow?.conexoes_bloqueadas ?? 0),
            falhasIntegracao,
            iRow?.ultima_sync is not null ? (DateTime?)iRow.ultima_sync : null
        );

        return new DashboardAdm360Dto(
            cotacoesIndicadores,
            xmlIndicadores,
            integracoesIndicadores,
            DateTime.UtcNow
        );
    }
}
