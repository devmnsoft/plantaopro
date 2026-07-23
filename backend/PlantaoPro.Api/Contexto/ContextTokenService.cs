using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using PlantaoPro.CrossCutting.Security;

namespace PlantaoPro.Api;

public sealed class ContextTokenService
{
    private readonly IConfiguration _cfg;
    public ContextTokenService(IConfiguration cfg) => _cfg = cfg;
    public string ModeTenant => "TENANT";
    public string ModeGlobal => "GLOBAL";
    public string Emitir(ClaimsPrincipal user, Guid? tenantId, Guid? clienteId, Guid? contextId, string mode, string scope)
    {
        var jwt = _cfg.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurada.");
        var now = DateTime.UtcNow;
        var claims = user.Claims.Where(c => c.Type is not ("tenant_id" or "cliente_id" or "tenant_context_id" or "context_mode" or "access_scope" or "session_id")).ToList();
        if (tenantId.HasValue) claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
        if (clienteId.HasValue) claims.Add(new Claim("cliente_id", clienteId.Value.ToString()));
        if (contextId.HasValue) claims.Add(new Claim("tenant_context_id", contextId.Value.ToString()));
        claims.Add(new Claim("context_mode", mode));
        claims.Add(new Claim("access_scope", scope));
        claims.Add(new Claim("session_id", Guid.NewGuid().ToString("N")));
        var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims, now, now.AddHours(8), new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
