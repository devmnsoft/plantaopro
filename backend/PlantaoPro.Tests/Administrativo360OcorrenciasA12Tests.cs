using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

// Rodada 10 r10b1 — writer da adm360_ocorrencias (spec A12); Rodada 11 r11b1 — bloqueio
// imediato de saldo (A12 L112): abrir gera pernas BLOQUEIO_OCORRENCIA (LIBERADO ->
// BLOQUEADO, abrangendo saldos internos e externos do lote), resolver inverte as pernas
// (LIBERACAO_OCORRENCIA); tratar/encerrar não geram pernas. Contrato: progressão estrita
// ABERTA -> EM_TRATAMENTO -> RESOLVIDA -> ENCERRADA (um passo por vez), encerrar exige
// destino + justificativa, e a soma física por (lote, local) é invariante no ciclo
// completo ("Registro de ocorrência não apaga estoque").
public sealed class Administrativo360OcorrenciasA12Tests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private static string ObterConnectionString() => TestDatabase.ConnectionString;

    private static async Task GarantirConexaoBancoAsync(string cs)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para a suíte de integração não está acessível: {ex.Message}");
        }
    }

    private sealed record Fixtures(Guid Tenant, Guid Usuario, Guid Produto, Guid Lote, Guid Local);

    private static async Task<Fixtures> CriarFixturesAsync(string cs, string sufixo)
    {
        var tenant = Guid.NewGuid();
        var usuario = Guid.NewGuid();
        var produto = Guid.NewGuid();
        var lote = Guid.NewGuid();
        var local = Guid.NewGuid();

        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync(Adm360TenantCleanup.SqlDelete(tenant)); // idempotente contra resíduos
        await cn.ExecuteAsync($@"
            INSERT INTO plantaopro.tenants(id, tenant_id, codigo, nome, status)
            VALUES(@tenantId, @tenantId, 'a12-{sufixo}-t', 'A12 {sufixo}', 'ATIVO');

            INSERT INTO plantaopro.adm360_produtos(id, tenant_id, sku, nome, unidade, controla_lote, exige_inspecao, preco_custo, ativo)
            VALUES(@produtoId, @tenantId, 'A12-{sufixo}-SKU', 'Produto A12 {sufixo}', 'UN', true, false, 10, true);

            INSERT INTO plantaopro.adm360_lotes(id, tenant_id, produto_id, codigo, validade)
            VALUES(@loteId, @tenantId, @produtoId, 'LOTE-A12-{sufixo}', '2027-12-31');

            INSERT INTO plantaopro.adm360_locais(id, tenant_id, codigo, nome, tipo, ativo)
            VALUES(@localId, @tenantId, 'A12-{sufixo}-LOC', 'Deposito A12 {sufixo}', 'INTERNO', true);",
            new { tenantId = tenant, produtoId = produto, loteId = lote, localId = local });

        return new Fixtures(tenant, usuario, produto, lote, local);
    }

    private static async Task LimparAsync(string cs, params Guid[] tenants)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync(Adm360TenantCleanup.SqlDelete(tenants));
    }

    // ===== helpers do bloqueio imediato (r11b1) =====

    private sealed class PernaRow
    {
        public Guid LocalId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Condicao { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    private static async Task SearSaldoLiberadoAsync(string cs, Guid tenant, Guid usuario, Guid produto, Guid lote, Guid local, decimal qtd, string tag)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_movimentos(id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, origem_tipo, origem_id, idempotency_key, created_by)
            VALUES(gen_random_uuid(), @tenantId, @produtoId, @loteId, @localId, 'ENTRADA', 'LIBERADO', @qtd, 'TESTE', @origemId, @tag, @usuarioId)",
            new { tenantId = tenant, produtoId = produto, loteId = lote, localId = local, qtd, origemId = Guid.NewGuid(), tag, usuarioId = usuario });
    }

    private static async Task<Guid> CriarLocalExtraAsync(string cs, Guid tenant, string codigo, string tipo)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return await cn.ExecuteScalarAsync<Guid>(@"
            INSERT INTO plantaopro.adm360_locais(id, tenant_id, codigo, nome, tipo, ativo)
            VALUES(gen_random_uuid(), @t, @codigo, 'Local extra ' || @codigo, @tipo, true)
            RETURNING id", new { t = tenant, codigo, tipo });
    }

    private static decimal ConsultarDisponivel(string cs, Guid tenant, Guid produto, Guid lote, Guid local)
    {
        using var cn = new NpgsqlConnection(cs);
        cn.Open();
        return cn.ExecuteScalar<decimal?>(
            "select coalesce(disponivel, 0) from plantaopro.adm360_saldos where tenant_id=@t and produto_id=@p and lote_id=@l and local_id=@lo and condicao='LIBERADO'",
            new { t = tenant, p = produto, l = lote, lo = local }) ?? 0m;
    }

    private static decimal ConsultarFisicoPorCondicao(string cs, Guid tenant, Guid produto, Guid lote, Guid local, string condicao)
    {
        using var cn = new NpgsqlConnection(cs);
        cn.Open();
        return cn.ExecuteScalar<decimal?>(
            "select coalesce(fisico, 0) from plantaopro.adm360_saldos where tenant_id=@t and produto_id=@p and lote_id=@l and local_id=@lo and condicao=@c",
            new { t = tenant, p = produto, l = lote, lo = local, c = condicao }) ?? 0m;
    }

    private static decimal SomaFisicaTotal(string cs, Guid tenant)
    {
        using var cn = new NpgsqlConnection(cs);
        cn.Open();
        return cn.ExecuteScalar<decimal>("select coalesce(sum(quantidade), 0) from plantaopro.adm360_movimentos where tenant_id=@t", new { t = tenant });
    }

    private static async Task<List<PernaRow>> PernasDaOcorrenciaAsync(string cs, Guid tenant, Guid ocorrenciaId)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        return (await cn.QueryAsync<PernaRow>(@"
            SELECT local_id AS LocalId, tipo AS Tipo, condicao AS Condicao, quantidade AS Quantidade,
                   coalesce(motivo, '') AS Motivo
            FROM plantaopro.adm360_movimentos
            WHERE tenant_id=@t AND origem_tipo='OCORRENCIA' AND origem_id=@id
            ORDER BY id", new { t = tenant, id = ocorrenciaId })).AsList();
    }

    private static CriarOcorrenciaCommand Novo(Admini360Args args, decimal quantidade = 2m)
        => new(args.Produto, args.Lote, args.Local, "QUEBRA", "Itens danificados na caixa", quantidade,
            Prazo: DateOnly.FromDateTime(DateTime.Today.AddDays(5)));

    private sealed record Admini360Args(Guid Produto, Guid Lote, Guid Local);

    // ============================ unit — máquina de estados ============================

    [Theory]
    [InlineData("ABERTA", "EM_TRATAMENTO", true)]
    [InlineData("EM_TRATAMENTO", "RESOLVIDA", true)]
    [InlineData("RESOLVIDA", "ENCERRADA", true)]
    [InlineData("ABERTA", "RESOLVIDA", false)]
    [InlineData("ABERTA", "ENCERRADA", false)]
    [InlineData("EM_TRATAMENTO", "ABERTA", false)]
    [InlineData("RESOLVIDA", "EM_TRATAMENTO", false)]
    [InlineData("ENCERRADA", "ABERTA", false)]
    [InlineData("ABERTA", "ABERTA", false)]
    public void A12_MaquinaDeEstados_ProgressaoEstritaUmPassoPorVez(string atual, string destino, bool esperado)
        => Assert.Equal(esperado, OcorrenciasA12.PodeTransicionar(atual, destino));

    [Fact]
    public void A12_ValidarCriacao_RejeitaTipoDescricaoVaziosTipoLongoEQuantidadeNaoPositiva()
    {
        Assert.Throws<Administrativo360BusinessException>(() => OcorrenciasA12.ValidarCriacao(" ", "desc", 1m));
        Assert.Throws<Administrativo360BusinessException>(() => OcorrenciasA12.ValidarCriacao(new string('x', 41), "desc", 1m));
        Assert.Throws<Administrativo360BusinessException>(() => OcorrenciasA12.ValidarCriacao("tipo", " ", 1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => OcorrenciasA12.ValidarCriacao("tipo", "desc", 0m));
        // sem Assert.DoesNotThrow neste xunit: chamar direto — exceção falha o fato
        OcorrenciasA12.ValidarCriacao("QUANTIDADE", "Divergencia de 2 unidades", 2m);
    }

    [Fact]
    public void A12_ValidarEncerramento_RequiresDestinoEJustificativa()
    {
        Assert.Throws<Administrativo360BusinessException>(() => OcorrenciasA12.ValidarEncerramento("destino", " "));
        Assert.Throws<Administrativo360BusinessException>(() => OcorrenciasA12.ValidarEncerramento(null, "justificativa"));
        // sem Assert.DoesNotThrow neste xunit: chamar direto — exceção falha o fato
        OcorrenciasA12.ValidarEncerramento("Devolvido ao fornecedor", "Lote com quebra");
    }

    // ============================ integração — ciclo completo ============================

    [Fact]
    public async Task A12_CicloCompleto_AbreTrataResolveEncerraComDestinoEJustificativa()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "ciclo");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            var id = await repo.AbrirAsync(fx.Tenant, fx.Usuario, Novo(new(fx.Produto, fx.Lote, fx.Local)), Ct);
            Assert.NotEqual(Guid.Empty, id);

            var abertas = await repo.ListarAsync(fx.Tenant, "ABERTA", Ct);
            var oc = Assert.Single(abertas);
            Assert.Equal("Produto A12 ciclo", oc.Produto);
            Assert.Equal("LOTE-A12-ciclo", oc.Lote);
            Assert.Equal(OcorrenciasA12.Aberta, oc.Situacao);
            Assert.Equal(1, oc.Versao);
            Assert.Equal(fx.Usuario, oc.ResponsavelId); // default registrado: autor da abertura responde

            await repo.TratarAsync(fx.Tenant, fx.Usuario, id, 1, Ct);
            await repo.ResolverAsync(fx.Tenant, fx.Usuario, id, 2, Ct);
            await repo.EncerrarAsync(fx.Tenant, fx.Usuario, id,
                new EncerrarOcorrenciaCommand(3, "Devolvido ao fornecedor", "Fornecedor confirmou reposicao"), Ct);

            var encerradas = await repo.ListarAsync(fx.Tenant, "ENCERRADA", Ct);
            var fim = Assert.Single(encerradas);
            Assert.Equal(OcorrenciasA12.Encerrada, fim.Situacao);
            Assert.Equal("Devolvido ao fornecedor", fim.Destino);
            Assert.Equal("Fornecedor confirmou reposicao", fim.JustificativaEncerramento);
            Assert.Equal(4, fim.Versao);

            Assert.Empty(await repo.ListarAsync(fx.Tenant, "ABERTA", Ct));
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant);
        }
    }

    [Fact]
    public async Task A12_TransicaoInvalidaEVersaoDesatualizada_RejeitamSemEfeito()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "inv");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            var id = await repo.AbrirAsync(fx.Tenant, fx.Usuario, Novo(new(fx.Produto, fx.Lote, fx.Local)), Ct);

            // pulo de etapa: ABERTA -> RESOLVIDA
            await Assert.ThrowsAsync<Administrativo360BusinessException>(
                () => repo.ResolverAsync(fx.Tenant, fx.Usuario, id, 1, Ct));

            await repo.TratarAsync(fx.Tenant, fx.Usuario, id, 1, Ct);

            // versao desatualizada (1 em vez de 2)
            var conflito = await Assert.ThrowsAsync<Administrativo360BusinessException>(
                () => repo.ResolverAsync(fx.Tenant, fx.Usuario, id, 1, Ct));
            Assert.Contains("alterada por outro", conflito.Message);

            var emTratamento = Assert.Single(await repo.ListarAsync(fx.Tenant, "EM_TRATAMENTO", Ct));
            Assert.Equal(2, emTratamento.Versao);

            // ocorrencia inexistente
            await Assert.ThrowsAsync<Administrativo360BusinessException>(
                () => repo.TratarAsync(fx.Tenant, fx.Usuario, Guid.NewGuid(), 1, Ct));
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant);
        }
    }

    [Fact]
    public async Task A12_EncerrarSemDestinoOuJustificativa_RejeitaEMantemResolvida()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "enc");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            var id = await repo.AbrirAsync(fx.Tenant, fx.Usuario, Novo(new(fx.Produto, fx.Lote, fx.Local)), Ct);
            await repo.TratarAsync(fx.Tenant, fx.Usuario, id, 1, Ct);
            await repo.ResolverAsync(fx.Tenant, fx.Usuario, id, 2, Ct);

            await Assert.ThrowsAsync<Administrativo360BusinessException>(
                () => repo.EncerrarAsync(fx.Tenant, fx.Usuario, id, new EncerrarOcorrenciaCommand(3, " ", null), Ct));
            await Assert.ThrowsAsync<Administrativo360BusinessException>(
                () => repo.EncerrarAsync(fx.Tenant, fx.Usuario, id, new EncerrarOcorrenciaCommand(3, "Destino ok", null), Ct));

            var resolvida = Assert.Single(await repo.ListarAsync(fx.Tenant, "RESOLVIDA", Ct));
            Assert.Equal(3, resolvida.Versao);
            Assert.Null(resolvida.Destino);
            Assert.Null(resolvida.JustificativaEncerramento);
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant);
        }
    }

    [Fact]
    public async Task A12_SemSaldoLiberado_CicloCompleto_NaoGeraPernasDeBloqueio()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "estoque");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            // saldo pré-existente apenas em QUARENTENA (nenhum LIBERADO): o bloqueio imediato
            // não tem candidato e o ciclo completo não gera nenhuma perna da ocorrência
            await using (var cn = new NpgsqlConnection(cs))
            {
                await cn.OpenAsync();
                await cn.ExecuteAsync(@"
                    INSERT INTO plantaopro.adm360_movimentos(id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, origem_tipo, origem_id, idempotency_key, created_by)
                    VALUES(gen_random_uuid(), @tenantId, @produtoId, @loteId, @localId, 'ENTRADA', 'QUARENTENA', 10, 'RECEBIMENTO', gen_random_uuid(), 'a12-' || gen_random_uuid()::text, @usuarioId)",
                    new { tenantId = fx.Tenant, produtoId = fx.Produto, loteId = fx.Lote, localId = fx.Local, usuarioId = fx.Usuario });
            }

            int ContarMovimentos()
            {
                using var cn = new NpgsqlConnection(cs);
                cn.Open();
                return cn.ExecuteScalar<int>("select count(*) from plantaopro.adm360_movimentos where tenant_id=@t", new { t = fx.Tenant });
            }

            var antes = ContarMovimentos();
            var id = await repo.AbrirAsync(fx.Tenant, fx.Usuario, Novo(new(fx.Produto, fx.Lote, fx.Local)), Ct);
            await repo.TratarAsync(fx.Tenant, fx.Usuario, id, 1, Ct);
            await repo.ResolverAsync(fx.Tenant, fx.Usuario, id, 2, Ct);
            await repo.EncerrarAsync(fx.Tenant, fx.Usuario, id,
                new EncerrarOcorrenciaCommand(3, "Descarte assistido", "Quebra confirmada"), Ct);
            var depois = ContarMovimentos();

            Assert.Equal(antes, depois); // nenhuma perna BLOQUEIO/LIBERACAO_OCORRENCIA gerada
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant);
        }
    }

    [Fact]
    public async Task A12_IsolamentoPorTenant_OuTroTenantNaoVejaENaoTransicione()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var a = await CriarFixturesAsync(cs, "isoa");
        var b = await CriarFixturesAsync(cs, "isob");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            var idA = await repo.AbrirAsync(a.Tenant, a.Usuario, Novo(new(a.Produto, a.Lote, a.Local)), Ct);

            Assert.Empty(await repo.ListarAsync(b.Tenant, null, Ct));
            Assert.Single(await repo.ListarAsync(a.Tenant, null, Ct));

            // tentativa do tenant B de tratar ocorrência do tenant A
            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(
                () => repo.TratarAsync(b.Tenant, b.Usuario, idA, 1, Ct));
            Assert.Contains("não encontrada", ex.Message);

            var aindaAberta = Assert.Single(await repo.ListarAsync(a.Tenant, "ABERTA", Ct));
            Assert.Equal(1, aindaAberta.Versao);
        }
        finally
        {
            await LimparAsync(cs, a.Tenant, b.Tenant);
        }
    }

    [Fact]
    public async Task A12_Abrir_ValidaReferenciasExistentesNoTenant()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "fk");
        var outra = await CriarFixturesAsync(cs, "fk2"); // produto/lote de OUTRO tenant
        var repo = new OcorrenciasRepository(cs);
        try
        {
            // produto inexistente no tenant
            await Assert.ThrowsAsync<Administrativo360BusinessException>(
                () => repo.AbrirAsync(fx.Tenant, fx.Usuario, new CriarOcorrenciaCommand(outra.Produto, fx.Lote, fx.Local, "T", "D", 1m), Ct));

            // lote válido mas de outro produto
            await Assert.ThrowsAsync<Administrativo360BusinessException>(
                () => repo.AbrirAsync(fx.Tenant, fx.Usuario, new CriarOcorrenciaCommand(fx.Produto, outra.Lote, fx.Local, "T", "D", 1m), Ct));

            // local inexistente no tenant
            await Assert.ThrowsAsync<Administrativo360BusinessException>(
                () => repo.AbrirAsync(fx.Tenant, fx.Usuario, new CriarOcorrenciaCommand(fx.Produto, fx.Lote, outra.Local, "T", "D", 1m), Ct));

            Assert.Empty(await repo.ListarAsync(fx.Tenant, null, Ct));
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant, outra.Tenant);
        }
    }

    // ============================ R11b1 — bloqueio imediato de saldo (A12 L112 + L296) ============================

    [Fact]
    public async Task R11_Abrir_BloqueioImediato_GeraPernasComOrigemEMotivo_E_BaixaDisponivel()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "blk");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            await SearSaldoLiberadoAsync(cs, fx.Tenant, fx.Usuario, fx.Produto, fx.Lote, fx.Local, 10m, "r11-blk-seed");
            Assert.Equal(10m, ConsultarDisponivel(cs, fx.Tenant, fx.Produto, fx.Lote, fx.Local));

            var id = await repo.AbrirAsync(fx.Tenant, fx.Usuario, Novo(new(fx.Produto, fx.Lote, fx.Local), 2m), Ct);

            var pernas = await PernasDaOcorrenciaAsync(cs, fx.Tenant, id);
            Assert.Equal(2, pernas.Count);

            var saida = Assert.Single(pernas, p => p.Condicao == "LIBERADO");
            Assert.Equal(-2m, saida.Quantidade);
            Assert.Equal("BLOQUEIO_OCORRENCIA", saida.Tipo);

            var entrada = Assert.Single(pernas, p => p.Condicao == "BLOQUEADO");
            Assert.Equal(2m, entrada.Quantidade);
            Assert.Equal("BLOQUEIO_OCORRENCIA", entrada.Tipo);

            // L296 — motivo do bloqueio consultável, com origem na ocorrência
            Assert.All(pernas, p =>
            {
                Assert.Contains("ocorrencia", p.Motivo);
                Assert.Contains(id.ToString(), p.Motivo);
                Assert.Contains("QUEBRA", p.Motivo);
            });

            Assert.Equal(8m, ConsultarDisponivel(cs, fx.Tenant, fx.Produto, fx.Lote, fx.Local));
            Assert.Equal(2m, ConsultarFisicoPorCondicao(cs, fx.Tenant, fx.Produto, fx.Lote, fx.Local, "BLOQUEADO"));
            Assert.Equal(10m, SomaFisicaTotal(cs, fx.Tenant)); // -2/+2: soma física invariante
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant);
        }
    }

    [Fact]
    public async Task R11_AbreETrata_LoteBloqueado_ReservaAcimaDoDisponivel_Falha()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "resv");
        var repo = new OcorrenciasRepository(cs);
        var estoque = new EstoqueRepository(cs);
        try
        {
            await SearSaldoLiberadoAsync(cs, fx.Tenant, fx.Usuario, fx.Produto, fx.Lote, fx.Local, 10m, "r11-resv-seed");

            var id = await repo.AbrirAsync(fx.Tenant, fx.Usuario, Novo(new(fx.Produto, fx.Lote, fx.Local), 8m), Ct);
            Assert.Equal(2m, ConsultarDisponivel(cs, fx.Tenant, fx.Produto, fx.Lote, fx.Local));

            await repo.TratarAsync(fx.Tenant, fx.Usuario, id, 1, Ct);
            // bloqueio persiste durante EM_TRATAMENTO
            Assert.Equal(8m, ConsultarFisicoPorCondicao(cs, fx.Tenant, fx.Produto, fx.Lote, fx.Local, "BLOQUEADO"));

            // L296 — lote bloqueado não pode ser reservado além do saldo disponível
            await estoque.ReservarAsync(fx.Tenant, fx.Usuario,
                new ReservarCommand(fx.Produto, fx.Lote, fx.Local, 2m, "Reserva de teste", Guid.NewGuid(), "r11-resv-k1"), Ct);
            Assert.Equal(0m, ConsultarDisponivel(cs, fx.Tenant, fx.Produto, fx.Lote, fx.Local));

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                estoque.ReservarAsync(fx.Tenant, fx.Usuario,
                    new ReservarCommand(fx.Produto, fx.Lote, fx.Local, 1m, "Reserva de teste 2", Guid.NewGuid(), "r11-resv-k2"), Ct));
            Assert.Contains("excede o estoque disponível", ex.Message);
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant);
        }
    }

    [Fact]
    public async Task R11_Abrir_MultiLocal_InternoEEsterno_CobreAtaQuantidadeEmOrdemDeterministica()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "multi"); // local da ocorrência (INTERNO)
        var localExterno = await CriarLocalExtraAsync(cs, fx.Tenant, "A12-multi-EXT", "EXTERNO");
        var localInterno2 = await CriarLocalExtraAsync(cs, fx.Tenant, "A12-multi-INT2", "INTERNO");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            await SearSaldoLiberadoAsync(cs, fx.Tenant, fx.Usuario, fx.Produto, fx.Lote, fx.Local, 3m, "r11-multi-a");
            await SearSaldoLiberadoAsync(cs, fx.Tenant, fx.Usuario, fx.Produto, fx.Lote, localExterno, 5m, "r11-multi-b");
            await SearSaldoLiberadoAsync(cs, fx.Tenant, fx.Usuario, fx.Produto, fx.Lote, localInterno2, 4m, "r11-multi-c");

            var id = await repo.AbrirAsync(fx.Tenant, fx.Usuario, Novo(new(fx.Produto, fx.Lote, fx.Local), 6m), Ct);

            var bloqueadoPorLocal = (await PernasDaOcorrenciaAsync(cs, fx.Tenant, id))
                .Where(p => p.Condicao == "BLOQUEADO")
                .GroupBy(p => p.LocalId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));

            // ordem determinística: local da ocorrência primeiro (3 de 6); depois por código
            // de local ("EXT" antes de "INT2") cobre os 3 restantes; INT2 não tocado
            Assert.Equal(2, bloqueadoPorLocal.Count);
            Assert.Equal(3m, bloqueadoPorLocal[fx.Local]);
            Assert.Equal(3m, bloqueadoPorLocal[localExterno]);
            Assert.False(bloqueadoPorLocal.ContainsKey(localInterno2));

            Assert.Equal(0m, ConsultarDisponivel(cs, fx.Tenant, fx.Produto, fx.Lote, fx.Local));
            Assert.Equal(2m, ConsultarDisponivel(cs, fx.Tenant, fx.Produto, fx.Lote, localExterno));
            Assert.Equal(4m, ConsultarDisponivel(cs, fx.Tenant, fx.Produto, fx.Lote, localInterno2));
            Assert.Equal(12m, SomaFisicaTotal(cs, fx.Tenant));
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant);
        }
    }

    [Fact]
    public async Task R11_Abrir_SaldoMenorQueQuantidade_BloqueiaParcialENaoRejeita()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "parc");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            await SearSaldoLiberadoAsync(cs, fx.Tenant, fx.Usuario, fx.Produto, fx.Lote, fx.Local, 3m, "r11-parc-seed");

            var id = await repo.AbrirAsync(fx.Tenant, fx.Usuario, Novo(new(fx.Produto, fx.Lote, fx.Local), 10m), Ct);

            var pernas = await PernasDaOcorrenciaAsync(cs, fx.Tenant, id);
            Assert.Equal(3m, pernas.Where(p => p.Condicao == "BLOQUEADO").Sum(p => p.Quantidade));
            Assert.Equal(0m, ConsultarDisponivel(cs, fx.Tenant, fx.Produto, fx.Lote, fx.Local));

            var oc = Assert.Single(await repo.ListarAsync(fx.Tenant, "ABERTA", Ct));
            Assert.Equal(10m, oc.Quantidade); // abertura registrada integralmente; bloqueio parcial
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant);
        }
    }

    [Fact]
    public async Task R11_Resolver_InverteBloqueio_RestauraDisponivelECicloNetZero()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "netz");
        var repo = new OcorrenciasRepository(cs);
        var estoque = new EstoqueRepository(cs);
        try
        {
            await SearSaldoLiberadoAsync(cs, fx.Tenant, fx.Usuario, fx.Produto, fx.Lote, fx.Local, 10m, "r11-netz-seed");
            var somaInicial = SomaFisicaTotal(cs, fx.Tenant);

            var id = await repo.AbrirAsync(fx.Tenant, fx.Usuario, Novo(new(fx.Produto, fx.Lote, fx.Local), 4m), Ct);
            await repo.TratarAsync(fx.Tenant, fx.Usuario, id, 1, Ct);
            await repo.ResolverAsync(fx.Tenant, fx.Usuario, id, 2, Ct);

            var pernas = await PernasDaOcorrenciaAsync(cs, fx.Tenant, id);
            Assert.Equal(4, pernas.Count);

            var liberacao = pernas.Where(p => p.Tipo == "LIBERACAO_OCORRENCIA").ToList();
            Assert.Equal(2, liberacao.Count);
            Assert.Equal(4m, liberacao.Single(p => p.Condicao == "LIBERADO").Quantidade);
            Assert.Equal(-4m, liberacao.Single(p => p.Condicao == "BLOQUEADO").Quantidade);

            Assert.Equal(0m, ConsultarFisicoPorCondicao(cs, fx.Tenant, fx.Produto, fx.Lote, fx.Local, "BLOQUEADO"));
            Assert.Equal(10m, ConsultarDisponivel(cs, fx.Tenant, fx.Produto, fx.Lote, fx.Local));
            Assert.Equal(somaInicial, SomaFisicaTotal(cs, fx.Tenant)); // ciclo completo net-zero

            // itens tratados voltam à circulação: reserva sobre o saldo restaurado é possível
            await estoque.ReservarAsync(fx.Tenant, fx.Usuario,
                new ReservarCommand(fx.Produto, fx.Lote, fx.Local, 4m, "Reserva pos-resolucao", Guid.NewGuid(), "r11-netz-res"), Ct);
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant);
        }
    }

    [Fact]
    public async Task R11_TrataryEncerrar_NaoGeraPernasNovas()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "tc");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            await SearSaldoLiberadoAsync(cs, fx.Tenant, fx.Usuario, fx.Produto, fx.Lote, fx.Local, 10m, "r11-tc-seed");

            var id = await repo.AbrirAsync(fx.Tenant, fx.Usuario, Novo(new(fx.Produto, fx.Lote, fx.Local), 4m), Ct);
            Assert.Equal(2, (await PernasDaOcorrenciaAsync(cs, fx.Tenant, id)).Count);

            await repo.TratarAsync(fx.Tenant, fx.Usuario, id, 1, Ct);
            Assert.Equal(2, (await PernasDaOcorrenciaAsync(cs, fx.Tenant, id)).Count); // TRATAR sem efeito em saldo

            await repo.ResolverAsync(fx.Tenant, fx.Usuario, id, 2, Ct);
            Assert.Equal(4, (await PernasDaOcorrenciaAsync(cs, fx.Tenant, id)).Count); // RESOLVER inverte

            await repo.EncerrarAsync(fx.Tenant, fx.Usuario, id,
                new EncerrarOcorrenciaCommand(3, "Descarte assistido", "Quebra confirmada"), Ct);
            Assert.Equal(4, (await PernasDaOcorrenciaAsync(cs, fx.Tenant, id)).Count); // ENCERRAR sem efeito em saldo
        }
        finally
        {
            await LimparAsync(cs, fx.Tenant);
        }
    }

    [Fact]
    public async Task R11_Abrir_EscopoTenant_NaoTocaSaldoDoOutroTenant()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var a = await CriarFixturesAsync(cs, "ttca");
        var b = await CriarFixturesAsync(cs, "ttcb");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            await SearSaldoLiberadoAsync(cs, a.Tenant, a.Usuario, a.Produto, a.Lote, a.Local, 5m, "r11-ttc-a");
            await SearSaldoLiberadoAsync(cs, b.Tenant, b.Usuario, b.Produto, b.Lote, b.Local, 7m, "r11-ttc-b");

            var idA = await repo.AbrirAsync(a.Tenant, a.Usuario, Novo(new(a.Produto, a.Lote, a.Local), 2m), Ct);

            Assert.Empty(await PernasDaOcorrenciaAsync(cs, b.Tenant, idA));
            Assert.Equal(7m, ConsultarDisponivel(cs, b.Tenant, b.Produto, b.Lote, b.Local));
            Assert.Equal(7m, SomaFisicaTotal(cs, b.Tenant));

            Assert.Equal(2m, ConsultarFisicoPorCondicao(cs, a.Tenant, a.Produto, a.Lote, a.Local, "BLOQUEADO"));
        }
        finally
        {
            await LimparAsync(cs, a.Tenant, b.Tenant);
        }
    }
}
