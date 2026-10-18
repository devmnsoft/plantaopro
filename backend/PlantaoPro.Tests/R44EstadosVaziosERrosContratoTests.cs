using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R44 (diretriz r24a1, epic E0 design — estados vazios, carregamento, erros): quatro telas de
/// detalhe (Notificação, Unidade/Hospital, Profissional/Médico, Especialidade) exibiam o payload
/// da API em JSON cru dentro de &lt;pre&gt; quando o registro carregava com sucesso — e apenas um
/// aviso genérico quando não carregava. Agora cada tela tem: campos com rótulos em português e
/// linguagem simples, "Não informado" quando o dado ausente é legítimo, estado claro quando o
/// registro não está disponível e botão de volta para a lista. Recon dos estados existentes
/// (Pendencias/Saude360 Modulo já usavam _EmptyState e página de erro padrão) no r44a0. Plano:
/// docs/evidencias/2026-10-18-rodada-44/r44a0-e0-estados-vazios-erros-plano.md.
/// </summary>
public class R44EstadosVaziosERrosContratoTests
{
    private static string ReadRelative(params string[] partes) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(partes).ToArray()), Encoding.UTF8);

    [Fact]
    public void NaoExisteMaisDumpDeJsonCruEmNenhumaView()
    {
        var raizViews = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views");
        var ofensores = new List<string>();
        foreach (var arquivo in Directory.GetFiles(raizViews, "*.cshtml", SearchOption.AllDirectories))
        {
            if (File.ReadAllText(arquivo, Encoding.UTF8).Contains("JsonSerializer.Serialize(Model?.Data)", StringComparison.Ordinal))
                ofensores.Add(arquivo);
        }
        Assert.Empty(ofensores);
    }

    [Fact]
    public void TelasDeDetalhe_TemCamposRotuladosEstadosEVoltaParaALista()
    {
        var casos = new (string View, string Rótulo)[]
        {
            ("Notificacoes\\Details.cshtml", "Título"),
            ("Hospitais\\Details.cshtml", "Responsável"),
            ("Medicos\\Details.cshtml", "Registro"),
            ("Especialidades\\Details.cshtml", "Descrição"),
        };
        foreach (var (view, rotulo) in casos)
        {
            var v = ReadRelative("backend", "PlantaoPro.Web", "Views", view);
            Assert.DoesNotContain("<pre>", v, StringComparison.Ordinal);
            Assert.Contains(rotulo, v, StringComparison.Ordinal);
            Assert.Contains("alert alert-info", v, StringComparison.Ordinal);   // estado: indisponível
            Assert.Contains("alert alert-warning", v, StringComparison.Ordinal); // estado: erro
            Assert.Contains("asp-action=\"Index\"", v, StringComparison.Ordinal); // saída clara
        }
    }

    [Fact]
    public void DadosAusentesLegitimos_NaoAparemComoEspacosEmBranco()
    {
        foreach (var view in new[] { "Hospitais\\Details.cshtml", "Medicos\\Details.cshtml" })
        {
            var v = ReadRelative("backend", "PlantaoPro.Web", "Views", view);
            Assert.Contains("Não informado", v, StringComparison.Ordinal);
        }
        var esp = ReadRelative("backend", "PlantaoPro.Web", "Views", "Especialidades\\Details.cshtml");
        Assert.Contains("Não descrita.", esp, StringComparison.Ordinal);
    }
}
