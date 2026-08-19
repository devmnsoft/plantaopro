using System.Text.Json;
using PlantaoPro.Api.SavedViews;

namespace PlantaoPro.Tests;

public sealed class V192SavedViewValidationTests
{
    [Fact]
    public void Filters_AcceptsWhitelistedKeys()
    {
        using var json = JsonDocument.Parse("""{"status":"ABERTO","hospital":"x","inicio":"2026-08-19"}""");
        Assert.Equal(json.RootElement.GetRawText(), SavedViewValidation.Json(json.RootElement, "PLANTOES", false));
    }

    [Fact]
    public void Filters_RejectsUnknownKeysInsteadOfPassingThemToSql()
    {
        using var json = JsonDocument.Parse("""{"status":"ABERTO","sql":"drop table"}""");
        var error = Assert.Throws<SavedViewValidationException>(() => SavedViewValidation.Json(json.RootElement, "PLANTOES", false));
        Assert.Contains("sql", error.Message);
    }

    [Fact]
    public void ModuleAndName_AreCanonicalAndBounded()
    {
        Assert.Equal("PRODUTIVIDADE", SavedViewValidation.Module(" produtividade "));
        Assert.Equal("MINHA VISÃO", SavedViewValidation.NormalizedName("Minha visão"));
        Assert.Throws<SavedViewValidationException>(() => SavedViewValidation.Name(new string('x', 81)));
    }
}
