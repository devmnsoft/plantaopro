using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace PlantaoPro.Api;
public sealed class ImpersonationTokenService
{
    private readonly IConfiguration _cfg; public ImpersonationTokenService(IConfiguration cfg)=>_cfg=cfg;
    public DateTime ExpiresAt(int? minutes)=>DateTime.UtcNow.AddMinutes(Math.Clamp(minutes??30,1,30));
    public string Emitir(ClaimsPrincipal user, Guid original, Guid target, Guid session, Guid tenant, Guid? cliente, DateTime exp){var jwt=_cfg.GetSection("Jwt"); var key=jwt["Key"]??throw new InvalidOperationException("Jwt:Key não configurada."); var claims=user.Claims.Where(c=>!c.Type.StartsWith("impersonation")&&c.Type is not ("original_user_id" or "impersonated_user_id" or "tenant_id" or "cliente_id")).ToList(); claims.Add(new("impersonation","true")); claims.Add(new("original_user_id",original.ToString())); claims.Add(new("impersonated_user_id",target.ToString())); claims.Add(new("impersonation_session_id",session.ToString())); claims.Add(new("impersonation_expires_at",exp.ToString("O"))); claims.Add(new("tenant_id",tenant.ToString())); if(cliente.HasValue) claims.Add(new("cliente_id",cliente.Value.ToString())); var token=new JwtSecurityToken(jwt["Issuer"],jwt["Audience"],claims,DateTime.UtcNow,exp,new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),SecurityAlgorithms.HmacSha256)); return new JwtSecurityTokenHandler().WriteToken(token);}
}
