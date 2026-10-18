using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Npgsql;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R56 — passada de copy "Dashboard" -> "Painel" (fila r53b1 §7 item 2, junto da revisão de menus).
/// D-R56-1: em pt-BR, "Painel" é o termo canônico para as telas de visão consolidada (substitui o
/// empréstimo "Dashboard"); en mantém "Dashboard" (termo idiomático), es/fr mantêm as traduções já
/// idiomáticas — continuidade de D-R50-1: nomes de código (rotas, controllers, chaves resx, ícones, classes CSS,
/// códigos de módulo/papel) permanecem "Dashboard". D-R56-2: zonas preservadas — ComercialDemoWeb
/// (página comercial pública), breadcrumb da área compat V112 (contrato R4C11), catálogo comercial
/// de plano ('DASHBOARD_GESTOR') e permissões exibidas como código (DASHBOARD.*). D-R56-3: v2355
/// renomeia o nome de exibição do módulo DASHBOARD e da capacidade CLINICA_DASHBOARD em
/// modulos_sistema (o que o tenant vê em Minha Assinatura/contratação); os códigos seguem estáveis.
/// D-R56-4: contrato travado aqui + prova e2e de texto visível.
/// </summary>
public sealed class R56PainelCopyContratoTests
{
    private const string MigrationFile = "2026_10_v2355_r56_nome_modulo_painel.sql";

    private const string UpsertEsperado = "update plantaopro.modulos_sistema set nome='Painel' where upper(codigo)='DASHBOARD' and nome <> 'Painel';";

    private const string UpsertCapacidadeEsperado = "update plantaopro.modulos_sistema set nome='Painel Clinico' where upper(codigo)='CLINICA_DASHBOARD' and nome <> 'Painel Clinico';";

    private static string ViewsDir => Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Views");

    private static string ResxPath(string locale = "") => Path.Combine(
        RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Localization",
        string.IsNullOrEmpty(locale) ? "UiTexts.resx" : $"UiTexts.{locale}.resx");

    private static string ValorDoResx(string caminho, string chave)
    {
        var texto = File.ReadAllText(caminho, Encoding.UTF8);
        var m = Regex.Match(texto, $@"<data name=""{chave}""[^>]*>\s*<value>([^<]*)</value>");
        Assert.True(m.Success, $"chave {chave} ausente em {Path.GetFileName(caminho)}");
        return m.Groups[1].Value;
    }

    [Fact]
    public void Navegacao_PtBrUsaPainELosDemaisLocalesMantemTraducaoIdiomatica()
    {
        // pt-BR adota o termo canônico; en/es/fr já traziam os equivalentes idiomáticos
        // (verificados no inventário R56) e não mudam nesta rodada.
        Assert.Equal("Painel", ValorDoResx(ResxPath(), "Nav.Dashboard"));
        Assert.Equal("Dashboard", ValorDoResx(ResxPath("en"), "Nav.Dashboard"));
        Assert.Equal("Panel de control", ValorDoResx(ResxPath("es"), "Nav.Dashboard"));
        Assert.Equal("Tableau de bord", ValorDoResx(ResxPath("fr"), "Nav.Dashboard"));
    }

    [Fact]
    public void ResxPtBr_NenhumValorVisivelContemDashboard()
    {
        var resx = File.ReadAllText(ResxPath(), Encoding.UTF8);
        var valores = Regex.Matches(resx, "<value>([^<]*)</value>").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
        Assert.True(valores.Count > 500, "resx pt-BR com menos valores do que o esperado: " + valores.Count);
        var violacoes = valores
            .Where(v => Regex.IsMatch(v, @"\bdashboard\b", RegexOptions.IgnoreCase))
            .Select(v => v.Length > 90 ? v.Substring(0, 90) : v)
            .ToList();
        Assert.True(violacoes.Count == 0,
            "Valores do resx pt-BR ainda exibem \"Dashboard\" (use \"Painel\"):\n" + string.Join("\n", violacoes.Take(10)));
    }

    [Fact]
    public void Adm360PtBr_TitulosDoPainelGerencial()
    {
        Assert.Equal("Painel Gerencial — Administrativo 360", ValorDoResx(ResxPath(), "Adm360.Dash.PageTitle"));
        Assert.Equal("Painel Gerencial &amp; Estatístico 360", ValorDoResx(ResxPath(), "Adm360.Dash.Heading"));
        Assert.Equal("Painel Gerencial", ValorDoResx(ResxPath(), "Adm360.Dash.CapDashboardTitle"));
    }

