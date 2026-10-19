using System.Text.RegularExpressions;

namespace PlantaoPro.Tests;

/// <summary>
/// Contrato de idempotencia do banco: os artefatos canonicos de importacao devem poder ser
/// aplicados duas vezes no mesmo banco sem erro (reinstalacao/UPGRADE). As regras estaticas
/// abaixo travam o padrao; a prova empirica de dupla aplicacao vive em
/// docs/evidencias/&lt;data&gt;-rodada-68/r68a (instalador completo aplicado 2x com zero erros).
/// </summary>
public class DatabaseIdempotenciaContratoTests
{
    private static readonly (string Arquivo, string[] Partes)[] Scripts =
    {
        ("scrpt_completo.sql", new[] { "database", "scrpt_completo.sql" }),
        ("instalar_no_banco_atual.sql", new[] { "database", "pgadmin", "instalar_no_banco_atual.sql" }),
    };

    private static readonly string[] SeedsDemo =
    {
        "121_acesso_demo_local.sql",
        "130_operacao_demo_santacasa.sql",
        "140_administrativo360_demo.sql",
        "141_administrativo360_suprimentos_demo.sql",
    };

    [Fact]
    public void ArtefatosCanonicos_DDL_E_Idempotente_Por_Construcao()
    {
        var alvos = new List<(string Nome, string Texto)>();
        foreach (var (nome, partes) in Scripts)
            alvos.Add((nome, RepositoryPathResolver.ReadRepositoryFile(partes)));
        foreach (var seed in SeedsDemo)
            alvos.Add((seed, RepositoryPathResolver.ReadRepositoryFile("database", "seeds", "development", seed)));

        var falhas = new List<string>();
        foreach (var (nome, texto) in alvos)
            VerificarArquivo(nome, texto, falhas);

        Assert.True(falhas.Count == 0,
            "Padrao de idempotencia violado:\n" + string.Join("\n", falhas.Take(25)));
    }

    private static void VerificarArquivo(string nome, string texto, List<string> falhas)
    {
        var linhas = texto.Replace("\r\n", "\n").Split('\n');
        var emDo = new bool[linhas.Length];
        var pilha = new Stack<string>();
        var abrirDo = new Regex("^do\\s+\\$([A-Za-z_][A-Za-z0-9_]*)?\\$", RegexOptions.IgnoreCase);
        for (var i = 0; i < linhas.Length; i++)
        {
            var s = linhas[i].Trim();
            if (!s.StartsWith("--"))
            {
                var abri = abrirDo.Match(s);
                if (abri.Success)
                {
                    var tag = abri.Groups[1].Success ? abri.Groups[1].Value : "";
                    pilha.Push(tag);
                }
                else
                {
                    while (pilha.Count > 0)
                    {
                        var atual = pilha.Peek();
                        if (Regex.IsMatch(s, "^end\\s+\\$" + Regex.Escape(atual) + "\\$\\s*;?\\s*$", RegexOptions.IgnoreCase))
                            pilha.Pop();
                        else
                            break;
                    }
                }
            }
            emDo[i] = pilha.Count > 0;
        }

        for (var i = 0; i < linhas.Length; i++)
        {
            var s = linhas[i].Trim();
            if (s.StartsWith("--")) continue;

            // R1: CREATE TABLE/INDEX top-level exige IF NOT EXISTS na declaracao.
            if (!emDo[i] && Regex.IsMatch(s, "^create\\s+(unique\\s+)?(table|index)(\\s|$)", RegexOptions.IgnoreCase))
            {
                var ctx = "";
                var vistas = 0;
                for (var j = i; j < linhas.Length && vistas < 4; j++)
                {
                    if (linhas[j].Trim().Length == 0) continue;
                    ctx += " " + linhas[j].Trim();
                    vistas++;
                }
                if (!ctx.Contains("if not exists", StringComparison.OrdinalIgnoreCase))
                    falhas.Add($"{nome} L{i + 1}: CREATE TABLE/INDEX sem IF NOT EXISTS -> {Curto(s)}");
            }

            // R2: ADD CONSTRAINT top-level exige DROP CONSTRAINT IF EXISTS imediatamente antes.
            if (!emDo[i] && Regex.IsMatch(s, "^alter\\s+table\\s.*add\\s+constraint", RegexOptions.IgnoreCase))
            {
                var ok = false;
                var vistas = 0;
                for (var j = i - 1; j >= 0 && vistas < 5; j--)
                {
                    var prev = linhas[j].Trim();
                    if (prev.Length == 0 || prev.StartsWith("--")) continue;
                    vistas++;
                    if (prev.Contains("drop constraint if exists", StringComparison.OrdinalIgnoreCase)) { ok = true; break; }
                }
                if (!ok) falhas.Add($"{nome} L{i + 1}: ADD CONSTRAINT sem DROP CONSTRAINT IF EXISTS previo -> {Curto(s)}");
            }

            // R3: INSERT top-level termina apenas com ON CONFLICT ou guarda NOT EXISTS na instrucao.
            if (!emDo[i] && Regex.IsMatch(s, "^insert\\s+into", RegexOptions.IgnoreCase))
            {
                var stmt = "";
                for (var j = i; j < linhas.Length; j++)
                {
                    var ln = linhas[j].Trim();
                    stmt += " " + ln;
                    if (!ln.StartsWith("--") && ln.EndsWith(";")) break;
                    if (stmt.Length > 200_000) break;
                }
                if (!stmt.Contains("on conflict", StringComparison.OrdinalIgnoreCase) &&
                    !stmt.Contains("not exists", StringComparison.OrdinalIgnoreCase))
                    falhas.Add($"{nome} L{i + 1}: INSERT sem ON CONFLICT nem NOT EXISTS -> {Curto(s)}");
            }

            // R4: CREATE TRIGGER exige DROP TRIGGER IF EXISTS do mesmo nome logo antes.
            var mTrg = Regex.Match(s, "^create\\s+trigger\\s+(\\S+)", RegexOptions.IgnoreCase);
            if (mTrg.Success)
            {
                var alvo = mTrg.Groups[1].Value;
                var ok = false;
                for (var j = i - 1; j >= Math.Max(0, i - 6); j--)
                {
                    if (Regex.IsMatch(linhas[j].Trim(), "^drop\\s+trigger\\s+if\\s+exists\\s+" + Regex.Escape(alvo), RegexOptions.IgnoreCase))
                    { ok = true; break; }
                }
                if (!ok) falhas.Add($"{nome} L{i + 1}: CREATE TRIGGER {alvo} sem DROP TRIGGER IF EXISTS previo");
            }

            // R5: CREATE FUNCTION top-level so como CREATE OR REPLACE FUNCTION.
            if (!emDo[i] &&
                Regex.IsMatch(s, "^create\\s+function", RegexOptions.IgnoreCase) &&
                !Regex.IsMatch(s, "^create\\s+or\\s+replace\\s+function", RegexOptions.IgnoreCase))
            {
                falhas.Add($"{nome} L{i + 1}: CREATE FUNCTION sem OR REPLACE -> {Curto(s)}");
            }
        }
    }

