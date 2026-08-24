using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Data;
using Dapper;
using Npgsql;

namespace PlantaoPro.Api.Controllers;

[ApiController]
[Route("api/permissoes")]
[Authorize(Roles = RolesConstants.AdministradorGlobal + "," + RolesConstants.Administrador + "," + RolesConstants.AdministradorCliente + "," + RolesConstants.Diretor + "," + RolesConstants.Suporte + "," + RolesConstants.Auditor)]
public sealed class PermissoesController : ControllerBase
{
    private readonly IEffectivePermissionService effectivePermissions;
    private readonly IConfiguration configuration;
    private readonly ICurrentUserService current;
    private readonly IAuditService audit;

    public PermissoesController(IEffectivePermissionService effectivePermissions,IConfiguration configuration,ICurrentUserService current,IAuditService audit)
    {
        this.effectivePermissions = effectivePermissions;
        this.configuration=configuration;this.current=current;this.audit=audit;
    }
    private static readonly string[] Perfis = new[]
    {
        RolesConstants.AdministradorGlobal, RolesConstants.Administrador, RolesConstants.AdministradorCliente, RolesConstants.Diretor, RolesConstants.Coordenador, RolesConstants.Operador, RolesConstants.Financeiro, RolesConstants.Medico, RolesConstants.Hospital, RolesConstants.Parceiro, RolesConstants.Suporte, RolesConstants.Auditor, RolesConstants.Comercial, RolesConstants.CustomerSuccess
    };

