namespace PlantaoPro.Tests;

public sealed class FechamentoDemonstrativosV2169ContractTests
{
    private static string Read(string path)=>File.ReadAllText(Path.Combine(FindRoot(),path));
    private static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"README.md")))d=d.Parent;return d?.FullName??throw new DirectoryNotFoundException();}

    [Fact] public void Fechamento_DeveExigirTenantEExecucaoConferida()
    { var s=Read("backend/PlantaoPro.Api/Fechamentos/FechamentoOperacionalService.cs");Assert.Contains("p.tenant_id=@Tenant and p.cliente_id=@Cliente",s);Assert.Contains("mc.status_conferencia='APROVADA'",s);Assert.Contains("for update",s); }

    [Fact] public void Demonstrativo_DeveUsarValoresPersistidosESaldoCanonico()
    { var s=Read("backend/PlantaoPro.Api/Data.cs");Assert.Contains("coalesce(pg.valor_apurado,pg.valor_previsto) as ValorApurado",s);Assert.Contains("coalesce(pg.valor_aprovado,pg.valor_apurado,pg.valor_previsto)-coalesce(pg.valor_pago,0)",s);Assert.Contains("financeiro_pagamento_origem",s); }

    [Fact] public void Divergencia_DeveSerAutorizadaPorPropriedadeEPreservarValor()
    { var s=Read("backend/PlantaoPro.Api/Data.cs");Assert.Contains("m.usuario_id=@uid",s);Assert.Contains("pagamento_contestacoes",s);Assert.DoesNotContain("update plantaopro.pagamentos set valor",s[s.IndexOf("CriarContestacaoAsync",StringComparison.Ordinal)..]); }

    [Fact] public void Csv_DeveNeutralizarFormulaEViewsDevemExplicarUso()
    { var service=Read("backend/PlantaoPro.Api/Fechamentos/FechamentoOperacionalService.cs");Assert.Contains("=+-@",service);Assert.Contains("Como usar esta página",Read("backend/PlantaoPro.Web/Views/OperacaoPremium/Fechamentos.cshtml"));Assert.Contains("Como usar esta página",Read("backend/PlantaoPro.Web/Views/MinhaAgenda/MeusPagamentos.cshtml")); }
}
