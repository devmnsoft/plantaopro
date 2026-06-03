using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
[Route("api/planos")]
[Tags("SaaS - Planos")]
public sealed class PlanosController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly IAuditService _audit;
    private readonly ILogger<PlanosController> _logger;

    public PlanosController(IConfiguration cfg, IAuditService audit, ILogger<PlanosController> logger)
    {
        _cfg = cfg;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
    {
        try
        {
            var p = Math.Max(1, page);
            var s = Math.Clamp(pageSize, 1, 100);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var where = string.IsNullOrWhiteSpace(status) ? "reg_status='A'" : "reg_status='A' and upper(status)=upper(@status)";
            var total = await cn.ExecuteScalarAsync<long>($"select count(1) from plantaopro.planos where {where}", new { status });
            var items = await cn.QueryAsync<PlanoComercialDto>($@"select id as ""Id"",
       coalesce(nome,'') as ""Nome"",
       coalesce(descricao,'') as ""Descricao"",
       valor_mensal as ""ValorMensal"",
       limite_medicos as ""LimiteMedicos"",
       limite_hospitais as ""LimiteHospitais"",
       limite_plantoes_mes as ""LimitePlantoesMes"",
       coalesce(permite_mobile, permite_api, false) as ""PermiteMobile"",
       coalesce(permite_bi, permite_relatorios, false) as ""PermiteBi"",
       coalesce(permite_relatorios_avancados, permite_relatorios, false) as ""PermiteRelatoriosAvancados"",
       coalesce(permite_integracoes, permite_api, false) as ""PermiteIntegracoes"",
       coalesce(status,'') as ""Status""
from plantaopro.planos
where {where}
order by nome
limit @s offset @offset", new { status, s, offset = (p - 1) * s });
            return Ok(ApiResponse<PagedResult<PlanoComercialDto>>.Ok(new PagedResult<PlanoComercialDto>(items, p, s, total)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar planos SaaS");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar os planos.", 500));
        }
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalhar(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var item = await cn.QueryFirstOrDefaultAsync<PlanoComercialDto>(@"select id as ""Id"", coalesce(nome,'') as ""Nome"", coalesce(descricao,'') as ""Descricao"", valor_mensal as ""ValorMensal"",
       limite_medicos as ""LimiteMedicos"", limite_hospitais as ""LimiteHospitais"", limite_plantoes_mes as ""LimitePlantoesMes"",
       coalesce(permite_mobile, permite_api, false) as ""PermiteMobile"", coalesce(permite_bi, permite_relatorios, false) as ""PermiteBi"",
       coalesce(permite_relatorios_avancados, permite_relatorios, false) as ""PermiteRelatoriosAvancados"", coalesce(permite_integracoes, permite_api, false) as ""PermiteIntegracoes"",
       coalesce(status,'') as ""Status""
from plantaopro.planos where id=@id and reg_status='A'", new { id });
            return item is null ? NotFound(ApiResponse<string>.Fail("Plano não encontrado.", 404)) : Ok(ApiResponse<PlanoComercialDto>.Ok(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao detalhar plano SaaS {PlanoId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar o plano.", 500));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] PlanoComercialRequest request)
    {
        try
        {
            var validacao = ValidarPlano(request);
            if (!validacao.Success) return BadRequest(validacao);

            var id = Guid.NewGuid();
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.planos(id,nome,descricao,valor_mensal,limite_medicos,limite_hospitais,limite_plantoes_mes,permite_relatorios,permite_api,permite_mobile,permite_bi,permite_relatorios_avancados,permite_integracoes,status,reg_status,reg_date)
values(@id,@Nome,@Descricao,@ValorMensal,@LimiteMedicos,@LimiteHospitais,@LimitePlantoesMes,@PermiteRelatoriosAvancados,@PermiteIntegracoes,@PermiteMobile,@PermiteBi,@PermiteRelatoriosAvancados,@PermiteIntegracoes,'ATIVO','A',now())", new { id, request.Nome, request.Descricao, request.ValorMensal, request.LimiteMedicos, request.LimiteHospitais, request.LimitePlantoesMes, request.PermiteMobile, request.PermiteBi, request.PermiteRelatoriosAvancados, request.PermiteIntegracoes });
            await AuditarAsync(AuditoriaConstants.Entidades.Plano, id, AuditoriaConstants.Acoes.Criar, new { request.Nome, request.ValorMensal });
            return Ok(ApiResponse<Guid>.Ok(id, "Plano criado com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar plano SaaS");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar o plano.", 500));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] PlanoComercialRequest request)
    {
        try
        {
            var validacao = ValidarPlano(request);
            if (!validacao.Success) return BadRequest(validacao);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var rows = await cn.ExecuteAsync(@"update plantaopro.planos
set nome=@Nome, descricao=@Descricao, valor_mensal=@ValorMensal, limite_medicos=@LimiteMedicos,
    limite_hospitais=@LimiteHospitais, limite_plantoes_mes=@LimitePlantoesMes,
    permite_relatorios=@PermiteRelatoriosAvancados, permite_api=@PermiteIntegracoes,
    permite_mobile=@PermiteMobile, permite_bi=@PermiteBi,
    permite_relatorios_avancados=@PermiteRelatoriosAvancados, permite_integracoes=@PermiteIntegracoes,
    reg_update=now()
where id=@id and reg_status='A'", new { id, request.Nome, request.Descricao, request.ValorMensal, request.LimiteMedicos, request.LimiteHospitais, request.LimitePlantoesMes, request.PermiteMobile, request.PermiteBi, request.PermiteRelatoriosAvancados, request.PermiteIntegracoes });
            if (rows == 0) return NotFound(ApiResponse<string>.Fail("Plano não encontrado.", 404));
            await AuditarAsync(AuditoriaConstants.Entidades.Plano, id, AuditoriaConstants.Acoes.Editar, new { request.Nome });
            return Ok(ApiResponse<string>.Ok("ok", "Plano atualizado com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao editar plano SaaS {PlanoId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível atualizar o plano.", 500));
        }
    }

    [HttpPost("{id:guid}/inativar")]
    public Task<IActionResult> Inativar(Guid id) => AlterarStatus(id, "INATIVO", "Plano inativado com sucesso.");

    [HttpPost("{id:guid}/reativar")]
    public Task<IActionResult> Reativar(Guid id) => AlterarStatus(id, "ATIVO", "Plano reativado com sucesso.");

    private async Task<IActionResult> AlterarStatus(Guid id, string status, string mensagem)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var rows = await cn.ExecuteAsync("update plantaopro.planos set status=@status, reg_update=now() where id=@id and reg_status='A'", new { id, status });
            if (rows == 0) return NotFound(ApiResponse<string>.Fail("Plano não encontrado.", 404));
            await AuditarAsync(AuditoriaConstants.Entidades.Plano, id, AuditoriaConstants.Acoes.AlterarStatus, new { status });
            return Ok(ApiResponse<string>.Ok("ok", mensagem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar status do plano {PlanoId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível alterar o status do plano.", 500));
        }
    }

    private static ApiResponse<string> ValidarPlano(PlanoComercialRequest request)
    {
        var erros = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Nome)) erros.Add("Nome do plano é obrigatório.");
        if (request.ValorMensal < 0) erros.Add("Valor mensal não pode ser negativo.");
        if (request.LimiteMedicos < 0 || request.LimiteHospitais < 0 || request.LimitePlantoesMes < 0) erros.Add("Limites não podem ser negativos.");
        return erros.Count == 0 ? ApiResponse<string>.Ok("ok") : ApiResponse<string>.Fail("Verifique os dados do plano.", 400, erros);
    }

    private Task AuditarAsync(string entidade, Guid entidadeId, string acao, object detalhes)
        => _audit.RegistrarAsync(null, null, entidade, entidadeId, acao, detalhes, true, HttpContext.Connection.RemoteIpAddress?.ToString(), "ADMINISTRADOR_GLOBAL");
}

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
[Route("api/assinaturas")]
[Tags("SaaS - Assinaturas")]
public sealed class AssinaturasController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly AssinaturaGuardService _guard;
    private readonly IAuditService _audit;
    private readonly ILogger<AssinaturasController> _logger;

    public AssinaturasController(IConfiguration cfg, AssinaturaGuardService guard, IAuditService audit, ILogger<AssinaturasController> logger)
    {
        _cfg = cfg;
        _guard = guard;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] Guid? clienteId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var p = Math.Max(1, page);
            var s = Math.Clamp(pageSize, 1, 100);
            var where = "a.reg_status='A' and (@clienteId is null or a.cliente_id=@clienteId) and (@status is null or upper(a.status)=upper(@status))";
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var total = await cn.ExecuteScalarAsync<long>($"select count(1) from plantaopro.assinaturas a where {where}", new { clienteId, status });
            var items = await cn.QueryAsync<AssinaturaComercialDto>($@"select a.id as ""Id"", a.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       a.plano_id as ""PlanoId"", coalesce(p.nome,'') as ""PlanoNome"", a.data_inicio as ""DataInicio"", a.data_fim as ""DataFim"",
       coalesce(a.status,'') as ""Status"", a.valor_contratado as ""ValorContratado"", a.dia_vencimento as ""DiaVencimento"",
       coalesce(a.observacoes,'') as ""Observacoes""
from plantaopro.assinaturas a
join plantaopro.clientes c on c.id=a.cliente_id
join plantaopro.planos p on p.id=a.plano_id
where {where}
order by a.reg_date desc
limit @s offset @offset", new { clienteId, status, s, offset = (p - 1) * s });
            return Ok(ApiResponse<PagedResult<AssinaturaComercialDto>>.Ok(new PagedResult<AssinaturaComercialDto>(items, p, s, total)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar assinaturas SaaS");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar assinaturas.", 500));
        }
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalhar(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var item = await cn.QueryFirstOrDefaultAsync<AssinaturaComercialDto>(@"select a.id as ""Id"", a.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       a.plano_id as ""PlanoId"", coalesce(p.nome,'') as ""PlanoNome"", a.data_inicio as ""DataInicio"", a.data_fim as ""DataFim"",
       coalesce(a.status,'') as ""Status"", a.valor_contratado as ""ValorContratado"", a.dia_vencimento as ""DiaVencimento"", coalesce(a.observacoes,'') as ""Observacoes""
from plantaopro.assinaturas a
join plantaopro.clientes c on c.id=a.cliente_id
join plantaopro.planos p on p.id=a.plano_id
where a.id=@id and a.reg_status='A'", new { id });
            return item is null ? NotFound(ApiResponse<string>.Fail("Assinatura não encontrada.", 404)) : Ok(ApiResponse<AssinaturaComercialDto>.Ok(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao detalhar assinatura SaaS {AssinaturaId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar assinatura.", 500));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] AssinaturaComercialRequest request)
    {
        try
        {
            if (request.DataFim <= request.DataInicio) return BadRequest(ApiResponse<string>.Fail("Data final deve ser maior que a data inicial.", 400));
            if (request.ValorContratado < 0) return BadRequest(ApiResponse<string>.Fail("Valor contratado não pode ser negativo.", 400));
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var rows = await cn.ExecuteAsync(@"update plantaopro.assinaturas
set plano_id=@PlanoId, data_inicio=@DataInicio, data_fim=@DataFim, valor_contratado=@ValorContratado, dia_vencimento=@DiaVencimento, observacoes=@Observacoes, reg_update=now()
where id=@id and cliente_id=@ClienteId and reg_status='A'", new { id, request.ClienteId, request.PlanoId, request.DataInicio, request.DataFim, request.ValorContratado, request.DiaVencimento, request.Observacoes });
            if (rows == 0) return NotFound(ApiResponse<string>.Fail("Assinatura não encontrada.", 404));
            await AuditarAsync(request.ClienteId, id, AuditoriaConstants.Acoes.Editar, new { request.PlanoId, request.ValorContratado });
            return Ok(ApiResponse<string>.Ok("ok", "Assinatura atualizada com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao editar assinatura SaaS {AssinaturaId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível atualizar assinatura.", 500));
        }
    }

    [HttpGet("{id:guid}/uso")]
    public async Task<IActionResult> Uso(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteId = await cn.ExecuteScalarAsync<Guid?>("select cliente_id from plantaopro.assinaturas where id=@id and reg_status='A'", new { id });
            if (!clienteId.HasValue) return NotFound(ApiResponse<string>.Fail("Assinatura não encontrada.", 404));
            var uso = await _guard.ObterUsoPlano(clienteId.Value);
            return StatusCode(uso.StatusCode, uso);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar uso da assinatura {AssinaturaId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar uso da assinatura.", 500));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] AssinaturaComercialRequest request)
    {
        try
        {
            if (request.DataFim <= request.DataInicio) return BadRequest(ApiResponse<string>.Fail("Data final deve ser maior que a data inicial.", 400));
            if (request.ValorContratado < 0) return BadRequest(ApiResponse<string>.Fail("Valor contratado não pode ser negativo.", 400));
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var planoAtivo = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from plantaopro.planos where id=@PlanoId and reg_status='A' and upper(status)='ATIVO')", request);
            if (!planoAtivo) return BadRequest(ApiResponse<string>.Fail("Plano inativo não pode gerar nova assinatura.", 400));
            var jaAtiva = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from plantaopro.assinaturas where cliente_id=@ClienteId and reg_status='A' and upper(status)='ATIVA')", request);
            if (jaAtiva) return BadRequest(ApiResponse<string>.Fail("Cliente já possui uma assinatura ativa.", 400));

            var id = Guid.NewGuid();
            await cn.ExecuteAsync(@"insert into plantaopro.assinaturas(id,cliente_id,plano_id,data_inicio,data_fim,status,valor_contratado,dia_vencimento,observacoes,reg_status,reg_date)
values(@id,@ClienteId,@PlanoId,@DataInicio,@DataFim,'ATIVA',@ValorContratado,@DiaVencimento,@Observacoes,'A',now())", new { id, request.ClienteId, request.PlanoId, request.DataInicio, request.DataFim, request.ValorContratado, request.DiaVencimento, request.Observacoes });
            await AuditarAsync(request.ClienteId, id, AuditoriaConstants.Acoes.Criar, new { request.PlanoId, request.ValorContratado });
            return Ok(ApiResponse<Guid>.Ok(id, "Assinatura criada com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar assinatura SaaS");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar assinatura.", 500));
        }
    }

    [HttpPost("{id:guid}/suspender")]
    public Task<IActionResult> Suspender(Guid id, [FromBody] JustificativaRequest request) => AlterarStatus(id, "SUSPENSA", request.Justificativa, "Assinatura suspensa com sucesso.");

    [HttpPost("{id:guid}/reativar")]
    public Task<IActionResult> Reativar(Guid id) => AlterarStatus(id, "ATIVA", "reativação comercial", "Assinatura reativada com sucesso.");

    [HttpPost("{id:guid}/cancelar")]
    public Task<IActionResult> Cancelar(Guid id, [FromBody] JustificativaRequest request) => AlterarStatus(id, "CANCELADA", request.Justificativa, "Assinatura cancelada com sucesso.");

    private async Task<IActionResult> AlterarStatus(Guid id, string status, string justificativa, string mensagem)
    {
        try
        {
            if ((status == "SUSPENSA" || status == "CANCELADA") && string.IsNullOrWhiteSpace(justificativa)) return BadRequest(ApiResponse<string>.Fail("Justificativa obrigatória.", 400));
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteId = await cn.ExecuteScalarAsync<Guid?>(@"update plantaopro.assinaturas set status=@status, motivo_cancelamento=case when @status='CANCELADA' then @justificativa else motivo_cancelamento end, data_cancelamento=case when @status='CANCELADA' then now() else data_cancelamento end, reg_update=now()
where id=@id and reg_status='A'
returning cliente_id", new { id, status, justificativa });
            if (!clienteId.HasValue) return NotFound(ApiResponse<string>.Fail("Assinatura não encontrada.", 404));
            await AuditarAsync(clienteId.Value, id, AuditoriaConstants.Acoes.AlterarStatus, new { status, justificativa });
            return Ok(ApiResponse<string>.Ok("ok", mensagem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar assinatura {AssinaturaId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível alterar a assinatura.", 500));
        }
    }

    private Task AuditarAsync(Guid clienteId, Guid entidadeId, string acao, object detalhes)
        => _audit.RegistrarAsync(null, clienteId, AuditoriaConstants.Entidades.Assinatura, entidadeId, acao, detalhes, true, HttpContext.Connection.RemoteIpAddress?.ToString(), "ADMINISTRADOR_GLOBAL");
}

[ApiController]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
[Route("api/faturamento-saas")]
[Tags("SaaS - Faturamento")]
public sealed class FaturamentoSaasController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly IAuditService _audit;
    private readonly ILogger<FaturamentoSaasController> _logger;

    public FaturamentoSaasController(IConfiguration cfg, IAuditService audit, ILogger<FaturamentoSaasController> logger)
    {
        _cfg = cfg;
        _audit = audit;
        _logger = logger;
    }

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo()
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await AtualizarVencidasAsync(cn);
            var dto = await cn.QuerySingleAsync<FaturamentoSaasResumoDto>(@"select
coalesce(sum(case when status in ('ABERTA','VENCIDA','EM_CONTESTACAO') then valor else 0 end),0) as ""ReceitaPrevista"",
coalesce(sum(case when status='PAGA' then coalesce(valor_pago, valor) else 0 end),0) as ""ReceitaRecebida"",
count(*) filter (where status='ABERTA')::bigint as ""FaturasAbertas"",
count(*) filter (where status='VENCIDA')::bigint as ""FaturasVencidas"",
count(*) filter (where status='EM_CONTESTACAO')::bigint as ""FaturasEmContestacao""
from plantaopro.faturas_saas where reg_status='A'");
            return Ok(ApiResponse<FaturamentoSaasResumoDto>.Ok(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar resumo de faturamento SaaS");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar resumo de faturamento.", 500));
        }
    }

    [HttpGet("faturas")]
    public async Task<IActionResult> Faturas([FromQuery] Guid? clienteId, [FromQuery] string? status, [FromQuery] DateOnly? competencia, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var p = Math.Max(1, page);
            var s = Math.Clamp(pageSize, 1, 100);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await AtualizarVencidasAsync(cn);
            var where = "f.reg_status='A' and (@clienteId is null or f.cliente_id=@clienteId) and (@status is null or f.status=@status) and (@competencia is null or f.competencia=@competencia)";
            var args = new { clienteId, status, competencia, s, offset = (p - 1) * s };
            var total = await cn.ExecuteScalarAsync<long>($"select count(1) from plantaopro.faturas_saas f where {where}", args);
            var items = await cn.QueryAsync<FaturaSaasDto>($@"select f.id as ""Id"", f.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"", f.assinatura_id as ""AssinaturaId"",
       f.competencia as ""Competencia"", f.valor as ""Valor"", f.vencimento as ""Vencimento"", f.status as ""Status"",
       f.valor_pago as ""ValorPago"", f.data_pagamento as ""DataPagamento"", coalesce(f.forma_pagamento,'') as ""FormaPagamento"",
       coalesce(f.motivo_cancelamento,'') as ""MotivoCancelamento"", coalesce(f.motivo_contestacao,'') as ""MotivoContestacao"", f.criado_em as ""CriadoEm""
from plantaopro.faturas_saas f
join plantaopro.clientes c on c.id=f.cliente_id
where {where}
order by f.vencimento desc, f.criado_em desc
limit @s offset @offset", args);
            return Ok(ApiResponse<PagedResult<FaturaSaasDto>>.Ok(new PagedResult<FaturaSaasDto>(items, p, s, total)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar faturas SaaS");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar faturas.", 500));
        }
    }

    [HttpGet("faturas/{id:guid}")]
    public async Task<IActionResult> Detalhar(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var item = await cn.QueryFirstOrDefaultAsync<FaturaSaasDto>(@"select f.id as ""Id"", f.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"", f.assinatura_id as ""AssinaturaId"",
       f.competencia as ""Competencia"", f.valor as ""Valor"", f.vencimento as ""Vencimento"", f.status as ""Status"",
       f.valor_pago as ""ValorPago"", f.data_pagamento as ""DataPagamento"", coalesce(f.forma_pagamento,'') as ""FormaPagamento"",
       coalesce(f.motivo_cancelamento,'') as ""MotivoCancelamento"", coalesce(f.motivo_contestacao,'') as ""MotivoContestacao"", f.criado_em as ""CriadoEm""
from plantaopro.faturas_saas f join plantaopro.clientes c on c.id=f.cliente_id where f.id=@id and f.reg_status='A'", new { id });
            return item is null ? NotFound(ApiResponse<string>.Fail("Fatura não encontrada.", 404)) : Ok(ApiResponse<FaturaSaasDto>.Ok(item));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao detalhar fatura SaaS {FaturaId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar fatura.", 500));
        }
    }

    [HttpPost("gerar-mensal")]
    public async Task<IActionResult> GerarMensal([FromBody] GerarFaturasSaasRequest request)
    {
        try
        {
            var competencia = new DateOnly(request.Competencia.Year, request.Competencia.Month, 1);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var ids = (await cn.QueryAsync<Guid>(@"insert into plantaopro.faturas_saas(id, cliente_id, assinatura_id, competencia, valor, vencimento, status, criado_em, atualizado_em, reg_status)
select gen_random_uuid(), a.cliente_id, a.id, @competencia, a.valor_contratado, make_date(extract(year from @competencia::date)::int, extract(month from @competencia::date)::int, least(greatest(a.dia_vencimento,1),28)), 'ABERTA', now(), now(), 'A'
from plantaopro.assinaturas a
join plantaopro.clientes c on c.id=a.cliente_id
where a.reg_status='A' and upper(a.status)='ATIVA' and c.reg_status='A' and upper(c.status) not in ('SUSPENSO','CANCELADO')
  and not exists (select 1 from plantaopro.faturas_saas f where f.assinatura_id=a.id and f.competencia=@competencia and f.reg_status='A')
returning id", new { competencia })).ToArray();
            await _audit.RegistrarAsync(null, null, AuditoriaConstants.Entidades.FaturaSaas, null, AuditoriaConstants.Acoes.Criar, new { competencia, total = ids.Length }, true, HttpContext.Connection.RemoteIpAddress?.ToString(), "ADMINISTRADOR_GLOBAL");
            return Ok(ApiResponse<IEnumerable<Guid>>.Ok(ids, ids.Length == 0 ? "Nenhuma nova fatura gerada para a competência." : "Faturas geradas com sucesso."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar faturamento SaaS mensal");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível gerar faturas mensais.", 500));
        }
    }

    [HttpPost("faturas/{id:guid}/marcar-paga")]
    public async Task<IActionResult> MarcarPaga(Guid id, [FromBody] MarcarFaturaPagaRequest request)
    {
        if (request.ValorPago <= 0) return BadRequest(ApiResponse<string>.Fail("Valor pago deve ser maior que zero.", 400));
        if (string.IsNullOrWhiteSpace(request.FormaPagamento)) return BadRequest(ApiResponse<string>.Fail("Forma de pagamento é obrigatória.", 400));
        return await AtualizarFatura(id, "PAGA", new { valorPago = (decimal?)request.ValorPago, dataPagamento = (DateOnly?)request.DataPagamento, formaPagamento = request.FormaPagamento, motivoCancelamento = (string?)null, motivoContestacao = (string?)null }, "Fatura marcada como paga.");
    }

    [HttpPost("faturas/{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] JustificativaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Justificativa)) return BadRequest(ApiResponse<string>.Fail("Justificativa obrigatória para cancelar fatura.", 400));
        return await AtualizarFatura(id, "CANCELADA", new { valorPago = (decimal?)null, dataPagamento = (DateOnly?)null, formaPagamento = (string?)null, motivoCancelamento = request.Justificativa, motivoContestacao = (string?)null }, "Fatura cancelada com sucesso.");
    }

    [HttpPost("faturas/{id:guid}/contestar")]
    public async Task<IActionResult> Contestar(Guid id, [FromBody] MotivoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo)) return BadRequest(ApiResponse<string>.Fail("Motivo obrigatório para contestação.", 400));
        return await AtualizarFatura(id, "EM_CONTESTACAO", new { valorPago = (decimal?)null, dataPagamento = (DateOnly?)null, formaPagamento = (string?)null, motivoCancelamento = (string?)null, motivoContestacao = request.Motivo }, "Fatura enviada para contestação.");
    }

    [HttpPost("faturas/{id:guid}/notificar")]
    public async Task<IActionResult> Notificar(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var clienteId = await cn.ExecuteScalarAsync<Guid?>("select cliente_id from plantaopro.faturas_saas where id=@id and reg_status='A'", new { id });
            if (!clienteId.HasValue) return NotFound(ApiResponse<string>.Fail("Fatura não encontrada.", 404));
            await _audit.RegistrarAsync(null, clienteId, AuditoriaConstants.Entidades.FaturaSaas, id, AuditoriaConstants.Acoes.Notificar, new { canal = "in_app", origem = "faturamento_saas" }, true, HttpContext.Connection.RemoteIpAddress?.ToString(), "ADMINISTRADOR_GLOBAL");
            return Ok(ApiResponse<string>.Ok("ok", "Notificação de cobrança registrada."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar fatura SaaS {FaturaId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível notificar cobrança.", 500));
        }
    }

    [HttpGet("inadimplencia")]
    public async Task<IActionResult> Inadimplencia([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var p = Math.Max(1, page);
            var s = Math.Clamp(pageSize, 1, 100);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await AtualizarVencidasAsync(cn);
            var total = await cn.ExecuteScalarAsync<long>("select count(distinct cliente_id) from plantaopro.faturas_saas where status='VENCIDA' and reg_status='A'");
            var items = await cn.QueryAsync<InadimplenciaSaasDto>(@"select f.cliente_id as ""ClienteId"", coalesce(c.nome_fantasia,c.razao_social,'') as ""ClienteNome"",
       count(*)::bigint as ""FaturasVencidas"", coalesce(sum(f.valor),0) as ""ValorVencido"", min(f.vencimento) as ""VencimentoMaisAntigo""
from plantaopro.faturas_saas f
join plantaopro.clientes c on c.id=f.cliente_id
where f.status='VENCIDA' and f.reg_status='A'
group by f.cliente_id, c.nome_fantasia, c.razao_social
order by min(f.vencimento)
limit @s offset @offset", new { s, offset = (p - 1) * s });
            return Ok(ApiResponse<PagedResult<InadimplenciaSaasDto>>.Ok(new PagedResult<InadimplenciaSaasDto>(items, p, s, total)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar inadimplência SaaS");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar inadimplência.", 500));
        }
    }

    private async Task<IActionResult> AtualizarFatura(Guid id, string status, object dados, string mensagem)
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var fatura = await cn.QueryFirstOrDefaultAsync<(Guid ClienteId, string Status)>("select cliente_id as ClienteId, status as Status from plantaopro.faturas_saas where id=@id and reg_status='A'", new { id });
            if (fatura == default) return NotFound(ApiResponse<string>.Fail("Fatura não encontrada.", 404));
            if (fatura.Status == "CANCELADA") return BadRequest(ApiResponse<string>.Fail("Fatura cancelada não pode ser alterada.", 400));

            var p = new DynamicParameters(dados);
            p.Add("id", id);
            p.Add("status", status);
            await cn.ExecuteAsync(@"update plantaopro.faturas_saas
set status=@status, valor_pago=coalesce(@valorPago, valor_pago), data_pagamento=coalesce(@dataPagamento, data_pagamento), forma_pagamento=coalesce(@formaPagamento, forma_pagamento),
    motivo_cancelamento=coalesce(@motivoCancelamento, motivo_cancelamento), motivo_contestacao=coalesce(@motivoContestacao, motivo_contestacao), atualizado_em=now()
where id=@id and reg_status='A'", p);
            await _audit.RegistrarAsync(null, fatura.ClienteId, AuditoriaConstants.Entidades.FaturaSaas, id, AuditoriaConstants.Acoes.AlterarStatus, new { status }, true, HttpContext.Connection.RemoteIpAddress?.ToString(), "ADMINISTRADOR_GLOBAL");
            return Ok(ApiResponse<string>.Ok("ok", mensagem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar fatura SaaS {FaturaId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível atualizar fatura.", 500));
        }
    }

    private static Task AtualizarVencidasAsync(NpgsqlConnection cn)
        => cn.ExecuteAsync("update plantaopro.faturas_saas set status='VENCIDA', atualizado_em=now() where status='ABERTA' and vencimento < current_date and reg_status='A'");
}
