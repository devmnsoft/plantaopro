using System.Text;
using System.Text.RegularExpressions;
using PlantaoPro.Web.DesignSystem;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R46 (diretriz r24a1, passada crítica geral — limpeza de pendências acumuladas): o menu lateral
/// reutilizava o mesmo ícone em itens distintos e semanticamente diferentes (Coverage para
/// Cobertura/Administrativo360/Usuários, User para Convites/Perfis/Clientes/Usuários e Acessos,
/// Report para Dashboard/Relatórios/Planos/Observabilidade, Finance para Visão/Caixa/Faturamento,
/// Calendar/Consultation em pares da mesma seção) — pendência aberta desde o recon da R43.
/// Agora cada item tem ícone próprio tirado do AppIconRegistry, com pares residuais declarados:
/// 3 de família conceitual (Coverage: Cobertura×Usuários; Doctor: Jornada×Profissionais; Payment:
/// Pagamentos×Minha Assinatura) e 2 de audiência exclusiva (User e WorkItem — um item guarda por
/// !isGlobalAdmin e o outro só renderiza para admin global, nunca exibidos juntos). Contrato:
/// nenhum ícone pode exceder esses limites e todo nome usado precisa resolver no registro
/// (nome inválido falharia só em runtime, via TagHelper). Plano:
/// docs/evidencias/2026-10-18-rodada-46/r46a0-passada-critica-plano.md.
/// </summary>
public class R46PassadaCriticaMenusContratoTests
{
    // Pares residuais autorizados (declaração explícita, não buraco na regra):
    // FAMÍLIA DE SIGNIFICADO (mesmo conceito, rótulos distintos): Coverage = Cobertura ×
    // Usuários (pessoas); Doctor = Jornada Clínica × Profissionais (médico); Payment =
    // Pagamentos × Minha Assinatura (dinheiro).
    // AUDIÊNCIAS EXCLUSIVAS (o Razor nunca mostra os dois ao mesmo tempo — um guarda por
    // !isGlobalAdmin e o outro só renderiza para admin global): User = Perfis (tenant) ×
    // Usuários e Acessos (global); WorkItem = Implantação (tenant) × Assinaturas (global).
    private static readonly Dictionary<string, int> LimitePorIcone = new(StringComparer.Ordinal)
    {
        ["Coverage"] = 2,
        ["Doctor"] = 2,
        ["Payment"] = 2,
        ["User"] = 2,
        ["WorkItem"] = 2,
    };

    [Fact]
    public void MenuNaoReutilizaIconesForaDosParesConceituais()
    {
        var p = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views", "Shared", "_AppSidebar.cshtml");
        var texto = File.ReadAllText(p, Encoding.UTF8);

        // Um link de navegação: asp-controller="X" ... > <app-icon name="Y" ...>
        var matches = Regex.Matches(texto, "asp-controller=\"(\\w+)\"[^>]*><app-icon name=\"([A-Za-z]+)\"");
        Assert.True(matches.Count >= 25, $"esperado menu completo, encontrados {matches.Count} links");

        var contagem = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (Match m in matches)
        {
            var icon = m.Groups[2].Value;
            if (!contagem.TryGetValue(icon, out var lista)) contagem[icon] = lista = new List<string>();
            lista.Add(m.Groups[1].Value);
        }

        var violacoes = new List<string>();
        foreach (var (icon, controllers) in contagem)
        {
            var limite = LimitePorIcone.GetValueOrDefault(icon, 1);
            if (controllers.Count > limite)
                violacoes.Add($"{icon} x{controllers.Count} ({string.Join(", ", controllers)})");
        }
        Assert.True(violacoes.Count == 0, string.Join("; ", violacoes));
    }

    [Fact]
    public void TodoIconeDoMenuResolveNoRegistroDoDesignSystem()
    {
        var p = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views", "Shared", "_AppSidebar.cshtml");
        var texto = File.ReadAllText(p, Encoding.UTF8);
        var nomes = Regex.Matches(texto, "<app-icon name=\"([A-Za-z]+)\"").Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.NotEmpty(nomes);
        foreach (var nome in nomes)
            Assert.True(Enum.IsDefined(typeof(AppIconKey), nome), $"ícone '{nome}' não existe no AppIconKey (falharia em runtime)");
    }
}
