using System.Text;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R38 (diretriz r24a1, epic E2 — "tenant visível apenas para super-admin"): a palavra "tenant"
/// chegava ao usuário da instituição em três grupos de telas de uso institucional:
/// (A) jornada clínica (Cid/Consultas/Prescricoes — 20 views com o mesmo subtítulo),
/// (B) Central de Segurança (11 views com o mesmo aviso, que ainda citava endpoints crus) e
/// (C) capa da Central de Segurança ("multi-tenant" + KPI "Sem tenant").
/// Reescritos em linguagem simples ("sua instituição"); triagem completa por audiência no
/// docs/evidencias/2026-10-18-rodada-38/r38a0-e2-tenant-visibilidade-triagem.md. A classe é
/// estendida pela fatia b2 (partials compartilhados + Ajuda + pontos soltos).
/// </summary>
public class R38TenantVisibilidadeInstituicaoContratoTests
{
    private static readonly string[] ViewsClinicas =
    {
        "Cid\\Create.cshtml", "Cid\\Details.cshtml", "Cid\\Edit.cshtml", "Cid\\Favoritos.cshtml",
        "Cid\\Importar.cshtml", "Cid\\Index.cshtml", "Cid\\MaisUsados.cshtml",
        "Consultas\\Create.cshtml", "Consultas\\Details.cshtml", "Consultas\\Edit.cshtml",
        "Consultas\\HistoricoPaciente.cshtml", "Consultas\\Imprimir.cshtml", "Consultas\\Resumo.cshtml",
        "Prescricoes\\Create.cshtml", "Prescricoes\\Details.cshtml", "Prescricoes\\Edit.cshtml",
        "Prescricoes\\HistoricoPaciente.cshtml", "Prescricoes\\Imprimir.cshtml", "Prescricoes\\Index.cshtml", "Prescricoes\\Modelos.cshtml",
    };

    private static readonly string[] ViewsSeguranca =
    {
        "Seguranca\\AcessosNegados.cshtml", "Seguranca\\Auditoria.cshtml", "Seguranca\\Matriz.cshtml",
        "Seguranca\\PerfilDetalhes.cshtml", "Seguranca\\Perfis.cshtml", "Seguranca\\Permissoes.cshtml",
        "Seguranca\\PoliticasSenha.cshtml", "Seguranca\\Sessoes.cshtml", "Seguranca\\TentativasLogin.cshtml",
        "Seguranca\\UsuarioDetalhes.cshtml", "Seguranca\\Usuarios.cshtml",
    };

    private static string ReadView(string view) => File.ReadAllText(
        Path.Combine(new[] { RepositoryPathResolver.RepoRoot }.Concat(
            new[] { "backend", "PlantaoPro.Web", "Views", view }).ToArray()), Encoding.UTF8);

    [Fact]
    public void JornadaClinica_SemTenantNoSubtitulo()
    {
        foreach (var view in ViewsClinicas)
        {
            var v = ReadView(view);
            Assert.DoesNotContain("com tenant, perfil, auditoria", v, StringComparison.Ordinal);
            Assert.Contains("com escopo da sua instituição, auditoria LGPD", v, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Seguranca_AvisoSemTenantESemEndpointCru()
    {
        foreach (var view in ViewsSeguranca)
        {
            var v = ReadView(view);
            Assert.DoesNotContain("respeitando tenant", v, StringComparison.Ordinal);
            Assert.DoesNotContain("pelos endpoints", v, StringComparison.Ordinal);
            Assert.Contains("limitados à sua instituição e respeitam o perfil e as permissões", v, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CapaDaSeguranca_SemMultiTenant()
    {
        var v = ReadView("Seguranca\\Index.cshtml");
        Assert.DoesNotContain("multi-tenant", v, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Sem tenant", v, StringComparison.Ordinal);
        Assert.Contains("auditoria da instituição", v, StringComparison.Ordinal);
        Assert.Contains("Sem instituição", v, StringComparison.Ordinal);
    }
}
