using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R95 (B5 confiabilidade): o drill de backup/restore do banco de teste. O script
/// r95a_drill_backup_restore.ps1 prova DUAS rotas de recuperacao: PA = backup de dados
/// (pg_dump -Fc do banco vivo -> DROP -> recriar vazio -> pg_restore -> paridade completa:
/// conjunto de tabelas, contagem de TODAS as linhas, indices, foreign keys com definicao,
/// sequencias, extensoes e digest de conteudo de um conjunto estavel -> API funcional contra
/// o banco restaurado); PB = reconstrucao pelo script unico (artefato B1): instalador
/// scripts/database/install-plantaopro.ps1 em modo CLEAN + runner oficial de migracoes
/// (auto-cura) + API sobe e faz login do superadministrador de bootstrap. Achado documentado:
/// o restore revela que duas FKs (adm360_vale_itens_vale_id_fkey e
/// adm360_cotacao_envios_resposta_id_fkey) existem NOT VALID na fonte (linhas orfaas); o
/// repair recria fielmente (add NOT VALID + validate quando os dados permitem). Contrato
/// travado: (1) o script com as duas rotas e marcadores maquina-legiveis; (2) a execucao
/// canonica com RESULTADO=PASSEI, dump verificado, paridade completa e funcionalidade das
/// duas rotas; (3) contagem de todas as tabelas identica antes/depois do restore;
/// (4) checksums do manifest coerentes com os arquivos para todas as migracoes ativas
/// (guarda contra a classe de drift arquivo x manifest que o drill descobriu em v1186
/// e v2360, que nenhum contrato anterior cobria).
/// </summary>
public sealed class R95DrillBackupRestoreContratoTests
{
    private const string PastaR95 = "docs/evidencias/2026-10-22-rodada-95";
    private const string ArquivoScript = "r95a_drill_backup_restore.ps1";
    private const string ArquivoSaida = "r95a_drill_saida.txt";
    private const string ArquivoLinhasAntes = "r95a_linhas_antes.txt";
    private const string ArquivoLinhasDepois = "r95a_linhas_depois.txt";

    // Fato 1 - o script trava as duas rotas de recuperacao e os marcadores da saida:
    // PA usa pg_dump custom format como artefato, drop/recreate template0, restore sem dono,
    // repair de FK fiel a fonte; PB executa o instalador do script unico (B1) em modo CLEAN
    // e o runner oficial de migracoes.
    [Fact]
    public void Script_trava_as_duas_rotas_de_recuperacao_e_os_marcadores_da_saida()
    {
        var ps = File.ReadAllText(Caminho(ArquivoScript));
        Assert.StartsWith("# R95 (B5 confiabilidade)", ps);
        foreach (var fragmento in new[]
        {
            // PA - backup/restore de dados
            "-Fc -f $dumpArq",
            "--no-owner --no-privileges $dumpArq",
            "drop database \"\"$dbSrc\"\" with (force)",
            "template template0 encoding 'UTF8' owner postgres",
            "pg_get_constraintdef(con.oid)",
            "add constraint \"' + $cnm + '\" ' + $df + ' not valid;",
            "validate constraint \"' + $cnm",
            "string_agg(row_text, chr(1) order by row_text)",
            // PB - script unico (artefato B1) + auto-cura
            "scripts/database/install-plantaopro.ps1",
            "-Mode CLEAN -Environment Development",
            "& dotnet $toolDbDll upgrade",
            // marcadores maquina-legiveis da saida
            "'ESTAGEM_1_BASELINE_OK",
            "'ESTAGEM_2_DUMP_OK",
            "'ESTAGEM_3_DROP_RECREATE_OK",
            "'ESTAGEM_4_RESTORE_OK",
            "'ESTAGEM_5_PARIDADE",
            "'ESTAGEM_6_FUNCIONAL_API_OK",
            "'PARIDADE_COMPLETA_OK'",
            "'PA_CONCLUIDO'",
            "'ESTAGEM_7_SCRIPT_UNICO_APROVADO",
            "'ESTAGEM_8_MIGRACOES_UPGRADE_OK",
            "'ESTAGEM_9_AUTO_CURA_E_LOGIN_OK",
            "'CLASSIFICACAO_DELTA_ESTRUTURAL_OK",
            "'PB_CONCLUIDO'",
            "'RESULTADO=PASSEI'",
            "'RESULTADO=FALHOU'",
        })
        {
            Assert.Contains(fragmento, ps);
        }
    }

