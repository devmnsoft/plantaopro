using Xunit;

namespace PlantaoPro.Tests;

public sealed class Saude360BaseClinicaContractTests
{
    [Fact]
    public void MigrationContainsClinicalBaseTablesAndTenantIndexes()
    {
        var sql = Read("database/migrations/2026_plantao_pro_saude360_base_clinica.sql");
        foreach (var table in new[] { "pacientes", "paciente_contatos", "paciente_enderecos", "paciente_documentos", "paciente_historico", "paciente_observacoes", "paciente_consentimentos", "paineis_chamada", "painel_chamada_configuracoes", "painel_chamada_setores", "painel_chamada_salas", "painel_chamada_guiches", "painel_chamada_fila", "painel_chamada_historico", "agendamentos", "agendamento_historico", "agendamento_bloqueios", "agendamento_cancelamentos", "agendamento_checkins", "agendamento_tipos", "agendamento_status_historico", "triagens", "triagem_sinais_vitais", "triagem_classificacoes_risco", "triagem_historico", "triagem_fila", "triagem_encaminhamentos", "clinica_parametros", "clinica_unidades_atendimento", "clinica_salas_atendimento", "clinica_setores_atendimento", "clinica_permissoes_modulos" })
        {
            Assert.Contains("create table if not exists plantaopro." + table, sql, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("ux_pacientes_cliente_cpf_informado", sql);
        Assert.Contains("cliente_id", sql);
        Assert.Contains("reg_status", sql);
        Assert.DoesNotContain("add constraint if not exists", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiExposesRequiredClinicalBaseEndpoints()
    {
        var controller = Read("backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs");
        foreach (var fragment in new[] { "api/pacientes", "resumo-clinico", "buscar", "api/agendamentos", "marcar-falta", "agenda-dia", "api/painel-chamada", "rechamar", "ausente", "fila", "api/triagens", "iniciar", "classificacoes-risco", "api/clinica-dashboard" })
        {
            Assert.Contains(fragment, controller, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ClinicalServiceImplementsCriticalRulesAndAuditing()
    {
        var service = Read("backend/PlantaoPro.Api/Saude360ClinicalService.cs");
        Assert.Contains("CPF já cadastrado para este tenant", service);
        Assert.Contains("Já existe agendamento para o médico", service);
        Assert.Contains("Check-in permitido apenas", service);
        Assert.Contains("painel_chamada_fila", service);
        Assert.Contains("triagem_fila", service);
        Assert.Contains("triagem_encaminhamentos", service);
        Assert.Contains("AuditAsync", service);
        Assert.Contains("SafeRequest", service);
    }

    [Fact]
    public void WebExposesClinicalMenusAndProfiles()
    {
        var roles = Read("backend/PlantaoPro.Web/Security/RolesConstants.cs");
        var menu = Read("backend/PlantaoPro.Web/Services/Security/MenuBuilderService.cs");
        var web = Read("backend/PlantaoPro.Web/Controllers/Saude360WebControllers.cs");

        foreach (var role in new[] { "RECEPCAO", "TRIAGEM", "ENFERMAGEM", "COORDENADOR_CLINICO", "ADMINISTRADOR_CLINICA", "AUDITOR_CLINICO" }) Assert.Contains(role, roles);
        foreach (var item in new[] { "Pacientes", "Agendamento", "Painel de chamada", "Fila de Atendimento" }) Assert.Contains(item, menu, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ClinicaDashboardController", web);
    }

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(GetRepoRoot(), relativePath));

    private static string GetRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "backend", "PlantaoPro.sln"))) dir = dir.Parent;
        if (dir is null) throw new InvalidOperationException("Repo root not found.");
        return dir.FullName;
    }
}
