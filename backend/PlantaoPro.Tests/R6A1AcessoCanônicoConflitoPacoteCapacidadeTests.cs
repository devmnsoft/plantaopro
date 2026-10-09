using Dapper;
using PlantaoPro.Tests.Infrastructure;
using Npgsql;

namespace PlantaoPro.Tests;

/// <summary>
/// R6-BlocoA item 1 (acesso canônico) — prova comportamental da função canônica
/// <c>plantaopro.modulos_efetivos()</c> sobre a regra de conflito pacote × capacidade
/// introduzida pela <c>v2340</c>. Cada teste roda numa transação com ROLLBACK final e
/// códigos únicos por Guid: não depende nem polui o catálogo compartilhado.
///
/// Cenários comprovados:
///   S1 — contratar o pacote libera as capacidades-filhas (herança por modulo_id);
///   S2 — uma linha própria de contrato para uma capacidade (desabilitada) dentro de um
///        pacote ativo EXCLUI a capacidade do conjunto efetivo (override prevalece);
///   S3 — revogação/suspensão do pacote remove todas as capacidades herdadas.
/// </summary>
public sealed class R6A1AcessoCanônicoConflitoPacoteCapacidadeTests
{
    private static readonly string Cs = TestDatabase.ConnectionString;

    private const string SeedModulo = @"
        insert into plantaopro.modulos_sistema (id, codigo, nome, status, reg_status, reg_date, pacote_codigo)
        values (@Id, @Codigo, @Nome, 'ATIVO', 'A', now(), @Pacote);";

    private const string SeedLinha = @"
        insert into plantaopro.tenant_modulos
            (id, tenant_id, cliente_id, modulo_id, codigo_modulo, codigo, nome, status, habilitado, origem, reg_status, reg_date)
        values
            (@Id, @Tenant, @Tenant, @ModuloId, @CodigoModulo, @CodigoModulo, @Nome, @Status, @Habilitado, 'TESTE_R6A1', 'A', now());";

    private static async Task<IReadOnlySet<string>> EfetivosAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid tenant)
        => (await cn.QueryAsync<string>("select codigo from plantaopro.modulos_efetivos(@t)", new { t = tenant }, transaction: tx)).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static async Task<(NpgsqlConnection cn, NpgsqlTransaction tx, Guid Tenant, Guid PacoteId, Guid CapAId, Guid CapBId, string PacoteCod, string CapACod, string CapBCod)>
        NovoEscopoAsync(string sufixo)
    {
        var cn = new NpgsqlConnection(Cs);
        await cn.OpenAsync();
        var tx = await cn.BeginTransactionAsync();
        var tenant = Guid.NewGuid();
        var pacoteId = Guid.NewGuid();
        var capAId = Guid.NewGuid();
        var capBId = Guid.NewGuid();
        var pacoteCod = "PQT_" + sufixo + "_" + tenant.ToString("N").Substring(0, 8);
        var capACod = "CAPA_" + sufixo + "_" + tenant.ToString("N").Substring(0, 8);
        var capBCod = "CAPB_" + sufixo + "_" + tenant.ToString("N").Substring(0, 8);

        await cn.ExecuteAsync(SeedModulo, new { Id = pacoteId, Codigo = pacoteCod, Nome = "Pacote " + pacoteCod, Pacote = (string?)null }, transaction: tx);
        await cn.ExecuteAsync(SeedModulo, new { Id = capAId, Codigo = capACod, Nome = "Capacidade A " + capACod, Pacote = (string?)pacoteCod }, transaction: tx);
        await cn.ExecuteAsync(SeedModulo, new { Id = capBId, Codigo = capBCod, Nome = "Capacidade B " + capBCod, Pacote = (string?)pacoteCod }, transaction: tx);

        return (cn, tx, tenant, pacoteId, capAId, capBId, pacoteCod, capACod, capBCod);
    }

    private static async Task LinhaAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid id, Guid tenant, Guid moduloId, string codigoModulo, string nome, string status, bool habilitado)
        => await cn.ExecuteAsync(SeedLinha, new { Id = id, Tenant = tenant, ModuloId = moduloId, CodigoModulo = codigoModulo, Nome = nome, Status = status, Habilitado = habilitado }, transaction: tx);

    [Fact]
    public async Task S1_ContratarPacote_LiberaCapacidadesHerdadas()
    {
        var s = await NovoEscopoAsync("S1");
        try
        {
            await LinhaAsync(s.cn, s.tx, Guid.NewGuid(), s.Tenant, s.PacoteId, s.PacoteCod, "Pacote", "ATIVO", habilitado: true);
            var efetivos = await EfetivosAsync(s.cn, s.tx, s.Tenant);

            Assert.Contains(s.PacoteCod, efetivos);
            Assert.Contains(s.CapACod, efetivos);
            Assert.Contains(s.CapBCod, efetivos);
        }
        finally
        {
            try { await s.tx.RollbackAsync(); } catch { /* transação já concluída */ }
            await s.cn.DisposeAsync();
        }
    }

    [Fact]
    public async Task S2_CapacidadeDesabilitadaIndividualmente_NaoEntraEmPacoteAtivo()
    {
        var s = await NovoEscopoAsync("S2");
        try
        {
            // Pacote ativo...
            await LinhaAsync(s.cn, s.tx, Guid.NewGuid(), s.Tenant, s.PacoteId, s.PacoteCod, "Pacote", "ATIVO", habilitado: true);
            // ...com uma linha PRÓPRIA (decisão per-capacidade) desabilitando CAPA.
            await LinhaAsync(s.cn, s.tx, Guid.NewGuid(), s.Tenant, s.CapAId, s.CapACod, "CapA", "ATIVO", habilitado: false);

            var efetivos = await EfetivosAsync(s.cn, s.tx, s.Tenant);

            Assert.Contains(s.PacoteCod, efetivos);
            Assert.DoesNotContain(s.CapACod, efetivos);   // restrição individual prevalece sobre o pacote
            Assert.Contains(s.CapBCod, efetivos);          // demais capacidades continuam herdadas
        }
        finally
        {
            try { await s.tx.RollbackAsync(); } catch { /* transação já concluída */ }
            await s.cn.DisposeAsync();
        }
    }

    [Fact]
    public async Task S3_RevogacaoDoPacote_RemoveTudoInclusoCapacidadeComLinhaAtiva()
    {
        var s = await NovoEscopoAsync("S3");
        try
        {
            // Pacote suspenso (desativado) + uma linha própria ATIVA de uma capacidade.
            await LinhaAsync(s.cn, s.tx, Guid.NewGuid(), s.Tenant, s.PacoteId, s.PacoteCod, "Pacote", "ATIVO", habilitado: false);
            await LinhaAsync(s.cn, s.tx, Guid.NewGuid(), s.Tenant, s.CapAId, s.CapACod, "CapA", "ATIVO", habilitado: true);

            var efetivos = await EfetivosAsync(s.cn, s.tx, s.Tenant);

            Assert.DoesNotContain(s.PacoteCod, efetivos);   // pacote revogado não está efetivo
            Assert.DoesNotContain(s.CapBCod, efetivos);     // herança desaparece com o pacote
            Assert.Contains(s.CapACod, efetivos);           // linha própria ativa permanece (override independente)
        }
        finally
        {
            try { await s.tx.RollbackAsync(); } catch { /* transação já concluída */ }
            await s.cn.DisposeAsync();
        }
    }
}
