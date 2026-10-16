using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B17 (validação original) + Rodada 10 r10b2 (CONSOLIDAÇÃO efetivada).
///
/// Histórico: R8-B17 validou por fonte canônica que o Meu Dia NÃO tinha fonte própria —
/// a antiga <c>MeuDiaServices.cs</c> (IMeuDiaService/MeuDiaService/IMeuDiaRepository/MeuDiaRepository)
/// era delegação pura para <c>IProductivityActionService</c> + um PADRÃO DE JANELA fixo
/// ("hoje" = <c>DateTimeOffset.UtcNow.AddDays(1)</c>, PageSize 25; agenda com <c>Module:"CLINICO"</c>)
/// + mapeamento de nomes PT-BR. Zero mudança de produção foi permitida naquela rodada;
/// a consolidação ficou registrada como dívida (r8b17 §5 item 1).
///
/// r10b2 removeu a fachada: <c>MeuDiaController</c> depende AGORA DIRETO de
/// <c>IProductivityActionService</c> (o MESMO serviço da Central de Ações) e o padrão de janela
/// foi relocado para helpers privados do controller (<c>JanelaHoje()</c>/<c>JanelaAgendaClinica()</c>).
/// Este contrato trava a NOVA topologia e o anti-drift original: se alguém der ao Meu Dia uma
/// fonte própria (fachada nova, SQL inline, conexão, repositório-alvo direto), ele falha e força
/// decisão consciente. Caveat UTC documentado: a janela segue em <c>UtcNow</c> (não fuso do
/// tenant/usuário) — mudança de comportamento, registrada como dívida.
/// </summary>
public sealed class R8B17MeuDiaFacadeContratoTests
{
    private static string ApiCtl(string file) =>
        Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.Api", "Controllers", file);

    [Fact]
    public void MeuDia_DependeDiretamenteDoServicoCanonical_E_FachadaFoiRemovida()
    {
        var meuDiaCtl = File.ReadAllText(ApiCtl("MeuDiaController.cs"));
        // O controller Meu Dia consome o único serviço canônico de produtividade.
        Assert.Contains("IProductivityActionService", meuDiaCtl);
        // A fachada foi apagada: sem os símbolos IMeuDiaService/IMeuDiaRepository no projeto Api.
        var apiRoot = Path.Combine(RepositoryPathResolver.BackendRoot, "PlantaoPro.Api");
        Assert.False(File.Exists(Path.Combine(apiRoot, "MeuDiaServices.cs")),
            "r10b2: a fachada Meu Dia (MeuDiaServices.cs) deveria ter sido removida.");
        foreach (var cs in Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories))
        {
            var src = File.ReadAllText(cs);
            Assert.DoesNotContain("IMeuDiaService", src);
            Assert.DoesNotContain("IMeuDiaRepository", src);
        }
    }

    [Fact]
    public void MeuDiaController_SoDelega_E_NaoAbreFontePropria()
    {
        var ctl = File.ReadAllText(ApiCtl("MeuDiaController.cs"));
        // Cada rota delega ao serviço canônico (ListAsync/SummaryAsync/QuickActions).
        Assert.Contains("productivity.ListAsync", ctl);
        Assert.Contains("productivity.SummaryAsync(ct)", ctl);
        Assert.Contains("productivity.QuickActions()", ctl);
        // Sem fonte própria: sem SQL inline, sem conexão, sem o repositório de produtividade direto.
        Assert.True(!ctl.Contains("select", StringComparison.OrdinalIgnoreCase),
            "MeuDiaController não deveria ter SQL própria (a fonte é o ProductivityActionService).");
        Assert.DoesNotContain("NpgsqlConnection", ctl);
        Assert.DoesNotContain("IProductivityActionRepository", ctl);
    }

    [Fact]
    public void JanelaHoje_MantraUtcExplicitaEPreservada()
    {
        var ctl = File.ReadAllText(ApiCtl("MeuDiaController.cs"));
        // Padrão de janela relocado da fachada: "hoje" = UtcNow + 1 dia, pág. 25; agenda CLINICO.
        Assert.Contains("DateTimeOffset.UtcNow.AddDays(1)", ctl);
        Assert.Contains("PageSize: 25", ctl);
        Assert.Contains("Module: \"CLINICO\"", ctl);
        // Caveat registrado (r8b17 §5): a janela é UTC, não o fuso do tenant/usuário.
        Assert.Contains("UTC", ctl);
    }

    [Fact]
    public void MeuDiaeCentralDeAcoes_RosamNoUnicoServicoCanonical()
    {
        var meuDiaCtl = File.ReadAllText(ApiCtl("MeuDiaController.cs"));
        var prodCtl   = File.ReadAllText(ApiCtl("ProductivityActionController.cs"));
        // As duas superfícies dependem do MESMO serviço canônico — sem camada intermediária.
        Assert.Contains("IProductivityActionService", meuDiaCtl);
        Assert.Contains("IProductivityActionService", prodCtl);
    }
}
