using Dapper;
using Npgsql;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Text.Json;

namespace PlantaoPro.Api;

public sealed class V113OperationalService
{
    private readonly IConfiguration cfg;
    private readonly ICurrentUserService currentUser;
    private readonly IAuditService audit;
    private readonly ILogger<V113OperationalService> logger;

    public V113OperationalService(IConfiguration cfg, ICurrentUserService currentUser, IAuditService audit, ILogger<V113OperationalService> logger)
    {
        this.cfg = cfg;
        this.currentUser = currentUser;
        this.audit = audit;
        this.logger = logger;
    }

    private NpgsqlConnection Cn() => new NpgsqlConnection(cfg.GetConnectionString("Default"));
    private Guid? TenantId => currentUser.ClienteId ?? currentUser.TenantId;
    private Guid? UserId => currentUser.UserId;
    private string Perfil => string.Join(',', currentUser.Roles);
    private bool Global => currentUser.IsGlobalAdmin();
    private object Scope => new { tenantId = TenantId, isGlobal = Global };

    public async Task<ApiResponse<PageResult<CustomerDto>>> ListCustomersAsync(string? q, string? status, int page, int pageSize)
    {
        try
        {
            await using var cn = Cn();
            var where = "where reg_status <> 'D' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) and (@status is null or status=@status) and (@q is null or nome ilike @like or documento ilike @like or email ilike @like)";
            var args = new { tenantId = TenantId, isGlobal = Global, status = Normalize(status), q = Blank(q), like = Like(q), skip = Skip(page, pageSize), take = Take(pageSize) };
            var total = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.v113_clientes " + where, args);
            var rows = await cn.QueryAsync<CustomerDto>("select id, nome as name, documento as document, email, status='ACTIVE' as active, created_at as createdAt, updated_at as updatedAt from plantaopro.v113_clientes " + where + " order by created_at desc offset @skip limit @take", args);
            return ApiResponse<PageResult<CustomerDto>>.Ok(new PageResult<CustomerDto>(Page(page), Take(pageSize), total, rows.ToList()), "Clientes carregados do PostgreSQL.");
        }
        catch (Exception ex) { return Fail<PageResult<CustomerDto>>(ex, "Falha ao listar clientes."); }
    }

    public async Task<ApiResponse<CustomerDto>> GetCustomerAsync(Guid id) => await QueryOne<CustomerDto>("select id, nome as name, documento as document, email, status='ACTIVE' as active, created_at as createdAt, updated_at as updatedAt from plantaopro.v113_clientes where id=@id and reg_status <> 'D' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)", new { id, tenantId = TenantId, isGlobal = Global }, "Cliente não encontrado.");

    public async Task<ApiResponse<CustomerDto>> CreateCustomerAsync(CustomerDto dto)
    {
        try
        {
            var id = Guid.NewGuid();
            await using var cn = Cn();
            await cn.ExecuteAsync("insert into plantaopro.v113_clientes(id,cliente_id,tenant_id,nome,documento,email,status,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@name,@document,@email,'ACTIVE','A',now(),@userId)", new { id, tenantId = TenantId, userId = UserId, dto.Name, dto.Document, dto.Email });
            await AddOutboxAsync(cn, "CUSTOMER_CREATED", id, new { id, dto.Name });
            await AuditAsync("v113_clientes", id, "CUSTOMER_CREATED", true, new { dto.Name });
            return await GetCustomerAsync(id);
        }
        catch (Exception ex) { return Fail<CustomerDto>(ex, "Falha ao criar cliente."); }
    }

    public async Task<ApiResponse<CustomerDto>> UpdateCustomerAsync(Guid id, CustomerDto dto)
    {
        try
        {
            await using var cn = Cn();
            var affected = await cn.ExecuteAsync("update plantaopro.v113_clientes set nome=@name, documento=@document, email=@email, updated_at=now(), updated_by=@userId where id=@id and reg_status <> 'D' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)", new { id, tenantId = TenantId, isGlobal = Global, userId = UserId, dto.Name, dto.Document, dto.Email });
            if (affected == 0) return ApiResponse<CustomerDto>.Fail("Cliente não encontrado.", 404);
            await AuditAsync("v113_clientes", id, "CUSTOMER_UPDATED", true, new { dto.Name });
            return await GetCustomerAsync(id);
        }
        catch (Exception ex) { return Fail<CustomerDto>(ex, "Falha ao atualizar cliente."); }
    }

