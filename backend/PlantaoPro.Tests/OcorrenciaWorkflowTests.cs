using PlantaoPro.Domain.Ocorrencias;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class OcorrenciaWorkflowTests
{
    private static string ServiceSource => File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "OcorrenciaService.cs"));

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
    public void Listagem_restringe_nao_gestores_a_ocorrencias_acessiveis()
    {
        Assert.Contains("(@gestor or o.solicitante_id=@user or o.responsavel_id=@user)", ServiceSource, StringComparison.Ordinal);
        Assert.Contains("gestor=Gestor", ServiceSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Entidades_legadas_usam_contexto_do_cliente()
    {
        Assert.Contains("h.cliente_id=@cliente", ServiceSource, StringComparison.Ordinal);
        Assert.Contains("p.cliente_id=@cliente", ServiceSource, StringComparison.Ordinal);
        Assert.Contains("cliente_id=@cliente", ServiceSource, StringComparison.Ordinal);
        Assert.Contains("cliente=Cliente", ServiceSource, StringComparison.Ordinal);
    }
}
