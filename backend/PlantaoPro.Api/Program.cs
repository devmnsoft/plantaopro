using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using PlantaoPro.Api;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Security;
using PlantaoPro.Api.Clinical;
using PlantaoPro.Api.Realtime;
using PlantaoPro.Api.Operation360.WorkItems;
using PlantaoPro.Api.Operation360.Realtime;
using PlantaoPro.Api.Operation360.Notifications;
using PlantaoPro.Api.Operation360.Productivity;
using PlantaoPro.Api.Productivity;
using PlantaoPro.Api.Ai;
using PlantaoPro.Api.SavedViews;
using System.Text;

using PlantaoPro.CrossCutting.Security;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;

// O Dapper não mapeia DateOnly em parâmetros; registro global antes de qualquer consulta.
DapperTypeHandlerRegistrar.RegistrarTodos();

var builder = WebApplication.CreateBuilder(args);
// F5 (Jornada S): o JWT embute o catalogo de permissoes do perfil. Para administradores com
// centenas de permissoes (ex.: admin.clinica ~41 KB) o header Authorization excede o limite
// padrao do Kestrel (32 KB) e toda chamada autenticada BFF->API falha com HTTP 431.
builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestHeadersTotalSize = 128 * 1024);
var connectionString = builder.Configuration.GetConnectionString("Default");
DatabaseStartupReadinessValidator.Validate(connectionString, builder.Environment, builder.Configuration);
if (builder.Environment.IsProduction() && !builder.Configuration.GetValue("Authentication:LoginLockoutEnabled", true))
{
    throw new InvalidOperationException("Authentication:LoginLockoutEnabled não pode ser desativado em Production.");
}


builder.Services.AddSignalR();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequestLogContextFilter>();
    options.Filters.Add<Adm360BusinessExceptionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpLogging(_ => { });
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IRoleCatalog, RoleCatalog>();
builder.Services.AddSingleton<IPrimaryRoleResolver, PrimaryRoleResolver>();
builder.Services.AddSingleton<IAccessScopeResolver, AccessScopeResolver>();
builder.Services.AddSingleton<ITenantContextResolver, TenantContextResolver>();

builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace("+", ".", StringComparison.Ordinal));
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
var jwtKey = jwt["Key"] ?? string.Empty;
var jwtIssuer = jwt["Issuer"] ?? string.Empty;
var jwtAudience = jwt["Audience"] ?? string.Empty;
JwtConfigurationValidator.Validate(jwtKey, jwtIssuer, jwtAudience);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sessions = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationSessionService>();
                if (context.Principal is null || !await sessions.ValidateAsync(context.Principal, context.HttpContext.RequestAborted))
                {
                    context.Fail("Sessão expirada, revogada ou sem contexto ativo.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    var scopes=new[]{"GlobalAccess","TenantAccess","HybridAccess","TenantContextRequired","TenantContextOptional"};
    foreach(var name in scopes)options.AddPolicy(name,p=>p.RequireAuthenticatedUser().AddRequirements(new EffectiveAccessRequirement(name)));
    var permissions=new Dictionary<string,string>{["Adm360.Ver"]="ADM360:VER",["Adm360.Compras"]="ADM360:COMPRAS",["Adm360.Estoque"]="ADM360:ESTOQUE",["Adm360.Qualidade"]="ADM360:LIBERAR_QUALIDADE",["Adm360.InventarioAprovar"]="ADM360:INVENTARIO_APROVAR",["Adm360.Comercial"]="ADM360:COMERCIAL",["Adm360.Cirurgias"]="ADM360:CIRURGIAS",["Adm360.Separar"]="ADM360:SEPARAR",["Adm360.Expedir"]="ADM360:EXPEDIR",["Adm360.Reconciliar"]="ADM360:RECONCILIAR",["Adm360.Valorizar"]="ADM360:VALORIZAR",["Adm360.Vendas"]="ADM360:VENDAS",["Adm360.Receber"]="ADM360:RECEBER",["Adm360.Estornar"]="ADM360:ESTORNAR",["Adm360.Financeiro"]="ADM360:FINANCEIRO",["Adm360.Pagar"]="ADM360:PAGAR",["Adm360.AprovarDespesa"]="ADM360:APROVAR_DESPESA",["Adm360.CriarDespesa"]="ADM360:CRIAR_DESPESA",["Adm360.FecharCaixa"]="ADM360:FECHAR_CAIXA",["Adm360.ConfigurarIntegracao"]="ADM360:CONFIGURAR_INTEGRACAO",["Adm360.CotacaoConsultar"]="ADM360:COTACAO_CONSULTAR",["Adm360.MapearCadastros"]="ADM360:MAPEAR_CADASTROS",["Adm360.MapearProdutos"]="ADM360:MAPEAR_PRODUTOS",["Adm360.ElaborarOrcamento"]="ADM360:ELABORAR_ORCAMENTO",["Adm360.AprovarResposta"]="ADM360:APROVAR_RESPOSTA",["Adm360.TransmitirResposta"]="ADM360:TRANSMITIR_RESPOSTA",["Adm360.ConsultarAnexos"]="ADM360:CONSULTAR_ANEXOS",["Adm360.ImportarXml"]="ADM360:IMPORTAR_XML",["Adm360.ManifestarDfe"]="ADM360:MANIFESTAR_DFE",["Adm360.VincularDocumentos"]="ADM360:VINCULAR_DOCUMENTOS",["Adm360.Exportar"]="ADM360:EXPORTAR",["Adm360.Auditar"]="ADM360:AUDITAR",["CanSwitchTenant"]="CONTEXTO:TROCAR",["CanImpersonateTenant"]="TENANT_SUPORTE:ENTRAR",["CanManageSaas"]="SAAS:GERENCIAR",["CanViewGlobalAudit"]="AUDITORIA:VER",["CentralAtendimento.Ver"]="CENTRAL_ATENDIMENTO:VER",["Agendamento.Criar"]="AGENDAMENTO:CRIAR",["Agendamento.Confirmar"]="AGENDAMENTO:CONFIRMAR",["Agendamento.CheckIn"]="AGENDAMENTO:CHECKIN",["PainelChamada.Operar"]="PAINEL_CHAMADA:OPERAR",["Triagem.Iniciar"]="TRIAGEM:INICIAR",["Triagem.Finalizar"]="TRIAGEM:FINALIZAR",["Consulta.Iniciar"]="CONSULTA:INICIAR",["Consulta.Editar"]="CONSULTA:EDITAR",["Consulta.Finalizar"]="CONSULTA:FINALIZAR",["Consulta.Adendo"]="CONSULTA:ADENDO",["Consulta.VerDadosSensiveis"]="CONSULTA:VER_DADOS_SENSIVEIS",["CID.Vincular"]="CID:VINCULAR",["CID.Remover"]="CID:REMOVER",["Prescricao.Criar"]="PRESCRICAO:CRIAR",["Prescricao.Editar"]="PRESCRICAO:EDITAR",["Prescricao.Finalizar"]="PRESCRICAO:FINALIZAR",["Relatorios.Ver"]="RELATORIOS:VER",["Relatorios.Exportar"]="RELATORIOS:EXPORTAR",["Relatorios.Executivos"]="RELATORIOS:EXECUTIVOS",["Relatorios.Financeiros"]="RELATORIOS:FINANCEIROS",["Relatorios.Clinicos"]="RELATORIOS:CLINICOS",["Relatorios.DadosSensiveis"]="RELATORIOS:DADOS_SENSIVEIS"};
    foreach(var item in permissions)options.AddPolicy(item.Key,p=>p.RequireAuthenticatedUser().AddRequirements(new EffectiveAccessRequirement(item.Key,item.Value)));
});
builder.Services.AddScoped<IAuthorizationHandler,EffectiveAccessAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationMiddlewareResultHandler>();

builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<PainelTvService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<IAuthenticationSessionService, AuthenticationSessionService>();
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
builder.Services.AddScoped<ProfessionalPortalService>();
builder.Services.AddScoped<ExecutionConferenceService>();
builder.Services.AddScoped<OperationalReportService>();
builder.Services.AddScoped<ManagerCommandCenterService>();
builder.Services.AddScoped<UnitDashboardService>();
builder.Services.AddScoped<ShiftRequestService>();
builder.Services.AddScoped<ShiftRequestApprovalService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<NotificationPreferenceService>();
builder.Services.AddScoped<PremiumOperacoesService>();
builder.Services.AddScoped<OperacaoService>();
builder.Services.AddScoped<OcorrenciaService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<OnboardingService>();
builder.Services.AddScoped<BiService>();
builder.Services.AddScoped<RequestLogContextFilter>();
builder.Services.AddScoped<UsuarioContextService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPermissionService, ModulePermissionService>();
builder.Services.AddScoped<IEffectivePermissionService, EffectivePermissionService>();
builder.Services.AddScoped<IPasswordPolicyService, PasswordPolicyService>();
builder.Services.AddScoped<SecurityAdministrationService>();
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
builder.Services.AddScoped<SaasModuleCatalogService>();
builder.Services.AddScoped<ModuleContractingService>();
builder.Services.AddScoped<Administrativo360Service>();
string GetConn(IServiceProvider sp) => sp.GetRequiredService<IConfiguration>().GetConnectionString("Default") ?? connectionString!;
builder.Services.AddScoped<ICadastrosRepository>(sp => new CadastrosRepository(GetConn(sp)));
builder.Services.AddScoped<IComprasRepository>(sp => new ComprasRepository(GetConn(sp)));
builder.Services.AddScoped<IEstoqueRepository>(sp => new EstoqueRepository(GetConn(sp)));
builder.Services.AddScoped<IQualidadeRepository>(sp => new QualidadeRepository(GetConn(sp)));
builder.Services.AddScoped<IColetaRepository>(sp => new ColetaRepository(GetConn(sp)));
builder.Services.AddScoped<IInventarioRepository>(sp => new InventarioRepository(GetConn(sp)));
builder.Services.AddScoped<IOrcamentoCirurgicoRepository>(sp => new OrcamentoCirurgicoRepository(GetConn(sp)));
builder.Services.AddScoped<ICirurgiaRepository>(sp => new CirurgiaRepository(GetConn(sp)));
builder.Services.AddScoped<IValeConsignacaoRepository>(sp => new ValeConsignacaoRepository(GetConn(sp)));
builder.Services.AddScoped<IAdm360RelatoriosRepository>(sp => new Adm360RelatoriosRepository(GetConn(sp)));
builder.Services.AddScoped<IValorizacaoRepository>(sp => new ValorizacaoRepository(GetConn(sp)));
builder.Services.AddScoped<IVendaRepository>(sp => new VendaRepository(GetConn(sp)));
builder.Services.AddScoped<IContasReceberRepository>(sp => new ContasReceberRepository(GetConn(sp)));
builder.Services.AddScoped<IContasPagarRepository>(sp => new ContasPagarRepository(GetConn(sp)));
builder.Services.AddScoped<ICaixaRepository>(sp => new CaixaRepository(GetConn(sp)));
builder.Services.AddScoped<IAdm360FinanceiroRelatoriosRepository>(sp => new Adm360FinanceiroRelatoriosRepository(GetConn(sp)));
builder.Services.AddScoped<ICotacoesRepository>(sp => new CotacoesRepository(GetConn(sp)));
builder.Services.AddScoped<IDocumentosXmlRepository>(sp => new DocumentosXmlRepository(GetConn(sp)));
builder.Services.AddScoped<IGestaoDashboardRepository>(sp => new GestaoDashboardRepository(GetConn(sp)));
builder.Services.AddScoped<OperationalAutomationService>();
builder.Services.AddScoped<Saude360ClinicalService>();
builder.Services.AddScoped<IWorkflowSaude360Service, WorkflowSaude360Service>();
builder.Services.AddScoped<IConsultaRepository, ConsultaRepository>();
builder.Services.AddScoped<IConsultaApplicationService, ConsultaApplicationService>();
builder.Services.AddScoped<ILongitudinalRepository, LongitudinalRepository>();
builder.Services.AddScoped<ILongitudinalService, LongitudinalService>();
builder.Services.AddScoped<IClinicalAccessService, ClinicalAccessService>();
builder.Services.AddSingleton<IClinicalDocumentSignatureProvider, NoOpClinicalDocumentSignatureProvider>();
builder.Services.AddScoped<IGlobalSearchRepository, GlobalSearchRepository>();
builder.Services.AddScoped<IGlobalSearchService, GlobalSearchService>();
builder.Services.AddScoped<ICentralAtendimentoService, CentralAtendimentoService>();
builder.Services.AddScoped<Fase6BiIntegracoesService>();
builder.Services.AddScoped<OperacaoRecomendacaoService>();
builder.Services.AddScoped<DashboardPremiumService>();
builder.Services.AddScoped<V113OperationalService>();
builder.Services.AddScoped<V114ProdutoService>();
builder.Services.AddScoped<V115FaturamentoRegraService>();
builder.Services.AddScoped<V115RepasseMedicoService>();
builder.Services.AddScoped<V115GlosaService>();
builder.Services.AddScoped<FinancialTenantContext>();
builder.Services.AddScoped<PlantaoPro.Api.Fechamentos.FechamentoOperacionalService>();
builder.Services.AddScoped<V116ConvenioService>();
builder.Services.AddScoped<V116LoteFaturamentoService>();
builder.Services.AddScoped<V116CaixaService>();
builder.Services.AddScoped<V116TimelineService>();
builder.Services.AddScoped<V116NotificacaoOperacionalService>();
builder.Services.AddScoped<V116RelatorioExecutivoService>();
builder.Services.AddSingleton<IReportCatalogService, ReportCatalogService>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IReportPermissionService, ReportPermissionService>();
builder.Services.AddScoped<IReportAuditService, ReportAuditService>();
builder.Services.AddScoped<CsvExportService>();
builder.Services.AddScoped<IContextoRepository, ContextoRepository>();
builder.Services.AddScoped<IContextoService, ContextoService>();
builder.Services.AddScoped<ContextTokenService>();
builder.Services.AddScoped<ContextAuthorizationService>();
builder.Services.AddScoped<IImpersonationRepository, ImpersonationRepository>();
builder.Services.AddScoped<IImpersonationService, ImpersonationService>();
builder.Services.AddScoped<ImpersonationTokenService>();
builder.Services.AddScoped<ImpersonationAuthorizationService>();
builder.Services.AddScoped<IMeuDiaRepository, MeuDiaRepository>();
builder.Services.AddScoped<IMeuDiaService, MeuDiaService>();
builder.Services.AddScoped<IWorkItemRepository, WorkItemRepository>();
builder.Services.AddScoped<IWorkItemService, WorkItemService>();
builder.Services.AddScoped<IOperationRealtimePublisher, OperationRealtimePublisher>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IOperationNotificationService, OperationNotificationService>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddScoped<IAlertRuleService, AlertRuleService>();
builder.Services.AddScoped<IProductivityRepository, ProductivityRepository>();
builder.Services.AddScoped<IProductivityService, ProductivityService>();
builder.Services.AddScoped<IProductivityActionRepository, ProductivityActionRepository>();
builder.Services.AddScoped<IProductivityActionService, ProductivityActionService>();
builder.Services.AddScoped<ISavedViewRepository, SavedViewRepository>();
builder.Services.AddScoped<ISavedViewService, SavedViewService>();

