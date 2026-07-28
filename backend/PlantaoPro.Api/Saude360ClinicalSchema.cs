using Dapper;
using Npgsql;

namespace PlantaoPro.Api;

/// <summary>Readiness check only. Schema changes belong to versioned database migrations.</summary>
internal static class Saude360ClinicalSchema
{
    private static readonly string[] RequiredTables =
    {
        "pacientes", "paciente_contatos", "paciente_enderecos", "paciente_documentos",
        "paciente_historico", "agendamentos", "agendamento_checkins", "painel_chamada",
        "painel_chamada_fila", "agendamento_historico", "triagens", "triagem_fila",
        "triagem_historico", "triagem_encaminhamentos"
    };

    public static async Task GarantirBaseClinicaAsync(NpgsqlConnection cn, ILogger logger)
    {
        var available = await cn.QueryAsync<string>(
            "select tablename from pg_catalog.pg_tables where schemaname = @Schema",
            new { Schema = "plantaopro" });
        var set = new HashSet<string>(available, StringComparer.OrdinalIgnoreCase);
        var missing = RequiredTables.Where(table => !set.Contains(table)).ToArray();
        if (missing.Length == 0) return;

        logger.LogError("Base clínica indisponível: {MissingCount} migrations obrigatórias não foram aplicadas.", missing.Length);
        throw new InvalidOperationException("Base clínica indisponível. Aplique as migrations pendentes antes de iniciar a operação.");
    }
}
