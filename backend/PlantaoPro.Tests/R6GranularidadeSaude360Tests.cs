using System.Text;
using System.Text.Json;
using PlantaoPro.CrossCutting.Security;

namespace PlantaoPro.Tests;

/// <summary>
/// R6-BlocoA item 2 — granularidade real do Saúde 360. Fixa o contrato do kernel
/// de acesso efetivo introduzido pela migration v2339: o predicado de vigência B6
/// (ModuleContractVigencia.EffectivePredicate, C#) e o das capacidades herdadas do
/// pacote vivem materializados na função canônica plantaopro.modulos_efetivos() e
/// seu invólucro modulo_efetivo(), e TODO o runtime de acesso (claims de login,
/// política por request, gate clínico, autosserviço, onboarding) passa pela função,
/// nunca por cópia local do predicado. Teste de sincronia texto-a-texto + checagens
/// estruturais da migração e do manifest. Não exige banco.
/// </summary>
public sealed class R6GranularidadeSaude360Tests
{
    private const string MigrationRel = "database/migrations/2026_10_v2339_r6a_granularidade_saude360_pacote_capacidades.sql";
    private const string Version = "2026_10_v2339_r6a_granularidade_saude360_pacote_capacidades";
    private const string MigrationFile = "2026_10_v2339_r6a_granularidade_saude360_pacote_capacidades.sql";

    // v2340 redefine plantaopro.modulos_efetivos() com a regra de override per-capacidade
    // (conflito pacote x capacidade do BlocoA item 1). É a função CANÔNICA VIGENTE.
    private const string MigrationV2340Rel = "database/migrations/2026_10_v2340_r6a1_acesso_canonico_conflito_pacote_capacidade.sql";
    private const string VersionV2340 = "2026_10_v2340_r6a1_acesso_canonico_conflito_pacote_capacidade";
    private const string MigrationFileV2340 = "2026_10_v2340_r6a1_acesso_canonico_conflito_pacote_capacidade.sql";

    private static readonly string[] FamilyCapabilities =
    {
        "AGENDAMENTOS", "TRIAGEM", "UNIDADES", "CLINICA_DASHBOARD", "PAINEL_CHAMADA", "CID",
        "PRESCRICOES", "CLINICA_FINANCEIRO", "CONVENIOS", "PLANOS_SAUDE", "PENDENCIAS_CLINICAS"
    };

    private static string Read(string rel) => File.ReadAllText(Path.Combine(RepositoryPathResolver.RepoRoot, rel));

