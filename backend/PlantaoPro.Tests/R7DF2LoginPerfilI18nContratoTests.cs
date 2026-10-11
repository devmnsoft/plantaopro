using System;
using System.IO;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R7-D-F2: primeira e última impressão (login falho, logout, recuperação e reset de
/// senha, refresh de contexto) em recursos localizados + padronização dos nomes das
/// chaves Profile.* para inglês. Contrato de fonte (mesmo padrão F1/R7-E1): os literais
/// pt-BR não podem voltar ao controller/views e os nomes legados das chaves somem.
/// </summary>
public sealed class R7DF2LoginPerfilI18nContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();

    private static string Read(params string[] partes) => File.ReadAllText(Path.Combine(new[] { Root }.Concat(partes).ToArray()));

    [Fact]
    public void AccountController_MensagensDeSessaoESenha_UseRecursosENaoCarregaLiteraisPtBr()
    {
        var cs = Read("backend", "PlantaoPro.Web", "Controllers", "AccountController.cs");

        foreach (var chave in new[]
                 {
                     "Auth.ErrorUserInactive", "Auth.ErrorUserLocked", "Auth.ErrorInvalidCredentials",
                     "Auth.ErrorAuthFailedWithRef", "Auth.LoginUnexpected", "Auth.LoginTimeout",
                     "Auth.LoginConnection", "Auth.SessionEnded", "Auth.ForgotSent",
                     "Auth.ResetSuccess", "Auth.ResetTokenInvalid", "Auth.ContextRefreshed",
                     "Auth.ContextExpired", "Auth.ContextRefreshFailed"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", cs);
        }

        foreach (var literal in new[]
                 {
                     "Seu usuário está inativo. Procure o administrador.",
                     "Usuário bloqueado temporariamente.",
                     "Identificador ou senha inválidos.",
                     "Não foi possível autenticar. Tente novamente. Referência:",
                     "Não foi possível realizar o login no momento.",
                     "A autenticação excedeu o tempo de resposta",
                     "Não foi possível conectar ao serviço de autenticação",
                     "Sessão encerrada com sucesso.",
                     "Se o e-mail estiver cadastrado, enviaremos instruções para recuperação.",
                     "Senha redefinida com sucesso.",
                     "Token inválido ou expirado.",
                     "Contexto e módulos atualizados com sucesso.",
                     "Sua sessão expirou ou foi revogada. Entre novamente para continuar.",
                     "Não foi possível atualizar seu acesso. Entre novamente para continuar com segurança."
                 })
        {
            Assert.DoesNotContain(literal, cs);
        }
    }

    [Fact]
    public void ViewsDeRecuperacaoESenha_UseRecursosENaoCarregaLiteraisPtBr()
    {
        var forgot = Read("backend", "PlantaoPro.Web", "Views", "Account", "ForgotPassword.cshtml");
        foreach (var chave in new[]
                 {
                     "Auth.ForgotTitle", "Auth.ForgotBack", "Auth.ForgotEyebrow", "Auth.ForgotHeading",
                     "Auth.ForgotIntro", "Auth.ForgotProtectionTitle", "Auth.ForgotProtectionBody",
                     "Auth.ForgotEmailLabel", "Auth.ForgotEmailHelp", "Auth.ForgotSubmit"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", forgot);
        }

        var reset = Read("backend", "PlantaoPro.Web", "Views", "Account", "ResetPassword.cshtml");
        foreach (var chave in new[]
                 {
                     "Auth.ResetTitle", "Auth.ResetEyebrow", "Auth.ResetHeading", "Auth.ResetIntro",
                     "Auth.ResetNoReuseTitle", "Auth.ResetNoReuseBody", "Auth.ResetNewPasswordLabel",
                     "Auth.ResetNewPasswordHelp", "Auth.ResetConfirmLabel", "Auth.ResetSubmit"
                 })
        {
            Assert.Contains($"UiText.L(\"{chave}\")", reset);
        }

        foreach (var literal in new[] { "Recupere seu acesso", "Recuperação segura", "Proteção da sua conta", "Enviar instruções" })
            Assert.DoesNotContain(literal, forgot);

        foreach (var literal in new[] { "Crie uma senha segura", "Nova credencial", "Não reutilize senhas", "Confirme a nova senha" })
            Assert.DoesNotContain(literal, reset);
    }

    [Fact]
    public void ChavesProfile_PadronizadasParaInglês_EmTodosOsQuatroIdiomas()
    {
        foreach (var arquivo in new[] { "UiTexts.resx", "UiTexts.en.resx", "UiTexts.es.resx", "UiTexts.fr.resx" })
        {
            var resx = Read("backend", "PlantaoPro.Web", "Localization", arquivo);
            Assert.Contains("<data name=\"Profile.Name\"", resx, StringComparison.Ordinal);
            Assert.Contains("<data name=\"Profile.Phone\"", resx, StringComparison.Ordinal);
            Assert.Contains("<data name=\"Profile.Notifications\"", resx, StringComparison.Ordinal);
            Assert.DoesNotContain("<data name=\"Profile.Nome\"", resx, StringComparison.Ordinal);
            Assert.DoesNotContain("<data name=\"Profile.Telefone\"", resx, StringComparison.Ordinal);
            Assert.DoesNotContain("<data name=\"Profile.Notificacoes\"", resx, StringComparison.Ordinal);
        }
    }
}
