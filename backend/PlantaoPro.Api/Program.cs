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
builder.Services.AddScoped<EspecialidadeService>();
builder.Services.AddScoped<HospitalService>();
builder.Services.AddScoped<EscalaService>();
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

var app = builder.Build();
app.UseHttpLogging();

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    await DevelopmentSeed.RunAsync(app.Services);
    app.UseCors("DevelopmentCors");
}

app.UseExceptionHandler(a => a.Run(async ctx =>
{
    ctx.Response.StatusCode = 500;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsJsonAsync(ApiResponse<string>.Fail("Erro interno ao processar a solicitação.", 500));
}));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
