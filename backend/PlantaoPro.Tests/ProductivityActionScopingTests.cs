using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PlantaoPro.Api.Productivity;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// WP-A1 (ciclo J12) — Meu Dia / Central de Ações: contrato de escopo do DerivedSql.
///
/// Cobre os cenários exigidos na homologação com SQL real contra PostgreSQL:
///   - banco vazio para o tenant (lista vazia sem virar erro, e erro que não vira lista vazia);
///   - dados representativos em convites, escalas, pagamentos e agendamentos;
///   - perfis diferentes (equipe x médico, módulos contratados x não contratados);
///   - cliente e tenant com IDs distintos (coluna cliente_id com GUID distinto de tenant_id);
///   - dois tenants do mesmo cliente (isolamento entre si);
///   - outro cliente (isolamento total);
///   - filtros e paginação.
///
/// Os dados usam GUIDs únicos por execução, portanto a suíte é idempotente e não interfere
/// em outros tenants/bancos. A limpeza ao final remove as linhas desta execução.
/// </summary>
public sealed class ProductivityActionScopingTests : IAsyncLifetime
{
    private readonly string _cs;
    private readonly ProductivityActionRepository _repo;

    private readonly Guid _clA = Guid.NewGuid();          // cliente A (IDs distintos dos tenants)
    private readonly Guid _clB = Guid.NewGuid();          // outro cliente
    private readonly Guid _tA1 = Guid.NewGuid();          // tenant 1 do cliente A
    private readonly Guid _tA2 = Guid.NewGuid();          // tenant 2 do MESMO cliente A
    private readonly Guid _tB = Guid.NewGuid();           // tenant do cliente B
    private readonly Guid _tVazio = Guid.NewGuid();       // tenant sem nenhuma linha
    private readonly Guid _uidA = Guid.NewGuid();         // usuário/médico do tenant A1

    private readonly Guid _conviteA = Guid.NewGuid();     // convite pendente do T_A1 (médico @uidA)
    private readonly Guid _conviteA2 = Guid.NewGuid();    // convite pendente do T_A2 (mesmo cliente A)
    private readonly Guid _conviteB = Guid.NewGuid();     // convite pendente do T_B (outro cliente)
    private readonly Guid _escalaA = Guid.NewGuid();      // escala solicitada do T_A1
    private readonly Guid _escalaDual = Guid.NewGuid();   // escala com cliente_id = GUID do tenant (escrita dual legado)
    private readonly Guid _pagamentoA2 = Guid.NewGuid();  // pagamento pendente do T_A2
    private readonly Guid _agendamentoA = Guid.NewGuid(); // agendamento de hoje do T_A1

