using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R70 — varredura responsiva em 360/480/768/1024 px (forms, modais e tabelas densas sem
/// estouro/truncamento). A bateria é executável: docs/evidencias/2026-10-20-rodada-70/r70_sweep.mjs
/// (Chrome headless via CDP, login real como gestor do tenant demo e como superadmin, 22 páginas
/// x 4 larguras = 88 medições por perfil). A baseline registrou uma unica falha: /Pendencias em
/// ~1024px cortava o campo "Periodo — ate" fora da tela — o template base .action-filter exigia
/// ~822px minimos na faixa 901–1299px de viewport. Correção na própria folha do componente
/// (operations.css, onde já viviam os breakpoints ≤900/≤575): 3 colunas fluidas só nessa faixa.
/// Detalhe de cascata documentado: a section Styles da página carrega operações.css DEPOIS do
/// v2151 no _Layout, então o ajuste vive na folha do componente, não na camada canônica.
/// Este contrato trava a evidência final (FALHAS=0 com as 88 medições nos dois perfis) e a regra
/// da faixa segura.
/// </summary>
public class R70VarreduraResponsivaContratoTests
{
    // 22 paginas da bateria x 4 larguras = 88 medicoes por perfil.
    private const int MedicoesEsperadasPorPerfil = 88;

    private static string Roda70() => Path.Combine(RepositoryPathResolver.RepoRoot, "docs", "evidencias", "2026-10-20-rodada-70");

    [Fact]
    public void BateriaExecutavelEVarreduraFinalSemFalhasNosDoisPerfis()
    {
        Assert.True(File.Exists(Path.Combine(Roda70(), "r70_sweep.mjs")), "Runner r70_sweep.mjs ausente na evidencia.");
        foreach (var tag in new[] { "final", "final-super" })
        {
            var caminho = Path.Combine(Roda70(), $"r70_sweep_{tag}.txt");
            Assert.True(File.Exists(caminho), $"Resumo da varredura ausente: r70_sweep_{tag}.txt");
            var texto = File.ReadAllText(caminho, Encoding.UTF8);
            Assert.Contains($"R70_SWEEP_DONE FALHAS=0 medicoes={MedicoesEsperadasPorPerfil}", texto, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TemplateDoFiltroDePendenciasTemFaixaSeguraEntre901E1299Px()
    {
        var caminho = Path.Combine(RepositoryPathResolver.WebRoot, "wwwroot", "css", "design-system", "operations.css");
        var texto = File.ReadAllText(caminho, Encoding.UTF8);
        Assert.Contains(
            "@media(min-width:901px) and (max-width:1299px){.action-filter{grid-template-columns:repeat(3,minmax(0,1fr))}",
            texto,
            StringComparison.Ordinal);
    }
}
