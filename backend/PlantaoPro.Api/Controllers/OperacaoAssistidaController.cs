using System.Collections.Concurrent;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,COORDENACAO")]
[Route("api/operacao-assistida")]
public sealed class OperacaoAssistidaController : ControllerBase
{
    private static readonly ConcurrentDictionary<Guid, OperacaoAssistidaChecklistDto> ChecklistOverrides = new();
    private static readonly ConcurrentDictionary<Guid, OperacaoAssistidaOcorrenciaDto> OcorrenciasMemoria = new();
    private static readonly ConcurrentDictionary<Guid, OperacaoAssistidaTreinamentoDto> TreinamentosMemoria = new();
    private readonly IConfiguration _cfg;
    private readonly IAuditService _audit;
    private readonly UsuarioContextService _usuarioContext;
    private readonly TenantGuardService _tenantGuard;
    private readonly ILogger<OperacaoAssistidaController> _logger;

    public OperacaoAssistidaController(
        IConfiguration cfg,
        IAuditService audit,
        UsuarioContextService usuarioContext,
        TenantGuardService tenantGuard,
        ILogger<OperacaoAssistidaController> logger)
    {
        _cfg = cfg;
        _audit = audit;
        _usuarioContext = usuarioContext;
        _tenantGuard = tenantGuard;
        _logger = logger;
    }

