using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Api.Controllers;

/// <summary>R8-B25 (§27): resposta do dashboard gerencial que carrega as definições fiéis ao SQL dos indicadores.
/// Vive na camada Api porque KpiDefinicao está no projeto Api (o DTO de Aplicação não o referencia).</summary>
public sealed record DashboardAdm360Resposta(
    CotacoesIndicadoresDto Cotacoes,
    XmlIndicadoresDto DocumentosXml,
    IntegracoesIndicadoresDto Integracoes,
    DateTime GeradoEm,
    IReadOnlyList<KpiDefinicao> Definicoes,
    string TimeCriterion);

[ApiController]
[Route("api/administrativo360/gestao")]
public sealed class Adm360GestaoController : ControllerBase
{
    private readonly IGestaoDashboardRepository repository;
    private readonly ICurrentUserService current;
    private readonly ILogger<Adm360GestaoController> logger;

    public Adm360GestaoController(
        IGestaoDashboardRepository repository,
        ICurrentUserService current,
        ILogger<Adm360GestaoController> logger)
    {
        this.repository = repository;
        this.current = current;
        this.logger = logger;
    }

    private (Guid Tenant, Guid User) Context() =>
        (current.TenantId ?? throw new UnauthorizedAccessException("Tenant não identificado."),
         current.UserId ?? throw new UnauthorizedAccessException("Usuário não autenticado."));

    [HttpGet("dashboard")]
    [Authorize(Policy = "Adm360.Ver")]
    public async Task<IActionResult> ObterDashboard(
        [FromQuery] Guid? estabelecimentoId,
        [FromQuery] string? provedor,
        [FromQuery] DateOnly? dataInicio,
        [FromQuery] DateOnly? dataFim,
        [FromQuery] string? situacao,
        CancellationToken ct)
    {
        var (tenant, _) = Context();
        var filtro = new FiltroDashboardDto(
            EstabelecimentoId: estabelecimentoId,
            Provedor: provedor,
            DataInicio: dataInicio,
            DataFim: dataFim,
            Situacao: situacao);

        var dto = await repository.ObterDashboardAsync(tenant, filtro, ct);
        var resposta = new DashboardAdm360Resposta(
            dto.Cotacoes,
            dto.DocumentosXml,
            dto.Integracoes,
            dto.GeradoEm,
            Definicoes(),
            TempoCritico);
        return Ok(resposta);
    }

    [HttpGet("capacidades-ativas")]
    [Authorize(Policy = "Adm360.Ver")]
    public async Task<IActionResult> ListarCapacidadesAtivas(CancellationToken ct)
    {
        var (tenant, _) = Context();
        var lista = await repository.ListarCapacidadesAtivasAsync(tenant, ct);
        return Ok(lista);
    }

