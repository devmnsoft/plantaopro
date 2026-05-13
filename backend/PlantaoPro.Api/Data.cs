using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PlantaoPro.Api.Data;
public interface IAuditService { Task LogAsync(Guid? userId,string acao,string entidade,Guid? registroId,string descricao,string? valorAnterior=null,string? valorNovo=null,string? ip=null,string? userAgent=null); }
public sealed class AuditService(IConfiguration cfg):IAuditService{
    public async Task LogAsync(Guid? userId,string acao,string entidade,Guid? registroId,string descricao,string? valorAnterior=null,string? valorNovo=null,string? ip=null,string? userAgent=null){
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.ExecuteAsync("insert into plantaopro.auditoria(id,usuario_id,acao,entidade,registro_id,ip,descricao,valor_anterior,valor_novo,user_agent,reg_date,reg_status) values (gen_random_uuid(),@u,@a,@e,@r,@ip,@d,@va,@vn,@ua,now(),'A')",
            new{u=userId,a=acao,e=entidade,r=registroId,ip,d=descricao,va=valorAnterior,vn=valorNovo,ua=userAgent});
    }
}
public sealed class AuthService(IConfiguration cfg,IAuditService audit,ILogger<AuthService> logger){
    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest req,string? ip,string? ua){
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var user = await cn.QueryFirstOrDefaultAsync("select id,nome,email,senha_hash,reg_status from plantaopro.usuarios where email=@email",new{email=req.Email});
        if(user is null || (string)user.reg_status!="A" || !BCrypt.Net.BCrypt.Verify(req.Senha,(string)user.senha_hash)){
            logger.LogWarning("Login inválido para {Email}", req.Email);
            await audit.LogAsync(null,"LOGIN_FALHA","usuarios",null,$"Falha login: {req.Email}",ip:ip,userAgent:ua);
            return ApiResponse<LoginResponse>.Fail("Credenciais inválidas.",401);
        }
        var roles=(await cn.QueryAsync<string>("select p.nome from plantaopro.perfis p join plantaopro.usuarios_perfis up on up.perfil_id=p.id where up.usuario_id=@id and up.reg_status='A'",new{id=(Guid)user.id})).ToArray();
        var token = GenerateToken((Guid)user.id,(string)user.email,roles);
        logger.LogInformation("Login realizado para {Email}", req.Email);
        await audit.LogAsync((Guid)user.id,"LOGIN","usuarios",(Guid)user.id,"Login realizado",ip:ip,userAgent:ua);
        return ApiResponse<LoginResponse>.Ok(new LoginResponse(token,DateTime.UtcNow.AddHours(8),(Guid)user.id,(string)user.nome,roles),"Login realizado com sucesso.");
    }
    string GenerateToken(Guid uid,string email,string[] roles){var jwt=cfg.GetSection("Jwt"); var claims=new List<Claim>{new(JwtRegisteredClaimNames.Sub,uid.ToString()),new(ClaimTypes.Name,email),new("uid",uid.ToString())}; claims.AddRange(roles.Select(r=>new Claim(ClaimTypes.Role,r))); var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)); var creds=new SigningCredentials(key,SecurityAlgorithms.HmacSha256); var tk=new JwtSecurityToken(jwt["Issuer"],jwt["Audience"],claims,expires:DateTime.UtcNow.AddHours(8),signingCredentials:creds); return new JwtSecurityTokenHandler().WriteToken(tk);} }
public sealed class MedicoService(IConfiguration cfg,IAuditService audit,ILogger<MedicoService> logger){
  public async Task<ApiResponse<IEnumerable<MedicoDto>>> ListarAsync(){await using var cn=new NpgsqlConnection(cfg.GetConnectionString("Default")); var data=await cn.QueryAsync<MedicoDto>("select id,nome,cpf,crm,uf_crm as UfCrm,email,telefone,cidade,estado,especialidade_id as EspecialidadeId,reg_status from plantaopro.medicos where reg_status='A' order by nome"); return ApiResponse<IEnumerable<MedicoDto>>.Ok(data,"Médicos carregados.");}
  public async Task<ApiResponse<MedicoDto>> CriarAsync(CreateMedicoRequest req,Guid userId,string? ip,string? ua){await using var cn=new NpgsqlConnection(cfg.GetConnectionString("Default")); await cn.OpenAsync(); await using var tx=await cn.BeginTransactionAsync();
    var exists=await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.medicos where cpf=@cpf or (crm=@crm and uf_crm=@uf)",new{cpf=req.Cpf,crm=req.Crm,uf=req.UfCrm},tx);
    if(exists>0) return ApiResponse<MedicoDto>.Fail("CPF ou CRM já cadastrado.");
    var id=Guid.NewGuid();
    await cn.ExecuteAsync("insert into plantaopro.medicos(id,nome,cpf,crm,uf_crm,email,telefone,cidade,estado,especialidade_id,reg_date,reg_status,created_by) values(@id,@n,@cpf,@crm,@uf,@e,@t,@c,@es,@esp,now(),'A',@u)",new{id,n=req.Nome,cpf=req.Cpf,crm=req.Crm,uf=req.UfCrm,e=req.Email,t=req.Telefone,c=req.Cidade,es=req.Estado,esp=req.EspecialidadeId,u=userId},tx);
    await cn.ExecuteAsync("insert into plantaopro.auditoria(id,usuario_id,acao,entidade,registro_id,descricao,ip,user_agent,reg_date,reg_status) values(gen_random_uuid(),@u,'CREATE','medicos',@id,'Criação médico',@ip,@ua,now(),'A')",new{u=userId,id,ip,ua},tx);
    await tx.CommitAsync(); logger.LogInformation("Médico criado {MedicoId}",id);
    return ApiResponse<MedicoDto>.Ok(new MedicoDto(id,req.Nome,req.Cpf,req.Crm,req.UfCrm,req.Email,req.Telefone,req.Cidade,req.Estado,req.EspecialidadeId,'A'),"Médico cadastrado com sucesso.");
  }
}
