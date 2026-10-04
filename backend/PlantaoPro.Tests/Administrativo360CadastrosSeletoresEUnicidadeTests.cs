using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Tests;

/// <summary>
/// Suíte do Bloco 3 (seletores de cadastros + unicidade por tenant com backstop em banco).
///
/// Contexto: as listagens de Parceiros/Produtos/Locais eram limitadas ao teto de 100 itens dos
/// lookups (com filtragem in-memory na Web) e o histórico inativo era ocultado no padrão.
/// Correção: listagens dedicadas com paginação no servidor (teto de 100 por página, clamps),
/// filtro de histórico inativo, busca/papéis no PostgreSQL e tradução do 23505 (unique violation)
/// em mensagem de negócio nos três cadastros — cobrindo a janela TOCTOU do pré-checagem EXISTS.
///
/// Executa os repositórios REAIS contra PostgreSQL real com dados determinísticos em tenants
/// dedicados (d42a0f30/d42a0f31). A semente é idempotente (delete+insert em comando único) e o
/// prefixo por execução torna os dados auditáveis em consultas manuais.
/// </summary>
public sealed class Administrativo360CadastrosSeletoresEUnicidadeTests
{
    // Tenants dedicados a esta suíte (nenhum outro teste toca esses dados).
    private static readonly Guid TenantA = Guid.Parse("d42a0f30-0042-4000-8000-000000000f30");
    private static readonly Guid TenantB = Guid.Parse("d42a0f31-0042-4000-8000-000000000f31");

    // Prefixo por execução (8 chars): evita colisão com resíduos de execuções anteriores e
    // permite auditar a semente no banco (documentos varchar(20), SKUs varchar(40), códigos varchar(30)).
    private static readonly string Prefixo = "SEL" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();

    // Semente executada uma única vez por processo (idempotente: delete+insert atômico).
    private static readonly Task SementePronta = SementarAsync();

    private readonly CadastrosRepository _cadastros = new(TestDatabase.ConnectionString);

    // =================================================================
    // PAGINAÇÃO E TETO DE 100 ITENS
    // =================================================================

    [Fact(DisplayName = "Produtos: 105 itens → 100 na página 1, 5 na página 2, fora da faixa vazio, clamp de limite e página")]
    public async Task Produtos_Paginacao_TetoCemEClamps()
    {
        await SementePronta;
        var ct = CancellationToken.None;

        var pagina1 = await _cadastros.ListarProdutosAsync(TenantA, null, true, ct, pagina: 1, limite: 100);
        Assert.Equal(100, pagina1.Count);

        var pagina2 = await _cadastros.ListarProdutosAsync(TenantA, null, true, ct, pagina: 2, limite: 100);
        Assert.Equal(5, pagina2.Count);

        // Sem sobreposição entre páginas (ordenação estável por nome).
        Assert.DoesNotContain(pagina2, p => pagina1.Any(q => q.Id == p.Id));
        Assert.Equal(Prefixo + " ITEM 001", pagina1.First().Nome);
        Assert.Equal(Prefixo + " ITEM 105", pagina2.Last().Nome);

        var foraDaFaixa = await _cadastros.ListarProdutosAsync(TenantA, null, true, ct, pagina: 3, limite: 100);
        Assert.Empty(foraDaFaixa);

        // Clamp do limite: 500 → 100 (nunca retorna mais que uma página cheia).
        var limiteEnorme = await _cadastros.ListarProdutosAsync(TenantA, null, true, ct, limite: 500);
        Assert.Equal(100, limiteEnorme.Count);

        // Clamp da página: 0 → 1; e paginação em passos menores (limite=10, página 11 → offset 100).
        var paginaZero = await _cadastros.ListarProdutosAsync(TenantA, null, true, ct, pagina: 0, limite: 10);
        Assert.Equal(10, paginaZero.Count);
        Assert.Equal(Prefixo + " ITEM 001", paginaZero.First().Nome);

        var paginaOnze = await _cadastros.ListarProdutosAsync(TenantA, null, true, ct, pagina: 11, limite: 10);
        Assert.Equal(5, paginaOnze.Count);
    }

