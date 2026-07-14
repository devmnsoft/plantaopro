using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize]
public sealed class V112HomologationController : ControllerBase
{
    private readonly V113OperationalService service;

    public V112HomologationController(V113OperationalService service)
    {
        this.service = service;
    }

    [HttpGet("api/customers")]
    [HttpGet("api/v113/customers")]
    public async Task<ActionResult<ApiResponse<PageResult<CustomerDto>>>> Customers([FromQuery] string? q, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) => ToAction(await service.ListCustomersAsync(q, status, page, pageSize));

    [HttpGet("api/customers/{id:guid}")]
    [HttpGet("api/v113/customers/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> Customer(Guid id) => ToAction(await service.GetCustomerAsync(id));

    [HttpPost("api/customers")]
    [HttpPost("api/v113/customers")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> CreateCustomer([FromBody] CustomerDto dto) => ToAction(await service.CreateCustomerAsync(dto));

    [HttpPut("api/customers/{id:guid}")]
    [HttpPut("api/v113/customers/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> UpdateCustomer(Guid id, [FromBody] CustomerDto dto) => ToAction(await service.UpdateCustomerAsync(id, dto));

    [HttpDelete("api/customers/{id:guid}")]
    [HttpDelete("api/v113/customers/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> DeleteCustomer(Guid id) => ToAction(await service.DeleteCustomerAsync(id));

    [HttpGet("api/products")]
    [HttpGet("api/v113/products")]
    public async Task<ActionResult<ApiResponse<PageResult<ProductDto>>>> Products([FromQuery] string? q, [FromQuery] string? status, [FromQuery] bool? critical, [FromQuery] int page = 1, [FromQuery] int pageSize = 20) => ToAction(await service.ListProductsAsync(q, status, critical, page, pageSize));

    [HttpGet("api/products/{id:guid}")]
    [HttpGet("api/v113/products/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Product(Guid id) => ToAction(await service.GetProductAsync(id));

    [HttpPost("api/products")]
    [HttpPost("api/v113/products")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] ProductDto dto) => ToAction(await service.CreateProductAsync(dto));

    [HttpPut("api/products/{id:guid}")]
    [HttpPut("api/v113/products/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(Guid id, [FromBody] ProductDto dto) => ToAction(await service.UpdateProductAsync(id, dto));

    [HttpDelete("api/products/{id:guid}")]
    [HttpDelete("api/v113/products/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> DeleteProduct(Guid id) => ToAction(await service.DeleteProductAsync(id));

    [HttpGet("api/inventory/balance")]
    [HttpGet("api/v113/inventory/balance")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Balance() => ToAction(await service.InventoryBalanceAsync());

    [HttpGet("api/inventory/movements")]
    [HttpGet("api/v113/inventory/movements")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Movements() => ToAction(await service.InventoryMovementsAsync());

    [HttpPost("api/inventory/entries")]
    [HttpPost("api/v113/inventory/movements")]
    public async Task<ActionResult<ApiResponse<InventoryMovementDto>>> Entry([FromBody] InventoryEntryDto dto) => ToAction(await service.EntryAsync(dto));

    [HttpGet("api/orders")]
    [HttpGet("api/v113/orders")]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrderDto>>>> Orders() => ToAction(await service.ListOrdersAsync());

    [HttpGet("api/orders/{id:guid}")]
    [HttpGet("api/v113/orders/{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Order(Guid id) => ToAction(await service.GetOrderAsync(id));

    [HttpPost("api/orders")]
    [HttpPost("api/v113/orders")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderDto dto) => ToAction(await service.CreateOrderAsync(dto));

    [HttpPost("api/orders/{id:guid}/items")]
    [HttpPost("api/v113/orders/{id:guid}/items")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> AddItem(Guid id, [FromBody] OrderItemDto dto) => ToAction(await service.AddItemAsync(id, dto));

    [HttpDelete("api/orders/{id:guid}/items/{itemId:guid}")]
    [HttpDelete("api/v113/orders/{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> RemoveItem(Guid id, Guid itemId) => ToAction(await service.RemoveItemAsync(id, itemId));

    [HttpPost("api/orders/{id:guid}/confirm")]
    [HttpPost("api/v113/orders/{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Confirm(Guid id) => ToAction(await service.ConfirmAsync(id));

    [HttpPost("api/orders/{id:guid}/cancel")]
    [HttpPost("api/v113/orders/{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Cancel(Guid id) => ToAction(await service.CancelAsync(id));

    [HttpGet("api/tasks/my")]
    [HttpGet("api/v113/tasks/my")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> MyTasks([FromQuery] string? status) => ToAction(await service.MyTasksAsync(status));

    [HttpPost("api/tasks/{id:guid}/claim")]
    [HttpPost("api/v113/tasks/{id:guid}/claim")]
    public async Task<ActionResult<ApiResponse<object>>> Claim(Guid id) => ToAction(await service.TaskActionAsync(id, "claim", null));

    [HttpPost("api/tasks/{id:guid}/comments")]
    [HttpPost("api/v113/tasks/{id:guid}/comments")]
    public async Task<ActionResult<ApiResponse<object>>> Comment(Guid id, [FromBody] CommentDto dto) => ToAction(await service.TaskActionAsync(id, "comment", dto));

    [HttpPost("api/tasks/{id:guid}/complete")]
    [HttpPost("api/v113/tasks/{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<object>>> Complete(Guid id) => ToAction(await service.TaskActionAsync(id, "complete", null));

    [HttpGet("api/billing/invoices")]
    [HttpGet("api/v113/billing/invoices")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Invoices() => ToAction(await service.InvoicesAsync());

    [HttpGet("api/billing/titles")]
    [HttpGet("api/v113/billing/titles")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Titles() => ToAction(await service.TitlesAsync());

    [HttpPost("api/billing/invoices/from-order/{orderId:guid}")]
    [HttpPost("api/v113/billing/invoices/from-order/{orderId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Invoice(Guid orderId) => ToAction(await service.InvoiceFromOrderAsync(orderId));

    [HttpPost("api/billing/invoices/{invoiceId:guid}/titles")]
    [HttpPost("api/v113/billing/invoices/{invoiceId:guid}/titles")]
    public async Task<ActionResult<ApiResponse<object>>> Title(Guid invoiceId) => ToAction(await service.TitleFromInvoiceAsync(invoiceId));

    [HttpPost("api/v113/billing/titles/{titleId:guid}/demo-boleto")]
    public async Task<ActionResult<ApiResponse<object>>> DemoBoleto(Guid titleId) => ToAction(await service.DemoBoletoAsync(titleId));

    [HttpGet("api/outbox")]
    [HttpGet("api/v113/outbox")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Outbox() => ToAction(await service.OutboxAsync());

    [HttpPost("api/outbox/{id:guid}/process")]
    [HttpPost("api/v113/outbox/{id:guid}/process")]
    public async Task<ActionResult<ApiResponse<object>>> Process(Guid id) => ToAction(await service.OutboxActionAsync(id, "process"));

    [HttpPost("api/outbox/{id:guid}/retry")]
    [HttpPost("api/v113/outbox/{id:guid}/retry")]
    public async Task<ActionResult<ApiResponse<object>>> Retry(Guid id) => ToAction(await service.OutboxActionAsync(id, "retry"));

    [HttpPost("api/v113/outbox/{id:guid}/error")]
    public async Task<ActionResult<ApiResponse<object>>> Error(Guid id) => ToAction(await service.OutboxActionAsync(id, "error"));

    [HttpGet("api/v113/outbox/{id:guid}/logs")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Logs(Guid id) => ToAction(await service.OutboxLogsAsync(id));

    [HttpGet("api/templates")]
    [HttpGet("api/v113/templates")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Templates() => ToAction(await service.TemplatesAsync());

    [HttpPost("api/templates/{id}/install")]
    [HttpPost("api/v113/templates/{id}/install")]
    public async Task<ActionResult<ApiResponse<object>>> Install(string id) => ToAction(await service.InstallTemplateAsync(id));

    [HttpGet("api/v112/dashboard")]
    [HttpGet("api/v113/dashboard")]
    public async Task<ActionResult<ApiResponse<object>>> Dashboard() => ToAction(await service.DashboardAsync());

    [HttpGet("api/journey/what-to-do-now")]
    [HttpGet("api/v113/journey/what-to-do-now")]
    public async Task<ActionResult<ApiResponse<object>>> WhatNow() => ToAction(await service.WhatNowAsync());

    [HttpGet("api/activities")]
    [HttpGet("api/v113/activities")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Activities() => ToAction(await service.ActivitiesAsync());

    [HttpGet("api/homologation/status")]
    [HttpGet("api/v113/homologation/status")]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> Homologation() => ToAction(await service.HomologationAsync());

    [HttpGet("api/validation/worker/status")]
    [HttpGet("api/v113/validation/worker/status")]
    public async Task<ActionResult<ApiResponse<object>>> Worker()
    {
        var outbox = await service.OutboxAsync();
        return ToAction(ApiResponse<object>.Ok(new { status = "READY", dataSource = "PostgreSQL", outbox = outbox.Data }, "Worker v1.13 pronto para homologação."));
    }

    private static ActionResult<ApiResponse<T>> ToAction<T>(ApiResponse<T> response)
    {
        return new ObjectResult(response) { StatusCode = response.StatusCode };
    }
}

public sealed record CustomerDto(Guid Id, string Name, string Document, string Email, bool Active, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record ProductDto(Guid Id, string Code, string Name, decimal Price, decimal MinimumStock, bool Active, decimal Balance, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record InventoryEntryDto(Guid ProductId, decimal Quantity, string? Note);
public sealed record InventoryMovementDto(Guid Id, Guid ProductId, decimal Quantity, string Type, string Note, DateTime CreatedAt);
public sealed record CreateOrderDto(Guid CustomerId);
public sealed record OrderItemDto(Guid Id, Guid ProductId, decimal Quantity, decimal UnitPrice);
public sealed record OrderDto(Guid Id, Guid CustomerId, string Status, List<OrderItemDto> Items, decimal Total, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record CommentDto(string Text);
