using PlantaoPro.Api.Models;

namespace PlantaoPro.Tests;

public sealed class FinanceiroPagamentoDetailsV2104Tests
{
    [Fact]
    public void Pagamento_details_possui_construtor_padrao_e_propriedades_gravaveis_para_o_dapper()
    {
        var type = typeof(PagamentoDetailsDto);
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));

        var expectedProperties = new[]
        {
            "Id", "EscalaId", "MedicoId", "PlantaoId", "MedicoNome", "MedicoCrm", "MedicoUfCrm",
            "MedicoEmail", "MedicoTelefone", "HospitalNome", "HospitalCidade", "HospitalEstado",
            "EspecialidadeNome", "DataInicioPlantao", "DataFimPlantao", "ValorPrevisto", "ValorPago",
            "Status", "DataPrevista", "DataPagamento", "FormaPagamento", "ChavePix", "Observacoes", "RegDate"
        };

        foreach (var name in expectedProperties)
            Assert.True(type.GetProperty(name)?.CanWrite, $"A propriedade {name} deve ser gravável pelo Dapper.");
    }

    [Fact]
    public void Campos_opcionais_de_liquidacao_aceitam_pagamento_pendente()
    {
        var dto = new PagamentoDetailsDto
        {
            Id = Guid.NewGuid(),
            ValorPrevisto = 1250m,
            ValorPago = null,
            DataPagamento = null,
            DataPrevista = null
        };

        Assert.Null(dto.ValorPago);
        Assert.Null(dto.DataPagamento);
        Assert.Null(dto.DataPrevista);
    }

    [Fact]
    public void Query_de_detalhe_tem_aliases_exatos_parametros_e_isolamento_por_tenant()
    {
        var data = File.ReadAllText(Path.Combine(RepositoryPathResolver.ApiRoot, "Data.cs"));
        var start = data.IndexOf("public async Task<ApiResponse<PagamentoDetailsDto>> GetByIdAsync", StringComparison.Ordinal);
        var end = data.IndexOf("public async Task<ApiResponse<Guid>> GerarAsync", start, StringComparison.Ordinal);
        var method = data[start..end];

        foreach (var alias in new[] { "Id", "DataInicioPlantao", "DataFimPlantao", "ValorPago", "DataPagamento", "ChavePix", "RegDate" })
            Assert.Contains($"as \"\"{alias}\"\"", method, StringComparison.Ordinal);

        Assert.Contains("pg.id=@Id", method, StringComparison.Ordinal);
        Assert.Contains("pg.tenant_id=@TenantId", method, StringComparison.Ordinal);
        Assert.Contains("new { Id = id, TenantId = tenantId", method, StringComparison.Ordinal);
        Assert.DoesNotContain("select *", method, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Detalhe_financeiro_exibe_estados_dados_e_formulario_sem_id_manual()
    {
        var root = RepositoryPathResolver.RepoRoot;
        var view = File.ReadAllText(Path.Combine(root, "backend", "PlantaoPro.Web", "Views", "Financeiro", "Details.cshtml"));

        foreach (var content in new[] { "pp-payment-state--error", "Pagamento não encontrado", "status == \"pendente\"", "Valor previsto", "Chave Pix", "Rastreabilidade", "<select" })
            Assert.Contains(content, view, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain("Digite o ID", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"#\"", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert(", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("confirm(", view, StringComparison.OrdinalIgnoreCase);
    }
}
