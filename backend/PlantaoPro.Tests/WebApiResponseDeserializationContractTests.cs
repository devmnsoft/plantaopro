namespace PlantaoPro.Tests;

public class WebApiResponseDeserializationContractTests
{
    [Fact]
    public void BaseWebController_DeveDesserializarEnvelopeApiResponseTipadoERegistrarFalhas()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var arquivo = Path.Combine(RepositoryPathResolver.WebRoot, "Controllers", "BaseWebController.cs");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("JsonSerializer.Deserialize<ApiResponse<T>>", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LooksLikeApiResponseEnvelope", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TryReadEnvelopeStatus", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("API retornou envelope sem sucesso", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ResponseSample", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BuildApiErrorMessage", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HttpStatusCode.Forbidden", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HttpStatusCode.NotFound", conteudo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ComunicacaoController_DeveUsarLeitoresPadronizadosDaBaseParaApiResponse()
    {
        var raiz = RepositoryPathResolver.RepoRoot;
        var arquivo = Path.Combine(RepositoryPathResolver.WebRoot, "Controllers", "ComunicacaoController.cs");
        var conteudo = File.ReadAllText(arquivo);

        Assert.Contains("ReadApiResponse<IEnumerable<ConversaResumoDto>>", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ReadApiResponse<ConversaDetalhesViewModel>", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ReadApiListResponseAsync<UsuarioConversaOpcaoDto>", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SendApiAsync<object, Guid>", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SendApiWithoutResponseAsync", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("JsonSerializer.Deserialize<ApiResponse", conteudo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ReadAsStringAsync", conteudo, StringComparison.OrdinalIgnoreCase);
    }
}