    [Fact]
    public void EspelhoPgadmin_ManteMesmosMarcadoresDeFonte_Do_ScriptCompleto()
    {
        var marcadores = new Func<string, List<(string Caminho, string Hash)>>(p =>
        {
            var lista = new List<(string, string)>();
            var rx = new Regex(@"^-- SOURCE: (.+?)\r?\n-- SOURCE-SHA256: ([0-9a-f]{64})", RegexOptions.Multiline);
            foreach (Match m in rx.Matches(p))
                lista.Add((m.Groups[1].Value.Trim(), m.Groups[2].Value));
            return lista;
        });
        var scrpt = marcadores(RepositoryPathResolver.ReadRepositoryFile("database", "scrpt_completo.sql"));
        var pgadmin = marcadores(RepositoryPathResolver.ReadRepositoryFile("database", "pgadmin", "instalar_no_banco_atual.sql"));

        Assert.Equal(scrpt.Count, pgadmin.Count);
        for (var i = 0; i < scrpt.Count; i++)
            Assert.True(scrpt[i].Caminho == pgadmin[i].Caminho && scrpt[i].Hash == pgadmin[i].Hash,
                $"Marcadores divergem no indice {i}: {scrpt[i].Caminho}/{scrpt[i].Hash[..8]} vs {pgadmin[i].Caminho}/{pgadmin[i].Hash[..8]}");
    }

    [Fact]
    public void Instaladores_ExpoemPerfil_BaseE_Demo_EnvolvendooSeedDeDemonstracao()
    {
        var ps1 = RepositoryPathResolver.ReadRepositoryFile("scripts", "database", "install-plantaopro.ps1");
        Assert.Contains("ValidateSet('base','demo')", ps1);
        Assert.Contains("-Environment Development", ps1);

        var sh = RepositoryPathResolver.ReadRepositoryFile("scripts", "database", "install-plantaopro.sh");
        Assert.Contains("PLANTAOPRO_PROFILE", sh);

        foreach (var seed in SeedsDemo)
        {
            Assert.Contains(seed, ps1);
            Assert.Contains(seed, sh);
            var texto = RepositoryPathResolver.ReadRepositoryFile("database", "seeds", "development", seed);
            Assert.True(texto.Length > 0, $"seed vazio: {seed}");
        }
    }

    private static string Curto(string s) => s.Length <= 90 ? s : s[..90];
}
