using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;
using PlantaoPro.CrossCutting.Security;

namespace PlantaoPro.Api.Data;

/// <summary>
/// R5-C7/C8: jornada de onboarding adaptada ao contrato.
///
/// - Materializacao: as etapas vao do catalogo canonicos (onboarding_etapas_catalogo) para o
///   checklist do tenant. Nucleo geral sempre; etapas de modulo SÓ quando o contrato do modulo
///   esta efetivo no tenant (mesmo predicado de vigencia dos claims de login - B4/B6).
/// - Conclusao derivada: nada de "marcar como concluida" por clique quando existe criterio. O
///   avaliador le os DADOS PERSISTIDOS do tenant (plantoes, pacientes, convites, operacoes...) e
///   so considera a etapa atendida quando o criterio e verificado. A transicao e persistida com
///   origem (AUTOMATICO/MANUAL) e evidencia legivel; se o dado subjacente deixar de existir, a
///   etapa volta a pendente (a verdade segue os dados, nao o botao).
/// - Escopo das metricas: quando o tenant tem etapas do catalogo (codigo ONB_*), progresso e
///   revisao consideram apenas elas; linhas legadas ETAPA_xx continuam visiveis como historico.
/// - Opcional pulavel: etapa nao-obrigatoria pode ser "pulada" com justificativa PERSISTIDA e
///   nunca trava progresso/revisao. Etapa obrigatoria nunca e pulada (409 honesto).
/// </summary>
public sealed class OnboardingJornadaService
{
    private readonly IConfiguration cfg;
    private readonly ILogger<OnboardingJornadaService> logger;

    public OnboardingJornadaService(IConfiguration cfg, ILogger<OnboardingJornadaService> logger)
    {
        this.cfg = cfg;
        this.logger = logger;
    }

    // ------------------------------------------------------------------
    // Materiais de leitura interna
    // ------------------------------------------------------------------
    private sealed class EtapaRow
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Objetivo { get; set; } = string.Empty;
        public string Responsavel { get; set; } = "CLIENTE";
        public string? PreRequisitoCodigo { get; set; }
        public string? ModuloCodigo { get; set; }
        public string? ModuloNome { get; set; }
        public string? CriterioCodigo { get; set; }
        public string LinkAcao { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public bool Obrigatorio { get; set; }
        public bool Concluido { get; set; }
        public bool Pulada { get; set; }
        public string? OrigemConclusao { get; set; }
    }

    private const string RowSelect = @"select x.id as ""Id"", coalesce(x.codigo,'') as ""Codigo"", coalesce(x.titulo,'') as ""Titulo"",
    coalesce(x.descricao,'') as ""Descricao"", coalesce(x.objetivo,'') as ""Objetivo"",
    coalesce(nullif(x.responsavel,''),'CLIENTE') as ""Responsavel"", x.pre_requisito_codigo as ""PreRequisitoCodigo"",
    x.modulo_codigo as ""ModuloCodigo"", coalesce(ms.nome, initcap(lower(x.modulo_codigo))) as ""ModuloNome"",
    x.criterio_codigo as ""CriterioCodigo"", coalesce(x.link_acao,'') as ""LinkAcao"", coalesce(x.ordem,0) as ""Ordem"",
    coalesce(x.obrigatorio,true) as ""Obrigatorio"", coalesce(x.concluido,false) as ""Concluido"",
    coalesce(x.pulada,false) as ""Pulada"", x.origem_conclusao as ""OrigemConclusao""
from plantaopro.tenant_onboarding_checklist x
left join lateral (select ms0.nome from plantaopro.modulos_sistema ms0 where upper(ms0.codigo)=upper(x.modulo_codigo) and ms0.reg_status='A' limit 1) ms on true
where x.tenant_id=@tenantId and x.reg_status='A'
order by coalesce(x.ordem,0), coalesce(x.codigo,'')";

