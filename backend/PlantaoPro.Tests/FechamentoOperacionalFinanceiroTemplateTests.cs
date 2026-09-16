namespace PlantaoPro.Tests;

public sealed class FechamentoOperacionalFinanceiroTemplateTests
{
    private static string Read(string path) => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, path));

    [Fact]
    public void Confirmacao_revalida_origem_conferencia_regra_e_correcoes_sob_lock()
    {
        var service = Read("backend/PlantaoPro.Api/Fechamentos/FechamentoOperacionalService.cs");
        Assert.Contains("for update of e, p", service);
        Assert.Contains("ORIGEM_ALTERADA", service);
        Assert.Contains("EXECUCAO_NAO_CONFERIDA", service);
        Assert.Contains("REGRA_REMUNERACAO_AUSENTE", service);
        Assert.Contains("CORRECAO_PENDENTE", service);
        Assert.Contains("f.Status==FechamentoStatus.Aprovado", service);
    }

    [Fact]
    public void Previa_expoe_memoria_criterio_competencia_bloqueios_e_separacao_do_pagamento()
    {
        var view = Read("backend/PlantaoPro.Web/Views/OperacaoPremium/Fechamentos.cshtml");
        Assert.Contains("Memória de cálculo", view);
        Assert.Contains("data de início da execução", view);
        Assert.Contains("Pendências da verificação", view);
        Assert.Contains("Obrigação não gerada", view);
        Assert.Contains("não é um PDF assinado nem comprovante bancário", view);
    }

    [Fact]
    public void Fechamento_e_origem_financeira_possuem_unicidade_no_banco()
    {
        var migration = Read("database/schema/270_v1410_cobertura_escalas_fechamento_financeiro.sql");
        Assert.Contains("ux_fechamento_plantao_ativo", migration);
        Assert.Contains("unique(tenant_id, escala_id)", migration);
    }
}