    // Fato 2 - execucao canonica: RESULTADO=PASSEI sem FALHA; artefato de dump verificado
    // (sha256 + tamanho plausivel); nenhuma FK faltando ao final; paridade completa em
    // tabelas/indices/sequencias/contagens; extensoes e digest iguais; invariantes de
    // negocio; API funcional nas duas rotas (gestor no banco restaurado e superadministrador
    // no banco do script unico) com migracoes convergidas.
    [Fact]
    public void Execucao_canonica_passei_paridade_completa_e_funcionalidade_das_duas_rotas()
    {
        var linhas = File.ReadAllLines(Caminho(ArquivoSaida));
        Assert.StartsWith("R95A_DRILL data=", linhas[0]);
        Assert.DoesNotContain(linhas, l => l.StartsWith("FALHA ", StringComparison.Ordinal));

        var dump = Parsear(ObterLinha(linhas, "ESTAGEM_2_DUMP_OK "));
        Assert.Matches("^[0-9a-f]{64}$", dump["sha256"]);
        Assert.True(long.Parse(dump["tamanho_bytes"], CultureInfo.InvariantCulture) >= 10_000_000,
            $"artefato de dump pequeno demais: {dump["tamanho_bytes"]} bytes.");

        var restore = Parsear(ObterLinha(linhas, "ESTAGEM_4_RESTORE_OK "));
        Assert.Equal("0", restore["fks_faltando_fim"]);

        foreach (var rotulo in new[] { "DIF_TABELAS ", "DIF_INDICES ", "DIF_SEQUENCIAS ", "DIF_CONTAGENS " })
        {
            var dif = Parsear(ObterLinha(linhas, rotulo));
            Assert.True(
                string.Equals(dif["faltando"], "0", StringComparison.Ordinal) &&
                string.Equals(dif["extra"], "0", StringComparison.Ordinal),
                $"diferenca em {rotulo.Trim()}: faltando={dif["faltando"]} extra={dif["extra"]}.");
        }

        var paridade = Parsear(ObterLinha(linhas, "ESTAGEM_5_PARIDADE "));
        Assert.Equal("SIM", paridade["extensoes_iguais"]);
        Assert.Equal("f", paridade["fn_bypass"]);
        var digestes = paridade["digest_ok"].Split('/');
        Assert.True(int.Parse(digestes[0], CultureInfo.InvariantCulture) >= 8,
            $"conjunto de digest pequeno: {paridade["digest_ok"]}.");
        Assert.Equal(digestes[0], digestes[1]);

        Assert.Equal("PARIDADE_COMPLETA_OK", ObterLinha(linhas, "PARIDADE_COMPLETA_OK"));

        var funcional = Parsear(ObterLinha(linhas, "ESTAGEM_6_FUNCIONAL_API_OK "));
        Assert.Equal("SIM", funcional["login_gestor"]);
        Assert.Equal("200", funcional["orcamentos_http"]);
        Assert.Equal("PA_CONCLUIDO", ObterLinha(linhas, "PA_CONCLUIDO"));

        Assert.StartsWith(
            "ESTAGEM_7_SCRIPT_UNICO_APROVADO banco=plantaopro_r95_script modo=CLEAN",
            ObterLinha(linhas, "ESTAGEM_7_SCRIPT_UNICO_APROVADO "));
        var migracoes = Parsear(ObterLinha(linhas, "ESTAGEM_8_MIGRACOES_UPGRADE_OK "));
        Assert.Equal("0", migracoes["falhas"]);
        Assert.Equal("0", migracoes["pendentes"]);
        var loginPb = Parsear(ObterLinha(linhas, "ESTAGEM_9_AUTO_CURA_E_LOGIN_OK "));
        Assert.Equal("SIM", loginPb["login_superadmin"]);
        // Delta estrutural vs banco vivo: o drill classifica cada tabela fora do conjunto
        // comum (32 = DDL legado so do script unico; 2 = seeds de perfil demo) e falha se
        // aparecer qualquer nome fora da classificacao documentada.
        var difPb = Parsear(ObterLinha(linhas, "DIF_SCRIPT_UNICO_VS_ORIGINAL "));
        Assert.Equal("2", difPb["faltando"]);
        Assert.Equal("32", difPb["extra"]);
        var classif = Parsear(ObterLinha(linhas, "CLASSIFICACAO_DELTA_ESTRUTURAL_OK "));
        Assert.Equal("0", classif["inesperados"]);
        Assert.Equal("PB_CONCLUIDO", ObterLinha(linhas, "PB_CONCLUIDO"));

        Assert.Equal("SIM", Parsear(ObterLinha(linhas, "PORTAS_LIVRES="))["PORTAS_LIVRES"]);
        Assert.Equal("PASSEI", Parsear(ObterLinha(linhas, "RESULTADO="))["RESULTADO"]);
    }