    /// <summary>Descricoes canonicas de cada criterio (o que exatamente e lido nos dados).</summary>
    public static readonly IReadOnlyDictionary<string, string> CriterioDescricoes = new Dictionary<string, string>
    {
        ["EMPRESA_DADOS_COMPLETOS"] = "Cadastro do cliente com e-mail, telefone, cidade e estado preenchidos",
        ["CONVITE_EQUIPE_ENVIADO"] = "Convite de equipe registrado (token com expiracao e uso unico)",
        ["MEMBRO_EQUIPE_ATIVO"] = "Convite de equipe aceito - uso do token registrado no banco",
        ["WHITE_LABEL_PERSONALIZADO"] = "Identidade visual com logo ou nome de plataforma personalizados",
        ["PRIMEIRO_PLANTAO_CRIADO"] = "Plantao registrado na grade do tenant",
        ["PRIMEIRO_PLANTAO_PUBLICADO"] = "Plantao publicado (aberto/confirmado/realizado) para a operacao",
        ["ESCALA_PREENCHIDA"] = "Escala confirmada/realizada vinculada a um plantao do tenant",
        ["PRIMEIRA_UNIDADE_SAUDE"] = "Unidade de atendimento cadastrada no Saude 360",
        ["PRIMEIRO_PACIENTE_CADASTRADO"] = "Paciente cadastrado no tenant",
        ["PRIMEIRA_TRIAGEM_REALIZADA"] = "Triagem registrada no fluxo assistido do tenant",
        ["PRIMEIRA_OPERACAO_ADM360"] = "Operacao administrativa lancada no Administrativo 360",
        ["REVISAO_GERAL"] = "Todas as etapas obrigatorias contratadas verificadas em dados reais",
    };

    // ------------------------------------------------------------------
    // Contratos efetivos do tenant (mesma regra dos claims de login)
    // ------------------------------------------------------------------
    public static async Task<List<string>> ModulosEfetivosAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction? tx, Guid tenantId)
    {
        var sql = $@"select distinct upper(ms.codigo)
from plantaopro.tenant_modulos tm
join plantaopro.modulos_sistema ms on ms.id=tm.modulo_id and ms.reg_status='A' and upper(ms.status)='ATIVO'
where tm.tenant_id=@tenantId and ({ModuleContractVigencia.EffectivePredicate})
order by 1";
        return (await cn.QueryAsync<string>(new CommandDefinition(sql, new { tenantId }, tx))).ToList();
    }

    // ------------------------------------------------------------------
    // Materializacao a partir do catalogo (idempotente por codigo)
    // ------------------------------------------------------------------
    /// <summary>
    /// Insere no checklist as etapas do catalogo que ainda nao existem para o tenant: nucleo
    /// geral + etapas cujo modulo tem contrato efetivo. Usada pelo kernel de provisionamento,
    /// por POST /api/onboarding/iniciar e por POST /api/onboarding/sincronizar.
    /// Retorna quantas etapas foram adicionadas.
    /// </summary>
    public static async Task<int> MaterializarAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction? tx, Guid tenantId, Guid clienteId, Guid? onboardingId)
    {
        var modulos = await ModulosEfetivosAsync(cn, tx, tenantId);
        const string sql = @"insert into plantaopro.tenant_onboarding_checklist(
    id,onboarding_id,tenant_id,cliente_id,codigo,titulo,descricao,ordem,obrigatorio,link_acao,status,reg_date,reg_status,
    objetivo,responsavel,pre_requisito_codigo,modulo_codigo,criterio_codigo)
select gen_random_uuid(),@onboardingId,@tenantId,@clienteId,c.codigo,c.titulo,c.descricao,c.ordem,c.obrigatorio,c.link_acao,'PENDENTE',now(),'A',
    c.objetivo,c.responsavel,c.pre_requisito_codigo,c.modulo_codigo,c.criterio_codigo