    // Normalização para comparação resistente a formatação: remove todo espaço em
    // branco (o predicado aparece quebrado em linhas diferentes no C# e no SQL).
    private static string Norm(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s) if (!char.IsWhiteSpace(c)) sb.Append(c);
        return sb.ToString();
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (needle.Length == 0) return 0;
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0) { count++; idx += needle.Length; }
        return count;
    }

    private static string FunctionBody(string migration, string signatureMarker)
    {
        var start = migration.IndexOf(signatureMarker, StringComparison.OrdinalIgnoreCase);
        Assert.True(start >= 0, $"função não encontrada na migração: {signatureMarker}");
        var end = migration.IndexOf("$fn$;", start, StringComparison.Ordinal);
        Assert.True(end > start, $"corpo da função não delimitado: {signatureMarker}");
        return migration.Substring(start, end - start);
    }

    [Fact]
    public void Canonical_b6_predicate_in_csharp_and_in_sql_function_do_not_diverge()
    {
        var predicate = Norm(ModuleContractVigencia.EffectivePredicate);

        // O predicado B6 do C# tem que estar presente, byte-a-byte (ignorando espaço),
        // nas QUATRO ramificações do corpo, TANTO na v2339 original quanto na v2340
        // vigente (que só acrescenta o override per-capacidade). Editar uma ramificação
        // sem editar o C# derruba a contagem; editar o C# sem o SQL derruba o Contains.
        foreach (var rel in new[] { MigrationRel, MigrationV2340Rel })
        {
            var mig = Read(rel);
            var fnSql = FunctionBody(mig, "create or replace function plantaopro.modulos_efetivos");
            var normalizedFn = Norm(fnSql);

            Assert.True(normalizedFn.Contains(predicate, StringComparison.Ordinal), $"predicado B6 ausente em {rel}.");
            Assert.True(CountOccurrences(normalizedFn, predicate) == 4, $"predicado B6 deve ocorrer 4x em {rel}.");
        }
    }

    [Fact]
    public void Kernel_exposes_canonical_function_and_pointwise_lookup_in_crosscutting()
    {
        // As constantes que o runtime consome apontam exatamente para as funções criadas.
        Assert.Contains("plantaopro.modulos_efetivos(@tenantId)", ModuleContractVigencia.ModulosEfetivosSql);
        Assert.Contains("plantaopro.modulo_efetivo(@tenantId,@moduleCode)", ModuleContractVigencia.ModuloEfetivoSql);

        var mig = Read(MigrationRel);
        Assert.Contains("create or replace function plantaopro.modulos_efetivos(p_tenant_id uuid)", mig, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("create or replace function plantaopro.modulo_efetivo(p_tenant_id uuid, p_codigo text)", mig, StringComparison.OrdinalIgnoreCase);
        // modulo_efetivo delega para modulos_efetivos (mesma semântica, ponto único).
        var wrapper = FunctionBody(mig, "create or replace function plantaopro.modulo_efetivo");
        Assert.Contains("fromplantaopro.modulos_efetivos(p_tenant_id)", Norm(wrapper));
    }

    [Fact]
    public void All_runtime_access_points_consult_the_canonical_function_never_an_inline_copy()
    {
        // Cada ponto de enforcement consulta a função canônica e NÃO reimplementa o
        // predicado B6 (marca distintiva: 'desativado_em is null'). O predicado isolado
        // só sobrevive no CrossCutting (linha tm administrativa) e na própria migração.
        (string Path, string Marker)[] points =
        {
            ("backend/PlantaoPro.Api/Data.cs", "ModuleContractVigencia.ModulosEfetivosSql"),
            ("backend/PlantaoPro.Api/SecurityAdministrationServices.cs", "ModuleContractVigencia.ModuloEfetivoSql"),
            ("backend/PlantaoPro.Api/SelfServiceServices.cs", "plantaopro.modulo_efetivo(@tenantId,m.codigo)"),
            ("backend/PlantaoPro.Api/Controllers/SelfServiceSaasController.cs", "plantaopro.modulo_efetivo(@tenantId,m.codigo)"),
            ("backend/PlantaoPro.Api/OnboardingJornadaService.cs", "plantaopro.modulos_efetivos(@tenantId)"),
            ("backend/PlantaoPro.Api/Saude360ModuleFilter.cs", "plantaopro.modulo_efetivo(@tenantId,'SAUDE360')"),
        };
        foreach (var (path, marker) in points)
        {
            var src = Read(path);
            Assert.True(src.Contains(marker, StringComparison.OrdinalIgnoreCase), $"{path} deve consultar a fonte canônica ({marker}).");
            Assert.False(src.Contains("desativado_em is null", StringComparison.OrdinalIgnoreCase), $"{path} não pode reimplementar o predicado B6 inline.");
        }
    }

    [Fact]
    public void Saude360_gate_no_longer_fails_open_on_database_error()
    {
        // R6 removeu o try/catch fail-open: falha de banco é indisponibilidade real
        // (500 honesto), não autorização silenciosa de módulo não contratado.
        var src = Read("backend/PlantaoPro.Api/Saude360ModuleFilter.cs");
        Assert.DoesNotContain("catch", src, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migration_registers_missing_capabilities_and_links_them_to_the_package()
    {
        var mig = Read(MigrationRel);
        Assert.Contains("add column if not exists pacote_codigo", mig, StringComparison.OrdinalIgnoreCase);
        // Capacidades-filhas que o guard cobra por controller e faltavam no catálogo.
        foreach (var cap in FamilyCapabilities)
            Assert.Contains($"('{cap}", mig, StringComparison.Ordinal);
        // Vínculo pacote->capacidade e cobertura da família inteira (inclui topo PACIENTES/CONSULTAS).
        Assert.Contains("set pacote_codigo = 'SAUDE360'", mig, StringComparison.OrdinalIgnoreCase);
        foreach (var code in FamilyCapabilities.Concat(new[] { "pacientes", "consultas" }))
            Assert.Contains(code, mig, StringComparison.OrdinalIgnoreCase);
        // Pré-condição honesta: exige o módulo-pacote SAUDE360 no catálogo.
        Assert.Contains("SAUDE360 ausente", mig, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Migration_grants_suite_to_admins_translates_operational_guard_and_readonly_for_auditor()
    {
        var mig = Read(MigrationRel);
        // Administradores recebem a suíte completa das capacidades contratadas.
        Assert.Contains("'ADMINISTRADOR_CLIENTE', 'ADMINISTRADOR_CLINICA'", mig, StringComparison.Ordinal);
        // Tradução dos conjuntos hardcoded do guard legado para grants de catálogo.
        foreach (var perfil in new[] { "RECEPCAO", "TRIAGEM", "MEDICO", "FINANCEIRO_CLINICA", "FATURAMENTO_CONVENIO" })
            Assert.Contains($"'{perfil}'", mig, StringComparison.Ordinal);
        // Auditor: somente leitura.
        Assert.Contains("'AUDITOR_CLINICO'", mig, StringComparison.Ordinal);
        // Conjuntos de ações do resolver genérico do guard.
        Assert.Contains("CONFIRMAR", mig, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_is_additive_and_idempotent_never_reduces_existing_access()
    {
        var mig = Read(MigrationRel);
        Assert.Contains("on conflict", mig, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do nothing", mig, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("drop table", mig, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("truncate", mig, StringComparison.OrdinalIgnoreCase);
        // Aditiva: nenhuma linha de contrato existente é removida/reduzida.
        Assert.DoesNotContain("delete from plantaopro.tenant_modulos", mig, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("delete from plantaopro.perfil_permissoes", mig, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Manifest_wires_the_migration_as_active_and_transactional_after_e13_p2()
    {
        using var doc = JsonDocument.Parse(Read("database/migration-manifest.json"));
        var root = doc.RootElement.GetProperty("migrations");
        JsonElement entry = default;
        bool found = false;
        foreach (var m in root.EnumerateArray())
        {
            if (string.Equals(m.GetProperty("version").GetString(), Version, StringComparison.Ordinal)) { entry = m; found = true; break; }
        }
        Assert.True(found, $"manifest sem a versão {Version}.");
        Assert.Equal("active", entry.GetProperty("status").GetString());
        Assert.True(entry.GetProperty("transactional").GetBoolean());
        Assert.EndsWith(MigrationFile, entry.GetProperty("source").GetString(), StringComparison.Ordinal);

        // Depende do backfill canônico E13-P2 e da garantia fiscal p/ não correr antes deles.
        var deps = entry.GetProperty("dependsOn").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains("2026_10_v2336_e13_p2_backfill_tenant_id_nucleo_clinico", deps);
        Assert.Contains("2026_10_v2338_p0_adm360_garantia_codigos_fiscais", deps);
    }

    [Fact]
    public void V2340_treats_per_capability_override_with_precedence_over_package_inheritance()
    {
        // BlocoA item 1: conflito pacote x capacidade. Uma linha propria de contrato para
        // uma capacidade (tenant_modulos) prevalece sobre a herança do pacote contratado.
        // Na função, as duas ramificações de herança ganham um anti-join que exclui a
        // capacidade quando o tenant tem linha própria (reg_status='A') por modulo_id ou
        // codigo_modulo — assim uma capacidade desabilitada individualmente dentro de um
        // pacote ativo deixa de ser efetiva.
        var mig = Read(MigrationV2340Rel);
        var fn = FunctionBody(mig, "create or replace function plantaopro.modulos_efetivos");
        var normalizedFn = Norm(fn);

        // Contrato da função preservado (mesma assinatura de retorno da v2339).
        Assert.Contains("returns table (codigo text, modulo_id uuid, pacote_codigo text)", mig, StringComparison.OrdinalIgnoreCase);

        // Anti-join de override presente nas DUAS ramificações de herança (b1 e b2).
        Assert.True(CountOccurrences(normalizedFn, "notexists(") == 2, "o override per-capacidade deve aplicar às duas ramificações de herança.");
        Assert.Contains("ovr.modulo_id=ch.id", normalizedFn);
        Assert.Contains("lower(nullif(ovr.codigo_modulo,''))=lower(ch.codigo)", normalizedFn);
    }

    [Fact]
    public void Manifest_wires_v2340_after_v2339_as_active_transactional()
    {
        using var doc = JsonDocument.Parse(Read("database/migration-manifest.json"));
        var root = doc.RootElement.GetProperty("migrations");
        JsonElement entry = default;
        bool found = false;
        foreach (var m in root.EnumerateArray())
        {
            if (string.Equals(m.GetProperty("version").GetString(), VersionV2340, StringComparison.Ordinal)) { entry = m; found = true; break; }
        }
        Assert.True(found, $"manifest sem a versão {VersionV2340}.");
        Assert.Equal("active", entry.GetProperty("status").GetString());
        Assert.True(entry.GetProperty("transactional").GetBoolean());
        Assert.EndsWith(MigrationFileV2340, entry.GetProperty("source").GetString(), StringComparison.Ordinal);

        // Depende do kernel da v2339 para não correr antes dele.
        var deps = entry.GetProperty("dependsOn").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains(Version, deps);
    }
}
