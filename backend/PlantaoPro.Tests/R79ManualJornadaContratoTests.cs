using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PlantaoPro.Web.Controllers;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R79 - Fechamento do bloco B3: contrato de cobertura da jornada critica e busca da
/// Ajuda cobrindo os termos das telas. (1) Toda etapa da jornada assistencial + nucleo
/// administrativo + onboarding tem artigo correspondente (lista literal travada; crescer
/// a jornada exige atualizar esta lista com evidencia). (2) A busca localiza cada artigo
/// pelo nome da tela alvo (controlador/acao entram no indice de busca - Regra R79 em
/// AjudaController.PaginaDoArtigo). O zero geral de gap segue mediado por
/// R75InventarioManualContratoTests (GapMaximoTravado) ate a fila restante ser consumida.
/// Plano: docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md (R79).
/// </summary>
public sealed class R79ManualJornadaContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRepositoryRoot();

    private static string Read(string relativo) => File.ReadAllText(Path.Combine(Root, relativo.Replace('/', Path.DirectorySeparatorChar)));

    // Jornada critica sem artigo = quebra do manual: clinica ponta a ponta, nucleo
    // administrativo/fiscal, assinatura self-service, onboarding e a propria ajuda.
    private static readonly string[] JornadaCritica =
    {
        "pacientes/buscar", "pacientes/create", "pacientes/details", "pacientes/prontuario",
        "agendamentos/index", "agendamentos/create", "agendamentos/checkin",
        "painelchamada/index",
        "triagem/fila", "triagem/create", "triagem/finalizar",
        "consultas/index", "consultas/atendimento",
        "prescricoes/index", "prescricoes/editor", "prescricoes/imprimir",
        "faturamentoclinico/index",
        "administrativo360/index", "administrativo360/cadastros", "administrativo360/produtos",
        "administrativo360/inventarios", "administrativo360/movimentacoes",
        "administrativo360/titulospagar", "administrativo360/novadespesa",
        "administrativo360/fluxocaixa", "administrativo360/vendas", "administrativo360/fechamentocaixa",
        "fechamentos/index", "fechamentos/conferencia",
        "adm360documentosxmlweb/sincronizacao",
        "usuarios/index", "perfis/index",
        "minhaassinatura/index", "minhaassinatura/faturas",
        "usuario/alterarsenha",
        "notificacoes/index",
        "cadastro/empresa", "cadastro/plano", "cadastro/usuario", "cadastro/confirmacao",
        "ajuda/index"
    };

    private static List<(string Controller, string Acao)> LerArtigosDaFonte()
    {
        var fonte = Read("backend/PlantaoPro.Web/Controllers/AjudaController.cs");
        var re = new Regex("Artigo\\(\"[^\"]+\",\\s*\"[^\"]+\",\\s*\"[^\"]+\",\\s*\"[^\"]+\",\\s*\"[^\"]*\"?,\\s*\"([^\"]+)\",\\s*\"([^\"]+)\"");
        return re.Matches(fonte)
            .Select(m => (m.Groups[1].Value, m.Groups[2].Value))
            .Distinct()
            .ToList();
    }

    [Fact]
    public void Jornada_critica_tem_artigo_em_cada_etapa()
    {
        var artigos = LerArtigosDaFonte();
        Assert.True(artigos.Count > 0, "nenhum artigo reconhecido em AjudaController - padrao de parser defasado");

        var cobertos = new HashSet<string>(
            artigos.Select(a => a.Controller + "/" + a.Acao), StringComparer.OrdinalIgnoreCase);

        var faltando = JornadaCritica.Where(p => !cobertos.Contains(p)).ToList();
        Assert.True(faltando.Count == 0, "etapa(s) da jornada critica sem artigo: " + string.Join(", ", faltando));
    }

    [Fact]
    public void Busca_localiza_cada_artigo_pelo_nome_da_tela()
    {
        var artigos = LerArtigosDaFonte();
        var controlador = new AjudaController(NullLogger<AjudaController>.Instance);

        foreach (var (controller, acao) in artigos)
        {
            // Termo que o usuario digita ao procurar pela tela onde esta parado.
            var termo = string.Equals(acao, "Index", StringComparison.OrdinalIgnoreCase) ? controller : acao;
            var resultado = controlador.Index(termo, "TODOS");
            var model = ((ViewResult)resultado).Model as PlantaoPro.Web.Models.AjudaInterativaIndexViewModel;
            Assert.NotNull(model);
            var achou = model.Artigos.Any(a =>
                string.Equals(a.Controller, controller, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(a.Action, acao, StringComparison.OrdinalIgnoreCase));
            Assert.True(achou, "busca por \"" + termo + "\" nao localiza o artigo da tela " + controller + "/" + acao);
        }
    }
}
