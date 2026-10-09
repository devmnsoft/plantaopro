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

        // R6-BlocoA item 2: verificação pela função canônica (v2339) — mesmo
        // predicado B6 dos claims, agora também para o gate por request e com
        // herança das capacidades do pacote. Fail-open removido: falha de banco
        // é indisponibilidade real (500 honesto), não autorização silenciosa
        // de módulo não contratado.
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var contratado = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
            "select plantaopro.modulo_efetivo(@tenantId,'SAUDE360')", new { tenantId = tenantId.Value }, cancellationToken: context.HttpContext.RequestAborted));
        if (!contratado) context.Result = ModuloIndisponivel();
    }

    private static ObjectResult ModuloIndisponivel() => new(new { success = false, message = "Módulo Saúde 360 não contratado para este cliente." }) { StatusCode = StatusCodes.Status403Forbidden };
}
