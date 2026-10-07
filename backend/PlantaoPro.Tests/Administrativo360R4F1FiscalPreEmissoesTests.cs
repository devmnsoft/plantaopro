using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// F1 (Rodada 4 — Fiscal) — camada de dados e regras da pré-emissão (MVP A29):
/// 1. Parâmetros fiscais: upsert único por tenant e status derivado deterministicamente
///    (completo = CONFIGURADO/BLOQUEADO; incompleto = PENDENTE_DE_CONFIGURACAO — pendência P2).
/// 2. Pré-nota: criação com numero sequencial NPE-xxxx, conferência de referência de origem
///    (VENDA/ORCAMENTO/COTACAO exigem documento existente no tenant; MANUAL é livre).
/// 3. Transições com invariante de evidência (H19/L33): AUTORIZADA exige chave de 44 dígitos +
///    momento; REJEITADA exige momento + mensagem; CANCELADA exige motivo; estados externos
///    nunca sem evidência válida (espelhado nos CHECKs do banco, migração v2321).
/// 4. Isolamento entre tenants e trilha de eventos EMISSAO_FISCAL imutável/idempotente.
/// </summary>
[Collection("A360Transmissao")]
public sealed class Administrativo360R4F1FiscalPreEmissoesTests
{
    // Tenants dedicados desta suíte (prefixo b3f7a101 — nunca usados por seeds de outras suítes).
    private static readonly Guid TenantA = Guid.Parse("b3f7a101-2c64-4e5a-9ea9-f10000000001");
    private static readonly Guid TenantB = Guid.Parse("b3f7a101-2c64-4e5a-9ea9-f10000000002");
    private static readonly Guid Usuario = Guid.Parse("b3f7a101-2c64-4e5a-9ea9-f1ff00000011");

    /// <summary>Chave de acesso NF-e válida: 44 dígitos (UF+AAMM+CNPJ+MOD+SÉRIE+NFE+EMI+CNF+CDV).</summary>
    private const string Chave44 = "55010212345678000195550011234567891123456781";

    /// <summary>Chave inválida: 43 dígitos (faltou um).</summary>
    private const string Chave43 = "5501021234567800019555001123456789112345678";

