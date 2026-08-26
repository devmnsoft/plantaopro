using System.Reflection;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Tests;

public sealed class FinanceiroDapperHardeningV2105Tests
{
    [Theory]
    [InlineData(typeof(PagamentoDetailsDto))]
    [InlineData(typeof(PagamentoResumoDto))]
    [InlineData(typeof(MedicoPagamentoDto))]
    [InlineData(typeof(DashboardDto))]
    [InlineData(typeof(DashboardChartItem))]
    public void Dtos_financeiros_materializados_possuem_construtor_padrao_e_propriedades_gravaveis(Type dto)
    {
        Assert.NotNull(dto.GetConstructor(Type.EmptyTypes));
        Assert.All(dto.GetProperties(BindingFlags.Instance | BindingFlags.Public), property => Assert.True(property.CanWrite, property.Name));
    }

    [Fact]
    public void Pagamentos_preservam_nulls_e_composicao_financeira()
    {
        var dto = new PagamentoResumoDto { Status = "pendente", ValorPago = null, DataPagamento = null, ValorLiquido = 125.50m };
        Assert.Null(dto.ValorPago);
        Assert.Null(dto.DataPagamento);
        Assert.Equal(125.50m, dto.ValorLiquido);
        dto.Status = "pago";
        dto.ValorPago = 125.50m;
        dto.DataPagamento = new DateOnly(2026, 8, 26);
        Assert.Equal("pago", dto.Status);
    }

    [Fact]
    public void Queries_de_pagamento_sao_explicitas_parametrizadas_e_isoladas_por_tenant()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Data.cs"));
        var start = source.IndexOf("public sealed class FinanceiroService", StringComparison.Ordinal);
        var end = source.IndexOf("public sealed class NotificacaoService", start, StringComparison.Ordinal);
        var financeiro = source[start..end];
        Assert.DoesNotContain("select *", financeiro, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pg.tenant_id=@TenantId", financeiro, StringComparison.Ordinal);
        foreach (var alias in new[] { "Id", "ValorPrevisto", "ValorPago", "ValorBruto", "ValorLiquido", "Descontos", "Acrescimos", "DataPagamento", "RegDate" })
            Assert.Contains($"as \"\"{alias}\"\"", financeiro, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Detalhe_e_listagem_cobrem_registro_inexistente_e_filtros_financeiros()
    {
        var source = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Data.cs"));
        Assert.Contains("Pagamento não encontrado", source, StringComparison.Ordinal);
        Assert.Contains("lower(pg.forma_pagamento)=lower(@fp)", source, StringComparison.Ordinal);
        Assert.Contains("lower(pg.status)=lower(@s)", source, StringComparison.Ordinal);
    }
}
