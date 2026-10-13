using System.Reflection;
using System.Text.Json;
using PlantaoPro.Api;
using PlantaoPro.Api.Administrativo360;
using PlantaoPro.Api.Productivity;
using PlantaoPro.Web.Models;
using Xunit;
using ApiChartItem = PlantaoPro.Api.Models.DashboardChartItem;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B11 — §29 item 3 (mapa C3): espelho de DTOs Api↔Web (BFF) em 4 pares.
///
/// O Web recebe a resposta da API via <c>ReadFromJsonAsync&lt;TViewModel&gt;</c> contra um tipo
/// DUPLICADO no projeto Web. O acoplamento é feito APENAS pelo nome das propriedades JSON (sem
/// vínculo de compilação entre a DTO Api e o VM Web): renomear/adicionar um campo na DTO Api faz o
/// valor cair em SILÊNCIO no VM (sem erro de compilação, sem falha). Este contrato fixa essa
/// correspondência por nome — recursiva sobre coleções e objetos embutidos — para os 4 pares do
/// mapa: ManagerCommandCenter, Administrativo360Resumo, ProductivityPage e DashboardPremium.
///
/// Direção Api→Web: tudo que a API pode enviar deve ser RECEBÍVEL pelo VM por nome. Campos extras
/// do VM (estado de view: Error/ErrorKind/CanViewTeam/…) são legítimos e não falham aqui.
///
/// Rito §29 (mapa, sem deleção nesta rodada): não faz merge de tipos; a consolidação física (um
/// único contrato compartilhado Api↔Web) fica pendente — aqui o risco de deriva vira contrato executado.
/// Regressão corrigida nesta fatia: o espelho de atalhos do DashboardPremium estava quebrado — a DTO
/// expõe <c>DashboardAtalhoDto(Titulo, Url)</c> mas o VM esperava <c>(Texto, Url, …)</c>, então o
/// rótulo do botão saía VAZIO em Views/DashboardsPremium/Perfil.cshtml. Alinhou-se Texto→Titulo.
/// </summary>
public sealed class R8B11EspelhoDtosApiWebContratoTests
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    // Omissões INTENIONAIS do espelho Api→Web. Cada entrada é um campo exposto pela DTO Api que a
    // VM Web não recebe de propósito. O contrato monitora todos os demais campos: um novo campo na
    // DTO Api sem contraparte no VM (ou fora desta lista) FALHA o teste — a deriva silenciosa.
    private static readonly HashSet<string> OmissoesIntencionais = new(StringComparer.Ordinal)
    {
        // Proveniência interna (qual calculador gerou o KPI); não exibida no card.
        "DashboardPremiumDto.Kpis.Fonte",
        // Dívida R8-B9: renderização em tela das Definições de KPI pendente (VM Web não exibe Definicoes).
        "DashboardPremiumDto.Kpis.Definicao",
        // Derivável na view a partir de Total/PageSize (paginação); o VM omite de propósito.
        "ProductivityPageDto.TotalPages",
        // Campo técnico de frescor (quando a fonte atualizou o item); não exibido na view.
        "ProductivityPageDto.Items.SourceUpdatedAt",
        // Estado de snooze derivado na view (o affordance CanSnooze já cobre a intenção).
        "ProductivityPageDto.Items.IsSnoozed",
    };

    // Os 4 pares do mapa C3: (DTO serializada pela Api, VM desserializada pelo Web).
    private static readonly (Type Api, Type Web)[] Pares =
    {
        (typeof(ManagerCommandCenterDto),    typeof(ManagerCommandCenterViewModel)),
        (typeof(Administrativo360ResumoDto), typeof(Administrativo360ResumoViewModel)),
        (typeof(ProductivityPageDto),        typeof(ProductivityPageViewModel)),
        (typeof(DashboardPremiumDto),        typeof(DashboardPremiumApiViewModel)),
    };

    [Fact]
    public void Contrato_EspelhoApiWeb_NenhumCampoDaDtoFicaSemContraparteNoVm()
    {
        foreach (var (api, web) in Pares)
        {
            var ausentes = new List<string>();
            Verificar(api, web, api.Name, ausentes);
            var inesperados = ausentes.Where(a => !OmissoesIntencionais.Contains(a)).ToList();
            Assert.True(inesperados.Count == 0,
                $"{api.Name}→{web.Name}: campo(s) da DTO Api SEM contraparte de nome no VM (fora da lista intencional): {string.Join(", ", inesperados)}");
        }
    }

    [Fact]
    public void Contrato_ListaIntencional_SemEntradaFantasma()
    {
        // Simétrico ao teste acima: garante que cada omissão permitida AINDA está ausente hoje.
        // Quando um campo ganhar contraparte no VM (deriva corrigida), a entrada vira fantasma e este
        // teste exige limpá-la — a lista intencional nunca cresce de forma silenciosa.
        var detectados = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (api, web) in Pares)
        {
            var ausentes = new List<string>();
            Verificar(api, web, api.Name, ausentes);
            foreach (var a in ausentes) detectados.Add(a);
        }
        var fantasmas = OmissoesIntencionais.Where(p => !detectados.Contains(p)).ToList();
        Assert.True(fantasmas.Count == 0,
            $"Entrada(s) na lista intencional que NÃO estão mais ausentes (remover da lista): {string.Join(", ", fantasmas)}");
    }

    [Fact]
    public void DashboardPremium_AtalhoRoundTrip_JsonLevaTituloEUrl()
    {
        // Regressão do espelho quebrado (R8-B11): antes da correção, "titulo" não tinha contraparte
        // no VM (que esperava "texto"), então o rótulo do atalho chegava VAZIO. Este round-trip prova
        // que o título flui do contrato Api para o VM Web pelo nome.
        var dto = new DashboardPremiumDto(
            "medico",
            new[] { new DashboardKpiDto("Plantões", 12, "plantaopro.plantoes", "Contagem de plantões ativos.") },
            new[] { "Alerta de exemplo" },
            new[] { "Passo de exemplo" },
            new[] { new DashboardAtalhoDto("Operação Inteligente", "/OperacaoInteligente/Index") },
            new[] { new ApiChartItem("Seg", 7m) },
            "Plano validado.");

        var json = JsonSerializer.Serialize(dto, Json);
        var vm = JsonSerializer.Deserialize<DashboardPremiumApiViewModel>(json, Json);

        Assert.NotNull(vm);
        Assert.Equal("medico", vm!.Perfil);
        Assert.Single(vm.Atalhos);
        Assert.Equal("Operação Inteligente", vm.Atalhos.First().Titulo);   // <-- vazio antes da correção
        Assert.Equal("/OperacaoInteligente/Index", vm.Atalhos.First().Url);
        // Campos de mesmo nome continuam fluindo intactos.
        Assert.Equal("Plantões", vm.Kpis.First().Titulo);
        Assert.Equal(12m, vm.Kpis.First().Valor);
        Assert.Equal("Seg", vm.Series.First().Label);
        Assert.Equal(7m, vm.Series.First().Valor);
    }

    /// <summary>
    /// Caminha a DTO Api e exige que cada propriedade tenha uma de MESMO NOME (case-insensitive) na
    /// VM Web. Segue coleções (recurso ao elemento) e objetos embutidos (referência); não recursa
    /// folhas (primitivo/string/date/guid/decimal/enum).
    /// </summary>
    private static void Verificar(Type api, Type web, string raiz, List<string> ausentes)
    {
        var webProps = web.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                          .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var p in api.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!webProps.TryGetValue(p.Name, out var wp))
            {
                ausentes.Add($"{raiz}.{p.Name}");
                continue;
            }
            var alvoApi = TipoParaRecursar(p.PropertyType);
            var alvoWeb = TipoParaRecursar(wp.PropertyType);
            if (alvoApi != null && alvoWeb != null)
                Verificar(alvoApi, alvoWeb, $"{raiz}.{p.Name}", ausentes);
        }
    }

    /// <summary>Coleção → tipo do elemento; objeto embutido → o próprio tipo; folha → null.</summary>
    private static Type? TipoParaRecursar(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        if (t.IsArray)
            t = t.GetElementType() ?? t;
        else if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def == typeof(IEnumerable<>) || def == typeof(IReadOnlyCollection<>) ||
                def == typeof(ICollection<>) || def == typeof(IList<>) ||
                def == typeof(IReadOnlyList<>) || def == typeof(List<>) ||
                def == typeof(HashSet<>))
                t = t.GetGenericArguments()[0];
        }
        return Folha(t) ? null : t;
    }

    private static bool Folha(Type t)
        => t.IsPrimitive || t.IsEnum
           || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid)
           || t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeSpan)
           || t == typeof(DateOnly) || t == typeof(TimeOnly);
}
