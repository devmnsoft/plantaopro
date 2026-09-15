namespace PlantaoPro.Tests;
public sealed class OperationalReportsContractTests
{
 private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../"));
 [Fact] public void Reports_use_real_scoped_sources_and_distinct_business_stages(){var s=File.ReadAllText(Path.Combine(Root,"PlantaoPro.Api/OperationalReportService.cs"));Assert.Contains("p.vagas",s);Assert.Contains("status_conferencia",s);Assert.Contains("valor_apurado",s);Assert.Contains("data_pagamento",s);Assert.Contains("greatest(b.vagas-coalesce(a.n,0),0)",s);Assert.Contains("s.nova_escala_id is not null",s);Assert.Contains("c.tenant_id=@Tenant",s);Assert.Contains("f.tenant_id=@Tenant",s);}
 [Fact] public void Report_ui_has_filters_accessible_help_print_and_export(){var s=File.ReadAllText(Path.Combine(Root,"PlantaoPro.Web/Views/Relatorios/Operacional.cshtml"));Assert.Contains("Como interpretar este relatório",s);Assert.Contains("window.print()",s);Assert.Contains("ExportarOperacional",s);Assert.Contains("Os valores não foram substituídos por zero",s);Assert.Contains("Plantões iniciados no período",s);Assert.DoesNotContain("href=\"#\"",s);}
 [Fact] public void Csv_neutralizes_formula_and_server_revalidates_access(){var s=File.ReadAllText(Path.Combine(Root,"PlantaoPro.Api/OperationalReportService.cs"));Assert.Contains("\"=+-@\".Contains",s);Assert.Contains("GetAsync(kind,all,ct)",s);Assert.Contains("PermissionConstants.FinanceiroVer",s);Assert.Contains("PodeAcessarHospitalAsync",s);}
}