    [Fact(DisplayName = "Parceiros: histórico inativo visível no padrão, apenasAtivos/apenasInativos e papéis filtram no servidor")]
    public async Task Parceiros_HistoricoInativoEPapeis()
    {
        await SementePronta;
        var ct = CancellationToken.None;

        var todos = await _cadastros.ListarParceirosAsync(TenantA, null, null, ct);
        Assert.Equal(4, todos.Count);
        Assert.Contains(todos, p => !p.Ativo); // histórico inativo visível por padrão

        var ativos = await _cadastros.ListarParceirosAsync(TenantA, null, null, ct, apenasAtivos: true);
        Assert.Equal(3, ativos.Count);
        Assert.All(ativos, p => Assert.True(p.Ativo));

        var inativos = await _cadastros.ListarParceirosAsync(TenantA, null, null, ct, apenasInativos: true);
        Assert.Equal(1, inativos.Count);
        Assert.False(inativos[0].Ativo);

        var hospitais = await _cadastros.ListarParceirosAsync(TenantA, null, null, ct, ehHospital: true);
        Assert.Single(hospitais);
        Assert.True(hospitais[0].EhHospital);

        var fornecedores = await _cadastros.ListarParceirosAsync(TenantA, null, true, ct);
        Assert.Single(fornecedores);
        Assert.True(fornecedores[0].Fornecedor);

        var pagadores = await _cadastros.ListarParceirosAsync(TenantA, null, null, ct, ehPagador: true);
        Assert.Single(pagadores);
        Assert.True(pagadores[0].EhPagador);

        // Combinação de papel + status (hospital inativo não existe na semente → vazio).
        var combinado = await _cadastros.ListarParceirosAsync(TenantA, null, null, ct, ehHospital: true, apenasInativos: true);
        Assert.Empty(combinado);

        // Busca textual continua funcionando sobre a listagem paginada.
        var busca = await _cadastros.ListarParceirosAsync(TenantA, Prefixo + "-FORN", null, ct);
        Assert.Single(busca);
    }

    [Fact(DisplayName = "Locais: busca textual, filtro de tipo e histórico inativo")]
    public async Task Locais_BuscaTipoEHistoricoInativo()
    {
        await SementePronta;
        var ct = CancellationToken.None;

        var todos = await _cadastros.ListarLocaisAsync(TenantA, null, false, ct);
        Assert.Equal(3, todos.Count);

        var internos = await _cadastros.ListarLocaisAsync(TenantA, "INTERNO", false, ct);
        Assert.Single(internos);
        Assert.Equal("INTERNO", internos[0].Tipo);

        var externos = await _cadastros.ListarLocaisAsync(TenantA, "EXTERNO", false, ct);
        Assert.Equal(2, externos.Count);

        var buscaTodos = await _cadastros.ListarLocaisAsync(TenantA, null, false, ct, busca: "DEPOSITO");
        Assert.Equal(3, buscaTodos.Count);

        var buscaInativo = await _cadastros.ListarLocaisAsync(TenantA, null, false, ct, apenasInativos: true, busca: "INATIVO");
        Assert.Single(buscaInativo);
        Assert.False(buscaInativo[0].Ativo);

        var nada = await _cadastros.ListarLocaisAsync(TenantA, null, false, ct, busca: Prefixo + "-NAOEXISTE");
        Assert.Empty(nada);
    }

    [Fact(DisplayName = "Lookups: teto de 100 itens por lista e vetores de papel explícitos (Bloco 4)")]
    public async Task Lookups_TetoCemE_VetoresDePapelExplícitos()
    {
        await SementePronta;
        var lookups = await _cadastros.ObterLookupsAsync(TenantA, CancellationToken.None);

        // 105 produtos ativos na semente → teto de 100 aplicado na lista geral.
        Assert.Equal(100, lookups.Produtos.Count);

        // Seletores de forms continuam ativos-only: 4 parceiros (1 inativo) → 3; 3 locais (1 inativo) → 2.
        Assert.Equal(3, lookups.Parceiros.Count);
        Assert.All(lookups.Parceiros, p => Assert.True(p.Ativo));
        Assert.Equal(2, lookups.Locais.Count);
        Assert.All(lookups.Locais, l => Assert.True(l.Ativo));

        // Vetores de papel derivados por flags explícitas (sem inferência por ausência de coluna).
        Assert.Single(lookups.Hospitais);
        Assert.True(lookups.Hospitais[0].EhHospital);
        Assert.Single(lookups.Fornecedores);
        Assert.True(lookups.Fornecedores[0].Fornecedor);
        Assert.Single(lookups.Pagadores);
        Assert.True(lookups.Pagadores[0].EhPagador);

        Assert.True(lookups.Lotes.Count <= 100);
        Assert.True(lookups.Medicos.Count <= 100);
    }

    // =================================================================
    // UNICIDADE POR TENANT (backstop em banco + 23505 traduzido)
    // =================================================================

