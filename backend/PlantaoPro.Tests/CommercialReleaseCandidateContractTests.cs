namespace PlantaoPro.Tests;

public class CommercialReleaseCandidateContractTests
{
    [Fact]
    public void PropostaComercial_DeveExporProvisionamentoCompletoDaConversao()
    {
        var tipo = typeof(PlantaoPro.Api.Models.CommercialProposalConversionDto);
        var propriedades = tipo.GetProperties().Select(p => p.Name).ToArray();

        Assert.Contains("PropostaId", propriedades);
        Assert.Contains("TenantId", propriedades);
        Assert.Contains("ClienteId", propriedades);
        Assert.Contains("AssinaturaId", propriedades);
        Assert.Contains("AdminClienteId", propriedades);
        Assert.Contains("OnboardingId", propriedades);
        Assert.Contains("ModoPagamento", propriedades);
        Assert.Contains("EtapasProvisionadas", propriedades);
    }

    [Fact]
    public void ConversaoComercial_DeveExigirPropostaAprovadaEIdentificarPagamentoSandboxManual()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var arquivo = Path.Combine(RepositoryPathResolver.ApiRoot, "CommercialDemoService.cs");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("Apenas propostas aprovadas podem ser convertidas em cliente", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Já existe cliente provisionado para esta empresa/CNPJ informado", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MANUAL_SANDBOX", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("gateway real não configurado", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CRIAR_TENANT_ASSINATURA_ONBOARDING", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DemoMode_DeveRegistrarContadoresOperacionaisMarcadosComoDemo()
    {
        var tipo = typeof(PlantaoPro.Api.Models.DemoStatusDto);
        var propriedades = tipo.GetProperties().Select(p => p.Name).ToArray();

        Assert.Contains("TenantsDemo", propriedades);
        Assert.Contains("ClientesDemo", propriedades);
        Assert.Contains("MedicosDemo", propriedades);
        Assert.Contains("HospitaisDemo", propriedades);
        Assert.Contains("PlantoesDemo", propriedades);
        Assert.Contains("ConvitesDemo", propriedades);
        Assert.Contains("EscalasDemo", propriedades);
        Assert.Contains("PagamentosDemo", propriedades);
        Assert.Contains("FaturasDemo", propriedades);
        Assert.Contains("PropostasDemo", propriedades);
        Assert.Contains("ParceirosDemo", propriedades);
    }
}
