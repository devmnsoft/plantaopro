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

    /// <summary>
    /// Fatia b2: partials compartilhados + Ajuda + identidades/perfis/permissões + white label +
    /// pontos soltos. Cada par (view, trecho) é o texto antigo exato que NÃO pode mais renderizar.
    /// Os keeps por audiência (zonas isGlobal, valores Model.Tenant*, claims, marketing) ficam no
    /// r38a0 e não entram aqui de propósito.
    /// </summary>
    [Fact]
    public void B2_ZonasInstitucionais_SemTenantNoTextoRenderizado()
    {
        var pares = new (string View, string Trecho)[]
        {
            ("Shared\\_ActionBar.cshtml", "conforme perfil, tenant"),
            ("Shared\\_AutocompleteField.cshtml", "registros do tenant"),
            ("Shared\\_ContextBar.cshtml", "limitados ao tenant"),
            ("Shared\\_ContextBar.cshtml", "\"Global\" : \"Tenant\")"),
            ("Shared\\_PremiumLockCard.cshtml", "permissão e tenant"),
            ("Shared\\_ScreenGuide.cshtml", "segregação por tenant"),
            ("Shared\\_ScreenGuide.cshtml", "permissão no tenant atual"),
            ("Shared\\_ScreenGuide.cshtml", "usuários do tenant"),
            ("Shared\\_ScreenGuide.cshtml", "do tenant contratado"),
            ("Ajuda\\Index.cshtml", "consultar outro tenant"),
            ("Ajuda\\PerguntasFrequentes.cshtml", "contexto do tenant"),
            ("Ajuda\\PerguntasFrequentes.cshtml", "próprio tenant"),
            ("Ajuda\\PrimeirosPassos.cshtml", "sujeitos a tenant"),
            ("Ajuda\\PrimeirosPassos.cshtml", "do seu tenant"),
            ("Usuarios\\Form.cshtml", "dentro do tenant atual"),
            ("Usuarios\\Form.cshtml", "isolados neste tenant"),
            ("Usuarios\\Index.cshtml", "de outro tenant"),
            ("Usuarios\\Index.cshtml", "o tenant selecionado"),
            ("Usuarios\\Perfis.cshtml", "perfis do tenant"),
            ("Perfis\\Form.cshtml", "Acesso por tenant"),
            ("Perfis\\Index.cshtml", "Segurança por tenant"),
            ("Permissoes\\Matriz.cshtml", "plano e tenant"),
            ("Permissoes\\Matriz.cshtml", "módulo e tenant"),
            ("Permissoes\\TestarAcesso.cshtml", "plano e tenant"),
            ("Permissoes\\TestarAcesso.cshtml", "módulo e tenant"),
            ("Parametrizacoes\\Financeiro.cshtml", "por tenant"),
            ("Parametrizacoes\\Lgpd.cshtml", "por tenant"),
            ("Parametrizacoes\\Notificacoes.cshtml", "por tenant"),
            ("Parametrizacoes\\Operacional.cshtml", "por tenant"),
            ("Parametrizacoes\\WhiteLabel.cshtml", "por tenant"),
            ("WhiteLabel\\Assets.cshtml", "do tenant"),
            ("WhiteLabel\\Edit.cshtml", "do tenant"),
            ("WhiteLabel\\Emails.cshtml", "do tenant"),
            ("WhiteLabel\\Index.cshtml", "do tenant"),
            ("WhiteLabel\\Preview.cshtml", "do tenant"),
            ("Configuracoes\\Index.cshtml", "parâmetros do tenant"),
            ("Configuracoes\\Index.cshtml", "o tenant esteja"),
            ("Integracoes\\ApiKeys.cshtml", "chaves por tenant"),
            ("Financeiro\\Details.cshtml", "no tenant selecionado"),
            ("Relatorios\\Index.cshtml", "Seu tenant"),
            ("Relatorios\\Index.cshtml", "do tenant"),
            ("Relatorios\\Index.cshtml", "e tenant"),
            ("Account\\AccessDenied.cshtml", "tenant atual"),
            ("Cadastro\\Sucesso.cshtml", "provisiona tenant"),
            ("Cadastro\\Sucesso.cshtml", "onboarding"),
            ("HospitalArea\\Index.cshtml", "outro tenant"),
            ("Administrativo360\\ContratoDetalhes.cshtml", "neste tenant"),
            ("Administrativo360\\DocumentosXml\\Sincronizacao.cshtml", "escopo de tenant"),
            ("Administrativo360\\DocumentosXml\\Triagens.cshtml", "do tenant"),
            ("Administrativo360\\DocumentosXml\\Triagens.cshtml", "neste tenant"),
            ("Administrativo360\\DocumentosXml\\Index.cshtml", "DOCTYPE"),
            ("Administrativo360\\DocumentosXml\\Index.cshtml", "XXE"),
        };

        var vistos = new System.Collections.Generic.HashSet<string>();
        foreach (var (view, trecho) in pares)
        {
            vistos.Add(view);
            var v = ReadView(view);
            Assert.DoesNotContain(trecho, v, StringComparison.Ordinal);
        }

        // Amostra de positivos: as substituições existem, não foram apagadas.
        Assert.Contains("Instituição\")", ReadView("Shared\\_ContextBar.cshtml"), StringComparison.Ordinal);
        Assert.Contains("ambiente separado", ReadView("Ajuda\\PerguntasFrequentes.cshtml"), StringComparison.Ordinal);
        Assert.Contains("lista de verificação inicial", ReadView("Cadastro\\Sucesso.cshtml"), StringComparison.Ordinal);
    }
}
