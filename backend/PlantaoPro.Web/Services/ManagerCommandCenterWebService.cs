using System.Net.Http.Headers; using System.Text.Json; using PlantaoPro.Web.Models;
namespace PlantaoPro.Web.Services;
public sealed class ManagerCommandCenterWebService
{
 private readonly IHttpClientFactory factory; private readonly ILogger<ManagerCommandCenterWebService> logger; private static readonly JsonSerializerOptions Options=new(){PropertyNameCaseInsensitive=true};
 public ManagerCommandCenterWebService(IHttpClientFactory factory,ILogger<ManagerCommandCenterWebService> logger){this.factory=factory;this.logger=logger;}
 public async Task<ManagerCommandCenterViewModel> GetAsync(string token,DateOnly from,DateOnly to,string? status,CancellationToken ct)
 { try {var client=factory.CreateClient("PlantaoProApi");client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",token);var url=$"api/manager-command-center?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&status={Uri.EscapeDataString(status??string.Empty)}";using var response=await client.GetAsync(url,ct);if(!response.IsSuccessStatusCode)return ManagerCommandCenterViewModel.Empty("Não foi possível carregar a operação. Confira seu acesso e tente novamente.");var envelope=await response.Content.ReadFromJsonAsync<ApiResponse<ManagerCommandCenterViewModel>>(Options,ct);return envelope?.Data??ManagerCommandCenterViewModel.Empty("A operação não retornou dados.");}catch(Exception ex){logger.LogError(ex,"Falha ao consultar Command Center");return ManagerCommandCenterViewModel.Empty("A conexão com a operação falhou. Tente novamente em instantes.");} }
}
