namespace PlantaoPro.Tests;

public sealed class V2157OperationalCycleContractTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void Migration_DeveProtegerVagaConviteEscalaEPagamentoSobConcorrencia()
    {
        var sql = Read("database/schema/360_v2157_central_operacional_escalas.sql");
        Assert.Contains("vagas_disponiveis between 0 and vagas", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ux_plantao_convite_pendente", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ux_escala_ocupacao_ativa", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ux_pagamento_origem_escala", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Confirmacao_DeveSerializarUltimaVagaERevalidarRegras()
    {
        var source = Read("backend/PlantaoPro.Api/Data.cs");
        Assert.Contains("from plantaopro.plantoes where id=@id for update", source);
        Assert.Contains("vagas_disponiveis=vagas_disponiveis-1", source);
        Assert.Contains("VerificarElegibilidadeParaPlantaoAsync(e.MedicoId", source);
        Assert.Contains("ExisteConflitoAsync(e.MedicoId", source);
    }

    [Fact]
    public void Convite_DeveSerReivindicadoUmaUnicaVezEValidarExpiracao()
    {
        var source = Read("backend/PlantaoPro.Api/Controllers/MobileController.cs");
        Assert.Contains("set status='PROCESSANDO'", source);
        Assert.Contains("expira_em is null or expira_em > now()", source);
        Assert.DoesNotContain("alert(", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pagamento_DeveExigirRealizacaoTenantEOrigemUnica()
    {
        var source = Read("backend/PlantaoPro.Api/Data.cs");
        Assert.Contains("row.Status != \"realizado\"", source);
        Assert.Contains("p.cliente_id=@clienteId for update of e", source);
        Assert.Contains("PostgresErrorCodes.UniqueViolation", source);
    }
}
