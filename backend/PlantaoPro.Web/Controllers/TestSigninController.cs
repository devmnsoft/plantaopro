using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using PlantaoPro.Web.Services.Security;

namespace PlantaoPro.Web.Controllers;

/// <summary>
/// Ponta de teste para a suíte de integração Web: emite um cookie de sessão com claims
/// controlados — papéis, módulos (catálogo v2149), permissões e status do cliente — e um
/// JWT fake na sessão, espelhando o formato real de SessionClaimsBuilder.
/// R4-A5: habilitada SOMENTE em ambiente Testing e com a flag explícita TestAuth:Enabled=true
/// (desabilitada por padrão). Em Production e Development responde sempre 404 — inclusive
/// com a flag presente — para que um pool mal configurado como Development não exponha a ponta.
/// </summary>
[Route("__test")]
[AllowAnonymous]
public sealed class TestSigninController : Controller
{
    private static readonly Guid UsuarioFixo = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly IAntiforgery _antiforgery;

    public TestSigninController(IWebHostEnvironment env, IConfiguration config, IAntiforgery antiforgery)
    {
        this._env = env;
        this._config = config;
        this._antiforgery = antiforgery;
    }

    /// <summary>
    /// R4-A5: fonte única da habilitação desta ponta. O ambiente deve ser Testing e a flag
    /// TestAuth:Enabled deve parsear para true. A flag NÃO habilita Development nem
    /// Production: a proteção contra pool com ambiente errado não depende de configuração.
    /// </summary>
    public static bool Habilitado(string ambiente, string? flag) =>
        string.Equals(ambiente, "Testing", StringComparison.OrdinalIgnoreCase)
        && bool.TryParse(flag, out var ligado) && ligado;

    [HttpGet("signin")]
    public async Task<IActionResult> Signin(
        [FromQuery] string roles = "MEDICO",
        [FromQuery] string? modules = null,
        [FromQuery] string? permissions = null,
        [FromQuery] string? jwt = "test-jwt",
        [FromQuery] bool semJwt = false,
        [FromQuery] bool? clienteBloqueado = null,
        [FromQuery(Name = "claim")] List<string>? claims = null)
    {
        if (!Habilitado(_env.EnvironmentName, _config["TestAuth:Enabled"]))
            return NotFound();

        var claimsList = new List<Claim>
        {
            new(ClaimTypes.Name, "Usuário de Teste"),
            new(ClaimTypes.NameIdentifier, UsuarioFixo.ToString()),
            new("uid", UsuarioFixo.ToString()),
            // Sessões v2.14.9: as permissões/módulos vêm do catálogo efetivo da API.
            new("access_catalog_version", "v2149")
        };

        foreach (var role in SplitCsv(roles))
            claimsList.Add(new(ClaimTypes.Role, role));
        foreach (var module in SplitCsv(modules))
            claimsList.Add(new("module", module.Trim().ToUpperInvariant().Replace(':', '.')));
        if (!string.IsNullOrWhiteSpace(permissions))
            claimsList.Add(new("permissions", permissions.Trim()));
        if (clienteBloqueado == true)
            claimsList.Add(new("cliente_status", "INATIVO"));
        if (claims != null)
        {
            foreach (var pair in claims)
            {
                var idx = pair.IndexOf('=');
                if (idx > 0) claimsList.Add(new(pair[..idx], pair[(idx + 1)..]));
            }
        }

        // Em .NET 10 a query vazia (?jwt=) nao faz bind para string? com padrao, por isso o
        // sinalizador explicito semJwt= para testes que precisam de sessao autenticada sem JWT.
        var tokenSessao = semJwt ? string.Empty : jwt ?? string.Empty;
        HttpContext.Session.SetString("JwtToken", tokenSessao);
        HttpContext.Session.SetString("jwt", tokenSessao);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claimsList, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        // Fixa o principal no contexto atual: no .NET 10 o SignInAsync nao atualiza
        // HttpContext.User dentro da requisicao, e o token antiforgery abaixo precisa ser
        // emitido com o uid JÁ autenticado para validar nos POSTs subsequentes da suíte.
        HttpContext.User = principal;

        // Token antiforgery gerado no contexto JÁ autenticado (uid do usuário de teste), para
        // que os POSTs da suíte passem na validação de antiforgery sem depender de scrape de HTML.
        // O cookie antiforgery também é definido aqui e fica guardado pelo client do teste.
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Content("{\"ok\":true,\"antiforgery\":\"" + tokens.RequestToken + "\"}", "application/json");
    }

    /// <summary>
    /// Diagnostico da sessão atual (apenas Testing habilitado pela flag — R4-A5): session
    /// keys/values e claims, além de uma sonda direta de IsModuleEnabled para investigar
    /// decisões de landing/guarda.
    /// </summary>
    [HttpGet("dump")]
    public IActionResult Dump()
    {
        if (!Habilitado(_env.EnvironmentName, _config["TestAuth:Enabled"]))
            return NotFound();

        var session = new Dictionary<string, string?>();
        foreach (var key in HttpContext.Session.Keys)
            session[key] = HttpContext.Session.GetString(key);

        var moduleAccess = HttpContext.RequestServices.GetService<IModuleAccessService>();
        var probe = new Dictionary<string, bool>();
        foreach (var modulo in new[] { "MEDICO_AREA", "MEU_DIA", "AGENDA" })
            probe[modulo] = moduleAccess is null ? false : moduleAccess.IsModuleEnabled(modulo);

        return Json(new
        {
            anon = !User.Claims.Any(),
            claims = User.Claims.Select(c => c.Type + "=" + c.Value),
            session,
            probe
        });
    }

    private static IEnumerable<string> SplitCsv(string? value) =>
        (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