    public async Task<ApiResponse<CustomerDto>> DeleteCustomerAsync(Guid id) => await SoftDelete<CustomerDto>("v113_clientes", id, "Cliente inativado.");

    public async Task<ApiResponse<PageResult<ProductDto>>> ListProductsAsync(string? q, string? status, bool? critical, int page, int pageSize)
    {
        try
        {
            await using var cn = Cn();
            var where = "where p.reg_status <> 'D' and (@isGlobal or @tenantId is null or p.cliente_id is null or p.cliente_id=@tenantId) and (@status is null or p.status=@status) and (@q is null or p.nome ilike @like or p.codigo ilike @like)";
            var having = critical == true ? " where balance <= minimumStock" : string.Empty;
            var args = new { tenantId = TenantId, isGlobal = Global, status = Normalize(status), q = Blank(q), like = Like(q), skip = Skip(page, pageSize), take = Take(pageSize) };
            var baseSql = "select p.id, p.codigo as code, p.nome as name, p.preco as price, p.estoque_minimo as minimumStock, p.status='ACTIVE' as active, coalesce(sum(m.quantidade),0) as balance, p.created_at as createdAt, p.updated_at as updatedAt from plantaopro.v113_produtos p left join plantaopro.v113_estoque_movimentos m on m.produto_id=p.id and m.reg_status='A' " + where + " group by p.id";
            var total = (await cn.QueryAsync<ProductDto>("select * from (" + baseSql + ") x" + having, args)).Count();
            var rows = await cn.QueryAsync<ProductDto>("select * from (" + baseSql + ") x" + having + " order by createdAt desc offset @skip limit @take", args);
            return ApiResponse<PageResult<ProductDto>>.Ok(new PageResult<ProductDto>(Page(page), Take(pageSize), total, rows.ToList()), "Produtos carregados do PostgreSQL.");
        }
        catch (Exception ex) { return Fail<PageResult<ProductDto>>(ex, "Falha ao listar produtos."); }
    }

    public async Task<ApiResponse<ProductDto>> GetProductAsync(Guid id) => await QueryOne<ProductDto>("select p.id, p.codigo as code, p.nome as name, p.preco as price, p.estoque_minimo as minimumStock, p.status='ACTIVE' as active, coalesce(sum(m.quantidade),0) as balance, p.created_at as createdAt, p.updated_at as updatedAt from plantaopro.v113_produtos p left join plantaopro.v113_estoque_movimentos m on m.produto_id=p.id and m.reg_status='A' where p.id=@id and p.reg_status <> 'D' and (@isGlobal or @tenantId is null or p.cliente_id is null or p.cliente_id=@tenantId) group by p.id", new { id, tenantId = TenantId, isGlobal = Global }, "Produto não encontrado.");

    public async Task<ApiResponse<ProductDto>> CreateProductAsync(ProductDto dto)
    {
        try
        {
            var id = Guid.NewGuid(); await using var cn = Cn();
            await cn.ExecuteAsync("insert into plantaopro.v113_produtos(id,cliente_id,tenant_id,codigo,nome,preco,estoque_minimo,status,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@code,@name,@price,@minimumStock,'ACTIVE','A',now(),@userId)", new { id, tenantId = TenantId, userId = UserId, dto.Code, dto.Name, dto.Price, dto.MinimumStock });
            await AddOutboxAsync(cn, "PRODUCT_CREATED", id, new { id, dto.Code });
            await AuditAsync("v113_produtos", id, "PRODUCT_CREATED", true, new { dto.Code });
            return await GetProductAsync(id);
        }
        catch (Exception ex) { return Fail<ProductDto>(ex, "Falha ao criar produto."); }
    }