from plantaopro.onboarding_etapas_catalogo c
where c.reg_status='A'
  and (c.modulo_codigo is null or upper(c.modulo_codigo) = any(@modulos))
  and not exists (select 1 from plantaopro.tenant_onboarding_checklist x
                  where x.tenant_id=@tenantId and x.codigo=c.codigo and x.reg_status='A')";
        return await cn.ExecuteAsync(new CommandDefinition(sql, new { tenantId, clienteId, onboardingId, modulos }, tx));
    }

    /// <summary>Remove (soft) etapas de modulo cujo contrato deixou de ser efetivo; devolve quantas sairam.</summary>
    public static async Task<int> RemoverEtapasDeModulosInefetivosAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction? tx, Guid tenantId)
    {
        var modulos = await ModulosEfetivosAsync(cn, tx, tenantId);
        const string sql = @"update plantaopro.tenant_onboarding_checklist
set reg_status='I', reg_update=now()
where tenant_id=@tenantId and reg_status='A' and modulo_codigo is not null
  and upper(modulo_codigo) <> all(@modulos)";
        return await cn.ExecuteAsync(new CommandDefinition(sql, new { tenantId, modulos }, tx));
    }

    // ------------------------------------------------------------------
    // Avaliador de criterios (leitura de dados persistidos do tenant)
    // ------------------------------------------------------------------
    private static async Task<(bool Atendida, string Evidencia)> AvaliarCriterioAsync(
        NpgsqlConnection cn, Guid tenantId, Guid clienteId, string criterio)
    {
        switch (criterio)
        {
            case "EMPRESA_DADOS_COMPLETOS":
                var faltando = await cn.QueryFirstOrDefaultAsync<string?>(@"select case
    when c.id is null then 'cadastro do cliente nao encontrado'
    when coalesce(regexp_replace(c.email,'\s','','g'),'')='' then 'e-mail corporativo'
    when coalesce(regexp_replace(c.telefone,'\D','','g'),regexp_replace(coalesce((select u.telefone from plantaopro.usuarios u where u.cliente_id=c.id and u.reg_status='A' order by u.reg_date limit 1),''),'\D','','g'))='' then 'telefone de contato'
    when coalesce(regexp_replace(c.cidade,'\s','','g'),(select regexp_replace(s.cidade,'\s','','g') from plantaopro.cadastro_cliente_solicitacoes s where regexp_replace(s.cnpj,'\D','','g')=regexp_replace(c.cnpj,'\D','','g') and s.cidade is not null and s.reg_status='A' order by s.reg_date desc limit 1),'')='' then 'cidade'
    when coalesce(regexp_replace(c.estado,'\s','','g'),'')='' then 'estado (UF)'
    else null end
from (select id,email,telefone,cidade,estado,cnpj from plantaopro.clientes where id=@clienteId) c", new { clienteId });
                return faltando is null
                    ? (true, "Dados da empresa completos no cadastro persistido.")
                    : (false, "Falta no cadastro: " + faltando + ".");

            case "CONVITE_EQUIPE_ENVIADO":
                var conviteEnviado = await cn.QuerySingleAsync<bool>(@"select exists(select 1 from plantaopro.usuario_convites cv where cv.tenant_id=@tenantId and cv.reg_status='A')", new { tenantId });
                return conviteEnviado
                    ? (true, "Convite de equipe registrado no banco (uso unico, com expiracao).")
                    : (false, "Nenhum convite de equipe registrado para este cliente.");

            case "MEMBRO_EQUIPE_ATIVO":
                var conviteAceito = await cn.QuerySingleAsync<bool>(@"select exists(select 1 from plantaopro.usuario_convites cv where cv.tenant_id=@tenantId and cv.reg_status='A' and (cv.usado_em is not null or cv.revogado_em is not null and cv.usado_por is not null))", new { tenantId });
                return conviteAceito
                    ? (true, "Um convidado usou o token e entrou no ambiente.")
                    : (false, "Ainda nao houve aceite de convite (token nao usado).");

            case "WHITE_LABEL_PERSONALIZADO":
                // O default do kernel e "Plantaopro"; sem normalizar acentos, "PLANTAOPRO" passaria por personalizaco.
                var wl = await cn.QuerySingleAsync<bool>(@"select exists(select 1 from plantaopro.tenant_white_label w where w.tenant_id=@tenantId and w.reg_status='A'
and (coalesce(w.logo_url,'')<>'' or coalesce(translate(lower(trim(w.nome_plataforma)),'áàâãäéèêëíìîïóòôõöúùûüç','aaaaaeeeeiiiiooooouuuuc'),'') not in ('','plantaopro') or coalesce(w.cor_primaria,'')<>'' or coalesce(w.login_banner_url,'')<>''))", new { tenantId });
                return wl
                    ? (true, "Identidade visual personalizada salva (logo, nome, cor ou banner).")
                    : (false, "Configuracao de identidade visual ainda esta no padrao PlantaoPro.");

            case "PRIMEIRO_PLANTAO_CRIADO":
                var pl = await cn.QuerySingleAsync<bool>(@"select exists(select 1 from plantaopro.plantoes p where p.tenant_id=@tenantId and p.reg_status='A')", new { tenantId });
                return pl ? (true, "Ha plantao registrado na grade do tenant.") : (false, "Nenhum plantao criado por este tenant.");

            case "PRIMEIRO_PLANTAO_PUBLICADO":
                var plPub = await cn.QuerySingleAsync<bool>(@"select exists(select 1 from plantaopro.plantoes p where p.tenant_id=@tenantId and p.reg_status='A' and lower(coalesce(p.status,'')) in ('aberto','aberta','confirmado','realizado','publicado'))", new { tenantId });
                return plPub ? (true, "Ha plantao publicado na grade (status aberto/confirmado/realizado).") : (false, "Nenhum plantao publicado; existe apenas rascunho ou nada foi criado.");

            case "ESCALA_PREENCHIDA":
                var esc = await cn.QuerySingleAsync<bool>(@"select exists(select 1 from plantaopro.escalas e join plantaopro.plantoes p on p.id=e.plantao_id where p.tenant_id=@tenantId and e.reg_status='A' and lower(coalesce(e.status,'')) in ('confirmado','confirmada','realizado','realizada','preenchido'))", new { tenantId });
                return esc ? (true, "Escala confirmada/realizada em plantao do tenant (fluxo medico completo).") : (false, "Nenhuma escala confirmada em plantao deste tenant ainda.");

            case "PRIMEIRA_UNIDADE_SAUDE":
                var un = await cn.QuerySingleAsync<bool>(@"select exists(select 1 from plantaopro.clinica_unidades_atendimento ua where ua.tenant_id=@tenantId and ua.reg_status='A')", new { tenantId });
                return un ? (true, "Unidade de atendimento cadastrada.") : (false, "Nenhuma unidade de atendimento cadastrada no Saude 360.");

            case "PRIMEIRO_PACIENTE_CADASTRADO":
                var pa = await cn.QuerySingleAsync<bool>(@"select exists(select 1 from plantaopro.pacientes pt where pt.tenant_id=@tenantId and pt.reg_status='A')", new { tenantId });
                return pa ? (true, "Paciente cadastrado no tenant.") : (false, "Nenhum paciente cadastrado por este tenant.");

            case "PRIMEIRA_TRIAGEM_REALIZADA":
                var tr = await cn.QuerySingleAsync<bool>(@"select exists(select 1 from plantaopro.triagens t where t.tenant_id=@tenantId and t.reg_status='A')", new { tenantId });
                return tr ? (true, "Triagem registrada no fluxo assistido.") : (false, "Nenhuma triagem registrada por este tenant.");

            case "PRIMEIRA_OPERACAO_ADM360":
                var ad = await cn.QuerySingleAsync<bool>(@"select exists(select 1 from plantaopro.adm360_operacoes o where o.tenant_id=@tenantId)", new { tenantId });
                return ad ? (true, "Operacao administrativa lancada no Administrativo 360.") : (false, "Nenhuma operacao lancada no Administrativo 360 deste tenant.");

            default:
                return (false, "Criterio '" + criterio + "' desconhecido pelo avaliador - configure o catalogo ou trate a etapa como conclusao humana.");
        }
    }

    // ------------------------------------------------------------------
    // Nucleo: calcula o modelo efetivo (leitura pura) e persiste transicoes
    // ------------------------------------------------------------------
    private sealed record EtapaEfetiva(EtapaRow Row, bool Atendida, string? Evidencia, bool PreRequisitoAtendido, string Status);

    private static async Task<List<EtapaEfetiva>> CalcularAsync(NpgsqlConnection cn, Guid tenantId, Guid clienteId)
    {
        var rows = (await cn.QueryAsync<EtapaRow>(new CommandDefinition(RowSelect, new { tenantId }))).ToList();
        var temCatalogo = rows.Any(r => r.Codigo.StartsWith("ONB_", StringComparison.Ordinal));
        var escopo = temCatalogo ? rows.Where(r => r.Codigo.StartsWith("ONB_", StringComparison.Ordinal)).ToList() : rows;
        var porCodigo = escopo.ToDictionary(r => r.Codigo);

        var avaliado = new Dictionary<string, (bool Atendida, string? Evidencia)>();
        foreach (var r in escopo)
        {
            if (r.Pulada) { avaliado[r.Codigo] = (true, "Etapa opcional pulada (justificativa registrada)."); continue; }
            if (r.CriterioCodigo is null || r.CriterioCodigo == "REVISAO_GERAL")
            {
                // Manual guarda a verdade humana; revisao e resolvida depois do resto.
                avaliado[r.Codigo] = (r.Concluido, r.CriterioCodigo is null
                    ? (r.Concluido ? "Conclusao humana registrada." : "Etapa de conclusao humana (sem criterio automatico).")
                    : (r.Concluido ? "Revisao ja registrada." : "Aguardando verificacao automatica das demais etapas."));
                continue;
            }
            avaliado[r.Codigo] = await AvaliarCriterioAsync(cn, tenantId, clienteId, r.CriterioCodigo);
        }

        foreach (var r in escopo.Where(x => x.CriterioCodigo == "REVISAO_GERAL"))
        {
            if (r.Pulada) continue;
            bool AtendidaDe(EtapaRow x) => avaliado.TryGetValue(x.Codigo, out var av) ? av.Atendida : x.Concluido;
            var restante = escopo.Count(x => !ReferenceEquals(x, r) && x.Obrigatorio && x.Codigo != "ONB_REVISAO" && !AtendidaDe(x));
            var pendentes = escopo.Where(x => !ReferenceEquals(x, r) && x.Obrigatorio && x.Codigo != "ONB_REVISAO" && !AtendidaDe(x)).Select(x => x.Titulo).Take(3).ToList();
            avaliado[r.Codigo] = restante == 0
                ? (true, "Todas as etapas obrigatorias verificadas em dados reais.")
                : (false, "Faltam " + restante + " etapa(s) obrigatoria(s): " + string.Join("; ", pendentes) + ".");
        }

        var resultado = new List<EtapaEfetiva>();
        foreach (var r in rows)
        {
            var dentroEscopo = avaliado.ContainsKey(r.Codigo);
            var (atendida, evidencia) = dentroEscopo ? avaliado[r.Codigo] : (r.Concluido, "Etapa legada (fora do catalogo); mantida como registrada.");
            var preOk = r.PreRequisitoCodigo is null || (porCodigo.TryGetValue(r.PreRequisitoCodigo, out var pre) && (avaliado.TryGetValue(pre.Codigo, out var pav) ? pav.Atendida : pre.Concluido));
            string status;
            if (r.Pulada) status = "PULADA";
            else if (atendida) status = "CONCLUIDO";
            else if (!preOk) status = "BLOQUEADO";
            else status = "PENDENTE";
            resultado.Add(new EtapaEfetiva(r, atendida, evidencia, preOk, status));
        }
        return resultado;
    }

    private static async Task PersistirTransicoesAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction tx, List<EtapaEfetiva> efetivas)
    {
        foreach (var e in efetivas)
        {
            var r = e.Row;
            if (e.Atendida && !r.Concluido)
                await cn.ExecuteAsync(new CommandDefinition(@"update plantaopro.tenant_onboarding_checklist
set concluido=true, concluido_em=coalesce(concluido_em,now()), status='CONCLUIDO', origem_conclusao=@origem, evidencia=@evidencia, avaliada_em=now(), reg_update=now()
where id=@id", new { id = r.Id, origem = r.CriterioCodigo is null ? "MANUAL" : "AUTOMATICO", evidencia = e.Evidencia }, tx));
            else if (!e.Atendida && r.Concluido && string.Equals(r.OrigemConclusao, "AUTOMATICO", StringComparison.OrdinalIgnoreCase))
                await cn.ExecuteAsync(new CommandDefinition(@"update plantaopro.tenant_onboarding_checklist
set concluido=false, concluido_em=null, status='PENDENTE', origem_conclusao=null, evidencia=@evidencia, avaliada_em=now(), reg_update=now()
where id=@id", new { id = r.Id, evidencia = e.Evidencia }, tx));
            else
                await cn.ExecuteAsync(new CommandDefinition(@"update plantaopro.tenant_onboarding_checklist set evidencia=@evidencia, avaliada_em=now(), reg_update=now()
where id=@id and (evidencia IS DISTINCT FROM @evidencia OR avaliada_em IS NULL)", new { id = r.Id, evidencia = e.Evidencia }, tx));
        }
    }

    private static async Task RecalcularResumoAsync(NpgsqlConnection cn, System.Data.Common.DbTransaction tx, Guid tenantId, List<EtapaEfetiva> efetivas)
    {
        var temCatalogo = efetivas.Any(e => e.Row.Codigo.StartsWith("ONB_", StringComparison.Ordinal));
        var escopo = temCatalogo ? efetivas.Where(e => e.Row.Codigo.StartsWith("ONB_", StringComparison.Ordinal)).ToList() : efetivas;
        var obrigatorias = escopo.Where(e => e.Row.Obrigatorio && e.Status != "PULADA").ToList();
        var feitas = obrigatorias.Count(e => e.Status == "CONCLUIDO");
        var progresso = obrigatorias.Count == 0 ? (escopo.Count > 0 ? 100 : 0) : (int)Math.Round((decimal)feitas * 100 / obrigatorias.Count);
        var proxima = escopo.FirstOrDefault(e => e.Status == "PENDENTE" || e.Status == "BLOQUEADO");
        var status = progresso >= 100 && escopo.Count > 0 ? "FINALIZADO" : "EM_ANDAMENTO";
        await cn.ExecuteAsync(new CommandDefinition(@"update plantaopro.tenant_onboarding
set progresso=@progresso, proxima_acao=coalesce(@proxima,'Onboarding finalizado'), status=@status,
    finalizado_em=case when @status='FINALIZADO' then now() else finalizado_em end, reg_update=now()
where tenant_id=@tenantId and reg_status='A'", new { tenantId, progresso, proxima = proxima?.Row.Titulo, status }, tx));
        return;
    }

    private static OnboardingChecklistItemDto ToDto(EtapaEfetiva e) => new()
    {
        Id = e.Row.Id,
        Codigo = e.Row.Codigo,
        Titulo = e.Row.Titulo,
        Descricao = e.Row.Descricao,
        Objetivo = e.Row.Objetivo,
        Responsavel = e.Row.Responsavel,
        PreRequisitoCodigo = e.Row.PreRequisitoCodigo,
        PreRequisitoAtendido = e.PreRequisitoAtendido,
        ModuloCodigo = e.Row.ModuloCodigo ?? string.Empty,
        ModuloNome = e.Row.ModuloNome ?? string.Empty,
        CriterioCodigo = e.Row.CriterioCodigo ?? string.Empty,
        CriterioDescricao = e.Row.CriterioCodigo is not null && CriterioDescricoes.TryGetValue(e.Row.CriterioCodigo, out var d) ? d : "Conclusao registrada pelo responsavel",
        Evidencia = e.Evidencia ?? string.Empty,
        Atendida = e.Atendida,
        LinkAcao = e.Row.LinkAcao,
        Ordem = e.Row.Ordem,
        Obrigatorio = e.Row.Obrigatorio,
        Concluido = e.Atendida && e.Status == "CONCLUIDO",
        Pulada = e.Status == "PULADA",
        Status = e.Status,
    };

    /// <summary>
    /// Reavalia o onboarding do tenant contra os dados reais, persiste as transicoes
    /// (conclusoes automaticas, reversoes e evidencias) e devolve o quadro atual.
    /// </summary>
    public async Task<ApiResponse<IEnumerable<OnboardingChecklistItemDto>>> ReavaliarAsync(Guid tenantId, Guid clienteId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            var efetivas = await CalcularAsync(cn, tenantId, clienteId);
            await using (var tx = await cn.BeginTransactionAsync())
            {
                await PersistirTransicoesAsync(cn, tx, efetivas);
                await RecalcularResumoAsync(cn, tx, tenantId, efetivas);
                await tx.CommitAsync();
            }
            return ApiResponse<IEnumerable<OnboardingChecklistItemDto>>.Ok(efetivas.Select(ToDto).ToList());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao reavaliar onboarding do tenant {TenantId}", tenantId);
            return ApiResponse<IEnumerable<OnboardingChecklistItemDto>>.Fail("Nao foi possivel reavaliar o onboarding.", 500);
        }
    }

    /// <summary>Inicia (ou garante) o onboarding de um tenant: linha-mestre + materializacao do catalogo.</summary>
    public async Task<ApiResponse<IEnumerable<OnboardingChecklistItemDto>>> IniciarAsync(Guid tenantId, Guid clienteId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await using (var tx = await cn.BeginTransactionAsync())
            {
                var existente = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition("select id from plantaopro.tenant_onboarding where tenant_id=@tenantId and reg_status='A' order by reg_date desc limit 1", new { tenantId }, tx));
                Guid onboardingId;
                if (existente.HasValue) onboardingId = existente.Value;
                else
                {
                    if (clienteId == Guid.Empty)
                        clienteId = await cn.QuerySingleAsync<Guid>(new CommandDefinition("select coalesce(cliente_id,(select t.cliente_id from plantaopro.tenants t where t.id=@tenantId)) from plantaopro.tenants where id=@tenantId", new { tenantId }, tx));
                    onboardingId = await cn.QuerySingleAsync<Guid>(new CommandDefinition(@"insert into plantaopro.tenant_onboarding(id,tenant_id,cliente_id,status,progresso,proxima_acao,reg_date,reg_status)
values(gen_random_uuid(),@tenantId,@clienteId,'EM_ANDAMENTO',0,'Completar dados da empresa',now(),'A') returning id", new { tenantId, clienteId }, tx));
                }
                await MaterializarAsync(cn, tx, tenantId, clienteId, onboardingId);
                await tx.CommitAsync();
            }
            return await ReavaliarAsync(tenantId, clienteId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao iniciar onboarding do tenant {TenantId}", tenantId);
            return ApiResponse<IEnumerable<OnboardingChecklistItemDto>>.Fail("Nao foi possivel iniciar o onboarding.", 500);
        }
    }

    /// <summary>Re-sincroniza o checklist com os contratos efetivos (adiciona novos, remove inefetivos) e reavalia.</summary>
    public async Task<ApiResponse<IEnumerable<OnboardingChecklistItemDto>>> SincronizarAsync(Guid tenantId, Guid clienteId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            if (clienteId == Guid.Empty)
                clienteId = await cn.QuerySingleAsync<Guid>("select coalesce(cliente_id,(select t.cliente_id from plantaopro.tenants t where t.id=@tenantId)) from plantaopro.tenants where id=@tenantId", new { tenantId });
            await using (var tx = await cn.BeginTransactionAsync())
            {
                var existente = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition("select id from plantaopro.tenant_onboarding where tenant_id=@tenantId and reg_status='A' order by reg_date desc limit 1", new { tenantId }, tx));
                Guid onboardingId;
                if (existente.HasValue) onboardingId = existente.Value;
                else
                    onboardingId = await cn.QuerySingleAsync<Guid>(new CommandDefinition(@"insert into plantaopro.tenant_onboarding(id,tenant_id,cliente_id,status,progresso,proxima_acao,reg_date,reg_status)
values(gen_random_uuid(),@tenantId,@clienteId,'EM_ANDAMENTO',0,'Completar dados da empresa',now(),'A') returning id", new { tenantId, clienteId }, tx));
                await RemoverEtapasDeModulosInefetivosAsync(cn, tx, tenantId);
                await MaterializarAsync(cn, tx, tenantId, clienteId, onboardingId);
                await tx.CommitAsync();
            }
            return await ReavaliarAsync(tenantId, clienteId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao sincronizar onboarding do tenant {TenantId}", tenantId);
            return ApiResponse<IEnumerable<OnboardingChecklistItemDto>>.Fail("Nao foi possivel sincronizar o onboarding.", 500);
        }
    }

    /// <summary>
    /// Conclusao explicita. So tem efeito real quando: a etapa e de conclusao humana (sem criterio)
    /// OU o criterio automatico ja esta atendido nos dados (o clique entao so materializa a
    /// transicao). Nunca inventa sucesso: criterio nao atendido devolve 409 com a evidencia.
    /// </summary>
    public async Task<ApiResponse<OnboardingChecklistItemDto>> ConcluirEtapaAsync(Guid tenantId, Guid clienteId, Guid etapaId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            var efetivas = await CalcularAsync(cn, tenantId, clienteId);
            var alvo = efetivas.FirstOrDefault(e => e.Row.Id == etapaId);
            if (alvo is null) return ApiResponse<OnboardingChecklistItemDto>.Fail("Etapa nao encontrada neste cliente.", 404);
            if (alvo.Status == "PULADA") return ApiResponse<OnboardingChecklistItemDto>.Fail("A etapa foi pulada; restaure-a para concluir.", 409);
            if (!alvo.PreRequisitoAtendido)
                return ApiResponse<OnboardingChecklistItemDto>.Fail("A etapa anterior (" + alvo.Row.PreRequisitoCodigo + ") ainda nao foi atendida.", 409);
            if (!alvo.Atendida && alvo.Row.CriterioCodigo is not null)
                return ApiResponse<OnboardingChecklistItemDto>.Fail(alvo.Row.Titulo + " ainda nao esta concluida: " + alvo.Evidencia, 409);
            await using (var tx = await cn.BeginTransactionAsync())
            {
                await cn.ExecuteAsync(new CommandDefinition(@"update plantaopro.tenant_onboarding_checklist
set concluido=true, concluido_em=coalesce(concluido_em,now()), status='CONCLUIDO', origem_conclusao=@origem, evidencia=@ev, avaliada_em=now(), reg_update=now()
where id=@id", new { id = etapaId, origem = alvo.Row.CriterioCodigo is null ? "MANUAL" : "AUTOMATICO", ev = alvo.Evidencia }, tx));
                await tx.CommitAsync();
            }
            // O resumo (progresso/status/proxima) e SEMPRE derivado pelo reavaliador, nunca chutado aqui.
            var apos = await ReavaliarAsync(tenantId, clienteId);
            return ApiResponse<OnboardingChecklistItemDto>.Ok(apos.Data!.First(d => d.Id == etapaId), "Etapa concluida.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao concluir etapa {EtapaId} do tenant {TenantId}", etapaId, tenantId);
            return ApiResponse<OnboardingChecklistItemDto>.Fail("Nao foi possivel concluir a etapa.", 500);
        }
    }

    /// <summary>Pular so vale para etapa opcional; a justificativa e persistida (auditoria).</summary>
    public async Task<ApiResponse<OnboardingChecklistItemDto>> PularEtapaAsync(Guid tenantId, Guid clienteId, Guid etapaId, string? justificativa)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            var efetivas = await CalcularAsync(cn, tenantId, clienteId);
            var alvo = efetivas.FirstOrDefault(e => e.Row.Id == etapaId);
            if (alvo is null) return ApiResponse<OnboardingChecklistItemDto>.Fail("Etapa nao encontrada neste cliente.", 404);
            if (alvo.Row.Obrigatorio)
                return ApiResponse<OnboardingChecklistItemDto>.Fail("Etapa obrigatoria nao pode ser pulada: " + alvo.Row.Titulo + ".", 409);
            await using (var tx = await cn.BeginTransactionAsync())
            {
                await cn.ExecuteAsync(new CommandDefinition(@"update plantaopro.tenant_onboarding_checklist
set pulada=true, motivo_pular=@motivo, status='PULADA', reg_update=now()
where id=@id", new { id = etapaId, motivo = string.IsNullOrWhiteSpace(justificativa) ? "Sem justificativa informada" : justificativa.Trim() }, tx));
                await tx.CommitAsync();
            }
            var apos = await ReavaliarAsync(tenantId, clienteId);
            return ApiResponse<OnboardingChecklistItemDto>.Ok(apos.Data!.First(d => d.Id == etapaId), "Etapa opcional pulada (registrado).");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao pular etapa {EtapaId} do tenant {TenantId}", etapaId, tenantId);
            return ApiResponse<OnboardingChecklistItemDto>.Fail("Nao foi possivel pular a etapa.", 500);
        }
    }

    /// <summary>Desfazer o pulo de etapa opcional (volto a avaliar normalmente).</summary>
    public async Task<ApiResponse<OnboardingChecklistItemDto>> RestaurarEtapaAsync(Guid tenantId, Guid clienteId, Guid etapaId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            var afetadas = await cn.ExecuteAsync("update plantaopro.tenant_onboarding_checklist set pulada=false, motivo_pular=null, reg_update=now() where id=@id and tenant_id=@tenantId and reg_status='A'", new { id = etapaId, tenantId });
            if (afetadas == 0) return ApiResponse<OnboardingChecklistItemDto>.Fail("Etapa nao encontrada neste cliente.", 404);
            var apos = await ReavaliarAsync(tenantId, clienteId);
            return ApiResponse<OnboardingChecklistItemDto>.Ok(apos.Data!.First(d => d.Id == etapaId), "Etapa restaurada.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao restaurar etapa {EtapaId} do tenant {TenantId}", etapaId, tenantId);
            return ApiResponse<OnboardingChecklistItemDto>.Fail("Nao foi possivel restaurar a etapa.", 500);
        }
    }

    /// <summary>
    /// Reinicio honesto: limpa apenas conclusoes MANUAIS (a verdade humana pode ser revista),
    /// re-sincroniza com os contratos e reavalia contra os dados. Conclusoes automaticas nunca
    /// sao apagadas a mão - elas seguem os dados do tenant.
    /// </summary>
    public async Task<ApiResponse<IEnumerable<OnboardingChecklistItemDto>>> ReiniciarAsync(Guid tenantId, Guid clienteId)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            await cn.OpenAsync();
            await cn.ExecuteAsync(@"update plantaopro.tenant_onboarding_checklist
set concluido=false, concluido_em=null, status='PENDENTE', origem_conclusao=null, pulada=false, motivo_pular=null, reg_update=now()
where tenant_id=@tenantId and reg_status='A' and (lower(coalesce(origem_conclusao,''))='manual' or coalesce(pulada,false)=true)", new { tenantId });
            return await SincronizarAsync(tenantId, clienteId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao reiniciar onboarding do tenant {TenantId}", tenantId);
            return ApiResponse<IEnumerable<OnboardingChecklistItemDto>>.Fail("Nao foi possivel reiniciar o onboarding.", 500);
        }
    }

    /// <summary>Status consolidado (linha-mestre + contagem derivada) para painel/Wizard.</summary>
    public async Task<ApiResponse<OnboardingStatusDto?>> StatusAsync(Guid tenantId)
    {
        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        var dto = await cn.QueryFirstOrDefaultAsync<OnboardingStatusDto>(@"select tenant_id as ""TenantId"", cliente_id as ""ClienteId"", coalesce(status,'') as ""Status"", progresso as ""Progresso"", coalesce(proxima_acao,'') as ""ProximaAcao""
from plantaopro.tenant_onboarding where tenant_id=@tenantId and reg_status='A' order by reg_date desc limit 1", new { tenantId });
        return dto is null
            ? ApiResponse<OnboardingStatusDto?>.Fail("Onboarding não iniciado para este cliente.", 404)
            : ApiResponse<OnboardingStatusDto?>.Ok(dto);
    }
}
