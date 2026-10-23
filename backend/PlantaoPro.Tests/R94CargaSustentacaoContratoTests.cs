using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R94 (B5 carga/sustentacao): o primeiro harness de carga do repositorio. O script
/// r94a_carga_api.ps1 sobe somente a API (Release, banco de teste), faz login do gestor demo,
/// aquece (WarmupSeg) e depois sustenta -Workers clientes concorrentes em I/O assincrono real
/// (exatamente N requests em voo, single-thread por proposito - scriptblock em outro thread
/// falha em PS 5.1 por falta de Runspace) em um mix somente leitura dos caminhos quentes do
/// Administrativo 360, contra thresholds documentados: percentis (p50/p90/p99), taxa de erro,
/// throughput minimo, degradacao entre as duas metades da janela, conexoes do banco
/// (pg_stat_activity a cada 3 s) e memoria da API. A saida e cultura-invariante (decimais com
/// ponto) e vira evidencia. Contrato travado: (1) o script com os thresholds/perfil/mix/
/// amostrador e os marcadores de saida; (2) a execucao canonica r94a_carga_saida.txt com
/// RESULTADO=PASSEI e cada valor medido dentro do proprio threshold declarado; (3) a
/// sustentacao monitorada sem degradacao no tempo e sem vazamento de conexoes.
/// </summary>
public sealed class R94CargaSustentacaoContratoTests
{
    private const string PastaR94 = "docs/evidencias/2026-10-22-rodada-94";
    private const string ArquivoScript = "r94a_carga_api.ps1";
    private const string ArquivoSaida = "r94a_carga_saida.txt";

    // Fato 1 - o script trava: thresholds documentados (edicao 1), perfil padrao (8x120s+15s),
    // mix das rotas quentes paginadas/instrumentadas (R91/R92), amostrador de sustentacao e os
    // marcadores maquina-legiveis da saida.
    [Fact]
    public void Script_tem_thresholds_documentados_perfil_padrao_mix_de_rotas_e_sustentacao()
    {
        var ps = File.ReadAllText(Caminho(ArquivoScript));
        Assert.StartsWith("# R94 - carga/sustentacao da API", ps);
        foreach (var fragmento in new[]
        {
            "$ThrP50MaxMs=250",
            "$ThrP90MaxMs=700",
            "$ThrP99MaxMs=1500",
            "$ThrErroMaxPct=1.0",
            "$ThrRpsMin=8.0",
            "$ThrDegradacaoMaxX=1.5",
            "$ThrConexaoDeltaMax=4",
            "[int]$Workers=8",
            "[int]$DuracaoSeg=120",
            "[int]$WarmupSeg=15",
            "Rota='/api/administrativo360/orcamentos'",
            "Rota='/api/administrativo360/orcamentos?pagina=2'",
            "Rota='/api/administrativo360/estoque'",
            "Rota='/api/administrativo360/fiscal/notas'",
            "Rota='/api/health'",
            "select count(*) from pg_stat_activity",
            "WorkingSet64",
            "GetAwaiter().GetResult()",
            "CurrentCulture=$invCult",
            "'WARMUP_SEG={0} descartados={1}'",
            "'POR_ROTA nome=" ,
            "'GERAL n={0} rps={1} erro_pct={2} media_ms={3} p50={4} p90={5} p99={6}'",
            "'METADES h1_n={0}",
            "'AMOSTRAS_CONEXAO n={0}",
            "'MEMORIA_API_MB min={0}",
            "'THRESHOLD p50_max_ms=",
            "'CHECK p50_ok=",
            "'RESULTADO='+$(if($todosOk){'PASSEI'}else{'FALHOU'})",
            "'PORTAS_LIVRES='+"
        })
        {
            Assert.Contains(fragmento, ps);
        }
    }