    public async Task<ApiResponse<ProductDto>> UpdateProductAsync(Guid id, ProductDto dto)
    {
        try
        {
            await using var cn = Cn();
            var affected = await cn.ExecuteAsync("update plantaopro.v113_produtos set codigo=@code,nome=@name,preco=@price,estoque_minimo=@minimumStock,updated_at=now(),updated_by=@userId where id=@id and reg_status <> 'D' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)", new { id, tenantId = TenantId, isGlobal = Global, userId = UserId, dto.Code, dto.Name, dto.Price, dto.MinimumStock });
            if (affected == 0) return ApiResponse<ProductDto>.Fail("Produto não encontrado.", 404);
            await AuditAsync("v113_produtos", id, "PRODUCT_UPDATED", true, new { dto.Code });
            return await GetProductAsync(id);
        }
        catch (Exception ex) { return Fail<ProductDto>(ex, "Falha ao atualizar produto."); }
    }

    public async Task<ApiResponse<ProductDto>> DeleteProductAsync(Guid id) => await SoftDelete<ProductDto>("v113_produtos", id, "Produto inativado.");

    public async Task<ApiResponse<IEnumerable<object>>> InventoryBalanceAsync() => await QueryList("select p.id as productId,p.codigo as code,p.nome as name,coalesce(sum(m.quantidade),0) as balance,p.estoque_minimo as minimumStock,(coalesce(sum(m.quantidade),0)<=p.estoque_minimo) as critical from plantaopro.v113_produtos p left join plantaopro.v113_estoque_movimentos m on m.produto_id=p.id and m.reg_status='A' where p.reg_status <> 'D' and (@isGlobal or @tenantId is null or p.cliente_id is null or p.cliente_id=@tenantId) group by p.id order by p.nome", "Saldo carregado.");
    public async Task<ApiResponse<IEnumerable<object>>> InventoryMovementsAsync() => await QueryList("select id,produto_id as productId,quantidade as quantity,tipo as type,observacao as note,created_at as createdAt from plantaopro.v113_estoque_movimentos where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Movimentos carregados.");

