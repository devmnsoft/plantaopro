using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

// Rodada 10 r10b1 — writer da adm360_ocorrencias (spec A12).
// Contrato: progressão estrita ABERTA → EM_TRATAMENTO → RESOLVIDA → ENCERRADA (um passo por vez),
// encerrar exige destino + justificativa, e nenhuma transição toca estoque (sem movimentos novos).
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
    public async Task A12_RegistroETransicoes_NaoTocamEstoque_NenhumMovimentoGerado()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);
        var fx = await CriarFixturesAsync(cs, "estoque");
        var repo = new OcorrenciasRepository(cs);
        try
        {
            // saldo pré-existente (entrega em quarentena) — a ocorrência NÃO pode alterar nada disso
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

            Assert.Equal(antes, depois);
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
}
