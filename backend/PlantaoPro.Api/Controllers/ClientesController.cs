using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador)]
public class ClientesController : ControllerBase
{
    private readonly ClienteService service;
    private readonly IConfiguration cfg;
    private readonly ILogger<ClientesController> logger;

    public ClientesController(ClienteService service, IConfiguration cfg, ILogger<ClientesController> logger)
    {
        this.service = service;
        this.cfg = cfg;
        this.logger = logger;
    }

    [HttpGet] public async Task<IActionResult> Get() { var r = await service.ListAsync(); return StatusCode(r.StatusCode, r); }
    [HttpGet("{id:guid}")] public async Task<IActionResult> GetById(Guid id){ var r=await service.GetAsync(id); return StatusCode(r.StatusCode,r);}    

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateClienteRequest req)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var id = Guid.NewGuid();
            await cn.ExecuteAsync(@"insert into plantaopro.clientes(id,razao_social,nome_fantasia,cnpj,email,telefone,cidade,estado,plano_id,status,reg_status,reg_date) values(@id,@RazaoSocial,@NomeFantasia,@Cnpj,@Email,@Telefone,@Cidade,@Estado,@PlanoId,@Status,'A',now())", new { id, req.RazaoSocial, req.NomeFantasia, req.Cnpj, req.Email, req.Telefone, req.Cidade, req.Estado, req.PlanoId, req.Status });
            return Ok(ApiResponse<Guid>.Ok(id, "Cliente criado com sucesso."));
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao criar cliente"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível criar cliente.", 500)); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Put(Guid id, [FromBody] UpdateClienteRequest req)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.ExecuteAsync(@"update plantaopro.clientes set razao_social=@RazaoSocial,nome_fantasia=@NomeFantasia,cnpj=@Cnpj,email=@Email,telefone=@Telefone,cidade=@Cidade,estado=@Estado,plano_id=@PlanoId,status=@Status,reg_status=@RegStatus,reg_update=now() where id=@id", new { id, req.RazaoSocial, req.NomeFantasia, req.Cnpj, req.Email, req.Telefone, req.Cidade, req.Estado, req.PlanoId, req.Status, req.RegStatus });
            if (rows == 0) return NotFound(ApiResponse<string>.Fail("Cliente não encontrado.", 404));
            return Ok(ApiResponse<string>.Ok("ok", "Cliente atualizado."));
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao atualizar cliente"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível atualizar cliente.", 500)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.ExecuteAsync("update plantaopro.clientes set reg_status='I',status='SUSPENSO',reg_update=now() where id=@id", new { id });
            if (rows == 0) return NotFound(ApiResponse<string>.Fail("Cliente não encontrado.", 404));
            return Ok(ApiResponse<string>.Ok("ok", "Cliente suspenso com sucesso."));
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao suspender cliente"); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível suspender cliente.", 500)); }
    }

    [HttpPost("{id:guid}/suspender")]
    public async Task<IActionResult> Suspender(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.ExecuteAsync("update plantaopro.clientes set status='SUSPENSO',reg_update=now() where id=@id and reg_status='A'", new { id });
            if (rows == 0) return NotFound(ApiResponse<string>.Fail("Cliente não encontrado.", 404));
            return Ok(ApiResponse<string>.Ok("ok", "Cliente suspenso com sucesso."));
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao suspender cliente {ClienteId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível suspender cliente.", 500)); }
    }

    [HttpPost("{id:guid}/reativar")]
    public async Task<IActionResult> Reativar(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.ExecuteAsync("update plantaopro.clientes set status='ATIVO',reg_status='A',reg_update=now() where id=@id", new { id });
            if (rows == 0) return NotFound(ApiResponse<string>.Fail("Cliente não encontrado.", 404));
            return Ok(ApiResponse<string>.Ok("ok", "Cliente reativado com sucesso."));
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao reativar cliente {ClienteId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível reativar cliente.", 500)); }
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.ExecuteAsync("update plantaopro.clientes set status='CANCELADO',reg_update=now() where id=@id and reg_status='A'", new { id });
            if (rows == 0) return NotFound(ApiResponse<string>.Fail("Cliente não encontrado.", 404));
            return Ok(ApiResponse<string>.Ok("ok", "Cliente cancelado com sucesso."));
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao cancelar cliente {ClienteId}", id); return StatusCode(500, ApiResponse<string>.Fail("Não foi possível cancelar cliente.", 500)); }
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup()
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.QueryAsync<object>(@"select id as ""Id"", coalesce(nome_fantasia, razao_social, '') as ""Nome"", coalesce(status,'') as ""Status""
from plantaopro.clientes
where reg_status='A'
order by coalesce(nome_fantasia, razao_social, '')
limit 200");
            return Ok(ApiResponse<IEnumerable<object>>.Ok(rows));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao carregar lookup de clientes");
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível carregar clientes.", 500));
        }
    }

    [HttpGet("{id:guid}/resumo-saas")]
    public async Task<IActionResult> ResumoSaas(Guid id, [FromServices] SaasIntelligenceService inteligencia, [FromServices] AssinaturaGuardService guard)
    {
        var saude = await inteligencia.CalcularSaudeClienteAsync(id);
        var uso = await guard.ObterUsoPlanoAsync(id);
        return Ok(ApiResponse<object>.Ok(new { saude = saude.Data, uso = uso.Data }, "Resumo SaaS carregado."));
    }

    [HttpGet("{id:guid}/saude")]
    public async Task<IActionResult> Saude(Guid id, [FromServices] SaasIntelligenceService inteligencia)
    {
        var response = await inteligencia.CalcularSaudeClienteAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:guid}/uso-plano")]
    public async Task<IActionResult> UsoPlano(Guid id, [FromServices] AssinaturaGuardService guard)
    {
        var response = await guard.ObterUsoPlanoAsync(id);
        return StatusCode(response.StatusCode, response);
    }

    [HttpGet("{id:guid}/bloqueios")]
    public async Task<IActionResult> Bloqueios(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.QueryAsync<object>(@"select id as ""Id"", tipo as ""Tipo"", motivo as ""Motivo"", origem as ""Origem"", reg_date as ""RegDate""
from plantaopro.cliente_bloqueios
where cliente_id=@id and reg_status='A'
order by reg_date desc
limit 100", new { id });
            return Ok(ApiResponse<IEnumerable<object>>.Ok(rows));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar bloqueios do cliente {ClienteId}", id);
            return StatusCode(500, ApiResponse<string>.Fail("Não foi possível listar bloqueios do cliente.", 500));
        }
    }

    [HttpGet("{id:guid}/alertas")]
    public async Task<IActionResult> Alertas(Guid id, [FromServices] SaasIntelligenceService inteligencia)
    {
        var response = await inteligencia.ListarAlertasClienteAsync(id);
        return StatusCode(response.StatusCode, response);
    }

}
