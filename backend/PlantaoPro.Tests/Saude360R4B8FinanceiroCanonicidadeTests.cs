using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Clinical;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R4-B8 Saúde 360 — Financiário canônico em clinica_* + pré-autorização.
///
/// Escopo homologável desta classe:
///   - baixa de recebimento (jornada paciente → conta → caixa) total e parcial,
///     com guarda de estado, excesso de valor, cancelada, outro tenant,
///     caixa fechado explícito, sem caixa aberto (baixa sem vínculo) e
///     corrida simultânea (lock FOR UPDATE + predicado do pendente esperado);
///   - estorno: reverte recebimento CONFIRMADO, conta (com floor em zero e
///     reabertura RECEBIDO → ABERTA/VENCIDA por vencimento), caixa somente se
///     aberta (fechada fica com aviso "concilie manualmente"), linha em
///     clinica_estornos + campos estorno_*, motivo obrigatório, estorno
///     duplicado 409, e parcelas múltiplas na mesma conta (índice único
///     legado derrubado na v2315);
///   - fechamento de caixa: saldo computado (sem contagem física), linha em
///     clinica_fechamentos_caixa com valor_informado/diferença, idempotente
///     para caixa já fechado;
///   - finalização de consulta: gate de pré-autorização SOMENTE para
///     faturamento por convênio/plano (PENDENTE e NEGADA bloqueiam com 409
///     sem gravar nada; APROVADA/libre segue; PARTICULAR/CORTESIA sem gate),
///     conta gerada com valores operacionais canônicos (valor_total/
///     valor_pendente = líquido) e baixável em seguida, e alerta de
///     pré-autorização exposto em PendenciasAsync.
///
/// Regras de concorrência/idempotência: transação única por operação, lock
/// de linha, predicados de estado esperado; perdedores saem com 409 e
/// rollback completo (nada é gravado em dobro).
/// </summary>
public sealed class Saude360R4B8FinanceiroCanonicidadeTests
{
    private sealed class B8FakeUser : ICurrentUserService
    {
        private readonly Guid? _tenant;
        public B8FakeUser(Guid? tenant) => _tenant = tenant;
        public Guid? UserId { get; } = Guid.NewGuid();
        public Guid? TenantId => null;
        public Guid? ClienteId => _tenant;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles { get; } = new[] { "TENANT_ADMIN", "SAUDE360_FINANCEIRO" };
        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => false;
        public bool IsTenantAdmin() => true;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => role == "TENANT_ADMIN";
    }

