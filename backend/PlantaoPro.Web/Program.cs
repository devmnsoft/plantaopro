using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using PlantaoPro.Web.Services;
using PlantaoPro.Web.Services.Mvc;
using PlantaoPro.Web.Services.Security;

using PlantaoPro.CrossCutting.Security;

var builder = WebApplication.CreateBuilder(args);

// A2 (rodada 4): validação de configuração na inicialização (fail-fast com mensagem clara
// e sem segredos): BaseUrl da API interna obrigatória, sem porta de dev fora de Development,
// e DataProtection:KeysDirectory obrigatório em Production (persistência de cookies/sessões).
PlantaoProApiStartupValidator.Validate(builder.Configuration, builder.Environment);

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestHeadersTotalSize = 128 * 1024);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<SaasRouteGuardFilter>();
    // Gate financeiro (item 2): todo decimal de formulario/route/query passa pelo contrato
    // central ValorHumano (pt-BR + invariante, ambiguidades rejeitadas), nunca pelo binder
    // padrao em cultura invariante que lia "12,50" como 1250 (bug M2.5).
    options.ModelBinderProviders.Insert(0, new PlantaoPro.Web.Models.ValorHumanoModelBinderProvider());
    // Metade 2 do gate: sem o filtro, um decimal que nao ligou chegaria a acao como 0
    // e o fluxo financeiro prosseguiria em silencio. Invalido -> PRG com mensagem humana.
    options.Filters.AddService<ModelStateInvalidoFiltro>();
});
// Gate financeiro (item 2): a lingua visual da aplicacao e o portugues do Brasil —
// formatacoes de moeda/data nas views ("C", "N2", "0.00") seguem a cultura pt-BR.
// O binding de decimal NAO depende dessa cultura: e tratado pelo binder acima.
builder.Services.AddRequestLocalization(localization =>
{
    localization.DefaultRequestCulture = new RequestCulture("pt-BR");
    localization.SupportedCultures.Add(new CultureInfo("pt-BR"));
    localization.SupportedUICultures.Add(new CultureInfo("pt-BR"));
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IRoleCatalog, RoleCatalog>();
builder.Services.AddSingleton<IPrimaryRoleResolver, PrimaryRoleResolver>();
builder.Services.AddSingleton<IAccessScopeResolver, AccessScopeResolver>();
builder.Services.AddSingleton<ITenantContextResolver, TenantContextResolver>();
builder.Services.AddSingleton<IFeatureCatalogService, FeatureCatalogService>();
builder.Services.AddScoped<IPageContextService, PageContextService>();

builder.Services.AddScoped<IInteligenciaNegocioService, InteligenciaNegocioService>();
builder.Services.AddScoped<IAssistenteContextualService, AssistenteContextualService>();
builder.Services.AddScoped<IFase2OperationalFlowService, Fase2OperationalFlowService>();
builder.Services.AddScoped<ProductivityWebService>();
builder.Services.AddScoped<AiWebService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IModuleAccessService, ModuleAccessService>();
builder.Services.AddScoped<ITenantAccessService, TenantAccessService>();
builder.Services.AddScoped<IMenuBuilderService, MenuBuilderService>();
builder.Services.AddScoped<SaasRouteGuardFilter>();
builder.Services.AddScoped<ModelStateInvalidoFiltro>();
builder.Services.AddSession();
// A2 (rodada 4): persistir as chaves de Data Protection fora da memória quando o diretório
// for configurado. Em Production o diretório é obrigatório (validado no startup) e deve ser
// compartilhado por todas as instâncias do Web; sem isso cookies e sessões morrem a cada
// reciclagem do pool. Sessões continuam em memória: em múltiplas instâncias usar sticky
// sessions ou externalizar o store (ver guia de implantação IIS).
var dataProtectionKeysDirectory = builder.Configuration["DataProtection:KeysDirectory"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysDirectory))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDirectory));
}
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "PlantaoPro.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        // Contrato JSON do BFF: consumidores de /bff/* recebem 401/403 em envelope JSON,
        // nunca redirect silencioso para as páginas HTML de login/negado.
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = async context =>
            {
                if (!BffContracts.IsBffPath(context.Request.Path))
                {
                    context.Response.Redirect(context.RedirectUri);
                    return;
                }
                await BffContracts.RespondAsync(context.HttpContext, StatusCodes.Status401Unauthorized, BffContracts.RazaoSessaoExpirada, BffContracts.MensagemSessaoExpirada);
            },
            OnRedirectToAccessDenied = async context =>
            {
                if (!BffContracts.IsBffPath(context.Request.Path))
                {
                    context.Response.Redirect(context.RedirectUri);
                    return;
                }
                await BffContracts.RespondAsync(context.HttpContext, StatusCodes.Status403Forbidden, BffContracts.RazaoAcessoNegado, BffContracts.MensagemAcessoNegado);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    var policies = new[] { "GlobalAccess", "TenantAccess", "HybridAccess", "TenantContextRequired", "TenantContextOptional", "CanSwitchTenant", "CanImpersonateTenant", "CanManageSaas", "CanViewGlobalAudit", "CentralAtendimento.Ver", "Agendamento.Criar", "Agendamento.Confirmar", "Agendamento.CheckIn", "PainelChamada.Operar", "Triagem.Iniciar", "Triagem.Finalizar", "Consulta.Iniciar", "Consulta.Editar", "Consulta.Finalizar", "Consulta.VerDadosSensiveis" };
    foreach (var policy in policies) options.AddPolicy(policy, p => p.RequireAuthenticatedUser());
});

builder.Services.AddHttpClient("PlantaoProApi", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("PlantaoProApiHttpClient");

    // Precedência oficial: ApiSettings:BaseUrl > PlantaoProApi:BaseUrl (validada no startup).
    var baseUrl = PlantaoProApiStartupValidator.ResolveBaseUrl(cfg);
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("Configuração PlantaoProApi:BaseUrl não encontrada.");

    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(cfg.GetValue("PlantaoProApi:LoginTimeoutSeconds", 15));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    logger.LogInformation("HttpClient PlantaoProApi configurado com BaseUrl: {BaseUrl}", client.BaseAddress);
});
// O BFF precisa ver redirects da API explicitamente para convertê-los em JSON (401/502),
// sem seguir silenciosamente para páginas HTML.
builder.Services.AddHttpClient("PlantaoProApi")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<Saude360WebService>();
builder.Services.AddScoped<MinhaCentralWebService>();
builder.Services.AddScoped<ManagerCommandCenterWebService>();
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/erro");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
// Páginas de erro HTML só para rotas de página: o BFF mantém o envelope JSON (status >= 400)
// sem re-execução, para consumidores de API nunca receberem HTML no corpo da resposta.
// (UseWhen porque UseStatusCodePagesWithReExecute não tem sobrecarga com predicate.)
app.UseWhen(context => !BffContracts.IsBffPath(context.Request.Path), branch => branch.UseStatusCodePagesWithReExecute("/erro/{0}"));

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
