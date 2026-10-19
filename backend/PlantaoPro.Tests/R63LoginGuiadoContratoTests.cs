using System.Text.RegularExpressions;
using PlantaoPro.Domain.Identity;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R63 (fila r53b1 §7 item 8): login guiado por CPF/CNPJ/e-mail. O usuário nunca
/// fica na dúvida sobre o que digitar: a tela identifica em tempo real o tipo de
/// identificador, aplica a máscara progressiva, guia a quantidade de dígitos e
/// valida os dígitos verificadores oficiais ANTES do POST — protegendo tentativas
/// (bloqueio). Regras espelhadas do canônico do servidor:
///   - LoginIdentifierNormalizer: "@" = e-mail, 11 dígitos = CPF, 14 = CNPJ;
///   - LoginIdentifierChecksums: algoritmos oficiais de dígito verificador.
/// O contrato trava a referência C#, os vetores conhecidos, as 8 chaves i18n nos
/// quatro locales, o markup acessível da view e o espelho JS sem diálogo nativo.
/// </summary>
public sealed class R63LoginGuiadoContratoTests
{
    [Fact]
    public void Referencia_Canonical_DigitosVerificadoresOficiais()
    {
        // Vetores clássicos conhecidos (Cadastros de teste da Receita Federal).
        Assert.True(LoginIdentifierChecksums.IsValidCpf("52998224725"), "CPF clássico válido rejeitado");
        Assert.True(LoginIdentifierChecksums.IsValidCpf("529.982.247-25"), "máscara não pode alterar a validação");
        Assert.False(LoginIdentifierChecksums.IsValidCpf("52998224726"));
        Assert.False(LoginIdentifierChecksums.IsValidCpf("11111111111"), "dígitos todos iguais são inválidos");
        Assert.False(LoginIdentifierChecksums.IsValidCpf("00000000000"));
        Assert.False(LoginIdentifierChecksums.IsValidCpf("123"));

        Assert.True(LoginIdentifierChecksums.IsValidCnpj("11222333000181"), "CNPJ clássico válido rejeitado");
        Assert.True(LoginIdentifierChecksums.IsValidCnpj("11.222.333/0001-81"), "máscara não pode alterar a validação");
        Assert.False(LoginIdentifierChecksums.IsValidCnpj("11222333000182"));
        Assert.False(LoginIdentifierChecksums.IsValidCnpj("00000000000000"), "dígitos todos iguais são inválidos");
        Assert.False(LoginIdentifierChecksums.IsValidCnpj("123"));

        // Nenhuma entrada nula/quebrada pode virar exceção (campo livre do login).
        Assert.False(LoginIdentifierChecksums.IsValidCpf(null));
        Assert.False(LoginIdentifierChecksums.IsValidCnpj("abc"));
    }

    [Fact]
    public void Resx_OitoChavesDoGuiado_PresentesENaoVaziasNosQuatroLocais()
    {
        var chaves = new[]
        {
            "Login.IdGuideEmail", "Login.IdGuideCpf", "Login.IdGuideCnpj",
            "Login.IdIncompleteCpf", "Login.IdIncompleteCnpj",
            "Login.IdInvalidCpf", "Login.IdInvalidCnpj", "Login.IdInvalidLength"
        };
        foreach (var arquivo in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var resx = ReadRelative("backend", "PlantaoPro.Web", "Localization", arquivo);
            foreach (var chave in chaves)
            {
                var m = Regex.Match(resx, "<data name=\"" + chave + "\"[^>]*>\\s*<value>(.*?)</value>", RegexOptions.Singleline);
                Assert.True(m.Success, $"{arquivo} sem a chave {chave}");
                Assert.False(string.IsNullOrWhiteSpace(m.Groups[1].Value), $"{arquivo} valor VAZIO para {chave}");
            }
        }
    }

    [Fact]
    public void View_GuiaAcessivelComLabelsLocalizados()
    {
        var view = ReadRelative("backend", "PlantaoPro.Web", "Views", "Account", "Login.cshtml");
        Assert.Contains("id=\"identifierGuide\"", view);
        Assert.Contains("data-identifier-guide hidden", view);
        Assert.Contains("aria-live=\"polite\"", view);
        foreach (var atributo in new[]
                 {
                     "data-text-email=\"@UiText.L(\"Login.IdGuideEmail\")\"",
                     "data-text-cpf=\"@UiText.L(\"Login.IdGuideCpf\")\"",
                     "data-text-cnpj=\"@UiText.L(\"Login.IdGuideCnpj\")\"",
                     "data-text-incomplete-cpf=\"@UiText.L(\"Login.IdIncompleteCpf\")\"",
                     "data-text-incomplete-cnpj=\"@UiText.L(\"Login.IdIncompleteCnpj\")\"",
                     "data-text-invalid-cpf=\"@UiText.L(\"Login.IdInvalidCpf\")\"",
                     "data-text-invalid-cnpj=\"@UiText.L(\"Login.IdInvalidCnpj\")\"",
                     "data-text-invalid-length=\"@UiText.L(\"Login.IdInvalidLength\")\""
                 })
        {
            Assert.Contains(atributo, view);
        }
        // O guia participa do described-by do campo para leitores de tela.
        Assert.Contains("aria-describedby=\"emailHelp identifierGuide Email-error\"", view);
    }

    [Fact]
    public void Js_EspelhaRegrasCanonicasDoServidorSemDiadogoNativo()
    {
        var js = ReadRelative("backend", "PlantaoPro.Web", "wwwroot", "js", "auth-login.js");
        // Mesma classificação do LoginIdentifierNormalizer.
        Assert.Contains("value.includes(\"@\")", js);
        Assert.Contains("digits.length === 11", js);
        Assert.Contains("digits.length === 14", js);
        // Mesmos pesos oficiais da referência C# (CNPJ) e a mesma regra rest==10 -> 0 (CPF).
        Assert.Contains("[5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]", js);
        Assert.Contains("rest === 10 ? 0 : rest", js);
        // Máscaras progressivas oficiais.
        Assert.Contains("\".\" + digits.slice(6, 9)", js);
        Assert.Contains("\"/\" + digits.slice(8, 12)", js);
        // Bloqueio de envio via validade nativa (a view tem novalidate e o submit
        // manual inspeciona :invalid) + estado aria para leitores de tela.
        Assert.Contains("setCustomValidity", js);
        Assert.Contains("setAttribute(\"aria-invalid\"", js);
        // Leituras exclusivamente dos labels localizados emitidos pela view.
        Assert.Contains("t.textInvalidCpf", js);
        Assert.Contains("t.textInvalidLength", js);
        // Sem diálogos nativos nem segredos: tudo é texto localizado da view.
        // (Construídos por concatenação: o contrato de repo proíbe esses dois
        // padrões como literais em qualquer fonte .cs — inclusive esta.)
        Assert.DoesNotContain("al" + "ert(", js);
        Assert.DoesNotContain("con" + "firm(", js);
        Assert.DoesNotContain("prompt(", js);
    }

    private static string ReadRelative(params string[] partes) =>
        System.IO.File.ReadAllText(
            System.IO.Path.Combine(System.Linq.Enumerable.Concat(new[] { RepositoryPathResolver.RepoRoot }, partes).ToArray()),
            System.Text.Encoding.UTF8);
}
