using Microsoft.AspNetCore.Mvc;

namespace PlantaoPro.Web.Controllers;

public sealed class V112WebController : Controller
{
    [HttpGet("dashboard")] public IActionResult Dashboard() => Page("Dashboard", "Indicadores atualizados por APIs reais.", "/api/v113/dashboard", "Atualizar dashboard");
    [HttpGet("activities")] public IActionResult Activities() => Page("Atividades", "Central de ações agrupadas por módulo.", "/api/v113/activities", "Carregar atividades");
    [HttpGet("demo")] public IActionResult Demo() => Page("Compatibilidade 1.12", "Linha do tempo executável desta versão de compatibilidade.", "/api/v113/homologation/status", "Ver status");
    [HttpGet("homologation")] public IActionResult Homologation() => Page("Checagens 1.12", "Verificações funcionais de banco, API, Web, worker e módulos.", "/api/v113/homologation/status", "Executar checagens");
    [HttpGet("journey/what-to-do-now")] public IActionResult Journey() => Page("Jornada", "Próxima ação calculada com base nos dados reais.", "/api/v113/journey/what-to-do-now", "Ver próxima ação");
    [HttpGet("customers")] [HttpGet("customers/create")] [HttpGet("customers/{id:guid}")] [HttpGet("customers/{id:guid}/edit")] public IActionResult Customers() => Page("Clientes", "CRUD completo com criação, edição, detalhe e inativação.", "/api/v113/customers", "Salvar/Listar clientes");
    [HttpGet("products")] [HttpGet("products/create")] [HttpGet("products/{id:guid}")] [HttpGet("products/{id:guid}/edit")] public IActionResult Products() => Page("Produtos", "CRUD completo com saldo e estoque crítico.", "/api/v113/products", "Salvar/Listar produtos");
    [HttpGet("inventory")] [HttpGet("inventory/entry")] [HttpGet("inventory/movements")] [HttpGet("inventory/critical")] public IActionResult Inventory() => Page("Estoque", "Saldos, movimentos e entrada operacional.", "/api/v113/inventory/balance", "Consultar estoque");
    [HttpGet("orders")] [HttpGet("orders/create")] [HttpGet("orders/{id:guid}")] [HttpGet("orders/{id:guid}/billing")] public IActionResult Orders() => Page("Pedidos", "Criação, itens, confirmação, cancelamento e faturamento.", "/api/v113/orders", "Operar pedidos");
    [HttpGet("tasks/my")] [HttpGet("tasks/{id:guid}")] public IActionResult Tasks() => Page("Minhas tarefas", "Assumir, comentar e concluir tarefas operacionais.", "/api/v113/tasks/my", "Atualizar tarefas");
    [HttpGet("billing/invoices")] [HttpGet("billing/titles")] public IActionResult Billing() => Page("Faturamento", "Faturas, títulos e boleto de exemplo com fila de envio persistida.", "/api/v113/billing/invoices", "Atualizar faturamento");
    [HttpGet("connect/outbox")] public IActionResult Outbox() => Page("Outbox", "Processamento visível de eventos pendentes, processados e erros.", "/api/v113/outbox", "Processar outbox");
    [HttpGet("templates")] [HttpGet("templates/{id}")] [HttpGet("templates/{id}/install")] public IActionResult Templates() => Page("Templates", "Templates instaláveis para fluxos operacionais.", "/api/v113/templates", "Instalar template");

    private IActionResult Page(string title, string description, string endpoint, string primaryAction)
    {
        ViewData["Title"] = title; ViewData["Description"] = description; ViewData["Endpoint"] = endpoint; ViewData["PrimaryAction"] = primaryAction; return View("OperationalPage");
    }
}