    /// <summary>R8-B25 (§27): definições dos indicadores do dashboard gerencial, fiéis ao SQL real de
    /// GestaoDashboardRepository.ObterDashboardAsync (cotacoesSql/xmlSql/integracoesSql) + fórmulas derivadas
    /// calculadas no servidor (taxa de sucesso, tempo médio). Cada texto descreve exatamente o filtro/fórmula — nada inventado.</summary>
    private static IReadOnlyList<KpiDefinicao> Definicoes() => new[]
    {
        new KpiDefinicao("TotalRecebidas", "Cotações recebidas", "Total de cotações capturadas no escopo (tenant + filtros opcionais de estabelecimento/provedor/período por capturada_em); estado atual."),
        new KpiDefinicao("AguardandoRelacionamento", "Aguardando relacionamento", "Cotações com status_interno 'RECEBIDA' ou 'EM_RELACIONAMENTO' (aguardam relacionamento comercial); estado atual."),
        new KpiDefinicao("ProximasDoPrazo", "A vencer em 24h", "Cotações com prazo_resposta em até 24h (prazo_resposta <= now() + interval '24 hours') e ainda abertas (status não RESPONDIDA/CANCELADA/EXPIRADA); estado atual."),
        new KpiDefinicao("OrcamentosGerados", "Com orçamento gerado", "Cotações que geraram orçamento (orcamento_id IS NOT NULL); estado atual."),
        new KpiDefinicao("RespostasAceitas", "Respostas aceitas", "Cotações com status_interno 'RESPONDIDA'; estado atual."),
        new KpiDefinicao("PropostasVencedoras", "Propostas vencedoras", "Respostas de cotação (subselect em adm360_cotacao_respostas) com status_comercial_externo 'VENCEDORA'; escopo apenas tenant (NÃO aplica os filtros de estabelecimento/provedor/período do painel); estado atual."),
        new KpiDefinicao("PropostasPerdidas", "Propostas perdidas", "Respostas de cotação (subselect em adm360_cotacao_respostas) com status_comercial_externo 'PERDIDA'; escopo apenas tenant (NÃO aplica os filtros de estabelecimento/provedor/período do painel); estado atual."),
        new KpiDefinicao("TaxaSucessoPercentual", "Taxa de sucesso (%)", "Calculado no servidor (fórmula derivada, não no banco): vencedoras/(vencedoras+perdidas) quando há desfecho; senão respostasAceitas/totalRecebidas quando há cotações; 0 sem base."),
        new KpiDefinicao("TempoMedioRespostaHoras", "Tempo médio de resposta (h)", "Média (horas) entre captura e resposta das cotações RESPONDIDAS: AVG(EXTRACT(EPOCH FROM (updated_at - capturada_em))/3600); 0 quando não há respondidas; estado atual."),
        new KpiDefinicao("FalhasIntegracao", "Falhas de integração", "Respostas (subselect em adm360_cotacao_respostas) com status_transmissao 'REJEITADA_PELO_PORTAL' ou 'RESULTADO_DESCONHECIDO'; escopo apenas tenant (NÃO aplica os filtros do painel); estado atual."),
        new KpiDefinicao("TotalRecebidos", "Documentos XML recebidos", "Total de documentos XML recebidos no escopo (tenant + estabelecimento + período por data_emissao); estado atual."),
        new KpiDefinicao("Completos", "NF-e completas", "Documentos com tipo_documento 'NFE_COMPLETA'; estado atual."),
        new KpiDefinicao("Resumos", "Resumos", "Documentos com tipo_documento 'RESUMO'; estado atual."),
        new KpiDefinicao("Quarentenados", "Em quarentena", "Documentos com quarentena = true; estado atual."),
        new KpiDefinicao("PendentesConferencia", "Pendentes de conferência", "Documentos com status_conferencia 'PENDENTE' e quarentena = false; estado atual."),
        new KpiDefinicao("Vinculados", "Vinculados", "Documentos com status_conferencia 'VINCULADO'; estado atual."),
        new KpiDefinicao("ContasAtivas", "Contas de portal ativas", "Contas de portal com ativo = true e status_integracao 'CONFIGURADA'; estado atual."),
        new KpiDefinicao("ConexoesBloqueadas", "Conexões bloqueadas", "Contas de portal com status_integracao 'BLOQUEADA' ou 'ERRO_AUTENTICACAO'; estado atual."),
        new KpiDefinicao("FalhasUltimas24h", "Falhas (últimas 24h)", "Mesmo valor de 'Falhas de integração' das cotações repassado ao grupo de integrações; estado atual."),
        new KpiDefinicao("UltimaSincronizacaoGeral", "Última sincronização global", "MAX(ultima_sincronizacao) das contas de portal do tenant; null quando nunca sincronizou; estado atual.")
    };
    private const string TempoCritico = "Escopo por organização (tenant_id) + filtros opcionais de estabelecimento/provedor/período. Cotações filtram por capturada_em e documentos por data_emissao (janelas UTC via timestamptz). 'A vencer' usa agora do servidor (now() + 24h). Integrações são estado-atual (sem janela de data). Taxa de sucesso e tempo médio são derivados calculados no servidor. Nota: as contagens de propostas vencedoras/perdidas e de falhas usam subselect em adm360_cotacao_respostas filtrado SÓ por tenant (NÃO aplicam os filtros de estabelecimento/provedor/período).";
}
