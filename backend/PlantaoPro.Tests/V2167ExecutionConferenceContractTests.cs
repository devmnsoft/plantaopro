namespace PlantaoPro.Tests;
public sealed class V2167ExecutionConferenceContractTests
{
 private static string Read(string path)=>File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot,path));
 [Fact] public void Presence_uses_server_clock_transaction_and_unique_constraint(){var s=Read("backend/PlantaoPro.Api/ProfessionalPortalService.cs");Assert.Contains("BeginTransactionAsync",s);Assert.Contains("checkin_recebido_em",s);Assert.Contains("on conflict(tenant_id,escala_id) do nothing",s);Assert.Contains("for update",s);}
 [Fact] public void Correction_preserves_original_and_blocks_self_approval(){var s=Read("backend/PlantaoPro.Api/ExecutionConferenceService.cs");Assert.Contains("SolicitadoPor==c.User",s);Assert.Contains("valores_anteriores",s);Assert.Contains("AJUSTE_POS_APURACAO",s);Assert.Contains("row.Versao!=request.Versao",s);}
 [Fact] public void Schema_has_order_version_and_idempotency_guards(){var s=Read("database/schema/400_v2167_execucao_conferencia.sql");Assert.Contains("checkout_em >= checkin_em",Read("database/schema/310_v1450_design_system_executivo_operacao_comercial.sql"));Assert.Contains("versao bigint",s);Assert.Contains("ux_v2167_correcao_pendente",s);}
 [Fact] public void UI_compares_all_time_origins_and_has_no_fake_controls(){var s=Read("backend/PlantaoPro.Web/Views/MinhaAgenda/Presencas.cshtml");foreach(var label in new[]{"Horário previsto","Horário registrado","Horário proposto","Horário aprovado"})Assert.Contains(label,s);Assert.DoesNotContain("alert(",s);Assert.DoesNotContain("href=\"#\"",s);}
 [Fact] public void Previous_contracts_keep_repository_root_and_csharp10(){Assert.Contains("RepositoryPathResolver.RepoRoot",Read("backend/PlantaoPro.Tests/V2160ClinicalJourneyContractTests.cs"));Assert.DoesNotContain("required ",Read("backend/PlantaoPro.Api/ModuleContractingService.cs"));}
}
