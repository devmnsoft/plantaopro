<<<<<<< HEAD
using Microsoft.AspNetCore.Authentication.JwtBearer; using Microsoft.IdentityModel.Tokens; using System.Text; var builder=WebApplication.CreateBuilder(args); builder.Services.AddControllers(); builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen(); var jwt=builder.Configuration.GetSection("Jwt"); builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>{o.TokenValidationParameters=new TokenValidationParameters{ValidateIssuer=true,ValidateAudience=true,ValidateIssuerSigningKey=true,ValidIssuer=jwt["Issuer"],ValidAudience=jwt["Audience"],IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))};}); builder.Services.AddCors(o=>o.AddPolicy("all",p=>p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin())); var app=builder.Build(); app.UseSwagger(); app.UseSwaggerUI(); app.UseCors("all"); app.UseAuthentication(); app.UseAuthorization(); app.MapControllers(); app.Run();
=======
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var jwt=builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(o=>o.TokenValidationParameters=new TokenValidationParameters{
 ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,
 ValidIssuer=jwt["Issuer"],ValidAudience=jwt["Audience"],IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
});
builder.Services.AddScoped<IAuditService,AuditService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MedicoService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<PlantaoService>();
builder.Services.AddScoped<EspecialidadeService>();
builder.Services.AddScoped<HospitalService>();

var app=builder.Build();
app.UseExceptionHandler(a=>a.Run(async ctx=>{ctx.Response.StatusCode=500;ctx.Response.ContentType="application/json";await ctx.Response.WriteAsJsonAsync(ApiResponse<string>.Fail("Erro interno ao processar a solicitação.",500));}));
app.UseSwagger();app.UseSwaggerUI();
app.UseAuthentication();app.UseAuthorization();
app.MapControllers();
app.Run();
>>>>>>> pr-2
