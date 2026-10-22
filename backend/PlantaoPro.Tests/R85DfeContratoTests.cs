using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Legislacao;
using PlantaoPro.Infrastructure.Legislacao;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R85 (bloco B4 - DF-e nacional): configuração SEFAZ por UF (27 cUF oficiais + ambientes SAT),
/// layouts oficiais vigentes na fonte única e chave de acesso de 44 dígitos com dígito verificador
/// módulo 11 (pesos 2..9 da direita para a esquerda, reiniciando em 2; resto 0 ou 1 resulta em DV 0).
/// Vetores aritméticos travados: resto 0 (DV 0), resto 1 (DV 0) e resto 6 (DV 5), incluindo CPF
/// zerado à esquerda no corpo. Tudo que varia por UF (cUF e modelos apoiados) lê da fonte única.
/// </summary>
public sealed class R85DfeContratoTests
{
    private const string MigrationV2363 = "database/migrations/2026_10_v2363_r80_parametros_legais.sql";
    private const string MigrationV2368 = "database/migrations/2026_10_v2368_r85_dfe_nacional.sql";
    private const string MigrationVersion = "2026_10_v2368_r85_dfe_nacional";

    // Códigos oficiais cUF (tabela de UFs do DF-e / Ajuste SINIEF 07/05), travados para as 27 UFs.
    private static readonly Dictionary<string, int> EsperadoCuf = new(StringComparer.Ordinal)
    {
        ["RO"] = 11, ["AC"] = 12, ["AM"] = 13, ["RR"] = 14, ["PA"] = 15, ["AP"] = 16,
        ["TO"] = 17, ["MA"] = 21, ["PI"] = 22, ["CE"] = 23, ["RN"] = 24, ["PB"] = 25, ["PE"] = 26,
        ["AL"] = 27, ["SE"] = 28, ["BA"] = 29, ["MG"] = 31, ["ES"] = 32, ["RJ"] = 33,
        ["SP"] = 35, ["PR"] = 41, ["SC"] = 42, ["RS"] = 43, ["MS"] = 50, ["MT"] = 51,
        ["GO"] = 52, ["DF"] = 53
    };

    // Ambientes SAT na data da rodada (Portaria SVS/ME 159/2011 e extensões): o CF-e SAT substitui a NFC-e nestas UFs.
    private static readonly HashSet<string> EsperadoUfsSat = new(StringComparer.Ordinal) { "AC", "AP", "CE", "PA", "PB", "PE", "RR" };

    // Versões vigentes dos layouts na fonte única (2026).
    private static readonly Dictionary<string, string> EsperadoLayouts = new(StringComparer.Ordinal)
    {
        ["NFE"] = "4.0", ["NFCE"] = "4.0", ["CFE_SAT"] = "1.00", ["CFE_SERVICOS"] = "3.00"
    };

