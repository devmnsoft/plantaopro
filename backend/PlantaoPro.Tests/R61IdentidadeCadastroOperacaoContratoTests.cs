using System.Text;
using System.Text.RegularExpressions;
using PlantaoPro.Web.Services;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R61 (fila r53b1 §7 item 7 — epic E7 cadastro×operação): identidade única das telas e design
/// das mensagens/formulários. D-R61-1: toda página tem h1/trilha pt-BR garantido (a topbar lê
/// ViewData["Title"]; sem ele o BreadcrumbService cai em Humanize(action) e exibe "Index"/
/// "Details" em inglês — os 61 pares auditados estão travados com o título exato; fonte das
/// inserções: docs/evidencias/2026-10-19-rodada-61/r61a1_titles.ps1). D-R61-2: nenhuma mensagem
/// com conotação de homologação/sandbox fora do whitelist de domínio SEFAZ. D-R61-3: todo input
/// de formulário tem nome acessível (r61_forms_aria.ps1). Scanner de fonte (padrão de casa).
/// </summary>
public sealed class R61IdentidadeCadastroOperacaoContratoTests
{
    private static string WebRoot => Path.Combine(RepositoryPathResolver.RepoRoot, "backend", "PlantaoPro.Web");
    private static string ViewsRoot => Path.Combine(WebRoot, "Views");

    // Auditoria E7: os 61 pares (controller, ação, título pt-BR exato) que estavam em gap.
    private static readonly (string Ctrl, string Acao, string Titulo)[] ParesTravados =
    {
        ("Agendamentos", "AgendaDia", "Agenda do dia"),
        ("Agendamentos", "AgendaMedico", "Agenda por médico"),
        ("Agendamentos", "Calendario", "Calendário clínico"),
        ("Agendamentos", "Cancelamentos", "Cancelamentos"),
        ("Agendamentos", "Details", "Detalhes do agendamento"),
        ("ClinicaDashboard", "Index", "Painel da clínica"),
        ("ClinicaFinanceiro", "Caixa", "Caixa"),
        ("ClinicaFinanceiro", "ContasReceber", "Contas a receber"),
        ("ClinicaFinanceiro", "FechamentoCaixa", "Fechamento de caixa"),
        ("ClinicaFinanceiro", "Glosas", "Glosas"),
        ("ClinicaFinanceiro", "Index", "Financeiro da clínica"),
        ("ClinicaFinanceiro", "Receber", "Receber"),
        ("ClinicaFinanceiro", "Relatorios", "Relatórios financeiros"),
        ("ClinicaFinanceiro", "Repasses", "Repasses"),
        ("ClinicaUnidades", "Create", "Nova unidade"),
        ("ClinicaUnidades", "Edit", "Editar unidade"),
        ("ClinicaUnidades", "Index", "Unidades"),
        ("Consultas", "Index", "Consultas"),
        ("Convenios", "Autorizacoes", "Autorizações de convênio"),
        ("Convenios", "Contratos", "Contratos de convênio"),
        ("Convenios", "Create", "Novo convênio"),
        ("Convenios", "Dashboard", "Painel de convênios"),
        ("Convenios", "Details", "Detalhes do convênio"),
        ("Convenios", "Edit", "Editar convênio"),
        ("Convenios", "Faturamento", "Faturamento com convênios"),
        ("Convenios", "Glosas", "Glosas de convênio"),
        ("Convenios", "Index", "Convênios"),
        ("Convenios", "Planos", "Planos dos convênios"),
        ("Convenios", "Tabelas", "Tabelas dos convênios"),
        ("Escalas", "Details", "Detalhes da escala"),
        ("Escalas", "Substituir", "Substituição na escala"),
        ("MinhaAssinatura", "Downgrade", "Reduzir plano"),
        ("MinhaAssinatura", "Faturas", "Faturas da assinatura"),
        ("MinhaAssinatura", "Upgrade", "Ampliar plano"),
        ("Pacientes", "Buscar", "Localizar paciente"),
        ("Pacientes", "Details", "Ficha do paciente"),
        ("Pacientes", "Historico", "Histórico do paciente"),
        ("Pacientes", "Prontuario", "Prontuário do paciente"),
        ("Pacientes", "ResumoClinico", "Resumo clínico do paciente"),
        ("PainelChamada", "Configuracoes", "Configurações do painel"),
        ("PainelChamada", "Guiches", "Guichês"),
        ("PainelChamada", "Historico", "Histórico de chamadas"),
        ("PainelChamada", "Salas", "Salas"),
        ("PainelChamada", "Setores", "Setores"),
        ("PainelChamada", "Tv", "Tela TV"),
        ("Parametrizacoes", "Index", "Parâmetros do sistema"),
        ("PendenciasClinicas", "Index", "Pendências clínicas"),
        ("Perfis", "Details", "Detalhes do perfil"),
        ("PlanosSaude", "Autorizacoes", "Autorizações do plano de saúde"),
        ("PlanosSaude", "Coberturas", "Coberturas do plano de saúde"),
        ("PlanosSaude", "Create", "Novo plano de saúde"),
        ("PlanosSaude", "Details", "Detalhes do plano de saúde"),
        ("PlanosSaude", "Edit", "Editar plano de saúde"),
        ("PlanosSaude", "Index", "Planos de saúde"),
        ("PlanosSaude", "Pacientes", "Pacientes do plano de saúde"),
        ("Triagem", "ClassificacaoRisco", "Classificação de risco"),
        ("Triagem", "Create", "Novo atendimento de triagem"),
        ("Triagem", "Details", "Detalhes da triagem"),
        ("Triagem", "Edit", "Editar triagem"),
        ("Triagem", "Fila", "Fila de triagem"),
        ("Triagem", "HistoricoPaciente", "Histórico na triagem"),
    };

