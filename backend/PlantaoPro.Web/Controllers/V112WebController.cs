using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

public sealed class V112WebController : Controller
{
    [HttpGet("dashboard")] public IActionResult Dashboard() => Page("Dashboard", "Indicadores atualizados por APIs reais.", "/api/dashboard", "Atualizar dashboard");
    [HttpGet("activities")] public IActionResult Activities() => Page("Atividades", "Central de ações agrupadas por módulo.", "/api/activities", "Carregar atividades");
    [HttpGet("demo")] public IActionResult Demo() => Page("Demo funcional", "Timeline executável da homologação v1.12.", "/api/demo/status", "Ver status da demo");
    [HttpGet("homologation")] public IActionResult Homologation() => Page("Homologação visual", "Checks funcionais de banco, API, Web, worker e módulos.", "/api/homologation/status", "Validar homologação");
    [HttpGet("journey/what-to-do-now")] public IActionResult Journey() => Page("Jornada", "Próxima ação calculada com base nos dados reais.", "/api/journey/what-to-do-now", "Ver próxima ação");
    [HttpGet("customers")] [HttpGet("customers/create")] [HttpGet("customers/{id:guid}")] [HttpGet("customers/{id:guid}/edit")] public IActionResult Customers() => Page("Clientes", "CRUD completo com criação, edição, detalhe e inativação.", "/api/customers", "Salvar/Listar clientes");
    [HttpGet("products")] [HttpGet("products/create")] [HttpGet("products/{id:guid}")] [HttpGet("products/{id:guid}/edit")] public IActionResult Products() => Page("Produtos", "CRUD completo com saldo e estoque crítico.", "/api/products", "Salvar/Listar produtos");
    [HttpGet("inventory")] [HttpGet("inventory/entry")] [HttpGet("inventory/movements")] [HttpGet("inventory/critical")] public IActionResult Inventory() => Page("Estoque", "Saldos, movimentos e entrada operacional.", "/api/inventory/balance", "Consultar estoque");
    [HttpGet("orders")] [HttpGet("orders/create")] [HttpGet("orders/{id:guid}")] [HttpGet("orders/{id:guid}/billing")] public IActionResult Orders() => Page("Pedidos", "Criação, itens, confirmação, cancelamento e faturamento.", "/api/orders", "Operar pedidos");
    [HttpGet("tasks/my")] [HttpGet("tasks/{id:guid}")] public IActionResult Tasks() => Page("Minhas tarefas", "Assumir, comentar e concluir tarefas operacionais.", "/api/tasks/my", "Atualizar tarefas");
    [HttpGet("billing/invoices")] [HttpGet("billing/titles")] public IActionResult Billing() => Page("Faturamento", "Faturas, títulos e boleto fake com outbox de cobrança.", "/api/billing/invoices", "Atualizar faturamento");
    [HttpGet("connect/outbox")] public IActionResult Outbox() => Page("Outbox", "Processamento visível de eventos pendentes, processados e erros.", "/api/outbox", "Processar outbox");
    [HttpGet("templates")] [HttpGet("templates/{id}")] [HttpGet("templates/{id}/install")] public IActionResult Templates() => Page("Templates", "Templates instaláveis para fluxos operacionais.", "/api/templates", "Instalar template");

    private IActionResult Page(string title, string description, string endpoint, string primaryAction)
    {
        ViewData["Title"] = title; ViewData["Description"] = description; ViewData["Endpoint"] = endpoint; ViewData["PrimaryAction"] = primaryAction; return View("OperationalPage");
    }
}
