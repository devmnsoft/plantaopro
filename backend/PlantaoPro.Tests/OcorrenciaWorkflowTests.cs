using PlantaoPro.Domain.Ocorrencias;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class OcorrenciaWorkflowTests
{
    [Theory]
    [InlineData("ABERTA","EM_ATENDIMENTO",true,true)]
    [InlineData("ABERTA","RESOLVIDA",true,false)]
    [InlineData("EM_ATENDIMENTO","RESOLVIDA",false,true)]
    [InlineData("RESOLVIDA","ABERTA",false,false)]
    [InlineData("RESOLVIDA","ABERTA",true,true)]
    public void Aplica_matriz_de_transicoes(string atual,string destino,bool gestor,bool esperado)
        => Assert.Equal(esperado,OcorrenciaWorkflow.PodeTransicionar(atual,destino,gestor));

    [Fact] public void Catalogos_nao_misturam_prioridade_e_situacao()
    { Assert.Contains("CRITICA",OcorrenciaWorkflow.Prioridades); Assert.DoesNotContain("CRITICA",OcorrenciaWorkflow.Situacoes); }

    [Fact]
    public void Servico_UsaAliasesVerbatimValidosEProtegeTenantVersaoEHistorico()
    {
        var service = File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, "backend/PlantaoPro.Api/OcorrenciaService.cs"));
        Assert.DoesNotContain("as \\\"Id\\\"", service);
        Assert.Contains("as \"\"Id\"\"", service);
        Assert.Contains("@gestor or o.solicitante_id=@user or o.responsavel_id=@user", service);
        Assert.Contains("versao=@versao", service);
        Assert.Contains("changed!=1", service);
        Assert.Contains("insert into plantaopro.ocorrencia_eventos", service, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delete from plantaopro.ocorrencias_operacionais", service, StringComparison.OrdinalIgnoreCase);
    }
}
