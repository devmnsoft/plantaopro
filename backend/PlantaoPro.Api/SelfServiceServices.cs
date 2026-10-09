using System.Security.Claims;
using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;
using PlantaoPro.Api.Security;

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
            var tenantClaim = LerGuidClaim("tenant_id");
            if (tenantRota.HasValue && !UsuarioEhAdminGlobal() && tenantClaim != tenantRota)
                return ApiResponse<TenantContextDto>.Fail("Acesso negado: o tenant solicitado não pertence ao usuário autenticado.", 403);
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

    public bool UsuarioEhAdminGlobal()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole("ADMINISTRADOR_GLOBAL") == true ||
            user?.Claims.Any(c => (c.Type == ClaimTypes.Role || c.Type == "role") && string.Equals(c.Value, "ADMINISTRADOR_GLOBAL", StringComparison.OrdinalIgnoreCase)) == true;
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
from plantaopro.usuarios_perfis up
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
        var normalizado = string.Concat((valor ?? string.Empty).Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-'));
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

    public async Task<ApiResponse<IEnumerable<PlanoFaqDto>>> FaqPlanosAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var rows = (await cn.QueryAsync<PlanoFaqDto>(@"select f.plano_id as ""PlanoId"", coalesce(p.nome,'') as ""PlanoNome"", coalesce(f.pergunta,'') as ""Pergunta"", coalesce(f.resposta,'') as ""Resposta"", f.ordem as ""Ordem""
from plantaopro.plano_faq f
left join plantaopro.planos p on p.id=f.plano_id
where f.reg_status='A' and upper(coalesce(f.status,'ATIVO'))='ATIVO'
order by f.ordem, f.reg_date
limit 50")).ToList();
            if (rows.Count == 0)
            {
                rows.Add(new PlanoFaqDto { Pergunta = "Posso começar sem implantação manual?", Resposta = "Sim. O cadastro self-service provisiona tenant, cliente, assinatura, administrador, LGPD, white label padrão e onboarding.", Ordem = 1 });
                rows.Add(new PlanoFaqDto { Pergunta = "White label está disponível em todos os planos?", Resposta = "White label é liberado conforme regra comercial do plano, com fallback visual PlantãoPro quando indisponível.", Ordem = 2 });
                rows.Add(new PlanoFaqDto { Pergunta = "Como funcionam upgrade e downgrade?", Resposta = "Upgrade registra solicitação comercial; downgrade valida limites para evitar perda operacional.", Ordem = 3 });
            }
            return ApiResponse<IEnumerable<PlanoFaqDto>>.Ok(rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar FAQ público de planos");
            return ApiResponse<IEnumerable<PlanoFaqDto>>.Fail("Não foi possível carregar as dúvidas de planos.", 500);
        }
    }

    public async Task<ApiResponse<CadastroSelfServiceResultadoDto>> FinalizarCadastroAsync(CadastroSelfServiceRequest request, string? ip, string? userAgent)
    {
        var erros = ValidarCadastro(request);
        if (erros.Count > 0) return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("Verifique os dados do cadastro.", 400, erros);
        // B5: slug único global (ux_tenants_slug) com retry limitado: dois cadastros
        // simultâneos com o mesmo nome fantasia não viram 500 — o segundo tenta
        // com sufixo; duplicata real (CNPJ/e-mail) vira 409 honesto.
        for (var tentativa = 0; tentativa < 3; tentativa++)
        {
            var resultado = await TentarProvisionarAsync(request, null, "ATIVA", null,
                "Criada via self-service", true, "SELF_SERVICE", "ADMINISTRADOR_CLIENTE", ip, userAgent,
                tentativa > 0);
            if (resultado is not null) return resultado;
        }
        return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("Nome fantasia em disputa simultânea; ajuste e tente novamente.", 409);
    }

    /// <summary>
    /// B5: provisionamento manual B2B (MNSOFT) sobre o MESMO núcleo do
    /// self-service. TRIAL exige DiasTrial explícito (1..90, sem padrão
    /// silencioso); data_fim = trial + 30 dias de janela de conversão
    /// (documentado: o gate de trial dispara antes do de vigência).
    /// Trial não gera cobrança inicial (billing do trial = pendência B6).
    /// </summary>
    public async Task<ApiResponse<CadastroSelfServiceResultadoDto>> ProvisionarManualAsync(ProvisionarClienteRequest request, string? ip)
    {
        var mapped = new CadastroSelfServiceRequest
        {
            Empresa = request.Empresa,
            Plano = new CadastroPlanoRequest
            {
                PlanoId = request.PlanoId,
                Periodicidade = string.IsNullOrWhiteSpace(request.Periodicidade) ? "MENSAL" : request.Periodicidade.Trim().ToUpperInvariant(),
                AceiteTermos = request.AceiteTermos,
                AceitePrivacidade = request.AceitePrivacidade,
                ConsentimentoLgpd = false
            },
            UsuarioAdmin = request.UsuarioAdmin
        };
        var erros = ValidarCadastro(mapped);
        if (erros.Count > 0) return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("Verifique os dados do provisionamento.", 400, erros);
        var status = (request.StatusInicial ?? "TRIAL").Trim().ToUpperInvariant();
        if (status is not ("TRIAL" or "ATIVA")) return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("Status inicial deve ser TRIAL ou ATIVA.", 400);
        if (status == "TRIAL" && (request.DiasTrial is < 1 or > 90)) return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("DiasTrial deve estar entre 1 e 90 para TRIAL.", 400);
        var trialFim = status == "TRIAL" ? DateTime.UtcNow.Date.AddDays(request.DiasTrial) : (DateTime?)null;
        for (var tentativa = 0; tentativa < 3; tentativa++)
        {
            var resultado = await TentarProvisionarAsync(mapped, _tenantContext.ObterUsuarioId(), status, trialFim,
                "Criada via provisionamento manual B2B", status == "ATIVA", "ADMIN_MANUAL", "ADMINISTRADOR_GLOBAL", ip, null,
                tentativa > 0);
            if (resultado is not null) return resultado;
        }
        return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("Nome fantasia em disputa simultânea; ajuste e tente novamente.", 409);
    }

    /// <summary>
    /// Núcleo único de provisionamento (self-service + admin): valida
    /// duplicatas em-tx, insere o mundo do cliente em UMA transação
    /// (tudo-ou-nada) e retorna null só quando o slug colidiu (pedido de
    /// retry com sufixo). Corrida real de CNPJ/e-mail vira 409 via índice
    /// único (ux_clientes_cnpj_ativo / ux_usuarios_email_normalizado).
    /// </summary>
    private async Task<ApiResponse<CadastroSelfServiceResultadoDto>?> TentarProvisionarAsync(
        CadastroSelfServiceRequest request, Guid? atorId, string statusAssinatura,
        DateTime? dataTrialFim, string observacaoAssinatura,
        bool pagamentoInicial, string origem, string perfilAuditoria,
        string? ip, string? userAgent, bool comSufixo)
    {
        try
        {
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
            var slugBase = TenantContextService.Slug(request.Empresa.NomeFantasia);
            var slug = comSufixo ? $"{slugBase}-{solicitacaoId.ToString("N")[..4]}" : slugBase;
            var senhaHash = Security.PasswordHashService.Hash(request.UsuarioAdmin.Senha);
            var fim = DateTime.UtcNow.Date.AddMonths(1);
            if (statusAssinatura == "TRIAL" && dataTrialFim.HasValue) fim = dataTrialFim.Value.AddDays(30);

            await cn.ExecuteAsync(@"insert into plantaopro.cadastro_cliente_solicitacoes(id,plano_id,nome_fantasia,razao_social,cnpj,segmento,qtd_medicos,qtd_hospitais,volume_plantoes_mes,cidade,uf,telefone,email_corporativo,responsavel_nome,responsavel_email,responsavel_telefone,responsavel_cargo,periodicidade,aceite_termos,aceite_privacidade,consentimento_lgpd,status,reg_date,reg_status)
values(@solicitacaoId,@PlanoId,@NomeFantasia,@RazaoSocial,@Cnpj,@Segmento,@QuantidadeMedicos,@QuantidadeHospitais,@VolumePlantoesMes,@Cidade,@Uf,@Telefone,@EmailCorporativo,@AdminNome,@AdminEmail,@AdminTelefone,@Cargo,@Periodicidade,@AceiteTermos,@AceitePrivacidade,@ConsentimentoLgpd,'FINALIZADO',now(),'A')", new { solicitacaoId, request.Plano.PlanoId, request.Empresa.NomeFantasia, request.Empresa.RazaoSocial, Cnpj = cnpjLimpo, request.Empresa.Segmento, request.Empresa.QuantidadeMedicos, request.Empresa.QuantidadeHospitais, request.Empresa.VolumePlantoesMes, request.Empresa.Cidade, request.Empresa.Uf, request.Empresa.Telefone, request.Empresa.EmailCorporativo, AdminNome = request.UsuarioAdmin.Nome, AdminEmail = request.UsuarioAdmin.Email, AdminTelefone = request.UsuarioAdmin.Telefone, request.UsuarioAdmin.Cargo, request.Plano.Periodicidade, request.Plano.AceiteTermos, request.Plano.AceitePrivacidade, request.Plano.ConsentimentoLgpd }, tx);

            await cn.ExecuteAsync("insert into plantaopro.tenants(id,cliente_id,nome,slug,status,plano_id,subdominio,reg_date,reg_status) values(@tenantId,@clienteId,@nome,@slug,'ATIVO',@planoId,@slug,now(),'A')", new { tenantId, clienteId, nome = request.Empresa.NomeFantasia, slug, planoId = request.Plano.PlanoId }, tx);
            await cn.ExecuteAsync(@"insert into plantaopro.clientes(id,razao_social,nome_fantasia,cnpj,email,telefone,cidade,estado,plano_id,status,reg_status,reg_date)
values(@clienteId,@RazaoSocial,@NomeFantasia,@Cnpj,@Email,@Telefone,@Cidade,@Uf,@PlanoId,'ATIVO','A',now())", new { clienteId, request.Empresa.RazaoSocial, request.Empresa.NomeFantasia, Cnpj = cnpjLimpo, Email = request.Empresa.EmailCorporativo, request.Empresa.Telefone, request.Empresa.Cidade, request.Empresa.Uf, request.Plano.PlanoId }, tx);
            await cn.ExecuteAsync(@"insert into plantaopro.assinaturas(id,tenant_id,cliente_id,plano_id,data_inicio,data_fim,data_trial_fim,status,valor_contratado,dia_vencimento,observacoes,periodicidade,reg_status,reg_date)
values(@assinaturaId,@tenantId,@clienteId,@PlanoId,current_date,@Fim,@TrialFim,@Status,@Valor,@Dia,@Observacoes,@Periodicidade,'A',now())", new { assinaturaId, tenantId, clienteId, request.Plano.PlanoId, Fim = fim, TrialFim = (DateTime?)dataTrialFim, Status = statusAssinatura, Valor = plano.ValorMensal, Dia = DateTime.UtcNow.Day, Observacoes = observacaoAssinatura, request.Plano.Periodicidade }, tx);
            await cn.ExecuteAsync(@"insert into plantaopro.usuarios(id,nome,email,email_normalizado,telefone,senha_hash,senha_alteracao_obrigatoria,preferencias_notificacao,cliente_id,status,reg_status,reg_date)
values(@usuarioId,@Nome,@Email,upper(@Email),@Telefone,@SenhaHash,false,'{}',@clienteId,'ATIVO','A',now())", new { usuarioId, request.UsuarioAdmin.Nome, request.UsuarioAdmin.Email, request.UsuarioAdmin.Telefone, SenhaHash = senhaHash, clienteId }, tx);

            var perfilId = await GarantirPerfilAdminClienteAsync(cn, tx, tenantId, clienteId);
            await cn.ExecuteAsync("insert into plantaopro.usuarios_perfis(id,tenant_id,cliente_id,usuario_id,perfil_id,reg_date,reg_status) values(gen_random_uuid(),@tenantId,@clienteId,@usuarioId,@perfilId,now(),'A')", new { tenantId, clienteId, usuarioId, perfilId }, tx);
            await CriarWhiteLabelPadraoAsync(cn, tx, tenantId, request.Empresa.NomeFantasia);
            await CriarOnboardingAsync(cn, tx, tenantId, clienteId);
            var lgpdAceito = string.Equals(origem, "SELF_SERVICE", StringComparison.OrdinalIgnoreCase) && request.Plano.ConsentimentoLgpd;
            // B5: base_legal NOT NULL — self-service com aceite = 'consentimento';
            // manual B2B (aceito=false, a coletar no primeiro acesso) = 'contrato'.
            // consentido/ip atendem ao shape vigente da tabela (v2331 completa o resto).
            var baseLegal = lgpdAceito ? "consentimento" : "contrato";
            await cn.ExecuteAsync(@"insert into plantaopro.lgpd_consentimentos(id,tenant_id,cliente_id,usuario_id,titular_email,finalidade,base_legal,versao_politica,aceito,consentido,origem,ip,ip_origem,user_agent,reg_date,reg_status)
values(gen_random_uuid(),@tenantId,@clienteId,@usuarioId,@Email,'cadastro_self_service',@baseLegal,'1.0',@aceito,@aceito,@origem,@ip,@ip,@ua,now(),'A')", new { tenantId, clienteId, usuarioId, request.UsuarioAdmin.Email, baseLegal, aceito = lgpdAceito, origem, ip = ip ?? string.Empty, ua = userAgent ?? string.Empty }, tx);
            if (pagamentoInicial)
            {
                await cn.ExecuteAsync(@"insert into plantaopro.cadastro_cliente_pagamentos_iniciais(id,solicitacao_id,cliente_id,assinatura_id,valor,status,vencimento,reg_date,reg_status)
values(gen_random_uuid(),@solicitacaoId,@clienteId,@assinaturaId,@Valor,'ABERTO',current_date+7,now(),'A')", new { solicitacaoId, clienteId, assinaturaId, Valor = plano.ValorMensal }, tx);
            }

            await tx.CommitAsync();
            await _audit.RegistrarAsync(atorId ?? usuarioId, clienteId, "SELF_SERVICE", solicitacaoId, "CADASTRO_FINALIZADO", new { tenantId, plano = plano.Nome, origem }, true, ip, perfilAuditoria);
            return ApiResponse<CadastroSelfServiceResultadoDto>.Ok(new CadastroSelfServiceResultadoDto { SolicitacaoId = solicitacaoId, TenantId = tenantId, ClienteId = clienteId, AssinaturaId = assinaturaId, UsuarioAdminId = usuarioId, LoginUrl = "/Account/Login", OnboardingUrl = "/Onboarding" }, "Cadastro finalizado com sucesso.");
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            // Corrida real de duplicata: índice único arbitra; slug tenta de novo,
            // CNPJ/e-mail viram 409 honesto (nunca 500, nunca duplicata).
            if (string.Equals(ex.ConstraintName, "ux_tenants_slug", StringComparison.OrdinalIgnoreCase)) return null;
            if (string.Equals(ex.ConstraintName, "ux_clientes_cnpj_ativo", StringComparison.OrdinalIgnoreCase))
                return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("CNPJ já cadastrado.", 409);
            if (string.Equals(ex.ConstraintName, "ux_usuarios_email_normalizado", StringComparison.OrdinalIgnoreCase))
                return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("E-mail do administrador já cadastrado.", 409);
            _logger.LogWarning(ex, "Conflito de unicidade no provisionamento");
            return ApiResponse<CadastroSelfServiceResultadoDto>.Fail("Registro duplicado simultâneo; tente novamente.", 409);
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
        var wl = await cn.QueryFirstOrDefaultAsync<WhiteLabelConfiguracaoDto>(@"select tenant_id as ""TenantId"", coalesce(nome_plataforma,'PlantãoPro') as ""NomePlataforma"", coalesce(cliente_nome,'') as ""ClienteNome"", coalesce(slogan,'') as ""Slogan"", coalesce(logo_url,'') as ""LogoUrl"", coalesce(logo_reduzida_url,'') as ""LogoReduzidaUrl"", coalesce(favicon_url,'') as ""FaviconUrl"", coalesce(cor_primaria,'#0757d9') as ""CorPrimaria"", coalesce(cor_secundaria,'#20c997') as ""CorSecundaria"", coalesce(cor_fundo,'#f8fafc') as ""CorFundo"", coalesce(cor_menu,'#0f172a') as ""CorMenu"", coalesce(tema,'claro') as ""Tema"", coalesce(email_remetente,'') as ""EmailRemetente"", coalesce(texto_boas_vindas,'') as ""TextoBoasVindas"", coalesce(texto_rodape,'') as ""TextoRodape"", coalesce(login_banner_url,'') as ""LoginBannerUrl"" from plantaopro.tenant_white_label where tenant_id=@tenantId and reg_status='A' limit 1", new { tenantId = ctx.Data.TenantId.Value });
        return ApiResponse<WhiteLabelConfiguracaoDto>.Ok(wl ?? new WhiteLabelConfiguracaoDto { TenantId = ctx.Data.TenantId.Value, ClienteNome = ctx.Data.TenantNome });
    }

    public async Task<ApiResponse<WhiteLabelConfiguracaoDto>> SalvarWhiteLabelAsync(Guid tenantId, WhiteLabelConfiguracaoDto request, string? ip)
    {
        try
        {
            var ctx = await _tenantContext.ObterAtualAsync(tenantId);
            if (!ctx.Success || ctx.Data?.TenantId != tenantId) return ApiResponse<WhiteLabelConfiguracaoDto>.Fail(ctx.Message, ctx.StatusCode);
            var validation = WhiteLabelSecurityValidator.Validate(request);
            if (validation is not null) return ApiResponse<WhiteLabelConfiguracaoDto>.Fail(validation, 400);
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
            var nome = request.Nome?.Trim() ?? string.Empty;
            var descricao = request.Descricao?.Trim() ?? string.Empty;
            if (nome.Length is < 3 or > 160) return ApiResponse<Guid>.Fail("Nome deve ter entre 3 e 160 caracteres.", 400);
            if (descricao.Length is < 10 or > 1000) return ApiResponse<Guid>.Fail("Descrição deve ter entre 10 e 1000 caracteres.", 400);
            var ctx = await _tenantContext.ObterAtualAsync();
            if (!ctx.Success || ctx.Data?.TenantId is null) return ApiResponse<Guid>.Fail(ctx.Message, ctx.StatusCode);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            var tenantId = ctx.Data.TenantId.Value;
            // Serializa nomes/códigos dentro do tenant. A checagem e a escrita precisam
            // compartilhar a transação para que dois submits concorrentes não criem duplicatas.
            await cn.ExecuteAsync("select pg_advisory_xact_lock(hashtextextended(@scope, 0))", new { scope = $"perfil:{tenantId:N}" }, tx);
            var perfilId = id ?? Guid.NewGuid();
            if (id.HasValue)
            {
                var affected = await cn.ExecuteAsync(@"update plantaopro.perfis
set nome=@nome, descricao=@descricao, reg_update=now(), updated_by=@actor
where id=@id and tenant_id=@tenantId and reg_status='A' and coalesce(base_sistema,false)=false", new { id, nome, descricao, tenantId, actor = _tenantContext.ObterUsuarioId() }, tx);
                if (affected == 0)
                {
                    await tx.RollbackAsync();
                    return ApiResponse<Guid>.Fail("Perfil não encontrado no tenant ou protegido pelo sistema.", 404);
                }
            }
            else
            {
                var codigo = string.IsNullOrWhiteSpace(request.Codigo) ? TenantContextService.Slug(nome).Replace('-', '_').ToUpperInvariant() : request.Codigo.Trim().ToUpperInvariant();
                if (!System.Text.RegularExpressions.Regex.IsMatch(codigo, "^[A-Z][A-Z0-9_]{1,79}$"))
                {
                    await tx.RollbackAsync();
                    return ApiResponse<Guid>.Fail("Código inválido. Use letras maiúsculas, números e sublinhado.", 400);
                }
                var duplicate = await cn.ExecuteScalarAsync<bool>(@"select exists(select 1 from plantaopro.perfis
where tenant_id=@tenantId and reg_status='A' and (upper(nome)=upper(@nome) or upper(codigo)=upper(@codigo)))", new { tenantId, nome, codigo }, tx);
                if (duplicate)
                {
                    await tx.RollbackAsync();
                    return ApiResponse<Guid>.Fail("Já existe um perfil ativo com este nome ou código no cliente.", 409);
                }
                await cn.ExecuteAsync("insert into plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_date,reg_status,created_by) values(@perfilId,@tenantId,@clienteId,@codigo,@nome,@descricao,false,true,'ATIVO',now(),'A',@actor)", new { perfilId, tenantId, clienteId = ctx.Data.ClienteId, codigo, nome, descricao, actor = _tenantContext.ObterUsuarioId() }, tx);
            }
            await tx.CommitAsync();
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data.ClienteId, "PERFIL", perfilId, id.HasValue ? "EDITAR_PERFIL" : "CRIAR_PERFIL", new { nome }, true, ip, "ADMINISTRADOR_CLIENTE");
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
            if (!ctx.Success || ctx.Data?.TenantId is null) return ApiResponse<string>.Fail(ctx.Message, ctx.StatusCode);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using var tx = await cn.BeginTransactionAsync();
            var profileExists = await cn.ExecuteScalarAsync<bool>(@"select exists(select 1 from plantaopro.perfis where id=@id and tenant_id=@tenantId and base_sistema=false and reg_status='A')", new { id, tenantId = ctx.Data.TenantId.Value }, tx);
            if (!profileExists)
            {
                await tx.RollbackAsync();
                return ApiResponse<string>.Fail("Perfil não encontrado no tenant ou protegido pelo sistema.", 404);
            }
            var permissionIds = request.PermissoesPermitidas.Distinct().ToArray();
            var validPermissionCount = await cn.ExecuteScalarAsync<int>(@"select count(*) from plantaopro.permissoes p
join plantaopro.modulos_sistema m on m.id=p.modulo_id and m.reg_status='A'
where p.id=any(@permissionIds) and p.reg_status='A'
and exists(select 1 from plantaopro.tenant_modulos tm where tm.tenant_id=@tenantId and tm.modulo_id=m.id and tm.reg_status='A' and tm.habilitado=true and upper(coalesce(tm.status,'ATIVO'))='ATIVO')", new { permissionIds, tenantId = ctx.Data.TenantId.Value }, tx);
            if (validPermissionCount != permissionIds.Length)
            {
                await tx.RollbackAsync();
                return ApiResponse<string>.Fail("Uma ou mais permissões pertencem a módulos não contratados.", 400);
            }
            var before = (await cn.QueryAsync<Guid>("select permissao_id from plantaopro.perfil_permissoes where perfil_id=@id and permitido=true and reg_status='A' order by permissao_id", new { id }, tx)).ToArray();
            await cn.ExecuteAsync("update plantaopro.perfil_permissoes set reg_status='I', reg_update=now() where perfil_id=@id and reg_status='A'", new { id }, tx);
            foreach (var permissaoId in permissionIds)
            {
                await cn.ExecuteAsync("insert into plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,reg_date,reg_status) values(gen_random_uuid(),@id,@permissaoId,true,now(),'A')", new { id, permissaoId }, tx);
            }
            await tx.CommitAsync();
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data.ClienteId, "PERFIL", id, "ALTERAR_PERMISSOES", new { antes = before, depois = permissionIds, adicionadas = permissionIds.Except(before).Count(), removidas = before.Except(permissionIds).Count() }, true, ip, "ADMINISTRADOR_CLIENTE");
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
        if (!ctx.Success) return ApiResponse<MinhaAssinaturaDto>.Fail(ctx.Message, ctx.StatusCode);
        // B4: tenant resolvido mas sem cliente vinculado (ex.: demo sem assinatura)
        // devolvia Fail(ctx.Message=200/"Sucesso") — envelope 200-falso. Estado
        // explícito: 404 honesto, propagado por uso/faturas/solicitações.
        if (ctx.Data?.ClienteId is null) return ApiResponse<MinhaAssinaturaDto>.Fail("Organização sem cliente vinculado para a assinatura.", 404);
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
        if (!ctx.Success) return ApiResponse<UsoPlanoDto>.Fail(ctx.Message, ctx.StatusCode);
        if (ctx.Data?.ClienteId is null) return ApiResponse<UsoPlanoDto>.Fail("Organização sem cliente vinculado para a assinatura.", 404);
        return await _assinaturaGuard.ObterUsoPlanoAsync(ctx.Data.ClienteId.Value);
    }

    /// <summary>
    /// B4: solicitações de plano do próprio tenant (upgrade/downgrade/
    /// cancelamento) com mensagem de estado honesta — "aguardando avaliação",
    /// nunca "concluído".
    /// </summary>
    public async Task<ApiResponse<IEnumerable<MinhaSolicitacaoPlanoDto>>> MinhasSolicitacoesAsync()
    {
        try
        {
            var ctx = await _tenantContext.ObterAtualAsync();
            if (!ctx.Success) return ApiResponse<IEnumerable<MinhaSolicitacaoPlanoDto>>.Fail(ctx.Message, ctx.StatusCode);
            if (ctx.Data?.TenantId is null) return ApiResponse<IEnumerable<MinhaSolicitacaoPlanoDto>>.Fail("Organização sem tenant identificado.", 404);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var rows = new List<MinhaSolicitacaoPlanoDto>();
            foreach (var tabela in new[] { "plantaopro.upgrade_solicitacoes", "plantaopro.downgrade_solicitacoes" })
            {
                var kind = tabela.Contains("upgrade") ? "UPGRADE" : "DOWNGRADE";
                rows.AddRange(await cn.QueryAsync<MinhaSolicitacaoPlanoDto>($@"select s.id as ""Id"",
       case when upper(coalesce(s.status,''))='CANCELAMENTO_SOLICITADO' then 'CANCELAMENTO' else '{kind}' end as ""Tipo"",
       coalesce(pd.nome,'') as ""PlanoDestinoNome"", coalesce(s.status,'') as ""Status"", s.reg_date as ""SolicitadoEm"",
       case upper(coalesce(s.status,''))
         when 'SOLICITADO' then 'Aguardando avaliacao comercial. Nada mudou no seu plano.'
         when 'CANCELAMENTO_SOLICITADO' then 'Aguardando avaliacao comercial. Seu acesso continua ativo.'
         when 'APROVADA' then 'Aprovada e aplicada. Confira os dados atuais da assinatura.'
         when 'RECUSADA' then 'Recusada pela avaliacao comercial. Nada mudou no seu plano.'
         else 'Verifique a situacao com o atendimento comercial.'
       end as ""MensagemEstado""
  from {tabela} s
  left join plantaopro.planos pd on pd.id=s.plano_destino_id
 where s.reg_status='A' and s.tenant_id=@tenantId
 order by s.reg_date desc limit 50", new { tenantId = ctx.Data.TenantId.Value }));
            }
            return ApiResponse<IEnumerable<MinhaSolicitacaoPlanoDto>>.Ok(rows.OrderByDescending(r => r.SolicitadoEm));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar minhas solicitacoes de plano");
            return ApiResponse<IEnumerable<MinhaSolicitacaoPlanoDto>>.Fail("Não foi possível listar as solicitações.", 500);
        }
    }

    public async Task<ApiResponse<IEnumerable<MinhaAssinaturaFaturaDto>>> FaturasMinhaAssinaturaAsync()
    {
        try
        {
            var assinatura = await MinhaAssinaturaAsync();
            if (!assinatura.Success || assinatura.Data is null) return ApiResponse<IEnumerable<MinhaAssinaturaFaturaDto>>.Fail(assinatura.Message, assinatura.StatusCode);
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            var rows = await cn.QueryAsync<MinhaAssinaturaFaturaDto>(@"select id as ""Id"", cliente_id as ""ClienteId"", assinatura_id as ""AssinaturaId"", vencimento as ""Vencimento"", valor as ""Valor"", coalesce(status,'') as ""Status"", coalesce(descricao,'Fatura SaaS') as ""Descricao""
from plantaopro.cadastro_cliente_pagamentos_iniciais
where cliente_id=@clienteId and assinatura_id=@assinaturaId and reg_status='A'
order by vencimento desc
limit 24", new { clienteId = assinatura.Data.ClienteId, assinaturaId = assinatura.Data.AssinaturaId });
            return ApiResponse<IEnumerable<MinhaAssinaturaFaturaDto>>.Ok(rows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao carregar faturas da minha assinatura");
            return ApiResponse<IEnumerable<MinhaAssinaturaFaturaDto>>.Fail("Não foi possível carregar as faturas.", 500);
        }
    }

    public async Task<ApiResponse<string>> SolicitarCancelamentoAssinaturaAsync(SolicitacaoCancelamentoAssinaturaRequest request, string? ip)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Motivo) || request.Motivo.Trim().Length < 10) return ApiResponse<string>.Fail("Informe um motivo com pelo menos 10 caracteres.", 400);
            var assinatura = await MinhaAssinaturaAsync();
            if (!assinatura.Success || assinatura.Data is null) return ApiResponse<string>.Fail(assinatura.Message, assinatura.StatusCode);
            var ctx = await _tenantContext.ObterAtualAsync();
            await using var cn = new NpgsqlConnection(_cfg.GetConnectionString("Default"));
            await cn.ExecuteAsync(@"insert into plantaopro.downgrade_solicitacoes(id,tenant_id,cliente_id,assinatura_id,plano_atual_id,plano_destino_id,motivo,impacto_validado,status,solicitado_por,reg_date,reg_status)
values(gen_random_uuid(),@tenantId,@clienteId,@assinaturaId,@planoAtualId,@planoAtualId,@motivo,true,'CANCELAMENTO_SOLICITADO',@usuarioId,now(),'A')", new { tenantId = ctx.Data?.TenantId, clienteId = assinatura.Data.ClienteId, assinaturaId = assinatura.Data.AssinaturaId, planoAtualId = assinatura.Data.PlanoId, motivo = request.Motivo.Trim(), usuarioId = _tenantContext.ObterUsuarioId() });
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), assinatura.Data.ClienteId, "ASSINATURA", assinatura.Data.AssinaturaId, "SOLICITAR_CANCELAMENTO", new { motivoInformado = true }, true, ip, "ADMINISTRADOR_CLIENTE");
            return ApiResponse<string>.Ok("ok", "Solicitação de cancelamento registrada para avaliação comercial.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao solicitar cancelamento de assinatura");
            return ApiResponse<string>.Fail("Não foi possível solicitar cancelamento.", 500);
        }
    }

    private async Task<Guid> GarantirPerfilAdminClienteAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction tx, Guid tenantId, Guid clienteId)
    {
        var perfilId = await cn.ExecuteScalarAsync<Guid?>("select id from plantaopro.perfis where tenant_id=@tenantId and codigo='ADMINISTRADOR_CLIENTE' and reg_status='A' limit 1", new { tenantId }, tx);
        if (perfilId.HasValue) return perfilId.Value;
        var id = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_date,reg_status) values(@id,@tenantId,@clienteId,'ADMINISTRADOR_CLIENTE','Administrador cliente','Administrador do tenant',false,true,'ATIVO',now(),'A')", new { id, tenantId, clienteId }, tx);
        await cn.ExecuteAsync(@"insert into plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,reg_date,reg_status)
select gen_random_uuid(),@id,p.id,true,now(),'A' from plantaopro.permissoes p where p.codigo in ('DASHBOARD.VISUALIZAR','CONFIGURACOES.CONFIGURAR','WHITE_LABEL.CONFIGURAR','PLANOS.VISUALIZAR','ASSINATURAS.VISUALIZAR','USUARIOS.ADMINISTRAR') or p.codigo like 'MEDICOS.%' or p.codigo like 'HOSPITAIS.%' or p.codigo like 'ESPECIALIDADES.%'", new { id }, tx);
        // R5-A2: fiscal ADM360 (API autoriza POR ACAO). Dedupe deterministico ponto/dois-pontos
        // + not-exists por codigo normalizado: seed reaplicado nao duplica grants.
        await cn.ExecuteAsync(@"insert into plantaopro.perfil_permissoes(id,perfil_id,permissao_id,permitido,reg_date,reg_status)
select gen_random_uuid(),@id,esc.id,true,now(),'A' from (values ('ADM360.VER'),('ADM360.EXPORTAR'),('ADM360.IMPORTAR_XML'),('ADM360.CRIAR'),('ADM360.EDITAR'),('ADM360.CONFIGURAR'),('ADM360.REABRIR'),('ADM360.CONFIRMAR'),('ADM360.CANCELAR'),('ADM360.CONFERIR'),('ADM360.MANIFESTAR_DFE'),('ADM360.VINCULAR_DOCUMENTOS'),('ADM360.TRANSMITIR_RESPOSTA')) as alvo(codigo)
join lateral (select pe.id from plantaopro.permissoes pe where replace(pe.codigo,':','.')=alvo.codigo order by pe.id limit 1) as esc on true
where not exists (select 1 from plantaopro.perfil_permissoes pp join plantaopro.permissoes pe2 on pe2.id=pp.permissao_id where pp.perfil_id=@id and pp.reg_status='A' and replace(pe2.codigo,':','.')=alvo.codigo)", new { id }, tx);
        return id;
    }

    private static async Task CriarWhiteLabelPadraoAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction tx, Guid tenantId, string clienteNome)
    {
        await cn.ExecuteAsync("insert into plantaopro.tenant_white_label(id,tenant_id,nome_plataforma,cliente_nome,slogan,texto_boas_vindas,texto_rodape,reg_date,reg_status) values(gen_random_uuid(),@tenantId,'PlantãoPro',@clienteNome,'Gestão inteligente de plantões','Bem-vindo ao seu ambiente PlantãoPro','PlantãoPro SaaS',now(),'A')", new { tenantId, clienteNome }, tx);
    }

    private static async Task CriarOnboardingAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction tx, Guid tenantId, Guid clienteId)
    {
        // R5-C7: as etapas do checklist (plantaopro.tenant_onboarding_checklist) nao sao mais as
        // 11 genericas fixas: vao materializadas do catalogo canonicos onboarding_etapas_catalogo
        // (nucleo geral + etapas de modulo com contrato efetivo), com objetivo, responsavel,
        // pre-requisito e criterio verificavel em dados persistidos (avaliador C8).
        var onboardingId = Guid.NewGuid();
        await cn.ExecuteAsync("insert into plantaopro.tenant_onboarding(id,tenant_id,cliente_id,status,progresso,proxima_acao,reg_date,reg_status) values(@onboardingId,@tenantId,@clienteId,'EM_ANDAMENTO',0,'Completar dados da empresa',now(),'A')", new { onboardingId, tenantId, clienteId }, tx);
        await OnboardingJornadaService.MaterializarAsync(cn, tx, tenantId, clienteId, onboardingId);
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
    private static string SomenteDigitos(string valor) => string.Concat((valor ?? string.Empty).Where(char.IsDigit));
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