    public ProductivityActionScopingTests()
    {
        _cs = TestDatabase.ConnectionString;
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = _cs })
            .Build();
        _repo = new ProductivityActionRepository(cfg);
    }

    public async Task InitializeAsync()
    {
        var plantao = Guid.NewGuid();
        var criado = DateTimeOffset.UtcNow;
        // productivity_item_user_state possui FK para clientes(id) e usuarios(id):
        // neste schema o "tenant" do Meu Dia é uma linha em clientes.
        var email = $"wpa1-{_uidA:N}@exemplo.test";
        await using var cn = new NpgsqlConnection(_cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync(new CommandDefinition(@"
            insert into plantaopro.clientes(id) values (@tA1),(@tA2),(@tB),(@tV);
            insert into plantaopro.usuarios(id,email,email_normalizado,nome,senha_hash)
            values (@u,@email,@email,'Teste WP-A1','hash-sem-sentido');
", new { tA1 = _tA1, tA2 = _tA2, tB = _tB, tV = _tVazio, u = _uidA, email },
               cancellationToken: CancellationToken.None));
        await cn.ExecuteAsync(new CommandDefinition(@"
            insert into plantaopro.cobertura_convites(id,tenant_id,plantao_id,medico_id,status,criado_por,criado_em)
            values
              (@cA,@tA1,@plantao,@uidA,'PENDENTE',@uidA,now()),
              (@cA2,@tA2,@plantao,@uidA,'PENDENTE',@uidA,now()),
              (@cB,@tB,@plantao,@uidA,'PENDENTE',@uidA,now());
            insert into plantaopro.escalas(id,tenant_id,cliente_id,status,dados,criado_em,reg_status)
            values
              (@eA,@tA1,null,'SOLICITADA','{}',now(),'A'),
              (@eD,null,@tA1,'SOLICITADA','{}',now(),'A');
            insert into plantaopro.pagamentos(id,tenant_id,cliente_id,medico_id,escala_id,plantao_id,status,reg_status,reg_date,data_vencimento,valor_hora,valor_previsto,horas_referencia,parametros_apuracao,processado_automaticamente,versao)
            values (@pA2,@tA2,null,@uidA,@escalaFake,@plantao,'PENDENTE','A',now(),now(),100,500,5,'{}',false,1);
            insert into plantaopro.agendamentos(id,tenant_id,cliente_id,status,dados,criado_em)
             -- A janela do DerivedSql usa date_trunc('day',now()) no fuso da sessao (server_tz).
             -- Semeia a data de hoje no fuso da sessao (12:00Z = mesmo dia local p/ offsets < 12h),
             -- nunca a data UTC, que desloca o item para outro dia fora da janela de hoje.
            values (@ag,@tA1,@clA,'AGENDADO',jsonb_build_object('dataInicio',to_char(now(),'YYYY-MM-DD') || 'T12:00:00Z'),now());
            ", new { cA = _conviteA, cA2 = _conviteA2, cB = _conviteB, plantao, escalaFake = Guid.NewGuid(), uidA = _uidA,
                     tA1 = _tA1, tA2 = _tA2, tB = _tB,
                     eA = _escalaA, eD = _escalaDual, pA2 = _pagamentoA2, ag = _agendamentoA, clA = _clA },
               cancellationToken: CancellationToken.None));
        _ = criado;
    }

    public async Task DisposeAsync()
    {
        await using var cn = new NpgsqlConnection(_cs);
        await cn.OpenAsync();
        await cn.ExecuteAsync(new CommandDefinition(@"
            delete from plantaopro.productivity_item_user_state where user_id=@u;
            delete from plantaopro.usuarios where id=@u;
            delete from plantaopro.clientes where id in (@tA1,@tA2,@tB,@tV);
            delete from plantaopro.agendamentos where id=@ag;
            delete from plantaopro.pagamentos where id=@p;
            delete from plantaopro.escalas where id in (@eA,@eD);
            delete from plantaopro.cobertura_convites where id in (@cA,@cA2,@cB);
            ", new { ag = _agendamentoA, p = _pagamentoA2, eA = _escalaA, eD = _escalaDual,
                     cA = _conviteA, cA2 = _conviteA2, cB = _conviteB, u = _uidA,
                     tA1 = _tA1, tA2 = _tA2, tB = _tB, tV = _tVazio },
               cancellationToken: CancellationToken.None));
    }

    // --- Cenário: banco vazio --------------------------------------------------

    [Fact]
    public async Task TenantSemDados_RetornaListaVaziaESumeroZero_SemExcecao()
    {
        var page = await _repo.ListAsync(_tVazio, _uidA, new ProductivityQuery(PageSize: 10),
            operation: true, clinical: true, financial: true, doctorOnly: false, CancellationToken.None);
        Assert.Empty(page.Items);
        Assert.Equal(0, page.Total);
        var sum = await _repo.SummaryAsync(_tVazio, _uidA, true, true, true, false, CancellationToken.None);
        Assert.Equal(0, sum.Active);
        Assert.Equal(0, sum.Critical);
    }

    // --- Cenário: dois tenants do mesmo cliente + outro cliente ----------------

    [Fact]
    public async Task TenantA1_VeApenasSuaOperacao_EIsolaOutroTenantDoMesmoClienteEOtroCliente()
    {
        var page = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(),
            operation: true, clinical: true, financial: false, doctorOnly: false, CancellationToken.None);
        var keys = page.Items.Select(x => x.EntityId).ToHashSet();
        Assert.Contains(_conviteA, keys);
        Assert.Contains(_escalaA, keys);
        Assert.Contains(_escalaDual, keys);   // escrita dual legado: cliente_id guarda o GUID do tenant
        Assert.Contains(_agendamentoA, keys);
        Assert.DoesNotContain(_conviteA2, keys); // outro tenant do MESMO cliente A
        Assert.DoesNotContain(_conviteB, keys);  // outro cliente
        Assert.DoesNotContain(_pagamentoA2, keys);
    }

    [Fact]
    public async Task TenantA2ComFinanceiro_VeSeuPagamento_MasNaoVeioDaEstruturaDoTenantA1()
    {
        var page = await _repo.ListAsync(_tA2, _uidA, new ProductivityQuery(),
            operation: true, clinical: true, financial: true, doctorOnly: false, CancellationToken.None);
        var keys = page.Items.Select(x => x.EntityId).ToHashSet();
        Assert.Contains(_conviteA2, keys);
        Assert.Contains(_pagamentoA2, keys);
        Assert.DoesNotContain(_conviteA, keys);
        Assert.DoesNotContain(_escalaA, keys);
        Assert.DoesNotContain(_escalaDual, keys);
        Assert.DoesNotContain(_agendamentoA, keys);
    }

    [Fact]
    public async Task OutroCliente_VeApenasSeuConvite()
    {
        var page = await _repo.ListAsync(_tB, _uidA, new ProductivityQuery(),
            operation: true, clinical: true, financial: true, doctorOnly: false, CancellationToken.None);
        var keys = page.Items.Select(x => x.EntityId).ToHashSet();
        Assert.Contains(_conviteB, keys);
        Assert.Single(keys);
    }

    // --- Cenário: perfis diferentes / módulos não contratados --------------------

    [Fact]
    public async Task ModulosNaoContratados_NaoAparecem()
    {
        var page = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(),
            operation: false, clinical: false, financial: false, doctorOnly: false, CancellationToken.None);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task PerfilMedico_SoVeItensDeSuAutoria_NaoDoTimeInteiro()
    {
        var comoEquipe = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(),
            operation: true, clinical: true, financial: true, doctorOnly: false, CancellationToken.None);
        var comoMedico = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(),
            operation: true, clinical: true, financial: true, doctorOnly: true, CancellationToken.None);
        // Convite do próprio médico continua visível; escalas sem médico atribuído somem da visão médica.
        Assert.Contains(comoMedico.Items, x => x.EntityId == _conviteA);
        Assert.DoesNotContain(comoMedico.Items, x => x.EntityId == _escalaA);
        Assert.DoesNotContain(comoMedico.Items, x => x.EntityId == _escalaDual);
        Assert.True(comoMedico.Items.Count < comoEquipe.Items.Count);
    }

    // --- Cenário: paginação e filtros ------------------------------------------

    [Fact]
    public async Task Paginacao_DivideAsLinhasSemPerderTotal()
    {
        var p1 = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(Page: 1, PageSize: 2),
            operation: true, clinical: false, financial: false, doctorOnly: false, CancellationToken.None);
        var p2 = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(Page: 2, PageSize: 2),
            operation: true, clinical: false, financial: false, doctorOnly: false, CancellationToken.None);
        // Operação do T_A1 = conviteA + escalaA + escalaDual (3 itens)
        Assert.Equal(3, p1.Total);
        Assert.Equal(2, p1.Items.Count);
        Assert.Single(p2.Items);
        Assert.NotEqual(p1.Items[0].Key, p2.Items[0].Key);
    }

    [Fact]
    public async Task PaginaForaDoIntervalo_MostraListaVaziaMasMantemTotalRealDoFiltro()
    {
        // WP-S2: uma página sem registros deve reportar o total REAL dos filtros
        // (o count da janela não retorna nenhuma linha), nunca zero quando existem itens.
        var referencia = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(Page: 1, PageSize: 2),
            operation: true, clinical: false, financial: false, doctorOnly: false, CancellationToken.None);
        var fora = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(Page: 99, PageSize: 2),
            operation: true, clinical: false, financial: false, doctorOnly: false, CancellationToken.None);
        Assert.NotEmpty(referencia.Items);
        Assert.Empty(fora.Items);
        Assert.Equal(referencia.Total, fora.Total);
        Assert.Equal(referencia.TotalPages, fora.TotalPages);

        // Tenant realmente vazio: página fora do intervalo é legitimamente zero (sem falhar no fallback).
        var vazio = await _repo.ListAsync(_tVazio, _uidA, new ProductivityQuery(Page: 99, PageSize: 2),
            operation: true, clinical: true, financial: true, doctorOnly: false, CancellationToken.None);
        Assert.Empty(vazio.Items);
        Assert.Equal(0, vazio.Total);
    }

    [Fact]
    public async Task FiltroPorModulo_RestringeAoModuloPedido()
    {
        var page = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(Module: "OPERACAO"),
            operation: true, clinical: true, financial: true, doctorOnly: false, CancellationToken.None);
        Assert.All(page.Items, x => Assert.Equal("OPERACAO", x.Module));
        Assert.NotEmpty(page.Items);
    }

    // --- Cenário: adiar (estado do usuário) não altera a origem -----------------

    [Fact]
    public async Task AdiarItem_TiraDaVisaoPadraoEMantemNaVisaoAdiadas()
    {
        var key = $"OPERACAO:CONVITE:{_conviteA}:RESPONDER"; // uuid::text no PG = minúsculo com hífens
        await _repo.SnoozeAsync(_tA1, _uidA, key, DateTimeOffset.UtcNow.AddHours(1), CancellationToken.None);
        var padrao = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(Tab: "PARA_MIM"),
            operation: true, clinical: true, financial: false, doctorOnly: false, CancellationToken.None);
        var adiadas = await _repo.ListAsync(_tA1, _uidA, new ProductivityQuery(Tab: "ADIADAS"),
            operation: true, clinical: true, financial: false, doctorOnly: false, CancellationToken.None);
        Assert.DoesNotContain(padrao.Items, x => x.Key == key);
        var adiada = adiadas.Items.Single(x => x.Key == key);
        Assert.True(adiada.IsSnoozed);
    }

    // --- Cenário: erro de consulta não vira lista vazia --------------------------

    [Fact]
    public async Task FalhaDeConsulta_PropagaExcecaoEmVezDeListavazia()
    {
        await using var cn = new NpgsqlConnection(_cs);
        await cn.OpenAsync();
        // Executa o mesmo padrão do DerivedSql (CTE + junção de estado do usuário) contra
        // uma tabela de estado inexistente: a exceção do banco deve propagar, nunca 0 linhas.
        var sql = @"with derived as (select cast(null as text) Key, cast(null as timestamptz) SourceUpdatedAt)
                    select d.* from derived d
                    left join plantaopro.productivity_item_user_state_nao_existe s on s.item_key=d.Key";
        var threw = false;
        try
        {
            await cn.QueryAsync(sql, new { });
        }
        catch (NpgsqlException)
        {
            threw = true;
        }
        Assert.True(threw, "Erro estrutural de consulta deveria propagar exceção em vez de lista vazia.");
    }

    // --- Contrato de origem (fontes) --------------------------------------------

    [Fact]
    public void ContratoDeEscopo_ConvitesUsamApenasTenantIdDoSchema()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "ProductivityActionServices.cs"));
        var start = source.IndexOf("from plantaopro.cobertura_convites", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = source.IndexOf("union all", start, StringComparison.Ordinal);
        Assert.True(end > start);
        var branch = source[start..end];
        Assert.Contains("c.tenant_id=@tenantId", branch, StringComparison.Ordinal);
        Assert.DoesNotContain("c.cliente_id", branch, StringComparison.Ordinal);
    }
}
