using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R5-E13/P2: contrato do scheduler de contratos AGENDADO (kernel B4). A transicao AGENDADO->ATIVO
/// so pode ocorrer por dado vencido e por caminho conhecido; estas assercoes travam que o scheduler
/// (a) usa o mesmo kernel transacional da rota manual, (b) nao finge usuario logado (ator do tick e
/// o sistema), (c) nao roda na Testing (a suite xUnit compartilha o banco) e (d) continua registrado.
/// </summary>
public sealed class SaasAgendadosSchedulerContractTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void Kernel_RotaManualMantemGateESchedulerUsaMesmoNucleo()
    {
        var service = Read("backend/PlantaoPro.Api/SaasCoreServices.cs");
        var rota = service.Substring(service.IndexOf("public async Task<ApiResponse<int>> AtivarAgendadosAsync", StringComparison.Ordinal), 400);
        var programado = service.Substring(service.IndexOf("Task<ApiResponse<int>> AtivarAgendadosProgramadoAsync", StringComparison.Ordinal), 200);

        // A rota manual continua exigindo ADMINISTRADOR_GLOBAL (gate B4 intacto).
        Assert.Contains("currentUser.IsGlobalAdmin()", rota);
        Assert.Contains("AtivarVencidosAsync(currentUser.UserId, ip, ct)", rota);
        // O scheduler entra pelo MESMO nucleo, com ator do sistema (usuario NULL, ip SCHEDULER).
        Assert.Contains("AtivarVencidosAsync(null, \"SCHEDULER\", ct)", programado);
        // Nucleo unico: a transicao vencida so existe num lugar (AGENDADO && ativado_em<=now, FOR UPDATE).
        Assert.Equal(1, CountOccurrences(service, "upper(coalesce(status,''))='AGENDADO' and ativado_em is not null and ativado_em<=now() for update"));
        // Auditoria com usuario logado so na rota; no tick a trilha canonica e o historico.
        Assert.Contains("if (due.Count > 0 && usuarioId.HasValue)", service);
    }

    [Fact]
    public void Scheduler_NaoRodaEmTestingCompartilhaConfigEAoFalhaNaoDerrubaHost()
    {
        var host = Read("backend/PlantaoPro.Api/SaasModulosAgendadosHostedService.cs");

        // Suite xUnit roda em Testing contra banco compartilhado: scheduler precisa ficar mudo la.
        Assert.Contains("IsEnvironment(\"Testing\")", host);
        Assert.Contains("Saas:AtivarAgendados:IntervaloSegundos", host);
        Assert.Contains("intervaloSegundos <= 0", host);
        // Servico e scoped: um escopo por tick, sem contexto HTTP.
        Assert.Contains("scopeFactory.CreateScope()", host);
        Assert.Contains("AtivarAgendadosProgramadoAsync(stoppingToken)", host);
        // Tick falho e logado e retomado; o host nao cai (padrao B7).
        Assert.Contains("catch (Exception ex)", host);
    }

    [Fact]
    public void Scheduler_EstaRegistradoNaHost()
    {
        var program = Read("backend/PlantaoPro.Api/Program.cs");
        Assert.Contains("AddHostedService<SaasModulosAgendadosHostedService>()", program);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal)) count++;
        return count;
    }
}