    private sealed class B8FakeAudit : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao, object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default) => Task.CompletedTask;
        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao, string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null) => Task.CompletedTask;
    }

    private static IConfiguration Cfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static Saude360FinanceiroService Financeiro(Guid? tenant) =>
        new(Cfg(), new B8FakeUser(tenant), new B8FakeAudit(), NullLogger<Saude360FinanceiroService>.Instance);

    private static ConsultaApplicationService Clinico(Guid? tenant) =>
        new(new ConsultaRepository(Cfg()), new B8FakeUser(tenant), new B8FakeAudit());

    private static Saude360CreateRequest Baixa(Guid contaId, decimal valor, Guid? caixaId = null) =>
        new() { ContaReceberId = contaId, Valor = valor, FormaPagamento = "DINHEIRO", DataPagamento = DateTime.UtcNow, CaixaId = caixaId };

    private static Saude360ActionRequest EstornoReq(Guid recId, string? motivo = "Pagamento em duplicidade identificado em conciliação.") =>
        new() { Id = recId, Motivo = motivo };

    private static FinalizarConsultaRequest Finalizar(TipoFaturamentoAssistencial tipo, decimal bruto = 0m, decimal desconto = 0m, decimal copart = 0m, string? justificativa = null) =>
        new(1, tipo, bruto, desconto, copart, justificativa);

    // ------------------------------------------------------------------
    // Seeds (tenant único por teste; cliente_id = tenant, tenant_id NULL —
    // convenção canônica dos escritores clínicos).
    // ------------------------------------------------------------------
    private static async Task SeedTenantAsync(NpgsqlConnection cn, Guid tenant) =>
        await cn.ExecuteAsync(new CommandDefinition(
            "insert into plantaopro.tenants(id,tenant_id,codigo,nome,status) values(@tenant,@tenant,'b8-x','Tenant B8','ATIVO') on conflict (id) do nothing", new { tenant }));

    private static async Task<Guid> SeedContaAsync(NpgsqlConnection cn, Guid tenant, decimal valor, int vencDias, string status = "ABERTA")
    {
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(new CommandDefinition(@"
            insert into plantaopro.clinica_contas_receber(id,cliente_id,paciente_id,descricao,valor_total,valor_pendente,vencimento,status,origem)
            values(@id,@tenant,@paciente,'Conta B8',@valor,@valor,CAST(@venc AS date),@status,'MANUAL')",
            new { id, tenant, paciente = Guid.NewGuid(), valor, venc = DateTime.Today.AddDays(vencDias), status }));
        return id;
    }

    private static async Task SeedCaixaAsync(NpgsqlConnection cn, Guid tenant, Guid caixaId, decimal saldoInicial, string status = "ABERTO") =>
        await cn.ExecuteAsync(new CommandDefinition(
            "insert into plantaopro.clinica_caixa(id,cliente_id,saldo_inicial,status) values(@caixaId,@tenant,@saldoInicial,@status)",
            new { caixaId, tenant, saldoInicial, status }));

    private static async Task<Guid> SeedAutorizacaoAsync(NpgsqlConnection cn, Guid tenant, Guid consultaId, Guid pacienteId, string status)
    {
        var id = Guid.NewGuid();
        await cn.ExecuteAsync(new CommandDefinition(@"
            insert into plantaopro.convenio_autorizacoes(id,cliente_id,convenio_id,paciente_id,consulta_id,procedimento,motivo,valor_autorizado,status)
            values(@id,@tenant,@convenio,@pacienteId,@consultaId,'P001','Pre-anestesia B8',100,@status)",
            new { id, tenant, convenio = Guid.NewGuid(), pacienteId, consultaId, status }));
        return id;
    }

    /// <summary>Cadeia mínima p/ FinalizarAsync: atendimento (unha única linha casando paciente+unidade) + consulta EM_ATENDIMENTO v1.</summary>
    private static async Task<(Guid ConsultaId, Guid PacienteId)> SeedConsultaAsync(NpgsqlConnection cn, Guid tenant)
    {
        var consultaId = Guid.NewGuid();
        var atendimentoId = Guid.NewGuid();
        var unidade = Guid.NewGuid();
        var agendamento = Guid.NewGuid();
        var paciente = Guid.NewGuid();
        var medico = Guid.NewGuid();
        await cn.ExecuteAsync(new CommandDefinition(@"
            insert into plantaopro.atendimentos_fila(id,cliente_id,unidade_id,agendamento_id,paciente_id,status)
            values(@atendimentoId,@tenant,@unidade,@agendamento,@paciente,'EM_ATENDIMENTO');
            insert into plantaopro.consultas(id,cliente_id,unidade_id,atendimento_id,paciente_id,medico_id,status,anamnese,conduta,versao)
            values(@consultaId,@tenant,@unidade,@atendimentoId,@paciente,@medico,'EM_ATENDIMENTO','Queixa principal B8.','Conduta B8.',1);",
            new { consultaId, atendimentoId, unidade, agendamento, paciente, medico, tenant }));
        return (consultaId, paciente);
    }

    private static async Task LimparAsync(NpgsqlConnection cn, Guid tenant)
    {
        await cn.ExecuteAsync(new CommandDefinition(@"
            delete from plantaopro.clinica_financeiro_historico where cliente_id=@tenant;
            delete from plantaopro.clinica_lancamentos where cliente_id=@tenant;
            delete from plantaopro.clinica_estornos where cliente_id=@tenant;
            delete from plantaopro.clinica_fechamentos_caixa where cliente_id=@tenant;
            delete from plantaopro.clinica_recebimentos where cliente_id=@tenant;
            delete from plantaopro.clinica_contas_receber where cliente_id=@tenant;
            delete from plantaopro.clinica_caixa where cliente_id=@tenant;
            delete from plantaopro.convenio_autorizacoes where cliente_id=@tenant;
            delete from plantaopro.consulta_historico where cliente_id=@tenant;
            delete from plantaopro.consultas where cliente_id=@tenant;
            delete from plantaopro.atendimentos_fila where cliente_id=@tenant;
            delete from plantaopro.tenants where id=@tenant;", new { tenant }));
    }

    private sealed record EstadoConta(string Status, decimal Pendente, decimal Pago);
    private static Task<EstadoConta> LerContaAsync(NpgsqlConnection cn, Guid contaId) =>
        cn.QuerySingleAsync<EstadoConta>(new CommandDefinition(
            "select status Status, coalesce(valor_pendente,0) Pendente, coalesce(valor_pago,0) Pago from plantaopro.clinica_contas_receber where id=@id",
            new { id = contaId }));

    // ------------------------------------------------------------------
    // Baixa de recebimento
    // ------------------------------------------------------------------
    [Fact]
    public async Task T01_BaixaParcial_AtualizaConta_Caixa_Lancamento_E_Historico()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var contaId = await SeedContaAsync(cn, tenant, 100m, +30);
            var caixaId = Guid.NewGuid();
            await SeedCaixaAsync(cn, tenant, caixaId, 100m);

            var r = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 50m), CancellationToken.None);

            Assert.True(r.Success);
            Assert.False(r.Data!.ContaQuitada);
            Assert.True(r.Data.CaixaVinculado);
            Assert.Equal(caixaId, r.Data.CaixaId);

            var conta = await LerContaAsync(cn, contaId);
            Assert.Equal("ABERTA", conta.Status);
            Assert.Equal(50m, conta.Pago);
            Assert.Equal(50m, conta.Pendente);

            var statusRec = await cn.ExecuteScalarAsync<string>(new CommandDefinition("select status from plantaopro.clinica_recebimentos where id=@id", new { id = r.Data.ReceivingId }));
            var caixaRec = await cn.ExecuteScalarAsync<Guid?>((new CommandDefinition("select caixa_id from plantaopro.clinica_recebimentos where id=@id", new { id = r.Data.ReceivingId })));
            Assert.Equal("CONFIRMADO", statusRec);
            Assert.Equal(caixaId, caixaRec);

            var caixa = await cn.QuerySingleAsync<(decimal Entradas, decimal Saldo)>(new CommandDefinition(
                "select total_entradas Entradas, saldo_final Saldo from plantaopro.clinica_caixa where id=@id", new { id = caixaId }));
            Assert.Equal(50m, caixa.Entradas);
            Assert.Equal(150m, caixa.Saldo);

            var lanc = await cn.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
                "select tipo from plantaopro.clinica_lancamentos where cliente_id=@tenant and caixa_id=@caixaId", new { tenant, caixaId }));
            Assert.Equal("ENTRADA", lanc);

            var hist = await cn.ExecuteScalarAsync<int>(new CommandDefinition(
                "select count(*)::int from plantaopro.clinica_financeiro_historico where cliente_id=@tenant and entidade='clinica_recebimentos' and acao='BAIXA_RECEBIMENTO' and entidade_id=@rec",
                new { tenant, rec = r.Data.ReceivingId }));
            Assert.Equal(1, hist);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    [Fact]
    public async Task T02_BaixaIntegral_Quita_E_SegundaBaixaDa409()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var contaId = await SeedContaAsync(cn, tenant, 80m, +30);

            var ok = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 80m), CancellationToken.None);
            Assert.True(ok.Success);
            Assert.True(ok.Data!.ContaQuitada);
            Assert.Equal("Recebimento baixado e conta quitada.", ok.Message);

            var conta = await LerContaAsync(cn, contaId);
            Assert.Equal("RECEBIDO", conta.Status);
            Assert.Equal(0m, conta.Pendente);
            Assert.Equal(80m, conta.Pago);

            var deNovo = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 10m), CancellationToken.None);
            Assert.False(deNovo.Success);
            Assert.Equal(409, deNovo.StatusCode);
            Assert.Contains("já está quitada", deNovo.Message);

            var quantos = await cn.ExecuteScalarAsync<int>(new CommandDefinition(
                "select count(*)::int from plantaopro.clinica_recebimentos where conta_receber_id=@id and status='CONFIRMADO'", new { id = contaId }));
            Assert.Equal(1, quantos);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    [Fact]
    public async Task T03_Baixa_ValidacoesDeEntrada_404_400_E_409()
    {
        var tenant = Guid.NewGuid();
        var outroTenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            await SeedTenantAsync(cn, outroTenant);
            var contaId = await SeedContaAsync(cn, tenant, 100m, +30);
            var contaOutra = await SeedContaAsync(cn, outroTenant, 100m, +30);
            var contaCancelada = await SeedContaAsync(cn, tenant, 40m, +30, "CANCELADA");

            var naoEncontrada = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(Guid.NewGuid(), 10m), CancellationToken.None);
            Assert.False(naoEncontrada.Success);
            Assert.Equal(404, naoEncontrada.StatusCode);

            var excedente = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 150m), CancellationToken.None);
            Assert.False(excedente.Success);
            Assert.Equal(400, excedente.StatusCode);
            Assert.Contains("excede o saldo pendente", excedente.Message);

            var campos = await Financeiro(tenant).BaixarRecebimentoAsync(new Saude360CreateRequest { ContaReceberId = contaId, Valor = 10m }, CancellationToken.None);
            Assert.False(campos.Success);
            Assert.Equal(400, campos.StatusCode);
            Assert.Contains("forma de pagamento e data de pagamento", campos.Message);

            var cancelada = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaCancelada, 10m), CancellationToken.None);
            Assert.False(cancelada.Success);
            Assert.Equal(409, cancelada.StatusCode);
            Assert.Contains("cancelada", cancelada.Message);

            var foraTenant = await Financeiro(outroTenant).BaixarRecebimentoAsync(Baixa(contaId, 10m), CancellationToken.None);
            Assert.False(foraTenant.Success);
            Assert.Equal(404, foraTenant.StatusCode);
            _ = contaOutra;

            var semOrganizacao = await Financeiro(null).BaixarRecebimentoAsync(Baixa(contaId, 10m), CancellationToken.None);
            Assert.False(semOrganizacao.Success);
            Assert.Equal(403, semOrganizacao.StatusCode);
        }
        finally
        {
            await LimparAsync(cn, tenant);
            await LimparAsync(cn, outroTenant);
        }
    }

    [Fact]
    public async Task T04_BaixaSemCaixaAberto_BaixaSemVinculacaoECaixaNaoMovida()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var contaId = await SeedContaAsync(cn, tenant, 100m, +30);

            var r = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 30m), CancellationToken.None);

            Assert.True(r.Success);
            Assert.False(r.Data!.CaixaVinculado);
            Assert.Null(r.Data.CaixaId);
            Assert.Contains("Nenhum caixa aberto", r.Data.Detalhes);

            var conta = await LerContaAsync(cn, contaId);
            Assert.Equal(70m, conta.Pendente);
            Assert.Equal("ABERTA", conta.Status);

            var caixaDoRec = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition("select caixa_id from plantaopro.clinica_recebimentos where id=@id", new { id = r.Data.ReceivingId }));
            Assert.Null(caixaDoRec);
            var lancs = await cn.ExecuteScalarAsync<int>(new CommandDefinition("select count(*)::int from plantaopro.clinica_lancamentos where cliente_id=@tenant", new { tenant }));
            Assert.Equal(0, lancs);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    [Fact]
    public async Task T05_CorridaDuasBaixasSimultaneas_ExatamenteUmaVence()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var contaId = await SeedContaAsync(cn, tenant, 50m, +30);
            var svc = Financeiro(tenant);

            var t1 = svc.BaixarRecebimentoAsync(Baixa(contaId, 50m), CancellationToken.None);
            var t2 = svc.BaixarRecebimentoAsync(Baixa(contaId, 50m), CancellationToken.None);
            var resultados = await Task.WhenAll(new[] { t1, t2 });
            var r1 = resultados[0];
            var r2 = resultados[1];

            Assert.Equal(1, new[] { r1, r2 }.Count(x => x.Success));
            var perdedor = r1.Success ? r2 : r1;
            Assert.False(perdedor.Success);
            Assert.Equal(409, perdedor.StatusCode);

            var conta = await LerContaAsync(cn, contaId);
            Assert.Equal("RECEBIDO", conta.Status);
            Assert.Equal(0m, conta.Pendente);
            var duplicatas = await cn.ExecuteScalarAsync<int>(new CommandDefinition(
                "select count(*)::int from plantaopro.clinica_recebimentos where conta_receber_id=@id and status='CONFIRMADO'", new { id = contaId }));
            Assert.Equal(1, duplicatas);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    // ------------------------------------------------------------------
    // Estorno
    // ------------------------------------------------------------------
    [Fact]
    public async Task T06_Estornar_ReverteContaECaixa_GravaEstorno_EstornoDuplicadoDa409()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var contaId = await SeedContaAsync(cn, tenant, 100m, +30);
            var caixaId = Guid.NewGuid();
            await SeedCaixaAsync(cn, tenant, caixaId, 100m);
            var ok = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 50m), CancellationToken.None);
            Assert.True(ok.Success);

            var r = await Financeiro(tenant).EstornarRecebimentoAsync(EstornoReq(ok.Data!.ReceivingId), CancellationToken.None);

            Assert.True(r.Success);
            Assert.True(r.Data!.ContaRevertida);
            Assert.True(r.Data.CaixaRevertida);

            var rec = await cn.QuerySingleAsync<(string Status, DateTime? Em, string? Motivo)>(new CommandDefinition(
                "select status Status, estornado_em Em, justificativa_estorno Motivo from plantaopro.clinica_recebimentos where id=@id", new { id = r.Data.ReceivingId }));
            Assert.Equal("ESTORNADO", rec.Status);
            Assert.NotNull(rec.Em);
            Assert.StartsWith("Pagamento em duplicidade", rec.Motivo);

            var estorno = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(
                "select valor from plantaopro.clinica_estornos where recebimento_id=@id", new { id = r.Data.ReceivingId }));
            Assert.Equal(50m, estorno);

            var conta = await LerContaAsync(cn, contaId);
            Assert.Equal("ABERTA", conta.Status);
            Assert.Equal(100m, conta.Pendente);
            Assert.Equal(0m, conta.Pago);

            var caixa = await cn.QuerySingleAsync<(decimal Entradas, decimal Saldo)>(new CommandDefinition(
                "select total_entradas Entradas, saldo_final Saldo from plantaopro.clinica_caixa where id=@id", new { id = caixaId }));
            Assert.Equal(0m, caixa.Entradas);
            Assert.Equal(100m, caixa.Saldo);

            var tipos = (await cn.QueryAsync<string>(new CommandDefinition(
                "select tipo from plantaopro.clinica_lancamentos where cliente_id=@tenant order by reg_date", new { tenant }))).ToList();
            Assert.Equal(new[] { "ENTRADA", "SAIDA" }, tipos.ToList());

            var deNovo = await Financeiro(tenant).EstornarRecebimentoAsync(EstornoReq(r.Data.ReceivingId), CancellationToken.None);
            Assert.False(deNovo.Success);
            Assert.Equal(409, deNovo.StatusCode);
            Assert.Contains("já foi estornado", deNovo.Message);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    [Fact]
    public async Task T07_EstornoSemMotivoDa400_E_ParcelsMultiplasNaMesmaContaFuncionam()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            // Índice único legado (cliente,conta WHERE CONFIRMADO) caiu na v2315:
            // a mesma conta aceita parcelas múltiplas confirmadas.
            var contaId = await SeedContaAsync(cn, tenant, 100m, +30);
            var p1 = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 40m), CancellationToken.None);
            var p2 = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 30m), CancellationToken.None);
            Assert.True(p1.Success && p2.Success);

            var semMotivo = await Financeiro(tenant).EstornarRecebimentoAsync(EstornoReq(p1.Data!.ReceivingId, null), CancellationToken.None);
            Assert.False(semMotivo.Success);
            Assert.Equal(400, semMotivo.StatusCode);
            Assert.Contains("motivo ou justificativa", semMotivo.Message);

            var ok = await Financeiro(tenant).EstornarRecebimentoAsync(EstornoReq(p1.Data.ReceivingId, "Pagamento devolvido por erro de digitação do operador."), CancellationToken.None);
            Assert.True(ok.Success);
            var conta = await LerContaAsync(cn, contaId);
            Assert.Equal("ABERTA", conta.Status);
            Assert.Equal(70m, conta.Pendente);
            Assert.Equal(30m, conta.Pago);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    [Fact]
    public async Task T08_EstornoDeQuitada_ReabreComoVencidaOUAbertaSegundoVencimento()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var vencida = await SeedContaAsync(cn, tenant, 60m, -10);
            var futura = await SeedContaAsync(cn, tenant, 60m, +10);

            var b1 = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(vencida, 60m), CancellationToken.None);
            var b2 = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(futura, 60m), CancellationToken.None);
            Assert.True(b1.Success && b2.Success);
            Assert.True(b1.Data!.ContaQuitada && b2.Data!.ContaQuitada);

            await Financeiro(tenant).EstornarRecebimentoAsync(EstornoReq(b1.Data.ReceivingId), CancellationToken.None);
            await Financeiro(tenant).EstornarRecebimentoAsync(EstornoReq(b2.Data.ReceivingId), CancellationToken.None);

            Assert.Equal("VENCIDA", (await LerContaAsync(cn, vencida)).Status);
            Assert.Equal("ABERTA", (await LerContaAsync(cn, futura)).Status);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    [Fact]
    public async Task T09_EstornoComCaixaFechado_NaoReabreECaixaComAvisoDeConciliacao()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var contaId = await SeedContaAsync(cn, tenant, 100m, +30);
            var caixaId = Guid.NewGuid();
            await SeedCaixaAsync(cn, tenant, caixaId, 100m);
            var ok = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 50m, caixaId), CancellationToken.None);
            Assert.True(ok.Success);
            var fechado = await Financeiro(tenant).FecharCaixaAsync(new Saude360ActionRequest { CaixaId = caixaId }, CancellationToken.None);
            Assert.True(fechado.Success);

            var explicitoFechado = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 10m, caixaId), CancellationToken.None);
            Assert.False(explicitoFechado.Success);
            Assert.Equal(409, explicitoFechado.StatusCode);
            Assert.Contains("não está aberto", explicitoFechado.Message);

            var r = await Financeiro(tenant).EstornarRecebimentoAsync(EstornoReq(ok.Data!.ReceivingId), CancellationToken.None);
            Assert.True(r.Success);
            Assert.True(r.Data!.ContaRevertida);
            Assert.False(r.Data.CaixaRevertida);
            Assert.Contains("concilie manualmente", r.Data.Detalhes);

            var caixa = await cn.QuerySingleAsync<(string Status, decimal Entradas)>(new CommandDefinition(
                "select status Status, total_entradas Entradas from plantaopro.clinica_caixa where id=@id", new { id = caixaId }));
            Assert.Equal("FECHADO", caixa.Status);
            Assert.Equal(50m, caixa.Entradas); // congelado no fechamento, não reaberto
        }
        finally { await LimparAsync(cn, tenant); }
    }

    // ------------------------------------------------------------------
    // Fechamento de caixa
    // ------------------------------------------------------------------
    [Fact]
    public async Task T10_FecharCaixa_CompensaSaldo_GravaFechamento_E_Idempotente()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var contaId = await SeedContaAsync(cn, tenant, 100m, +30);
            var caixaId = Guid.NewGuid();
            await SeedCaixaAsync(cn, tenant, caixaId, 100m);
            await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 50m), CancellationToken.None);

            var r = await Financeiro(tenant).FecharCaixaAsync(new Saude360ActionRequest { CaixaId = caixaId }, CancellationToken.None);

            Assert.True(r.Success);
            Assert.False(r.Data!.JaviaFechado);
            Assert.Equal(150m, r.Data.SaldoFinal);
            Assert.Contains("sem contagem física", r.Message);

            var caixa = await cn.QuerySingleAsync<(string Status, decimal Informado, decimal Diferenca, DateTime? FechadoEm)>(new CommandDefinition(
                "select status Status, coalesce(saldo_informado,0) Informado, coalesce(diferenca,0) Diferenca, fechado_em FechadoEm from plantaopro.clinica_caixa where id=@id", new { id = caixaId }));
            Assert.Equal("FECHADO", caixa.Status);
            Assert.Equal(150m, caixa.Informado);
            Assert.Equal(0m, caixa.Diferenca);
            Assert.NotNull(caixa.FechadoEm);

            var linha = await cn.QuerySingleAsync<(decimal Informado, decimal Diferenca, string Obs)>(new CommandDefinition(
                "select valor_informado Informado, diferenca Diferenca, observacoes Obs from plantaopro.clinica_fechamentos_caixa where cliente_id=@tenant and caixa_id=@id", new { tenant, id = caixaId }));
            Assert.Equal(150m, linha.Informado);
            Assert.Equal(0m, linha.Diferenca);
            Assert.Contains("sem contagem física", linha.Obs);

            var deNovo = await Financeiro(tenant).FecharCaixaAsync(new Saude360ActionRequest { CaixaId = caixaId }, CancellationToken.None);
            Assert.True(deNovo.Success);
            Assert.True(deNovo.Data!.JaviaFechado);
            Assert.Contains("já estava fechado", deNovo.Message);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    // ------------------------------------------------------------------
    // Finalização de consulta: gate de pré-autorização + conta canônica
    // ------------------------------------------------------------------
    [Fact]
    public async Task T11_FinalizarConvênioComAutorizacaoPendente_Bloqueia409SemGravar()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var (consultaId, paciente) = await SeedConsultaAsync(cn, tenant);
            await SeedAutorizacaoAsync(cn, tenant, consultaId, paciente, "PENDENTE");

            var r = await Clinico(tenant).FinalizarAsync(consultaId, Finalizar(TipoFaturamentoAssistencial.CONVENIO, 200m), CancellationToken.None);

            Assert.False(r.Success);
            Assert.Equal(409, r.StatusCode);
            Assert.Contains("Pré-autorização pendente", r.Message);
            Assert.Contains("aprovar/negar", r.Message);

            var c = await cn.QuerySingleAsync<(string Status, int Versao)>(new CommandDefinition(
                "select status Status, versao Versao from plantaopro.consultas where id=@id", new { id = consultaId }));
            Assert.Equal("EM_ATENDIMENTO", c.Status);
            Assert.Equal(1, c.Versao);

            var contas = await cn.ExecuteScalarAsync<int>(new CommandDefinition(
                "select count(*)::int from plantaopro.clinica_contas_receber where cliente_id=@tenant", new { tenant }));
            Assert.Equal(0, contas);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    [Fact]
    public async Task T12_FinalizarConvênioNegada_Bloqueia_AprovadaLiberaEGeraConta()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var (consultaId, paciente) = await SeedConsultaAsync(cn, tenant);
            var aut = await SeedAutorizacaoAsync(cn, tenant, consultaId, paciente, "NEGADA");

            var negada = await Clinico(tenant).FinalizarAsync(consultaId, Finalizar(TipoFaturamentoAssistencial.CONVENIO, 200m), CancellationToken.None);
            Assert.False(negada.Success);
            Assert.Equal(409, negada.StatusCode);
            Assert.Contains("Pré-autorização negada", negada.Message);

            await cn.ExecuteAsync(new CommandDefinition("update plantaopro.convenio_autorizacoes set status='APROVADA' where id=@id", new { id = aut }));

            var ok = await Clinico(tenant).FinalizarAsync(consultaId, Finalizar(TipoFaturamentoAssistencial.CONVENIO, 200m), CancellationToken.None);
            Assert.True(ok.Success);
            Assert.True(ok.Data!.PodeAbrirFaturamento);
            Assert.NotNull(ok.Data.FinanceiroId);

            var conta = await cn.QuerySingleAsync<(string Status, string Origem, decimal Total, decimal Pendente, decimal Liquido)>(new CommandDefinition(
                "select status Status, origem Origem, valor_total Total, valor_pendente Pendente, valor_liquido Liquido from plantaopro.clinica_contas_receber where consulta_id=@id", new { id = consultaId }));
            Assert.Equal("EM_ANALISE", conta.Status);
            Assert.Equal("CONSULTA", conta.Origem);
            Assert.Equal(200m, conta.Total);
            Assert.Equal(200m, conta.Pendente);
            Assert.Equal(200m, conta.Liquido);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    [Fact]
    public async Task T13_ConvênioSemAutorizacao_Finaliza_GeraContaUnificada_E_JornadaAtéCaixa()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var (consultaId, _) = await SeedConsultaAsync(cn, tenant);

            var r = await Clinico(tenant).FinalizarAsync(consultaId, Finalizar(TipoFaturamentoAssistencial.CONVENIO, 200m, desconto: 20m, copart: 5m), CancellationToken.None);
            Assert.True(r.Success);
            var contaId = r.Data!.FinanceiroId!.Value;

            var conta = await cn.QuerySingleAsync<(string Status, decimal Total, decimal Pendente)>(new CommandDefinition(
                "select status Status, valor_total Total, valor_pendente Pendente from plantaopro.clinica_contas_receber where id=@id", new { id = contaId }));
            Assert.Equal("EM_ANALISE", conta.Status);
            Assert.Equal(185m, conta.Total); // líquido = 200 - 20 + 5
            Assert.Equal(185m, conta.Pendente);

            var fila = await cn.ExecuteScalarAsync<int>(new CommandDefinition(
                "select count(*)::int from plantaopro.atendimentos_fila where cliente_id=@tenant and status='FINALIZADO'", new { tenant }));
            Assert.Equal(1, fila);

            // Jornada fim a fim: baixa integral da conta gerada pela consulta.
            var caixaId = Guid.NewGuid();
            await SeedCaixaAsync(cn, tenant, caixaId, 0m);
            var baixa = await Financeiro(tenant).BaixarRecebimentoAsync(Baixa(contaId, 185m), CancellationToken.None);
            Assert.True(baixa.Success);
            Assert.True(baixa.Data!.ContaQuitada);
            var final = await LerContaAsync(cn, contaId);
            Assert.Equal("RECEBIDO", final.Status);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    [Fact]
    public async Task T14_ParticularE_Cortesia_NaoPassamPorGateMesmoComAutorizacaoPendente()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var (particularId, pacA) = await SeedConsultaAsync(cn, tenant);
            await SeedAutorizacaoAsync(cn, tenant, particularId, pacA, "PENDENTE");

            var part = await Clinico(tenant).FinalizarAsync(particularId, Finalizar(TipoFaturamentoAssistencial.PARTICULAR, 150m), CancellationToken.None);
            Assert.True(part.Success);
            var contaPart = await cn.QuerySingleAsync<(string Status, decimal Total)>(new CommandDefinition(
                "select status Status, valor_total Total from plantaopro.clinica_contas_receber where consulta_id=@id", new { id = particularId }));
            Assert.Equal("ABERTA", contaPart.Status);
            Assert.Equal(150m, contaPart.Total);

            var (cortesiaId, pacB) = await SeedConsultaAsync(cn, tenant);
            await SeedAutorizacaoAsync(cn, tenant, cortesiaId, pacB, "PENDENTE");

            var semJustificativa = await Clinico(tenant).FinalizarAsync(cortesiaId, Finalizar(TipoFaturamentoAssistencial.CORTESIA), CancellationToken.None);
            Assert.False(semJustificativa.Success);
            Assert.Equal(400, semJustificativa.StatusCode);
            Assert.Contains("exige justificativa", semJustificativa.Message);

            var cortes = await Clinico(tenant).FinalizarAsync(cortesiaId, Finalizar(TipoFaturamentoAssistencial.CORTESIA, 0m, justificativa: "Atendimento de cortesia aprovado pela diretoria técnica."), CancellationToken.None);
            Assert.True(cortes.Success);
            Assert.False(cortes.Data!.PodeAbrirFaturamento);
            Assert.Null(cortes.Data.FinanceiroId);
            var contas = await cn.ExecuteScalarAsync<int>(new CommandDefinition(
                "select count(*)::int from plantaopro.clinica_contas_receber where consulta_id=@id", new { id = cortesiaId }));
            Assert.Equal(0, contas);
        }
        finally { await LimparAsync(cn, tenant); }
    }

    [Fact]
    public async Task T15_Pendencias_ExibeAlertaDePreAutorizacaoSemImpedir()
    {
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        try
        {
            await SeedTenantAsync(cn, tenant);
            var (consultaId, paciente) = await SeedConsultaAsync(cn, tenant);
            await SeedAutorizacaoAsync(cn, tenant, consultaId, paciente, "PENDENTE");

            var r = await Clinico(tenant).PendenciasAsync(consultaId, CancellationToken.None);

            Assert.True(r.Success);
            Assert.Empty(r.Data!.Impeditivas);
            Assert.True(r.Data.PodeFinalizar);
            Assert.Contains(r.Data.Alertas, a => a.Contains("Pré-autorização pendente para este atendimento"));
        }
        finally { await LimparAsync(cn, tenant); }
    }
}
