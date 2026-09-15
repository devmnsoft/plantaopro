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
}