    // Vetores aritméticos travados do módulo 11 oficial (corpo de 43 + DV).
    private const string VetorSpResto0 = "35261011222333000181550010000000011123456780"; // resto 0 => DV 0
    private const string VetorSpResto1 = "35261011222333000181550010000000021000000140"; // resto 1 => DV 0
    private const string VetorMgCpf = "31260100011144477735550070000009992000123455"; // CPF zerado a esquerda; resto 6 => DV 5

    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static string Sha256Lf(string conteudo) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(conteudo.Replace("\r\n", "\n")))).ToLowerInvariant();

    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r85_dfe_schema'));");
        try
        {
            var temTabela = await cn.ExecuteScalarAsync<bool>(
                "select exists(select 1 from information_schema.tables where table_schema='plantaopro' and table_name='parametros_legais')");
            if (!temTabela)
            {
                for (var tentativa = 1; ; tentativa++)
                {
                    try { await cn.ExecuteAsync(Read(MigrationV2363)); break; }
                    catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5) { await Task.Delay(150 * tentativa); }
                }
            }
            // v2368 e idempotente: reexecucao nao duplica (on conflict do nothing).
            await cn.ExecuteAsync(Read(MigrationV2368));
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r85_dfe_schema'));"); }
    }

    private static IDfeNacionalService Servico() =>
        new DfeNacionalService(new ParametrosLegaisRepository(TestDatabase.ConnectionString));

    [Fact]
    public void Manifesto_tem_entrada_v2368_com_hash_real_dependencia_e_status()
    {
        var sql = Read(MigrationV2368);
        var manifest = Read("database/migration-manifest.json");

        using var doc = JsonDocument.Parse(manifest);
        var entrada = doc.RootElement.GetProperty("migrations").EnumerateArray()
            .FirstOrDefault(m => m.GetProperty("version").GetString() == MigrationVersion);
        Assert.NotEqual(JsonValueKind.Undefined, entrada.ValueKind);

        Assert.Equal(MigrationV2368.Replace('\\', '/'), entrada.GetProperty("source").GetString());
        Assert.Equal(Sha256Lf(sql), entrada.GetProperty("checksum").GetString());
        Assert.Equal("active", entrada.GetProperty("status").GetString());
        Assert.True(entrada.GetProperty("transactional").GetBoolean());
        var deps = entrada.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()).ToArray();
        Assert.Equal(new[] { "2026_10_v2367_r84_calendario_fiscal_2026" }, deps);

        foreach (var uf in new[] { "SP", "RO", "AC", "DF", "RR", "MG", "CE" })
            Assert.Contains("SEFAZ_UF_" + uf, sql);
        foreach (var layout in new[] { "DFE_LAYOUT_NFE", "DFE_LAYOUT_NFCE", "DFE_LAYOUT_CFE_SAT", "DFE_LAYOUT_CFE_SERVICOS" })
            Assert.Contains(layout, sql);
        Assert.Contains("'4.0'", sql);
        Assert.Contains("'3.00'", sql);
        Assert.Contains("on conflict (codigo, ano)", sql);
    }

    [Fact]
    public async Task Catalogo_sefaz_por_uf_e_layouts_semeados_na_fonte_unica_com_fonte_citavel()
    {
        await GarantirSchemaAsync();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();

        var total = await cn.ExecuteScalarAsync<int>(@"select count(*) from plantaopro.parametros_legais where codigo like 'SEFAZ\_UF\_%' escape '\' and ano=2026 and reg_status='A'");
        Assert.Equal(27, total);

        async Task<(string Valor, string Fonte)> Linha(string codigo)
        {
            var r = await cn.QuerySingleAsync<dynamic>(@"select valor, fonte_oficial from plantaopro.parametros_legais where codigo=@c and ano=2026 and reg_status='A'", new { c = codigo });
            return ((string)r.valor, (string)r.fonte_oficial);
        }

        foreach (var par in EsperadoCuf)
        {
            var (valor, fonte) = await Linha("SEFAZ_UF_" + par.Key);
            var partes = valor.Split('|');
            Assert.Equal(2, partes.Length);
            Assert.Equal(par.Value.ToString(), partes[0]);
            var docs = partes[1].Split(',');
            Assert.Contains("NFE", docs);
            Assert.Contains("CFE_SERVICOS", docs);
            var ehSat = EsperadoUfsSat.Contains(par.Key);
            Assert.True(ehSat == docs.Contains("CFE_SAT"), $"documentos de {par.Key}: {valor}");
            Assert.True(ehSat == !docs.Contains("NFC-E"), $"documentos de {par.Key}: {valor}");
            Assert.False(string.IsNullOrWhiteSpace(fonte), $"fonte vazia para {par.Key}");
        }

        foreach (var par in EsperadoLayouts)
        {
            var (valor, fonte) = await Linha("DFE_LAYOUT_" + par.Key);
            Assert.Equal(par.Value, valor);
            Assert.False(string.IsNullOrWhiteSpace(fonte), $"fonte vazia para o layout {par.Key}");
        }
    }

    [Fact]
    public async Task Gerar_chave_SP_reproduz_o_vetor_travado_com_descomposicao_em_segmentos()
    {
        await GarantirSchemaAsync();
        var svc = Servico();
        var r = await svc.GerarChaveAsync(new GerarChaveCommand("sp", new DateOnly(2026, 10, 15), "11.222.333/0001-81", 55, 1, 1, 1, 12345678), default);

        Assert.True(r.Valida);
        Assert.Empty(r.Erros);
        Assert.NotNull(r.Chave);
        var c = r.Chave!;
        Assert.Equal(VetorSpResto0, c.Chave);
        Assert.Equal(44, c.Chave.Length);
        Assert.Equal(35, c.Cuf);
        Assert.Equal("SP", c.Uf);
        Assert.Equal(2026, c.Ano);
        Assert.Equal(10, c.Mes);
        Assert.Equal("11222333000181", c.Registro);
        Assert.Equal(55, c.Modulo);
        Assert.Equal(1, c.Serie);
        Assert.Equal(1, c.NumeroNota);
        Assert.Equal(1, c.TipoEmissao);
        Assert.Equal(12345678L, c.CodigoNumerico);
        Assert.Equal(0, c.DigitoVerificador);
        Assert.Contains("resto módulo 11 0", r.DetalheCalculo);
        Assert.Equal(9, c.Segmentos.Count);
        Assert.Equal("35 (SP)", c.Segmentos[0].Valor);
        Assert.Equal("000000001", c.Segmentos[5].Valor);
        Assert.Equal("0", c.Segmentos[8].Valor);
    }

    [Fact]
    public async Task Regras_especiais_do_dv_modulo_11_estao_travadas_com_vetores_aritmeticos()
    {
        await GarantirSchemaAsync();
        var svc = Servico();

        // Resto 1 => DV 0 (regra oficial), mesmo prefixo SP, nNF 2, codigo 14.
        var resto1 = await svc.GerarChaveAsync(new GerarChaveCommand("SP", new DateOnly(2026, 10, 20), "11222333000181", 55, 1, 2, 1, 14), default);
        Assert.True(resto1.Valida);
        Assert.Equal(VetorSpResto1, resto1.Chave!.Chave);
        Assert.Equal(0, resto1.Chave.DigitoVerificador);
        Assert.Contains("resto módulo 11 1", resto1.DetalheCalculo);

        // Resto 6 => DV 11-6=5; CPF (11 dígitos) entra zerado à esquerda no corpo de 43.
        var mg = await svc.GerarChaveAsync(new GerarChaveCommand("MG", new DateOnly(2026, 1, 31), "111.444.777-35", 55, 7, 999, 2, 12345), default);
        Assert.True(mg.Valida);
        Assert.Equal(VetorMgCpf, mg.Chave!.Chave);
        Assert.Equal(5, mg.Chave.DigitoVerificador);
        Assert.Equal("00011144477735", mg.Chave.Registro);
        Assert.Contains("11-6=5", mg.DetalheCalculo);

        // Os vetores travados batem com a implementação do serviço (confiança cruzada da aritmética).
        Assert.Equal(0, DfeNacionalService.Modulo11(VetorSpResto0[..43]).Dv);
        Assert.Equal(0, DfeNacionalService.Modulo11(VetorSpResto1[..43]).Dv);
        Assert.Equal(5, DfeNacionalService.Modulo11(VetorMgCpf[..43]).Dv);
        Assert.Equal(6, DfeNacionalService.Modulo11(VetorMgCpf[..43]).Resto);
    }

    [Fact]
    public async Task Validar_chave_confirma_dv_por_uf_e_rejeita_formato_dv_cuf_e_modelo_fora_do_escopo()
    {
        await GarantirSchemaAsync();
        var svc = Servico();

        var ok = await svc.ValidarChaveAsync(VetorSpResto0, default);
        Assert.True(ok.Valida);
        Assert.Empty(ok.Erros);
        Assert.Equal("SP", ok.Chave!.Uf);
        Assert.Equal(35, ok.Chave.Cuf);

        var okMg = await svc.ValidarChaveAsync(VetorMgCpf, default);
        Assert.True(okMg.Valida);
        Assert.Equal("MG", okMg.Chave!.Uf);

        // DV errado (informado 7, calculado 0): falha apontando calculado x informado.
        var dvErrado = await svc.ValidarChaveAsync(VetorSpResto0[..43] + "7", default);
        Assert.False(dvErrado.Valida);
        Assert.Single(dvErrado.Erros);
        Assert.Contains("calculado 0 pelo módulo 11, informado 7", dvErrado.Erros[0]);
        Assert.False(string.IsNullOrWhiteSpace(dvErrado.DetalheCalculo));

        // Formato: 43 dígitos e caractere não numérico caem na checagem de formato.
        var curta = await svc.ValidarChaveAsync(VetorSpResto0[..43], default);
        Assert.False(curta.Valida);
        Assert.Single(curta.Erros);
        Assert.Contains("44 dígitos", curta.Erros[0]);
        var naoNumerica = await svc.ValidarChaveAsync(VetorSpResto0[..43] + "X", default);
        Assert.False(naoNumerica.Valida);
        Assert.Single(naoNumerica.Erros);

        // cUF fora do catálogo oficial: DV correto não salva — a UF é o erro único.
        var corpoDesconhecido = "99" + VetorSpResto0[2..43];
        var chaveDesconhecida = corpoDesconhecido + DfeNacionalService.Modulo11(corpoDesconhecido).Dv;
        var cufErrado = await svc.ValidarChaveAsync(chaveDesconhecida, default);
        Assert.False(cufErrado.Valida);
        Assert.Single(cufErrado.Erros);
        Assert.Contains("cUF 99", cufErrado.Erros[0]);

        // Modelo 57 (CT-e): DV confere, mas está fora dos layouts geridos nesta configuração.
        var corpoMod57 = VetorSpResto0[..20] + "57" + VetorSpResto0.Substring(22, 21);
        var chaveMod57 = corpoMod57 + DfeNacionalService.Modulo11(corpoMod57).Dv;
        var modFora = await svc.ValidarChaveAsync(chaveMod57, default);
        Assert.False(modFora.Valida);
        Assert.Single(modFora.Erros);
        Assert.Contains("fora dos layouts geridos", modFora.Erros[0]);
    }

    [Fact]
    public async Task Gerar_chave_rejeita_faixas_fora_do_oficial_e_modelo_inapropriado_para_a_uf()
    {
        await GarantirSchemaAsync();
        var svc = Servico();
        var baseCmd = new GerarChaveCommand("SP", new DateOnly(2026, 10, 22), "11222333000181", 55, 1, 1, 1, 12345678);

        async Task<IReadOnlyList<string>> Erros(GerarChaveCommand cmd)
        {
            var r = await svc.GerarChaveAsync(cmd, default);
            Assert.False(r.Valida);
            return r.Erros;
        }

        Assert.Contains("série", (await Erros(baseCmd with { Serie = 0 })).Single());
        Assert.Contains("série", (await Erros(baseCmd with { Serie = 1000 })).Single());
        Assert.Contains("número da nota", (await Erros(baseCmd with { NumeroNota = 0 })).Single());
        Assert.Contains("número da nota", (await Erros(baseCmd with { NumeroNota = 1_000_000_000 })).Single());
        Assert.Contains("tipo de emissão", (await Erros(baseCmd with { TipoEmissao = 0 })).Single());
        Assert.Contains("tipo de emissão", (await Erros(baseCmd with { TipoEmissao = 10 })).Single());
        Assert.Contains("código numérico", (await Erros(baseCmd with { CodigoNumerico = -1 })).Single());
        Assert.Contains("código numérico", (await Erros(baseCmd with { CodigoNumerico = 100_000_000 })).Single());
        Assert.Contains("fora dos layouts geridos", (await Erros(baseCmd with { Modulo = 54 })).Single());
        Assert.Contains("11 dígitos (CPF) ou 14", (await Erros(baseCmd with { RegistroCnpjCpf = "123" })).Single());
        Assert.Contains("11 dígitos (CPF) ou 14", (await Erros(baseCmd with { RegistroCnpjCpf = "112223330001811" })).Single());

        await Assert.ThrowsAsync<ArgumentException>(() => svc.GerarChaveAsync(baseCmd with { Uf = "ZZ" }, default));

        // NFC-e não é o documento de consumidor em ambiente SAT (CE); CF-e SAT não existe fora deles (RJ).
        var nfcEmCe = await Erros(baseCmd with { Uf = "CE", Modulo = 65 });
        Assert.Single(nfcEmCe);
        Assert.Contains("NFC-e", nfcEmCe[0]);
        Assert.Contains("CE", nfcEmCe[0]);

        var satEmRj = await Erros(baseCmd with { Uf = "RJ", Modulo = 59 });
        Assert.Single(satEmRj);
        Assert.Contains("CF-e SAT", satEmRj[0]);
        Assert.Contains("RJ", satEmRj[0]);

        // Dentro da regra: CF-e SAT em UF SAT gera chave com o cUF daquele estado; CF-e Serviços é federal.
        var satOk = await svc.GerarChaveAsync(baseCmd with { Uf = "CE", Modulo = 59 }, default);
        Assert.True(satOk.Valida);
        Assert.Equal("23", satOk.Chave!.Chave[..2]);
        Assert.Equal(DfeNacionalService.Modulo11(satOk.Chave.Chave[..43]).Dv, satOk.Chave.DigitoVerificador);

        var servicosOk = await svc.GerarChaveAsync(baseCmd with { Modulo = 67 }, default);
        Assert.True(servicosOk.Valida);
        Assert.Equal(67, servicosOk.Chave!.Modulo);
    }

    [Fact]
    public async Task Servico_expo_catalogo_de_ufs_e_layouts_ordenado_pela_fonte_unica()
    {
        await GarantirSchemaAsync();
        var svc = Servico();

        var ufs = (await svc.ListarUfsAsync(default)).ToList();
        Assert.Equal(27, ufs.Count);
        Assert.Equal(EsperadoCuf.Values.OrderBy(v => v).ToList(), ufs.Select(u => u.Cuf));
        Assert.All(ufs, u => Assert.False(string.IsNullOrWhiteSpace(u.Descricao)));
        Assert.All(ufs, u => Assert.False(string.IsNullOrWhiteSpace(u.FonteOficial)));
        foreach (var u in ufs)
            Assert.True(EsperadoUfsSat.Contains(u.Uf) == u.DocumentosApoiados.Contains("CFE_SAT"), $"apoio SAT de {u.Uf}");

        var sp = ufs.First(u => u.Uf == "SP");
        Assert.Equal(new string[] { "NFE", "NFC-E", "CFE_SERVICOS" }, sp.DocumentosApoiados);
        var ac = ufs.First(u => u.Uf == "AC");
        Assert.Equal(new string[] { "NFE", "CFE_SAT", "CFE_SERVICOS" }, ac.DocumentosApoiados);

        var layouts = (await svc.ListarLayoutsAsync(default)).ToList();
        Assert.Equal(4, layouts.Count);
        foreach (var l in layouts)
        {
            Assert.Equal(EsperadoLayouts[l.Documento], l.Versao);
            Assert.False(string.IsNullOrWhiteSpace(l.Nome));
            Assert.False(string.IsNullOrWhiteSpace(l.FonteOficial));
        }
    }
}
