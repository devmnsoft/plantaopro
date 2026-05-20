using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace PlantaoPro.Api.Data
{
    public interface IAuditService
    {
        Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao, string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null);
    }
    public sealed class AuditService : IAuditService
    {
        private readonly IConfiguration cfg; public AuditService(IConfiguration cfg)
        {
            this.cfg = cfg;
        }
        public async Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao, string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync("insert into plantaopro.auditoria(id,usuario_id,acao,entidade,registro_id,ip,descricao,valor_anterior,valor_novo,user_agent,reg_date,reg_status) values (gen_random_uuid(),@u,@a,@e,@r,@ip,@d,@va,@vn,@ua,now(),'A')", new
            {
                u = userId,
                a = acao,
                e = entidade,
                r = registroId,
                ip,
                d = descricao,
                va = valorAnterior,
                vn = valorNovo,
                ua = userAgent
            });
        }
    }
    public sealed class AuthService
    {
        private const int MaxTentativasFalhas = 5;
        private const int BloqueioMinutos = 15;
        private readonly IConfiguration cfg; private readonly IAuditService audit; private readonly ILogger<AuthService> logger; public AuthService(IConfiguration cfg, IAuditService audit, ILogger<AuthService> logger)
        {
            this.cfg = cfg;
            this.audit = audit;
            this.logger = logger;
        }
        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest req, string? ip, string? ua)
        {
            var normalizedEmail = (req.Email ?? string.Empty).Trim().ToLowerInvariant();
            logger.LogInformation("Tentativa de login recebida Email:{Email} IP:{Ip}", normalizedEmail, ip);
            try
            {
                await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
                await GarantirTabelaTentativasAsync(cn);
                var user = await cn.QueryFirstOrDefaultAsync("select id,nome,email,senha_hash,reg_status from plantaopro.usuarios where lower(email)=lower(@Email) limit 1", new
                {
                    Email = normalizedEmail
                });
                if (user is null)
                {
                    await RegistrarTentativaAsync(cn, null, normalizedEmail, ip, ua, false, "USER_NOT_FOUND");
                    logger.LogWarning("Login negado: usuário não encontrado Email:{Email} IP:{Ip}", normalizedEmail, ip);
                    return ApiResponse<LoginResponse>.Fail("E-mail ou senha inválidos.", 401);
                }
                var usuarioId = (Guid)user.id;
                var bloqueioAte = await cn.QueryFirstOrDefaultAsync<DateTime?>(
                    @"select bloqueado_ate from plantaopro.login_tentativas
                      where usuario_id=@usuarioId and sucesso=false and bloqueado_ate is not null
                      order by reg_date desc limit 1", new { usuarioId });
                if (bloqueioAte.HasValue && bloqueioAte.Value > DateTime.UtcNow)
                {
                    var restante = (int)Math.Ceiling((bloqueioAte.Value - DateTime.UtcNow).TotalMinutes);
                    await RegistrarTentativaAsync(cn, usuarioId, normalizedEmail, ip, ua, false, "LOCKED_ACTIVE", bloqueioAte.Value);
                    logger.LogWarning("Login bloqueado temporariamente UsuarioId:{UsuarioId} Ate:{BloqueadoAte}", usuarioId, bloqueioAte.Value);
                    return ApiResponse<LoginResponse>.Fail($"Usuário bloqueado temporariamente. Tente novamente em {Math.Max(restante, 1)} minuto(s).", 423);
                }
                var regStatus = ((string?)user.reg_status) ?? "";
                if (!string.Equals(regStatus, "A", StringComparison.OrdinalIgnoreCase))
                {
                    await RegistrarTentativaAsync(cn, usuarioId, normalizedEmail, ip, ua, false, "USER_INACTIVE");
                    logger.LogWarning("Login negado: usuário inativo UsuarioId:{UsuarioId} Email:{Email} IP:{Ip}", usuarioId, normalizedEmail, ip);
                    return ApiResponse<LoginResponse>.Fail("Usuário inativo. Contate o administrador.", 403);
                }
                var senhaHash = (string?)user.senha_hash;
                if (string.IsNullOrWhiteSpace(senhaHash))
                {
                    await RegistrarTentativaAsync(cn, usuarioId, normalizedEmail, ip, ua, false, "PASSWORD_HASH_EMPTY");
                    logger.LogWarning("Login negado: senha_hash ausente UsuarioId:{UsuarioId} Email:{Email} IP:{Ip}", usuarioId, normalizedEmail, ip);
                    return ApiResponse<LoginResponse>.Fail("E-mail ou senha inválidos.", 401);
                }
                bool senhaValida = false;
                try
                {
                    // Compatibilidade com bases legadas que armazenavam senha em texto plano
                    // (ou em formato não BCrypt). Ao autenticar com sucesso em legado, o hash
                    // é atualizado imediatamente para BCrypt.
                    senhaValida = BCrypt.Net.BCrypt.Verify(req.Senha, senhaHash);
                }
                catch
                {
                    senhaValida = string.Equals(req.Senha, senhaHash, StringComparison.Ordinal);
                    if (senhaValida)
                    {
                        var senhaMigradaHash = BCrypt.Net.BCrypt.HashPassword(req.Senha);
                        await cn.ExecuteAsync("update plantaopro.usuarios set senha_hash=@hash,reg_update=now() where id=@id", new
                        {
                            hash = senhaMigradaHash,
                            id = (Guid)user.id
                        });
                        logger.LogInformation("Senha legada migrada para BCrypt UsuarioId:{UsuarioId} Email:{Email} IP:{Ip}", (Guid)user.id, normalizedEmail, ip);
                    }
                }
                if (!senhaValida)
                {
                    var tentativasFalhas = await cn.QueryFirstAsync<int>(
                        @"select count(*) from plantaopro.login_tentativas
                          where usuario_id=@usuarioId and sucesso=false and reg_date >= now() - interval '24 hours'",
                        new { usuarioId });
                    var proximaTentativa = tentativasFalhas + 1;
                    DateTime? novoBloqueio = proximaTentativa >= MaxTentativasFalhas ? DateTime.UtcNow.AddMinutes(BloqueioMinutos) : null;
                    await RegistrarTentativaAsync(cn, usuarioId, normalizedEmail, ip, ua, false, "INVALID_PASSWORD", novoBloqueio);
                    logger.LogWarning("Login negado: senha inválida UsuarioId:{UsuarioId} Tentativa:{Tentativa} Email:{Email} IP:{Ip}", usuarioId, proximaTentativa, normalizedEmail, ip);
                    if (novoBloqueio.HasValue)
                        return ApiResponse<LoginResponse>.Fail($"Múltiplas tentativas inválidas. Usuário bloqueado por {BloqueioMinutos} minutos.", 423);
                    return ApiResponse<LoginResponse>.Fail($"E-mail ou senha inválidos. Tentativa {proximaTentativa}/{MaxTentativasFalhas}.", 401);
                }
                var roles = (await cn.QueryAsync<string>("select p.nome from plantaopro.perfis p join plantaopro.usuarios_perfis up on up.perfil_id=p.id where up.usuario_id=@id and up.reg_status='A' and p.reg_status='A'", new
                {
                    id = (Guid)user.id
                })).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                if (roles.Length == 0)
                {
                    logger.LogWarning("Login negado: usuário sem perfil UsuarioId:{UsuarioId} Email:{Email} IP:{Ip}", (Guid)user.id, normalizedEmail, ip);
                    return ApiResponse<LoginResponse>.Fail("E-mail ou senha inválidos.", 401);
                }
                var token = GenerateToken((Guid)user.id, (string)user.email, roles);
                await RegistrarTentativaAsync(cn, usuarioId, normalizedEmail, ip, ua, true, "SUCCESS");
                await audit.LogAsync(usuarioId, "LOGIN", "usuarios", usuarioId, "Login", ip: ip, userAgent: ua);
                logger.LogInformation("Login bem-sucedido UsuarioId:{UsuarioId} Email:{Email} Perfis:{Perfis} IP:{Ip}", usuarioId, normalizedEmail, string.Join(',', roles), ip);
                return ApiResponse<LoginResponse>.Ok(new(token, DateTime.UtcNow.AddHours(8), usuarioId, (string)user.nome, roles), "Login realizado com sucesso.");
            }
            catch (NpgsqlException ex) { logger.LogError(ex, "Falha de conexão/operação com banco no login Email:{Email} IP:{Ip}", normalizedEmail, ip); return ApiResponse<LoginResponse>.Fail("Erro interno ao autenticar.", 500); }
            catch (Exception ex) { logger.LogError(ex, "Exceção inesperada no login Email:{Email} IP:{Ip}", normalizedEmail, ip); return ApiResponse<LoginResponse>.Fail("Erro interno ao autenticar.", 500); }
        }
        private static Task GarantirTabelaTentativasAsync(NpgsqlConnection cn) =>
            cn.ExecuteAsync(@"create table if not exists plantaopro.login_tentativas(
                id uuid primary key default gen_random_uuid(),
                usuario_id uuid null,
                email text not null,
                ip text null,
                user_agent text null,
                sucesso boolean not null,
                motivo text not null,
                bloqueado_ate timestamp null,
                reg_date timestamp not null default now()
            )");
        private static Task RegistrarTentativaAsync(NpgsqlConnection cn, Guid? usuarioId, string email, string? ip, string? ua, bool sucesso, string motivo, DateTime? bloqueadoAte = null) =>
            cn.ExecuteAsync(@"insert into plantaopro.login_tentativas(usuario_id,email,ip,user_agent,sucesso,motivo,bloqueado_ate,reg_date)
                values(@usuarioId,@email,@ip,@ua,@sucesso,@motivo,@bloqueadoAte,now())",
                new { usuarioId, email, ip, ua, sucesso, motivo, bloqueadoAte });
        string GenerateToken(Guid uid, string email, string[] roles)
        {
            var jwt = cfg.GetSection("Jwt");
            var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, uid.ToString()), new(ClaimTypes.Name, email), new(ClaimTypes.Email, email), new("uid", uid.ToString()) };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims, expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds));
        }
    }
    public sealed class MedicoService
    {
        private readonly IConfiguration cfg; private readonly IAuditService audit; private readonly ILogger<MedicoService> logger; public MedicoService(IConfiguration cfg, IAuditService audit, ILogger<MedicoService> logger)
        {
            this.cfg = cfg;
            this.audit = audit;
            this.logger = logger;
        }
        public async Task<ApiResponse<IEnumerable<MedicoDto>>> ListarAsync()
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            return ApiResponse<IEnumerable<MedicoDto>>.Ok(await cn.QueryAsync<MedicoDto>("select id,nome,cpf,crm,uf_crm as UfCrm,email,telefone,cidade,estado,especialidade_id as EspecialidadeId,reg_status as RegStatus from plantaopro.medicos where reg_status='A' order by nome"));
        }
        public async Task<ApiResponse<MedicoDto>> CriarAsync(CreateMedicoRequest req, Guid userId, string? ip, string? ua)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var id = Guid.NewGuid();
            await cn.ExecuteAsync("insert into plantaopro.medicos(id,nome,cpf,crm,uf_crm,email,telefone,cidade,estado,especialidade_id,reg_date,reg_status,created_by) values(@id,@n,@cpf,@crm,@uf,@e,@t,@c,@es,@esp,now(),'A',@u)", new
            {
                id,
                n = req.Nome,
                cpf = req.Cpf,
                crm = req.Crm,
                uf = req.UfCrm,
                e = req.Email,
                t = req.Telefone,
                c = req.Cidade,
                es = req.Estado,
                esp = req.EspecialidadeId,
                u = userId
            });
            await audit.LogAsync(userId, "CREATE", "medicos", id, "Criação médico", ip: ip, userAgent: ua);
            logger.LogInformation("Médico {Id}", id);
            return ApiResponse<MedicoDto>.Ok(new(id, req.Nome, req.Cpf, req.Crm, req.UfCrm, req.Email, req.Telefone, req.Cidade, req.Estado, req.EspecialidadeId, "A"));
        }
    }
    public sealed class HospitalService
    {
        private readonly IConfiguration cfg; private readonly IAuditService audit; private readonly ILogger<HospitalService> logger; public HospitalService(IConfiguration cfg, IAuditService audit, ILogger<HospitalService> logger)
        {
            this.cfg = cfg;
            this.audit = audit;
            this.logger = logger;
        }
        public async Task<ApiResponse<IEnumerable<HospitalDto>>> GetAllAsync()
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            return ApiResponse<IEnumerable<HospitalDto>>.Ok(await cn.QueryAsync<HospitalDto>("select id,razao_social as RazaoSocial,nome_fantasia as NomeFantasia,cnpj,telefone,email,endereco,cidade,estado,responsavel,reg_status as RegStatus from plantaopro.hospitais where reg_status='A' order by nome_fantasia"));
        }
        public async Task<ApiResponse<HospitalDto>> GetByIdAsync(Guid id)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var d = await cn.QueryFirstOrDefaultAsync<HospitalDto>("select id,razao_social as RazaoSocial,nome_fantasia as NomeFantasia,cnpj,telefone,email,endereco,cidade,estado,responsavel,reg_status as RegStatus from plantaopro.hospitais where id=@id", new
            {
                id
            });
            return d is null ? ApiResponse<HospitalDto>.Fail("Registro não encontrado.", 404) : ApiResponse<HospitalDto>.Ok(d);
        }
        public async Task<ApiResponse<HospitalDto>> CreateAsync(CreateHospitalRequest r, Guid u, string? ip, string? ua)
        {
            if (string.IsNullOrWhiteSpace(r.Cnpj) || string.IsNullOrWhiteSpace(r.NomeFantasia) || string.IsNullOrWhiteSpace(r.Cidade) || string.IsNullOrWhiteSpace(r.Estado))
                return ApiResponse<HospitalDto>.Fail("Campos obrigatórios inválidos.");
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var id = Guid.NewGuid();
            await cn.ExecuteAsync("insert into plantaopro.hospitais(id,razao_social,nome_fantasia,cnpj,telefone,email,endereco,cidade,estado,responsavel,reg_date,reg_status,created_by) values(@id,@rs,@nf,@cnpj,@t,@e,@en,@c,@es,@r,now(),'A',@u)", new
            {
                id,
                rs = r.RazaoSocial,
                nf = r.NomeFantasia,
                cnpj = r.Cnpj,
                t = r.Telefone,
                e = r.Email,
                en = r.Endereco,
                c = r.Cidade,
                es = r.Estado,
                r = r.Responsavel,
                u
            });
            await audit.LogAsync(u, "CREATE", "hospitais", id, "Criação hospital", ip: ip, userAgent: ua);
            logger.LogInformation("Hospital {Id}", id);
            return (await GetByIdAsync(id));
        }
        public async Task<ApiResponse<string>> UpdateAsync(Guid id, UpdateHospitalRequest r, Guid u, string? ip, string? ua)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var n = await cn.ExecuteAsync("update plantaopro.hospitais set razao_social=@rs,nome_fantasia=@nf,telefone=@t,email=@e,endereco=@en,cidade=@c,estado=@es,responsavel=@r,reg_status=@st,reg_update=now(),updated_by=@u where id=@id", new
            {
                id,
                rs = r.RazaoSocial,
                nf = r.NomeFantasia,
                t = r.Telefone,
                e = r.Email,
                en = r.Endereco,
                c = r.Cidade,
                es = r.Estado,
                r = r.Responsavel,
                st = r.RegStatus,
                u
            });
            if (n == 0)
                return ApiResponse<string>.Fail("Registro não encontrado.", 404);
            await audit.LogAsync(u, "UPDATE", "hospitais", id, "Atualização hospital", ip: ip, userAgent: ua);
            return ApiResponse<string>.Ok("ok", "Hospital atualizado com sucesso.");
        }
        public async Task<ApiResponse<string>> DeleteAsync(Guid id, Guid u, string? ip, string? ua)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync("update plantaopro.hospitais set reg_status='I',reg_update=now(),updated_by=@u where id=@id", new
            {
                id,
                u
            });
            await audit.LogAsync(u, "DELETE", "hospitais", id, "Inativação", ip: ip, userAgent: ua);
            return ApiResponse<string>.Ok("ok", "Hospital inativado com sucesso.");
        }
    }
    public sealed class EspecialidadeService
    {
        private readonly IConfiguration cfg; private readonly IAuditService audit; public EspecialidadeService(IConfiguration cfg, IAuditService audit)
        {
            this.cfg = cfg;
            this.audit = audit;
        }
        public async Task<ApiResponse<IEnumerable<EspecialidadeDto>>> GetAllAsync()
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            return ApiResponse<IEnumerable<EspecialidadeDto>>.Ok(await cn.QueryAsync<EspecialidadeDto>("select id,nome,descricao,reg_status as RegStatus from plantaopro.especialidades where reg_status='A' order by nome"));
        }
        public async Task<ApiResponse<EspecialidadeDto>> CreateAsync(CreateEspecialidadeRequest r, Guid u, string? ip, string? ua)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var id = Guid.NewGuid();
            await cn.ExecuteAsync("insert into plantaopro.especialidades(id,nome,descricao,reg_date,reg_status,created_by) values(@id,@n,@d,now(),'A',@u)", new
            {
                id,
                n = r.Nome,
                d = r.Descricao,
                u
            });
            await audit.LogAsync(u, "CREATE", "especialidades", id, "Criação", ip: ip, userAgent: ua);
            return ApiResponse<EspecialidadeDto>.Ok(new(id, r.Nome, r.Descricao, "A"));
        }
    }
    public sealed class PlantaoService
    {
        private readonly IConfiguration cfg; private readonly IAuditService audit; private readonly ILogger<PlantaoService> logger; public PlantaoService(IConfiguration cfg, IAuditService audit, ILogger<PlantaoService> logger)
        {
            this.cfg = cfg;
            this.audit = audit;
            this.logger = logger;
        }
        public async Task<ApiResponse<PagedResult<PlantaoResumoDto>>> GetAllAsync(PlantaoFilterRequest f)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var where = " where p.reg_status='A' ";
            var dp = new DynamicParameters();
            if (f.HospitalId.HasValue)
            {
                where += " and p.hospital_id=@h";
                dp.Add("h", f.HospitalId);
            }
            if (f.EspecialidadeId.HasValue)
            {
                where += " and p.especialidade_id=@e";
                dp.Add("e", f.EspecialidadeId);
            }
            if (!string.IsNullOrWhiteSpace(f.Status))
            {
                where += " and p.status=@s";
                dp.Add("s", f.Status);
            }
            if (f.DataInicio.HasValue)
            {
                where += " and p.data_inicio>=@di";
                dp.Add("di", f.DataInicio);
            }
            if (f.DataFim.HasValue)
            {
                where += " and p.data_fim<=@df";
                dp.Add("df", f.DataFim);
            }
            if (!string.IsNullOrWhiteSpace(f.Cidade))
            {
                where += " and h.cidade=@c";
                dp.Add("c", f.Cidade);
            }
            if (!string.IsNullOrWhiteSpace(f.Estado))
            {
                where += " and h.estado=@es";
                dp.Add("es", f.Estado);
            }
            var page = Math.Max(1, f.Page);
            var size = Math.Clamp(f.PageSize, 1, 100);
            dp.Add("off", (page - 1) * size);
            dp.Add("lim", size);
            var total = await cn.ExecuteScalarAsync<long>("select count(1) from plantaopro.plantoes p join plantaopro.hospitais h on h.id=p.hospital_id" + where, dp);
            var items = await cn.QueryAsync<PlantaoResumoDto>("select p.id,h.nome_fantasia as HospitalNome,h.cidade as HospitalCidade,h.estado as HospitalEstado,esp.nome as EspecialidadeNome,p.data_inicio as DataInicio,p.data_fim as DataFim,p.valor,p.vagas,p.vagas_disponiveis as VagasDisponiveis,p.tipo,p.status,coalesce(p.observacoes,'') as Observacoes from plantaopro.plantoes p join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades esp on esp.id=p.especialidade_id" + where + " order by p.data_inicio desc limit @lim offset @off", dp);
            return ApiResponse<PagedResult<PlantaoResumoDto>>.Ok(new(items, page, size, total));
        }
        public async Task<ApiResponse<PlantaoDetailsDto>> GetByIdAsync(Guid id)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var d = await cn.QueryFirstOrDefaultAsync<PlantaoDetailsDto>("select p.id,p.hospital_id as HospitalId,p.especialidade_id as EspecialidadeId,h.nome_fantasia as HospitalNome,h.cidade as HospitalCidade,h.estado as HospitalEstado,e.nome as EspecialidadeNome,p.data_inicio as DataInicio,p.data_fim as DataFim,p.valor,p.vagas,p.vagas_disponiveis as VagasDisponiveis,p.tipo,p.status,coalesce(p.observacoes,'') as Observacoes,p.reg_status as RegStatus,p.reg_date as RegDate from plantaopro.plantoes p join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades e on e.id=p.especialidade_id where p.id=@id and p.reg_status='A'", new
            {
                id
            });
            return d is null ? ApiResponse<PlantaoDetailsDto>.Fail("Plantão não encontrado", 404) : ApiResponse<PlantaoDetailsDto>.Ok(d);
        }
        public async Task<ApiResponse<PlantaoDto>> CreateAsync(CreatePlantaoRequest r, Guid u, string? ip, string? ua)
        {
            if (r.DataFim <= r.DataInicio || r.Valor <= 0 || r.Vagas <= 0)
                return ApiResponse<PlantaoDto>.Fail("Dados inválidos");
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var hospitalAtivo = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.hospitais where id=@id and reg_status='A'", new
            {
                id = r.HospitalId
            });
            var espAtiva = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.especialidades where id=@id and reg_status='A'", new
            {
                id = r.EspecialidadeId
            });
            if (hospitalAtivo == 0 || espAtiva == 0)
                return ApiResponse<PlantaoDto>.Fail("Hospital ou especialidade inválidos/inativos");
            var id = Guid.NewGuid();
            await cn.ExecuteAsync("insert into plantaopro.plantoes(id,hospital_id,especialidade_id,data_inicio,data_fim,valor,vagas,vagas_disponiveis,tipo,status,observacoes,reg_date,reg_status,created_by) values(@id,@h,@e,@di,@df,@v,@vg,@vg,@t,'rascunho',@o,now(),'A',@u)", new
            {
                id,
                h = r.HospitalId,
                e = r.EspecialidadeId,
                di = r.DataInicio,
                df = r.DataFim,
                v = r.Valor,
                vg = r.Vagas,
                t = r.Tipo,
                o = r.Observacoes,
                u
            });
            await audit.LogAsync(u, "CREATE", "plantoes", id, "Criação", ip: ip, userAgent: ua);
            logger.LogInformation("Plantão criado {Id}", id);
            return ApiResponse<PlantaoDto>.Ok(new(
       id,
       r.HospitalId,
       r.EspecialidadeId,
       r.DataInicio,
       r.DataFim,
       r.Valor,
       r.Vagas,
       r.Vagas,
       r.Tipo,
       "rascunho",
       r.Observacoes ?? string.Empty
   ));
        }
        public async Task<ApiResponse<string>> UpdateAsync(Guid id, UpdatePlantaoRequest r, Guid u, string? ip, string? ua)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var status = await cn.ExecuteScalarAsync<string>("select status from plantaopro.plantoes where id=@id", new
            {
                id
            });
            if (status is null)
                return ApiResponse<string>.Fail("Plantão não encontrado", 404);
            if (status != "rascunho")
                return ApiResponse<string>.Fail("Somente plantão em rascunho pode ser editado");
            if (r.DataFim <= r.DataInicio || r.Valor <= 0 || r.Vagas <= 0)
                return ApiResponse<string>.Fail("Dados inválidos");
            await cn.ExecuteAsync("update plantaopro.plantoes set hospital_id=@h,especialidade_id=@e,data_inicio=@di,data_fim=@df,valor=@v,vagas=@vg,vagas_disponiveis=@vg,tipo=@t,observacoes=@o,reg_update=now(),updated_by=@u where id=@id", new
            {
                id,
                h = r.HospitalId,
                e = r.EspecialidadeId,
                di = r.DataInicio,
                df = r.DataFim,
                v = r.Valor,
                vg = r.Vagas,
                t = r.Tipo,
                o = r.Observacoes,
                u
            });
            await audit.LogAsync(u, "UPDATE", "plantoes", id, "Edição", ip: ip, userAgent: ua);
            return ApiResponse<string>.Ok("ok", "Plantão atualizado");
        }
        public async Task<ApiResponse<string>> ChangeStatusAsync(Guid id, string novo, string just, Guid u, string? ip, string? ua)
        {
            if (novo == "cancelado" && string.IsNullOrWhiteSpace(just))
                return ApiResponse<string>.Fail("Justificativa obrigatória");
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            var old = await cn.ExecuteScalarAsync<string>("select status from plantaopro.plantoes where id=@id", new
            {
                id
            }, tx);
            if (old is null)
                return ApiResponse<string>.Fail("Plantão não encontrado", 404);
            var valid = (old, novo) switch
            {
                ("rascunho", "aberto") => true,
                ("rascunho", "cancelado") => true,
                ("aberto", "cancelado") => true,
                ("aberto", "reservado") => true,
                ("reservado", "confirmado") => true,
                ("reservado", "aberto") => true,
                ("confirmado", "realizado") => true,
                ("confirmado", "cancelado") => true,
                ("realizado", "pago") => true,
                _ => false
            };
            if (!valid)
                return ApiResponse<string>.Fail($"Transição inválida: {old} -> {novo}");
            await cn.ExecuteAsync("update plantaopro.plantoes set status=@s,reg_update=now(),updated_by=@u where id=@id", new
            {
                id,
                s = novo,
                u
            }, tx);
            await cn.ExecuteAsync("insert into plantaopro.historico_plantao(id,plantao_id,status_anterior,status_novo,justificativa,usuario_id,reg_date) values(gen_random_uuid(),@id,@a,@n,@j,@u,now())", new
            {
                id,
                a = old,
                n = novo,
                j = just,
                u
            }, tx);
            await tx.CommitAsync();
            await audit.LogAsync(u, "STATUS_CHANGE", "plantoes", id, $"{old}->{novo}", ip: ip, userAgent: ua);
            return ApiResponse<string>.Ok("ok", "Status atualizado.");
        }
    }
    public sealed class EscalaService
    {
        private readonly IConfiguration cfg; private readonly IAuditService audit; private readonly NotificacaoService notificacao; private readonly ILogger<EscalaService> logger; public EscalaService(IConfiguration cfg, IAuditService audit, NotificacaoService notificacao, ILogger<EscalaService> logger)
        {
            this.cfg = cfg;
            this.audit = audit;
            this.notificacao = notificacao;
            this.logger = logger;
        }
        private NpgsqlConnection Cn() => new(cfg.GetConnectionString("Default"));
        private async Task<(Guid Id, string C, string U, bool A)> ValidarMedicoAsync(Guid medicoId, NpgsqlConnection cn, NpgsqlTransaction tx)
        {
            var m = await cn.QueryFirstOrDefaultAsync<(Guid, string, string, bool)>("select id,coalesce(crm,''),coalesce(uf_crm,''),reg_status='A' from plantaopro.medicos where id=@id", new
            {
                id = medicoId
            }, tx);
            return m;
        }
        private async Task AddHistoricoAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid escalaId, string? ant, string novo, string? just, Guid u) => await cn.ExecuteAsync("insert into plantaopro.historico_escala(id,escala_id,status_anterior,status_novo,justificativa,usuario_id,reg_date) values(gen_random_uuid(),@escalaId,@ant,@novo,@just,@u,now())", new { escalaId, ant, novo, just, u }, tx);
        public async Task<ApiResponse<PagedResult<EscalaResumoDto>>> ListarAsync(EscalaFilterRequest f)
        {
            await using var cn = Cn();
            var w = " where e.reg_status='A'";
            var dp = new DynamicParameters();
            if (f.MedicoId.HasValue)
            {
                w += " and e.medico_id=@m";
                dp.Add("m", f.MedicoId);
            }
            if (f.PlantaoId.HasValue)
            {
                w += " and e.plantao_id=@p";
                dp.Add("p", f.PlantaoId);
            }
            if (!string.IsNullOrWhiteSpace(f.Status))
            {
                w += " and e.status=@s";
                dp.Add("s", f.Status);
            }
            if (f.DataInicio.HasValue)
            {
                w += " and pl.data_inicio>=@di";
                dp.Add("di", f.DataInicio);
            }
            if (f.DataFim.HasValue)
            {
                w += " and pl.data_fim<=@df";
                dp.Add("df", f.DataFim);
            }
            if (f.HospitalId.HasValue)
            {
                w += " and pl.hospital_id=@h";
                dp.Add("h", f.HospitalId);
            }
            if (f.EspecialidadeId.HasValue)
            {
                w += " and pl.especialidade_id=@esp";
                dp.Add("esp", f.EspecialidadeId);
            }
            var pg = Math.Max(1, f.Page);
            var ps = Math.Clamp(f.PageSize, 1, 100);
            dp.Add("off", (pg - 1) * ps);
            dp.Add("lim", ps);
            var total = await cn.ExecuteScalarAsync<long>("select count(1) from plantaopro.escalas e join plantaopro.plantoes pl on pl.id=e.plantao_id" + w, dp);
            var items = await cn.QueryAsync<EscalaResumoDto>("select e.id,e.plantao_id as PlantaoId,e.medico_id as MedicoId,m.nome as MedicoNome,m.crm as MedicoCrm,m.uf_crm as MedicoUfCrm,h.nome_fantasia as HospitalNome,esp.nome as EspecialidadeNome,pl.data_inicio as DataInicio,pl.data_fim as DataFim,pl.valor,pl.tipo as TipoPlantao,e.status,e.justificativa,e.reg_date as RegDate from plantaopro.escalas e join plantaopro.medicos m on m.id=e.medico_id join plantaopro.plantoes pl on pl.id=e.plantao_id join plantaopro.hospitais h on h.id=pl.hospital_id join plantaopro.especialidades esp on esp.id=pl.especialidade_id" + w + " order by e.reg_date desc limit @lim offset @off", dp);
            return ApiResponse<PagedResult<EscalaResumoDto>>.Ok(new(items, pg, ps, total));
        }
        public async Task<ApiResponse<EscalaDto>> GetByIdAsync(Guid id)
        {
            await using var cn = Cn();
            var d = await cn.QueryFirstOrDefaultAsync<EscalaDto>("select id,plantao_id as PlantaoId,medico_id as MedicoId,status,justificativa from plantaopro.escalas where id=@id and reg_status='A'", new
            {
                id
            });
            return d is null ? ApiResponse<EscalaDto>.Fail("Escala não encontrada", 404) : ApiResponse<EscalaDto>.Ok(d);
        }
        public async Task<ApiResponse<PagedResult<PlantaoDto>>> ListarPorMedicoUsuarioAsync(Guid uid, int page, int pageSize)
        {
            await using var cn = Cn();
            var med = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.medicos where usuario_id=@uid and reg_status='A'", new
            {
                uid
            });
            if (!med.HasValue)
                return ApiResponse<PagedResult<PlantaoDto>>.Fail("Médico não encontrado", 404);
            var p = Math.Max(1, page);
            var s = Math.Clamp(pageSize, 1, 100);
            var total = await cn.ExecuteScalarAsync<long>("select count(1) from plantaopro.escalas e where e.medico_id=@m and e.reg_status='A'", new
            {
                m = med
            });
            var items = await cn.QueryAsync<PlantaoDto>("select pl.id,pl.hospital_id as HospitalId,pl.especialidade_id as EspecialidadeId,pl.data_inicio as DataInicio,pl.data_fim as DataFim,pl.valor,pl.vagas,pl.vagas_disponiveis as VagasDisponiveis,pl.tipo,pl.status,coalesce(pl.observacoes,'') as Observacoes from plantaopro.escalas e join plantaopro.plantoes pl on pl.id=e.plantao_id where e.medico_id=@m and e.reg_status='A' order by pl.data_inicio desc limit @lim offset @off", new
            {
                m = med,
                lim = s,
                off = (p - 1) * s
            });
            return ApiResponse<PagedResult<PlantaoDto>>.Ok(new(items, p, s, total));
        }
        public async Task<ApiResponse<string>> AceitarAsync(Guid plantaoId, Guid medicoId, Guid userId, string? ip, string? ua)
        {
            await using var cn = Cn();
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                var med = await ValidarMedicoAsync(medicoId, cn, tx);
                if (med.Id == Guid.Empty || !med.A || string.IsNullOrWhiteSpace(med.C) || string.IsNullOrWhiteSpace(med.U))
                    return ApiResponse<string>.Fail("Médico inválido para aceite");
                var p = await cn.QueryFirstOrDefaultAsync<(Guid Id, string Status, int Vagas, DateTime Di, DateTime Df)>("select id,status,vagas_disponiveis,data_inicio,data_fim from plantaopro.plantoes where id=@id and reg_status='A'", new
                {
                    id = plantaoId
                }, tx);
                if (p.Id == Guid.Empty)
                    return ApiResponse<string>.Fail("Plantão não encontrado", 404);
                if (p.Status != "aberto" || p.Vagas <= 0)
                    return ApiResponse<string>.Fail("Plantão indisponível para aceite");
                var dup = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.escalas where plantao_id=@p and medico_id=@m and status in ('solicitado','confirmado','realizado') and reg_status='A'", new
                {
                    p = plantaoId,
                    m = medicoId
                }, tx);
                if (dup > 0)
                    return ApiResponse<string>.Fail("Médico já possui escala nesse plantão");
                var conflito = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.escalas e join plantaopro.plantoes pl on pl.id=e.plantao_id where e.medico_id=@m and e.status in ('solicitado','confirmado','realizado') and e.reg_status='A' and tsrange(pl.data_inicio,pl.data_fim,'[]') && tsrange(@di,@df,'[]')", new
                {
                    m = medicoId,
                    di = p.Di,
                    df = p.Df
                }, tx);
                if (conflito > 0)
                    return ApiResponse<string>.Fail("Conflito de horário para médico");
                var escalaId = Guid.NewGuid();
                await cn.ExecuteAsync("insert into plantaopro.escalas(id,plantao_id,medico_id,status,justificativa,created_by,reg_status,reg_date) values(@id,@p,@m,'solicitado',null,@u,'A',now())", new
                {
                    id = escalaId,
                    p = plantaoId,
                    m = medicoId,
                    u = userId
                }, tx);
                await cn.ExecuteAsync("update plantaopro.plantoes set vagas_disponiveis=vagas_disponiveis-1,status=case when vagas_disponiveis-1<=0 then 'reservado' else status end,updated_by=@u,reg_update=now() where id=@id", new
                {
                    id = plantaoId,
                    u = userId
                }, tx);
                await AddHistoricoAsync(cn, tx, escalaId, null, "solicitado", "Aceite de plantão", userId);
                await notificacao.CriarNotificacaoAsync(userId, "Aceite de plantão", "Aceite registrado com sucesso", "escala", tx);
                await audit.LogAsync(userId, "ACEITAR", "escalas", escalaId, "Aceite de plantão", ip: ip, userAgent: ua);
                await tx.CommitAsync();
                logger.LogInformation("Escala {EscalaId} aceita", escalaId);
                return ApiResponse<string>.Ok("ok", "Plantão aceito.");
            }
            catch (Exception ex) { await tx.RollbackAsync(); logger.LogError(ex, "Erro ao aceitar plantão {PlantaoId}", plantaoId); return ApiResponse<string>.Fail("Erro ao aceitar plantão", 500); }
        }
        public async Task<ApiResponse<string>> AlterarStatusAsync(Guid id, string novo, string? justificativa, Guid userId, Guid? novoMedicoId, string? ip, string? ua)
        {
            await using var cn = Cn();
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                var e = await cn.QueryFirstOrDefaultAsync<(Guid Id, Guid PlantaoId, Guid MedicoId, string Status)>("select id,plantao_id,medico_id,status from plantaopro.escalas where id=@id and reg_status='A'", new
                {
                    id
                }, tx);
                if (e.Id == Guid.Empty)
                    return ApiResponse<string>.Fail("Escala não encontrada", 404);
                if (novo == "confirmado")
                {
                    if (e.Status != "solicitado")
                        return ApiResponse<string>.Fail("Somente escala solicitada pode ser confirmada");
                    await cn.ExecuteAsync("update plantaopro.escalas set status='confirmado',updated_by=@u,reg_update=now() where id=@id", new
                    {
                        id,
                        u = userId
                    }, tx);
                    await cn.ExecuteAsync("update plantaopro.plantoes set status='confirmado',updated_by=@u,reg_update=now() where id=@p", new
                    {
                        p = e.PlantaoId,
                        u = userId
                    }, tx);
                    await AddHistoricoAsync(cn, tx, id, e.Status, "confirmado", justificativa, userId);
                    await notificacao.CriarNotificacaoAsync(userId, "Escala confirmada", "Sua escala foi confirmada", "escala", tx);
                }
                else if (novo == "recusado")
                {
                    if (e.Status != "solicitado" || string.IsNullOrWhiteSpace(justificativa))
                        return ApiResponse<string>.Fail("Recusa exige justificativa e status solicitado");
                    await cn.ExecuteAsync("update plantaopro.escalas set status='recusado',justificativa=@j,updated_by=@u,reg_update=now() where id=@id", new
                    {
                        id,
                        j = justificativa,
                        u = userId
                    }, tx);
                    await cn.ExecuteAsync("update plantaopro.plantoes set vagas_disponiveis=vagas_disponiveis+1,status=case when status='reservado' then 'aberto' else status end,updated_by=@u,reg_update=now() where id=@p", new
                    {
                        p = e.PlantaoId,
                        u = userId
                    }, tx);
                    await AddHistoricoAsync(cn, tx, id, e.Status, "recusado", justificativa, userId);
                    await notificacao.CriarNotificacaoAsync(userId, "Escala recusada", justificativa!, "escala", tx);
                }
                else if (novo == "cancelado")
                {
                    if (string.IsNullOrWhiteSpace(justificativa) || !(e.Status == "solicitado" || e.Status == "confirmado"))
                        return ApiResponse<string>.Fail("Cancelamento inválido");
                    await cn.ExecuteAsync("update plantaopro.escalas set status='cancelado',justificativa=@j,updated_by=@u,reg_update=now() where id=@id", new
                    {
                        id,
                        j = justificativa,
                        u = userId
                    }, tx);
                    await cn.ExecuteAsync("update plantaopro.plantoes set vagas_disponiveis=vagas_disponiveis+1,status=case when status in ('reservado','confirmado') then 'aberto' else status end,updated_by=@u,reg_update=now() where id=@p", new
                    {
                        p = e.PlantaoId,
                        u = userId
                    }, tx);
                    await AddHistoricoAsync(cn, tx, id, e.Status, "cancelado", justificativa, userId);
                    await notificacao.CriarNotificacaoAsync(userId, "Escala cancelada", justificativa!, "escala", tx);
                }
                else if (novo == "substituido")
                {
                    if (!novoMedicoId.HasValue || string.IsNullOrWhiteSpace(justificativa))
                        return ApiResponse<string>.Fail("Substituição exige novo médico e justificativa");
                    var med = await ValidarMedicoAsync(novoMedicoId.Value, cn, tx);
                    if (med.Id == Guid.Empty || !med.A || string.IsNullOrWhiteSpace(med.C) || string.IsNullOrWhiteSpace(med.U))
                        return ApiResponse<string>.Fail("Novo médico inválido");
                    var pl = await cn.QueryFirstAsync<(DateTime Di, DateTime Df)>("select data_inicio,data_fim from plantaopro.plantoes where id=@id", new
                    {
                        id = e.PlantaoId
                    }, tx);
                    var conflito = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.escalas e join plantaopro.plantoes pl on pl.id=e.plantao_id where e.medico_id=@m and e.status in ('solicitado','confirmado','realizado') and e.reg_status='A' and tsrange(pl.data_inicio,pl.data_fim,'[]') && tsrange(@di,@df,'[]')", new
                    {
                        m = novoMedicoId,
                        di = pl.Di,
                        df = pl.Df
                    }, tx);
                    if (conflito > 0)
                        return ApiResponse<string>.Fail("Novo médico com conflito de horário");
                    await cn.ExecuteAsync("update plantaopro.escalas set status='substituido',justificativa=@j,updated_by=@u,reg_update=now() where id=@id", new
                    {
                        id,
                        j = justificativa,
                        u = userId
                    }, tx);
                    var nova = Guid.NewGuid();
                    await cn.ExecuteAsync("insert into plantaopro.escalas(id,plantao_id,medico_id,status,justificativa,created_by,reg_status,reg_date) values(@id,@p,@m,'confirmado',@j,@u,'A',now())", new
                    {
                        id = nova,
                        p = e.PlantaoId,
                        m = novoMedicoId,
                        j = justificativa,
                        u = userId
                    }, tx);
                    await AddHistoricoAsync(cn, tx, id, e.Status, "substituido", justificativa, userId);
                    await AddHistoricoAsync(cn, tx, nova, null, "confirmado", "Escala criada por substituição", userId);
                    await notificacao.CriarNotificacaoAsync(userId, "Escala substituída", justificativa!, "escala", tx);
                }
                else if (novo == "realizado")
                {
                    if (e.Status != "confirmado")
                        return ApiResponse<string>.Fail("Somente escala confirmada pode ser realizada");
                    await cn.ExecuteAsync("update plantaopro.escalas set status='realizado',updated_by=@u,reg_update=now() where id=@id", new
                    {
                        id,
                        u = userId
                    }, tx);
                    await cn.ExecuteAsync("update plantaopro.plantoes set status='realizado',updated_by=@u,reg_update=now() where id=@p", new
                    {
                        p = e.PlantaoId,
                        u = userId
                    }, tx);
                    await AddHistoricoAsync(cn, tx, id, e.Status, "realizado", justificativa, userId);
                    await notificacao.CriarNotificacaoAsync(userId, "Escala realizada", "Escala marcada como realizada", "escala", tx);
                }
                else
                    return ApiResponse<string>.Fail("Status não suportado");
                await audit.LogAsync(userId, "STATUS_CHANGE", "escalas", id, $"{e.Status}->{novo}", ip: ip, userAgent: ua);
                await tx.CommitAsync();
                logger.LogInformation("Escala {EscalaId} status {Status}", id, novo);
                return ApiResponse<string>.Ok("ok", "Status atualizado");
            }
            catch (Exception ex) { await tx.RollbackAsync(); logger.LogError(ex, "Erro status escala {EscalaId}", id); return ApiResponse<string>.Fail("Erro ao alterar status da escala", 500); }
        }
    }

    public sealed class FinanceiroService
    {
        private readonly IConfiguration cfg; private readonly IAuditService audit; private readonly NotificacaoService notificacao; private readonly ILogger<FinanceiroService> logger; public FinanceiroService(IConfiguration cfg, IAuditService audit, NotificacaoService notificacao, ILogger<FinanceiroService> logger)
        {
            this.cfg = cfg;
            this.audit = audit;
            this.notificacao = notificacao;
            this.logger = logger;
        }
        private NpgsqlConnection Cn() => new(cfg.GetConnectionString("Default"));
        private async Task AddHistoricoAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid pagId, string? ant, string novo, string? just, Guid u) => await cn.ExecuteAsync("insert into plantaopro.historico_pagamento(id,pagamento_id,status_anterior,status_novo,justificativa,usuario_id,reg_date) values(gen_random_uuid(),@pagId,@ant,@novo,@just,@u,now())", new { pagId, ant, novo, just, u }, tx);

        public async Task<ApiResponse<PagedResult<PagamentoResumoDto>>> ListarAsync(PagamentoFilterRequest f)
        {
            await using var cn = Cn();
            var w = " where pg.reg_status='A'";
            var dp = new DynamicParameters();
            if (f.MedicoId.HasValue)
            {
                w += " and pg.medico_id=@m";
                dp.Add("m", f.MedicoId);
            }
            if (f.HospitalId.HasValue)
            {
                w += " and pl.hospital_id=@h";
                dp.Add("h", f.HospitalId);
            }
            if (f.EspecialidadeId.HasValue)
            {
                w += " and pl.especialidade_id=@e";
                dp.Add("e", f.EspecialidadeId);
            }
            if (!string.IsNullOrWhiteSpace(f.Status))
            {
                w += " and lower(pg.status)=lower(@s)";
                dp.Add("s", f.Status);
            }
            if (f.DataInicio.HasValue)
            {
                w += " and pl.data_inicio>=@di";
                dp.Add("di", f.DataInicio);
            }
            if (f.DataFim.HasValue)
            {
                w += " and pl.data_fim<=@df";
                dp.Add("df", f.DataFim);
            }
            var p = Math.Max(1, f.Page);
            var s = Math.Clamp(f.PageSize, 1, 100);
            dp.Add("off", (p - 1) * s);
            dp.Add("lim", s);
            var total = await cn.ExecuteScalarAsync<long>("select count(1) from plantaopro.pagamentos pg join plantaopro.plantoes pl on pl.id=pg.plantao_id" + w, dp);
            var items = await cn.QueryAsync<PagamentoResumoDto>("select pg.id,pg.escala_id as EscalaId,pg.medico_id as MedicoId,pg.plantao_id as PlantaoId,m.nome as MedicoNome,m.crm as MedicoCrm,h.nome_fantasia as HospitalNome,esp.nome as EspecialidadeNome,pl.data_inicio as DataPlantao,pg.valor_previsto as ValorPrevisto,pg.valor_pago as ValorPago,pg.status,pg.data_prevista as DataPrevista,pg.data_pagamento as DataPagamento,pg.forma_pagamento as FormaPagamento,null::text as ChavePix,pg.observacoes from plantaopro.pagamentos pg join plantaopro.plantoes pl on pl.id=pg.plantao_id join plantaopro.medicos m on m.id=pg.medico_id join plantaopro.hospitais h on h.id=pl.hospital_id join plantaopro.especialidades esp on esp.id=pl.especialidade_id" + w + " order by pg.reg_date desc limit @lim offset @off", dp);
            return ApiResponse<PagedResult<PagamentoResumoDto>>.Ok(new(items, p, s, total));
        }
        public async Task<ApiResponse<PagamentoDetailsDto>> GetByIdAsync(Guid id)
        {
            await using var cn = Cn();
            var d = await cn.QueryFirstOrDefaultAsync<PagamentoDetailsDto>("select pg.id,pg.escala_id as EscalaId,pg.medico_id as MedicoId,pg.plantao_id as PlantaoId,m.nome as MedicoNome,m.crm as MedicoCrm,m.uf_crm as MedicoUfCrm,m.email as MedicoEmail,m.telefone as MedicoTelefone,h.nome_fantasia as HospitalNome,h.cidade as HospitalCidade,h.estado as HospitalEstado,esp.nome as EspecialidadeNome,pl.data_inicio as DataInicioPlantao,pl.data_fim as DataFimPlantao,pg.valor_previsto as ValorPrevisto,pg.valor_pago as ValorPago,pg.status,pg.data_prevista as DataPrevista,pg.data_pagamento as DataPagamento,pg.forma_pagamento as FormaPagamento,null::text as ChavePix,pg.observacoes,pg.reg_date as RegDate from plantaopro.pagamentos pg join plantaopro.plantoes pl on pl.id=pg.plantao_id join plantaopro.medicos m on m.id=pg.medico_id join plantaopro.hospitais h on h.id=pl.hospital_id join plantaopro.especialidades esp on esp.id=pl.especialidade_id where pg.id=@id and pg.reg_status='A'", new
            {
                id
            });
            return d is null ? ApiResponse<PagamentoDetailsDto>.Fail("Pagamento não encontrado", 404) : ApiResponse<PagamentoDetailsDto>.Ok(d);
        }
        public async Task<ApiResponse<Guid>> GerarAsync(GerarPagamentoRequest req, Guid userId, string? ip, string? ua)
        {
            await using var cn = Cn();
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                var ex = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.pagamentos where escala_id=@e and reg_status='A'", new
                {
                    e = req.EscalaId
                }, tx);
                if (ex > 0)
                    return ApiResponse<Guid>.Fail("Pagamento já gerado para a escala");
                var row = await cn.QueryFirstOrDefaultAsync<(Guid EscalaId, Guid MedicoId, Guid PlantaoId, decimal Valor, string Status, Guid UsuarioId)>("select e.id,e.medico_id,e.plantao_id,p.valor,e.status,m.usuario_id from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id join plantaopro.medicos m on m.id=e.medico_id where e.id=@id and e.reg_status='A'", new
                {
                    id = req.EscalaId
                }, tx);
                if (row.EscalaId == Guid.Empty || row.Status != "realizado")
                    return ApiResponse<Guid>.Fail("Somente escala realizada pode gerar pagamento");
                var id = Guid.NewGuid();
                var dataPrevista = req.DataPrevista ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
                await cn.ExecuteAsync("insert into plantaopro.pagamentos(id,escala_id,medico_id,plantao_id,valor_previsto,valor_pago,status,data_prevista,observacoes,reg_date,reg_status,created_by) values(@id,@e,@m,@p,@v,null,'pendente',@d,@o,now(),'A',@u)", new
                {
                    id,
                    e = row.EscalaId,
                    m = row.MedicoId,
                    p = row.PlantaoId,
                    v = row.Valor,
                    d = dataPrevista,
                    o = req.Observacoes,
                    u = userId
                }, tx);
                await AddHistoricoAsync(cn, tx, id, null, "pendente", req.Observacoes ?? "Pagamento gerado", userId);
                await notificacao.CriarNotificacaoAsync(row.UsuarioId, "Pagamento gerado", "Seu pagamento foi gerado e está pendente.", "financeiro", tx);
                await audit.LogAsync(userId, "CREATE", "pagamentos", id, "Pagamento gerado", ip: ip, userAgent: ua);
                await tx.CommitAsync();
                return ApiResponse<Guid>.Ok(id, "Pagamento gerado");
            }
            catch (Exception ex) { await tx.RollbackAsync(); logger.LogError(ex, "Erro gerar pagamento"); return ApiResponse<Guid>.Fail("Erro ao gerar pagamento", 500); }
        }
        public async Task<ApiResponse<string>> ConfirmarAsync(Guid id, ConfirmarPagamentoRequest req, Guid userId, string? ip, string? ua)
        {
            if (req.ValorPago <= 0 || string.IsNullOrWhiteSpace(req.FormaPagamento))
                return ApiResponse<string>.Fail("Dados inválidos para confirmação");
            await using var cn = Cn();
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                var pg = await cn.QueryFirstOrDefaultAsync<(string Status, Guid UsuarioId)>("select pg.status,m.usuario_id from plantaopro.pagamentos pg join plantaopro.medicos m on m.id=pg.medico_id where pg.id=@id and pg.reg_status='A'", new
                {
                    id
                }, tx);
                if (pg.Status is null)
                    return ApiResponse<string>.Fail("Pagamento não encontrado", 404);
                if (pg.Status != "pendente")
                    return ApiResponse<string>.Fail("Somente pagamento pendente pode ser confirmado");
                await cn.ExecuteAsync("update plantaopro.pagamentos set status='pago',valor_pago=@v,forma_pagamento=@f,data_pagamento=@d,observacoes=@o,updated_by=@u,reg_update=now() where id=@id", new
                {
                    id,
                    v = req.ValorPago,
                    f = req.FormaPagamento,
                    d = req.DataPagamento,
                    o = req.Observacoes,
                    u = userId
                }, tx);
                await AddHistoricoAsync(cn, tx, id, pg.Status, "pago", req.Observacoes ?? "Pagamento confirmado", userId);
                await notificacao.CriarNotificacaoAsync(pg.UsuarioId, "Pagamento confirmado", "Seu pagamento foi confirmado.", "financeiro", tx);
                await audit.LogAsync(userId, "STATUS_CHANGE", "pagamentos", id, $"{pg.Status}->pago", ip: ip, userAgent: ua);
                await tx.CommitAsync();
                return ApiResponse<string>.Ok("ok", "Pagamento confirmado");
            }
            catch (Exception ex) { await tx.RollbackAsync(); logger.LogError(ex, "Erro confirmar pagamento"); return ApiResponse<string>.Fail("Erro ao confirmar pagamento", 500); }
        }
        public async Task<ApiResponse<string>> CancelarAsync(Guid id, string justificativa, Guid userId, string? ip, string? ua)
        {
            if (string.IsNullOrWhiteSpace(justificativa))
                return ApiResponse<string>.Fail("Justificativa obrigatória");
            await using var cn = Cn();
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            try
            {
                var pg = await cn.QueryFirstOrDefaultAsync<(string Status, Guid UsuarioId)>("select pg.status,m.usuario_id from plantaopro.pagamentos pg join plantaopro.medicos m on m.id=pg.medico_id where pg.id=@id and pg.reg_status='A'", new
                {
                    id
                }, tx);
                if (pg.Status is null)
                    return ApiResponse<string>.Fail("Pagamento não encontrado", 404);
                if (pg.Status != "pendente")
                    return ApiResponse<string>.Fail("Somente pagamento pendente pode ser cancelado");
                await cn.ExecuteAsync("update plantaopro.pagamentos set status='cancelado',observacoes=@j,updated_by=@u,reg_update=now() where id=@id", new
                {
                    id,
                    j = justificativa,
                    u = userId
                }, tx);
                await AddHistoricoAsync(cn, tx, id, pg.Status, "cancelado", justificativa, userId);
                await notificacao.CriarNotificacaoAsync(pg.UsuarioId, "Pagamento cancelado", justificativa, "financeiro", tx);
                await audit.LogAsync(userId, "STATUS_CHANGE", "pagamentos", id, $"{pg.Status}->cancelado", ip: ip, userAgent: ua);
                await tx.CommitAsync();
                return ApiResponse<string>.Ok("ok", "Pagamento cancelado");
            }
            catch (Exception ex) { await tx.RollbackAsync(); logger.LogError(ex, "Erro cancelar pagamento"); return ApiResponse<string>.Fail("Erro ao cancelar pagamento", 500); }
        }

        public async Task<ApiResponse<PagedResult<PagamentoResumoDto>>> MeusAsync(
    Guid usuarioId,
    int page,
    int pageSize)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));

            var medicoId = await cn.ExecuteScalarAsync<Guid?>(@"
        select id
        from plantaopro.medicos
        where usuario_id = @usuarioId
          and reg_status = 'A'
        limit 1;
    ", new
            {
                usuarioId
            });

            if (!medicoId.HasValue)
            {
                return ApiResponse<PagedResult<PagamentoResumoDto>>.Fail(
                    "Médico não encontrado para o usuário autenticado.",
                    404
                );
            }

            var filtro = new PagamentoFilterRequest(
                MedicoId: medicoId.Value,
                HospitalId: null,
                Status: null,
                DataInicio: null,
                DataFim: null,
                EspecialidadeId: null,
                Page: Math.Max(1, page),
                PageSize: Math.Clamp(pageSize, 1, 100)
            );

            return await ListarAsync(filtro);
        }

        public async Task<ApiResponse<PagamentoDetailsDto>> MeuByIdAsync(
            Guid usuarioId,
            Guid pagamentoId)
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));

            var medicoId = await cn.ExecuteScalarAsync<Guid?>(@"
        select id
        from plantaopro.medicos
        where usuario_id = @usuarioId
          and reg_status = 'A'
        limit 1;
    ", new
            {
                usuarioId
            });

            if (!medicoId.HasValue)
            {
                return ApiResponse<PagamentoDetailsDto>.Fail(
                    "Médico não encontrado para o usuário autenticado.",
                    404
                );
            }

            var pagamentoMedicoId = await cn.ExecuteScalarAsync<Guid?>(@"
        select medico_id
        from plantaopro.pagamentos
        where id = @pagamentoId
          and reg_status = 'A'
        limit 1;
    ", new
            {
                pagamentoId
            });

            if (!pagamentoMedicoId.HasValue)
            {
                return ApiResponse<PagamentoDetailsDto>.Fail(
                    "Pagamento não encontrado.",
                    404
                );
            }

            if (pagamentoMedicoId.Value != medicoId.Value)
            {
                return ApiResponse<PagamentoDetailsDto>.Fail(
                    "Acesso negado ao pagamento informado.",
                    403
                );
            }

            return await GetByIdAsync(pagamentoId);
        }

    }

    public sealed class NotificacaoService
    {
        private readonly IConfiguration cfg; private readonly IAuditService audit; private readonly ILogger<NotificacaoService> logger; public NotificacaoService(IConfiguration cfg, IAuditService audit, ILogger<NotificacaoService> logger)
        {
            this.cfg = cfg;
            this.audit = audit;
            this.logger = logger;
        }
        private NpgsqlConnection Cn() => new(cfg.GetConnectionString("Default")); public async Task CriarNotificacaoAsync(Guid usuarioId, string titulo, string mensagem, string tipo, NpgsqlTransaction transaction) => await transaction.Connection!.ExecuteAsync("insert into plantaopro.notificacoes(id,usuario_id,titulo,mensagem,tipo,lida,created_by,reg_status,reg_date) values(gen_random_uuid(),@usuarioId,@titulo,@mensagem,@tipo,false,@usuarioId,'A',now())", new { usuarioId, titulo, mensagem, tipo }, transaction);
        public async Task<ApiResponse<PagedResult<NotificacaoDto>>> ListarAsync(Guid uid, NotificationFilterRequest f)
        {
            await using var cn = Cn();
            var w = " where usuario_id=@uid and reg_status='A'";
            var dp = new DynamicParameters(new
            {
                uid
            });
            if (f.Lida.HasValue)
            {
                w += " and lida=@l";
                dp.Add("l", f.Lida);
            }
            if (!string.IsNullOrWhiteSpace(f.Tipo))
            {
                w += " and tipo=@t";
                dp.Add("t", f.Tipo);
            }
            if (f.DataInicio.HasValue)
            {
                w += " and reg_date>=@di";
                dp.Add("di", f.DataInicio);
            }
            if (f.DataFim.HasValue)
            {
                w += " and reg_date<=@df";
                dp.Add("df", f.DataFim);
            }
            var p = Math.Max(1, f.Page);
            var s = Math.Clamp(f.PageSize, 1, 100);
            dp.Add("off", (p - 1) * s);
            dp.Add("lim", s);
            var total = await cn.ExecuteScalarAsync<long>("select count(1) from plantaopro.notificacoes" + w, dp);
            var items = await cn.QueryAsync<NotificacaoDto>("select id,titulo,mensagem,tipo,lida,reg_date as RegDate from plantaopro.notificacoes" + w + " order by reg_date desc limit @lim offset @off", dp);
            return ApiResponse<PagedResult<NotificacaoDto>>.Ok(new(items, p, s, total));
        }
        public async Task<ApiResponse<string>> MarcarLidaAsync(Guid uid, Guid id, string? ip, string? ua)
        {
            await using var cn = Cn();
            var n = await cn.ExecuteAsync("update plantaopro.notificacoes set lida=true,updated_by=@uid,reg_update=now() where id=@id and usuario_id=@uid and reg_status='A'", new
            {
                uid,
                id
            });
            if (n == 0)
                return ApiResponse<string>.Fail("Notificação não encontrada", 404);
            await audit.LogAsync(uid, "UPDATE", "notificacoes", id, "Marcar notificação como lida", ip: ip, userAgent: ua);
            logger.LogInformation("Notificação {Id} lida", id);
            return ApiResponse<string>.Ok("ok", "Notificação marcada como lida");
        }
        public async Task<ApiResponse<string>> MarcarTodasLidasAsync(Guid uid, string? ip, string? ua)
        {
            await using var cn = Cn();
            await cn.ExecuteAsync("update plantaopro.notificacoes set lida=true,updated_by=@uid,reg_update=now() where usuario_id=@uid and lida=false and reg_status='A'", new
            {
                uid
            });
            await audit.LogAsync(uid, "UPDATE", "notificacoes", null, "Marcar todas notificações como lidas", ip: ip, userAgent: ua);
            logger.LogInformation("Notificações marcadas como lidas para {Uid}", uid);
            return ApiResponse<string>.Ok("ok", "Notificações atualizadas");
        }
    }


    public sealed class MedicoAreaService
    {
        private readonly IConfiguration cfg;
        private readonly IAuditService audit;
        private readonly ILogger<MedicoAreaService> logger;
        public MedicoAreaService(IConfiguration cfg, IAuditService audit, ILogger<MedicoAreaService> logger){this.cfg=cfg;this.audit=audit;this.logger=logger;}
        private NpgsqlConnection Cn()=>new(cfg.GetConnectionString("Default"));

        private async Task<(Guid? Id,string? Nome,string? Crm,string? UfCrm)> GetMedicoAsync(NpgsqlConnection cn, Guid uid)
            => await cn.QueryFirstOrDefaultAsync<(Guid?,string?,string?,string?)>("select id,nome,crm,uf_crm from plantaopro.medicos where usuario_id=@uid and reg_status='A' limit 1", new { uid });

        public async Task<ApiResponse<MedicoAreaResumoDto>> ResumoAsync(Guid uid)
        {
            await using var cn=Cn();
            var med=await GetMedicoAsync(cn,uid);
            if(med.Id is null) return ApiResponse<MedicoAreaResumoDto>.Fail("Médico não encontrado para o usuário autenticado.",404);
            var dto=await cn.QueryFirstAsync<MedicoAreaResumoDto>(@"select @nome as MedicoNome,@crm as Crm,@uf as UfCrm,
            (select count(1) from plantaopro.plantoes p where p.reg_status='A' and p.status='aberto' and p.vagas_disponiveis>0) as PlantoesDisponiveis,
            (select count(1) from plantaopro.escalas e where e.medico_id=@mid and e.status='solicitado' and e.reg_status='A') as SolicitacoesPendentes,
            (select count(1) from plantaopro.escalas e where e.medico_id=@mid and e.status='confirmado' and e.reg_status='A') as EscalasConfirmadas,
            (select count(1) from plantaopro.escalas e where e.medico_id=@mid and e.status='realizado' and e.reg_status='A') as PlantoesRealizados,
            (select count(1) from plantaopro.pagamentos pg where pg.medico_id=@mid and pg.status='pendente' and pg.reg_status='A') as PagamentosPendentes,
            (select coalesce(sum(pg.valor_previsto),0) from plantaopro.pagamentos pg where pg.medico_id=@mid and pg.status='pendente' and pg.reg_status='A') as ValorPendente,
            (select count(1) from plantaopro.notificacoes n where n.usuario_id=@uid and n.lida=false and n.reg_status='A') as NotificacoesNaoLidas",new{uid,mid=med.Id,nome=med.Nome,crm=med.Crm,uf=med.UfCrm});
            return ApiResponse<MedicoAreaResumoDto>.Ok(dto);
        }
        public async Task<ApiResponse<PagedResult<MedicoPlantaoDisponivelDto>>> PlantoesDisponiveisAsync(Guid uid,int page,int pageSize){await using var cn=Cn();var med=await GetMedicoAsync(cn,uid);if(med.Id is null) return ApiResponse<PagedResult<MedicoPlantaoDisponivelDto>>.Fail("Médico não encontrado para o usuário autenticado.",404);var p=Math.Max(1,page);var s=Math.Clamp(pageSize,1,100);var off=(p-1)*s;var total=await cn.ExecuteScalarAsync<long>("select count(1) from plantaopro.plantoes p where p.reg_status='A' and p.status='aberto' and p.vagas_disponiveis>0");var items=await cn.QueryAsync<MedicoPlantaoDisponivelDto>(@"select p.id as PlantaoId,h.nome_fantasia as HospitalNome,h.cidade as HospitalCidade,h.estado as HospitalEstado,esp.nome as EspecialidadeNome,p.data_inicio as DataInicio,p.data_fim as DataFim,p.valor,p.vagas_disponiveis as VagasDisponiveis,p.tipo,p.status,
exists(select 1 from plantaopro.escalas e where e.plantao_id=p.id and e.medico_id=@mid and e.reg_status='A') as JaSolicitado,
exists(select 1 from plantaopro.escalas e2 join plantaopro.plantoes p2 on p2.id=e2.plantao_id where e2.medico_id=@mid and e2.reg_status='A' and e2.status in ('solicitado','confirmado') and (p.data_inicio,p.data_fim) overlaps (p2.data_inicio,p2.data_fim)) as TemConflitoHorario
from plantaopro.plantoes p join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades esp on esp.id=p.especialidade_id where p.reg_status='A' and p.status='aberto' and p.vagas_disponiveis>0 order by p.data_inicio asc limit @s offset @off",new{mid=med.Id,s,off});return ApiResponse<PagedResult<MedicoPlantaoDisponivelDto>>.Ok(new(items,p,s,total));}
        public async Task<ApiResponse<PagedResult<MedicoEscalaDto>>> MinhasEscalasAsync(Guid uid, int page, int pageSize)
        {
            await using var cn = Cn();
            var med = await GetMedicoAsync(cn, uid);
            if (med.Id is null) return ApiResponse<PagedResult<MedicoEscalaDto>>.Fail("Médico não encontrado para o usuário autenticado.", 404);
            var p = Math.Max(1, page);
            var s = Math.Clamp(pageSize, 1, 100);
            var off = (p - 1) * s;
            var total = await cn.ExecuteScalarAsync<long>("select count(1) from plantaopro.escalas e where e.medico_id=@mid and e.reg_status='A'", new { mid = med.Id });
            var items = await cn.QueryAsync<MedicoEscalaDto>(@"select e.id as EscalaId,e.plantao_id as PlantaoId,h.nome_fantasia as HospitalNome,esp.nome as EspecialidadeNome,p.data_inicio as DataInicio,p.data_fim as DataFim,p.valor,e.status,e.justificativa
            from plantaopro.escalas e
            join plantaopro.plantoes p on p.id=e.plantao_id
            join plantaopro.hospitais h on h.id=p.hospital_id
            join plantaopro.especialidades esp on esp.id=p.especialidade_id
            where e.medico_id=@mid and e.reg_status='A'
            order by p.data_inicio desc limit @s offset @off", new { mid = med.Id, s, off });
            return ApiResponse<PagedResult<MedicoEscalaDto>>.Ok(new(items, p, s, total));
        }
        public async Task<ApiResponse<PagedResult<MedicoPagamentoDto>>> MeusPagamentosAsync(Guid uid, int page, int pageSize)
        {
            await using var cn = Cn();
            var med = await GetMedicoAsync(cn, uid);
            if (med.Id is null) return ApiResponse<PagedResult<MedicoPagamentoDto>>.Fail("Médico não encontrado para o usuário autenticado.", 404);
            var p = Math.Max(1, page);
            var s = Math.Clamp(pageSize, 1, 100);
            var off = (p - 1) * s;
            var total = await cn.ExecuteScalarAsync<long>("select count(1) from plantaopro.pagamentos pg where pg.medico_id=@mid and pg.reg_status='A'", new { mid = med.Id });
            var items = await cn.QueryAsync<MedicoPagamentoDto>(@"select pg.id as PagamentoId,h.nome_fantasia as HospitalNome,esp.nome as EspecialidadeNome,p.data_inicio as DataPlantao,pg.valor_previsto as ValorPrevisto,pg.valor_pago as ValorPago,pg.status,pg.data_prevista as DataPrevista,pg.data_pagamento as DataPagamento,pg.forma_pagamento as FormaPagamento
            from plantaopro.pagamentos pg
            join plantaopro.plantoes p on p.id=pg.plantao_id
            join plantaopro.hospitais h on h.id=p.hospital_id
            join plantaopro.especialidades esp on esp.id=p.especialidade_id
            where pg.medico_id=@mid and pg.reg_status='A'
            order by p.data_inicio desc limit @s offset @off", new { mid = med.Id, s, off });
            return ApiResponse<PagedResult<MedicoPagamentoDto>>.Ok(new(items, p, s, total));
        }
    }

    public sealed class DashboardService
    {
        private readonly IConfiguration cfg; public DashboardService(IConfiguration cfg)
        {
            this.cfg = cfg;
        }
        private NpgsqlConnection Cn() => new(cfg.GetConnectionString("Default")); public async Task<ApiResponse<DashboardOverviewDto>> GetAsync(Guid uid)
        {
            await using var cn = Cn();
            var ind = await cn.QueryFirstAsync<DashboardDto>("select (select count(1) from plantaopro.medicos where reg_status='A') as TotalMedicos,(select count(1) from plantaopro.hospitais where reg_status='A') as TotalHospitais,(select count(1) from plantaopro.especialidades where reg_status='A') as TotalEspecialidades,(select count(1) from plantaopro.plantoes where reg_status='A') as TotalPlantoes,(select count(1) from plantaopro.plantoes where status='aberto') as PlantoesAbertos,(select count(1) from plantaopro.plantoes where status='confirmado') as PlantoesConfirmados,(select count(1) from plantaopro.plantoes where status='realizado') as PlantoesRealizados,(select count(1) from plantaopro.plantoes where status='cancelado') as PlantoesCancelados,(select count(1) from plantaopro.pagamentos where status='pendente' and reg_status='A') as PagamentosPendentes,(select count(1) from plantaopro.pagamentos where status='pago' and reg_status='A') as PagamentosPagos,(select coalesce(sum(valor_previsto),0) from plantaopro.pagamentos where status='pendente' and reg_status='A') as ValorPendente,(select coalesce(sum(valor_pago),0) from plantaopro.pagamentos where status='pago' and reg_status='A' and date_trunc('month',coalesce(data_pagamento,now()))=date_trunc('month',now())) as ValorPagoMes,(select count(1) from plantaopro.notificacoes where usuario_id=@uid and lida=false and reg_status='A') as NotificacoesNaoLidas", new
            {
                uid
            });
            var prox = await cn.QueryAsync<PlantaoResumoDto>("select p.id,h.nome_fantasia as HospitalNome,h.cidade as HospitalCidade,h.estado as HospitalEstado,e.nome as EspecialidadeNome,p.data_inicio as DataInicio,p.data_fim as DataFim,p.valor,p.vagas,p.vagas_disponiveis as VagasDisponiveis,p.tipo,p.status,p.observacoes from plantaopro.plantoes p join plantaopro.hospitais h on h.id=p.hospital_id join plantaopro.especialidades e on e.id=p.especialidade_id where p.data_inicio>=now() and p.reg_status='A' order by p.data_inicio asc limit 5");
            var pag = await cn.QueryAsync<PagamentoResumoDto>("select pg.id,pg.escala_id as EscalaId,pg.medico_id as MedicoId,pg.plantao_id as PlantaoId,m.nome as MedicoNome,m.crm as MedicoCrm,h.nome_fantasia as HospitalNome,esp.nome as EspecialidadeNome,pl.data_inicio as DataPlantao,pg.valor_previsto as ValorPrevisto,pg.valor_pago as ValorPago,pg.status,pg.data_prevista as DataPrevista,pg.data_pagamento as DataPagamento,pg.forma_pagamento as FormaPagamento,null::text as ChavePix,pg.observacoes from plantaopro.pagamentos pg join plantaopro.plantoes pl on pl.id=pg.plantao_id join plantaopro.medicos m on m.id=pg.medico_id join plantaopro.hospitais h on h.id=pl.hospital_id join plantaopro.especialidades esp on esp.id=pl.especialidade_id where pg.reg_status='A' order by pg.reg_date desc limit 5");
            var nots = await cn.QueryAsync<NotificacaoDto>("select id,titulo,mensagem,tipo,lida,reg_date as RegDate from plantaopro.notificacoes where usuario_id=@uid and reg_status='A' order by reg_date desc limit 5", new
            {
                uid
            });
            var plMes = await cn.QueryAsync<DashboardChartItem>("select to_char(date_trunc('month',data_inicio),'YYYY-MM') as Label,count(1)::decimal as Valor from plantaopro.plantoes where reg_status='A' group by 1 order by 1");
            var pgMes = await cn.QueryAsync<DashboardChartItem>("select to_char(date_trunc('month',coalesce(data_pagamento::timestamp,reg_date)),'YYYY-MM') as Label,coalesce(sum(coalesce(valor_pago,valor_previsto)),0) as Valor from plantaopro.pagamentos where reg_status='A' group by 1 order by 1");
            var plEsp = await cn.QueryAsync<DashboardChartItem>("select e.nome as Label,count(1)::decimal as Valor from plantaopro.plantoes p join plantaopro.especialidades e on e.id=p.especialidade_id where p.reg_status='A' group by e.nome order by Valor desc");
            var plHosp = await cn.QueryAsync<DashboardChartItem>("select h.nome_fantasia as Label,count(1)::decimal as Valor from plantaopro.plantoes p join plantaopro.hospitais h on h.id=p.hospital_id where p.reg_status='A' group by h.nome_fantasia order by Valor desc");
            return ApiResponse<DashboardOverviewDto>.Ok(new(ind, prox, pag, nots, plMes, pgMes, plEsp, plHosp));
        }
    }
}
