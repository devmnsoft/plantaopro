using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class SaasIntelligenceService
{
    private readonly IConfiguration cfg;
    private readonly AssinaturaGuardService assinaturaGuard;
    private readonly ILogger<SaasIntelligenceService> logger;

    public SaasIntelligenceService(IConfiguration cfg, AssinaturaGuardService assinaturaGuard, ILogger<SaasIntelligenceService> logger)
    {
        this.cfg = cfg;
        this.assinaturaGuard = assinaturaGuard;
        this.logger = logger;
    }

    public async Task<ApiResponse<ClienteSaudeDto>> CalcularSaudeClienteAsync(Guid clienteId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var baseInfo = await cn.QueryFirstOrDefaultAsync<ClienteSaudeBaseRow>(@"select coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       upper(coalesce(c.status,'')) as ""ClienteStatus"",
       (select count(1)::bigint from plantaopro.faturas_saas f where f.cliente_id=c.id and f.status='VENCIDA' and f.reg_status='A') as ""FaturasVencidas"",
       (select max(p.data_inicio) from plantaopro.plantoes p where p.cliente_id=c.id and p.reg_status='A') as ""UltimoPlantao"",
       (select count(1)::bigint from plantaopro.plantoes p where p.cliente_id=c.id and p.reg_status='A' and date_trunc('month', p.data_inicio)=date_trunc('month', now())) as ""PlantoesMes""
from plantaopro.clientes c
where c.id=@clienteId and c.reg_status='A'", new { clienteId });
            if (baseInfo is null || string.IsNullOrWhiteSpace(baseInfo.ClienteNome)) return ApiResponse<ClienteSaudeDto>.Fail("Cliente não encontrado.", 404);

            var usoResponse = await assinaturaGuard.ObterUsoPlanoAsync(clienteId);
            var riscos = new List<string>();
            var oportunidades = new List<string>();
            var acoes = new List<string>();
            var score = 100;

            if (baseInfo.FaturasVencidas > 0)
            {
                score -= 35;
                riscos.Add("Cliente possui faturas SaaS vencidas.");
                acoes.Add("Acionar financeiro para regularização de inadimplência.");
            }

            if (string.Equals(baseInfo.ClienteStatus, "SUSPENSO", StringComparison.OrdinalIgnoreCase) || string.Equals(baseInfo.ClienteStatus, "CANCELADO", StringComparison.OrdinalIgnoreCase))
            {
                score -= 45;
                riscos.Add("Cliente está sem status contratual operacional.");
                acoes.Add("Validar reativação ou plano de recuperação contratual.");
            }

            var inativo = !baseInfo.UltimoPlantao.HasValue || baseInfo.UltimoPlantao.Value < DateTime.UtcNow.AddDays(-30);
            if (inativo)
            {
                score -= 20;
                riscos.Add("Cliente sem plantões publicados nos últimos 30 dias.");
                acoes.Add("Agendar contato de Customer Success para estimular adoção.");
            }

            var usoAlto = false;
            if (usoResponse.Success && usoResponse.Data is not null)
            {
                var uso = usoResponse.Data;
                usoAlto = AtingiuOitentaPorCento(uso.MedicosUsados, uso.MedicosLimite)
                    || AtingiuOitentaPorCento(uso.HospitaisUsados, uso.HospitaisLimite)
                    || AtingiuOitentaPorCento(uso.PlantoesMesUsados, uso.PlantoesMesLimite);
                if (usoAlto)
                {
                    score -= 10;
                    oportunidades.Add("Uso acima de 80% do limite contratado; cliente elegível para upgrade.");
                    acoes.Add("Oferecer comparação de planos e proposta de upgrade.");
                }
            }
            else
            {
                score -= 20;
                riscos.Add("Cliente sem assinatura ativa mapeada para validação de uso.");
                acoes.Add("Regularizar assinatura do cliente.");
            }

            score = Math.Clamp(score, 0, 100);
            var classificacao = score >= 80 ? "SAUDAVEL" : score >= 60 ? "ATENCAO" : score >= 35 ? "RISCO" : "CRITICO";
            var dto = new ClienteSaudeDto
            {
                ClienteId = clienteId,
                ClienteNome = baseInfo.ClienteNome,
                Classificacao = classificacao,
                Score = score,
                Inadimplente = baseInfo.FaturasVencidas > 0,
                UsoAlto = usoAlto,
                Inativo = inativo,
                ElegivelUpgrade = usoAlto,
                Riscos = riscos,
                Oportunidades = oportunidades,
                AcoesRecomendadas = acoes
            };

            await RegistrarSaudeAsync(cn, dto);
            return ApiResponse<ClienteSaudeDto>.Ok(dto, "Saúde do cliente calculada.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao calcular saúde SaaS cliente:{ClienteId}", clienteId);
            return ApiResponse<ClienteSaudeDto>.Fail("Não foi possível calcular a saúde do cliente.", 500);
        }
    }

    public Task<ApiResponse<ClienteSaudeDto>> DetectarRiscoCancelamentoAsync(Guid clienteId) => CalcularSaudeClienteAsync(clienteId);
    public Task<ApiResponse<ClienteSaudeDto>> DetectarOportunidadeUpgradeAsync(Guid clienteId) => CalcularSaudeClienteAsync(clienteId);
    public Task<ApiResponse<ClienteSaudeDto>> DetectarInatividadeAsync(Guid clienteId) => CalcularSaudeClienteAsync(clienteId);
    public Task<ApiResponse<ClienteSaudeDto>> DetectarUsoAltoAsync(Guid clienteId) => CalcularSaudeClienteAsync(clienteId);
    public Task<ApiResponse<ClienteSaudeDto>> DetectarInadimplenciaAsync(Guid clienteId) => CalcularSaudeClienteAsync(clienteId);
    public Task<ApiResponse<ClienteSaudeDto>> GerarResumoExecutivoAsync(Guid clienteId) => CalcularSaudeClienteAsync(clienteId);

    public async Task<ApiResponse<IEnumerable<ClienteAlertaSaasDto>>> GerarAlertasClienteAsync(Guid clienteId)
    {
        var saude = await CalcularSaudeClienteAsync(clienteId);
        if (!saude.Success || saude.Data is null) return ApiResponse<IEnumerable<ClienteAlertaSaasDto>>.Fail(saude.Message, saude.StatusCode);

        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        foreach (var risco in saude.Data.Riscos)
        {
            await InserirAlertaSeNaoExistirAsync(cn, clienteId, "RISCO", saude.Data.Classificacao == "CRITICO" ? "CRITICA" : "ALTA", "Risco comercial detectado", risco);
        }

        foreach (var oportunidade in saude.Data.Oportunidades)
        {
            await InserirAlertaSeNaoExistirAsync(cn, clienteId, "UPGRADE", "MEDIA", "Oportunidade de upgrade", oportunidade);
        }

        var alertas = await ListarAlertasClienteAsync(clienteId);
        return alertas;
    }

    public async Task<ApiResponse<IEnumerable<SaasRecomendacaoDto>>> GerarAcoesRecomendadasAsync(Guid clienteId)
    {
        var saude = await CalcularSaudeClienteAsync(clienteId);
        if (!saude.Success || saude.Data is null) return ApiResponse<IEnumerable<SaasRecomendacaoDto>>.Fail(saude.Message, saude.StatusCode);

        var recomendacoes = saude.Data.AcoesRecomendadas.Select(x => new SaasRecomendacaoDto
        {
            ClienteId = clienteId,
            ClienteNome = saude.Data.ClienteNome,
            Tipo = saude.Data.ElegivelUpgrade ? "UPGRADE" : "CUSTOMER_SUCCESS",
            Titulo = saude.Data.ElegivelUpgrade ? "Abordar upgrade de plano" : "Ação de sucesso do cliente",
            Descricao = x,
            Prioridade = saude.Data.Classificacao == "CRITICO" ? "ALTA" : "MEDIA"
        }).ToArray();
        return ApiResponse<IEnumerable<SaasRecomendacaoDto>>.Ok(recomendacoes);
    }

    public async Task<ApiResponse<SaasResumoExecutivoDto>> GerarResumoSaasAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var resumo = await cn.QuerySingleAsync<SaasResumoExecutivoDto>(@"select
    (select count(1)::bigint from plantaopro.clientes where status='ATIVO' and reg_status='A') as ""ClientesAtivos"",
    (select count(1)::bigint from plantaopro.assinaturas where status='TRIAL' and reg_status='A') as ""ClientesTrial"",
    (select count(1)::bigint from plantaopro.clientes where status='SUSPENSO' and reg_status='A') as ""ClientesSuspensos"",
    (select count(1)::bigint from plantaopro.clientes where status='CANCELADO' and reg_status='A') as ""ClientesCancelados"",
    (select count(1)::bigint from plantaopro.cliente_saude_historico where classificacao='RISCO' and reg_status='A' and reg_date::date=current_date) as ""ClientesRisco"",
    (select count(1)::bigint from plantaopro.cliente_saude_historico where classificacao='CRITICO' and reg_status='A' and reg_date::date=current_date) as ""ClientesCriticos"",
    (select coalesce(sum(valor),0) from plantaopro.faturas_saas where date_trunc('month', competencia)=date_trunc('month', current_date) and status <> 'CANCELADA' and reg_status='A') as ""ReceitaPrevistaMes"",
    (select coalesce(sum(valor_pago),0) from plantaopro.faturas_saas where date_trunc('month', competencia)=date_trunc('month', current_date) and status='PAGA' and reg_status='A') as ""ReceitaRecebidaMes"",
    (select count(1)::bigint from plantaopro.faturas_saas where status='ABERTA' and reg_status='A') as ""FaturasAbertas"",
    (select count(1)::bigint from plantaopro.faturas_saas where status='VENCIDA' and reg_status='A') as ""FaturasVencidas"",
    (select coalesce(sum(valor_contratado),0) from plantaopro.assinaturas where status='ATIVA' and reg_status='A') as ""MrrEstimado"",
    0::numeric as ""ChurnEstimado"",
    (select count(distinct cliente_id)::bigint from plantaopro.cliente_alertas where tipo='USO_ALTO' and resolvido=false and reg_status='A') as ""ClientesProximosLimite"",
    (select count(distinct cliente_id)::bigint from plantaopro.cliente_alertas where tipo='UPGRADE' and resolvido=false and reg_status='A') as ""OportunidadesUpgrade"",
    (select count(1)::bigint from plantaopro.cliente_alertas where resolvido=false and reg_status='A') as ""AlertasAbertos""");
            return ApiResponse<SaasResumoExecutivoDto>.Ok(resumo);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar resumo executivo SaaS");
            return ApiResponse<SaasResumoExecutivoDto>.Fail("Não foi possível carregar o dashboard SaaS.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<ClienteAlertaSaasDto>>> ListarAlertasClienteAsync(Guid clienteId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var alertas = await cn.QueryAsync<ClienteAlertaSaasDto>(@"select a.id as ""Id"", a.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       coalesce(a.tipo,'') as ""Tipo"", coalesce(a.severidade,'') as ""Severidade"", coalesce(a.titulo,'') as ""Titulo"", coalesce(a.mensagem,'') as ""Mensagem"",
       a.resolvido as ""Resolvido"", a.reg_date as ""RegDate""
from plantaopro.cliente_alertas a
join plantaopro.clientes c on c.id=a.cliente_id
where a.cliente_id=@clienteId and a.reg_status='A'
order by a.reg_date desc
limit 100", new { clienteId });
            return ApiResponse<IEnumerable<ClienteAlertaSaasDto>>.Ok(alertas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar alertas cliente:{ClienteId}", clienteId);
            return ApiResponse<IEnumerable<ClienteAlertaSaasDto>>.Fail("Não foi possível listar alertas do cliente.", 500);
        }
    }

    private static bool AtingiuOitentaPorCento(int usado, int limite)
    {
        return limite > 0 && (decimal)usado / limite >= 0.8m;
    }

    private static async Task InserirAlertaSeNaoExistirAsync(NpgsqlConnection cn, Guid clienteId, string tipo, string severidade, string titulo, string mensagem)
    {
        await cn.ExecuteAsync(@"insert into plantaopro.cliente_alertas(id, cliente_id, tipo, severidade, titulo, mensagem, resolvido, reg_status, reg_date)
select gen_random_uuid(), @clienteId, @tipo, @severidade, @titulo, @mensagem, false, 'A', now()
where not exists (
    select 1 from plantaopro.cliente_alertas
    where cliente_id=@clienteId and tipo=@tipo and mensagem=@mensagem and resolvido=false and reg_date::date=current_date
)", new { clienteId, tipo, severidade, titulo, mensagem });
    }

    private static async Task RegistrarSaudeAsync(NpgsqlConnection cn, ClienteSaudeDto dto)
    {
        await cn.ExecuteAsync(@"insert into plantaopro.cliente_saude_historico(id, cliente_id, score, classificacao, riscos, oportunidades, reg_status, reg_date)
values(gen_random_uuid(), @ClienteId, @Score, @Classificacao, @Riscos, @Oportunidades, 'A', now())", new
        {
            dto.ClienteId,
            dto.Score,
            dto.Classificacao,
            Riscos = string.Join("; ", dto.Riscos),
            Oportunidades = string.Join("; ", dto.Oportunidades)
        });
    }

    private sealed class ClienteSaudeBaseRow
    {
        public string ClienteNome { get; set; } = string.Empty;
        public string ClienteStatus { get; set; } = string.Empty;
        public long FaturasVencidas { get; set; }
        public DateTime? UltimoPlantao { get; set; }
        public long PlantoesMes { get; set; }
    }

}