    [HttpGet("clientes")]
    public async Task<IActionResult> ListarClientes([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 24)
    {
        try
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);
            var offset = (page - 1) * pageSize;
            var filtroCliente = _usuarioContext.IsAdminGlobal() ? null : _usuarioContext.GetClienteId();

            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var where = "c.reg_status = 'A' and (@clienteId is null or c.id = @clienteId) and (@status is null or oa.status = @status)";
            var total = await cn.ExecuteScalarAsync<long>($@"select count(1)
from plantaopro.clientes c
left join plantaopro.operacao_assistida_clientes oa on oa.cliente_id = c.id and oa.reg_status = 'A'
where {where}", new { clienteId = filtroCliente, status });

            var itens = (await cn.QueryAsync<OperacaoAssistidaClienteDto>($@"select
    c.id as ClienteId,
    coalesce(c.nome_fantasia, c.razao_social, 'Cliente') as ClienteNome,
    coalesce(c.status, 'ATIVO') as ClienteStatus,
    coalesce(oa.status, 'NAO_INICIADA') as Status,
    coalesce(oa.responsavel, 'MNSOFT') as Responsavel,
    oa.inicio_previsto as InicioPrevisto,
    oa.go_live_previsto as GoLivePrevisto,
    coalesce(oa.percentual, 0)::int as Percentual,
    coalesce(oa.risco, 'BAIXO') as Risco,
    coalesce(oa.observacoes, '') as Observacoes,
    coalesce((select count(1) from plantaopro.operacao_assistida_ocorrencias o where o.cliente_id = c.id and o.reg_status = 'A' and o.status in ('ABERTA','EM_ANALISE')), 0)::bigint as OcorrenciasAbertas,
    coalesce((select count(1) from plantaopro.operacao_assistida_ocorrencias o where o.cliente_id = c.id and o.reg_status = 'A' and o.prioridade = 'CRITICA' and o.status in ('ABERTA','EM_ANALISE')), 0)::bigint as OcorrenciasCriticas
from plantaopro.clientes c
left join plantaopro.operacao_assistida_clientes oa on oa.cliente_id = c.id and oa.reg_status = 'A'
where {where}
order by coalesce(oa.reg_update, oa.reg_date, c.reg_date) desc
limit @limit offset @offset", new { clienteId = filtroCliente, status, limit = pageSize, offset })).ToList();

            foreach (var item in itens)
            {
                var checklist = await ObterChecklistClienteAsync(cn, item.ClienteId);
                item.Percentual = CalcularPercentual(checklist);
            }

            return Ok(ApiResponse<PagedResult<OperacaoAssistidaClienteDto>>.Ok(new PagedResult<OperacaoAssistidaClienteDto>(itens, page, pageSize, total), "Clientes em operação assistida carregados."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar clientes da operação assistida.");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar a operação assistida.", 500));
        }
    }

    [HttpGet("clientes/{clienteId:guid}")]
    public async Task<IActionResult> DetalharCliente(Guid clienteId)
    {
        try
        {
            var acesso = await ValidarClienteAsync(clienteId);
            if (!acesso.Success) return StatusCode(acesso.StatusCode, acesso);

            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var cliente = await cn.QuerySingleOrDefaultAsync<OperacaoAssistidaClienteDto>(@"select
    c.id as ClienteId,
    coalesce(c.nome_fantasia, c.razao_social, 'Cliente') as ClienteNome,
    coalesce(c.status, 'ATIVO') as ClienteStatus,
    coalesce(oa.status, 'NAO_INICIADA') as Status,
    coalesce(oa.responsavel, 'MNSOFT') as Responsavel,
    oa.inicio_previsto as InicioPrevisto,
    oa.go_live_previsto as GoLivePrevisto,
    coalesce(oa.percentual, 0)::int as Percentual,
    coalesce(oa.risco, 'BAIXO') as Risco,
    coalesce(oa.observacoes, '') as Observacoes,
    coalesce((select count(1) from plantaopro.operacao_assistida_ocorrencias o where o.cliente_id = c.id and o.reg_status = 'A' and o.status in ('ABERTA','EM_ANALISE')), 0)::bigint as OcorrenciasAbertas,
    coalesce((select count(1) from plantaopro.operacao_assistida_ocorrencias o where o.cliente_id = c.id and o.reg_status = 'A' and o.prioridade = 'CRITICA' and o.status in ('ABERTA','EM_ANALISE')), 0)::bigint as OcorrenciasCriticas
from plantaopro.clientes c
left join plantaopro.operacao_assistida_clientes oa on oa.cliente_id = c.id and oa.reg_status = 'A'
where c.id = @clienteId and c.reg_status = 'A'", new { clienteId });

            if (cliente is null) return NotFound(ApiResponse<string>.Fail("Cliente não encontrado.", 404));
            var checklist = await ObterChecklistClienteAsync(cn, clienteId);
            var ocorrencias = await ObterOcorrenciasClienteAsync(cn, clienteId, null, null);
            var treinamentos = await ObterTreinamentosClienteAsync(cn, clienteId);
            cliente.Percentual = CalcularPercentual(checklist);

            return Ok(ApiResponse<OperacaoAssistidaDetalheDto>.Ok(new OperacaoAssistidaDetalheDto(cliente, checklist, ocorrencias, treinamentos), "Operação assistida carregada."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao detalhar operação assistida do cliente {ClienteId}", clienteId);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar os detalhes da operação assistida.", 500));
        }
    }

    [HttpGet("clientes/{clienteId:guid}/checklist")]
    public async Task<IActionResult> Checklist(Guid clienteId)
    {
        try
        {
            var acesso = await ValidarClienteAsync(clienteId);
            if (!acesso.Success) return StatusCode(acesso.StatusCode, acesso);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var itens = await ObterChecklistClienteAsync(cn, clienteId);
            return Ok(ApiResponse<IEnumerable<OperacaoAssistidaChecklistDto>>.Ok(itens, "Checklist carregado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar checklist da operação assistida. ClienteId={ClienteId}", clienteId);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar o checklist.", 500));
        }
    }

    [HttpPost("checklist/{id:guid}/concluir")]
    public async Task<IActionResult> ConcluirChecklist(Guid id, [FromBody] ConcluirChecklistOperacaoRequest request)
    {
        try
        {
            var item = await LocalizarChecklistAsync(id);
            if (item is null) return NotFound(ApiResponse<string>.Fail("Item de checklist não encontrado.", 404));
            var acesso = await ValidarClienteAsync(item.ClienteId);
            if (!acesso.Success) return StatusCode(acesso.StatusCode, acesso);

            item.Concluido = true;
            item.ConcluidoEm = DateTime.UtcNow;
            item.ConcluidoPor = request.Responsavel ?? User.Identity?.Name ?? "operação assistida";
            item.Justificativa = request.Observacao ?? string.Empty;
            ChecklistOverrides[id] = item;
            await PersistirChecklistAsync(item);
            await AuditarAsync(item.ClienteId, id, "CONCLUIR_CHECKLIST_OPERACAO_ASSISTIDA", new { item.Titulo, request.Observacao });
            return Ok(ApiResponse<OperacaoAssistidaChecklistDto>.Ok(item, "Item concluído com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao concluir checklist da operação assistida. Id={Id}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível concluir o item.", 500));
        }
    }

    [HttpPost("checklist/{id:guid}/reabrir")]
    public async Task<IActionResult> ReabrirChecklist(Guid id, [FromBody] ReabrirChecklistOperacaoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Justificativa))
            return BadRequest(ApiResponse<string>.Fail("Informe a justificativa para reabrir o item.", 400));

        try
        {
            var item = await LocalizarChecklistAsync(id);
            if (item is null) return NotFound(ApiResponse<string>.Fail("Item de checklist não encontrado.", 404));
            var acesso = await ValidarClienteAsync(item.ClienteId);
            if (!acesso.Success) return StatusCode(acesso.StatusCode, acesso);

            item.Concluido = false;
            item.ConcluidoEm = null;
            item.ConcluidoPor = string.Empty;
            item.Justificativa = request.Justificativa;
            ChecklistOverrides[id] = item;
            await PersistirChecklistAsync(item);
            await AuditarAsync(item.ClienteId, id, "REABRIR_CHECKLIST_OPERACAO_ASSISTIDA", new { item.Titulo, request.Justificativa });
            return Ok(ApiResponse<OperacaoAssistidaChecklistDto>.Ok(item, "Item reaberto com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reabrir checklist da operação assistida. Id={Id}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível reabrir o item.", 500));
        }
    }

    [HttpGet("clientes/{clienteId:guid}/ocorrencias")]
    public async Task<IActionResult> Ocorrencias(Guid clienteId, [FromQuery] string? status = null, [FromQuery] string? prioridade = null)
    {
        try
        {
            var acesso = await ValidarClienteAsync(clienteId);
            if (!acesso.Success) return StatusCode(acesso.StatusCode, acesso);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var itens = await ObterOcorrenciasClienteAsync(cn, clienteId, status, prioridade);
            return Ok(ApiResponse<IEnumerable<OperacaoAssistidaOcorrenciaDto>>.Ok(itens, "Ocorrências carregadas."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar ocorrências da operação assistida. ClienteId={ClienteId}", clienteId);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar ocorrências.", 500));
        }
    }

    [HttpPost("clientes/{clienteId:guid}/ocorrencias")]
    public async Task<IActionResult> CriarOcorrencia(Guid clienteId, [FromBody] CriarOcorrenciaOperacaoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Descricao))
            return BadRequest(ApiResponse<string>.Fail("Informe a descrição da ocorrência.", 400));

        try
        {
            var acesso = await ValidarClienteAsync(clienteId);
            if (!acesso.Success) return StatusCode(acesso.StatusCode, acesso);

            var ocorrencia = new OperacaoAssistidaOcorrenciaDto
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                Tipo = Normalizar(request.Tipo, "DUVIDA"),
                Prioridade = Normalizar(request.Prioridade, "MEDIA"),
                Status = "ABERTA",
                Responsavel = request.Responsavel ?? string.Empty,
                Descricao = request.Descricao.Trim(),
                Solucao = string.Empty,
                DataAbertura = DateTime.UtcNow
            };

            OcorrenciasMemoria[ocorrencia.Id] = ocorrencia;
            await PersistirOcorrenciaAsync(ocorrencia);
            await AuditarAsync(clienteId, ocorrencia.Id, "CRIAR_OCORRENCIA_OPERACAO_ASSISTIDA", new { ocorrencia.Tipo, ocorrencia.Prioridade });

            if (ocorrencia.Prioridade == "CRITICA")
            {
                await RegistrarAlertaCriticoAsync(clienteId, ocorrencia);
            }

            return Ok(ApiResponse<OperacaoAssistidaOcorrenciaDto>.Ok(ocorrencia, "Ocorrência registrada com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar ocorrência da operação assistida. ClienteId={ClienteId}", clienteId);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar a ocorrência.", 500));
        }
    }