    // =====================================================================
    // Garantia de schema: aplica a migração fiscal (v2321) se ausente.
    // =====================================================================
    private static async Task GarantirSchemaAsync(string cs)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('adm360_eventos_tests_schema'));");
        try
        {
            // Marcador F1 (v2321): presença da coluna status dos parâmetros fiscais.
            var temV2321 = await cn.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS(
                    SELECT 1 FROM information_schema.columns
                     WHERE table_schema = 'plantaopro'
                       AND table_name = 'adm360_parametros_fiscais'
                       AND column_name = 'status'
                );");

            if (!temV2321)
            {
                var arquivo = "database/migrations/2026_10_v2321_adm360_fiscal_parametros_pre_emitidas.sql";
                var caminho = Path.Combine(RepositoryPathResolver.RepoRoot, arquivo);
                Assert.True(File.Exists(caminho), $"Migration não encontrada: {arquivo}");
                await ExecutarMigrationComRetryAsync(cn, await File.ReadAllTextAsync(caminho));
            }
        }
        finally
        {
            await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('adm360_eventos_tests_schema'));");
        }
    }

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

    private static async Task GarantirTenantAsync(string cs, Guid tenantId, string nome)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync(
            "insert into plantaopro.tenants(id, nome) values (@id, @nome) on conflict (id) do nothing;",
            new { id = tenantId, nome });
    }

    private static async Task LimparAsync(string cs)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            // Bypass canônico (v2301): a proteção append-only de adm360_eventos /
            // adm360_documento_eventos / adm360_vale_eventos só é liberada dentro desta
            // transação; sem ele o delete dos eventos do tenant levantaria P0001.
            await cn.ExecuteAsync("SELECT set_config('plantao.bypass_imutabilidade_adm360', 'on', true);", transaction: tx);
            await cn.ExecuteAsync(Adm360TenantCleanup.SqlDelete(TenantA, TenantB), tx);
            await tx.CommitAsync();
        }
        catch
        {
            try { await tx.RollbackAsync(); } catch { /* best-effort */ }
            throw;
        }
    }

    // =====================================================================
    // 1. Regras de domínio — parâmetros (P2: pendência registrada, nunca inferida)
    // =====================================================================

    [Theory]
    [InlineData(true, false, ParametrosFiscaisRegras.Configurado)]
    [InlineData(true, true, ParametrosFiscaisRegras.Bloqueado)]
    [InlineData(false, false, ParametrosFiscaisRegras.PendenteDeConfiguracao)]
    [InlineData(false, true, ParametrosFiscaisRegras.PendenteDeConfiguracao)]
    public void Parametros_status_eh_derivado_da_completude_do_conjunto_comercial(
        bool completo, bool bloqueioExterno, string statusEsperado)
    {
        Assert.Equal(statusEsperado, ParametrosFiscaisRegras.DerivarStatus(completo, bloqueioExterno));
    }

    [Theory]
    [InlineData(null, "Sao Paulo", null, null, "SEFAZ_DIRETO", "PRODUCAO", false)]
    [InlineData("SP", null, null, null, "SEFAZ_DIRETO", "PRODUCAO", false)]
    [InlineData("SP", "Sao Paulo", null, null, "SEFAZ_DIRETO", "PRODUCAO", false)]
    [InlineData("SP", "Sao Paulo", null, "VENDA", "SEFAZ_DIRETO", "PRODUCAO", false)]
    [InlineData("SP", "Sao Paulo", "SIMPLES_NACIONAL", "VENDA", null, "PRODUCAO", false)]
    [InlineData("SP", "Sao Paulo", "SIMPLES_NACIONAL", "VENDA", "SEFAZ_DIRETO", "PENDENTE", false)]
    [InlineData("SP", "Sao Paulo", "SIMPLES_NACIONAL", "VENDA", "SEFAZ_DIRETO", "HOMOLOGACAO", true)]
    public void Parametros_conjunto_completo_somente_com_todas_as_decisoes_presentes(
        string? uf, string? municipio, string? regime, string? operacao, string? provedor, string ambiente, bool esperado)
    {
        Assert.Equal(esperado, ParametrosFiscaisRegras.ConjuntoCompleto(uf, municipio, regime, operacao, provedor, ambiente));
    }

    [Fact]
    public void Parametros_campo_invalido_bloqueia_a_gravacao()
    {
        // UF com 3 letras.
        Assert.Throws<Administrativo360BusinessException>(() =>
            ParametrosFiscaisRegras.ValidarCampos("SPA", "SP", null, null, null, "PENDENTE", null, null));
        // Regime fora do conjunto canônico.
        Assert.Throws<Administrativo360BusinessException>(() =>
            ParametrosFiscaisRegras.ValidarCampos(null, null, "SIMPLES", null, null, "PENDENTE", null, null));
        // Ambiente inválido.
        Assert.Throws<Administrativo360BusinessException>(() =>
            ParametrosFiscaisRegras.ValidarCampos(null, null, null, null, null, "TESTE", null, null));
        // Provedor inválido.
        Assert.Throws<Administrativo360BusinessException>(() =>
            ParametrosFiscaisRegras.ValidarCampos(null, null, null, null, null, "PENDENTE", "SEFAZ", null));
        // CFOP para operação desconhecida (sem CFOP universal — L376).
        Assert.Throws<Administrativo360BusinessException>(() =>
            ParametrosFiscaisRegras.ValidarCampos(null, null, null, null,
                new Dictionary<string, string> { ["EXPORTACAO"] = "5101" }, "PENDENTE", null, null));
        // CFOP não numérico / fora de 4 dígitos.
        Assert.Throws<Administrativo360BusinessException>(() =>
            ParametrosFiscaisRegras.ValidarCampos(null, null, null, null,
                new Dictionary<string, string> { ["VENDA"] = "510" }, "PENDENTE", null, null));
        // Credencial que parece valor (espaço) em vez de nome de referência (A33).
        Assert.Throws<Administrativo360BusinessException>(() =>
            ParametrosFiscaisRegras.ValidarCampos(null, null, null, null, null, "PENDENTE", null, "cert sp homo"));
        // CFOP válido não lança.
        ParametrosFiscaisRegras.ValidarCampos(null, null, null, null,
            new Dictionary<string, string> { ["VENDA"] = "5101", ["REMESSA"] = "6101" }, "PENDENTE", null, "SEFAZ_CERT_SP_HOMO");
    }

    [Fact]
    public void Bloqueio_emissao_deriva_dos_parametros_nunca_sucesso_ficticio()
    {
        Assert.NotNull(NotaPreEmitidaRegras.MotivoBloqueioEmissao(null));
        Assert.Contains("P2", NotaPreEmitidaRegras.MotivoBloqueioEmissao(new ParametrosFiscaisSnapshot(
            ParametrosFiscaisRegras.PendenteDeConfiguracao, null, false, null)));

        var bloqueada = NotaPreEmitidaRegras.MotivoBloqueioEmissao(new ParametrosFiscaisSnapshot(
            ParametrosFiscaisRegras.Bloqueado, "REF", true, "aguardando convênio estadual"));
        Assert.Contains("bloqueada", bloqueada, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aguardando convênio estadual", bloqueada);

        Assert.Contains("Credencial", NotaPreEmitidaRegras.MotivoBloqueioEmissao(new ParametrosFiscaisSnapshot(
            ParametrosFiscaisRegras.Configurado, null, false, null)));
        Assert.Contains("indisponível", NotaPreEmitidaRegras.MotivoBloqueioEmissao(new ParametrosFiscaisSnapshot(
            ParametrosFiscaisRegras.Configurado, "REF", false, null)));
        Assert.Null(NotaPreEmitidaRegras.MotivoBloqueioEmissao(new ParametrosFiscaisSnapshot(
            ParametrosFiscaisRegras.Configurado, "REF", true, null)));
    }

    // =====================================================================
    // 2. Regras de domínio — transições e evidência da pré-nota (H19/L33)
    // =====================================================================

    [Theory]
    [InlineData("RASCUNHO", "PRONTA_PARA_EMISSAO")]
    [InlineData("PRONTA_PARA_EMISSAO", "RASCUNHO")]
    [InlineData("PRONTA_PARA_EMISSAO", "ENVIANDO")]
    [InlineData("ENVIANDO", "AUTORIZADA")]
    [InlineData("ENVIANDO", "REJEITADA")]
    [InlineData("ENVIANDO", "PENDENTE_CONFIRMACAO")]
    [InlineData("ENVIANDO", "PRONTA_PARA_EMISSAO")]
    [InlineData("PENDENTE_CONFIRMACAO", "AUTORIZADA")]
    [InlineData("PENDENTE_CONFIRMACAO", "REJEITADA")]
    [InlineData("REJEITADA", "PRONTA_PARA_EMISSAO")]
    [InlineData("RASCUNHO", "CANCELADA")]
    [InlineData("PRONTA_PARA_EMISSAO", "CANCELADA")]
    [InlineData("ENVIANDO", "CANCELADA")]
    [InlineData("PENDENTE_CONFIRMACAO", "CANCELADA")]
    [InlineData("REJEITADA", "CANCELADA")]
    // Mesma situação = no-op idempotente (mesma regra usada pelo repositório em transição repetida).
    [InlineData("CANCELADA", "CANCELADA")]
    [InlineData("AUTORIZADA", "AUTORIZADA")]
    public void Transicao_valida_pela_matriz(string atual, string nova)
    {
        NotaPreEmitidaRegras.ValidarTransicao(atual, nova);
    }

    [Theory]
    [InlineData("RASCUNHO", "ENVIANDO")]
    [InlineData("RASCUNHO", "AUTORIZADA")]
    [InlineData("PRONTA_PARA_EMISSAO", "AUTORIZADA")]
    [InlineData("ENVIANDO", "RASCUNHO")]
    [InlineData("PENDENTE_CONFIRMACAO", "PRONTA_PARA_EMISSAO")]
    [InlineData("AUTORIZADA", "PRONTA_PARA_EMISSAO")]
    [InlineData("AUTORIZADA", "ENVIANDO")]
    [InlineData("AUTORIZADA", "CANCELADA")]
    [InlineData("CANCELADA", "RASCUNHO")]
    [InlineData("CANCELADA", "PRONTA_PARA_EMISSAO")]
    public void Transicao_invalida_lanca_excecao_de_negocio(string atual, string nova)
    {
        Assert.Throws<Administrativo360BusinessException>(() =>
            NotaPreEmitidaRegras.ValidarTransicao(atual, nova));
    }

    [Fact]
    public void Autorizada_exige_chave_de_quarenta_e_quatro_digitos_e_momento()
    {
        Assert.True(NotaPreEmitidaRegras.ChaveAcessoValida(Chave44));
        Assert.False(NotaPreEmitidaRegras.ChaveAcessoValida(Chave43));
        Assert.False(NotaPreEmitidaRegras.ChaveAcessoValida(null));
        Assert.False(NotaPreEmitidaRegras.ChaveAcessoValida("ABCDE"));

        var agora = DateTime.UtcNow;
        NotaPreEmitidaRegras.ValidarEvidenciaParaSituacao(NotaPreEmitidaSituacoes.Autorizada, Chave44, agora, null);
        Assert.Throws<Administrativo360BusinessException>(() =>
            NotaPreEmitidaRegras.ValidarEvidenciaParaSituacao(NotaPreEmitidaSituacoes.Autorizada, Chave43, agora, null));
        Assert.Throws<Administrativo360BusinessException>(() =>
            NotaPreEmitidaRegras.ValidarEvidenciaParaSituacao(NotaPreEmitidaSituacoes.Autorizada, null, agora, null));
        Assert.Throws<Administrativo360BusinessException>(() =>
            NotaPreEmitidaRegras.ValidarEvidenciaParaSituacao(NotaPreEmitidaSituacoes.Autorizada, Chave44, null, null));
    }

    [Fact]
    public void Rejeitada_exige_momento_e_mensagem_do_emissor()
    {
        var agora = DateTime.UtcNow;
        NotaPreEmitidaRegras.ValidarEvidenciaParaSituacao(NotaPreEmitidaSituacoes.Rejeitada, null, agora, "CFOP inválido");
        Assert.Throws<Administrativo360BusinessException>(() =>
            NotaPreEmitidaRegras.ValidarEvidenciaParaSituacao(NotaPreEmitidaSituacoes.Rejeitada, null, null, "CFOP inválido"));
        Assert.Throws<Administrativo360BusinessException>(() =>
            NotaPreEmitidaRegras.ValidarEvidenciaParaSituacao(NotaPreEmitidaSituacoes.Rejeitada, null, agora, "  "));
    }

    [Fact]
    public void Origem_nao_manual_exige_documento_de_origem()
    {
        NotaPreEmitidaRegras.ValidarOrigem("MANUAL", null);
        NotaPreEmitidaRegras.ValidarOrigem("VENDA", Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => NotaPreEmitidaRegras.ValidarOrigem("COMPRAS", Guid.NewGuid()));
        Assert.Throws<Administrativo360BusinessException>(() => NotaPreEmitidaRegras.ValidarOrigem("VENDA", null));
        Assert.Throws<Administrativo360BusinessException>(() => NotaPreEmitidaRegras.ValidarOrigem("ORCAMENTO", null));
        Assert.Throws<Administrativo360BusinessException>(() => NotaPreEmitidaRegras.ValidarOrigem("COTACAO", null));
    }

    [Fact]
    public void Cancelamento_exige_momento_e_motivo()
    {
        NotaPreEmitidaRegras.ValidarCancelamento(DateTime.UtcNow, "documento duplicado");
        Assert.Throws<Administrativo360BusinessException>(() => NotaPreEmitidaRegras.ValidarCancelamento(null, "motivo"));
        Assert.Throws<Administrativo360BusinessException>(() => NotaPreEmitidaRegras.ValidarCancelamento(DateTime.UtcNow, null));
    }

    // =====================================================================
    // 3. Integração PG — parâmetros fiscais
    // =====================================================================

    [Fact]
    public async Task Parametros_upsert_unico_por_tenant_com_status_progressivo()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        try
        {
            var repo = new ParametrosFiscaisRepository(cs);

            // P2 registrada: cadastro parcial -> PENDENTE_DE_CONFIGURACAO.
            var parcial = await repo.SalvarAsync(TenantA, Usuario, new SalvarParametrosFiscaisCommand(
                Uf: "sp", Municipio: null, RegimeFiscal: null, OperacaoFiscal: null, Cfops: null,
                ResponsavelId: null, ResponsavelNome: "Financeiro", Ambiente: "PENDENTE",
                Provedor: null, CertificadoReferencia: null, Observacao: null));
            Assert.Equal(ParametrosFiscaisRegras.PendenteDeConfiguracao, parcial.Status);
            Assert.Equal("SP", parcial.Uf);

            // Decisão comercial completada -> CONFIGURADO, sem duplicar linha.
            var completo = await repo.SalvarAsync(TenantA, Usuario, new SalvarParametrosFiscaisCommand(
                Uf: "SP", Municipio: "Sao Paulo", RegimeFiscal: "simples_nacional", OperacaoFiscal: "venda",
                Cfops: new Dictionary<string, string> { ["VENDA"] = "5101" },
                ResponsavelId: Usuario, ResponsavelNome: "Financeiro", Ambiente: "HOMOLOGACAO",
                Provedor: "SEFAZ_DIRETO", CertificadoReferencia: "SEFAZ_CERT_SP_HOMO", Observacao: null));
            Assert.Equal(ParametrosFiscaisRegras.Configurado, completo.Status);
            Assert.Equal(parcial.Id, completo.Id);
            Assert.Equal("SIMPLES_NACIONAL", completo.RegimeFiscal);
            Assert.Equal("VENDA", completo.OperacaoFiscal);
            Assert.Equal("5101", completo.Cfops["VENDA"]);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            var linhas = await cn.ExecuteScalarAsync<int>(
                "select count(*) from plantaopro.adm360_parametros_fiscais where tenant_id = @t", new { t = TenantA });
            Assert.Equal(1, linhas);
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    [Fact]
    public async Task Parametros_conjunto_completo_com_bloqueio_externo_fica_bloqueado()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        try
        {
            var repo = new ParametrosFiscaisRepository(cs);
            var salvo = await repo.SalvarAsync(TenantA, Usuario, new SalvarParametrosFiscaisCommand(
                "SP", "Sao Paulo", "LUCRO_PRESUMIDO", "VENDA",
                new Dictionary<string, string> { ["VENDA"] = "5101" },
                null, null, "PRODUCAO", "OPMENEXO", "OPMENEXO_TOKEN_PROD",
                Observacao: "aguardando credencial do provedor", BloqueioExterno: true));
            Assert.Equal(ParametrosFiscaisRegras.Bloqueado, salvo.Status);
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    [Fact]
    public async Task Parametros_isolamento_total_entre_tenants()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        await GarantirTenantAsync(cs, TenantB, "tenant-f1-b");
        try
        {
            var repo = new ParametrosFiscaisRepository(cs);
            await repo.SalvarAsync(TenantA, Usuario, new SalvarParametrosFiscaisCommand(
                "SP", null, null, null, null, null, null, "PENDENTE", null, null, null));

            var doB = await repo.ObterAsync(TenantB);
            Assert.Null(doB);

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
            // Contagem escopada aos tenants sob teste: a contagem global quebraria
            // assim que outro tenant ganhasse parâmetros (ex.: uso dev pela Web).
            var total = await cn.ExecuteScalarAsync<int>(
                "select count(*) from plantaopro.adm360_parametros_fiscais where tenant_id in (@a, @b)",
                new { a = TenantA, b = TenantB });
            Assert.Equal(1, total);
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    [Fact]
    public async Task Parametros_tenant_inexistente_bloqueia_o_cadastro()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        try
        {
            var repo = new ParametrosFiscaisRepository(cs);
            Assert.ThrowsAsync<KeyNotFoundException>(() => repo.SalvarAsync(Guid.NewGuid(), Usuario,
                new SalvarParametrosFiscaisCommand("SP", null, null, null, null, null, null, "PENDENTE", null, null, null)))
                .Wait();
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    // =====================================================================
    // 4. Integração PG — pré-nota: criação, conferência de referência, itens
    // =====================================================================

    [Fact]
    public async Task Nota_criacao_manual_gera_numero_sequencial_e_itens()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        try
        {
            var repo = new NotasPreEmitidasRepository(cs);
            var id = await repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand(
                "MANUAL", null, "Clinica Santa Casa", "12.345.678/0001-90",
                new[]
                {
                    new NotaPreEmitidaItemCommand("Kit curativo", 2, 15.50m),
                    new NotaPreEmitidaItemCommand("Luva M (caixa)", 1, 89.90m)
                }));

            var detalhes = await repo.ObterAsync(TenantA, id);
            Assert.NotNull(detalhes);
            Assert.Matches("^NPE-[0-9]{8}$", detalhes!.Resumo.Numero);
            Assert.Equal(NotaPreEmitidaSituacoes.Rascunho, detalhes.Resumo.Situacao);
            Assert.Equal(2, detalhes.Itens.Count);
            Assert.Equal(120.90m, Math.Round(detalhes.Resumo.ValorTotal, 4));
            Assert.Equal(31.00m, detalhes.Itens.First(i => i.Descricao == "Kit curativo").Total);

            var lista = await repo.ListarAsync(TenantA);
            Assert.Single(lista);
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    [Fact]
    public async Task Nota_isolamento_total_entre_tenants()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        await GarantirTenantAsync(cs, TenantB, "tenant-f1-b");
        try
        {
            var repo = new NotasPreEmitidasRepository(cs);
            var itens = new[] { new NotaPreEmitidaItemCommand("Item A", 1, 10m) };
            var idA = await repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand("MANUAL", null, "Cliente A", null, itens));

            Assert.Empty(await repo.ListarAsync(TenantB));
            Assert.Null(await repo.ObterAsync(TenantB, idA));
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    [Fact]
    public async Task Nota_origem_venda_exige_referencia_existente_no_tenant()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        try
        {
            var repo = new NotasPreEmitidasRepository(cs);
            var itens = new[] { new NotaPreEmitidaItemCommand("Item", 1, 10m) };

            // Origem não-manual sem identificador (regra de conferência — A29).
            await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand("VENDA", null, "Cliente", null, itens)));

            // Identificador que não existe no tenant.
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand("VENDA", Guid.NewGuid(), "Cliente", null, itens)));
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    [Fact]
    public async Task Nota_itens_somente_editaveis_em_rascunho()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        try
        {
            var repo = new NotasPreEmitidasRepository(cs);
            var id = await repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand(
                "MANUAL", null, "Cliente", null, new[] { new NotaPreEmitidaItemCommand("Item", 1, 10m) }));

            // Em RASCUNHO: substituição integral + totais recalculados.
            await repo.AtualizarItensAsync(TenantA, Usuario, new AtualizarNotaPreEmitidaItensCommand(id,
                new[] { new NotaPreEmitidaItemCommand("Item novo", 3, 7.25m) }));
            var apos = await repo.ObterAsync(TenantA, id);
            Assert.Single(apos!.Itens);
            Assert.Equal(21.75m, Math.Round(apos.Resumo.ValorTotal, 4));

            // Fora de RASCUNHO: travada.
            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(
                id, NotaPreEmitidaSituacoes.ProntaParaEmissao));
            await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.AtualizarItensAsync(TenantA, Usuario, new AtualizarNotaPreEmitidaItensCommand(id,
                    new[] { new NotaPreEmitidaItemCommand("Outro", 1, 1m) })));
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    [Fact]
    public async Task Nota_sem_itens_e_sem_destinatario_nao_existe()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        try
        {
            var repo = new NotasPreEmitidasRepository(cs);
            await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand("MANUAL", null, "Cliente", null, Array.Empty<NotaPreEmitidaItemCommand>())));
            await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand("MANUAL", null, "  ", null,
                    new[] { new NotaPreEmitidaItemCommand("Item", 0, 10m) })));
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    // =====================================================================
    // 5. Integração PG — transições com evidência + eventos imutáveis
    // =====================================================================

    [Fact]
    public async Task Transicao_autorizada_sem_evidencia_nunca_consola_estado()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        try
        {
            var repo = new NotasPreEmitidasRepository(cs);
            var id = await repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand(
                "MANUAL", null, "Cliente", null, new[] { new NotaPreEmitidaItemCommand("Item", 1, 10m) }));
            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.ProntaParaEmissao));
            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.Enviando));

            // Sem evidência externa: bloqueado no domínio e o estado permanece ENVIANDO.
            await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(
                    id, NotaPreEmitidaSituacoes.Autorizada, ChaveAcessoExterna: Chave43, EmitidaEm: DateTime.UtcNow)));

            var estado = await repo.ObterAsync(TenantA, id);
            Assert.Equal(NotaPreEmitidaSituacoes.Enviando, estado!.Resumo.Situacao);

            // Com evidência válida (chave 44 + momento): autorizado de fato.
            var emitidaEm = DateTime.UtcNow.AddMinutes(-1);
            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(
                id, NotaPreEmitidaSituacoes.Autorizada, ChaveAcessoExterna: Chave44,
                ProtocoloExterno: "PROT-0001", EmitidaEm: emitidaEm));

            var autorizada = await repo.ObterAsync(TenantA, id);
            Assert.Equal(NotaPreEmitidaSituacoes.Autorizada, autorizada!.Resumo.Situacao);
            Assert.Equal(Chave44, autorizada.Resumo.ChaveAcessoExterna);
            Assert.NotNull(autorizada.Resumo.EmitidaEm);

            // AUTORIZADA é terminal internamente (cancelamento oficial externo é P1).
            await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(
                    id, NotaPreEmitidaSituacoes.Cancelada, CanceladaEm: DateTime.UtcNow, MotivoCancelamento: "erro de digitação")));
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    [Fact]
    public async Task Transicao_rejeitada_exige_retorno_externo_completo()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        try
        {
            var repo = new NotasPreEmitidasRepository(cs);
            var id = await repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand(
                "MANUAL", null, "Cliente", null, new[] { new NotaPreEmitidaItemCommand("Item", 1, 10m) }));
            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.ProntaParaEmissao));
            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.Enviando));

            await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(
                    id, NotaPreEmitidaSituacoes.Rejeitada, EmitidaEm: DateTime.UtcNow)));

            var agora = DateTime.UtcNow;
            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(
                id, NotaPreEmitidaSituacoes.Rejeitada, ProtocoloExterno: "PROT-0002",
                EmitidaEm: agora, RejeicaoMensagem: "CFOP incompatível com a operação"));
            var rejeitada = await repo.ObterAsync(TenantA, id);
            Assert.Equal(NotaPreEmitidaSituacoes.Rejeitada, rejeitada!.Resumo.Situacao);
            Assert.Equal("CFOP incompatível com a operação", rejeitada.RejeicaoMensagem);
            Assert.Null(rejeitada.Resumo.ChaveAcessoExterna);
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    [Fact]
    public async Task Transicao_cancelada_exige_motivo_e_encerra_o_ciclo()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        try
        {
            var repo = new NotasPreEmitidasRepository(cs);
            var id = await repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand(
                "MANUAL", null, "Cliente", null, new[] { new NotaPreEmitidaItemCommand("Item", 1, 10m) }));

            await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(
                    id, NotaPreEmitidaSituacoes.Cancelada, CanceladaEm: DateTime.UtcNow, MotivoCancelamento: null)));

            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(
                id, NotaPreEmitidaSituacoes.Cancelada, MotivoCancelamento: "cliente desistiu"));
            var cancelada = await repo.ObterAsync(TenantA, id);
            Assert.Equal(NotaPreEmitidaSituacoes.Cancelada, cancelada!.Resumo.Situacao);
            Assert.NotNull(cancelada.CanceladaEm);
            Assert.Equal("cliente desistiu", cancelada.MotivoCancelamento);

            // Estado terminal: nenhuma saída.
            await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(
                    id, NotaPreEmitidaSituacoes.ProntaParaEmissao)));
        }
        finally
        {
            await LimparAsync(cs);
        }
    }

    [Fact]
    public async Task Transicao_registra_evento_emissao_fiscal_e_repeticao_nao_duplica()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirSchemaAsync(cs);
        await LimparAsync(cs);
        await GarantirTenantAsync(cs, TenantA, "tenant-f1-a");
        try
        {
            var repo = new NotasPreEmitidasRepository(cs);
            var id = await repo.CriarAsync(TenantA, Usuario, new CriarNotaPreEmitidaCommand(
                "MANUAL", null, "Cliente", null, new[] { new NotaPreEmitidaItemCommand("Item", 1, 10m) }));
            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.ProntaParaEmissao));

            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();

            async Task<int> ContarEventos(Guid notaId) =>
                (int)(await cn.ExecuteScalarAsync<long>(new CommandDefinition(
                    "select count(*) from plantaopro.adm360_eventos where tenant_id = @t and entidade_id = @id and tipo_evento = 'EMISSAO_FISCAL'",
                    new { t = TenantA, id = notaId })));

            // criacao + PRONTA = 2 eventos.
            Assert.Equal(2, await ContarEventos(id));

            // Repetir a mesma situação: idempotente (sem novo evento, sem erro).
            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.ProntaParaEmissao));
            Assert.Equal(2, await ContarEventos(id));

            // Avanço real: ENVIANDO adiciona exatamente 1 evento.
            await repo.TransicionarAsync(TenantA, Usuario, new TransicionarNotaPreEmitidaCommand(id, NotaPreEmitidaSituacoes.Enviando));
            Assert.Equal(3, await ContarEventos(id));
        }
        finally
        {
            await LimparAsync(cs);
        }
    }
}
