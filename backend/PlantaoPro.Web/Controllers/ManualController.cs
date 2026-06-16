using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

[Authorize]
public sealed class ManualController : Controller
{
    public IActionResult Index() { return View("Manual", Build("Manual do sistema por perfil", "Escolha o perfil para ver a jornada, menus, limites de acesso, dúvidas frequentes e suporte.")); }
    public IActionResult Perfil() { return View("Manual", Build("Manual do meu perfil", "Use este guia para entender o que fazer agora sem depender de termos técnicos.")); }
    public IActionResult Recepcao() { return View("Manual", Build("Manual da Recepção", "Buscar paciente, cadastrar, agendar, fazer check-in, chamar no painel e acompanhar pendências.")); }
    public IActionResult Triagem() { return View("Manual", Build("Manual da Triagem", "Organizar fila, registrar queixa e sinais vitais, classificar risco e encaminhar para consulta.")); }
    public IActionResult Medico() { return View("Manual", Build("Manual do Médico", "Ver agenda, abrir consulta, revisar triagem, pesquisar CID, prescrever e finalizar atendimento.")); }
    public IActionResult Financeiro() { return View("Manual", Build("Manual do Financeiro", "Controlar contas a receber, recebimentos, caixa, repasses, glosas e relatórios.")); }
    public IActionResult AdminClinica() { return View("Manual", Build("Manual do Admin da Clínica", "Configurar usuários, perfis, LGPD, auditoria, white label, convênios e módulos contratados.")); }
    public IActionResult AdminSaas() { return View("Manual", Build("Manual do Admin SaaS", "Gerir clientes, tenants, planos, assinaturas, billing, marketplace e parceiros.")); }
    public IActionResult Convenios() { return View("Manual", Build("Manual de Convênios", "Cadastrar convênios e planos, acompanhar autorizações, glosas e faturamento.")); }
    public IActionResult Parceiro() { return View("Manual", Build("Manual do Parceiro", "Acompanhar leads, propostas, clientes, comissões e materiais sem acessar dados clínicos.")); }
    public IActionResult Auditor() { return View("Manual", Build("Manual do Auditor", "Consultar trilhas de auditoria e evidências de leitura sem executar ações operacionais.")); }

    private static ManualPage Build(string title, string description)
    {
        return new ManualPage { Title = title, Description = description };
    }
}

public sealed class ManualPage
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