    public async Task<ApiResponse<InventoryMovementDto>> EntryAsync(InventoryEntryDto dto)
    {
        try
        {
            var id = Guid.NewGuid(); await using var cn = Cn();
            var product = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.v113_produtos where id=@productId and reg_status <> 'D' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)", new { dto.ProductId, tenantId = TenantId, isGlobal = Global });
            if (product == 0) return ApiResponse<InventoryMovementDto>.Fail("Produto inválido.", 400);
            await cn.ExecuteAsync("insert into plantaopro.v113_estoque_movimentos(id,cliente_id,tenant_id,produto_id,quantidade,tipo,observacao,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@productId,@quantity,'ENTRY',@note,'A',now(),@userId)", new { id, tenantId = TenantId, userId = UserId, dto.ProductId, dto.Quantity, note = dto.Note ?? "Entrada operacional v1.13" });
            await AuditAsync("v113_estoque_movimentos", id, "INVENTORY_ENTRY", true, new { dto.ProductId, dto.Quantity });
            return ApiResponse<InventoryMovementDto>.Ok(new InventoryMovementDto(id, dto.ProductId, dto.Quantity, "ENTRY", dto.Note ?? "Entrada operacional v1.13", DateTime.UtcNow), "Entrada registrada.");
        }
        catch (Exception ex) { return Fail<InventoryMovementDto>(ex, "Falha ao registrar estoque."); }
    }

    public async Task<ApiResponse<IEnumerable<OrderDto>>> ListOrdersAsync() { var rows = await LoadOrdersAsync(null); return ApiResponse<IEnumerable<OrderDto>>.Ok(rows, "Pedidos carregados."); }
    public async Task<ApiResponse<OrderDto>> GetOrderAsync(Guid id) { var rows = await LoadOrdersAsync(id); var one = rows.FirstOrDefault(); return one is null ? ApiResponse<OrderDto>.Fail("Pedido não encontrado.", 404) : ApiResponse<OrderDto>.Ok(one, "Pedido encontrado."); }
    public async Task<ApiResponse<OrderDto>> CreateOrderAsync(CreateOrderDto dto)
    {
        try { var id = Guid.NewGuid(); await using var cn = Cn(); await cn.ExecuteAsync("insert into plantaopro.v113_pedidos(id,cliente_id,tenant_id,cliente_operacional_id,status,total,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@customerId,'DRAFT',0,'A',now(),@userId)", new { id, tenantId = TenantId, userId = UserId, dto.CustomerId }); await AddOutboxAsync(cn, "ORDER_CREATED", id, new { id }); await AuditAsync("v113_pedidos", id, "ORDER_CREATED", true, new { dto.CustomerId }); return await GetOrderAsync(id); }
        catch (Exception ex) { return Fail<OrderDto>(ex, "Falha ao criar pedido."); }
    }
    public async Task<ApiResponse<OrderDto>> AddItemAsync(Guid id, OrderItemDto dto)
    {
        try { var itemId = Guid.NewGuid(); await using var cn = Cn(); var price = dto.UnitPrice <= 0 ? await cn.ExecuteScalarAsync<decimal>("select preco from plantaopro.v113_produtos where id=@productId", new { dto.ProductId }) : dto.UnitPrice; await cn.ExecuteAsync("insert into plantaopro.v113_pedido_itens(id,cliente_id,tenant_id,pedido_id,produto_id,quantidade,valor_unitario,reg_status,created_at,created_by) values(@itemId,@tenantId,@tenantId,@id,@productId,@quantity,@price,'A',now(),@userId); update plantaopro.v113_pedidos set total=(select coalesce(sum(quantidade*valor_unitario),0) from plantaopro.v113_pedido_itens where pedido_id=@id and reg_status='A'), updated_at=now(), updated_by=@userId where id=@id", new { itemId, id, tenantId = TenantId, userId = UserId, dto.ProductId, dto.Quantity, price }); return await GetOrderAsync(id); }
        catch (Exception ex) { return Fail<OrderDto>(ex, "Falha ao adicionar item."); }
    }
    public async Task<ApiResponse<OrderDto>> RemoveItemAsync(Guid id, Guid itemId)
    {
        try { await using var cn = Cn(); await cn.ExecuteAsync("update plantaopro.v113_pedido_itens set reg_status='D',updated_at=now(),updated_by=@userId where pedido_id=@id and id=@itemId; update plantaopro.v113_pedidos set total=(select coalesce(sum(quantidade*valor_unitario),0) from plantaopro.v113_pedido_itens where pedido_id=@id and reg_status='A'), updated_at=now(), updated_by=@userId where id=@id", new { id, itemId, userId = UserId }); return await GetOrderAsync(id); }
        catch (Exception ex) { return Fail<OrderDto>(ex, "Falha ao remover item."); }
    }
    public async Task<ApiResponse<OrderDto>> ConfirmAsync(Guid id)
    {
        try
        {
            await using var cn = Cn();
            var missing = await cn.QueryFirstOrDefaultAsync<Guid?>("select i.produto_id from plantaopro.v113_pedido_itens i left join plantaopro.v113_estoque_movimentos m on m.produto_id=i.produto_id and m.reg_status='A' where i.pedido_id=@id and i.reg_status='A' group by i.produto_id,i.quantidade having coalesce(sum(m.quantidade),0)<i.quantidade limit 1", new { id });
            if (missing.HasValue) return ApiResponse<OrderDto>.Fail("Estoque insuficiente para confirmar o pedido.", 400, new List<string> { missing.Value.ToString() });
            var items = (await cn.QueryAsync<OrderItemDto>("select id,produto_id as productId,quantidade as quantity,valor_unitario as unitPrice from plantaopro.v113_pedido_itens where pedido_id=@id and reg_status='A'", new { id })).ToList();
            foreach (var item in items) await cn.ExecuteAsync("insert into plantaopro.v113_estoque_movimentos(id,cliente_id,tenant_id,produto_id,quantidade,tipo,observacao,pedido_id,reg_status,created_at,created_by) values(@movId,@tenantId,@tenantId,@productId,@quantity,'ORDER_CONFIRM',@note,@id,'A',now(),@userId)", new { movId = Guid.NewGuid(), tenantId = TenantId, userId = UserId, item.ProductId, quantity = -item.Quantity, id, note = "Pedido " + id });
            await cn.ExecuteAsync("update plantaopro.v113_pedidos set status='CONFIRMED', updated_at=now(), updated_by=@userId where id=@id", new { id, userId = UserId });
            await cn.ExecuteAsync("insert into plantaopro.v113_tarefas(id,cliente_id,tenant_id,pedido_id,titulo,status,comentarios,reg_status,created_at,created_by) values(@taskId,@tenantId,@tenantId,@id,'Separar pedido','PENDING','[]'::jsonb,'A',now(),@userId)", new { taskId = Guid.NewGuid(), tenantId = TenantId, userId = UserId, id });
            await AddOutboxAsync(cn, "ORDER_CONFIRMED", id, new { id }); await AuditAsync("v113_pedidos", id, "ORDER_CONFIRMED", true, new { id }); return await GetOrderAsync(id);
        }
        catch (Exception ex) { return Fail<OrderDto>(ex, "Falha ao confirmar pedido."); }
    }
    public async Task<ApiResponse<OrderDto>> CancelAsync(Guid id) { try { await using var cn = Cn(); await cn.ExecuteAsync("update plantaopro.v113_pedidos set status='CANCELED',updated_at=now(),updated_by=@userId where id=@id", new { id, userId = UserId }); await AddOutboxAsync(cn, "ORDER_CANCELED", id, new { id }); await AuditAsync("v113_pedidos", id, "ORDER_CANCELED", true, new { id }); return await GetOrderAsync(id); } catch (Exception ex) { return Fail<OrderDto>(ex, "Falha ao cancelar pedido."); } }

    public async Task<ApiResponse<IEnumerable<object>>> MyTasksAsync(string? status) => await QueryList("select id,pedido_id as orderId,titulo as title,status,responsavel as owner,comentarios as comments,created_at as createdAt,updated_at as updatedAt from plantaopro.v113_tarefas where reg_status='A' and (@status is null or status=@status) and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Tarefas carregadas.", new { tenantId = TenantId, isGlobal = Global, status = Normalize(status) });
    public async Task<ApiResponse<object>> TaskActionAsync(Guid id, string action, CommentDto? comment)
    {
        try { await using var cn = Cn(); var sql = action == "comment" ? "update plantaopro.v113_tarefas set comentarios=coalesce(comentarios,'[]'::jsonb) || to_jsonb(@text::text),updated_at=now(),updated_by=@userId where id=@id" : "update plantaopro.v113_tarefas set status=@status,responsavel=coalesce(responsavel,@owner),updated_at=now(),updated_by=@userId where id=@id"; await cn.ExecuteAsync(sql, new { id, userId = UserId, status = action == "complete" ? "DONE" : "CLAIMED", owner = UserId?.ToString() ?? "homologador", text = comment?.Text ?? string.Empty }); await AuditAsync("v113_tarefas", id, "TASK_" + action.ToUpperInvariant(), true, new { id }); var row = await cn.QueryFirstOrDefaultAsync<object>("select * from plantaopro.v113_tarefas where id=@id", new { id }); return ApiResponse<object>.Ok(row!, "Tarefa atualizada."); } catch (Exception ex) { return Fail<object>(ex, "Falha ao atualizar tarefa."); }
    }

    public async Task<ApiResponse<IEnumerable<object>>> InvoicesAsync() => await QueryList("select id,pedido_id as orderId,valor as amount,status,created_at as createdAt from plantaopro.v113_faturas where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Faturas carregadas.");
    public async Task<ApiResponse<IEnumerable<object>>> TitlesAsync() => await QueryList("select id,fatura_id as invoiceId,valor as amount,status,demo_boleto as demoBoleto,vencimento as dueDate from plantaopro.v113_titulos where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Títulos carregados.");
    public async Task<ApiResponse<object>> InvoiceFromOrderAsync(Guid orderId) { try { var id = Guid.NewGuid(); await using var cn = Cn(); var amount = await cn.ExecuteScalarAsync<decimal?>("select total from plantaopro.v113_pedidos where id=@orderId and status='CONFIRMED'", new { orderId }); if (!amount.HasValue) return ApiResponse<object>.Fail("Pedido não confirmado.", 400); await cn.ExecuteAsync("insert into plantaopro.v113_faturas(id,cliente_id,tenant_id,pedido_id,valor,status,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@orderId,@amount,'ISSUED','A',now(),@userId)", new { id, tenantId = TenantId, userId = UserId, orderId, amount }); await AddOutboxAsync(cn, "INVOICE_ISSUED", id, new { id, orderId }); await AuditAsync("v113_faturas", id, "INVOICE_ISSUED", true, new { orderId }); return ApiResponse<object>.Ok((await cn.QueryFirstAsync<object>("select * from plantaopro.v113_faturas where id=@id", new { id })), "Fatura emitida."); } catch (Exception ex) { return Fail<object>(ex, "Falha ao emitir fatura."); } }
    public async Task<ApiResponse<object>> TitleFromInvoiceAsync(Guid invoiceId) { try { var id = Guid.NewGuid(); await using var cn = Cn(); var amount = await cn.ExecuteScalarAsync<decimal?>("select valor from plantaopro.v113_faturas where id=@invoiceId", new { invoiceId }); if (!amount.HasValue) return ApiResponse<object>.Fail("Fatura não encontrada.", 404); await cn.ExecuteAsync("insert into plantaopro.v113_titulos(id,cliente_id,tenant_id,fatura_id,valor,status,demo_boleto,vencimento,reg_status,created_at,created_by) values(@id,@tenantId,@tenantId,@invoiceId,@amount,'OPEN',false,now()+interval '7 days','A',now(),@userId)", new { id, tenantId = TenantId, userId = UserId, invoiceId, amount }); await AddOutboxAsync(cn, "TITLE_CREATED", id, new { id, invoiceId }); await AuditAsync("v113_titulos", id, "TITLE_CREATED", true, new { invoiceId }); return ApiResponse<object>.Ok((await cn.QueryFirstAsync<object>("select * from plantaopro.v113_titulos where id=@id", new { id })), "Título criado."); } catch (Exception ex) { return Fail<object>(ex, "Falha ao criar título."); } }
    public async Task<ApiResponse<object>> DemoBoletoAsync(Guid titleId) { try { await using var cn = Cn(); var affected = await cn.ExecuteAsync("update plantaopro.v113_titulos set demo_boleto=true,status='DEMO_BOLETO',updated_at=now(),updated_by=@userId where id=@titleId", new { titleId, userId = UserId }); if (affected == 0) return ApiResponse<object>.Fail("Título não encontrado.", 404); await AddOutboxAsync(cn, "DEMO_BOLETO_CREATED", titleId, new { titleId, message = "Boleto demonstrativo sem valor financeiro real." }); await AuditAsync("v113_titulos", titleId, "DEMO_BOLETO_CREATED", true, new { titleId }); return ApiResponse<object>.Ok(new { titleId, status = "DEMO_BOLETO", message = "Boleto demonstrativo sem valor financeiro real." }, "Boleto demonstrativo gerado."); } catch (Exception ex) { return Fail<object>(ex, "Falha ao gerar boleto demonstrativo."); } }

    public async Task<ApiResponse<IEnumerable<object>>> OutboxAsync() => await QueryList("select id,tipo as type,payload_ref as payloadRef,status,erro as error,created_at as createdAt from plantaopro.v113_outbox_eventos where reg_status='A' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", "Outbox carregada.");
    public async Task<ApiResponse<object>> OutboxActionAsync(Guid id, string action) { try { var status = action == "error" ? "ERROR" : action == "retry" ? "PENDING" : "PROCESSED"; await using var cn = Cn(); await cn.ExecuteAsync("update plantaopro.v113_outbox_eventos set status=@status,erro=case when @status='ERROR' then 'Erro marcado manualmente em homologação' else null end,updated_at=now(),updated_by=@userId where id=@id; insert into plantaopro.v113_outbox_logs(id,cliente_id,tenant_id,outbox_evento_id,status,detalhe,reg_status,created_at,created_by) values(gen_random_uuid(),@tenantId,@tenantId,@id,@status,@detail,'A',now(),@userId)", new { id, status, userId = UserId, tenantId = TenantId, detail = "Ação " + action + " registrada sem envio externo de produção." }); return ApiResponse<object>.Ok((await cn.QueryFirstAsync<object>("select * from plantaopro.v113_outbox_eventos where id=@id", new { id })), "Outbox atualizada."); } catch (Exception ex) { return Fail<object>(ex, "Falha ao atualizar outbox."); } }
    public async Task<ApiResponse<IEnumerable<object>>> OutboxLogsAsync(Guid id) => await QueryList("select * from plantaopro.v113_outbox_logs where outbox_evento_id=@id order by created_at desc", "Logs carregados.", new { id, tenantId = TenantId, isGlobal = Global });

    public async Task<ApiResponse<IEnumerable<object>>> TemplatesAsync() => await QueryList("select id,codigo as id,nome as name,descricao as description from plantaopro.v113_templates where reg_status='A' order by nome", "Templates carregados.");
    public async Task<ApiResponse<object>> InstallTemplateAsync(string id) { try { await using var cn = Cn(); var template = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.v113_templates where codigo=@id and reg_status='A'", new { id }); if (!template.HasValue) return ApiResponse<object>.Fail("Template não encontrado.", 404); var inst = Guid.NewGuid(); await cn.ExecuteAsync("insert into plantaopro.v113_template_instalacoes(id,cliente_id,tenant_id,template_id,status,reg_status,created_at,created_by) values(@inst,@tenantId,@tenantId,@template,'INSTALLED','A',now(),@userId)", new { inst, tenantId = TenantId, userId = UserId, template }); return ApiResponse<object>.Ok(new { id = inst, templateId = id, status = "INSTALLED", installedAt = DateTime.UtcNow }, "Template instalado."); } catch (Exception ex) { return Fail<object>(ex, "Falha ao instalar template."); } }
    public async Task<ApiResponse<object>> DashboardAsync() { try { await using var cn = Cn(); var d = await cn.QueryFirstAsync<object>(@"select (select count(1) from plantaopro.v113_clientes where reg_status='A') as activeCustomers,(select count(1) from plantaopro.v113_produtos where reg_status='A') as activeProducts,(select count(1) from plantaopro.v113_pedidos where status='DRAFT' and reg_status='A') as draftOrders,(select count(1) from plantaopro.v113_tarefas where status<>'DONE' and reg_status='A') as pendingTasks,(select count(1) from plantaopro.v113_faturas where status='ISSUED' and reg_status='A') as issuedInvoices,(select count(1) from plantaopro.v113_titulos where status in ('OPEN','DEMO_BOLETO') and reg_status='A') as openTitles,(select count(1) from plantaopro.v113_outbox_eventos where status='PENDING' and reg_status='A') as pendingOutbox", Scope); return ApiResponse<object>.Ok(d, "Dashboard PostgreSQL carregado."); } catch (Exception ex) { return Fail<object>(ex, "Falha ao carregar dashboard."); } }
    public Task<ApiResponse<object>> WhatNowAsync() => Task.FromResult(ApiResponse<object>.Ok(new { code = "FOLLOW_DASHBOARD", label = "Acompanhar dashboard v1.13", url = "/dashboard", dataSource = "PostgreSQL" }, "Próxima ação calculada."));
    public async Task<ApiResponse<IEnumerable<object>>> ActivitiesAsync() => await QueryList("select id,tipo as module,descricao as status,created_at as createdAt from plantaopro.v113_atividades where reg_status='A' order by created_at desc", "Atividades carregadas.");
    public async Task<ApiResponse<IEnumerable<object>>> HomologationAsync() => await QueryList("select codigo as code,status,detalhe as detail,erro as error,open_url as openUrl from plantaopro.v113_jornada_acoes where reg_status='A' order by ordem", "Status de homologação carregado.");

    private async Task<List<OrderDto>> LoadOrdersAsync(Guid? id)
    {
        await using var cn = Cn();
        var orderRows = (await cn.QueryAsync<OrderRow>("select id,cliente_operacional_id as customerId,status,total,created_at as createdAt,updated_at as updatedAt from plantaopro.v113_pedidos where reg_status='A' and (@id is null or id=@id) and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId) order by created_at desc", new { id, tenantId = TenantId, isGlobal = Global })).ToList();
        var itemRows = (await cn.QueryAsync<OrderItemRow>("select id,pedido_id as orderId,produto_id as productId,quantidade as quantity,valor_unitario as unitPrice from plantaopro.v113_pedido_itens where reg_status='A' and pedido_id=any(@ids)", new { ids = orderRows.Select(x => x.Id).ToArray() })).ToList();
        return orderRows.Select(o => new OrderDto(o.Id, o.CustomerId, o.Status, itemRows.Where(i => i.OrderId == o.Id).Select(i => new OrderItemDto(i.Id, i.ProductId, i.Quantity, i.UnitPrice)).ToList(), o.Total, o.CreatedAt, o.UpdatedAt)).ToList();
    }

    private async Task<ApiResponse<T>> QueryOne<T>(string sql, object args, string notFound)
    {
        try { await using var cn = Cn(); var row = await cn.QueryFirstOrDefaultAsync<T>(sql, args); return row is null ? ApiResponse<T>.Fail(notFound, 404) : ApiResponse<T>.Ok(row, "Registro carregado."); } catch (Exception ex) { return Fail<T>(ex, "Falha ao consultar registro."); }
    }
    private async Task<ApiResponse<IEnumerable<object>>> QueryList(string sql, string message) => await QueryList(sql, message, Scope);
    private async Task<ApiResponse<IEnumerable<object>>> QueryList(string sql, string message, object args) { try { await using var cn = Cn(); var rows = await cn.QueryAsync<object>(sql, args); return ApiResponse<IEnumerable<object>>.Ok(rows.ToList(), message); } catch (Exception ex) { return Fail<IEnumerable<object>>(ex, message); } }
    private async Task<ApiResponse<T>> SoftDelete<T>(string table, Guid id, string message)
    {
        try { await using var cn = Cn(); var affected = await cn.ExecuteAsync("update plantaopro." + table + " set reg_status='D', status='INACTIVE', updated_at=now(), updated_by=@userId where id=@id and reg_status <> 'D' and (@isGlobal or @tenantId is null or cliente_id is null or cliente_id=@tenantId)", new { id, userId = UserId, tenantId = TenantId, isGlobal = Global }); if (affected == 0) return ApiResponse<T>.Fail("Registro não encontrado.", 404); await AuditAsync(table, id, "SOFT_DELETE", true, new { id }); return ApiResponse<T>.Ok(default!, message); } catch (Exception ex) { return Fail<T>(ex, "Falha ao inativar registro."); }
    }
    private async Task AddOutboxAsync(NpgsqlConnection cn, string type, Guid reference, object payload) => await cn.ExecuteAsync("insert into plantaopro.v113_outbox_eventos(id,cliente_id,tenant_id,tipo,payload_ref,payload,status,reg_status,created_at,created_by) values(gen_random_uuid(),@tenantId,@tenantId,@type,@reference,cast(@payload as jsonb),'PENDING','A',now(),@userId)", new { tenantId = TenantId, userId = UserId, type, reference = reference.ToString(), payload = JsonSerializer.Serialize(payload) });
    private async Task AuditAsync(string entity, Guid id, string action, bool success, object details) => await audit.RegistrarAsync(UserId, TenantId, entity, id, action, details, success, null, Perfil);
    private ApiResponse<T> Fail<T>(Exception ex, string message) { logger.LogError(ex, "V1.13 operacional: {Message}", message); return ApiResponse<T>.Fail(message, 500); }
    private static int Page(int page) => Math.Max(1, page); private static int Take(int pageSize) => Math.Clamp(pageSize, 1, 100); private static int Skip(int page, int pageSize) => (Page(page) - 1) * Take(pageSize); private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant(); private static string? Like(string? value) => string.IsNullOrWhiteSpace(value) ? null : "%" + value.Trim() + "%";
    private sealed record OrderRow(Guid Id, Guid CustomerId, string Status, decimal Total, DateTime CreatedAt, DateTime? UpdatedAt);
    private sealed record OrderItemRow(Guid Id, Guid OrderId, Guid ProductId, decimal Quantity, decimal UnitPrice);
}

public sealed record PageResult<T>(int Page, int PageSize, int Total, List<T> Items);