    [Fact]
    public void Paginas_TodasComIdentidadePortuguesa()
    {
        // 1) Cada par auditado traz o título pt-BR exato no topo da própria view.
        foreach (var (ctrl, acao, titulo) in ParesTravados)
        {
            var caminho = Path.Combine(ViewsRoot, ctrl, acao + ".cshtml");
            Assert.True(File.Exists(caminho), "view ausente: " + ctrl + "/" + acao);
            var texto = File.ReadAllText(caminho, Encoding.UTF8);
            Assert.True(texto.Contains("ViewData[\"Title\"] = \"" + titulo + "\";"),
                ctrl + "/" + acao + " sem o titulo travado: " + titulo);
        }

        // 2) Nenhum título volta a jargão de ação em inglês ou rascunho.
        var proibido = new Regex(@"\b(Index|Details|Create|Edit|Controller|Action)\b|TODO|FIXME",
            RegexOptions.IgnoreCase);
        var violacoes = ParesTravados
            .Where(t => proibido.IsMatch(t.Titulo))
            .Select(t => t.Ctrl + "/" + t.Acao + " -> " + t.Titulo)
            .ToList();
        Assert.True(violacoes.Count == 0, "títulos com jargão de ação:\n" + string.Join("\n", violacoes));

        // 3) Varredura global: qualquer OUTRA view de página precisa de Title no arquivo OU par no
        // catálogo canônico OU isenção documentada (impressão Layout=null / título do controlador).
        var catalogo = new FeatureCatalogService().Features
            .Select(f => (f.Controller, f.Action))
            .ToList();
        var isenções = new[]
        {
            (Controller: "Administrativo360", Action: "OrcamentoImprimir"), // Layout=null, <title> próprio
            (Controller: "Administrativo360", Action: "ValeImprimir"),      // Layout=null, <title> próprio
            (Controller: "V112", Action: "OperationalPage"),                // V112WebController aporta Title por endpoint
            (Controller: "Pendencias", Action: "Index"),                    // PendenciasController aporta "Central de Ações"
        };

        var gaps = new List<string>();
        foreach (var caminho in Directory.EnumerateFiles(ViewsRoot, "*.cshtml", SearchOption.AllDirectories)
                     .Where(c => !c.Contains(Path.Combine("Views", "Shared"), StringComparison.OrdinalIgnoreCase))
                     .Where(c => !Path.GetFileName(c).StartsWith("_")))
        {
            if (File.ReadAllText(caminho, Encoding.UTF8).Contains("ViewData[\"Title\"]")) continue;
            var relativo = caminho[(ViewsRoot.Length + 1)..].Replace('\\', '/');
            var partes = relativo.Split('/');
            var par = (partes[0], Path.GetFileNameWithoutExtension(caminho));
            if (catalogo.Contains(par) || isenções.Contains(par)) continue;
            gaps.Add($"{par.Item1}/{par.Item2}");
        }

        Assert.True(gaps.Count == 0, "páginas sem identidade pt-BR garantida:\n" + string.Join("\n", gaps.Take(25)));
    }

