using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequestLogContextFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpLogging(_ => { });
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<DefaultApiResponseOperationFilter>();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PlantaoPro API",
        Version = "v1",
        Description = "API principal do PlantaoPro para autenticação, escalas e gestão operacional."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentCors", policy =>
    {
        policy.WithOrigins(
            "https://localhost:5259",
            "http://localhost:5259",
            "https://localhost:5001",
            "http://localhost:5000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Configuração Jwt:Key não encontrada ou inválida.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    });

builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MedicoService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<PlantaoService>();
builder.Services.AddScoped<PlantaoRegraService>();
builder.Services.AddScoped<PlantaoHistoricoService>();
builder.Services.AddScoped<PlantaoTransicaoService>();
builder.Services.AddScoped<EspecialidadeService>();
builder.Services.AddScoped<HospitalService>();
builder.Services.AddScoped<EscalaService>();
builder.Services.AddScoped<ConflitoHorarioService>();
builder.Services.AddScoped<MedicoElegibilidadeService>();
builder.Services.AddScoped<MedicoRecomendacaoService>();
builder.Services.AddScoped<FinanceiroService>();
builder.Services.AddScoped<NotificacaoService>();
builder.Services.AddScoped<MedicoAreaService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<NotificationPreferenceService>();
builder.Services.AddScoped<PremiumOperacoesService>();
builder.Services.AddScoped<OperacaoService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<OnboardingService>();
builder.Services.AddScoped<BiService>();
builder.Services.AddScoped<RequestLogContextFilter>();
builder.Services.AddScoped<UsuarioContextService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPermissionService, ModulePermissionService>();
builder.Services.AddScoped<IModuleAccessService, ModulePermissionService>();
builder.Services.AddScoped<ITenantAccessService, ModulePermissionService>();
builder.Services.AddScoped<TenantGuardService>();
builder.Services.AddScoped<PermissionGuardService>();
builder.Services.AddScoped<AssinaturaGuardService>();
builder.Services.AddScoped<SaasIntelligenceService>();
builder.Services.AddScoped<ILogOperacionalService, LogOperacionalService>();
builder.Services.AddScoped<ILgpdAuditService, LgpdAuditService>();
builder.Services.AddScoped<IEventoSistemaService, EventoSistemaService>();
builder.Services.AddScoped<LgpdService>();
builder.Services.AddScoped<JornadaClienteService>();
builder.Services.AddScoped<ComercialSaasService>();
builder.Services.AddScoped<AjudaInterativaService>();
builder.Services.AddScoped<TenantContextService>();
builder.Services.AddScoped<SelfServiceSaasService>();
builder.Services.AddScoped<TenantIsolationValidatorService>();
builder.Services.AddScoped<B2BLaunchService>();
builder.Services.AddScoped<B2BCommercialOpsService>();
builder.Services.AddScoped<CommercialDemoService>();
builder.Services.AddScoped<OperationalAutomationService>();
builder.Services.AddScoped<Saude360ClinicalService>();
builder.Services.AddScoped<Fase6BiIntegracoesService>();
builder.Services.AddScoped<OperacaoRecomendacaoService>();
builder.Services.AddScoped<DashboardPremiumService>();
builder.Services.AddScoped<V113OperationalService>();
builder.Services.AddScoped<V114ProdutoService>();
builder.Services.AddScoped<V115FaturamentoRegraService>();
builder.Services.AddScoped<V115RepasseMedicoService>();
builder.Services.AddScoped<V115GlosaService>();
builder.Services.AddScoped<V116ConvenioService>();
builder.Services.AddScoped<V116LoteFaturamentoService>();
builder.Services.AddScoped<V116CaixaService>();
builder.Services.AddScoped<V116TimelineService>();
builder.Services.AddScoped<V116NotificacaoOperacionalService>();
builder.Services.AddScoped<V116RelatorioExecutivoService>();

var app = builder.Build();
app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));

    await DevelopmentSeed.RunAsync(app.Services);
    app.UseCors("DevelopmentCors");
}
else
{
    app.MapGet("/", (IWebHostEnvironment environment) => Results.Ok(ApiResponse<HealthDto>.Ok(
        new HealthDto(
            "PlantaoPro.Api",
            "Healthy",
            environment.EnvironmentName,
            DateTime.UtcNow,
            typeof(Program).Assembly.GetName().Version?.ToString() ?? string.Empty),
        "PlantaoPro.Api online")));
}

app.UseExceptionHandler(a => a.Run(async ctx =>
{
    ctx.Response.StatusCode = 500;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(ApiResponse<string>.Fail("Erro interno ao processar a solicitação.", 500));
}));

app.UseAuthentication();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();