// P2 IA — camada canônica de assistentes (adaptadores Groq/Gemini/DeepSeek).
// BaseAddress com barra final: os adapters usam URIs relativas. O timeout aqui é o teto
// bruto do HttpClient; o tempo real por tarefa vem da linha ai_config (CTS encadeado no gateway).
var aiTimeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Ai:TimeoutSeconds", 30));
Uri AiBaseUrl(string provider, string fallback) =>
    new Uri(builder.Configuration[$"Ai:Providers:{provider}:BaseUrl"] ?? fallback);
builder.Services.AddHttpClient("AiGroq", c =>
{
    c.BaseAddress = AiBaseUrl("Groq", "https://api.groq.com/openai/v1/");
    c.Timeout = aiTimeout;
});
builder.Services.AddHttpClient("AiGemini", c =>
{
    c.BaseAddress = AiBaseUrl("Gemini", "https://generativelanguage.googleapis.com/");
    c.Timeout = aiTimeout;
});
builder.Services.AddHttpClient("AiDeepSeek", c =>
{
    c.BaseAddress = AiBaseUrl("DeepSeek", "https://api.deepseek.com/");
    c.Timeout = aiTimeout;
});
builder.Services.AddSingleton<AiSecretProtector>();
builder.Services.AddSingleton<IAiProviderAdapter, GroqAdapter>();
builder.Services.AddSingleton<IAiProviderAdapter, GeminiAdapter>();
builder.Services.AddSingleton<IAiProviderAdapter, DeepSeekAdapter>();
builder.Services.AddScoped<IAiConfigRepository, AiConfigRepository>();
builder.Services.AddScoped<IAiGateway, AiGateway>();
builder.Services.AddScoped<AiJornadaMeuDia>();
builder.Services.AddScoped<AiJornadaCotacao>();

