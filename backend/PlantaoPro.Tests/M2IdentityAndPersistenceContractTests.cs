using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Domain.Identity;

namespace PlantaoPro.Tests;

public sealed class M2IdentityAndPersistenceContractTests
{
    [Theory]
    [InlineData(" Pessoa@Example.COM ", LoginIdentifierKind.Email, "pessoa@example.com")]
    [InlineData("123.456.789-01", LoginIdentifierKind.Cpf, "12345678901")]
    [InlineData("12.345.678/0001-90", LoginIdentifierKind.Cnpj, "12345678000190")]
    public void Login_identifier_contract_is_preserved(string input, LoginIdentifierKind kind, string normalized)
    {
        Assert.Equal(kind, LoginIdentifierNormalizer.Classify(input));
        Assert.Equal(normalized, LoginIdentifierNormalizer.Normalize(input, kind));
        Assert.DoesNotContain(normalized, LoginIdentifierNormalizer.AuditValue(input, kind));
    }

    [Fact]
    public void Eligibility_requires_active_user_and_unambiguous_credentials()
    {
        Assert.True(IdentityEligibilityPolicy.IsActive("A", "ATIVO"));
        Assert.False(IdentityEligibilityPolicy.IsActive("I", "ATIVO"));
        Assert.False(IdentityEligibilityPolicy.IsActive("A", "INATIVO"));
        Assert.True(IdentityEligibilityPolicy.HasExactlyOneCredentialMatch(1));
        Assert.False(IdentityEligibilityPolicy.HasExactlyOneCredentialMatch(0));
        Assert.False(IdentityEligibilityPolicy.HasExactlyOneCredentialMatch(2));
    }

    [Theory]
    [InlineData(nameof(Fase6BiIntegracoesController.ConfigurarWidgets))]
    [InlineData(nameof(Fase6BiIntegracoesController.SalvarFiltro))]
    [InlineData(nameof(Fase6BiIntegracoesController.RotacionarApiKey))]
    [InlineData(nameof(Fase6BiIntegracoesController.Reenviar))]
    [InlineData(nameof(Fase6BiIntegracoesController.PublicPostAgendamento))]
    public void Incomplete_commands_are_explicitly_unavailable(string action)
    {
        var method = typeof(Fase6BiIntegracoesController).GetMethod(action)!;
        Assert.NotNull(method);
        var source = File.ReadAllText(Path.Combine(RepositoryRoot(), "backend/PlantaoPro.Api/Controllers/Fase6BiIntegracoesController.cs"));
        var declaration = source.Split('\n').Single(line => line.Contains($" {action}(", StringComparison.Ordinal));
        Assert.Contains("Indisponivel(", declaration);
        Assert.DoesNotContain("Ok(ApiResponse", declaration);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, ".git"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }
}
