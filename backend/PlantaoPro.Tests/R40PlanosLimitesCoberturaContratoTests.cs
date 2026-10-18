using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R40 (diretriz r24a1, epic E9 — planos e limites por plano): o guard de limite de usuários do plano
/// existia (PodeCadastrarUsuarioAsync) mas nenhum endpoint o chamava; médicos e hospitais já aplicavam.
/// Fato 1 trava que POST api/seguranca/usuarios aplica o guard ANTES de salvar e registra o uso depois;
/// fato 2 trava que a antiga mensagem com "tenant" ("no tenant permitido") não volta a existir em nenhum
/// .cs do backend (8 pontos reescritos para "no escopo de acesso"). Plano:
/// docs/evidencias/2026-10-18-rodada-40/r40a0-e9-planos-limites-mapa-cobertura.md.
/// </summary>
public class R40PlanosLimitesCoberturaContratoTests
{
    [Fact]
    public void CriarUsuario_AplicaLimiteDoPlanoAntesDeSalvarERegistraUso()
    {
        var p = Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Api", "Controllers", "SegurancaController.cs");
        var texto = File.ReadAllText(p, Encoding.UTF8);
        var idx = texto.IndexOf("CriarUsuario", StringComparison.Ordinal);
        Assert.True(idx >= 0, "endpoint CriarUsuario não encontrado");
        var corpo = texto.Substring(idx, Math.Min(1200, texto.Length - idx));
        Assert.Contains("PodeCadastrarUsuarioAsync", corpo, StringComparison.Ordinal);
        Assert.Contains("RegistrarUsoAsync", corpo, StringComparison.Ordinal);
        Assert.Contains("\"USUARIOS\"", corpo, StringComparison.Ordinal);
        var iGuard = corpo.IndexOf("PodeCadastrarUsuarioAsync", StringComparison.Ordinal);
        var iSalvar = corpo.IndexOf("SalvarUsuarioAsync", StringComparison.Ordinal);
        Assert.True(iGuard >= 0 && iSalvar >= 0 && iGuard < iSalvar, "o guard precisa rodar antes do SalvarUsuarioAsync");
    }

    [Fact]
    public void NenhumaMensagemDoBackend_CitaTenantPermitido()
    {
        var raiz = Path.Combine(RepositoryPathResolver.RepoRoot, "backend");
        var ofensores = new List<string>();
        foreach (var arquivo in Directory.GetFiles(raiz, "*.cs", SearchOption.AllDirectories))
        {
            if (arquivo.Contains("obj\\", StringComparison.Ordinal) || arquivo.Contains("bin\\", StringComparison.Ordinal)) continue;
            if (arquivo.Contains("PlantaoPro.Tests", StringComparison.Ordinal)) continue; // o pin do termo vive nos próprios testes
            var texto = File.ReadAllText(arquivo, Encoding.UTF8);
            if (texto.IndexOf("no tenant permitido", StringComparison.Ordinal) >= 0) ofensores.Add(arquivo);
        }
        Assert.Empty(ofensores);
    }
}
