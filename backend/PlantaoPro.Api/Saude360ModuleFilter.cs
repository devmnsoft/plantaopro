using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Npgsql;

namespace PlantaoPro.Api;

/// <summary>
/// Gate de módulo clínico por contrato do tenant: exige assinatura SAUDE360 ativa
/// (tenant_modulos habilitado/ativo) para usuários não-globais. Global admin faz bypass.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class Saude360ModuleAttribute : TypeFilterAttribute
{
    public Saude360ModuleAttribute() : base(typeof(Saude360ModuleFilter)) { }
}

public sealed class Saude360ModuleFilter : IAsyncAuthorizationFilter
{
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _cfg;

    public Saude360ModuleFilter(ICurrentUserService currentUser, IConfiguration cfg)
    {
        _currentUser = currentUser;
        _cfg = cfg;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Anônimo (rotas [AllowAnonymous], ex.: TV) e 401 são da camada de autenticação.
        if (context.HttpContext.User.Identity is not { IsAuthenticated: true }) return;
        if (_currentUser.IsGlobalAdmin()) return; // global admin bypass

        var tenantId = _currentUser.TenantId;
        if (tenantId is null)
        {
            context.Result = ModuloIndisponivel();
            return;
        }

        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var contratado = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"select exists(
select 1 from plantaopro.tenant_modulos tm
left join plantaopro.modulos_sistema ms on ms.id=tm.modulo_id and ms.reg_status='A'
where tm.tenant_id=@tenantId and tm.reg_status='A' and tm.habilitado=true
and upper(coalesce(tm.status,'ATIVO'))='ATIVO'
and upper(coalesce(nullif(tm.codigo_modulo,''),ms.codigo))='SAUDE360')", new { tenantId = tenantId.Value }, cancellationToken: context.HttpContext.RequestAborted));
            if (contratado) return;
        }
        catch
        {
            // fail-open: indisponibilidade de verificação não derruba o módulo inteiro
            // (uma falha de banco já apareceria em qualquer operação clínica).
            return;
        }

        context.Result = ModuloIndisponivel();
    }

    private static ObjectResult ModuloIndisponivel() => new(new { success = false, message = "Módulo Saúde 360 não contratado para este cliente." }) { StatusCode = StatusCodes.Status403Forbidden };
}
