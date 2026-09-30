using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Tests;

/// <summary>
/// Suíte dos filtros opcionais do Administrativo 360 (blocos 1 e 2 do plano de homologação).
///
/// Contexto: as listagens falhavam com 42P08 (tipo de operador indefinido) quando um parâmetro
/// nullable (DateOnly?, Guid?, bool?) chegava nulo ao PostgreSQL em predicados "@param IS NULL".
/// Correção: casts no lado do parâmetro ("@param::date", "@param::uuid", "@param::boolean"),
/// sobrescrita de TypeHandler&lt;DateOnly&gt;.GetDbType() e validação de período inválido como
/// mensagem de negócio (ArgumentException → HTTP 400).
///
/// A suíte executa os repositórios REAIS contra PostgreSQL real, com dados determinísticos em
/// tenants dedicados (d42a0f10/d42a0f11), cobrindo: tudo nulo, só início, só fim, ambos,
/// início=fim, período vazio, início&gt;fim, texto+situação+período, limites inclusivos,
/// parâmetros booleanos e de identificador opcionais e isolamento entre tenants.
/// </summary>
public sealed class Administrativo360FiltrosOpcionaisTests
{
    // Tenants dedicados a esta suíte (nenhum outro teste toca esses dados).
    private static readonly Guid TenantA = Guid.Parse("d42a0f10-0042-4000-8000-000000000f10");
    private static readonly Guid TenantB = Guid.Parse("d42a0f11-0042-4000-8000-000000000f11");

    // Identidades fixas da semente (permitem asserções exatas por id/valor).
    private static readonly Guid FornecedorAlfa = Guid.Parse("d42a0f12-0042-4000-8000-000000000f12");
    private static readonly Guid HospitalBeta = Guid.Parse("d42a0f13-0042-4000-8000-000000000f13");
    private static readonly Guid LocalA = Guid.Parse("d42a0f14-0042-4000-8000-000000000f14");
    private static readonly Guid ProdutoGamma = Guid.Parse("d42a0f16-0042-4000-8000-000000000f16");
    private static readonly Guid LoteUnico = Guid.Parse("d42a0f18-0042-4000-8000-000000000f18");
    private static readonly Guid ContaCaixa = Guid.Parse("d42a0f19-0042-4000-8000-000000000f19");
    private static readonly Guid OrcamentoFiltra = Guid.Parse("d42a0f2a-0042-4000-8000-000000000f2a");

    // Semente executada uma única vez por processo (idempotente: delete+insert atômico).
    private static readonly Task SementePronta = SementarAsync();

    private readonly ComprasRepository _compras = new(TestDatabase.ConnectionString);
    private readonly CirurgiaRepository _cirurgia = new(TestDatabase.ConnectionString);
    private readonly ValeConsignacaoRepository _vales = new(TestDatabase.ConnectionString);
    private readonly CaixaRepository _caixa = new(TestDatabase.ConnectionString);
    private readonly EstoqueRepository _estoque = new(TestDatabase.ConnectionString);
    private readonly CadastrosRepository _cadastros = new(TestDatabase.ConnectionString);
    private readonly DocumentosXmlRepository _documentos = new(TestDatabase.ConnectionString);
    private readonly OrcamentoCirurgicoRepository _orcamento = new(TestDatabase.ConnectionString);
    private readonly CotacoesRepository _cotacoes = new(TestDatabase.ConnectionString);
    private readonly Adm360RelatoriosRepository _relatorios = new(TestDatabase.ConnectionString);
    private readonly InventarioRepository _inventario = new(TestDatabase.ConnectionString);

    // =================================================================
    // COMPRAS (pedidos) — created_at: 2026-01-10 / 2026-02-15 / 2026-03-20
    // =================================================================

    [Fact(DisplayName = "Compras: sem nenhum filtro retorna todos os pedidos (reprodução original do 42P08)")]
    public async Task Compras_SemNenhumFiltro_RetornaTodosSemExcecao()
    {
        await SementePronta;
        var lista = await _compras.ListarAsync(TenantA, null, null, null, null, CancellationToken.None);
        Assert.Equal(3, lista.Count);
        Assert.Contains(lista, p => p.Numero == "FILT-PD-01");
        Assert.Contains(lista, p => p.Numero == "FILT-PD-02");
        Assert.Contains(lista, p => p.Numero == "FILT-PD-03");
        Assert.All(lista, p => Assert.Equal("FILT-ALFA FORNECEDOR LTDA", p.Fornecedor));
    }

