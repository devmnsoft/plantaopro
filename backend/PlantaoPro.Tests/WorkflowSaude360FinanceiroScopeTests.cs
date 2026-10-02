using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// B4 (J12) — Investigação ClienteId/TenantId do WorkflowSaude360Service.
///
/// DIAGNÓSTICO: os escritores clínicos (ConsultaApplicationService e
/// Saude360ClinicalService) gravam em clinica_contas_receber.cliente_id /
/// clinica_recebimentos.cliente_id (= o tenant da clínica) e NUNCA em
/// tenant_id — 100% das linhas existentes possuem tenant_id NULL. O
/// vocabulário de status inclui ABERTO (default DDL/CRUD) e ABERTA
/// (faturamento de consulta), além de VENCIDA (lookup canônico da UI).
/// O workflow filtrava apenas por tenant_id e por ('ABERTO','VENCIDA'),
/// o que zerava sistematicamente os indicadores ContasPendentes e
/// PagamentosRecebidos.
///
/// REGRESSÃO: o resumo deve escopar por coalesce(tenant_id, cliente_id)
/// e contar todas as contas abertas/vencidas dos 4 sinônimos, sem
/// cruzar tenants nem contar contas já recebidas.
/// </summary>
public sealed class WorkflowSaude360FinanceiroScopeTests
{
    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly Guid _tenantId;

        public FakeCurrentUser(Guid tenantId) => _tenantId = tenantId;

        public Guid? UserId => Guid.Empty;
        public Guid? TenantId => _tenantId;
        public Guid? ClienteId => null;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles => Array.Empty<string>();
        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => false;
        public bool IsTenantAdmin() => true;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => role == "TENANT_ADMIN";
    }

    [Fact]
    public async Task B4_Workflow_EscopaContasClinicasPorClienteId_E_NaoCruzaTenants()
    {
        var cs = TestDatabase.ConnectionString;
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var pacienteId = Guid.NewGuid();
        var contaAberto = Guid.NewGuid();     // writer CRUD (Saude360ClinicalService) -> 'ABERTO'
        var contaAberta = Guid.NewGuid();     // writer faturamento consulta -> 'ABERTA'
        var contaVencida = Guid.NewGuid();    // lookup canônico da UI -> 'VENCIDA'
        var contaOutra = Guid.NewGuid();      // pertence a OUTRO tenant
        var contaRecebida = Guid.NewGuid();   // quitada -> não é pendência
        var recA = Guid.NewGuid();            // recebimento confirmado hoje (tenant A)
        var recB = Guid.NewGuid();            // recebimento confirmado hoje (tenant B)

        try
        {
            await cn.ExecuteAsync(new CommandDefinition(@"
                insert into plantaopro.tenants(id, tenant_id, codigo, nome, status)
                values(@tenantA,@tenantA,'wf-b4-a','Tenant WF B4 A','ATIVO'),
                      (@tenantB,@tenantB,'wf-b4-b','Tenant WF B4 B','ATIVO');

                -- Convenção canônica dos escritores: tenant_id NULL, cliente_id = tenant da clínica.
                insert into plantaopro.clinica_contas_receber(
                    id, tenant_id, cliente_id, paciente_id, descricao, origem, valor_total, valor_pendente, vencimento, status, reg_date, reg_status)
                values
                    (@contaAberto, null, @tenantA, @pacienteId, 'WFB4 conta ABERTA (CRUD)', 'MANUAL', 100, 100, current_date + 30, 'ABERTO', now(), 'A'),
                    (@contaAberta, null, @tenantA, @pacienteId, 'WFB4 conta ABERTA (consulta)', 'CONSULTA', 80, 80, current_date + 30, 'ABERTA', now(), 'A'),
                    (@contaVencida, null, @tenantA, @pacienteId, 'WFB4 conta VENCIDA', 'MANUAL', 50, 50, current_date - 5, 'VENCIDA', now(), 'A'),
                    (@contaOutra, null, @tenantB, @pacienteId, 'WFB4 conta OUTRO tenant', 'MANUAL', 40, 40, current_date + 30, 'ABERTA', now(), 'A'),
                    (@contaRecebida, null, @tenantA, @pacienteId, 'WFB4 conta RECEBIDA', 'MANUAL', 30, 0, current_date - 10, 'RECEBIDO', now(), 'A');

                insert into plantaopro.clinica_recebimentos(
                    id, tenant_id, cliente_id, conta_receber_id, paciente_id, valor, forma_pagamento, data_recebimento, status, reg_date, reg_status)
                values
                    (@recA, null, @tenantA, @contaRecebida, @pacienteId, 30, 'DINHEIRO', now(), 'CONFIRMADO', now(), 'A'),
                    (@recB, null, @tenantB, @contaOutra, @pacienteId, 40, 'PIX', now(), 'CONFIRMADO', now(), 'A');
            ", new
            {
                tenantA, tenantB, pacienteId,
                contaAberto, contaAberta, contaVencida, contaOutra, contaRecebida, recA, recB
            }));

            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = cs })
                .Build();

            var filtro = new WorkflowSaude360Filtro
            {
                Inicio = DateTimeOffset.UtcNow.AddDays(-1),
                Fim = DateTimeOffset.UtcNow.AddDays(1)
            };

            // --- TENANT A: conta as 3 pendências (ABERTO + ABERTA + VENCIDA);
            //     exclui RECEBIDA (quitada) e a conta do outro tenant. ---
            var serviceA = new WorkflowSaude360Service(cfg, new FakeCurrentUser(tenantA));
            var resumoA = await serviceA.ResumoAsync(filtro, CancellationToken.None);
            Assert.Equal(3, resumoA.ContasPendentes);
            Assert.Equal(1, resumoA.PagamentosRecebidos);

            // --- TENANT B: enxerga apenas os próprios dados (isolamento). ---
            var serviceB = new WorkflowSaude360Service(cfg, new FakeCurrentUser(tenantB));
            var resumoB = await serviceB.ResumoAsync(filtro, CancellationToken.None);
            Assert.Equal(1, resumoB.ContasPendentes);
            Assert.Equal(1, resumoB.PagamentosRecebidos);
        }
        finally
        {
            await cn.ExecuteAsync(new CommandDefinition(@"
                delete from plantaopro.clinica_recebimentos where id in (@recA,@recB);
                delete from plantaopro.clinica_contas_receber where id in (@contaAberto,@contaAberta,@contaVencida,@contaOutra,@contaRecebida);
                delete from plantaopro.tenants where id in (@tenantA,@tenantB);
            ", new
            {
                contaAberto, contaAberta, contaVencida, contaOutra, contaRecebida, recA, recB, tenantA, tenantB
            }));
        }
    }
}