// B7: reconciliação periódica de respostas presas em ENVIANDO (complemento à recovery de boot;
// pulada no Testing e desligada quando Adm360:TransmissaoRecovery:IntervaloSegundos <= 0).
builder.Services.AddHostedService<Adm360TransmissaoRecoveryHostedService>();

var app = builder.Build();

// P4: concilia respostas presas em ENVIANDO por queda do processo no boot (estado honesto; nunca órfão).
if (!app.Environment.IsEnvironment("Testing") && !string.IsNullOrWhiteSpace(connectionString))
{
    try
    {
        var recuperadas = await Adm360TransmissaoRecovery.ReconciliarEnviandoAncoradosAsync(connectionString);
        if (recuperadas > 0)
            app.Logger.LogWarning("Administrativo 360: {Quantidade} resposta(s) recuperada(s) de ENVIANDO para RESULTADO_DESCONHECIDO no boot.", recuperadas);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning("Administrativo 360: conciliação de ENVIANDO não executada no boot: {Motivo}", ex.Message);
    }
}

var provisionDemo = args.Contains("--provision-demo", StringComparer.OrdinalIgnoreCase);
var resetDemoPasswords = args.Contains("--reset-demo-passwords", StringComparer.OrdinalIgnoreCase);
if (provisionDemo || resetDemoPasswords)
{
    await DevelopmentSeed.RunAsync(app.Services, resetDemoPasswords);
    return;
}

if (app.Environment.IsDevelopment()
    && app.Configuration.GetValue("DemoSeed:Enabled", false)
    && app.Configuration.GetValue("DemoSeed:AutoProvisionIfEmpty", false))
{
    await DevelopmentSeed.RunIfEmptyAsync(app.Services);
}

app.UseHttpLogging();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));

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

// Correlação global: uma única ID por requisição (reaproveita a enviada pelo cliente quando válida),
// exposta na resposta e propagada como escopo estruturado para todos os logs do pipeline.
app.Use(async (ctx, next) =>
{
    var supplied = ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    var correlationId = !string.IsNullOrWhiteSpace(supplied) && supplied.Length <= 128
        ? supplied!
        : Guid.NewGuid().ToString("N");
    ctx.Items["CorrelationId"] = correlationId;
    ctx.Response.Headers["X-Correlation-ID"] = correlationId;
    var correlationLogger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PlantaoPro.Api");
    using (correlationLogger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
    {
        await next(ctx);
    }
});

app.UseExceptionHandler(a => a.Run(async ctx =>
{
    var correlationId = ctx.Items.TryGetValue("CorrelationId", out var stored) && stored is string storedValue && !string.IsNullOrWhiteSpace(storedValue)
        ? storedValue
        : Guid.NewGuid().ToString("N");
    var error = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    if (!ctx.Response.Headers.ContainsKey("X-Correlation-ID"))
    {
        ctx.Response.Headers["X-Correlation-ID"] = correlationId;
    }
    var exceptionLogger = ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("PlantaoPro.Api.GlobalExceptionHandler");
    using (exceptionLogger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
    {
        exceptionLogger.LogError(error, "Falha técnica ao processar a solicitação CorrelationId={CorrelationId} Method={Method} Endpoint={Endpoint}", correlationId, ctx.Request.Method, ctx.Request.Path.Value ?? "/");
    }
    if (!ctx.Response.HasStarted)
    {
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(ApiResponse<string>.Fail("Erro interno ao processar a solicitação.", 500));
    }
}));

app.UseAuthentication();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHub<OperacaoHub>("/hubs/operacao");
app.MapHub<FilaHub>("/hubs/fila");
app.MapHub<NotificacoesHub>("/hubs/notificacoes");
app.MapHub<EscalasHub>("/hubs/escalas");
ApiRouteStartupValidator.Validate(app);
app.Run();

public partial class Program { }