    [Fact]
    public void Mensagens_SemConotacaoDeHomologacao()
    {
        // D-R61-2: o ambiente de homologação é domínio SEFAZ (código HOMOLOGACAO) — em copy de
        // produto não aparece "sandbox" nem asset nomeado por work-package de homologação.
        var views = Directory.EnumerateFiles(Path.Combine(ViewsRoot), "*.cshtml", SearchOption.AllDirectories);
        var comSandbox = views
            .Where(caminho => Regex.IsMatch(File.ReadAllText(caminho, Encoding.UTF8), "sandbox", RegexOptions.IgnoreCase))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(comSandbox.Count == 0, "copy com 'sandbox' em views:\n" + string.Join(", ", comSandbox));

        foreach (var layout in new[] { "_Layout.cshtml", "_AuthLayout.cshtml" })
        {
            var caminho = Path.Combine(WebRoot, "Views", "Shared", layout);
            var texto = File.ReadAllText(caminho, Encoding.UTF8);
            Assert.True(!texto.ToLowerInvariant().Contains("homologacao"),
                layout + " referencia asset de homologacao");
        }

        var ocorrencias = File.ReadAllText(
            Path.Combine(ViewsRoot, "OperacaoAssistida", "Ocorrencias.cshtml"), Encoding.UTF8);
        Assert.Equal(2, CountOcorrencias(ocorrencias, "Ocorrências da Operação Assistida"));
        Assert.True(!ocorrencias.Contains("Homologação"),
            "Ocorrencias.cshtml voltou a usar 'Homologação' em vez do termo de domínio");
    }

    [Fact]
    public void Formularios_TodosInputsComNomeAcessivel()
    {
        // D-R61-3 (regra idêntica ao recon r61a0 e ao r61_forms_aria.ps1): input de formulário
        // sem aria-label/aria-labelledby, sem asp-for, sem type de controle não-textual, sem
        // label[for] no arquivo e sem label envolvente na janela de 220 chars é órfão.
        var orfaos = new List<string>();
        foreach (var caminho in Directory.EnumerateFiles(ViewsRoot, "*.cshtml", SearchOption.AllDirectories)
                     .Where(caminho => !Path.GetFileName(caminho).StartsWith("_")))
        {
            var texto = File.ReadAllText(caminho, Encoding.UTF8);
            foreach (Match m in Regex.Matches(texto, @"<input\b[^>]*>"))
            {
                var tag = m.Value;
                if (Regex.IsMatch(tag, @"aria-label|aria-labelledby|type=""(hidden|submit|button|checkbox|radio)""")) continue;
                if (tag.Contains("asp-for=")) continue;
                var idm = Regex.Match(tag, "id=\"([^\"]+)\"");
                if (idm.Success && Regex.IsMatch(texto, "for=\"" + Regex.Escape(idm.Groups[1].Value) + "\"")) continue;
                var inicio = Math.Max(0, m.Index - 220);
                var janela = texto.Substring(inicio, Math.Min(220, m.Index - inicio));
                if (Regex.IsMatch(janela, "<label\\b") && !Regex.IsMatch(janela, "</label\\s*>")) continue;
                orfaos.Add(Path.GetFileName(caminho) + " | " + tag.Substring(0, Math.Min(80, tag.Length)));
            }
        }

        Assert.True(orfaos.Count == 0, "inputs sem nome acessível:\n" + string.Join("\n", orfaos.Take(25)));
    }

    private static int CountOcorrencias(string texto, string alvo)
    {
        var n = 0;
        var i = texto.IndexOf(alvo, StringComparison.Ordinal);
        while (i >= 0)
        {
            n++;
            i = texto.IndexOf(alvo, i + alvo.Length, StringComparison.Ordinal);
        }

        return n;
    }
}
