using System.Security.Cryptography;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

// ============================================================================
// B5 - Convites de equipe com expiração e uso único.
// Token: 32 bytes aleatórios (hex na URL); o banco guarda SÓ o hash SHA-256.
// Aceite atômico: UPDATE condicional (pendente + válido) — repetição ou
// corrida devolve 409/410 sem duplicar usuário. Capacidade do plano e perfis
// revalidados no aceite (não só no convite).
// ============================================================================
public sealed class ConvitesEquipeService
{
    /// <summary>Validade padrão do convite (dias) — decisão explícita B5.</summary>
    public const int ValidadePadraoDias = 7;

    /// <summary>Validade máxima do convite (dias) — decisão explícita B5.</summary>
    public const int ValidadeMaximaDias = 30;

    private readonly IConfiguration _cfg;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService _audit;
    private readonly ILogger<ConvitesEquipeService> _logger;

    public ConvitesEquipeService(IConfiguration cfg, ICurrentUserService currentUser, IAuditService audit, ILogger<ConvitesEquipeService> logger)
    {
        _cfg = cfg;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    private NpgsqlConnection Connection() => new(_cfg.GetConnectionString("Default"));

    public static string GerarToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    public static string EstadoDe(DateTime? usadoEm, DateTime? revogadoEm, DateTime expiraEm, DateTime agora)
    {
        if (usadoEm.HasValue) return "USADO";
        if (revogadoEm.HasValue) return "REVOGADO";
        if (expiraEm <= agora) return "EXPIRADO";
        return "PENDENTE";
    }

    private (Guid TenantId, Guid ClienteId)? Escopo() =>
        _currentUser.TenantId is Guid tenant && _currentUser.IsTenantAdmin()
            ? (tenant, tenant)
            : null;

    public async Task<ApiResponse<ConviteEquipeDto>> CriarAsync(CriarConviteEquipeRequest request, string? ip, CancellationToken ct)
    {
        var escopo = Escopo();
        if (escopo is null) return ApiResponse<ConviteEquipeDto>.Fail("Somente administradores do cliente podem convidar.", 403);
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) return ApiResponse<ConviteEquipeDto>.Fail("Informe um e-mail válido.", 400);
        var perfilIds = (request.PerfilIds ?? Array.Empty<Guid>()).Where(p => p != Guid.Empty).Distinct().ToArray();
        if (perfilIds.Length == 0) return ApiResponse<ConviteEquipeDto>.Fail("Selecione ao menos um perfil.", 400);
        var dias = request.DiasValidade is > 0 ? Math.Min(request.DiasValidade.Value, ValidadeMaximaDias) : ValidadePadraoDias;

        try
        {
            await using var cn = Connection();
            await cn.OpenAsync(ct);
            await using var tx = await cn.BeginTransactionAsync(ct);
            var (tenantId, clienteId) = escopo.Value;

            var pendente = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
                "select exists(select 1 from plantaopro.usuario_convites where reg_status='A' and tenant_id=@tenantId and lower(email)=lower(@email) and usado_em is null and revogado_em is null)",
                new { tenantId, email }, tx, cancellationToken: ct));
            if (pendente)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<ConviteEquipeDto>.Fail("Já existe um convite pendente para este e-mail.", 409);
            }
            var existente = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
                "select exists(select 1 from plantaopro.usuarios where lower(coalesce(email_normalizado,email))=lower(@email) and reg_status='A')",
                new { email }, tx, cancellationToken: ct));
            if (existente)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<ConviteEquipeDto>.Fail("Este e-mail já possui usuário ativo.", 409);
            }
            var perfis = await cn.QueryAsync<(Guid Id, string Nome)>(new CommandDefinition(
                @"select id as ""Id"", coalesce(nome,'') as ""Nome"" from plantaopro.perfis
where id=any(@ids) and reg_status='A' and coalesce(status,'ATIVO')='ATIVO'
  and (tenant_id is null or tenant_id=@tenantId)
  and upper(coalesce(codigo,nome)) not in ('ADMINISTRADOR_GLOBAL')",
                new { ids = perfilIds, tenantId }, tx, cancellationToken: ct));
            var validos = perfis.AsList();
            if (validos.Count != perfilIds.Length)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<ConviteEquipeDto>.Fail("Um ou mais perfis não pertencem ao cliente ou não podem ser atribuídos.", 400);
            }
            var capacidade = await CapacidadeAsync(cn, tx, tenantId, ct);
            if (capacidade.Limite > 0 && capacidade.Usados >= capacidade.Limite)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<ConviteEquipeDto>.Fail($"Limite contratual de {capacidade.Limite} usuários ativos atingido.", 409);
            }

            var token = GerarToken();
            var id = Guid.NewGuid();
            var expira = DateTime.UtcNow.AddDays(dias);
            await cn.ExecuteAsync(new CommandDefinition(
                @"insert into plantaopro.usuario_convites(id,tenant_id,cliente_id,email,perfil_ids,token_hash,expira_em,criado_por,reg_date,reg_status)
values(@id,@tenantId,@clienteId,@email,@perfis,@hash,@expira,@autor,now(),'A')",
                new { id, tenantId, clienteId, email, perfis = perfilIds, hash = HashToken(token), expira, autor = _currentUser.UserId }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            await _audit.RegistrarAsync(_currentUser.UserId, clienteId, "CONVITE_EQUIPE", id, "CRIAR",
                new { email, perfis = validos.Select(p => p.Nome), expira }, true, ip, string.Join(',', _currentUser.Roles));
            return ApiResponse<ConviteEquipeDto>.Ok(new ConviteEquipeDto
            {
                Id = id,
                Email = email,
                PerfilIds = perfilIds,
                PerfilNomes = validos.Select(p => p.Nome).ToArray(),
                ExpiraEm = expira,
                Estado = "PENDENTE",
                Token = token
            }, "Convite criado. Compartilhe o link uma única vez — o token não é exibido de novo.");
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning(ex, "Corrida de convite duplicado tenant:{Tenant}", escopo.Value.TenantId);
            return ApiResponse<ConviteEquipeDto>.Fail("Já existe um convite pendente para este e-mail.", 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar convite de equipe");
            return ApiResponse<ConviteEquipeDto>.Fail("Não foi possível criar o convite.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<ConviteEquipeDto>>> ListarAsync(CancellationToken ct)
    {
        var escopo = Escopo();
        if (escopo is null) return ApiResponse<IEnumerable<ConviteEquipeDto>>.Fail("Somente administradores do cliente podem listar convites.", 403);
        try
        {
            await using var cn = Connection();
            var agora = DateTime.UtcNow;
            var rows = await cn.QueryAsync<ConviteRow>(new CommandDefinition(
                @"select c.id as ""Id"", c.email as ""Email"", c.perfil_ids as ""PerfilIds"", c.expira_em as ""ExpiraEm"",
       c.usado_em as ""UsadoEm"", c.revogado_em as ""RevogadoEm"", c.reg_date as ""CriadoEm""
  from plantaopro.usuario_convites c
 where c.reg_status='A' and c.tenant_id=@tenantId
 order by c.reg_date desc limit 100", new { tenantId = escopo.Value.TenantId }, cancellationToken: ct));
            var nomes = (await cn.QueryAsync<(Guid Id, string Nome)>(new CommandDefinition(
                "select id as \"Id\", coalesce(nome,'') as \"Nome\" from plantaopro.perfis where reg_status='A' and (tenant_id is null or tenant_id=@tenantId)",
                new { tenantId = escopo.Value.TenantId }, cancellationToken: ct))).ToDictionary(p => p.Id, p => p.Nome);
            return ApiResponse<IEnumerable<ConviteEquipeDto>>.Ok(rows.Select(r => new ConviteEquipeDto
            {
                Id = r.Id,
                Email = r.Email,
                PerfilIds = r.PerfilIds ?? Array.Empty<Guid>(),
                PerfilNomes = (r.PerfilIds ?? Array.Empty<Guid>()).Select(id => nomes.GetValueOrDefault(id, id.ToString())).ToArray(),
                ExpiraEm = r.ExpiraEm,
                UsadoEm = r.UsadoEm,
                Estado = EstadoDe(r.UsadoEm, r.RevogadoEm, r.ExpiraEm, agora),
                CriadoEm = r.CriadoEm
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar convites de equipe");
            return ApiResponse<IEnumerable<ConviteEquipeDto>>.Fail("Não foi possível listar os convites.", 500);
        }
    }

    public async Task<ApiResponse<string>> RevogarAsync(Guid id, string? ip, CancellationToken ct)
    {
        var escopo = Escopo();
        if (escopo is null) return ApiResponse<string>.Fail("Somente administradores do cliente podem revogar convites.", 403);
        try
        {
            await using var cn = Connection();
            var marcadas = await cn.ExecuteAsync(new CommandDefinition(
                "update plantaopro.usuario_convites set revogado_em=now(), reg_update=now() where id=@id and tenant_id=@tenantId and reg_status='A' and usado_em is null and revogado_em is null",
                new { id, tenantId = escopo.Value.TenantId }, cancellationToken: ct));
            if (marcadas == 0) return ApiResponse<string>.Fail("Convite não encontrado ou já encerrado.", 404);
            await _audit.RegistrarAsync(_currentUser.UserId, escopo.Value.ClienteId, "CONVITE_EQUIPE", id, "REVOGAR", new { }, true, ip, string.Join(',', _currentUser.Roles));
            return ApiResponse<string>.Ok("ok", "Convite revogado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao revogar convite {Id}", id);
            return ApiResponse<string>.Fail("Não foi possível revogar o convite.", 500);
        }
    }

    public async Task<ApiResponse<ConvitePublicoDto>> ValidarAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return ApiResponse<ConvitePublicoDto>.Fail("Convite inválido.", 404);
        try
        {
            await using var cn = Connection();
            var row = await cn.QueryFirstOrDefaultAsync<ConvitePublicoRow>(new CommandDefinition(
                @"select c.email as ""Email"", coalesce(t.nome,'') as ""TenantNome"", c.expira_em as ""ExpiraEm"",
       c.usado_em as ""UsadoEm"", c.revogado_em as ""RevogadoEm""
  from plantaopro.usuario_convites c
  join plantaopro.tenants t on t.id=c.tenant_id
 where c.token_hash=@hash and c.reg_status='A'",
                new { hash = HashToken(token.Trim()) }, cancellationToken: ct));
            if (row is null) return ApiResponse<ConvitePublicoDto>.Fail("Convite inválido.", 404);
            var agora = DateTime.UtcNow;
            var estado = EstadoDe(row.UsadoEm, row.RevogadoEm, row.ExpiraEm, agora);
            return ApiResponse<ConvitePublicoDto>.Ok(new ConvitePublicoDto
            {
                Email = row.Email,
                TenantNome = row.TenantNome,
                ExpiraEm = row.ExpiraEm,
                Valido = estado == "PENDENTE",
                Motivo = estado == "PENDENTE" ? string.Empty : "Este convite está " + estado.ToLowerInvariant() + "."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar convite");
            return ApiResponse<ConvitePublicoDto>.Fail("Não foi possível validar o convite.", 500);
        }
    }

    public async Task<ApiResponse<Guid>> AceitarAsync(string token, AceitarConviteRequest request, string? ip, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return ApiResponse<Guid>.Fail("Convite inválido.", 404);
        if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Trim().Length < 3) return ApiResponse<Guid>.Fail("Informe seu nome (mínimo 3 caracteres).", 400);
        if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 8) return ApiResponse<Guid>.Fail("Senha deve ter no mínimo 8 caracteres.", 400);
        try
        {
            await using var cn = Connection();
            await cn.OpenAsync(ct);
            await using var tx = await cn.BeginTransactionAsync(ct);
            // Uso único atômico: só PENDENTE dentro da validade transiciona; a
            // segunda tentativa (ou corrida) encontra 0 linhas e recebe 409/410.
            var convite = await cn.QueryFirstOrDefaultAsync<ConviteAceiteRow>(new CommandDefinition(
                @"update plantaopro.usuario_convites set reg_update=now()
where token_hash=@hash and reg_status='A' and usado_em is null and revogado_em is null and expira_em>now()
returning id, tenant_id as ""TenantId"", cliente_id as ""ClienteId"", email as ""Email"", perfil_ids as ""PerfilIds"", expira_em as ""ExpiraEm""",
                new { hash = HashToken(token.Trim()) }, tx, cancellationToken: ct));
            if (convite is null)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<Guid>.Fail(await MotivoEncerradoAsync(cn, HashToken(token.Trim()), ct), 410);
            }
            var ocupado = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
                "select exists(select 1 from plantaopro.usuarios where lower(coalesce(email_normalizado,email))=lower(@email) and reg_status='A')",
                new { email = convite.Email }, tx, cancellationToken: ct));
            if (ocupado)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<Guid>.Fail("Este e-mail já possui usuário ativo.", 409);
            }
            var perfis = await cn.QueryAsync<Guid>(new CommandDefinition(
                @"select id from plantaopro.perfis
where id=any(@ids) and reg_status='A' and coalesce(status,'ATIVO')='ATIVO'
  and (tenant_id is null or tenant_id=@tenantId)
  and upper(coalesce(codigo,nome)) not in ('ADMINISTRADOR_GLOBAL')",
                new { ids = convite.PerfilIds, tenantId = convite.TenantId }, tx, cancellationToken: ct));
            if (perfis.Count() != convite.PerfilIds.Length)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<Guid>.Fail("Os perfis do convite não estão mais disponíveis.", 410);
            }
            var capacidade = await CapacidadeAsync(cn, tx, convite.TenantId, ct);
            if (capacidade.Limite > 0 && capacidade.Usados >= capacidade.Limite)
            {
                await tx.RollbackAsync(ct);
                return ApiResponse<Guid>.Fail($"Limite contratual de {capacidade.Limite} usuários ativos atingido.", 409);
            }

            var usuarioId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(
                @"insert into plantaopro.usuarios(id,tenant_id,cliente_id,nome,email,email_normalizado,senha_hash,telefone,status,reg_status,senha_alteracao_obrigatoria,reg_date)
values(@id,@tenantId,@clienteId,@nome,@email,upper(@email),@hash,@telefone,'ATIVO','A',false,now())",
                new
                {
                    id = usuarioId,
                    tenantId = convite.TenantId,
                    clienteId = convite.ClienteId,
                    nome = request.Nome.Trim(),
                    email = convite.Email,
                    hash = Security.PasswordHashService.Hash(request.Senha),
                    telefone = string.IsNullOrWhiteSpace(request.Telefone) ? null : request.Telefone.Trim()
                }, tx, cancellationToken: ct));
            foreach (var perfilId in convite.PerfilIds)
            {
                await cn.ExecuteAsync(new CommandDefinition(
                    "insert into plantaopro.usuarios_perfis(id,tenant_id,cliente_id,usuario_id,perfil_id,reg_date,reg_status) values(gen_random_uuid(),@tenantId,@clienteId,@usuarioId,@perfilId,now(),'A')",
                    new { tenantId = convite.TenantId, clienteId = convite.ClienteId, usuarioId, perfilId }, tx, cancellationToken: ct));
            }
            await cn.ExecuteAsync(new CommandDefinition(
                "update plantaopro.usuario_convites set usado_em=now(), usado_por=@usuarioId, reg_update=now() where id=@id",
                new { id = convite.Id, usuarioId }, tx, cancellationToken: ct));
            await tx.CommitAsync(ct);
            await _audit.RegistrarAsync(usuarioId, convite.ClienteId, "CONVITE_EQUIPE", convite.Id, "ACEITAR",
                new { email = convite.Email }, true, ip, "CONVIDADO");
            return ApiResponse<Guid>.Ok(usuarioId, "Conta criada. Faça login para continuar.");
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            _logger.LogWarning(ex, "Corrida no aceite de convite");
            return ApiResponse<Guid>.Fail("Este convite acabou de ser utilizado.", 409);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aceitar convite");
            return ApiResponse<Guid>.Fail("Não foi possível concluir o aceite.", 500);
        }
    }

    private async Task<string> MotivoEncerradoAsync(NpgsqlConnection cn, string hash, CancellationToken ct)
    {
        var estado = await cn.QueryFirstOrDefaultAsync<(DateTime? Usado, DateTime? Revogado, DateTime? Expira)>(new CommandDefinition(
            "select usado_em as \"Usado\", revogado_em as \"Revogado\", expira_em as \"Expira\" from plantaopro.usuario_convites where token_hash=@hash and reg_status='A'",
            new { hash }, cancellationToken: ct));
        if (estado == default) return "Convite inválido.";
        return EstadoDe(estado.Usado, estado.Revogado, estado.Expira ?? DateTime.MinValue, DateTime.UtcNow) switch
        {
            "USADO" => "Este convite já foi utilizado.",
            "REVOGADO" => "Este convite foi revogado.",
            _ => "Este convite expirou."
        };
    }

    private async Task<(int Limite, int Usados)> CapacidadeAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction tx, Guid tenantId, CancellationToken ct) =>
        await cn.QuerySingleAsync<(int Limit, int Used)>(new CommandDefinition(@"select
coalesce((select p.limite_usuarios from plantaopro.assinaturas a join plantaopro.planos p on p.id=a.plano_id
 where a.cliente_id=@tenantId and a.reg_status='A' and upper(coalesce(a.status,'')) in ('ATIVA','TRIAL')
 and (a.data_fim is null or a.data_fim>=current_date)
 order by case when upper(a.status)='ATIVA' then 0 else 1 end,a.reg_date desc limit 1),0) as ""Limit"",
(select count(*)::int from plantaopro.usuarios u where coalesce(u.tenant_id,u.cliente_id)=@tenantId
 and u.reg_status='A' and coalesce(u.status,'ATIVO')='ATIVO') as ""Used""", new { tenantId }, tx, cancellationToken: ct));

    private sealed class ConviteRow
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public Guid[]? PerfilIds { get; set; }
        public DateTime ExpiraEm { get; set; }
        public DateTime? UsadoEm { get; set; }
        public DateTime? RevogadoEm { get; set; }
        public DateTime CriadoEm { get; set; }
    }

    private sealed class ConvitePublicoRow
    {
        public string Email { get; set; } = string.Empty;
        public string TenantNome { get; set; } = string.Empty;
        public DateTime ExpiraEm { get; set; }
        public DateTime? UsadoEm { get; set; }
        public DateTime? RevogadoEm { get; set; }
    }

    private sealed class ConviteAceiteRow
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ClienteId { get; set; }
        public string Email { get; set; } = string.Empty;
        public Guid[] PerfilIds { get; set; } = Array.Empty<Guid>();
        public DateTime ExpiraEm { get; set; }
    }
}