    // Fato 2 - execucao canonica: RESULTADO=PASSEI, cada valor medido dentro do proprio
    // threshold declarado pelo arquivo (mesma fonte), sem erro em nenhuma rota, volume sustentado
    // e porta livre ao final.
    [Fact]
    public void Execucao_canonica_passei_com_valores_medidos_dentro_dos_proprios_limiares()
    {
        var linhas = File.ReadAllLines(Caminho(ArquivoSaida));
        Assert.StartsWith("R94A_CARGA data=", linhas[0]);
        Assert.StartsWith("VOLUME adm360_orcamentos=", ObterLinha(linhas, "VOLUME "));

        var geral = Parsear(ObterLinha(linhas, "GERAL "));
        var thr = Parsear(ObterLinha(linhas, "THRESHOLD "));
        var check = Parsear(ObterLinha(linhas, "CHECK "));
        var resultado = Parsear(ObterLinha(linhas, "RESULTADO="));

        foreach (var chave in new[] { "p50_ok", "p90_ok", "p99_ok", "erro_ok", "rps_ok", "degradacao_ok", "conexao_ok" })
        {
            Assert.Equal("SIM", check[chave]);
        }
        Assert.Equal("PASSEI", resultado["RESULTADO"]);

        Assert.True(Num(geral["p50"]) <= Num(thr["p50_max_ms"]),
            $"p50 medido {geral["p50"]} acima do limite {thr["p50_max_ms"]}.");
        Assert.True(Num(geral["p90"]) <= Num(thr["p90_max_ms"]),
            $"p90 medido {geral["p90"]} acima do limite {thr["p90_max_ms"]}.");
        Assert.True(Num(geral["p99"]) <= Num(thr["p99_max_ms"]),
            $"p99 medido {geral["p99"]} acima do limite {thr["p99_max_ms"]}.");
        Assert.True(Num(geral["erro_pct"]) <= Num(thr["erro_max_pct"]),
            $"erro_pct medido {geral["erro_pct"]} acima do limite {thr["erro_max_pct"]}.");
        Assert.True(Num(geral["rps"]) >= Num(thr["rps_min"]),
            $"rps medido {geral["rps"]} abaixo do minimo {thr["rps_min"]}.");

        var n = long.Parse(geral["n"], CultureInfo.InvariantCulture);
        Assert.True(n >= 2000, $"volume sustentado insuficiente na janela: n={n}.");

        var rotas = linhas.Where(l => l.StartsWith("POR_ROTA ", StringComparison.Ordinal)).ToArray();
        Assert.True(rotas.Length >= 4, $"esperado >= 4 rotas medidas, obtido {rotas.Length}.");
        foreach (var rota in rotas)
        {
            var d = Parsear(rota);
            Assert.Equal("0", d["err"]);
            Assert.True(long.Parse(d["n"], CultureInfo.InvariantCulture) > 0, $"rota sem pedidos: {rota}.");
        }

        Assert.Equal("SIM", Parsear(ObterLinha(linhas, "PORTAS_LIVRES="))["PORTAS_LIVRES"]);
    }

    // Fato 3 - sustentacao monitorada: degradacao entre metades dentro do limite, amostras de
    // conexoes do banco suficientes com delta dentro do limite (sem vazamento), memoria da API
    // amostada e aquecimento com descarte real de requests.
    [Fact]
    public void Sustentacao_monitorada_sem_degradacao_e_sem_vazamento_de_conexoes()
    {
        var linhas = File.ReadAllLines(Caminho(ArquivoSaida));
        var thr = Parsear(ObterLinha(linhas, "THRESHOLD "));

        var metades = Parsear(ObterLinha(linhas, "METADES "));
        Assert.True(long.Parse(metades["h1_n"], CultureInfo.InvariantCulture) >= 1000, $"h1 pequena: {metades["h1_n"]}.");
        Assert.True(long.Parse(metades["h2_n"], CultureInfo.InvariantCulture) >= 1000, $"h2 pequena: {metades["h2_n"]}.");
        Assert.True(Num(metades["degradacao_x"]) <= Num(thr["degradacao_max_x"]),
            $"degradacao x={metades["degradacao_x"]} acima do limite {thr["degradacao_max_x"]}.");

        var conns = Parsear(ObterLinha(linhas, "AMOSTRAS_CONEXAO "));
        var nAmostras = long.Parse(conns["n"], CultureInfo.InvariantCulture);
        Assert.True(nAmostras >= 5, $"amostras de conexoes insuficientes: n={nAmostras}.");
        Assert.True(Num(conns["delta"]) <= Num(thr["conexao_delta_max"]),
            $"delta de conexoes {conns["delta"]} acima do limite {thr["conexao_delta_max"]} (vazamento).");

        var mem = Parsear(ObterLinha(linhas, "MEMORIA_API_MB "));
        Assert.True(Num(mem["min"]) <= Num(mem["max"]), $"memoria inconsistente: {mem["min"]}/{mem["max"]}.");
        var amostras = linhas.Where(l => l.StartsWith("AMOSTRA_t=", StringComparison.Ordinal)).ToArray();
        Assert.True(amostras.Length >= 2, $"esperado >= 2 amostras inline, obtido {amostras.Length}.");

        Assert.Equal("LOGIN_OK", ObterLinha(linhas, "LOGIN_OK"));
        Assert.True(int.Parse(Parsear(ObterLinha(linhas, "APPS "))["api_pid"], CultureInfo.InvariantCulture) > 0);
        var warmup = Parsear(ObterLinha(linhas, "WARMUP_SEG="));
        Assert.True(long.Parse(warmup["descartados"], CultureInfo.InvariantCulture) > 0,
            "aquecimento deve descartar requests reais antes da janela medida.");
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static string Caminho(string nomeArquivo) =>
        Path.Combine(RepositoryPathResolver.ResolveRoot(), PastaR94, nomeArquivo);

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

    /// <summary>A saida e cultura-invariante (decimais com ponto); aceita virgula como normalizacao defensiva.</summary>
    private static double Num(string valor) => double.Parse(valor.Replace(",", "."), CultureInfo.InvariantCulture);
}