    [Fact(DisplayName = "Compras: filtro só-início é inclusivo na data-limite")]
    public async Task Compras_FiltroSomenteInicio_DataInclusiva()
    {
        await SementePronta;
        var dentro = await _compras.ListarAsync(TenantA, null, null, new DateOnly(2026, 2, 1), null, CancellationToken.None);
        Assert.Equal(2, dentro.Count);
        Assert.DoesNotContain(dentro, p => p.Numero == "FILT-PD-01");

        var limite = await _compras.ListarAsync(TenantA, null, null, new DateOnly(2026, 1, 10), null, CancellationToken.None);
        Assert.Equal(3, limite.Count); // início no dia de um registro: inclusivo

        var excluiPrimeiro = await _compras.ListarAsync(TenantA, null, null, new DateOnly(2026, 1, 11), null, CancellationToken.None);
        Assert.Equal(2, excluiPrimeiro.Count);
    }

    [Fact(DisplayName = "Compras: filtro só-fim, janela completa, início=fim e período vazio")]
    public async Task Compras_FiltroSomenteFimEJanelas()
    {
        await SementePronta;
        var ateMar = await _compras.ListarAsync(TenantA, null, null, null, new DateOnly(2026, 3, 1), CancellationToken.None);
        Assert.Equal(2, ateMar.Count);
        Assert.DoesNotContain(ateMar, p => p.Numero == "FILT-PD-03");

        var excluiUltimo = await _compras.ListarAsync(TenantA, null, null, null, new DateOnly(2026, 3, 19), CancellationToken.None);
        Assert.Equal(2, excluiUltimo.Count);

        var janela = await _compras.ListarAsync(TenantA, null, null, new DateOnly(2026, 1, 10), new DateOnly(2026, 2, 15), CancellationToken.None);
        Assert.Equal(2, janela.Count);
        Assert.Contains(janela, p => p.Numero == "FILT-PD-01");
        Assert.Contains(janela, p => p.Numero == "FILT-PD-02");

        var diaUnico = await _compras.ListarAsync(TenantA, null, null, new DateOnly(2026, 2, 15), new DateOnly(2026, 2, 15), CancellationToken.None);
        var unico = Assert.Single(diaUnico);
        Assert.Equal("FILT-PD-02", unico.Numero);

        var vazio = await _compras.ListarAsync(TenantA, null, null, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31), CancellationToken.None);
        Assert.Empty(vazio); // período vazio: lista vazia, não exceção
    }

    [Fact(DisplayName = "Compras: início > fim gera mensagem de negócio (não 42P08 nem HTTP 500)")]
    public async Task Compras_PeriodoInvalido_GeraMensagemDeNegocio()
    {
        await SementePronta;
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _compras.ListarAsync(TenantA, null, null, new DateOnly(2026, 3, 1), new DateOnly(2026, 1, 1), CancellationToken.None));
        Assert.Contains("Período inválido", ex.Message);
    }

    [Fact(DisplayName = "Compras: busca textual e situação se combinam com período")]
    public async Task Compras_BuscaESituacaoEMaisPeriodo()
    {
        await SementePronta;
        var porFornecedor = await _compras.ListarAsync(TenantA, "ALFA", null, null, null, CancellationToken.None);
        Assert.Equal(3, porFornecedor.Count);

        var semCorrespondencia = await _compras.ListarAsync(TenantA, "ZETA-INEXISTENTE", null, null, null, CancellationToken.None);
        Assert.Empty(semCorrespondencia);

        var combinado = await _compras.ListarAsync(TenantA, "ALFA", "APROVADO", new DateOnly(2026, 2, 1), null, CancellationToken.None);
        Assert.Equal(2, combinado.Count);
    }

    // =================================================================
    // CIRURGIAS — data_prevista: 2026-01-05 / 2026-02-10 / 2026-03-15
    // =================================================================

    [Fact(DisplayName = "Cirurgias: matriz completa de filtros opcionais")]
    public async Task Cirurgias_MatrizDeFiltrosOpcionais()
    {
        await SementePronta;
        var todas = await _cirurgia.ListarAsync(TenantA, null, null, null, null, CancellationToken.None);
        Assert.Equal(3, todas.Count);

        var desdeFeve = await _cirurgia.ListarAsync(TenantA, null, null, new DateOnly(2026, 2, 1), null, CancellationToken.None);
        Assert.Equal(2, desdeFeve.Count);
        Assert.DoesNotContain(desdeFeve, c => c.Numero == "FILT-CIR-01");

        var excluiPrimeiroDia = await _cirurgia.ListarAsync(TenantA, null, null, new DateOnly(2026, 1, 6), null, CancellationToken.None);
        Assert.Equal(2, excluiPrimeiroDia.Count);

        var ateMar = await _cirurgia.ListarAsync(TenantA, null, null, null, new DateOnly(2026, 3, 1), CancellationToken.None);
        Assert.Equal(2, ateMar.Count);

        var janela = await _cirurgia.ListarAsync(TenantA, null, null, new DateOnly(2026, 1, 5), new DateOnly(2026, 2, 10), CancellationToken.None);
        Assert.Equal(2, janela.Count);

        var diaUnico = await _cirurgia.ListarAsync(TenantA, null, null, new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 10), CancellationToken.None);
        var unica = Assert.Single(diaUnico);
        Assert.Equal("FILT-CIR-02", unica.Numero);

        var vazio = await _cirurgia.ListarAsync(TenantA, null, null, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), CancellationToken.None);
        Assert.Empty(vazio);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _cirurgia.ListarAsync(TenantA, null, null, new DateOnly(2026, 3, 1), new DateOnly(2026, 1, 1), CancellationToken.None));
        Assert.Contains("Período inválido", ex.Message);

        var buscaComPeriodo = await _cirurgia.ListarAsync(TenantA, "COLESTECTOMIA", null, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), CancellationToken.None);
        var achada = Assert.Single(buscaComPeriodo);
        Assert.Equal("FILT-CIR-01", achada.Numero);
    }

    // =================================================================
    // VALES CONSIGNADOS — data_saida_prevista: 2026-02-20 / 2026-03-25
    // =================================================================

    [Fact(DisplayName = "Vales: matriz de filtros opcionais")]
    public async Task Vales_MatrizDeFiltrosOpcionais()
    {
        await SementePronta;
        var todos = await _vales.ListarAsync(TenantA, null, null, null, null, CancellationToken.None);
        Assert.Equal(2, todos.Count);

        var desdeMar = await _vales.ListarAsync(TenantA, null, null, new DateOnly(2026, 3, 1), null, CancellationToken.None);
        var unicoDesdeMar = Assert.Single(desdeMar);
        Assert.Equal("FILT-VALE-02", unicoDesdeMar.Numero);

        var ateFimFev = await _vales.ListarAsync(TenantA, null, null, null, new DateOnly(2026, 2, 28), CancellationToken.None);
        var unicoAteFev = Assert.Single(ateFimFev);
        Assert.Equal("FILT-VALE-01", unicoAteFev.Numero);

        var diaUnico = await _vales.ListarAsync(TenantA, null, null, new DateOnly(2026, 2, 20), new DateOnly(2026, 2, 20), CancellationToken.None);
        Assert.Single(diaUnico);

        var vazio = await _vales.ListarAsync(TenantA, null, null, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), CancellationToken.None);
        Assert.Empty(vazio);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _vales.ListarAsync(TenantA, null, null, new DateOnly(2026, 4, 1), new DateOnly(2026, 3, 1), CancellationToken.None));
        Assert.Contains("Período inválido", ex.Message);
    }

    // =================================================================
    // CAIXA — extrato da conta C1 (movimentos 2026-02-10 ENTRADA 100 /
    //        2026-03-15 SAÍDA 40; saldo inicial 0 em 2000-01-01)
    // =================================================================

    [Fact(DisplayName = "Caixa: extrato sem filtro, só-início e só-fim com saldos conferíveis")]
    public async Task Caixa_Extrato_SemFiltroEComPeriodo()
    {
        await SementePronta;
        var completo = await _caixa.ExtratoContaAsync(TenantA, ContaCaixa, null, null, CancellationToken.None);
        Assert.Equal(2, completo.Movimentos.Count);
        Assert.Equal(0m, completo.SaldoAbertura);
        Assert.Equal(100m, completo.TotalEntradas);
        Assert.Equal(40m, completo.TotalSaidas);
        Assert.Equal(60m, completo.SaldoFechamento);

        var desde11Fev = await _caixa.ExtratoContaAsync(TenantA, ContaCaixa, new DateOnly(2026, 2, 11), null, CancellationToken.None);
        var movUnico = Assert.Single(desde11Fev.Movimentos);
        Assert.Equal("SAIDA", movUnico.Tipo);
        Assert.Equal(100m, desde11Fev.SaldoAbertura); // saldo acumulado ANTES do período
        Assert.Equal(0m, desde11Fev.TotalEntradas);
        Assert.Equal(40m, desde11Fev.TotalSaidas);
        Assert.Equal(60m, desde11Fev.SaldoFechamento);

        var ate01Mar = await _caixa.ExtratoContaAsync(TenantA, ContaCaixa, null, new DateOnly(2026, 3, 1), CancellationToken.None);
        Assert.Equal(1, ate01Mar.Movimentos.Count);
        Assert.Equal(0m, ate01Mar.SaldoAbertura);
        Assert.Equal(100m, ate01Mar.TotalEntradas);
        Assert.Equal(0m, ate01Mar.TotalSaidas);
    }

    [Fact(DisplayName = "Caixa: extrato com período inválido e conta inexistente")]
    public async Task Caixa_Extrato_PeriodoInvalidoEContaInexistente()
    {
        await SementePronta;
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _caixa.ExtratoContaAsync(TenantA, ContaCaixa, new DateOnly(2026, 3, 1), new DateOnly(2026, 1, 1), CancellationToken.None));
        Assert.Contains("não pode ser maior", ex.Message);

        await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
            _caixa.ExtratoContaAsync(TenantA, Guid.NewGuid(), null, null, CancellationToken.None));
    }

    // =================================================================
    // ESTOQUE (view adm360_saldos) — LO1/LIBERADO 10 no Local A,
    //                                LO1/QUARENTENA 5 no Local B
    // =================================================================

    [Fact(DisplayName = "Estoque: parâmetros opcionais localId/condicao/busca")]
    public async Task Estoque_Consultar_ParametrosOpcionais()
    {
        await SementePronta;
        var todos = await _estoque.ConsultarAsync(TenantA, null, null, null, CancellationToken.None);
        Assert.Equal(2, todos.Count);

        var apenasLocalA = await _estoque.ConsultarAsync(TenantA, null, null, LocalA, CancellationToken.None);
        var linhaA = Assert.Single(apenasLocalA);
        Assert.Equal(LocalA, linhaA.LocalId);
        Assert.Equal("LIBERADO", linhaA.Condicao);
        Assert.Equal(10m, linhaA.Fisico);
        Assert.Equal(0m, linhaA.Reservado);
        Assert.Equal(10m, linhaA.Disponivel);

        var outroLocal = await _estoque.ConsultarAsync(TenantA, null, null, Guid.NewGuid(), CancellationToken.None);
        Assert.Empty(outroLocal);

        var porCondicao = await _estoque.ConsultarAsync(TenantA, null, "QUARENTENA", null, CancellationToken.None);
        var linhaQ = Assert.Single(porCondicao);
        Assert.Equal(5m, linhaQ.Fisico);
        Assert.Equal(0m, linhaQ.Disponivel); // quarentena não gera disponibilidade

        var porBusca = await _estoque.ConsultarAsync(TenantA, "GAMMA", null, null, CancellationToken.None);
        Assert.Equal(2, porBusca.Count);

        var buscaSemResultado = await _estoque.ConsultarAsync(TenantA, "ZETA-INEXISTENTE", null, null, CancellationToken.None);
        Assert.Empty(buscaSemResultado);
    }

    // =================================================================
    // CADASTROS — filtros bool? (fornecedor/apenasAtivos) e Guid? (produtoId)
    // =================================================================

    [Fact(DisplayName = "Cadastros: filtros booleanos e de identificador funcionam nulos e preenchidos")]
    public async Task Cadastros_FiltrosBooleanosEGuiDE_Opcionais()
    {
        await SementePronta;
        var todos = await _cadastros.ListarParceirosAsync(TenantA, null, null, CancellationToken.None);
        Assert.Equal(2, todos.Count);

        var fornecedores = await _cadastros.ListarParceirosAsync(TenantA, null, true, CancellationToken.None);
        var alfa = Assert.Single(fornecedores);
        Assert.Equal(FornecedorAlfa, alfa.Id);

        var naoFornecedores = await _cadastros.ListarParceirosAsync(TenantA, null, false, CancellationToken.None);
        var beta = Assert.Single(naoFornecedores);
        Assert.Equal(HospitalBeta, beta.Id);
        Assert.True(beta.EhHospital);

        var porBusca = await _cadastros.ListarParceirosAsync(TenantA, "HOSPITAL", null, CancellationToken.None);
        Assert.Single(porBusca);

        var produtosAtivos = await _cadastros.ListarProdutosAsync(TenantA, null, true, CancellationToken.None);
        var gamma = Assert.Single(produtosAtivos);
        Assert.Equal(ProdutoGamma, gamma.Id);

        var produtosTodos = await _cadastros.ListarProdutosAsync(TenantA, null, false, CancellationToken.None);
        Assert.Equal(2, produtosTodos.Count);

        var locaisAtivos = await _cadastros.ListarLocaisAsync(TenantA, null, true, CancellationToken.None);
        Assert.Equal(2, locaisAtivos.Count);

        var locaisInternos = await _cadastros.ListarLocaisAsync(TenantA, "INTERNO", true, CancellationToken.None);
        var interno = Assert.Single(locaisInternos);
        Assert.Equal(LocalA, interno.Id);

        var lotesSemFiltro = await _cadastros.ListarLotesAsync(TenantA, null, CancellationToken.None);
        Assert.Single(lotesSemFiltro);

        var lotesDoProduto = await _cadastros.ListarLotesAsync(TenantA, ProdutoGamma, CancellationToken.None);
        var lote = Assert.Single(lotesDoProduto);
        Assert.Equal(LoteUnico, lote.Id);

        var lotesOutroProduto = await _cadastros.ListarLotesAsync(TenantA, Guid.NewGuid(), CancellationToken.None);
        Assert.Empty(lotesOutroProduto);
    }

    // =================================================================
    // DOCUMENTOS XML — bool? quarentena (nulo / true / false) e status
    // =================================================================

    [Fact(DisplayName = "Documentos XML: filtro quarentena (bool?) e status opcionais")]
    public async Task DocumentosXml_FiltroQuarentena_BoolOpcional()
    {
        await SementePronta;
        var todos = await _documentos.ListarDocumentosAsync(TenantA);
        Assert.Equal(2, todos.Count);

        var emQuarentena = await _documentos.ListarDocumentosAsync(TenantA, null, true);
        var q = Assert.Single(emQuarentena);
        Assert.True(q.Quarentena);
        Assert.Equal("FILT-Divergencia de quantidade no lote", q.MotivoQuarentena);

        var liberados = await _documentos.ListarDocumentosAsync(TenantA, null, false);
        var l = Assert.Single(liberados);
        Assert.False(l.Quarentena);

        var conferidos = await _documentos.ListarDocumentosAsync(TenantA, "CONFERIDO");
        Assert.Single(conferidos);
    }

    // =================================================================
    // REPOSITÓRIOS COM PREDICADOS ROBUSTOS + RELATÓRIOS (sonda de não-crash)
    // =================================================================

    [Fact(DisplayName = "Orçamentos/cotações/relatórios: parâmetros nulos não lançam 42P08")]
    public async Task OrcamentoECotacoes_ListagensVazias_NaoFalhamComParametrosNulos()
    {
        await SementePronta;
        // Com orçamento na semente: também exercita o caminho de materialização com dados
        // (lista vazia não exercita o mapeamento de colunas).
        var orcamentos = await _orcamento.ListarAsync(TenantA, null, null, null, null, CancellationToken.None);
        var orcSeedeado = Assert.Single(orcamentos);
        Assert.Equal("FILT-ORC-01", orcSeedeado.Numero);

        // Histórico de revisões: ordem decrescente e conversão timestamptz→DateTimeOffset.
        var revisoes = await _orcamento.ObterRevisoesAsync(TenantA, OrcamentoFiltra, CancellationToken.None);
        Assert.Equal(2, revisoes.Count);
        Assert.Equal(2, revisoes[0].Revisao);
        Assert.Contains("revisao de valores", revisoes[0].Motivo);
        Assert.All(revisoes, r => Assert.NotEqual(default, r.CriadoEm));

        // Outro tenant não enxerga o orçamento nem as revisões.
        Assert.Empty(await _orcamento.ListarAsync(TenantB, null, null, null, null, CancellationToken.None));
        Assert.Empty(await _orcamento.ObterRevisoesAsync(TenantB, OrcamentoFiltra, CancellationToken.None));

        Assert.Empty(await _cotacoes.ListarMapeamentosAsync(TenantA));
        Assert.Empty(await _cotacoes.ListarCotacoesAsync(TenantA));
        Assert.Empty(await _cotacoes.ListarRespostasAsync(TenantA));

        // Rastreabilidade sem produto/lote informado: deve executar sem erro de tipo indefinido
        // e retornar os dois movimentos de estoque da semente (origem MANUAL, sem vínculo a vale).
        var rastreabilidade = await _relatorios.RastreabilidadeAsync(TenantA, null, CancellationToken.None);
        Assert.Equal(2, rastreabilidade.Count);
        Assert.All(rastreabilidade, r => Assert.Equal("MANUAL", r.OrigemTipo));
        Assert.All(rastreabilidade, r => Assert.Null(r.ValeNumero));
    }

    // =================================================================
    // ISOLAMENTO ENTRE TENANTS
    // =================================================================

    [Fact(DisplayName = "Isolamento: o tenant B não enxerga nenhum dado do tenant A")]
    public async Task IsolamentoEntreTenants_TenantB_NaoVejaDadosDoTenantA()
    {
        await SementePronta;
        Assert.Empty(await _compras.ListarAsync(TenantB, null, null, null, null, CancellationToken.None));
        Assert.Empty(await _cirurgia.ListarAsync(TenantB, null, null, null, null, CancellationToken.None));
        Assert.Empty(await _vales.ListarAsync(TenantB, null, null, null, null, CancellationToken.None));
        Assert.Empty(await _estoque.ConsultarAsync(TenantB, null, null, null, CancellationToken.None));
        Assert.Empty(await _cadastros.ListarParceirosAsync(TenantB, null, null, CancellationToken.None));
        Assert.Empty(await _cadastros.ListarProdutosAsync(TenantB, null, false, CancellationToken.None));
        Assert.Empty(await _cadastros.ListarLotesAsync(TenantB, null, CancellationToken.None));
        Assert.Empty(await _documentos.ListarDocumentosAsync(TenantB));

        // A conta do tenant A não é visível ao tenant B.
        await Assert.ThrowsAsync<Administrativo360BusinessException>(() =>
            _caixa.ExtratoContaAsync(TenantB, ContaCaixa, null, null, CancellationToken.None));
    }

    [Fact(DisplayName = "Inventários: listagem com linhas materializa corretamente (mesma causa do bug dos pedidos)")]
    public async Task Inventarios_ListarComLinhas_MaterializaCorretamente()
    {
        await SementePronta;
        var lista = await _inventario.ListarAsync(TenantA, CancellationToken.None);
        var unico = Assert.Single(lista);
        Assert.Equal("FILT-DEPOSITO A", unico.Local);
        Assert.Equal("FILT-LOCAL-COMPLETO", unico.Escopo);
        Assert.Equal("CONTAGEM", unico.Situacao);
        Assert.NotEqual(default, unico.CriadoEm);

        Assert.Empty(await _inventario.ListarAsync(TenantB, CancellationToken.None));
    }

    // =================================================================
    // SEMENTE (idempotente, atômica: um único comando multi-statement)
    // =================================================================

    private static async Task SementarAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(SqlSemente);
    }

    private const string SqlSemente = @"