    [Fact]
    public void Views_SemFrasesAntigasDeDashboardForaDaZonaComercial()
    {
        // Frases (não palavras soltas): nomes de código — asp-action="Dashboard", app-icon name="Dashboard",
        // classes pp-*-dashboard, códigos SAUDE360_DASHBOARD/DASHBOARD_GERENCIAL — continuam válidos (D-R56-1).
        var frases = new[]
        {
            ">dashboard<", "dashboard executivo", "dashboard clínico", "dashboard inteligente",
            "dashboard saas", "= \"dashboard \"", "</i> dashboard", "</i>dashboard",
        };
        var violacoes = new List<string>();
        var badgesPainel = 0;
        foreach (var arquivo in Directory.EnumerateFiles(ViewsDir, "*.cshtml", SearchOption.AllDirectories))
        {
            var relativo = arquivo.Replace(ViewsDir, "").Replace('\\', '/');
            // Zona comercial pública whitelista desde R39 (landing tem <h2>Dashboards</h2>).
            if (relativo.StartsWith("CommercialDemoWeb", StringComparison.OrdinalIgnoreCase)) continue;
            var texto = File.ReadAllText(arquivo, Encoding.UTF8).ToLowerInvariant();
            foreach (var frase in frases)
            {
                if (texto.Contains(frase, StringComparison.Ordinal)) violacoes.Add($"{relativo}: \"{frase}\"");
            }
            badgesPainel += Regex.Matches(texto, "<span class=\"pp-badge\">painel</span>").Count;
        }
        Assert.True(violacoes.Count == 0, "Frases antigas \"Dashboard\" em texto visível:\n" + string.Join("\n", violacoes.Take(10)));

        // 4 telas com badge de seção: ClientePortal, ParceiroPortal, Demo e FeatureFlags.
        Assert.Equal(4, badgesPainel);
        // Sidebar continua ligada à mesma chave (código invariável); o valor exibido agora é "Painel".
        var sidebar = File.ReadAllText(Path.Combine(ViewsDir, "Shared", "_AppSidebar.cshtml"), Encoding.UTF8);
        Assert.Contains("UiText.L(\"Nav.Dashboard\")", sidebar);
    }

    [Fact]
    public void Migration_v2355_ExisteComOUpsertExato()
    {
        var path = RepositoryPathResolver.DatabaseFile("migrations", MigrationFile);
        Assert.True(File.Exists(path), $"Migration v2355 não encontrada em {path}.");
        var sql = Normalizar(File.ReadAllText(path, Encoding.UTF8));
        Assert.Contains(UpsertEsperado, sql);
        Assert.Contains(UpsertCapacidadeEsperado, sql);
    }

    [Fact]
    public void ScriptsCanonicos_MantemOUpsertSincronizado()
    {
        var completo = File.ReadAllText(RepositoryPathResolver.DatabaseFile("scrpt_completo.sql"), Encoding.UTF8);
        var instalador = File.ReadAllText(RepositoryPathResolver.DatabaseFile("pgadmin", "instalar_no_banco_atual.sql"), Encoding.UTF8);
        Assert.Contains(UpsertEsperado, Normalizar(completo));
        Assert.Contains(UpsertCapacidadeEsperado, Normalizar(completo));
        Assert.Contains(UpsertEsperado, Normalizar(instalador));
        Assert.Contains(UpsertCapacidadeEsperado, Normalizar(instalador));
    }

    [Fact]
    public async Task ModulosSistema_NomeDoModuloDashboardEhPainelNoBanco()
    {
        var cs = TestDatabase.ConnectionString;
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r56_nome_modulo_painel'));");
        try
        {
            var sql = File.ReadAllText(RepositoryPathResolver.DatabaseFile("migrations", MigrationFile), Encoding.UTF8);
            await ExecutarMigrationComRetryAsync(cn, sql);

            var total = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.modulos_sistema WHERE upper(codigo)='DASHBOARD';");
            Assert.True(total >= 1, "catálogo modulos_sistema sem a linha do módulo DASHBOARD");

            // Nome de exibição renomeado; o código segue estável (grants referenciam modulo_id/codigo).
            var semPainel = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.modulos_sistema WHERE upper(codigo)='DASHBOARD' AND nome <> 'Painel';");
            Assert.Equal(0, semPainel);

            // Capacidade do pacote SAUDE360 visível ao tenant no fluxo de contratação (v2339),
            // mesma regra de exibição: nome renomeado, código CLINICA_DASHBOARD estável.
            var totalCapacidade = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.modulos_sistema WHERE upper(codigo)='CLINICA_DASHBOARD';");
            Assert.True(totalCapacidade >= 1, "catálogo modulos_sistema sem a linha da capacidade CLINICA_DASHBOARD");
            var semPainelClinico = await cn.ExecuteScalarAsync<int>(
                "SELECT count(*) FROM plantaopro.modulos_sistema WHERE upper(codigo)='CLINICA_DASHBOARD' AND nome <> 'Painel Clinico';");
            Assert.Equal(0, semPainelClinico);
        }
        finally
        {
            await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r56_nome_modulo_painel'));");
        }
    }

    /// <summary>
    /// Executa a migration tolerando impasse (40P01) com sessões de teste paralelas (padrão R55/Bloco7).
    /// </summary>
    private static async Task ExecutarMigrationComRetryAsync(NpgsqlConnection cn, string sql)
    {
        for (var tentativa = 1; ; tentativa++)
        {
            try
            {
                await cn.ExecuteAsync(sql);
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5)
            {
                await Task.Delay(150 * tentativa);
            }
        }
    }

    private static string Normalizar(string sql) =>
        string.Join(" ", sql.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
}
