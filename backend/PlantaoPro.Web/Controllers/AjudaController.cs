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
        // D-R58-1: DIRETOR é membro da mesma família (UserRoleExtensions.IsTenantAdmin e mesmo
        // DefaultRoute /ClientePortal): lê o manual "Operação da instituição" em vez de só os TODOS.
        if (string.Equals(normalizado, "DIRETOR", StringComparison.OrdinalIgnoreCase)) normalizado = "ADMINISTRADOR_CLIENTE";
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
                || a.Passos.Any(p => p.Contains(termo, StringComparison.OrdinalIgnoreCase))
                || PaginaDoArtigo(a).Contains(termo, StringComparison.OrdinalIgnoreCase));
        }

        return query.OrderBy(a => a.Ordem).ThenBy(a => a.Titulo).ToList();
    }

    private static bool PertenceAoPerfil(AjudaInterativaArtigoViewModel artigo, string perfil)
    {
        return string.Equals(perfil, "TODOS", StringComparison.OrdinalIgnoreCase) || string.Equals(artigo.Perfil, "TODOS", StringComparison.OrdinalIgnoreCase) || string.Equals(artigo.Perfil, perfil, StringComparison.OrdinalIgnoreCase);
    }

    // R79: a busca cobre tambem os termos da tela que o artigo aponta (modulo + acao),
    // para quem procura pelo nome da aba ou do modulo onde esta parado encontrar o artigo.
    private static string PaginaDoArtigo(AjudaInterativaArtigoViewModel artigo) => artigo.Controller + " " + artigo.Action;

    private static List<AjudaInterativaTopicoViewModel> CriarTopicos()
    {
        return new List<AjudaInterativaTopicoViewModel>
        {
            new AjudaInterativaTopicoViewModel { Slug = "saas", Perfil = "ADMINISTRADOR_GLOBAL", Titulo = "Gestão SaaS", Descricao = "Clientes, planos, assinaturas, faturas e inteligência SaaS.", Icone = "bi-stars" },
            new AjudaInterativaTopicoViewModel { Slug = "operacao", Perfil = "COORDENACAO", Titulo = "Operação de plantões", Descricao = "Criação, publicação, convites e central de escala.", Icone = "bi-calendar2-week" },
            new AjudaInterativaTopicoViewModel { Slug = "medico", Perfil = "MEDICO", Titulo = "Área do médico", Descricao = "Plantões disponíveis, agenda, convites e pagamentos.", Icone = "bi-heart-pulse" },
            new AjudaInterativaTopicoViewModel { Slug = "clinico", Perfil = "TODOS", Titulo = "Cuidado clínico", Descricao = "Triagem, agendamento, consulta, prontuário e prescrições do fluxo assistencial.", Icone = "bi-clipboard-pulse" },
            new AjudaInterativaTopicoViewModel { Slug = "financeiro", Perfil = "FINANCEIRO", Titulo = "Financeiro", Descricao = "Pagamentos, contestação e relatórios financeiros.", Icone = "bi-cash-coin" },
            new AjudaInterativaTopicoViewModel { Slug = "hospital", Perfil = "HOSPITAL", Titulo = "Hospital", Descricao = "Acompanhamento de plantões, escalas e comunicação.", Icone = "bi-hospital" },
            new AjudaInterativaTopicoViewModel { Slug = "recepcao", Perfil = "RECEPCAO", Titulo = "Atendimento na recepção", Descricao = "Pacientes, agendamentos, check-in e sala de espera do guichê.", Icone = "bi-person-badge" },
            new AjudaInterativaTopicoViewModel { Slug = "parceiro", Perfil = "PARCEIRO", Titulo = "Portal do parceiro", Descricao = "Propostas, comissões e repasses da sua parceria.", Icone = "bi-handshake" },
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
            Artigo("adm-inadimplencia", "saas", "ADMINISTRADOR_GLOBAL", "Como acompanhar a inadimplência dos clientes", "Veja clientes com faturas vencidas e o valor em aberto.", "FaturamentoSaas", "Inadimplencia", 60, new List<string> { "Acesse Inadimplência em Faturamento SaaS.", "Revise a lista de clientes com faturas vencidas.", "Confira o vencimento mais antigo e o valor vencido.", "Priorize a régua de cobrança pelos totais exibidos." }),
            Artigo("coord-plantao", "operacao", "COORDENACAO", "Como criar plantão", "Cadastre plantões com hospital, especialidade, período e valor.", "Plantoes", "Create", 10, new List<string> { "Acesse Plantões.", "Clique em Novo Plantão.", "Informe hospital, especialidade, início, fim e valor.", "Salve como rascunho antes de publicar." }),
            Artigo("coord-publicar", "operacao", "COORDENACAO", "Como publicar plantão", "Publique vagas respeitando assinatura e limite mensal.", "Plantoes", "Index", 20, new List<string> { "Abra o plantão em rascunho.", "Revise dados obrigatórios.", "Clique em Publicar.", "Se houver bloqueio SaaS, regularize plano ou assinatura." }),
            Artigo("coord-convite", "operacao", "COORDENACAO", "Como convidar médico", "Convide profissionais elegíveis para o plantão.", "CentralEscala", "Index", 30, new List<string> { "Acesse Central de Escala.", "Selecione o plantão.", "Confira conflitos e elegibilidade.", "Envie convite e acompanhe resposta." }),
            Artigo("medico-disponiveis", "medico", "MEDICO", "Como acessar plantões disponíveis", "Veja oportunidades compatíveis com seu perfil.", "MinhaAgenda", "PlantoesDisponiveis", 10, new List<string> { "Acesse Minha Agenda.", "Abra Plantões disponíveis.", "Confira data, hospital e valor.", "Aceite apenas plantões compatíveis com sua disponibilidade." }),
            Artigo("medico-pagamentos", "medico", "MEDICO", "Como consultar pagamentos", "Acompanhe pagamentos previstos e confirmados.", "Financeiro", "Index", 20, new List<string> { "Acesse Meus Pagamentos.", "Use filtros por período.", "Confira status de pagamento.", "Acione a coordenação em caso de divergência." }),
            Artigo("medico-agenda", "medico", "MEDICO", "Como consultar sua agenda de plantões", "Veja apenas os plantões em que você foi alocado neste cliente.", "MinhaAgenda", "Agenda", 30, new List<string> { "Acesse Minha Agenda na área do médico.", "Filtre por período, unidade e situação do plantão.", "Alterne entre Semana, Mês e Lista acessível.", "Abra o detalhe para confirmar seu plantão." }),
            Artigo("medico-meuspagamentos", "medico", "MEDICO", "Como consultar seus demonstrativos de pagamento", "Acompanhe apuração, aprovação e pagamento dos seus plantões.", "MinhaAgenda", "MeusPagamentos", 40, new List<string> { "Acesse Meus Pagamentos na área do médico.", "Confira os totais de apurado, aprovado e pago.", "Revise cada item de execução e a situação de fechamento.", "Envie uma divergência para análise quando algo não fechar." }),
            Artigo("financeiro-pagamento", "financeiro", "FINANCEIRO", "Como confirmar pagamento", "Confirme pagamentos com rastreabilidade e auditoria.", "Financeiro", "Index", 10, new List<string> { "Acesse Pagamentos.", "Filtre pendentes.", "Confira valor e favorecido.", "Confirme após conciliação bancária." }),
            Artigo("hospital-escalas", "hospital", "HOSPITAL", "Como consultar escalas", "Acompanhe escalas confirmadas do hospital.", "Escalas", "Index", 10, new List<string> { "Acesse Escalas.", "Filtre por período.", "Verifique médicos confirmados.", "Use Comunicação para dúvidas operacionais." }),
            Artigo("lgpd-direitos", "privacidade", "TODOS", "Como usar Minha Privacidade", "Solicite exportação, correção ou anonimização quando permitido por lei.", "Lgpd", "MinhaPrivacidade", 10, new List<string> { "Acesse Privacidade LGPD.", "Leia a política vigente.", "Registre solicitação do titular.", "Acompanhe status e resposta na própria tela." }),
            Artigo("adm-orcamento", "instituicao", "ADMINISTRADOR_CLIENTE", "Como criar orçamento", "Monte a proposta com itens e valores antes de transformá-la em venda.", "Administrativo360", "Orcamentos", 10, new List<string> { "Acesse a tela de Orçamentos.", "Clique em novo orçamento e informe o cliente ou parceiro.", "Adicione itens, quantidades e valores; confira o total.", "Salve como rascunho e ajuste até ficar pronto para confirmar." }),
            Artigo("adm-compras", "instituicao", "ADMINISTRADOR_CLIENTE", "Como registrar pedido de compra", "Crie o pedido, aprove-o e registre o que chegou.", "Administrativo360", "PedidosCompra", 20, new List<string> { "Acesse Pedidos de Compra.", "Clique em criar pedido e informe fornecedor e itens.", "Aprove o pedido quando os dados estiverem corretos.", "Registre o recebimento quando o material chegar." }),
            Artigo("adm-estoque", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acompanhar estoque e inventário", "Confira saldos, lotes e feche a contagem quando inventariar.", "Administrativo360", "Estoque", 30, new List<string> { "Acesse a tela de Estoque.", "Confira saldo, lotes e movimentações dos produtos.", "Abra a contagem na tela de Inventários.", "Aprove o inventário finalizado para atualizar os saldos." }),
            Artigo("adm-titulos", "instituicao", "ADMINISTRADOR_CLIENTE", "Como controlar títulos a receber e a pagar", "Emita títulos, receba, pague e mantenha o fluxo organizado.", "Administrativo360", "TitulosReceber", 40, new List<string> { "Acesse Títulos a Receber.", "Emita ou receba títulos e confira vencimento e status.", "Acesse Títulos a Pagar para as saídas.", "Marque pago somente após confirmar a baixa no banco." }),
            Artigo("adm-relatorio", "instituicao", "ADMINISTRADOR_CLIENTE", "Como consultar relatórios financeiros", "Entenda resultado, margem e fluxo de caixa com linguagem simples.", "Administrativo360", "RelatoriosFinanceiros", 50, new List<string> { "Acesse Relatórios Financeiros.", "Escolha o relatório do período que deseja ver.", "Leia os totais e os destaques da página.", "Exporte em CSV para compartilhar com a equipe." }),
            Artigo("adm-panorama", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acompanhar o panorama organizacional", "Veja departamentos, cargos, colaboradores e contratos vigentes em um só lugar.", "Administrativo360", "Index", 60, new List<string> { "Acesse o panorama do Administrativo 360.", "Revise os contadores de itens ativos da instituição.", "Use os cartões de atalho para abrir Departamentos, Cargos, Colaboradores ou Contratos.", "Confira como cada indicador é calculado na própria tela." }),
            Artigo("adm-cadastros", "instituicao", "ADMINISTRADOR_CLIENTE", "Como manter parceiros, produtos e locais de armazenamento", "Cadastre e ative ou inative os cadastros-base do administrativo em uma tela.", "Administrativo360", "Cadastros", 70, new List<string> { "Acesse Cadastros no Administrativo 360.", "Use Novo Parceiro, Novo Produto ou Novo Local para criar.", "Preencha os dados e salve cada cadastro pelo botão da janela.", "Ative ou inative parceiros e produtos pelas ações das linhas." }),
            Artigo("adm-produtos", "instituicao", "ADMINISTRADOR_CLIENTE", "Como gerir o catálogo de produtos e OPME", "Busque, cadastre, edite e controle o status dos materiais.", "Administrativo360", "Produtos", 80, new List<string> { "Acesse Produtos e busque por SKU, descrição ou código de barras.", "Clique em Novo Produto para cadastrar um material.", "Edite itens com o botão de lápis na linha.", "Registre lotes e alterne entre ativo e inativo conforme a necessidade." }),
            Artigo("adm-locais", "instituicao", "ADMINISTRADOR_CLIENTE", "Como configurar locais de estoque internos e de consignação", "Cadastre e edite os pontos de armazenamento de material.", "Administrativo360", "Locais", 90, new List<string> { "Acesse Locais no Administrativo 360.", "Filtre por local interno ou externo (consignação).", "Crie ou edite um local com o formulário da tela.", "Salve com o tipo correto para permitir transferências entre eles." }),
            Artigo("adm-inventarios", "instituicao", "ADMINISTRADOR_CLIENTE", "Como realizar a contagem física de inventário", "Abra uma contagem, registre quantidades e aprove para atualizar saldos.", "Administrativo360", "Inventarios", 100, new List<string> { "Acesse Inventários e clique em Abrir Novo Inventário.", "Escolha o local e o escopo antes de abrir a contagem.", "Registre as contagens físicas por produto e lote.", "Aprove o inventário finalizado para atualizar os saldos." }),
            Artigo("adm-transferencias", "instituicao", "ADMINISTRADOR_CLIENTE", "Como transferir estoque entre locais", "Mova lotes disponíveis de um local para outro com motivo registrado.", "Administrativo360", "Movimentacoes", 110, new List<string> { "Acesse Transferências para ver os saldos disponíveis.", "Selecione a linha com o material e o lote a movimentar.", "Informe destino, quantidade e motivo da transferência.", "Confirme a transferência para gerar a movimentação." }),
            Artigo("adm-valorizacao", "instituicao", "ADMINISTRADOR_CLIENTE", "Como valorizar vales de consignação", "Revise vales aptos e confirme a geração da venda.", "Administrativo360", "Valorizacao", 120, new List<string> { "Acesse a valorização de vales de consignação.", "Busque por vale, hospital, cirurgia ou orçamento.", "Abra a prévia do vale apto a ser valorizado.", "Confirme na prévia para gerar a venda e os títulos." }),
            Artigo("adm-vendas", "instituicao", "ADMINISTRADOR_CLIENTE", "Como consultar as vendas internas", "Acompanhe vendas geradas, comissões e situação de cada item.", "Administrativo360", "Vendas", 130, new List<string> { "Acesse Vendas Internas.", "Filtre por situação, período ou termo de busca.", "Localize a venda que deseja revisar.", "Abra Detalhes para ver itens, parcelas e contas a receber." }),
            Artigo("adm-venda-detalhes", "instituicao", "ADMINISTRADOR_CLIENTE", "Como revisar e cancelar uma venda interna", "Veja totais, parcelas geradas e cancele com motivo obrigatório.", "Administrativo360", "VendaDetalhes", 140, new List<string> { "Abra Detalhes a partir das Vendas Internas.", "Revise os totais, os itens comerciais e as parcelas geradas.", "Para desfazer, use Cancelar Venda informando o motivo.", "Confirme e acompanhe a situação atualizar na lista." }),
            Artigo("adm-fechamento-caixa", "instituicao", "ADMINISTRADOR_CLIENTE", "Como fechar e conferir o caixa", "Registre o saldo conferido por conta e reabra quando houver erro.", "Administrativo360", "FechamentoCaixa", 150, new List<string> { "Acesse Fechamento de Caixa e filtre pela conta financeira.", "Clique em Realizar Fechamento e informe o período.", "Salve o fechamento com o saldo conferido.", "Se houver divergência, use Reabrir com justificativa." }),
            Artigo("adm-fluxo-caixa", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acompanhar fluxo de caixa e contas bancárias", "Consolide saldos, movimentos realizados e projeção do período.", "Administrativo360", "FluxoCaixa", 160, new List<string> { "Acesse Fluxo de Caixa e Contas Bancárias.", "Revise os cards consolidados de saldo e valores em aberto.", "Confira os movimentos e a projeção do período.", "Cadastre novos bancos com Nova Conta Financeira." }),
            Artigo("adm-contas-pagar", "instituicao", "ADMINISTRADOR_CLIENTE", "Como gerenciar as contas a pagar", "Aprove, pague, estorne e exporte os títulos de saída.", "Administrativo360", "TitulosPagar", 170, new List<string> { "Acesse Contas a Pagar.", "Filtre por situação e intervalo de vencimento.", "Abra cada título para aprovar, pagar ou estornar.", "Exporte em CSV para conciliar fora do sistema." }),
            Artigo("adm-despesa-manual", "instituicao", "ADMINISTRADOR_CLIENTE", "Como registrar uma despesa manual", "Crie o título a pagar com fornecedor, documento e vencimento.", "Administrativo360", "NovaDespesa", 180, new List<string> { "Acesse Nova Despesa a partir das Contas a Pagar.", "Informe fornecedor, número do documento e valor total.", "Defina competência, vencimento e centro de custo.", "Salve para enviar o título à aprovação." }),
            Artigo("fat-contas-clinicas", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acompanhar as contas clínicas a receber", "Revise o faturamento clínico, status e a jornada até o repasse.", "FaturamentoClinico", "Index", 190, new List<string> { "Acesse Faturamento Clínico.", "Filtre por status, competência e convênio.", "Revise as contas retornadas e os valores previstos.", "Siga a jornada para financeiro clínico quando disponível." }),
            Artigo("fat-titulos", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acompanhar os boletos das contas clínicas", "Visão consolidada dos boletos no fluxo de faturamento clínico.", "FaturamentoClinico", "Titulos", 200, new List<string> { "Acesse Títulos dentro do Faturamento Clínico.", "Entenda que o boleto nasce da conta clínica faturada.", "A tela ainda está em finalização, conforme o cartão indica.", "Use o atalho O que fazer agora para seguir o fluxo." }),
            Artigo("fat-repasses", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acompanhar os repasses médicos", "Revise repasses por plantão realizado e atendimento faturado.", "FaturamentoClinico", "RepassesMedicos", 210, new List<string> { "Acesse Repasses Médicos dentro do Faturamento Clínico.", "Entenda que o repasse vem de plantões realizados e faturados.", "A tela ainda está em finalização, conforme o cartão indica.", "Use o atalho O que fazer agora para seguir o fluxo." }),
            Artigo("fat-relatorios", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acessar os relatórios do financeiro clínico", "Veja a visão geral dos relatórios financeiros do módulo clínico.", "ClinicaFinanceiro", "Relatorios", 220, new List<string> { "Acesse Relatórios dentro do Financeiro Clínico.", "Revise os indicadores e os registros exibidos na visão geral.", "Use a navegação da jornada para voltar à etapa anterior.", "Retome o fluxo principal pelas contas quando precisar." }),
            Artigo("fiscal-sincronizacao", "instituicao", "ADMINISTRADOR_CLIENTE", "Como sincronizar o DF-e da Sefaz", "Acompanhe o estado por estabelecimento e execute a consulta.", "Adm360DocumentosXmlWeb", "Sincronizacao", 230, new List<string> { "Acesse Sincronização DF-e.", "Revise o status, o último NSU e a próxima consulta permitida.", "Execute a consulta DF-e escolhendo o estabelecimento.", "Confira o resultado da sincronização na tabela." }),
            Artigo("fiscal-importacao-xml", "instituicao", "ADMINISTRADOR_CLIENTE", "Como importar notas fiscais em XML manualmente", "Envie ou cole arquivos XML de NF-e, NFC-e e NFS-e.", "Adm360DocumentosXmlWeb", "Importar", 240, new List<string> { "Acesse Importação Manual de XML.", "Selecione o arquivo .xml ou cole o conteúdo bruto.", "Clique em Validar e Importar XML.", "Revise importados, quarentena e duplicados após o envio." }),
            Artigo("fiscal-notas", "instituicao", "ADMINISTRADOR_CLIENTE", "Como preparar a pré-emissão de NF-e", "Crie pré-notas e acompanhe a situação até a autorização.", "Adm360FiscalWeb", "Notas", 250, new List<string> { "Acesse Pré-Emissão de NF-e.", "Crie uma pré-nota com Nova Pré-Nota.", "Abra Detalhes para avançar as transições.", "Acompanhe a situação e a chave de acesso externa." }),
            Artigo("fech-apuracao", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acompanhar a apuração de plantões", "Siga execuções, divergências e aprovações do fechamento.", "Fechamentos", "Index", 260, new List<string> { "Acesse Fechamentos pela rota /fechamentos.", "Filtre por período, unidade, profissional e status.", "Revise os totais de previsto, aprovado e pendente.", "Use Exportar CSV para levar a apuração para fora do sistema." }),
            Artigo("fech-conferencia", "instituicao", "ADMINISTRADOR_CLIENTE", "Como conferir e aprovar um fechamento", "Compare a execução com a escala prevista, ajuste e aprove.", "Fechamentos", "Conferencia", 270, new List<string> { "Abra o item para conferir na apuração.", "Compare a execução conferida com a escala prevista.", "Trate divergências antes de enviar para aprovação.", "Aprove a prévia ou devolva para ajuste com motivo." }),
            Artigo("adm-assinatura", "instituicao", "ADMINISTRADOR_CLIENTE", "Como consultar a sua assinatura", "Veja plano atual, status, limites e histórico de cobrança.", "MinhaAssinatura", "Index", 280, new List<string> { "Acesse Minha Assinatura.", "Revise plano atual, status e próximo vencimento.", "Confira limites em uso e alertas contratuais.", "Use as ações para consultar planos ou abrir configurações." }),
            Artigo("adm-faturas", "instituicao", "ADMINISTRADOR_CLIENTE", "Como pagar as faturas da assinatura", "Veja faturas em aberto ou vencidas e pague online.", "MinhaAssinatura", "Faturas", 290, new List<string> { "Acesse Faturas em Minha Assinatura.", "Confira vencimento, status e valor de cada fatura.", "Clique em Pagar agora nas faturas em aberto ou vencidas.", "Siga para a página de pagamento e confirme." }),
            Artigo("adm-uso-plano", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acompanhar o uso contra os limites do plano", "Veja o consumo de médicos, hospitais, plantões e usuários.", "MinhaAssinatura", "Uso", 300, new List<string> { "Acesse Uso em Minha Assinatura.", "Revise as barras de consumo por métrica do plano.", "Limite zerado significa ilimitado para a métrica.", "Se estiver perto do limite, solicite upgrade na tela." }),
            Artigo("adm-modulos", "instituicao", "ADMINISTRADOR_CLIENTE", "Como contratar módulos à parte do plano", "Solicite módulos do catálogo e acompanhe a análise.", "MinhaAssinatura", "Modulos", 310, new List<string> { "Acesse Módulos em Minha Assinatura.", "Escolha os módulos disponíveis no catálogo.", "Revise condições e envie a solicitação para análise.", "Acompanhe o protocolo das solicitações em andamento." }),
            Artigo("adm-usuarios", "instituicao", "ADMINISTRADOR_CLIENTE", "Como gerenciar usuários da instituição", "Crie, edite, bloqueie e libere acessos no seu escopo.", "Usuarios", "Index", 320, new List<string> { "Acesse Usuários.", "Busque por nome ou e-mail.", "Use Novo usuário para criar um acesso.", "Bloqueie ou desbloqueie o vínculo pela linha do usuário." }),
            Artigo("adm-novo-usuario", "instituicao", "ADMINISTRADOR_CLIENTE", "Como criar um usuário com perfis", "Cadastre os dados de acesso e atribua os perfis corretos.", "Usuarios", "Create", 330, new List<string> { "Em Usuários, clique em Novo usuário.", "Preencha nome, e-mail de acesso e senha temporária.", "Selecione os perfis atribuíveis ao usuário.", "Salve e oriente a troca da senha no primeiro acesso." }),
            Artigo("adm-minha-conta", "instituicao", "ADMINISTRADOR_CLIENTE", "Como revisar sua conta pessoal", "Atualize dados, idioma e preferências da sua conta.", "Usuario", "Index", 340, new List<string> { "Abra Minha Conta no menu do usuário.", "Edite seus dados com o botão Editar dados.", "Alterne o idioma da interface se precisar.", "Salve para aplicar as alterações." }),
            Artigo("adm-alterar-senha", "instituicao", "ADMINISTRADOR_CLIENTE", "Como alterar a senha da sua conta", "Troque a senha confirmando a senha atual.", "Usuario", "AlterarSenha", 350, new List<string> { "Em Minha Conta, clique em Alterar senha.", "Informe a senha atual e a nova senha.", "Confirme a nova senha.", "Clique em Atualizar senha para concluir." }),
            Artigo("adm-perfis", "instituicao", "ADMINISTRADOR_CLIENTE", "Como gerir perfis de acesso", "Crie, edite e inative perfis e revise as permissões.", "Perfis", "Index", 360, new List<string> { "Acesse Perfis.", "Clique em Novo perfil para criar um acesso próprio.", "Ajuste as permissões do perfil quando necessário.", "Inative perfis que não forem mais usados." }),
            Artigo("adm-matriz-permissoes", "instituicao", "ADMINISTRADOR_CLIENTE", "Como consultar a matriz de permissões", "Veja quais perfis têm acesso a cada módulo.", "Permissoes", "Matriz", 370, new List<string> { "Acesse a Matriz de Permissões.", "Localize a interseção de perfil e módulo.", "Simule uma ação com o teste de acesso.", "Use o resultado para ajustar os perfis." }),
            Artigo("adm-folha-calculos", "instituicao", "ADMINISTRADOR_CLIENTE", "Como simular folha, 13º, férias e rescisão em linguagem simples", "Use os simuladores com os valores oficiais e entenda cada desconto.", "FolhaWeb", "Index", 380, new List<string> { "Acesse Folha e Rescisões no menu da administração.", "Simule o mês com salário base, horas extras e dependentes.", "Simule o 13º pelo período trabalhado ou as férias com o terço.", "Para rescisão sem justa causa, informe as datas e confira cada verba explicada na tela." }),
            Artigo("adm-simples-nacional", "instituicao", "ADMINISTRADOR_CLIENTE", "Como prever o DAS do Simples Nacional com as tabelas oficiais", "Veja as faixas cumulativas por atividade e gere a prévia do DAS com cada campo explicado.", "SimplesWeb", "Index", 390, new List<string> { "Acesse Simples Nacional no menu da administração.", "Revise a tabela oficial de faixas para a atividade da empresa.", "Informe a receita acumulada em 12 meses e a receita do mês de competência.", "Use Carregar exemplo real para ver a conta completa e entenda cada campo da prévia." }),
            Artigo("adm-calendario-fiscal", "instituicao", "ADMINISTRADOR_CLIENTE", "Como acompanhar as obrigações fiscais do mês no calendário", "Veja os vencimentos das obrigações legais com avisos antecipados e marque o que já foi entregue.", "CalendarioFiscalWeb", "Index", 400, new List<string> { "Acesse Calendário Fiscal no menu da administração.", "Selecione o mês e o ano do período e carregue o calendário.", "Revise primeiro as obrigações atrasadas e as que vencem em curto prazo.", "Ao entregar uma obrigação ao órgão responsável, marque-a como concluída; desative as que não se aplicam ao período." }),
            Artigo("adm-dfe-nacional", "instituicao", "ADMINISTRADOR_CLIENTE", "Como gerar e conferir a chave de acesso dos DF-e", "Confira a configuração SEFAZ de cada UF, os layouts oficiais vigentes e gere ou valide a chave de acesso com o dígito verificador módulo 11.", "DfeNacionalWeb", "Index", 410, new List<string> { "Acesse DF-e Nacional no menu da administração.", "Consulte a tabela de configuração SEFAZ por UF e as versões vigentes dos layouts oficiais.", "Preencha UF, data, inscrição, modelo, série, número e tipo de emissão para gerar a chave e conferir cada segmento.", "Cole a chave de um documento recebido para validar o dígito verificador, a UF de origem e o modelo apoiado naquela UF." }),
            Artigo("central-fatura", "central", "TODOS", "Como resolver fatura vencida", "Veja o aviso na Minha Central, confira o valor e regularize o pagamento.", "MinhaCentral", "Index", 10, new List<string> { "Entre na sua área.", "Abra Minha Central e procure o aviso Fatura vencida.", "Confira valor, competência e data de vencimento.", "Pague pelo canal indicado e acompanhe o status voltar ao normal." }),
            Artigo("central-notificacoes", "central", "TODOS", "Como configurar notificações", "Ligue ou desligue os avisos por canal, do jeito que faz sentido para você.", "Notificacoes", "Preferencias", 20, new List<string> { "Acesse Preferências de Notificações.", "Revise as categorias e os canais disponíveis.", "Ative ou desative cada tipo de aviso.", "Salve e aguarde o próximo aviso para confirmar que chegou." }),
            Artigo("cad-empresa", "central", "TODOS", "Como começar o cadastro da instituição", "Informe os dados da empresa no cadastro self-service.", "Cadastro", "Empresa", 30, new List<string> { "Comece o cadastro na página pública de planos.", "Preencha nome fantasia, razão social e CNPJ.", "Informe segmento, contato e volume estimado.", "Continue com as etapas de plano e administrador." }),
            Artigo("cad-plano", "central", "TODOS", "Como escolher o plano no cadastro", "Selecione o plano e a periodicidade antes de enviar.", "Cadastro", "Plano", 40, new List<string> { "No cadastro, vá até a etapa Plano.", "Escolha um plano do catálogo exibido.", "Selecione periodicidade mensal ou anual.", "Prossiga para os dados do administrador." }),
            Artigo("cad-administrador", "central", "TODOS", "Como informar o administrador responsável", "Cadastre quem responde pela conta da instituição.", "Cadastro", "Usuario", 50, new List<string> { "No cadastro, vá até a etapa Administrador.", "Informe nome, e-mail, telefone e cargo.", "Defina a senha inicial do responsável.", "Revise tudo antes de enviar o cadastro." }),
            Artigo("cad-envio", "central", "TODOS", "Como revisar e enviar o cadastro", "Aceite os termos e envie o cadastro para validação.", "Cadastro", "Confirmacao", 60, new List<string> { "Aceite os termos de uso, privacidade e LGPD.", "Revise os dados de empresa, plano e administrador.", "Clique em Enviar cadastro para validação.", "Aguarde a confirmação para seguir ao login." }),
            Artigo("cad-conclusao", "central", "TODOS", "Como concluir o cadastro self-service", "Confirme a recepção e entre na plataforma.", "Cadastro", "Sucesso", 70, new List<string> { "Após enviar o cadastro, veja a tela de confirmação.", "Confira a mensagem de recebimento.", "Clique em Ir para login para entrar.", "Acione o suporte se o acesso não estiver liberado." }),
            Artigo("cent-notificacoes", "central", "TODOS", "Como usar a central de notificações", "Filtre, resolva e arquive avisos operacionais.", "Notificacoes", "Index", 80, new List<string> { "Acesse a central de Notificações.", "Filtre por tipo, prioridade e status.", "Abra o contexto do aviso ou marque como lida.", "Resolva ou arquive conforme o encerramento." }),
            Artigo("ajuda-guia", "central", "TODOS", "Como usar a central de ajuda", "Busque artigos por termo, tópico e perfil.", "Ajuda", "Index", 90, new List<string> { "Acesse a central de Ajuda.", "Use a busca por palavras do título ou dos passos.", "Navegue pelos tópicos relevantes ao seu perfil.", "Envie feedback nos artigos para melhorar o manual." }),
            Artigo("recep-paciente", "recepcao", "RECEPCAO", "Como cadastrar paciente", "Registre os dados do paciente para agilizar o agendamento.", "Pacientes", "Create", 10, new List<string> { "Acesse a tela de Pacientes.", "Clique em novo paciente e preencha nome e documento.", "Confira contato e convênio antes de salvar.", "Atualize o cadastro sempre que faltarem dados." }),
            Artigo("recep-agendamento", "recepcao", "RECEPCAO", "Como agendar e fazer o check-in", "Crie o agendamento e registre a chegada do paciente na hora certa.", "Agendamentos", "CheckIn", 20, new List<string> { "Acesse a tela de Agendamentos.", "Crie o agendamento com paciente, profissional e horário.", "Quando o paciente chegar, abra a tela de Check-in.", "Confirme a chegada para liberar o atendimento." }),
            Artigo("recep-chamada", "recepcao", "RECEPCAO", "Como operar a sala de espera", "Chame pacientes na ordem correta sem expor dados sensíveis na TV.", "PainelChamada", "Index", 30, new List<string> { "Acesse o Painel de chamada.", "Chame o paciente da frente quando o profissional liberar.", "Use a rechamada se o paciente não aparecer.", "Acompanhe ausências no histórico do painel." }),
            Artigo("clin-triagem-fila", "clinico", "TODOS", "Como acompanhar a fila de triagem", "Veja quem está aguardando classificação e encaminhe na ordem certa.", "Triagem", "Fila", 10, new List<string> { "Acesse a Fila de triagem.", "Confira os pacientes aguardando classificação e encaminhamento.", "Abra uma nova triagem pelo botão Novo da tela.", "Após salvar a triagem, finalize e encaminhe para consulta." }),
            Artigo("clin-triagem-nova", "clinico", "TODOS", "Como registrar a triagem do paciente", "Preencha queixa, classificação de risco e sinais vitais.", "Triagem", "Create", 20, new List<string> { "Abra Nova triagem pela fila ou pelo botão Enviar para triagem do agendamento.", "Selecione o paciente e vincule o agendamento quando existir.", "Informe a queixa principal e escolha a classificação de risco.", "Registre os sinais vitais e salve a triagem para obter o identificador." }),
            Artigo("clin-triagem-finalizar", "clinico", "TODOS", "Como finalizar a triagem e encaminhar à consulta", "Valide os dados salvos e libere o fluxo de atendimento.", "Triagem", "Finalizar", 30, new List<string> { "Abra a triagem já salva do paciente.", "Revise queixa, classificação de risco e sinais vitais.", "Clique em Finalizar triagem para validar os dados.", "Confirme o encaminhamento e acompanhe na tela de detalhes." }),
            Artigo("clin-agendamento-agenda", "clinico", "TODOS", "Como acompanhar a agenda clínica do dia", "Veja os agendamentos por horário com filtros e ações rápidas.", "Agendamentos", "Index", 40, new List<string> { "Acesse a tela de Agendamentos.", "Use os filtros de data, médico, unidade, especialidade e status.", "Confira os cards por horário e o tempo de espera de cada um.", "Use as ações rápidas do card conforme a etapa do atendimento." }),
            Artigo("clin-agendamento-novo", "clinico", "TODOS", "Como criar um agendamento", "Cadastre paciente, profissional, unidade e horário do atendimento.", "Agendamentos", "Create", 50, new List<string> { "Na agenda, abra a criação de novo agendamento.", "Informe paciente e profissional obrigatórios e, se houver, unidade e sala.", "Defina o início e o término do atendimento.", "Salve o agendamento; a disponibilidade será verificada ao salvar." }),
            Artigo("clin-agendamento-cancelar", "clinico", "TODOS", "Como cancelar um agendamento com trilha de auditoria", "Cancele pelo card informando o motivo e confira o histórico.", "Agendamentos", "Cancelamentos", 60, new List<string> { "Abra o agendamento na agenda do dia.", "Clique em Cancelar no card do agendamento.", "Preencha o motivo do cancelamento, que é obrigatório.", "Confirme e revise o registro na tela de Cancelamentos." }),
            Artigo("clin-agenda-operacional", "clinico", "TODOS", "Como visualizar a agenda operacional de plantões", "Acompanhe plantões, coberturas e conflitos em timeline ou calendário.", "Agenda", "Index", 70, new List<string> { "Acesse a Agenda operacional pela rota /agenda.", "Alterne entre Timeline, Calendário, Por médico, Por hospital, Semana e Mês.", "Filtre por período e status do plantão.", "Na visão Conflitos, revise as sobreposições e abra o contexto do evento." }),
            Artigo("clin-consulta-lista", "clinico", "TODOS", "Como acessar a lista de consultas", "Localize consultas com vínculo de triagem, CID e prescrição.", "Consultas", "Index", 80, new List<string> { "Acesse a tela de Consultas.", "Localize a consulta pela identificação da lista.", "Use as ações do módulo para abrir atendimento ou histórico.", "Ao finalizar o atendimento, siga para o faturamento clínico." }),
            Artigo("clin-consulta-atendimento", "clinico", "TODOS", "Como conduzir o atendimento da consulta", "Preencha anamnese, exame físico, diagnóstico e conduta com gravação automática.", "Consultas", "Atendimento", 90, new List<string> { "Abra a consulta pelo botão Atendimento da lista.", "Preencha o contexto clínico, a anamnese e o exame físico.", "Registre o diagnóstico e adicione os CIDs vinculados.", "Use Salvar rascunho durante o atendimento e finalize com Revisar e finalizar." }),
            Artigo("clin-paciente-ficha", "clinico", "TODOS", "Como consultar a ficha do paciente", "Veja a identificação operacional do paciente com dados mínimos.", "Pacientes", "Details", 100, new List<string> { "Acesse a tela de Pacientes.", "Abra o paciente desejado para ver a ficha.", "Confira o contexto longitudinal nos cards da tela.", "Retorne à lista para localizar outro paciente." }),
            Artigo("clin-paciente-prontuario", "clinico", "TODOS", "Como consultar o prontuário longitudinal do paciente", "Acompanhe problemas ativos, alergias, medicamentos e a linha do tempo clínica.", "Pacientes", "Prontuario", 110, new List<string> { "Abra o prontuário do paciente pelo vínculo na jornada.", "Confira problemas ativos, alergias e medicamentos em uso.", "Navegue na timeline clínica paginada.", "Filtre os eventos por tipo como TRIAGEM, CONSULTA ou EXAME RESULTADO." }),
            Artigo("clin-paciente-busca", "clinico", "TODOS", "Como localizar um paciente", "Busque por nome, CPF ou telefone dentro da sua instituição.", "Pacientes", "Buscar", 120, new List<string> { "Acesse a busca de Pacientes.", "Informe o termo de busca, como nome, CPF ou telefone.", "Confira os resultados e abra o paciente localizado.", "Se não encontrar, cadastre um novo paciente." }),
            Artigo("clin-prescricao-modulo", "clinico", "TODOS", "Como acessar o módulo de prescrições", "Consulte o contexto das prescrições vinculadas a consultas e CIDs.", "Prescricoes", "Index", 130, new List<string> { "Acesse a tela de Prescrições.", "Confira o contexto clínico exibido: consulta médica, CID e auditoria clínica.", "Para prescrever, parta do atendimento e use o botão Abrir prescrição.", "Concluída a prescrição, siga para a impressão auditada." }),
            Artigo("clin-prescricao-editor", "clinico", "TODOS", "Como abrir a prescrição de uma consulta", "A prescrição nasce do atendimento e fica vinculada à consulta.", "Prescricoes", "Editor", 140, new List<string> { "No atendimento da consulta, clique em Abrir prescrição.", "Confira o vínculo com a consulta e o diagnóstico registrado.", "Preencha a prescrição conforme a conduta definida.", "Volte ao atendimento para revisar e finalizar." }),
            Artigo("clin-prescricao-imprimir", "clinico", "TODOS", "Como imprimir a prescrição com auditoria", "Gere a impressão da prescrição registrada no fluxo da consulta.", "Prescricoes", "Imprimir", 150, new List<string> { "Conclua a prescrição do atendimento.", "Acesse a impressão pelo módulo de Prescrições.", "Confira os dados antes de imprimir ou salvar em PDF.", "A impressão é registrada com trilha de auditoria." }),
            Artigo("parceiro-propostas", "parceiro", "PARCEIRO", "Como acompanhar propostas", "Veja as propostas enviadas e o status de cada uma.", "ParceiroPortal", "Propostas", 10, new List<string> { "Acesse Propostas no seu portal.", "Abra a proposta para revisar valores e condições.", "Solicite ajuste quando algo mudar.", "Use a versão aceita como referência do contrato." }),
            Artigo("parceiro-comissoes", "parceiro", "PARCEIRO", "Como acompanhar comissões e repasses", "Consulte o que foi apurado e o valor previsto de repasse.", "ParceiroPortal", "Comissoes", 20, new List<string> { "Acesse Comissões no seu portal.", "Filtre pelo período que deseja revisar.", "Compare a comissão apurada com os repasses.", "Em caso de divergência, abra um chamado em Suporte." })
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