delete from plantaopro.adm360_vale_itens where vale_id in (select id from plantaopro.adm360_vales where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11'));
delete from plantaopro.adm360_vales where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_documentos_recebidos where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_movimentos_financeiros where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_movimentos where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_reservas where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_pedido_itens where pedido_id in (select id from plantaopro.adm360_pedidos where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11'));
delete from plantaopro.adm360_pedidos where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_cirurgias where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_inventario_itens where inventario_id in (select id from plantaopro.adm360_inventarios where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11'));
delete from plantaopro.adm360_orcamento_revisoes where orcamento_id in (select id from plantaopro.adm360_orcamentos where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11'));
delete from plantaopro.adm360_orcamentos where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_inventarios where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_contas_financeiras where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_lotes where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_produtos where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_locais where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_parceiros where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.adm360_estabelecimentos where tenant_id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');
delete from plantaopro.tenants where id in ('d42a0f10-0042-4000-8000-000000000f10','d42a0f11-0042-4000-8000-000000000f11');

insert into plantaopro.tenants (id, nome) values
 ('d42a0f10-0042-4000-8000-000000000f10', 'A360 Filtros Tenanted A'),
 ('d42a0f11-0042-4000-8000-000000000f11', 'A360 Filtros Tenanted B');

insert into plantaopro.adm360_parceiros (id, tenant_id, nome, documento, fornecedor, ativo, eh_hospital) values
 ('d42a0f12-0042-4000-8000-000000000f12', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-ALFA FORNECEDOR LTDA', '12345678000190', true, true, false),
 ('d42a0f13-0042-4000-8000-000000000f13', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-BETA HOSPITAL', null, false, true, true);

insert into plantaopro.adm360_locais (id, tenant_id, codigo, nome, tipo, ativo) values
 ('d42a0f14-0042-4000-8000-000000000f14', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-LOC-A', 'FILT-DEPOSITO A', 'INTERNO', true),
 ('d42a0f15-0042-4000-8000-000000000f15', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-LOC-B', 'FILT-DEPOSITO B', 'EXTERNO', true);

insert into plantaopro.adm360_produtos (id, tenant_id, sku, nome, unidade, controla_lote, exige_inspecao, ativo, preco_custo) values
 ('d42a0f16-0042-4000-8000-000000000f16', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-SKU-GAMMA', 'FILT-PRODUTO GAMMA', 'UN', true, true, true, 10.00),
 ('d42a0f17-0042-4000-8000-000000000f17', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-SKU-DELTA', 'FILT-PRODUTO DELTA INATIVO', 'UN', false, false, false, 5.00);

insert into plantaopro.adm360_lotes (id, tenant_id, produto_id, codigo, fabricacao, validade) values
 ('d42a0f18-0042-4000-8000-000000000f18', 'd42a0f10-0042-4000-8000-000000000f10', 'd42a0f16-0042-4000-8000-000000000f16', 'FILT-LOTE-01', date '2026-01-15', date '2027-01-31');

insert into plantaopro.adm360_inventarios (id, tenant_id, local_id, situacao, escopo, created_by) values
 ('d42a0f29-0042-4000-8000-000000000f29', 'd42a0f10-0042-4000-8000-000000000f10', 'd42a0f14-0042-4000-8000-000000000f14', 'CONTAGEM', 'FILT-LOCAL-COMPLETO', 'd42a0f90-0042-4000-8000-000000000f90');

insert into plantaopro.adm360_orcamentos (id, tenant_id, numero, hospital_id, procedimento, responsavel_financeiro_id, data_prevista, validade) values
 ('d42a0f2a-0042-4000-8000-000000000f2a', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-ORC-01', 'd42a0f13-0042-4000-8000-000000000f13', 'FILT-ORCEDAMENTO TESTE', 'd42a0f13-0042-4000-8000-000000000f13', date '2026-04-10', date '2026-05-10');

insert into plantaopro.adm360_orcamento_revisoes (id, tenant_id, orcamento_id, revisao, motivo, snapshot_json) values
 ('d42a0f2b-0042-4000-8000-000000000f2b', 'd42a0f10-0042-4000-8000-000000000f10', 'd42a0f2a-0042-4000-8000-000000000f2a', 1, 'FILT-revisao inicial', '{""itens"":[]}'),
 ('d42a0f2c-0042-4000-8000-000000000f2c', 'd42a0f10-0042-4000-8000-000000000f10', 'd42a0f2a-0042-4000-8000-000000000f2a', 2, 'FILT-revisao de valores', '{""itens"":[]}');

insert into plantaopro.adm360_contas_financeiras (id, tenant_id, nome, tipo, saldo_inicial, ativo, data_saldo_inicial) values
 ('d42a0f19-0042-4000-8000-000000000f19', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-CONTA CAIXA DEMO', 'CAIXA', 0, true, date '2000-01-01');

insert into plantaopro.adm360_movimentos_financeiros (id, tenant_id, conta_id, tipo, valor, data_movimento, descricao, origem_tipo, origem_id) values
 ('d42a0f1a-0042-4000-8000-000000000f1a', 'd42a0f10-0042-4000-8000-000000000f10', 'd42a0f19-0042-4000-8000-000000000f19', 'ENTRADA', 100.00, date '2026-02-10', 'FILT-recebimento de Fevereiro', 'MANUAL', 'd42a0f90-0042-4000-8000-000000000f90'),
 ('d42a0f1b-0042-4000-8000-000000000f1b', 'd42a0f10-0042-4000-8000-000000000f10', 'd42a0f19-0042-4000-8000-000000000f19', 'SAIDA', 40.00, date '2026-03-15', 'FILT-pagamento de Marco', 'MANUAL', 'd42a0f90-0042-4000-8000-000000000f90');

insert into plantaopro.adm360_movimentos (id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key) values
 ('d42a0f27-0042-4000-8000-000000000f27', 'd42a0f10-0042-4000-8000-000000000f10', 'd42a0f16-0042-4000-8000-000000000f16', 'd42a0f18-0042-4000-8000-000000000f18', 'd42a0f14-0042-4000-8000-000000000f14', 'ENTRADA', 'LIBERADO', 10, 'Semente da suite de filtros', 'MANUAL', 'd42a0f90-0042-4000-8000-000000000f90', 'FILT-MOV-ESTOQUE-01'),
 ('d42a0f28-0042-4000-8000-000000000f28', 'd42a0f10-0042-4000-8000-000000000f10', 'd42a0f16-0042-4000-8000-000000000f16', 'd42a0f18-0042-4000-8000-000000000f18', 'd42a0f15-0042-4000-8000-000000000f15', 'ENTRADA', 'QUARENTENA', 5, 'Semente da suite de filtros', 'MANUAL', 'd42a0f90-0042-4000-8000-000000000f90', 'FILT-MOV-ESTOQUE-02');

insert into plantaopro.adm360_pedidos (id, tenant_id, numero, fornecedor_id, situacao, frete, created_at) values
 ('d42a0f1c-0042-4000-8000-000000000f1c', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-PD-01', 'd42a0f12-0042-4000-8000-000000000f12', 'APROVADO', 0, ('2026-01-10 12:00:00'::timestamp AT TIME ZONE current_setting('TimeZone'))),
 ('d42a0f1d-0042-4000-8000-000000000f1d', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-PD-02', 'd42a0f12-0042-4000-8000-000000000f12', 'APROVADO', 0, ('2026-02-15 12:00:00'::timestamp AT TIME ZONE current_setting('TimeZone'))),
 ('d42a0f1e-0042-4000-8000-000000000f1e', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-PD-03', 'd42a0f12-0042-4000-8000-000000000f12', 'APROVADO', 0, ('2026-03-20 12:00:00'::timestamp AT TIME ZONE current_setting('TimeZone')));

insert into plantaopro.adm360_cirurgias (id, tenant_id, numero, hospital_id, procedimento, data_prevista, local_destino_id) values
 ('d42a0f1f-0042-4000-8000-000000000f1f', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-CIR-01', 'd42a0f13-0042-4000-8000-000000000f13', 'FILT-COLESTECTOMIA', date '2026-01-05', 'd42a0f14-0042-4000-8000-000000000f14'),
 ('d42a0f20-0042-4000-8000-000000000f20', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-CIR-02', 'd42a0f13-0042-4000-8000-000000000f13', 'FILT-HISTERECTOMIA', date '2026-02-10', 'd42a0f14-0042-4000-8000-000000000f14'),
 ('d42a0f21-0042-4000-8000-000000000f21', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-CIR-03', 'd42a0f13-0042-4000-8000-000000000f13', 'FILT-APENDICECTOMIA', date '2026-03-15', 'd42a0f14-0042-4000-8000-000000000f14');

insert into plantaopro.adm360_vales (id, tenant_id, numero, hospital_id, local_origem_id, local_destino_id, data_saida_prevista) values
 ('d42a0f22-0042-4000-8000-000000000f22', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-VALE-01', 'd42a0f13-0042-4000-8000-000000000f13', 'd42a0f14-0042-4000-8000-000000000f14', 'd42a0f14-0042-4000-8000-000000000f14', date '2026-02-20'),
 ('d42a0f23-0042-4000-8000-000000000f23', 'd42a0f10-0042-4000-8000-000000000f10', 'FILT-VALE-02', 'd42a0f13-0042-4000-8000-000000000f13', 'd42a0f14-0042-4000-8000-000000000f14', 'd42a0f14-0042-4000-8000-000000000f14', date '2026-03-25');

insert into plantaopro.adm360_estabelecimentos (id, tenant_id, cnpj, razao_social) values
 ('d42a0f24-0042-4000-8000-000000000f24', 'd42a0f10-0042-4000-8000-000000000f10', '11222333000181', 'FILT-ESTABELECIMENTO GAMMA LTDA');

insert into plantaopro.adm360_documentos_recebidos (id, tenant_id, estabelecimento_id, chave_acesso, numero, serie, modelo, data_emissao, emitente_cnpj, emitente_nome, destinatario_cnpj, destinatario_nome, valor_total, valor_produtos, tipo_documento, status_manifestacao, status_conferencia, xml_conteudo, xml_hash, quarentena, motivo_quarentena, origem) values
 ('d42a0f25-0042-4000-8000-000000000f25', 'd42a0f10-0042-4000-8000-000000000f10', 'd42a0f24-0042-4000-8000-000000000f24', '99999999999999999999999999999999999999999901', '1001', '1', '55', '2026-02-01T10:00:00-03:00'::timestamptz, '99887766000199', 'FILT-EMITENTE ALFA', '11222333000181', 'FILT-ESTABELECIMENTO GAMMA LTDA', 150.00, 150.00, 'NFE_COMPLETA', 'SEM_MANIFESTACAO', 'CONFERIDO', '<nfeProc></nfeProc>', md5('FILT-D1'), false, null, 'IMPORTACAO_MANUAL'),
 ('d42a0f26-0042-4000-8000-000000000f26', 'd42a0f10-0042-4000-8000-000000000f10', 'd42a0f24-0042-4000-8000-000000000f24', '99999999999999999999999999999999999999999902', '1002', '1', '55', '2026-03-05T11:30:00-03:00'::timestamptz, '99887766000199', 'FILT-EMITENTE ALFA', '11222333000181', 'FILT-ESTABELECIMENTO GAMMA LTDA', 90.00, 90.00, 'NFE_COMPLETA', 'SEM_MANIFESTACAO', 'PENDENTE', '<nfeProc></nfeProc>', md5('FILT-D2'), true, 'FILT-Divergencia de quantidade no lote', 'IMPORTACAO_MANUAL');
";
}
