using System.Text.RegularExpressions;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B13 — §29 item 5 (mapa C5): menu _AppSidebar com Usuarios.Index ×2 e Perfis.Index ×2
/// — duplicação visível para super-admin.
///
/// Achado (fonte canônica): <c>canManage</c> INCLUI <c>AdministradorGlobal</c> e <c>isGlobalAdmin</c> É
/// <c>AdministradorGlobal</c> → <c>isGlobalAdmin ⊆ canManage</c>. Logo a seção "MNSOFT" (@if isGlobalAdmin)
/// é SEMPRE acompanhada pela seção "Cliente" (@if canManage). Os dois pares duplicados são links
/// <c>asp-action="Index"</c> SEM parâmetro de rota → destino idêntico (/Usuarios, /Perfis):
///   • Seção "Cliente" (:76/:77): rótulos "Usuários"/"Perfis", guardados por Can("USUARIOS"/"PERFIS").
///   • Seção "MNSOFT" (:90/:91): rótulos "Usuários e acessos"/"Perfis globais", incondicionais.
/// Nenhum dos dois controllers Web tem ação DISTINCTA para escopo global (verificado: apenas Index +
/// CRUD) — o escopo global é resolvido DENTRO da ação pelo usuário autenticado, não por rota.
/// → para um super-admin, as 2 cópias por rota eram redundância visível (e ainda deixavam 2 itens
/// marcados "active" ao mesmo tempo).
///
/// Nesta fatia (rito §29 — comparar → canônico → migrar → regressões, sem deleção antecipada):
/// reconciliou por EXCLUSÃO MÚTUA de guardas, sem remover rota/view. As cópias da seção "Cliente"
/// só renderizam quando <c>&amp;&amp; !isGlobalAdmin</c>; as da seção "MNSOFT" permanecem
/// (incondicionais dentro de @if isGlobalAdmin). Resultado: NO MÁXIMO 1 link por rota para QUALQUER
/// usuário (super-admin usa os rótulos globais; admin não-global mantém os itens da seção "Cliente";
/// a rota segue alcançável em todos os casos).
///
/// Contrato = (1) monitor de inventário (pega qualquer NOVA rota duplicada sem reconciliação) +
/// (2) prova da exclusão mútua dos pares conhecidos (premissa isGlobalAdmin ⊆ canManage).
/// </summary>
public sealed class R8B13SidebarNavDedupContratoTests
{
    private static string Sidebar() =>
        File.ReadAllText(Path.Combine(RepositoryPathResolver.WebRoot, "Views", "Shared", "_AppSidebar.cshtml"));

    // Rotas de navegação <a … asp-controller="X" asp-action="Y">.
    private static readonly Regex LinkRx = new(
        @"asp-controller=""(\w+)""\s+asp-action=""(\w+)""", RegexOptions.CultureInvariant);

    /// <summary>
    /// Inventário fixado de rotas com &gt;1 link-FONTE na sidebar. A ÚNICA duplicação intencional e
    /// documentada é MinhaCentral.Index (logo da marca + item da visão geral). Os pares Usuarios/Perfis
    /// figuram aqui porque existem como 2 links-fonte (seção "Cliente" + seção "MNSOFT") RECONCILIADOS
    /// por exclusão mútua de guarda — ou seja, nunca 2 visíveis ao mesmo tempo (ver 2º fato).
    /// </summary>
    private static readonly Dictionary<(string, string), int> RotasRepetidasEsperadas = new()
    {
        { ("MinhaCentral", "Index"), 2 },
        { ("Usuarios", "Index"), 2 },
        { ("Perfis", "Index"), 2 },
    };

    [Fact]
    public void Sidebar_InventarioDeRotasRepetidas_EstoFixo()
    {
        var contagem = new Dictionary<(string, string), int>();
        foreach (Match m in LinkRx.Matches(Sidebar()))
        {
            var rot = (m.Groups[1].Value, m.Groups[2].Value);
            contagem[rot] = contagem.GetValueOrDefault(rot) + 1;
        }
        var repetidas = contagem.Where(kv => kv.Value > 1).ToDictionary(kv => kv.Key, kv => kv.Value);

        // Nenhuma rota se repete além do inventário fixado → pega duplicação nova sem reconciliação.
        Assert.Equal(RotasRepetidasEsperadas.Count, repetidas.Count);
        foreach (var ((controlador, acao), esperado) in RotasRepetidasEsperadas)
            Assert.True(repetidas.TryGetValue((controlador, acao), out var obtido) && obtido == esperado,
                $"Rota {controlador}/{acao}: esperado {esperado} links-fonte, obtido {repetidas.GetValueOrDefault((controlador, acao))}.");
    }

    [Fact]
    public void Sidebar_ParesUsuariosEPerfis_ReconciliadosPorExclusaoMutuaDeGuardas()
    {
        var src = Sidebar();

        // Premissa estrutural: as duas seções coexistem (isGlobalAdmin ⊆ canManage → ambas renderizam p/ super-admin).
        Assert.Contains("@if (canManage)", src);
        Assert.Contains("@if (isGlobalAdmin)", src);

        // As cópias da seção "CLIENTE" estão excluídas para o global admin — complementares às da "MNSOFT"
        // (que vivem dentro de @if (isGlobalAdmin)). isGlobalAdmin e !isGlobalAdmin se excluem → NO MÁXIMO 1 link por rota.
        Assert.Contains(@"Can(""USUARIOS"") && !isGlobalAdmin", src);
        Assert.Contains(@"Can(""PERFIS"") && !isGlobalAdmin", src);
    }
}
