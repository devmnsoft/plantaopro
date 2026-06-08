using System.Security.Claims;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Data;

public sealed class TenantContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _cfg;
    private readonly ILogger<TenantContextService> _logger;

    public TenantContextService(IHttpContextAccessor httpContextAccessor, IConfiguration cfg, ILogger<TenantContextService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _cfg = cfg;
        _logger = logger;
    }

    public async Task<ApiResponse<TenantContextDto>> ObterAtualAsync(Guid? tenantRota = null)
    {
        try
        {
            var http = _httpContextAccessor.HttpContext;
            var tenantId = tenantRota ?? LerGuidClaim("tenant_id");
            var clienteId = LerGuidClaim("cliente_id");
            var host = http?.Request.Host.Host ?? string.Empty;
            var headerTenant = http?.Request.Headers["X-Tenant"].FirstOrDefault();

            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var tenant = await cn.QueryFirstOrDefaultAsync<TenantContextDto>(@"select t.id as ""TenantId"", t.cliente_id as ""ClienteId"", coalesce(t.nome,'') as ""TenantNome"", coalesce(t.status,'') as ""Status"", t.plano_id as ""PlanoId"", coalesce(p.nome,'') as ""PlanoNome""
from plantaopro.tenants t
left join plantaopro.planos p on p.id=t.plano_id
where t.reg_status='A' and (
    (@tenantId is not null and t.id=@tenantId) or
    (@clienteId is not null and t.cliente_id=@clienteId) or
    (@headerTenant <> '' and lower(t.slug)=lower(@headerTenant)) or
    (@host <> '' and (lower(t.dominio_customizado)=lower(@host) or lower(t.subdominio)=lower(split_part(@host,'.',1))))
)
order by case when @tenantId is not null and t.id=@tenantId then 0 else 1 end
limit 1", new { tenantId, clienteId, headerTenant = headerTenant ?? string.Empty, host });

            if (tenant is null && clienteId.HasValue)
            {
                tenant = await CriarContextoLegadoAsync(cn, clienteId.Value);
            }

            if (tenant is null) return ApiResponse<TenantContextDto>.Fail("Tenant não identificado. Informe X-Tenant, domínio, subdomínio ou autentique-se.", 404);
            if (string.Equals(tenant.Status, "SUSPENSO", StringComparison.OrdinalIgnoreCase)) return ApiResponse<TenantContextDto>.Fail("Tenant suspenso. Regularize a assinatura para continuar.", 403);

            if (tenant.TenantId.HasValue)
            {
                tenant.Modulos = (await cn.QueryAsync<string>("select coalesce(codigo_modulo,'') from plantaopro.tenant_modulos where tenant_id=@tenantId and habilitado=true and reg_status='A' order by codigo_modulo", new { tenantId = tenant.TenantId.Value })).ToArray();
                tenant.Permissoes = await ObterPermissoesUsuarioAsync(cn, tenant.TenantId.Value);
            }

            return ApiResponse<TenantContextDto>.Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao resolver contexto multi-tenant");
            return ApiResponse<TenantContextDto>.Fail("Não foi possível resolver o tenant atual.", 500);
        }
    }

    public Guid? ObterUsuarioId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var valor = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");
        Guid parsed;
        return Guid.TryParse(valor, out parsed) ? parsed : null;
    }

    private Guid? LerGuidClaim(string tipo)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var valor = user?.FindFirstValue(tipo);
        Guid parsed;
        return Guid.TryParse(valor, out parsed) ? parsed : null;
    }

    private static async Task<TenantContextDto?> CriarContextoLegadoAsync(NpgsqlConnection cn, Guid clienteId)
    {
        var cliente = await cn.QueryFirstOrDefaultAsync<ClienteTenantLegadoDto>(@"select id as ""Id"", coalesce(nome_fantasia, razao_social, '') as ""Nome"", plano_id as ""PlanoId"" from plantaopro.clientes where id=@clienteId and reg_status='A'", new { clienteId });
        if (cliente is null) return null;
        var tenantId = Guid.NewGuid();
        var slug = Slug(cliente.Nome);
        await cn.ExecuteAsync(@"insert into plantaopro.tenants(id,cliente_id,nome,slug,status,plano_id,reg_date,reg_status)
values(@tenantId,@clienteId,@nome,@slug,'ATIVO',@planoId,now(),'A')
on conflict do nothing", new { tenantId, clienteId, nome = cliente.Nome, slug, planoId = cliente.PlanoId });
        return new TenantContextDto { TenantId = tenantId, ClienteId = clienteId, TenantNome = cliente.Nome, Status = "ATIVO", PlanoId = cliente.PlanoId };
    }

    private sealed class ClienteTenantLegadoDto
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public Guid? PlanoId { get; set; }
    }

    private async Task<IEnumerable<string>> ObterPermissoesUsuarioAsync(NpgsqlConnection cn, Guid tenantId)
    {
        var usuarioId = ObterUsuarioId();
        if (!usuarioId.HasValue) return Array.Empty<string>();
        return (await cn.QueryAsync<string>(@"select distinct coalesce(pm.codigo,'')
from plantaopro.usuario_perfis up
join plantaopro.perfil_permissoes pp on pp.perfil_id=up.perfil_id and pp.permitido=true and pp.reg_status='A'
join plantaopro.permissoes pm on pm.id=pp.permissao_id and pm.reg_status='A'
where up.usuario_id=@usuarioId and up.tenant_id=@tenantId and up.reg_status='A'
union
select distinct coalesce(pm.codigo,'')
from plantaopro.usuario_permissoes_especiais upe
join plantaopro.permissoes pm on pm.id=upe.permissao_id and pm.reg_status='A'
where upe.usuario_id=@usuarioId and upe.tenant_id=@tenantId and upe.permitido=true and upe.reg_status='A'", new { usuarioId = usuarioId.Value, tenantId })).ToArray();
    }

    public static string Slug(string valor)
    {
        var normalizado = new string((valor ?? string.Empty).Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (normalizado.Contains("--", StringComparison.Ordinal)) normalizado = normalizado.Replace("--", "-", StringComparison.Ordinal);
        return string.IsNullOrWhiteSpace(normalizado.Trim('-')) ? "tenant" : normalizado.Trim('-');
    }
}

public sealed class SelfServiceSaasService
{
    private readonly IConfiguration _cfg;
    private readonly TenantContextService _tenantContext;
    private readonly IAuditService _audit;
    private readonly ILogger<SelfServiceSaasService> _logger;
    private readonly AssinaturaGuardService _assinaturaGuard;

    public SelfServiceSaasService(IConfiguration cfg, TenantContextService tenantContext, IAuditService audit, AssinaturaGuardService assinaturaGuard, ILogger<SelfServiceSaasService> logger)
    {
        _cfg = cfg;
        _tenantContext = tenantContext;
        _audit = audit;
        _assinaturaGuard = assinaturaGuard;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<PlanoPublicoDto>>> ListarPlanosPublicosAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var planos = (await cn.QueryAsync<PlanoPublicoDto>(@"select id as ""Id"", coalesce(nome,'') as ""Nome"", coalesce(slug,'') as ""Slug"", coalesce(descricao,'') as ""Descricao"", valor_mensal as ""ValorMensal"", limite_medicos as ""LimiteMedicos"", limite_hospitais as ""LimiteHospitais"", limite_plantoes_mes as ""LimitePlantoesMes"", coalesce(limite_usuarios,0) as ""LimiteUsuarios"", coalesce(permite_mobile,false) as ""PermiteMobile"", coalesce(permite_bi,false) as ""PermiteBi"", coalesce(permite_white_label,false) as ""PermiteWhiteLabel"", coalesce(destaque,false) as ""Destaque""
from plantaopro.planos where reg_status='A' and coalesce(publico,true)=true and upper(coalesce(status,'ATIVO'))='ATIVO' order by coalesce(ordem,999), valor_mensal limit 20")).ToList();
            foreach (var plano in planos)
            {
                plano.Recursos = RecursosDoPlano(plano).ToArray();
            }
            return ApiResponse<IEnumerable<PlanoPublicoDto>>.Ok(planos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar planos públicos");
            return ApiResponse<IEnumerable<PlanoPublicoDto>>.Fail("Não foi possível carregar os planos públicos.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<PlanoComparativoDto>>> ComparativoAsync()
    {
        var planosResponse = await ListarPlanosPublicosAsync();
        if (!planosResponse.Success || planosResponse.Data is null) return ApiResponse<IEnumerable<PlanoComparativoDto>>.Fail(planosResponse.Message, planosResponse.StatusCode);
        var planos = planosResponse.Data.ToArray();
        var linhas = new List<PlanoComparativoDto>();
        AdicionarLinha(linhas, planos, "Limites", "Médicos", p => LimiteTexto(p.LimiteMedicos));
        AdicionarLinha(linhas, planos, "Limites", "Hospitais/unidades", p => LimiteTexto(p.LimiteHospitais));
        AdicionarLinha(linhas, planos, "Limites", "Plantões/mês", p => LimiteTexto(p.LimitePlantoesMes));
        AdicionarLinha(linhas, planos, "Limites", "Usuários administrativos", p => LimiteTexto(p.LimiteUsuarios));
        AdicionarLinha(linhas, planos, "Recursos", "API Mobile", p => SimNao(p.PermiteMobile));
        AdicionarLinha(linhas, planos, "Recursos", "BI", p => SimNao(p.PermiteBi));
        AdicionarLinha(linhas, planos, "Recursos", "White label", p => SimNao(p.PermiteWhiteLabel));
        return ApiResponse<IEnumerable<PlanoComparativoDto>>.Ok(linhas);
    }

    public async Task<ApiResponse<CadastroSelfServiceResultadoDto>> FinalizarCadastroAsync(CadastroSelfServiceRequest request, string? ip, string? userAgent)
    {
        try
        {
            var erros = ValidarCadastro(request);
            if (erros.Count > 0) return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("Verifique os dados do cadastro.", 400, erros);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();

            var cnpjLimpo = SomenteDigitos(request.Empresa.Cnpj);
            var existeCnpj = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from plantaopro.clientes where regexp_replace(cnpj,'\\D','','g')=@cnpj and reg_status='A')", new { cnpj = cnpjLimpo }, tx);
            if (existeCnpj) { await tx.RollbackAsync(); return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("CNPJ já cadastrado.", 400); }
            var existeEmail = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from plantaopro.usuarios where lower(email)=lower(@email) and reg_status='A')", new { email = request.UsuarioAdmin.Email }, tx);
            if (existeEmail) { await tx.RollbackAsync(); return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("E-mail do administrador já cadastrado.", 400); }
            var plano = await cn.QueryFirstOrDefaultAsync<PlanoPublicoDto>(@"select id as ""Id"", coalesce(nome,'') as ""Nome"", coalesce(slug,'') as ""Slug"", coalesce(descricao,'') as ""Descricao"", valor_mensal as ""ValorMensal"", limite_medicos as ""LimiteMedicos"", limite_hospitais as ""LimiteHospitais"", limite_plantoes_mes as ""LimitePlantoesMes"", coalesce(limite_usuarios,0) as ""LimiteUsuarios"", coalesce(permite_mobile,false) as ""PermiteMobile"", coalesce(permite_bi,false) as ""PermiteBi"", coalesce(permite_white_label,false) as ""PermiteWhiteLabel"", coalesce(destaque,false) as ""Destaque"" from plantaopro.planos where id=@id and reg_status='A' and upper(coalesce(status,'ATIVO'))='ATIVO'", new { id = request.Plano.PlanoId }, tx);
            if (plano is null) { await tx.RollbackAsync(); return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("Plano escolhido não está ativo.", 400); }

            var solicitacaoId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var clienteId = Guid.NewGuid();
            var assinaturaId = Guid.NewGuid();
            var usuarioId = Guid.NewGuid();
            var slug = TenantContextService.Slug(request.Empresa.NomeFantasia);
            var senhaHash = BCrypt.Net.BCrypt.HashPassword(request.UsuarioAdmin.Senha);

            await cn.ExecuteAsync(@"insert into plantaopro.cadastro_cliente_solicitacoes(id,plano_id,nome_fantasia,razao_social,cnpj,segmento,qtd_medicos,qtd_hospitais,volume_plantoes_mes,cidade,uf,telefone,email_corporativo,responsavel_nome,responsavel_email,responsavel_telefone,responsavel_cargo,periodicidade,aceite_termos,aceite_privacidade,consentimento_lgpd,status,reg_date,reg_status)
values(@solicitacaoId,@PlanoId,@NomeFantasia,@RazaoSocial,@Cnpj,@Segmento,@QuantidadeMedicos,@QuantidadeHospitais,@VolumePlantoesMes,@Cidade,@Uf,@Telefone,@EmailCorporativo,@AdminNome,@AdminEmail,@AdminTelefone,@Cargo,@Periodicidade,@AceiteTermos,@AceitePrivacidade,@ConsentimentoLgpd,'FINALIZADO',now(),'A')", new { solicitacaoId, request.Plano.PlanoId, request.Empresa.NomeFantasia, request.Empresa.RazaoSocial, Cnpj = cnpjLimpo, request.Empresa.Segmento, request.Empresa.QuantidadeMedicos, request.Empresa.QuantidadeHospitais, request.Empresa.VolumePlantoesMes, request.Empresa.Cidade, request.Empresa.Uf, request.Empresa.Telefone, request.Empresa.EmailCorporativo, AdminNome = request.UsuarioAdmin.Nome, AdminEmail = request.UsuarioAdmin.Email, AdminTelefone = request.UsuarioAdmin.Telefone, request.UsuarioAdmin.Cargo, request.Plano.Periodicidade, request.Plano.AceiteTermos, request.Plano.AceitePrivacidade, request.Plano.ConsentimentoLgpd }, tx);

            await cn.ExecuteAsync("insert into plantaopro.tenants(id,cliente_id,nome,slug,status,plano_id,subdominio,reg_date,reg_status) values(@tenantId,@clienteId,@nome,@slug,'ATIVO',@planoId,@slug,now(),'A')", new { tenantId, clienteId, nome = request.Empresa.NomeFantasia, slug, planoId = request.Plano.PlanoId }, tx);
            await cn.ExecuteAsync(@"insert into plantaopro.clientes(id,razao_social,nome_fantasia,cnpj,email,telefone,cidade,estado,plano_id,status,reg_status,reg_date)
values(@clienteId,@RazaoSocial,@NomeFantasia,@Cnpj,@Email,@Telefone,@Cidade,@Uf,@PlanoId,'ATIVO','A',now())", new { clienteId, request.Empresa.RazaoSocial, request.Empresa.NomeFantasia, Cnpj = cnpjLimpo, Email = request.Empresa.EmailCorporativo, request.Empresa.Telefone, request.Empresa.Cidade, request.Empresa.Uf, request.Plano.PlanoId }, tx);
            await cn.ExecuteAsync(@"insert into plantaopro.assinaturas(id,tenant_id,cliente_id,plano_id,data_inicio,data_fim,status,valor_contratado,dia_vencimento,observacoes,periodicidade,reg_status,reg_date)
values(@assinaturaId,@tenantId,@clienteId,@PlanoId,now(),now()+interval '1 month','ATIVA',@Valor,@Dia,'Criada via self-service',@Periodicidade,'A',now())", new { assinaturaId, tenantId, clienteId, request.Plano.PlanoId, Valor = plano.ValorMensal, Dia = DateTime.UtcNow.Day, request.Plano.Periodicidade }, tx);
            await cn.ExecuteAsync(@"insert into plantaopro.usuarios(id,nome,email,telefone,senha_hash,cliente_id,status,reg_status,reg_date)
values(@usuarioId,@Nome,@Email,@Telefone,@SenhaHash,@clienteId,'ATIVO','A',now())", new { usuarioId, request.UsuarioAdmin.Nome, request.UsuarioAdmin.Email, request.UsuarioAdmin.Telefone, SenhaHash = senhaHash, clienteId }, tx);

            var perfilId = await GarantirPerfilAdminClienteAsync(cn, tx, tenantId, clienteId);
            await cn.ExecuteAsync("insert into plantaopro.usuario_perfis(id,tenant_id,cliente_id,usuario_id,perfil_id,reg_date,reg_status) values(gen_random_uuid(),@tenantId,@clienteId,@usuarioId,@perfilId,now(),'A')", new { tenantId, clienteId, usuarioId, perfilId }, tx);
            await CriarWhiteLabelPadraoAsync(cn, tx, tenantId, request.Empresa.NomeFantasia);
            await CriarOnboardingAsync(cn, tx, tenantId, clienteId);
            await cn.ExecuteAsync(@"insert into plantaopro.lgpd_consentimentos(id,tenant_id,cliente_id,usuario_id,titular_email,finalidade,versao_politica,aceito,origem,ip_origem,user_agent,reg_date,reg_status)
values(gen_random_uuid(),@tenantId,@clienteId,@usuarioId,@Email,'cadastro_self_service','1.0',true,'SELF_SERVICE',@ip,@ua,now(),'A')", new { tenantId, clienteId, usuarioId, request.UsuarioAdmin.Email, ip = ip ?? string.Empty, ua = userAgent ?? string.Empty }, tx);
            await cn.ExecuteAsync(@"insert into plantaopro.cadastro_cliente_pagamentos_iniciais(id,solicitacao_id,cliente_id,assinatura_id,valor,status,vencimento,reg_date,reg_status)
values(gen_random_uuid(),@solicitacaoId,@clienteId,@assinaturaId,@Valor,'ABERTO',current_date+7,now(),'A')", new { solicitacaoId, clienteId, assinaturaId, Valor = plano.ValorMensal }, tx);

            await tx.CommitAsync();
            await _audit.RegistrarAsync(usuarioId, clienteId, "SELF_SERVICE", solicitacaoId, "CADASTRO_FINALIZADO", new { tenantId, plano = plano.Nome }, true, ip, "ADMINISTRADOR_CLIENTE");
            return ApiResponse<CadastroSelfServiceResultadoDto>.Ok(new CadastroSelfServiceResultadoDto { SolicitacaoId = solicitacaoId, TenantId = tenantId, ClienteId = clienteId, AssinaturaId = assinaturaId, UsuarioAdminId = usuarioId, LoginUrl = "/Account/Login", OnboardingUrl = "/Onboarding" }, "Cadastro finalizado com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao finalizar cadastro self-service");
            return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("Não foi possível finalizar o cadastro.", 500);
        }
    }

    public async Task<ApiResponse<WhiteLabelConfiguracaoDto>> ObterWhiteLabelAsync(Guid? tenantId = null)
    {
        var ctx = await _tenantContext.ObterAtualAsync(tenantId);
        if (!ctx.Success || ctx.Data?.TenantId is null) return ApiResponse<WhiteLabelConfiguracaoDto>.Fail(ctx.Message, ctx.StatusCode);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var wl = await cn.QueryFirstOrDefaultAsync<WhiteLabelConfiguracaoDto>(@"select tenant_id as ""TenantId"", coalesce(nome_plataforma,'PlantãoPro') as ""NomePlataforma"", coalesce(cliente_nome,'') as ""ClienteNome"", coalesce(slogan,'') as ""Slogan"", coalesce(logo_url,'') as ""LogoUrl"", coalesce(logo_reduzida_url,'') as ""LogoReduzidaUrl"", coalesce(favicon_url,'') as ""FaviconUrl"", coalesce(cor_primaria,'#0d6efd') as ""CorPrimaria"", coalesce(cor_secundaria,'#20c997') as ""CorSecundaria"", coalesce(cor_fundo,'#f8fafc') as ""CorFundo"", coalesce(cor_menu,'#0f172a') as ""CorMenu"", coalesce(tema,'claro') as ""Tema"", coalesce(email_remetente,'') as ""EmailRemetente"", coalesce(texto_boas_vindas,'') as ""TextoBoasVindas"", coalesce(texto_rodape,'') as ""TextoRodape"", coalesce(login_banner_url,'') as ""LoginBannerUrl"" from plantaopro.tenant_white_label where tenant_id=@tenantId and reg_status='A' limit 1", new { tenantId = ctx.Data.TenantId.Value });
        return ApiResponse<WhiteLabelConfiguracaoDto>.Ok(wl ?? new WhiteLabelConfiguracaoDto { TenantId = ctx.Data.TenantId.Value, ClienteNome = ctx.Data.TenantNome });
    }

    public async Task<ApiResponse<WhiteLabelConfiguracaoDto>> SalvarWhiteLabelAsync(Guid tenantId, WhiteLabelConfiguracaoDto request, string? ip)
    {
        try
        {
            if (!CorValida(request.CorPrimaria) || !CorValida(request.CorSecundaria) || !CorValida(request.CorFundo) || !CorValida(request.CorMenu)) return ApiResponse<WhiteLabelConfiguracaoDto>.Fail("Cores devem estar no formato hexadecimal #RRGGBB.", 400);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.tenant_white_label(id,tenant_id,nome_plataforma,cliente_nome,slogan,logo_url,logo_reduzida_url,favicon_url,cor_primaria,cor_secundaria,cor_fundo,cor_menu,tema,email_remetente,texto_boas_vindas,texto_rodape,login_banner_url,reg_date,reg_status)
values(gen_random_uuid(),@tenantId,@NomePlataforma,@ClienteNome,@Slogan,@LogoUrl,@LogoReduzidaUrl,@FaviconUrl,@CorPrimaria,@CorSecundaria,@CorFundo,@CorMenu,@Tema,@EmailRemetente,@TextoBoasVindas,@TextoRodape,@LoginBannerUrl,now(),'A')
on conflict (tenant_id) where reg_status='A' do update set nome_plataforma=excluded.nome_plataforma, cliente_nome=excluded.cliente_nome, slogan=excluded.slogan, logo_url=excluded.logo_url, logo_reduzida_url=excluded.logo_reduzida_url, favicon_url=excluded.favicon_url, cor_primaria=excluded.cor_primaria, cor_secundaria=excluded.cor_secundaria, cor_fundo=excluded.cor_fundo, cor_menu=excluded.cor_menu, tema=excluded.tema, email_remetente=excluded.email_remetente, texto_boas_vindas=excluded.texto_boas_vindas, texto_rodape=excluded.texto_rodape, login_banner_url=excluded.login_banner_url, reg_update=now()", new { tenantId, request.NomePlataforma, request.ClienteNome, request.Slogan, request.LogoUrl, request.LogoReduzidaUrl, request.FaviconUrl, request.CorPrimaria, request.CorSecundaria, request.CorFundo, request.CorMenu, request.Tema, request.EmailRemetente, request.TextoBoasVindas, request.TextoRodape, request.LoginBannerUrl });
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), null, "WHITE_LABEL", tenantId, "ALTERAR_WHITE_LABEL", new { request.NomePlataforma, request.Tema }, true, ip, "CONFIGURACAO");
            request.TenantId = tenantId;
            return ApiResponse<WhiteLabelConfiguracaoDto>.Ok(request, "White label salvo com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar white label TenantId={TenantId}", tenantId);
            return ApiResponse<WhiteLabelConfiguracaoDto>.Fail("Não foi possível salvar o white label.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<PerfilDto>>> ListarPerfisAsync()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var tenantId = ctx.Data?.TenantId;
        var rows = await cn.QueryAsync<PerfilDto>(@"select id as ""Id"", tenant_id as ""TenantId"", cliente_id as ""ClienteId"", coalesce(codigo,'') as ""Codigo"", coalesce(nome,'') as ""Nome"", coalesce(descricao,'') as ""Descricao"", base_sistema as ""BaseSistema"", customizado as ""Customizado"", coalesce(status,'') as ""Status"" from plantaopro.perfis where reg_status='A' and (tenant_id is null or tenant_id=@tenantId) order by base_sistema desc,nome limit 200", new { tenantId });
        return ApiResponse<IEnumerable<PerfilDto>>.Ok(rows);
    }

    public async Task<ApiResponse<Guid>> SalvarPerfilAsync(Guid? id, PerfilRequest request, string? ip)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<Guid>.Fail("Nome do perfil é obrigatório.", 400);
            var ctx = await _tenantContext.ObterAtualAsync();
            if (!ctx.Success || ctx.Data?.TenantId is null) return ApiResponse<Guid>.Fail(ctx.Message, ctx.StatusCode);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var perfilId = id ?? Guid.NewGuid();
            if (id.HasValue)
            {
                var baseSistema = await cn.ExecuteScalarAsync<bool>("select coalesce(base_sistema,false) from plantaopro.perfis where id=@id and reg_status='A'", new { id });
                if (baseSistema) return ApiResponse<Guid>.Fail("Perfis base não podem ser editados pelo tenant.", 400);
                await cn.ExecuteAsync("update plantaopro.perfis set nome=@Nome, descricao=@Descricao, reg_update=now() where id=@id and tenant_id=@tenantId and reg_status='A'", new { id, request.Nome, request.Descricao, tenantId = ctx.Data.TenantId.Value });
            }
            else
            {
                var codigo = string.IsNullOrWhiteSpace(request.Codigo) ? TenantContextService.Slug(request.Nome).Replace('-', '_').ToUpperInvariant() : request.Codigo.Trim().ToUpperInvariant();
                await cn.ExecuteAsync("insert into plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_date,reg_status) values(@perfilId,@tenantId,@clienteId,@codigo,@Nome,@Descricao,false,true,'ATIVO',now(),'A')", new { perfilId, tenantId = ctx.Data.TenantId.Value, clienteId = ctx.Data.ClienteId, codigo, request.Nome, request.Descricao });
            }
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data.ClienteId, "PERFIL", perfilId, id.HasValue ? "EDITAR_PERFIL" : "CRIAR_PERFIL", new { request.Nome }, true, ip, "ADMINISTRADOR_CLIENTE");
            return ApiResponse<Guid>.Ok(perfilId, "Perfil salvo com sucesso.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar perfil");
            return ApiResponse<Guid>.Fail("Não foi possível salvar o perfil.", 500);
        }
    }

    public async Task<ApiResponse<string>> AtualizarPermissoesPerfilAsync(Guid id, PerfilPermissoesRequest request, string? ip)
    {
        try
        {
            var ctx = await _tenantContext.ObterAtualAsync();
            if (!ctx.Success) return ApiResponse<string>.Fail(ctx.Message, ctx.StatusCode);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            await cn.ExecuteAsync("update plantaopro.perfil_permissoes set reg_status='I', reg_update=now() where perfil_id=@id and reg_status='A'", new { id }, tx);
            foreach (var permissaoId in request.PermissoesPermitidas.Distinct())
            {
                await cn.ExecuteAsync("insert into plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,reg_date,reg_status) values(gen_random_uuid(),@id,@permissaoId,true,now(),'A')", new { id, permissaoId }, tx);
            }
            await tx.CommitAsync();
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data?.ClienteId, "PERFIL", id, "ALTERAR_PERMISSOES", new { total = request.PermissoesPermitidas.Length }, true, ip, "ADMINISTRADOR_CLIENTE");
            return ApiResponse<string>.Ok("ok", "Permissões atualizadas.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar permissões do perfil {PerfilId}", id);
            return ApiResponse<string>.Fail("Não foi possível atualizar as permissões.", 500);
        }
    }

    public async Task<ApiResponse<ParametrizacoesClienteDto>> ObterParametrizacoesAsync()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.TenantId is null) return ApiResponse<ParametrizacoesClienteDto>.Fail(ctx.Message, ctx.StatusCode);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var parametros = await cn.QueryAsync<(string Categoria, string Chave, string Valor)>("select categoria as Categoria, chave as Chave, valor as Valor from plantaopro.tenant_parametros where tenant_id=@tenantId and reg_status='A'", new { tenantId = ctx.Data.TenantId.Value });
        var dto = new ParametrizacoesClienteDto { TenantId = ctx.Data.TenantId.Value };
        foreach (var item in parametros)
        {
            Categoria(dto, item.Categoria)[item.Chave] = item.Valor;
        }
        var wl = await ObterWhiteLabelAsync(ctx.Data.TenantId.Value);
        if (wl.Data is not null) dto.WhiteLabel = wl.Data;
        return ApiResponse<ParametrizacoesClienteDto>.Ok(dto);
    }

    public async Task<ApiResponse<string>> SalvarParametrosAsync(string categoria, ParametrosCategoriaRequest request, string? ip)
    {
        try
        {
            var ctx = await _tenantContext.ObterAtualAsync();
            if (!ctx.Success || ctx.Data?.TenantId is null) return ApiResponse<string>.Fail(ctx.Message, ctx.StatusCode);
            if (request.Valores.Any(x => string.IsNullOrWhiteSpace(x.Key))) return ApiResponse<string>.Fail("Chaves de parâmetros são obrigatórias.", 400);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            foreach (var item in request.Valores)
            {
                var parametros = new { tenantId = ctx.Data.TenantId.Value, categoria, chave = item.Key, valor = item.Value ?? string.Empty };
                var atualizados = await cn.ExecuteAsync("update plantaopro.tenant_parametros set valor=@valor, reg_update=now() where tenant_id=@tenantId and lower(categoria)=lower(@categoria) and lower(chave)=lower(@chave) and reg_status='A'", parametros, tx);
                if (atualizados == 0)
                {
                    await cn.ExecuteAsync("insert into plantaopro.tenant_parametros(id,tenant_id,categoria,chave,valor,tipo,status,reg_date,reg_status) values(gen_random_uuid(),@tenantId,@categoria,@chave,@valor,'texto','ATIVO',now(),'A')", parametros, tx);
                }
            }
            await tx.CommitAsync();
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data.ClienteId, "PARAMETRIZACOES", ctx.Data.TenantId.Value, "ALTERAR_PARAMETROS", new { categoria, total = request.Valores.Count }, true, ip, "ADMINISTRADOR_CLIENTE");
            return ApiResponse<string>.Ok("ok", "Parametrizações salvas.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar parametrizações {Categoria}", categoria);
            return ApiResponse<string>.Fail("Não foi possível salvar parametrizações.", 500);
        }
    }

    public async Task<ApiResponse<MinhaAssinaturaDto>> MinhaAssinaturaAsync()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.ClienteId is null) return ApiResponse<MinhaAssinaturaDto>.Fail(ctx.Message, ctx.StatusCode);
        await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
        var dto = await cn.QueryFirstOrDefaultAsync<MinhaAssinaturaDto>(@"select a.id as ""AssinaturaId"", a.cliente_id as ""ClienteId"", a.plano_id as ""PlanoId"", coalesce(p.nome,'') as ""PlanoNome"", coalesce(a.status,'') as ""Status"", a.valor_contratado as ""ValorContratado"", a.data_inicio as ""DataInicio"", a.data_fim as ""DataFim"" from plantaopro.assinaturas a join plantaopro.planos p on p.id=a.plano_id where a.cliente_id=@clienteId and a.reg_status='A' order by a.reg_date desc limit 1", new { clienteId = ctx.Data.ClienteId.Value });
        return dto is null ? ApiResponse<MinhaAssinaturaDto>.Fail("Assinatura não encontrada.", 404) : ApiResponse<MinhaAssinaturaDto>.Ok(dto);
    }

    public async Task<ApiResponse<string>> SolicitarMudancaPlanoAsync(string tipo, SolicitacaoMudancaPlanoRequest request, string? ip)
    {
        try
        {
            var assinatura = await MinhaAssinaturaAsync();
            if (!assinatura.Success || assinatura.Data is null) return ApiResponse<string>.Fail(assinatura.Message, assinatura.StatusCode);
            var ctx = await _tenantContext.ObterAtualAsync();
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var destino = await cn.QueryFirstOrDefaultAsync<PlanoPublicoDto>("select id as \"Id\", coalesce(nome,'') as \"Nome\", limite_medicos as \"LimiteMedicos\", limite_hospitais as \"LimiteHospitais\", limite_plantoes_mes as \"LimitePlantoesMes\", coalesce(limite_usuarios,0) as \"LimiteUsuarios\" from plantaopro.planos where id=@id and reg_status='A' and upper(status)='ATIVO'", new { id = request.PlanoDestinoId });
            if (destino is null) return ApiResponse<string>.Fail("Plano destino inválido.", 400);
            if (string.Equals(tipo, "DOWNGRADE", StringComparison.OrdinalIgnoreCase))
            {
                var uso = await ObterUsoPlanoAsync();
                if (uso.Data is not null && ((destino.LimiteMedicos > 0 && uso.Data.MedicosUsados > destino.LimiteMedicos) || (destino.LimiteHospitais > 0 && uso.Data.HospitaisUsados > destino.LimiteHospitais) || (destino.LimitePlantoesMes > 0 && uso.Data.PlantoesMesUsados > destino.LimitePlantoesMes) || (destino.LimiteUsuarios > 0 && uso.Data.UsuariosUsados > destino.LimiteUsuarios)))
                {
                    return ApiResponse<string>.Fail("Downgrade bloqueado porque o uso atual excede os limites do plano destino.", 400);
                }
                await cn.ExecuteAsync("insert into plantaopro.downgrade_solicitacoes(id,tenant_id,cliente_id,assinatura_id,plano_atual_id,plano_destino_id,motivo,impacto_validado,status,solicitado_por,reg_date,reg_status) values(gen_random_uuid(),@tenantId,@clienteId,@assinaturaId,@planoAtualId,@planoDestinoId,@motivo,true,'SOLICITADO',@usuarioId,now(),'A')", new { tenantId = ctx.Data?.TenantId, clienteId = assinatura.Data.ClienteId, assinaturaId = assinatura.Data.AssinaturaId, planoAtualId = assinatura.Data.PlanoId, planoDestinoId = request.PlanoDestinoId, motivo = request.Motivo ?? string.Empty, usuarioId = _tenantContext.ObterUsuarioId() });
            }
            else
            {
                await cn.ExecuteAsync("insert into plantaopro.upgrade_solicitacoes(id,tenant_id,cliente_id,assinatura_id,plano_atual_id,plano_destino_id,motivo,status,solicitado_por,reg_date,reg_status) values(gen_random_uuid(),@tenantId,@clienteId,@assinaturaId,@planoAtualId,@planoDestinoId,@motivo,'SOLICITADO',@usuarioId,now(),'A')", new { tenantId = ctx.Data?.TenantId, clienteId = assinatura.Data.ClienteId, assinaturaId = assinatura.Data.AssinaturaId, planoAtualId = assinatura.Data.PlanoId, planoDestinoId = request.PlanoDestinoId, motivo = request.Motivo ?? string.Empty, usuarioId = _tenantContext.ObterUsuarioId() });
            }
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), assinatura.Data.ClienteId, "ASSINATURA", assinatura.Data.AssinaturaId, "SOLICITAR_" + tipo.ToUpperInvariant(), new { request.PlanoDestinoId }, true, ip, "ADMINISTRADOR_CLIENTE");
            return ApiResponse<string>.Ok("ok", "Solicitação registrada para avaliação comercial.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao solicitar mudança de plano {Tipo}", tipo);
            return ApiResponse<string>.Fail("Não foi possível solicitar mudança de plano.", 500);
        }
    }

    public async Task<ApiResponse<UsoPlanoDto>> ObterUsoPlanoAsync()
    {
        var ctx = await _tenantContext.ObterAtualAsync();
        if (!ctx.Success || ctx.Data?.ClienteId is null) return ApiResponse<UsoPlanoDto>.Fail(ctx.Message, ctx.StatusCode);
        return await _assinaturaGuard.ObterUsoPlanoAsync(ctx.Data.ClienteId.Value);
    }

    private async Task<Guid> GarantirPerfilAdminClienteAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction tx, Guid tenantId, Guid clienteId)
    {
        var perfilId = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.perfis where tenant_id=@tenantId and codigo='ADMINISTRADOR_CLIENTE' and reg_status='A' limit 1", new { tenantId }, tx);
        if (perfilId.HasValue) return perfilId.Value;
        var id = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_date,reg_status) values(@id,@tenantId,@clienteId,'ADMINISTRADOR_CLIENTE','Administrador cliente','Administrador do tenant',false,true,'ATIVO',now(),'A')", new { id, tenantId, clienteId }, tx);
        await cn.ExecuteAsync(@"insert into plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,reg_date,reg_status)
select gen_random_uuid(),@id,p.id,true,now(),'A' from plantaopro.permissoes p where p.codigo in ('DASHBOARD.VISUALIZAR','CONFIGURACOES.CONFIGURAR','WHITE_LABEL.CONFIGURAR','PLANOS.VISUALIZAR','ASSINATURAS.VISUALIZAR','USUARIOS.ADMINISTRAR') or p.codigo like 'MEDICOS.%' or p.codigo like 'HOSPITAIS.%' or p.codigo like 'ESPECIALIDADES.%'", new { id }, tx);
        return id;
    }

    private static async Task CriarWhiteLabelPadraoAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction tx, Guid tenantId, string clienteNome)
    {
        await cn.ExecuteAsync("insert into plantaopro.tenant_white_label(id,tenant_id,nome_plataforma,cliente_nome,slogan,texto_boas_vindas,texto_rodape,reg_date,reg_status) values(gen_random_uuid(),@tenantId,'PlantãoPro',@clienteNome,'Gestão inteligente de plantões','Bem-vindo ao seu ambiente PlantãoPro','PlantãoPro SaaS',now(),'A')", new { tenantId, clienteNome }, tx);
    }

    private static async Task CriarOnboardingAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction tx, Guid tenantId, Guid clienteId)
    {
        var onboardingId = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.tenant_onboarding(id,tenant_id,cliente_id,status,progresso,proxima_acao,reg_date,reg_status) values(@onboardingId,@tenantId,@clienteId,'EM_ANDAMENTO',0,'Completar dados da empresa',now(),'A')", new { onboardingId, tenantId, clienteId }, tx);
        var etapas = new[] { "Completar dados da empresa", "Configurar identidade visual", "Cadastrar primeiro hospital/unidade", "Cadastrar especialidades", "Convidar usuários", "Cadastrar ou importar médicos", "Criar primeiro plantão", "Publicar primeiro plantão", "Validar fluxo médico", "Configurar financeiro", "Finalizar implantação" };
        for (var i = 0; i < etapas.Length; i++)
        {
            await cn.ExecuteAsync("insert into plantaopro.tenant_onboarding_checklist(id,onboarding_id,tenant_id,cliente_id,codigo,titulo,descricao,ordem,obrigatorio,link_acao,status,reg_date,reg_status) values(gen_random_uuid(),@onboardingId,@tenantId,@clienteId,@codigo,@titulo,@descricao,@ordem,true,@link,'PENDENTE',now(),'A')", new { onboardingId, tenantId, clienteId, codigo = "ETAPA_" + (i + 1).ToString("00"), titulo = etapas[i], descricao = "Etapa de implantação self-service", ordem = i + 1, link = LinkEtapa(i + 1) }, tx);
        }
    }

    private static string LinkEtapa(int etapa)
    {
        switch (etapa)
        {
            case 2: return "/WhiteLabel";
            case 3: return "/Hospitais/Create";
            case 4: return "/Especialidades/Create";
            case 5: return "/Usuario";
            case 6: return "/Medicos/Create";
            case 7: return "/Plantoes/Create";
            case 10: return "/Parametrizacoes/Financeiro";
            default: return "/Onboarding";
        }
    }

    private static List<string> ValidarCadastro(CadastroSelfServiceRequest request)
    {
        var erros = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Empresa.RazaoSocial)) erros.Add("Razão social é obrigatória.");
        if (SomenteDigitos(request.Empresa.Cnpj).Length != 14) erros.Add("CNPJ deve ter 14 dígitos.");
        if (request.Plano.PlanoId == Guid.Empty) erros.Add("Plano é obrigatório.");
        if (!request.Plano.AceiteTermos || !request.Plano.AceitePrivacidade) erros.Add("Aceite de termos e política de privacidade é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.UsuarioAdmin.Email)) erros.Add("E-mail do administrador é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.UsuarioAdmin.Senha) || request.UsuarioAdmin.Senha.Length < 8) erros.Add("Senha deve ter no mínimo 8 caracteres.");
        return erros;
    }

    private static IEnumerable<string> RecursosDoPlano(PlanoPublicoDto p)
    {
        yield return LimiteTexto(p.LimiteMedicos) + " médicos";
        yield return LimiteTexto(p.LimiteHospitais) + " hospitais/unidades";
        yield return LimiteTexto(p.LimitePlantoesMes) + " plantões/mês";
        if (p.PermiteMobile) yield return "API Mobile";
        if (p.PermiteBi) yield return "BI";
        if (p.PermiteWhiteLabel) yield return "White label";
    }

    private static void AdicionarLinha(ICollection<PlanoComparativoDto> linhas, IEnumerable<PlanoPublicoDto> planos, string grupo, string recurso, Func<PlanoPublicoDto, string> valor)
    {
        var linha = new PlanoComparativoDto { Grupo = grupo, Recurso = recurso };
        foreach (var plano in planos) linha.ValoresPorPlano[plano.Nome] = valor(plano);
        linhas.Add(linha);
    }

    private static string LimiteTexto(int valor) => valor <= 0 ? "Ilimitado" : valor.ToString();
    private static string SimNao(bool valor) => valor ? "Incluído" : "Não incluído";
    private static string SomenteDigitos(string valor) => new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
    private static bool CorValida(string valor) => !string.IsNullOrWhiteSpace(valor) && valor.Length == 7 && valor[0] == '#' && valor.Skip(1).All(Uri.IsHexDigit);

    private static Dictionary<string, string> Categoria(ParametrizacoesClienteDto dto, string categoria)
    {
        if (string.Equals(categoria, "OPERACIONAL", StringComparison.OrdinalIgnoreCase)) return dto.Operacionais;
        if (string.Equals(categoria, "FINANCEIRA", StringComparison.OrdinalIgnoreCase)) return dto.Financeiras;
        if (string.Equals(categoria, "NOTIFICACOES", StringComparison.OrdinalIgnoreCase)) return dto.Notificacoes;
        if (string.Equals(categoria, "LGPD", StringComparison.OrdinalIgnoreCase)) return dto.Lgpd;
        return dto.Operacionais;
    }
}
