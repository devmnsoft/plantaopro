using System.Globalization;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R51 (roadmap r24a1): Ajuda central — "como usar" por área (extensão do E3).
/// Scanner de fonte sobre o <c>AjudaController</c> que trava: (1) o tópico da instituição para a
/// família administrador-cliente, (2) o conteúdo TODOS de fatura vencida e notificações e (3) a
/// normalização ADMINISTRADOR → ADMINISTRADOR_CLIENTE (D-R51-1).
/// Plano: docs/evidencias/2026-10-18-rodada-51/r51a0-ajuda-central-por-area-plano.md.
/// </summary>
public sealed class R51AjudaContratoTests
{
    private static string ControllerPath => System.IO.Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web", "Controllers", "AjudaController.cs");

    private static string Ler() => System.IO.File.ReadAllText(ControllerPath, System.Text.Encoding.UTF8);

    [Fact]
    public void TopicoInstituicao_CobrePerfilAdministradorCliente()
    {
        var t = Ler();
        Assert.Contains("Slug = \"instituicao\"", t);
        Assert.Contains("Perfil = \"ADMINISTRADOR_CLIENTE\", Titulo = \"Operação da instituição\"", t);
        var artigosCliente = 0;
        var i = 0;
        while ((i = t.IndexOf("Artigo(\"", i, StringComparison.Ordinal)) >= 0)
        {
            var fim = t.IndexOf(')', i);
            var bloco = fim > i ? t.Substring(i, fim - i) : string.Empty;
            if (bloco.Contains("\"ADMINISTRADOR_CLIENTE\"")) artigosCliente++;
            i += 8;
        }
        Assert.True(artigosCliente >= 5, "artigos ADMINISTRADOR_CLIENTE esperados >= 5, encontrados " + artigosCliente);
    }

    [Fact]
    public void TopicoTodos_CobreFaturaVencidaENotificacoes()
    {
        var t = Ler();
        Assert.Contains("Slug = \"central\"", t);
        Assert.Contains("central-fatura", t);
        Assert.Contains("central-notificacoes", t);
        Assert.Contains("Como resolver fatura vencida", t);
        Assert.Contains("Fatura vencida", t, StringComparison.Ordinal); // texto exibível em linguagem simples, sem jargão
    }

    [Fact]
    public void FamiliaAdministrador_CompartilhaManual()
    {
        var t = Ler();
        // D-R51-1: ADMINISTRADOR (prioridade 650) lê o mesmo manual de ADMINISTRADOR_CLIENTE (700).
        Assert.Contains("if (string.Equals(normalizado, \"ADMINISTRADOR\", StringComparison.OrdinalIgnoreCase)) normalizado = \"ADMINISTRADOR_CLIENTE\";", t);
    }
}