    [Fact(DisplayName = "Parceiro: mesmo documento é aceito entre tenants distintos; duplicado no mesmo tenant falha com mensagem de negócio")]
    public async Task Parceiro_Documento_IsolamentoEntreTenantsEDuplicidadeMesmoTenant()
    {
        await SementePronta;
        var doc = Prefixo + "-P01"; // documento que a semente já possui em um parceiro do Tenant A

        var criadoNoB = await _cadastros.SalvarParceiroAsync(TenantB, null, Prefixo + " DOCE NO TENANT B", doc, false, true, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, criadoNoB); // mesmo documento entre tenants diferentes: aceito

        try
        {
            // Documento já presente no MESMO tenant → pré-checa (ou índice 23505) traduz em mensagem de negócio.
            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
                _cadastros.SalvarParceiroAsync(TenantA, null, Prefixo + " DOCE NO TENANT A", doc, false, true, CancellationToken.None));
            Assert.Contains("Já existe um parceiro cadastrado com este documento neste cliente.", ex.Message);
        }
        finally
        {
            // Estado invariante para os demais testes da classe (execução serial, ordem não garantida).
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.OpenAsync();
            await cn.ExecuteAsync("delete from plantaopro.adm360_parceiros where id = @id", new { id = criadoNoB });
        }
    }

    [Fact(DisplayName = "Parceiro: corrida TOCTOU (INSERT concorrente não commitado) vira 23505 traduzido em mensagem de negócio")]
    public async Task Parceiro_CorridaTOCTOU_BackstopDoBancoTraduz23505()
    {
        await SementePronta;
        var doc = Prefixo + "-TOC";

        // Conexão A ocupa a chave única em transação não commitada: o pré-checagem (READ COMMITTED)
        // não a vê, mas o INSERT do repositório bloqueia no lock até o commit. Qualquer que seja a
        // intercalação, o resultado final é a MESMA mensagem de negócio (pré-checa ou índice 23505).
        var idA = Guid.NewGuid();
        await using var cnA = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cnA.OpenAsync();
        await using var txA = await cnA.BeginTransactionAsync();
        await cnA.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, documento, fornecedor, ativo)
            VALUES (@id, @tenantId, @nome, @doc, false, true)",
            new { id = idA, tenantId = TenantA, nome = Prefixo + " CORRIDA A", doc }, txA);

        try
        {
            var tarefa = Task.Run(() => _cadastros.SalvarParceiroAsync(TenantA, null, Prefixo + " CORRIDA B", doc, false, true, CancellationToken.None));
            await Task.Delay(TimeSpan.FromSeconds(2)); // garante que o INSERT do repositório chegou ao lock
            await txA.CommitAsync();

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(async () => await tarefa);
            Assert.Contains("Já existe um parceiro cadastrado com este documento neste cliente.", ex.Message);
        }
        finally
        {
            // Estado invariante para os demais testes da classe: descarta/commit e remove a linha A.
            await txA.DisposeAsync();
            await using var cnC = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cnC.OpenAsync();
            await cnC.ExecuteAsync("delete from plantaopro.adm360_parceiros where id = @id", new { id = idA });
        }
    }

    [Fact(DisplayName = "Produto: corrida TOCTOU sobre SKU vira mensagem de negócio (23505 traduzido)")]
    public async Task Produto_CorridaTOCTOU_SkuTraduzido()
    {
        await SementePronta;
        var sku = Prefixo + "-SKU-TOC";

        var idA = Guid.NewGuid();
        await using var cnA = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cnA.OpenAsync();
        await using var txA = await cnA.BeginTransactionAsync();
        await cnA.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_produtos (id, tenant_id, sku, nome, unidade, controla_lote, exige_inspecao, preco_custo, ativo)
            VALUES (@id, @tenantId, @sku, @nome, 'UN', false, false, 10.00, true)",
            new { id = idA, tenantId = TenantA, sku, nome = Prefixo + " CORRIDA A" }, txA);

        try
        {
            var tarefa = Task.Run(() => _cadastros.SalvarProdutoAsync(
                TenantA, null, sku, Prefixo + " CORRIDA B", "UN", null, false, false, 10.00m, true, CancellationToken.None));
            await Task.Delay(TimeSpan.FromSeconds(2));
            await txA.CommitAsync();

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(async () => await tarefa);
            Assert.Contains("Já existe um produto com este SKU neste cliente.", ex.Message);
        }
        finally
        {
            await txA.DisposeAsync();
            await using var cnC = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cnC.OpenAsync();
            await cnC.ExecuteAsync("delete from plantaopro.adm360_produtos where id = @id", new { id = idA });
        }
    }

    [Fact(DisplayName = "Local: corrida TOCTOU sobre código vira mensagem de negócio (23505 traduzido)")]
    public async Task Local_CorridaTOCTOU_CodigoTraduzido()
    {
        await SementePronta;
        var codigo = Prefixo + "-LTC";

        var idA = Guid.NewGuid();
        await using var cnA = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cnA.OpenAsync();
        await using var txA = await cnA.BeginTransactionAsync();
        await cnA.ExecuteAsync(@"
            INSERT INTO plantaopro.adm360_locais (id, tenant_id, codigo, nome, tipo, ativo)
            VALUES (@id, @tenantId, @codigo, @nome, 'INTERNO', true)",
            new { id = idA, tenantId = TenantA, codigo, nome = Prefixo + " CORRIDA A" }, txA);

        try
        {
            var tarefa = Task.Run(() => _cadastros.SalvarLocalAsync(
                TenantA, null, codigo, Prefixo + " CORRIDA B", "INTERNO", true, CancellationToken.None));
            await Task.Delay(TimeSpan.FromSeconds(2));
            await txA.CommitAsync();

            var ex = await Assert.ThrowsAsync<Administrativo360BusinessException>(async () => await tarefa);
            Assert.Contains("Já existe um local de estoque com este código neste cliente.", ex.Message);
        }
        finally
        {
            await txA.DisposeAsync();
            await using var cnC = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cnC.OpenAsync();
            await cnC.ExecuteAsync("delete from plantaopro.adm360_locais where id = @id", new { id = idA });
        }
    }

    // =================================================================
    // SEMENTE (idempotente, atômica: um único comando multi-statement)
    // =================================================================
    // Tenant A recebe 105 produtos ativos (paginação/teto), 4 parceiros
    // (1 inativo + papéis explícitos) e 3 locais (1 inativo). Tenant B fica vazio
    // para os testes de isolamento entre tenants.

    private static async Task SementarAsync()
    {
        var sb = new System.Text.StringBuilder();
        // WP-A4: limpeza completa e ordenada (segura para FK) de TODAS as tabelas adm360 do
        // escopo dos tenants — elimina o flake 23503 causado por estado residual de execuções
        // anteriores (ex.: orcamentos referenciando parceiros). Ver Adm360TenantCleanup.
        sb.Append(Adm360TenantCleanup.SqlDelete(TenantA, TenantB));
        sb.Append(@"
            insert into plantaopro.tenants (id, nome) values
             ('d42a0f30-0042-4000-8000-000000000f30', 'A360 Seletores Tenanted A'),
             ('d42a0f31-0042-4000-8000-000000000f31', 'A360 Seletores Tenanted B');

            insert into plantaopro.adm360_parceiros (id, tenant_id, nome, documento, fornecedor, ativo, eh_hospital, eh_pagador, eh_cliente) values
             (gen_random_uuid(), 'd42a0f30-0042-4000-8000-000000000f30', '{P}-FORN ALFA', '{P}-P01', true, true, false, false, false),
             (gen_random_uuid(), 'd42a0f30-0042-4000-8000-000000000f30', '{P}-HOSP BETA', '{P}-P02', false, true, true, false, false),
             (gen_random_uuid(), 'd42a0f30-0042-4000-8000-000000000f30', '{P}-PAG GAMA', null, false, true, false, true, false),
             (gen_random_uuid(), 'd42a0f30-0042-4000-8000-000000000f30', '{P}-LEGADO INATIVO', null, false, false, false, false, false);

            insert into plantaopro.adm360_locais (id, tenant_id, codigo, nome, tipo, ativo) values
             (gen_random_uuid(), 'd42a0f30-0042-4000-8000-000000000f30', '{P}-LOCA', '{P}-DEPOSITO A', 'INTERNO', true),
             (gen_random_uuid(), 'd42a0f30-0042-4000-8000-000000000f30', '{P}-LOCB', '{P}-DEPOSITO B', 'EXTERNO', true),
             (gen_random_uuid(), 'd42a0f30-0042-4000-8000-000000000f30', '{P}-LOCC', '{P}-DEPOSITO C INATIVO', 'EXTERNO', false);

            ".Replace("{P}", Prefixo));

        sb.Append("insert into plantaopro.adm360_produtos (id, tenant_id, sku, nome, unidade, controla_lote, exige_inspecao, ativo, preco_custo) values\n");
        for (var i = 1; i <= 105; i++)
        {
            sb.Append($" (gen_random_uuid(), 'd42a0f30-0042-4000-8000-000000000f30', '{Prefixo}-SKU-{i:D3}', '{Prefixo} ITEM {i:D3}', 'UN', false, false, true, 10.00)");
            sb.Append(i == 105 ? ";" : ",\n");
        }

        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(sb.ToString());
    }
}
