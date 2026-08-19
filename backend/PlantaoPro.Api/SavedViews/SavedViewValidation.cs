using System.Text;
using System.Text.Json;

namespace PlantaoPro.Api.SavedViews;

public static class SavedViewValidation
{
    public const int MaxJsonBytes = 16 * 1024;
    private static readonly IReadOnlyDictionary<string, HashSet<string>> Filters = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["PLANTOES"] = Set("status", "hospital", "especialidade", "inicio", "fim", "cobertura"),
        ["ESCALAS"] = Set("status", "medico", "hospital", "especialidade", "inicio", "fim"),
        ["PAGAMENTOS"] = Set("status", "medico", "hospital", "inicio", "fim", "vencimento"),
        ["PRODUTIVIDADE"] = Set("prioridade", "modulo", "status", "responsavel", "prazo", "unidade", "aba"),
        ["PACIENTES"] = Set("status", "unidade", "busca"),
        ["AGENDA"] = Set("status", "unidade", "profissional", "inicio", "fim")
    };
    private static readonly HashSet<string> SortKeys = Set("campo", "direcao");

    public static string Module(string? value)
    {
        var module = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (!Filters.ContainsKey(module)) throw new SavedViewValidationException("Módulo não aceita visões salvas.");
        return module;
    }

    public static string Name(string? value)
    {
        var name = (value ?? string.Empty).Trim();
        if (name.Length is < 1 or > 80) throw new SavedViewValidationException("Nome deve possuir entre 1 e 80 caracteres.");
        return name;
    }

    public static string NormalizedName(string name) => name.ToUpperInvariant();

    public static string Json(JsonElement value, string module, bool sort)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new SavedViewValidationException(sort ? "Ordenação deve ser um objeto JSON." : "Filtros devem ser um objeto JSON.");
        var allowed = sort ? SortKeys : Filters[module];
        var unknown = value.EnumerateObject().Select(x => x.Name).FirstOrDefault(x => !allowed.Contains(x));
        if (unknown is not null) throw new SavedViewValidationException($"Campo não permitido: {unknown}.");
        var json = value.GetRawText();
        if (Encoding.UTF8.GetByteCount(json) > MaxJsonBytes) throw new SavedViewValidationException("JSON excede o limite de 16 KB.");
        return json;
    }

    private static HashSet<string> Set(params string[] values) => new(values, StringComparer.OrdinalIgnoreCase);
}