    // Fato 3 - prova independente de paridade de dados: os arquivos de contagem
    // (uma linha por tabela do schema, tbl=n) sao identicos linha a linha entre o estado
    // anterior e o estado apos o restore, cobrem o catalogo completo e mantem o volume.
    [Fact]
    public void Contagem_de_todas_as_tabelas_identicas_antes_e_depois_do_restore()
    {
        var antes = File.ReadAllLines(Caminho(ArquivoLinhasAntes));
        var depois = File.ReadAllLines(Caminho(ArquivoLinhasDepois));
        Assert.True(antes.Length >= 700, $"catalogo incompleto: {antes.Length} tabelas.");
        Assert.Equal(antes.Length, depois.Length);
        Assert.Equal(antes, depois);

        var porTabela = new Dictionary<string, int>();
        long total = 0;
        foreach (var linha in antes)
        {
            var i = linha.LastIndexOf('=');
            Assert.True(i > 0, $"linha malformada: {linha}.");
            var nome = linha.Substring(0, i);
            var n = int.Parse(linha.Substring(i + 1), CultureInfo.InvariantCulture);
            total += n;
            porTabela[nome] = n;
        }
        Assert.True(total >= 300_000, $"volume total abaixo do esperado: {total}.");
        Assert.True(porTabela.TryGetValue("adm360_orcamentos", out var orcamentos) && orcamentos >= 11_000,
            $"adm360_orcamentos ausente ou pequeno: {orcamentos}.");
        Assert.True(porTabela.TryGetValue("usuarios", out var usuarios) && usuarios >= 5,
            $"usuarios ausente ou pequeno: {usuarios}.");
    }

    // Fato 4 - achado do drill: duas entradas do manifest (v1186 e v2360) estavam com
    // checksum dessincronizado dos arquivos e nenhuma suíte anterior cobria esse par:
    // cada contrato so valida a propria entrada da rodada. O runner oficial rejeita
    // qualquer dessincron arquivo x manifest independentemente do estado de aplicacao
    // (bloqueou a auto-cura de qualquer banco, inclusive o vivo). A classe de defeito
    // fica travada para todas as entradas ativas: o hash do arquivo (conteudo CRLF->LF,
    // com fallback para os bytes crus — mesma logica do runner) deve casar o manifest.
    [Fact]
    public void Manifesto_checksums_coerentes_com_os_arquivos_para_todas_as_migracoes_ativas()
    {
        var root = RepositoryPathResolver.ResolveRoot();
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "database", "migration-manifest.json")));
        var entradas = doc.RootElement.GetProperty("migrations").EnumerateArray().ToList();
        Assert.True(entradas.Count >= 144, $"manifest abaixo do piso canonico: {entradas.Count} entradas.");

        int ativas = 0;
        var defeitos = new List<string>();
        foreach (var m in entradas)
        {
            var status = m.TryGetProperty("status", out var s) ? s.GetString() : "active";
            if (!string.Equals(status, "active", StringComparison.Ordinal)) continue;
            ativas++;
            var versao = m.GetProperty("version").GetString() ?? "?";
            var source = m.GetProperty("source").GetString() ?? "?";
            var checksum = m.GetProperty("checksum").GetString() ?? "";
            var caminho = Path.Combine(root, source.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(caminho)) { defeitos.Add(versao + ": arquivo ausente (" + source + ")"); continue; }
            var bruto = File.ReadAllBytes(caminho);
            var lf = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(bruto).Replace("\r\n", "\n"));
            var ok = string.Equals(Sha256Hex(lf), checksum, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Sha256Hex(bruto), checksum, StringComparison.OrdinalIgnoreCase);
            if (!ok) defeitos.Add(versao + ": manifesto=" + checksum);
        }
        Assert.True(ativas >= 144, $"entradas ativas abaixo do piso canonico: {ativas}.");
        Assert.Empty(defeitos);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static string Caminho(string nomeArquivo) =>
        Path.Combine(RepositoryPathResolver.ResolveRoot(), PastaR95, nomeArquivo);

    private static string Sha256Hex(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static string ObterLinha(string[] linhas, string prefixo)
    {
        var linha = linhas.FirstOrDefault(l => l.StartsWith(prefixo, StringComparison.Ordinal));
        Assert.NotNull(linha);
        return linha!;
    }

    /// <summary>Parsa linha chave=valor separada por espacos (formato maquina-legivel do harness).</summary>
    private static Dictionary<string, string> Parsear(string linha)
    {
        var d = new Dictionary<string, string>();
        foreach (var token in linha.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = token.IndexOf('=');
            if (i > 0)
            {
                d[token.Substring(0, i)] = token.Substring(i + 1);
            }
        }
        return d;
    }
}