    [HttpPost("ocorrencias/{id:guid}/resolver")]
    public async Task<IActionResult> ResolverOcorrencia(Guid id, [FromBody] ResolverOcorrenciaOperacaoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Solucao))
            return BadRequest(ApiResponse<string>.Fail("Informe a solução aplicada.", 400));

        try
        {
            var ocorrencia = await LocalizarOcorrenciaAsync(id);
            if (ocorrencia is null) return NotFound(ApiResponse<string>.Fail("Ocorrência não encontrada.", 404));
            var acesso = await ValidarClienteAsync(ocorrencia.ClienteId);
            if (!acesso.Success) return StatusCode(acesso.StatusCode, acesso);

            ocorrencia.Status = "RESOLVIDA";
            ocorrencia.Solucao = request.Solucao.Trim();
            ocorrencia.DataResolucao = DateTime.UtcNow;
            ocorrencia.Responsavel = string.IsNullOrWhiteSpace(request.Responsavel) ? ocorrencia.Responsavel : request.Responsavel.Trim();
            OcorrenciasMemoria[id] = ocorrencia;
            await PersistirOcorrenciaAsync(ocorrencia);
            await AuditarAsync(ocorrencia.ClienteId, id, "RESOLVER_OCORRENCIA_OPERACAO_ASSISTIDA", new { ocorrencia.Tipo, ocorrencia.Prioridade });
            return Ok(ApiResponse<OperacaoAssistidaOcorrenciaDto>.Ok(ocorrencia, "Ocorrência resolvida com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao resolver ocorrência da operação assistida. Id={Id}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível resolver a ocorrência.", 500));
        }
    }

    [HttpPost("clientes/{clienteId:guid}/treinamentos")]
    public async Task<IActionResult> RegistrarTreinamento(Guid clienteId, [FromBody] RegistrarTreinamentoOperacaoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Tema))
            return BadRequest(ApiResponse<string>.Fail("Informe o tema do treinamento.", 400));

        try
        {
            var acesso = await ValidarClienteAsync(clienteId);
            if (!acesso.Success) return StatusCode(acesso.StatusCode, acesso);

            var treinamento = new OperacaoAssistidaTreinamentoDto
            {
                Id = Guid.NewGuid(),
                ClienteId = clienteId,
                Tema = request.Tema.Trim(),
                Perfil = request.Perfil ?? string.Empty,
                Responsavel = request.Responsavel ?? string.Empty,
                Participantes = request.Participantes ?? string.Empty,
                RealizadoEm = request.RealizadoEm ?? DateTime.UtcNow,
                Observacoes = request.Observacoes ?? string.Empty
            };

            TreinamentosMemoria[treinamento.Id] = treinamento;
            await PersistirTreinamentoAsync(treinamento);
            await AuditarAsync(clienteId, treinamento.Id, "REGISTRAR_TREINAMENTO_OPERACAO_ASSISTIDA", new { treinamento.Tema, treinamento.Perfil });
            return Ok(ApiResponse<OperacaoAssistidaTreinamentoDto>.Ok(treinamento, "Treinamento registrado com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar treinamento da operação assistida. ClienteId={ClienteId}", clienteId);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível registrar o treinamento.", 500));
        }
    }

    private async Task<ApiResponse<bool>> ValidarClienteAsync(Guid clienteId)
    {
        if (_usuarioContext.IsAdminGlobal()) return ApiResponse<bool>.Ok(true);
        return await _tenantGuard.ValidarAcessoClienteAsync(clienteId);
    }

    private async Task<List<OperacaoAssistidaChecklistDto>> ObterChecklistClienteAsync(NpgsqlConnection cn, Guid clienteId)
    {
        var salvos = (await cn.QueryAsync<OperacaoAssistidaChecklistDto>(@"select id as Id, cliente_id as ClienteId, ordem as Ordem, titulo as Titulo, descricao as Descricao, concluido as Concluido, concluido_em as ConcluidoEm, coalesce(concluido_por, '') as ConcluidoPor, coalesce(justificativa, '') as Justificativa
from plantaopro.operacao_assistida_checklist
where cliente_id = @clienteId and reg_status = 'A'
order by ordem", new { clienteId })).ToList();

        if (salvos.Count == 0)
        {
            salvos = ChecklistPadrao(clienteId).ToList();
        }

        foreach (var item in salvos)
        {
            if (ChecklistOverrides.TryGetValue(item.Id, out var atualizado))
            {
                item.Concluido = atualizado.Concluido;
                item.ConcluidoEm = atualizado.ConcluidoEm;
                item.ConcluidoPor = atualizado.ConcluidoPor;
                item.Justificativa = atualizado.Justificativa;
            }
        }

        return salvos;
    }

    private async Task<List<OperacaoAssistidaOcorrenciaDto>> ObterOcorrenciasClienteAsync(NpgsqlConnection cn, Guid clienteId, string? status, string? prioridade)
    {
        var itens = (await cn.QueryAsync<OperacaoAssistidaOcorrenciaDto>(@"select id as Id, cliente_id as ClienteId, tipo as Tipo, prioridade as Prioridade, status as Status, coalesce(responsavel, '') as Responsavel, coalesce(descricao, '') as Descricao, coalesce(solucao, '') as Solucao, data_abertura as DataAbertura, data_resolucao as DataResolucao
from plantaopro.operacao_assistida_ocorrencias
where cliente_id = @clienteId and reg_status = 'A' and (@status is null or status = @status) and (@prioridade is null or prioridade = @prioridade)
order by case prioridade when 'CRITICA' then 0 when 'ALTA' then 1 when 'MEDIA' then 2 else 3 end, data_abertura desc
limit 100", new { clienteId, status, prioridade })).ToList();

        itens.AddRange(OcorrenciasMemoria.Values.Where(x => x.ClienteId == clienteId && (status is null || x.Status == status) && (prioridade is null || x.Prioridade == prioridade) && itens.All(i => i.Id != x.Id)));
        return itens.OrderBy(x => x.Status == "RESOLVIDA").ThenByDescending(x => x.Prioridade == "CRITICA").ThenByDescending(x => x.DataAbertura).ToList();
    }

    private async Task<List<OperacaoAssistidaTreinamentoDto>> ObterTreinamentosClienteAsync(NpgsqlConnection cn, Guid clienteId)
    {
        var itens = (await cn.QueryAsync<OperacaoAssistidaTreinamentoDto>(@"select id as Id, cliente_id as ClienteId, tema as Tema, coalesce(perfil, '') as Perfil, coalesce(responsavel, '') as Responsavel, coalesce(participantes, '') as Participantes, realizado_em as RealizadoEm, coalesce(observacoes, '') as Observacoes
from plantaopro.operacao_assistida_treinamentos
where cliente_id = @clienteId and reg_status = 'A'
order by realizado_em desc
limit 50", new { clienteId })).ToList();
        itens.AddRange(TreinamentosMemoria.Values.Where(x => x.ClienteId == clienteId && itens.All(i => i.Id != x.Id)));
        return itens.OrderByDescending(x => x.RealizadoEm).ToList();
    }

    private async Task<OperacaoAssistidaChecklistDto?> LocalizarChecklistAsync(Guid id)
    {
        if (ChecklistOverrides.TryGetValue(id, out var cached)) return cached;
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var item = await cn.QuerySingleOrDefaultAsync<OperacaoAssistidaChecklistDto>(@"select id as Id, cliente_id as ClienteId, ordem as Ordem, titulo as Titulo, descricao as Descricao, concluido as Concluido, concluido_em as ConcluidoEm, coalesce(concluido_por, '') as ConcluidoPor, coalesce(justificativa, '') as Justificativa
from plantaopro.operacao_assistida_checklist where id = @id and reg_status = 'A'", new { id });
        return item;
    }

    private async Task<OperacaoAssistidaOcorrenciaDto?> LocalizarOcorrenciaAsync(Guid id)
    {
        if (OcorrenciasMemoria.TryGetValue(id, out var cached)) return cached;
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        return await cn.QuerySingleOrDefaultAsync<OperacaoAssistidaOcorrenciaDto>(@"select id as Id, cliente_id as ClienteId, tipo as Tipo, prioridade as Prioridade, status as Status, coalesce(responsavel, '') as Responsavel, coalesce(descricao, '') as Descricao, coalesce(solucao, '') as Solucao, data_abertura as DataAbertura, data_resolucao as DataResolucao
from plantaopro.operacao_assistida_ocorrencias where id = @id and reg_status = 'A'", new { id });
    }

    private async Task PersistirChecklistAsync(OperacaoAssistidaChecklistDto item)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.operacao_assistida_checklist(id, cliente_id, ordem, titulo, descricao, concluido, concluido_em, concluido_por, justificativa, reg_date, reg_status)
values (@Id, @ClienteId, @Ordem, @Titulo, @Descricao, @Concluido, @ConcluidoEm, @ConcluidoPor, @Justificativa, now(), 'A')
on conflict (id) do update set concluido = excluded.concluido, concluido_em = excluded.concluido_em, concluido_por = excluded.concluido_por, justificativa = excluded.justificativa, reg_update = now()", item);
            await AtualizarPercentualAsync(cn, item.ClienteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao persistir checklist da operação assistida. Id={Id}", item.Id);
        }
    }

    private async Task PersistirOcorrenciaAsync(OperacaoAssistidaOcorrenciaDto item)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.operacao_assistida_ocorrencias(id, cliente_id, tipo, prioridade, status, responsavel, descricao, solucao, data_abertura, data_resolucao, reg_date, reg_status)
values (@Id, @ClienteId, @Tipo, @Prioridade, @Status, @Responsavel, @Descricao, @Solucao, @DataAbertura, @DataResolucao, now(), 'A')
on conflict (id) do update set status = excluded.status, responsavel = excluded.responsavel, solucao = excluded.solucao, data_resolucao = excluded.data_resolucao, reg_update = now()", item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao persistir ocorrência da operação assistida. Id={Id}", item.Id);
        }
    }

    private async Task PersistirTreinamentoAsync(OperacaoAssistidaTreinamentoDto item)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.operacao_assistida_treinamentos(id, cliente_id, tema, perfil, responsavel, participantes, realizado_em, observacoes, reg_date, reg_status)
values (@Id, @ClienteId, @Tema, @Perfil, @Responsavel, @Participantes, @RealizadoEm, @Observacoes, now(), 'A')", item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao persistir treinamento da operação assistida. Id={Id}", item.Id);
        }
    }

    private async Task AtualizarPercentualAsync(NpgsqlConnection cn, Guid clienteId)
    {
        var existe = await cn.ExecuteScalarAsync<long>(@"select count(1) from plantaopro.operacao_assistida_clientes where cliente_id = @clienteId", new { clienteId });
        if (existe == 0)
        {
            await cn.ExecuteAsync(@"insert into plantaopro.operacao_assistida_clientes(id, cliente_id, status, percentual, responsavel, reg_date, reg_status)
values (gen_random_uuid(), @clienteId, 'EM_IMPLANTACAO', 0, 'MNSOFT', now(), 'A')", new { clienteId });
        }

        await cn.ExecuteAsync(@"update plantaopro.operacao_assistida_clientes oa set percentual = coalesce((select round(100.0 * sum(case when concluido then 1 else 0 end) / nullif(count(1),0))::int from plantaopro.operacao_assistida_checklist c where c.cliente_id = oa.cliente_id and c.reg_status = 'A'), 0), reg_update = now() where oa.cliente_id = @clienteId", new { clienteId });
    }

    private async Task RegistrarAlertaCriticoAsync(Guid clienteId, OperacaoAssistidaOcorrenciaDto ocorrencia)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.alertas_operacionais(id, cliente_id, tipo, titulo, descricao, severidade, status, origem, entidade_id, reg_date, reg_status)
values (gen_random_uuid(), @clienteId, 'OPERACAO_ASSISTIDA', 'Ocorrência crítica na homologação', @descricao, 'CRITICA', 'ABERTO', 'OPERACAO_ASSISTIDA', @id, now(), 'A')", new { clienteId, id = ocorrencia.Id, descricao = ocorrencia.Descricao });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar alerta crítico da operação assistida. OcorrenciaId={OcorrenciaId}", ocorrencia.Id);
        }
    }

    private Task AuditarAsync(Guid clienteId, Guid entidadeId, string acao, object detalhes)
    {
        var perfil = string.Join(',', _usuarioContext.GetRoles());
        return _audit.RegistrarAsync(_usuarioContext.GetUsuarioId(), clienteId, AuditoriaConstants.Entidades.Operacao, entidadeId, acao, detalhes, true, _usuarioContext.GetIpOrigem(), string.IsNullOrWhiteSpace(perfil) ? "sem-perfil" : perfil);
    }

    private static IEnumerable<OperacaoAssistidaChecklistDto> ChecklistPadrao(Guid clienteId)
    {
        var titulos = new[]
        {
            "Cliente cadastrado", "Plano selecionado", "Assinatura criada", "Usuário administrador criado", "Unidade criada", "Hospital cadastrado", "Especialidades cadastradas", "Médicos cadastrados", "Primeiro plantão criado", "Primeiro plantão publicado", "Primeiro médico convidado", "Primeira escala confirmada", "Primeiro pagamento confirmado", "Usuários treinados", "Área do médico validada", "Relatórios validados", "Faturamento SaaS validado", "Homologação aprovada"
        };

        for (var i = 0; i < titulos.Length; i++)
        {
            yield return new OperacaoAssistidaChecklistDto
            {
                Id = DeterministicGuid(clienteId, i + 1),
                ClienteId = clienteId,
                Ordem = i + 1,
                Titulo = titulos[i],
                Descricao = "Validar evidência operacional e registrar conclusão durante a homologação assistida."
            };
        }
    }

    private static int CalcularPercentual(IEnumerable<OperacaoAssistidaChecklistDto> checklist)
    {
        var lista = checklist.ToList();
        return lista.Count == 0 ? 0 : (int)Math.Round(lista.Count(x => x.Concluido) * 100d / lista.Count);
    }

    private static string Normalizar(string? valor, string padrao)
    {
        return string.IsNullOrWhiteSpace(valor) ? padrao : valor.Trim().ToUpperInvariant().Replace('Ú', 'U').Replace('Í', 'I').Replace('É', 'E').Replace('Á', 'A').Replace('Ã', 'A').Replace('Ç', 'C');
    }

    private static Guid DeterministicGuid(Guid clienteId, int ordem)
    {
        var bytes = clienteId.ToByteArray();
        var ordemBytes = BitConverter.GetBytes(ordem);
        for (var i = 0; i < ordemBytes.Length; i++) bytes[i] = (byte)(bytes[i] ^ ordemBytes[i]);
        return new Guid(bytes);
    }
}