    private static readonly Dictionary<string, string[]> Matriz = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [RolesConstants.AdministradorGlobal] = new[] { "*" },
        [RolesConstants.Administrador] = new[] { "CLIENTE_PORTAL:GERENCIAR", "USUARIOS:GERENCIAR", "PERFIS:GERENCIAR", "WHITE_LABEL:EDITAR", "PLANTOES:GERENCIAR", "ESCALAS:GERENCIAR", "FINANCEIRO:VER", "FATURAS:VER" },
        [RolesConstants.AdministradorCliente] = new[] { "CLIENTE_PORTAL:GERENCIAR", "USUARIOS:GERENCIAR", "PERFIS:GERENCIAR", "WHITE_LABEL:EDITAR", "PLANTOES:GERENCIAR", "ESCALAS:GERENCIAR", "FINANCEIRO:VER", "FATURAS:VER" },
        [RolesConstants.Diretor] = new[] { "CLIENTE_PORTAL:VER", "RELATORIOS:VER", "FINANCEIRO:VER", "PLANTOES:VER", "ESCALAS:VER" },
        [RolesConstants.Coordenador] = new[] { "CENTRAL_ESCALA:VER", "PLANTOES:GERENCIAR", "ESCALAS:GERENCIAR", "CONVITES:GERENCIAR", "MEDICOS:VER", "HOSPITAIS:VER" },
        [RolesConstants.Operador] = new[] { "CENTRAL_ESCALA:VER", "PLANTOES:VER", "ESCALAS:VER", "CONVITES:VER" },
        [RolesConstants.Financeiro] = new[] { "FINANCEIRO:GERENCIAR", "PAGAMENTOS:CONFIRMAR", "RELATORIOS:VER", "FATURAS:VER", "EXPORTACOES:GERAR" },
        [RolesConstants.Medico] = new[] { "MEDICO_AREA:VER", "CONVITES:VER", "AGENDA:VER", "PAGAMENTOS:VER", "DISPONIBILIDADE:EDITAR", "SUBSTITUICOES:SOLICITAR" },
        [RolesConstants.Hospital] = new[] { "HOSPITAL_AREA:VER", "PLANTOES:VER", "ESCALAS:VER" },
        [RolesConstants.Parceiro] = new[] { "PARCEIRO:VER", "LEADS:VER", "PROPOSTAS:VER", "COMISSOES:VER", "REPASSES:VER" },
        [RolesConstants.Suporte] = new[] { "SUPORTE:GERENCIAR", "CHAMADOS:GERENCIAR", "AUDITORIA:VER", "TENANT_SUPORTE:ENTRAR" },
        [RolesConstants.Auditor] = new[] { "AUDITORIA:VER", "RELATORIOS:VER", "LGPD:VER" },
        [RolesConstants.Comercial] = new[] { "COMERCIAL:GERENCIAR", "LEADS:GERENCIAR", "PROPOSTAS:GERENCIAR", "PLANOS:VER" },
        [RolesConstants.CustomerSuccess] = new[] { "CUSTOMER_SUCCESS:GERENCIAR", "ONBOARDING:GERENCIAR", "CLIENTES:VER", "HEALTH_SCORE:VER" }
    };

    [HttpGet("matriz")]
    public IActionResult GetMatriz()
    {
        return Ok(ApiResponse<object>.Ok(new { perfis = Perfis, matriz = Matriz, bloqueios = MotivosBloqueio() }, "Matriz de permissões carregada."));
    }

    [HttpGet("perfil/{perfil}")]
    public IActionResult GetPerfil(string perfil)
    {
        if (!Matriz.TryGetValue(perfil, out var permissoes)) return NotFound(ApiResponse<object>.Fail("Perfil não encontrado.", 404));
        return Ok(ApiResponse<object>.Ok(new { perfil, permissoes }, "Permissões do perfil carregadas."));
    }

    [HttpGet("usuario/{usuarioId}")]
    public async Task<IActionResult> GetUsuario(Guid usuarioId, [FromQuery] Guid? tenantId, CancellationToken ct)
    {
        var permissoes = (await effectivePermissions.ObterPermissoesAsync(usuarioId, tenantId, ct)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return Ok(ApiResponse<object>.Ok(new { usuarioId, tenantId, permissoes }, "Permissões efetivas do usuário solicitado carregadas do PostgreSQL."));
    }

    [HttpPost("testar-acesso")]
    public async Task<IActionResult> TestarAcesso([FromBody] TestarAcessoRequest request, CancellationToken ct)
    {
        if (!request.UsuarioId.HasValue) return BadRequest(ApiResponse<object>.Fail("usuarioId é obrigatório para decisão efetiva.", 400));
        var result = await effectivePermissions.TestarAsync(request.UsuarioId.Value, request.TenantId, request.Modulo ?? string.Empty, request.Acao ?? "VER", ct);
        return Ok(ApiResponse<object>.Ok(result, result.Motivo));
    }

    [HttpPost("perfil/{perfilId:guid}/salvar")]
    public async Task<IActionResult> SalvarPerfil(Guid perfilId,[FromBody] SalvarPerfilPermissoesRequest request,CancellationToken ct)
    {
        if (!User.IsInRole(RolesConstants.AdministradorGlobal) && !User.IsInRole(RolesConstants.AdministradorCliente) && !User.IsInRole(RolesConstants.Administrador)) return Forbid();
        var result=await PersistirAsync(perfilId,request.Permissoes??Array.Empty<string>(),ct);
        if(result is null)return NotFound(ApiResponse<object>.Fail("Perfil inexistente ou fora do tenant atual.",404));
        return Ok(ApiResponse<object>.Ok(new { perfilId,permissoes=result,persistido=true,transacao=true },"Permissões persistidas em transação auditável."));
    }

    [HttpPost("perfil/{perfil}/restaurar-padrao")]
    public async Task<IActionResult> RestaurarPadrao(string perfil,CancellationToken ct)
    {
        if (!Matriz.TryGetValue(perfil, out var permissoes)) return NotFound(ApiResponse<object>.Fail("Perfil não encontrado.", 404));
        var id=await PerfilIdAsync(perfil,ct);if(!id.HasValue)return NotFound(ApiResponse<object>.Fail("Perfil não encontrado no escopo atual.",404));
        var saved=await PersistirAsync(id.Value,permissoes,ct);return Ok(ApiResponse<object>.Ok(new{perfil,permissoes=saved},"Permissões padrão restauradas e persistidas."));
    }

    [HttpPost("perfil/{perfil}/copiar")]
    public async Task<IActionResult> Copiar(string perfil,[FromBody] CopiarPermissoesRequest request,CancellationToken ct)
    {
        var origem = request.PerfilOrigem ?? string.Empty;
        var origemId=await PerfilIdAsync(origem,ct);var destinoId=await PerfilIdAsync(perfil,ct);if(!origemId.HasValue||!destinoId.HasValue)return NotFound(ApiResponse<object>.Fail("Perfil de origem ou destino não encontrado no escopo atual.",404));
        await using var cn=new NpgsqlConnection(configuration.GetConnectionString("Default"));var permissoes=(await cn.QueryAsync<string>(new CommandDefinition("select p.codigo from plantaopro.perfil_permissoes pp join plantaopro.permissoes p on p.id=pp.permissao_id where pp.perfil_id=@origemId and pp.reg_status='A' and pp.permitido=true and p.reg_status='A'",new{origemId},cancellationToken:ct))).ToArray();
        var saved=await PersistirAsync(destinoId.Value,permissoes,ct);return Ok(ApiResponse<object>.Ok(new{perfilDestino=perfil,perfilOrigem=origem,permissoes=saved},"Permissões copiadas e persistidas."));
    }

    private async Task<Guid?> PerfilIdAsync(string codigo,CancellationToken ct){await using var cn=new NpgsqlConnection(configuration.GetConnectionString("Default"));return await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition("select id from plantaopro.perfis where upper(coalesce(codigo,nome))=upper(@codigo) and reg_status='A' and (@global or tenant_id=@tenantId) order by tenant_id nulls last limit 1",new{codigo,global=current.IsGlobalAdmin(),tenantId=current.TenantId},cancellationToken:ct));}
    private async Task<string[]?> PersistirAsync(Guid perfilId,IEnumerable<string> requested,CancellationToken ct)
    {
        var values=requested.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim().ToUpperInvariant()).Distinct().ToArray();
        await using var cn=new NpgsqlConnection(configuration.GetConnectionString("Default"));await cn.OpenAsync(ct);await using var tx=await cn.BeginTransactionAsync(ct);
        var allowed=await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.perfis where id=@perfilId and reg_status='A' and (@global or tenant_id=@tenantId))",new{perfilId,global=current.IsGlobalAdmin(),tenantId=current.TenantId},tx,cancellationToken:ct));if(!allowed){await tx.RollbackAsync(ct);return null;}
        var ids=(await cn.QueryAsync<(Guid Id,string Codigo)>(new CommandDefinition("select id,codigo Codigo from plantaopro.permissoes where codigo=any(@values) and reg_status='A'",new{values},tx,cancellationToken:ct))).ToArray();if(ids.Length!=values.Length){await tx.RollbackAsync(ct);throw new ArgumentException("Uma ou mais permissões não pertencem ao catálogo canônico.");}
        var before=(await cn.QueryAsync<string>(new CommandDefinition("select p.codigo from plantaopro.perfil_permissoes pp join plantaopro.permissoes p on p.id=pp.permissao_id where pp.perfil_id=@perfilId and pp.reg_status='A' and pp.permitido=true",new{perfilId},tx,cancellationToken:ct))).ToArray();
        await cn.ExecuteAsync(new CommandDefinition("update plantaopro.perfil_permissoes set reg_status='I',reg_update=now(),updated_by=@userId where perfil_id=@perfilId and reg_status='A'",new{perfilId,userId=current.UserId},tx,cancellationToken:ct));
        foreach(var permission in ids)await cn.ExecuteAsync(new CommandDefinition("insert into plantaopro.perfil_permissoes(perfil_id,permissao_id,permitido,created_by) values(@perfilId,@permissionId,true,@userId)",new{perfilId,permissionId=permission.Id,userId=current.UserId},tx,cancellationToken:ct));
        await tx.CommitAsync(ct);await audit.RegistrarAsync(current.UserId, current.TenantId,"PERFIL",perfilId,"PERMISSOES_ATUALIZADAS",new{Antes=before,Depois=values},true,null,current.Roles.FirstOrDefault(),ct);return values;
    }

    private static string[] MotivosBloqueio()
    {
        return new[] { "Sem perfil", "Sem permissão", "Plano não permite", "Módulo não contratado", "Tenant bloqueado", "Assinatura vencida", "Usuário inativo" };
    }
}

public sealed class TestarAcessoRequest
{
    public string? Perfil { get; set; }
    public string? Modulo { get; set; }
    public string? Acao { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UsuarioId { get; set; }
}

public sealed class SalvarPerfilPermissoesRequest
{
    public string[]? Permissoes { get; set; }
}

public sealed class CopiarPermissoesRequest
{
    public string? PerfilOrigem { get; set; }
}
