using System.Globalization;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R58 (fila r53b1 §7 item 4): Ajuda central — perfis operacionais sem manual próprio
/// (DIRETOR/RECEPCAO/PARCEIRO) + tour guiado consumindo os artigos como roteiro.
/// Scanner de fonte sobre <c>AjudaController</c> e <c>Ajuda/Index.cshtml</c>.
/// D-R58-1: DIRETOR é membro da família administrador de tenant (IsTenantAdmin e mesmo
/// DefaultRoute /ClientePortal) e lê o manual ADMINISTRADOR_CLIENTE (extensão do D-R51-1).
/// D-R58-2/3: tópicos recepcao (3 artigos) e parceiro (2 artigos) com rotas confirmadas.
/// D-R58-4: cada âncora de artigo vira passo do tour v2145 via data-tour-step (sem mudança de JS).
/// Plano: docs/evidencias/2026-10-19-rodada-58/r58a0-ajuda-perfis-e-tour-plano.md.
/// </summary>
public sealed class R58AjudaPerfisContratoTests
{
    private static string ControllerPath => System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Controllers", "AjudaController.cs");
    private static string IndexPath => System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views", "Ajuda", "Index.cshtml");

    private static string Ler() => System.IO.File.ReadAllText(ControllerPath, System.Text.Encoding.UTF8);

    // Mesmo scanner da R51: bloco de Artigo(...) até o primeiro fechamento de parêntese.
    private static List<string> BlocosArtigos(string texto)
    {
        var blocos = new List<string>();
        var i = 0;
        while ((i = texto.IndexOf("Artigo(\"", i, StringComparison.Ordinal)) >= 0)
        {
            var fim = texto.IndexOf(')', i);
            blocos.Add(fim > i ? texto.Substring(i, fim - i) : string.Empty);
            i += 8;
        }
        return blocos;
    }

    [Fact]
    public void Diretor_CompartilhaManualDaFamiliaTenantAdmin()
    {
        var t = Ler();
        // D-R58-1: DIRETOR → ADMINISTRADOR_CLIENTE dentro da ajuda (mesma mecânica do D-R51-1).
        Assert.Contains("if (string.Equals(normalizado, \"DIRETOR\", StringComparison.OrdinalIgnoreCase)) normalizado = \"ADMINISTRADOR_CLIENTE\";", t);
    }

    [Fact]
    public void TopicoRecepcao_CobreRotinasDoGuiche()
    {
        var t = Ler();
        Assert.Contains("Slug = \"recepcao\"", t);
        Assert.Contains("Perfil = \"RECEPCAO\", Titulo = \"Atendimento na recepção\"", t);
        var blocos = BlocosArtigos(t).Where(b => b.Contains("\"RECEPCAO\"")).ToList();
        Assert.True(blocos.Count >= 3, "artigos RECEPCAO esperados >= 3, encontrados " + blocos.Count);
        // Rotas confirmadas em Saude360WebControllers (Pacientes.Create, Agendamentos.CheckIn, PainelChamada.Index).
        Assert.Contains("\"Pacientes\", \"Create\"", t);
        Assert.Contains("\"Agendamentos\", \"CheckIn\"", t);
        Assert.Contains("\"PainelChamada\", \"Index\"", t);
    }

    [Fact]
    public void TopicoParceiro_CobrePropostasEComissoes()
    {
        var t = Ler();
        Assert.Contains("Slug = \"parceiro\"", t);
        Assert.Contains("Perfil = \"PARCEIRO\", Titulo = \"Portal do parceiro\"", t);
        var blocos = BlocosArtigos(t).Where(b => b.Contains("\"PARCEIRO\"")).ToList();
        Assert.True(blocos.Count >= 2, "artigos PARCEIRO esperados >= 2, encontrados " + blocos.Count);
        // Rotas confirmadas em ParceiroPortalController (CommercialDemoWebController.cs).
        Assert.Contains("\"ParceiroPortal\", \"Propostas\"", t);
        Assert.Contains("\"ParceiroPortal\", \"Comissoes\"", t);
    }

    [Fact]
    public void TourConsomeArtigosDoPerfil()
    {
        var view = System.IO.File.ReadAllText(IndexPath, System.Text.Encoding.UTF8);
        // D-R58-4: o laço de artigos do "Recomendados para você" vira o roteiro do tour v2145.
        Assert.Contains("data-tour-step=\"@artigo.Titulo|@artigo.Resumo\"", view);
        // Os 3 passos fixos de orientação permanecem (ordem de DOM: UI primeiro, artigos depois).
        Assert.Contains("data-tour-step=\"Busca rápida|", view);
        Assert.Contains("data-tour-step=\"Atalhos principais|", view);
        Assert.Contains("data-tour-step=\"Manuais curtos|", view);
        Assert.Contains("data-tour-start=\"help-center\"", view);
    }
}
