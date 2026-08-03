using System.Net.Http.Headers; using System.Text.Json; using PlantaoPro.Web.Models;
namespace PlantaoPro.Web.Services;
public sealed class MinhaCentralWebService
{
 private readonly IHttpClientFactory factory; private static readonly JsonSerializerOptions Options=new(){PropertyNameCaseInsensitive=true}; public MinhaCentralWebService(IHttpClientFactory factory)=>this.factory=factory;
 public async Task<MinhaCentralViewModel> GetAsync(string token,CancellationToken ct){var client=factory.CreateClient("PlantaoProApi");client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);using var response=await client.GetAsync("api/minha-central",ct);if(!response.IsSuccessStatusCode)return new(){Error="Não conseguimos carregar suas pendências. Verifique a conexão e tente novamente."};return await response.Content.ReadFromJsonAsync<MinhaCentralViewModel>(Options,ct)??new(){Error="A Central retornou uma resposta sem conteúdo."};}
}
