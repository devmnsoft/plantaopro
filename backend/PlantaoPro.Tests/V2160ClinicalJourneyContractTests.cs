using PlantaoPro.Api;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class V2160ClinicalJourneyContractTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void ValoresIncomunsDaTriagem_SaoAlertasENaoBloqueios()
    {
        var request = new TriagemUpdateRequest { ClassificacaoRisco = "URGENTE", Temperatura = 50, Saturacao = 30 };
        Assert.Empty(ClinicalMeasurements.Validar(request, true));
        Assert.Equal(2, ClinicalMeasurements.AlertasConferencia(request).Count);
        Assert.Null(ClinicalMeasurements.CalcularImc(null, 1.70m));
        Assert.Null(ClinicalMeasurements.CalcularImc(70, null));
    }

    [Fact]
    public void Consulta_NaoExigeDiagnosticoDefinitivoOuCidPorPadrao()
    {
        var service = Read("backend/PlantaoPro.Api/Clinical/ConsultaApplicationService.cs");
        Assert.Contains("Diagnóstico não informado; confirme", service);
        Assert.Contains("CID principal não informado; confirme", service);
        Assert.DoesNotContain("p.Add(\"Preencha o diagnóstico.\")", service);
        Assert.Contains("versao=@Versao", service);
    }

    [Fact]
    public void Persistencia_ProtegeAtendimentoAtivoSnapshotEEncaminhamentoUnico()
    {
        var sql = Read("database/schema/380_v2160_triagem_consulta_jornada.sql");
        Assert.Contains("ux_v2160_atendimento_agendamento_ativo", sql);
        Assert.Contains("ux_v2160_encaminhamento_triagem_consulta", sql);
        Assert.Contains("triagem_snapshot", sql);
        Assert.Contains("assumida_por", sql);
        Assert.Contains("versao integer not null", sql);
        Assert.Contains("V2160_ATENDIMENTOS_ATIVOS_DUPLICADOS", sql);
        Assert.Contains("t.paciente_id=new.paciente_id", sql);
        Assert.Contains("before insert", sql);
        Assert.Contains("before update of triagem_id", sql);
        Assert.DoesNotContain("before insert or update of triagem_id", sql);
    }

    [Fact]
    public void WorkspaceClinico_NaoUsaCacheNemPerdeTextoEmConflitoOuSaida()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/ConsultasWorkspaceController.cs");
        var script = Read("backend/PlantaoPro.Web/wwwroot/js/clinical-workspace.js");
        Assert.Contains("ResponseCache(NoStore = true", controller);
        Assert.Contains("beforeunload", script);
        Assert.Contains("data-conflict-modal", script);
        Assert.DoesNotContain("localStorage", script);
    }

    [Fact]
    public void Finalizacao_RejeitaEReverteVinculoOperacionalIncompativel()
    {
        var service = Read("backend/PlantaoPro.Api/Clinical/ConsultaApplicationService.cs");
        Assert.Contains("atendimentoAtualizado != 1", service);
        Assert.Contains("agendamentoAtualizado != 1", service);
        Assert.Contains("paciente_id=@pacienteId and unidade_id=@unidadeId", service);
        Assert.Contains("Nenhuma finalização foi gravada", service);
    }
}
