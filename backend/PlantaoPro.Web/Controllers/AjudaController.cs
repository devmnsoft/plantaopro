using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Web.Models;
using System.Security.Claims;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class AjudaController : Controller
{
    private static readonly List<AjudaInterativaTopicoViewModel> Topicos = CriarTopicos();
    private static readonly List<AjudaInterativaArtigoViewModel> Artigos = CriarArtigos();
    private readonly ILogger<AjudaController> logger;

    public AjudaController(ILogger<AjudaController> logger)
    {
        this.logger = logger;
    }

    public IActionResult Index(string? busca, string? perfil)
    {
        var perfilNormalizado = NormalizarPerfil(perfil);
        var artigos = FiltrarArtigos(busca, perfilNormalizado);
        var model = new AjudaInterativaIndexViewModel
        {
            Busca = busca ?? string.Empty,
            Perfil = perfilNormalizado,
            Topicos = FiltrarTopicos(perfilNormalizado),
            Artigos = artigos
        };
        return View(model);
    }

    public IActionResult Busca(string? termo, string? perfil)
    {
        var perfilNormalizado = NormalizarPerfil(perfil);
        var model = new AjudaInterativaIndexViewModel
        {
            Busca = termo ?? string.Empty,
            Perfil = perfilNormalizado,
            Topicos = FiltrarTopicos(perfilNormalizado),
            Artigos = FiltrarArtigos(termo, perfilNormalizado)
        };
        return View(model);
    }

    public IActionResult Checklist(string? perfil)
    {
        var perfilNormalizado = NormalizarPerfil(perfil);
        var model = new AjudaInterativaChecklistViewModel
        {
            Perfil = perfilNormalizado,
            Artigos = Artigos
                .Where(a => PertenceAoPerfil(a, perfilNormalizado))
                .OrderBy(a => a.Ordem)
                .ThenBy(a => a.Titulo)
                .ToList()
        };
        return View(model);
    }

    public IActionResult PrimeirosPassos()
    {
        var perfilNormalizado = NormalizarPerfil(null);
        var model = new AjudaInterativaChecklistViewModel
        {
            Perfil = perfilNormalizado,
            Artigos = Artigos
                .Where(a => PertenceAoPerfil(a, perfilNormalizado))
                .OrderBy(a => a.Ordem)
                .Take(6)
                .ToList()
        };
        return View(model);
    }

    public IActionResult FluxoAtendimento()
    {
        return View();
    }

    public IActionResult PerguntasFrequentes()
    {
        return View();
    }

    public IActionResult Modulo(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["Warning"] = "Selecione um módulo de ajuda para continuar.";
            return RedirectToAction(nameof(Index));
        }

        var artigo = Artigos.FirstOrDefault(a => string.Equals(a.TopicoSlug, id, StringComparison.OrdinalIgnoreCase));
        if (artigo is null)
        {
            TempData["Warning"] = "Módulo de ajuda não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        return View("Artigo", artigo);
    }

    public IActionResult Artigo(string id)
    {
        var artigo = Artigos.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
        if (artigo is null)
        {
            TempData["Warning"] = "Artigo de ajuda não encontrado.";
            return RedirectToAction(nameof(Index));
        }

        return View(artigo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Feedback(AjudaFeedbackWebViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.ArtigoId))
        {
            TempData["Error"] = "Não foi possível identificar o artigo avaliado.";
            return RedirectToAction(nameof(Index));
        }

        logger.LogInformation("Feedback ajuda Artigo:{ArtigoId} Util:{Util} Usuario:{Usuario}", model.ArtigoId, model.Util, User.Identity?.Name ?? "usuario");
        TempData["Success"] = model.Util ? "Obrigado! Feedback positivo registrado." : "Obrigado! O feedback foi registrado para melhoria do manual.";
        return RedirectToAction(nameof(Artigo), new { id = model.ArtigoId });
    }

    private string NormalizarPerfil(string? perfil)
    {
        var bruto = !string.IsNullOrWhiteSpace(perfil)
            ? perfil.Trim().ToUpperInvariant()
            : (User.FindFirstValue("perfil") ?? User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role"));
        var normalizado = string.IsNullOrWhiteSpace(bruto) ? "TODOS" : bruto.Trim().ToUpperInvariant();
        // D-R51-1: a família administrador de tenant compartilha um único manual (mesmo escopo
        // Tenant e mesmo DefaultRoute /ClientePortal; o onboarding atribui os dois perfis).
        if (string.Equals(normalizado, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase)) normalizado = "ADMINISTRADOR_CLIENTE";
        return normalizado;
    }

    private static List<AjudaInterativaTopicoViewModel> FiltrarTopicos(string perfil)
    {
        return Topicos
            .Where(t => string.Equals(perfil, "TODOS", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Perfil, "TODOS", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Perfil, perfil, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Perfil)
            .ThenBy(t => t.Titulo)
            .ToList();
    }

    private static List<AjudaInterativaArtigoViewModel> FiltrarArtigos(string? termo, string perfil)
    {
        var query = Artigos.Where(a => PertenceAoPerfil(a, perfil));
        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(a => a.Titulo.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || a.Resumo.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || a.Conteudo.Contains(termo, StringComparison.OrdinalIgnoreCase)
                || a.Passos.Any(p => p.Contains(termo, StringComparison.OrdinalIgnoreCase)));
        }

        return query.OrderBy(a => a.Ordem).ThenBy(a => a.Titulo).ToList();
    }

    private static bool PertenceAoPerfil(AjudaInterativaArtigoViewModel artigo, string perfil)
    {
        return string.Equals(perfil, "TODOS", StringComparison.OrdinalIgnoreCase) || string.Equals(artigo.Perfil, "TODOS", StringComparison.OrdinalIgnoreCase) || string.Equals(artigo.Perfil, perfil, StringComparison.OrdinalIgnoreCase);
    }

    private static List<AjudaInterativaTopicoViewModel> CriarTopicos()
    {
        return new List<AjudaInterativaTopicoViewModel>
        {
            new AjudaInterativaTopicoViewModel { Slug = "saas", Perfil = "ADMINISTRADOR_GLOBAL", Titulo = "Gestão SaaS", Descricao = "Clientes, planos, assinaturas, faturas e inteligência SaaS.", Icone = "bi-stars" },
            new AjudaInterativaTopicoViewModel { Slug = "operacao", Perfil = "COORDENACAO", Titulo = "Operação de plantões", Descricao = "Criação, publicação, convites e central de escala.", Icone = "bi-calendar2-week" },
            new AjudaInterativaTopicoViewModel { Slug = "medico", Perfil = "MEDICO", Titulo = "Área do médico", Descricao = "Plantões disponíveis, agenda, convites e pagamentos.", Icone = "bi-heart-pulse" },
            new AjudaInterativaTopicoViewModel { Slug = "financeiro", Perfil = "FINANCEIRO", Titulo = "Financeiro", Descricao = "Pagamentos, contestação e relatórios financeiros.", Icone = "bi-cash-coin" },
            new AjudaInterativaTopicoViewModel { Slug = "hospital", Perfil = "HOSPITAL", Titulo = "Hospital", Descricao = "Acompanhamento de plantões, escalas e comunicação.", Icone = "bi-hospital" },
            new AjudaInterativaTopicoViewModel { Slug = "instituicao", Perfil = "ADMINISTRADOR_CLIENTE", Titulo = "Operação da instituição", Descricao = "Vendas, compras, estoque, títulos e relatórios financeiros do dia a dia.", Icone = "bi-buildings" },
            new AjudaInterativaTopicoViewModel { Slug = "central", Perfil = "TODOS", Titulo = "Minha Central e avisos", Descricao = "Tarefas, faturas vencidas e preferências de notificação que você acompanha.", Icone = "bi-bell" },
            new AjudaInterativaTopicoViewModel { Slug = "privacidade", Perfil = "TODOS", Titulo = "Privacidade LGPD", Descricao = "Consentimentos, direitos do titular e exportação de dados.", Icone = "bi-shield-lock" }
        };
    }

    private static List<AjudaInterativaArtigoViewModel> CriarArtigos()
    {
        return new List<AjudaInterativaArtigoViewModel>
        {
            Artigo("admin-cliente", "saas", "ADMINISTRADOR_GLOBAL", "Como cadastrar cliente", "Crie o cliente, valide CNPJ e acompanhe a jornada comercial.", "Clientes", "Index", 10, new List<string> { "Acesse Clientes.", "Clique em Novo Cliente.", "Preencha razão social, CNPJ e status.", "Salve e acompanhe a Jornada Cliente." }),
            Artigo("admin-plano", "saas", "ADMINISTRADOR_GLOBAL", "Como criar plano", "Configure limites de médicos, hospitais, plantões e recursos premium.", "Planos", "Index", 20, new List<string> { "Acesse Planos.", "Informe valor mensal e limites.", "Marque mobile, BI e relatórios avançados conforme contrato.", "Revise antes de ativar o plano." }),
            Artigo("admin-assinatura", "saas", "ADMINISTRADOR_GLOBAL", "Como criar assinatura", "Associe cliente e plano para liberar operação conforme limites SaaS.", "Assinaturas", "Index", 30, new List<string> { "Acesse Assinaturas.", "Selecione cliente e plano ativo.", "Defina vigência, dia de vencimento e valor contratado.", "Confirme e monitore alertas de uso." }),
            Artigo("admin-fatura", "saas", "ADMINISTRADOR_GLOBAL", "Como gerar fatura", "Controle faturamento SaaS, vencimentos e pagamentos.", "FaturamentoSaas", "Index", 40, new List<string> { "Acesse Faturamento SaaS.", "Confira competência e vencimento.", "Gere a fatura com itens vinculados.", "Marque pagamento somente após confirmação financeira." }),
            Artigo("admin-risco", "saas", "ADMINISTRADOR_GLOBAL", "Como acompanhar clientes em risco", "Use inteligência determinística para priorizar clientes críticos.", "SaasDashboard", "Index", 50, new List<string> { "Acesse Inteligência SaaS.", "Revise cards de risco e alertas.", "Abra ações de Customer Success.", "Recalcule saúde após a ação." }),
            Artigo("coord-plantao", "operacao", "COORDENACAO", "Como criar plantão", "Cadastre plantões com hospital, especialidade, período e valor.", "Plantoes", "Create", 10, new List<string> { "Acesse Plantões.", "Clique em Novo Plantão.", "Informe hospital, especialidade, início, fim e valor.", "Salve como rascunho antes de publicar." }),
            Artigo("coord-publicar", "operacao", "COORDENACAO", "Como publicar plantão", "Publique vagas respeitando assinatura e limite mensal.", "Plantoes", "Index", 20, new List<string> { "Abra o plantão em rascunho.", "Revise dados obrigatórios.", "Clique em Publicar.", "Se houver bloqueio SaaS, regularize plano ou assinatura." }),
            Artigo("coord-convite", "operacao", "COORDENACAO", "Como convidar médico", "Convide profissionais elegíveis para o plantão.", "CentralEscala", "Index", 30, new List<string> { "Acesse Central de Escala.", "Selecione o plantão.", "Confira conflitos e elegibilidade.", "Envie convite e acompanhe resposta." }),
            Artigo("medico-disponiveis", "medico", "MEDICO", "Como acessar plantões disponíveis", "Veja oportunidades compatíveis com seu perfil.", "MinhaAgenda", "PlantoesDisponiveis", 10, new List<string> { "Acesse Minha Agenda.", "Abra Plantões disponíveis.", "Confira data, hospital e valor.", "Aceite apenas plantões compatíveis com sua disponibilidade." }),
            Artigo("medico-pagamentos", "medico", "MEDICO", "Como consultar pagamentos", "Acompanhe pagamentos previstos e confirmados.", "Financeiro", "Index", 20, new List<string> { "Acesse Meus Pagamentos.", "Use filtros por período.", "Confira status de pagamento.", "Acione a coordenação em caso de divergência." }),
            Artigo("financeiro-pagamento", "financeiro", "FINANCEIRO", "Como confirmar pagamento", "Confirme pagamentos com rastreabilidade e auditoria.", "Financeiro", "Index", 10, new List<string> { "Acesse Pagamentos.", "Filtre pendentes.", "Confira valor e favorecido.", "Confirme após conciliação bancária." }),
            Artigo("hospital-escalas", "hospital", "HOSPITAL", "Como consultar escalas", "Acompanhe escalas confirmadas do hospital.", "Escalas", "Index", 10, new List<string> { "Acesse Escalas.", "Filtre por período.", "Verifique médicos confirmados.", "Use Comunicação para dúvidas operacionais." }),
            Artigo("lgpd-direitos", "privacidade", "TODOS", "Como usar Minha Privacidade", "Solicite exportação, correção ou anonimização quando permitido por lei.", "Lgpd", "MinhaPrivacidade", 10, new List<string> { "Acesse Privacidade LGPD.", "Leia a política vigente.", "Registre solicitação do titular.", "Acompanhe status e resposta na própria tela." }),
            Artigo("adm-orcamento", "instituicao", "ADMINISTRADOR_CLIENTE", "Como criar orçamento", "Monte a proposta com itens e valores antes de transformá-la em venda.", "Administrativo360", "Orcamentos", 10, new List<string> { "Acesse a tela de Orçamentos.", "Clique em novo orçamento e informe o cliente ou parceiro.", "Adicione itens, quantidades e valores; confira o total.", "Salve como rascunho e ajuste até ficar pronto para confirmar." }),
            Artigo("adm-compras", "instituicao", "ADMINISTRADOR_CLIENTE", "Como registrar pedido de compra", "Crie o pedido, aprove-o e registre o que chegou.", "Administrativo360", "PedidosCompra", 20, new List<string> { "Acesse Pedidos de Compra.", "Clique em criar pedido e informe fornecedor e itens.", "Aprove o pedido quando os dados estiverem corretos.", "Registre o recebimento quando o material chegar." }),
            Artigo("adm-estoque", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acompanhar estoque e inventário", "Confira saldos, lotes e feche a contagem quando inventariar.", "Administrativo360", "Estoque", 30, new List<string> { "Acesse a tela de Estoque.", "Confira saldo, lotes e movimentações dos produtos.", "Abra a contagem na tela de Inventários.", "Aprove o inventário finalizado para atualizar os saldos." }),
            Artigo("adm-titulos", "instituicao", "ADMINISTRADOR_CLIENTE", "Como controlar títulos a receber e a pagar", "Emita títulos, receba, pague e mantenha o fluxo organizado.", "Administrativo360", "TitulosReceber", 40, new List<string> { "Acesse Títulos a Receber.", "Emita ou receba títulos e confira vencimento e status.", "Acesse Títulos a Pagar para as saídas.", "Marque pago somente após confirmar a baixa no banco." }),
            Artigo("adm-relatorio", "instituicao", "ADMINISTRADOR_CLIENTE", "Como consultar relatórios financeiros", "Entenda resultado, margem e fluxo de caixa com linguagem simples.", "Administrativo360", "RelatoriosFinanceiros", 50, new List<string> { "Acesse Relatórios Financeiros.", "Escolha o relatório do período que deseja ver.", "Leia os totais e os destaques da página.", "Exporte em CSV para compartilhar com a equipe." }),
            Artigo("central-fatura", "central", "TODOS", "Como resolver fatura vencida", "Veja o aviso na Minha Central, confira o valor e regularize o pagamento.", "MinhaCentral", "Index", 10, new List<string> { "Entre na sua área.", "Abra Minha Central e procure o aviso Fatura vencida.", "Confira valor, competência e data de vencimento.", "Pague pelo canal indicado e acompanhe o status voltar ao normal." }),
            Artigo("central-notificacoes", "central", "TODOS", "Como configurar notificações", "Ligue ou desligue os avisos por canal, do jeito que faz sentido para você.", "Notificacoes", "Preferencias", 20, new List<string> { "Acesse Preferências de Notificações.", "Revise as categorias e os canais disponíveis.", "Ative ou desative cada tipo de aviso.", "Salve e aguarde o próximo aviso para confirmar que chegou." })
        };
    }

    private static AjudaInterativaArtigoViewModel Artigo(string id, string topico, string perfil, string titulo, string resumo, string controller, string action, int ordem, List<string> passos)
    {
        return new AjudaInterativaArtigoViewModel
        {
            Id = id,
            TopicoSlug = topico,
            Perfil = perfil,
            Titulo = titulo,
            Resumo = resumo,
            Conteudo = resumo,
            Controller = controller,
            Action = action,
            Ordem = ordem,
            Passos = passos
        };
    }
}
