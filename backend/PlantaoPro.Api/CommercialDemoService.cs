using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class CommercialDemoService
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Guid, PublicLeadDto> Leads = new();
    private static readonly Dictionary<Guid, CommercialProposalDto> Proposals = new();
    private static readonly Dictionary<Guid, ModuleDto> Modules = SeedModules().ToDictionary(x => x.Id);
    private static readonly Dictionary<Guid, FeatureFlagDto> FeatureFlags = SeedFlags().ToDictionary(x => x.Id);
    private static readonly HashSet<string> DemoMarkers = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime? LastDemoGeneration;
    private readonly IAuditService _audit;
    private readonly ILogger<CommercialDemoService> _logger;

    public CommercialDemoService(IAuditService audit, ILogger<CommercialDemoService> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public Task<ApiResponse<LandingContentDto>> LandingAsync()
    {
        return Task.FromResult(ApiResponse<LandingContentDto>.Ok(new LandingContentDto
        {
            Titulo = "Gestão de plantões médicos pronta para hospitais, clínicas e redes de saúde",
            Subtitulo = "O PlantãoPro conecta coordenação, médicos, financeiro e parceiros em uma operação SaaS multi-tenant, com white label, auditoria e billing.",
            Ctas = new List<string> { "Começar agora", "Agendar demonstração", "Ver planos" },
            Secoes = new List<LandingSectionDto>
            {
                Section("problema", "Planilhas não escalam uma operação médica", "Reduza retrabalho, falhas de cobertura, convites manuais e baixa rastreabilidade.", "Plantões descobertos em destaque", "Convites com histórico", "Financeiro integrado"),
                Section("solucao", "Operação centralizada ponta a ponta", "Do cadastro do hospital ao pagamento médico, a jornada fica auditável e organizada por tenant.", "Escalas e convites", "Onboarding guiado", "Dashboards executivos"),
                Section("white-label", "White label para consultorias e parceiros", "Personalize marca, domínio, módulos e ofertas para revenda B2B.", "Identidade por cliente", "Templates reutilizáveis", "Portal do parceiro"),
                Section("lgpd", "Segurança, LGPD e governança", "Permissões por perfil, isolamento multi-tenant, auditoria e registros operacionais.", "Controle de acesso", "Auditoria", "Governança de módulos")
            },
            Faq = FaqItems(),
            CasosUso = UseCases()
        }));
    }

    public Task<ApiResponse<IEnumerable<LandingFaqDto>>> FaqAsync() => Task.FromResult(ApiResponse<IEnumerable<LandingFaqDto>>.Ok(FaqItems()));
    public Task<ApiResponse<IEnumerable<UseCaseDto>>> UseCasesAsync() => Task.FromResult(ApiResponse<IEnumerable<UseCaseDto>>.Ok(UseCases()));

    public async Task<ApiResponse<PublicLeadDto>> RegisterLeadAsync(PublicLeadRequest request, string? ip)
    {
        try
        {
            var validation = ValidateLead(request);
            if (validation.Count > 0) return ApiResponse<PublicLeadDto>.Fail("Dados inválidos para cadastro do lead.", 400, validation);
            if (!string.IsNullOrWhiteSpace(request.Website)) return ApiResponse<PublicLeadDto>.Fail("Não foi possível registrar o lead.", 400);
            var dto = new PublicLeadDto
            {
                Id = Guid.NewGuid(),
                Nome = request.Nome.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                Telefone = request.Telefone.Trim(),
                Empresa = request.Empresa.Trim(),
                Origem = Normalize(request.Origem, "LANDING"),
                Status = "NOVO_COMERCIAL",
                RegDate = DateTime.UtcNow
            };
            lock (Gate) Leads[dto.Id] = dto;
            await AuditAsync("PUBLIC_LEAD", dto.Id, "CRIAR", new { dto.Id, dto.Email, dto.Empresa, dto.Origem }, ip);
            return ApiResponse<PublicLeadDto>.Ok(dto, "Lead enviado para o time comercial.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao registrar lead público");
            return ApiResponse<PublicLeadDto>.Fail("Não foi possível registrar o lead.", 500);
        }
    }

    public async Task<ApiResponse<PublicLeadDto>> ScheduleDemoAsync(DemoScheduleRequest request, string? ip)
    {
        request.Origem = string.IsNullOrWhiteSpace(request.Origem) ? "DEMO" : request.Origem;
        var result = await RegisterLeadAsync(request, ip);
        if (!result.Success || result.Data is null) return result;
        result.Data.Status = "DEMO_AGENDADA";
        lock (Gate) Leads[result.Data.Id] = result.Data;
        await AuditAsync("PUBLIC_DEMO", result.Data.Id, "AGENDAR", new { result.Data.Id, request.DataPreferida, request.TamanhoOperacao }, ip);
        return ApiResponse<PublicLeadDto>.Ok(result.Data, "Demonstração solicitada. O comercial fará contato para confirmar o horário.");
    }

    public Task<ApiResponse<IEnumerable<SimulatorQuestionDto>>> SimulatorQuestionsAsync()
    {
        var questions = new List<SimulatorQuestionDto>
        {
            Question("medicos", "Quantos médicos serão cadastrados?", "number"),
            Question("unidades", "Quantos hospitais/unidades?", "number"),
            Question("plantoesMes", "Quantos plantões por mês?", "number"),
            Question("usuariosAdministrativos", "Quantos usuários administrativos?", "number"),
            Question("whiteLabel", "Deseja white label?", "boolean", "Sim", "Não"),
            Question("api", "Deseja API?", "boolean", "Sim", "Não"),
            Question("bi", "Deseja BI?", "boolean", "Sim", "Não"),
            Question("operacaoAssistida", "Deseja operação assistida?", "boolean", "Sim", "Não"),
            Question("suportePrioritario", "Deseja suporte prioritário?", "boolean", "Sim", "Não"),
            Question("revenda", "Deseja revender para outros clientes?", "boolean", "Sim", "Não"),
            Question("dominioProprio", "Deseja domínio próprio?", "boolean", "Sim", "Não"),
            Question("integracoes", "Deseja integrações?", "boolean", "Sim", "Não")
        };
        return Task.FromResult(ApiResponse<IEnumerable<SimulatorQuestionDto>>.Ok(questions));
    }

    public async Task<ApiResponse<PlanSimulatorResultDto>> CalculatePlanAsync(PlanSimulatorRequest request, string? ip)
    {
        try
        {
            if (request.Medicos <= 0 || request.Unidades <= 0 || request.PlantoesMes <= 0) return ApiResponse<PlanSimulatorResultDto>.Fail("Informe médicos, unidades e plantões mensais.", 400);
            var enterprise = request.Medicos > 200 || request.Unidades > 10 || request.PlantoesMes > 1000 || request.Revenda || request.OperacaoAssistida;
            var professional = !enterprise && (request.Medicos > 40 || request.Unidades > 2 || request.PlantoesMes > 180 || request.WhiteLabel || request.Api || request.Bi || request.Integracoes);
            var plan = enterprise ? "ENTERPRISE" : professional ? "PROFISSIONAL" : "ESSENCIAL";
            var basePrice = enterprise ? 2990m : professional ? 1290m : 490m;
            var addons = new List<string>();
            if (request.WhiteLabel) addons.Add("White label e domínio próprio");
            if (request.Api || request.Integracoes) addons.Add("API e integrações");
            if (request.Bi) addons.Add("BI executivo");
            if (request.OperacaoAssistida) addons.Add("Operação assistida");
            if (request.SuportePrioritario) addons.Add("Suporte prioritário");
            if (request.Revenda) addons.Add("Portal parceiro/revenda");
            var result = new PlanSimulatorResultDto
            {
                HistoricoId = Guid.NewGuid(),
                PlanoRecomendado = plan,
                Justificativa = enterprise ? "Volume, revenda ou operação assistida indicam contrato enterprise." : professional ? "A operação precisa de módulos avançados e maior capacidade." : "O volume informado cabe em uma implantação self-service controlada.",
                MensalidadeEstimada = basePrice + addons.Count * 190m,
                Limites = new List<string> { $"Médicos informados: {request.Medicos}", $"Unidades informadas: {request.Unidades}", $"Plantões/mês: {request.PlantoesMes}", $"Usuários administrativos: {request.UsuariosAdministrativos}" },
                RecursosInclusos = new List<string> { "Multi-tenant", "Convites médicos", "Financeiro médico", "Auditoria", "Onboarding guiado" },
                ModulosSugeridos = addons,
                ProximoPasso = enterprise ? "Solicitar proposta customizada" : "Começar cadastro self-service ou receber proposta"
            };
            await AuditAsync("SIMULADOR_PLANOS", result.HistoricoId, "CALCULAR", new { result.PlanoRecomendado, result.MensalidadeEstimada, request.Origem }, ip);
            return ApiResponse<PlanSimulatorResultDto>.Ok(result, "Plano recomendado calculado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular simulador de planos");
            return ApiResponse<PlanSimulatorResultDto>.Fail("Não foi possível calcular o plano ideal.", 500);
        }
    }

    public async Task<ApiResponse<PublicLeadDto>> LeadFromSimulationAsync(PublicLeadRequest request, string? ip)
    {
        request.Origem = "SIMULADOR";
        return await RegisterLeadAsync(request, ip);
    }

    public Task<ApiResponse<IEnumerable<CommercialProposalDto>>> ListProposalsAsync()
    {
        lock (Gate) return Task.FromResult(ApiResponse<IEnumerable<CommercialProposalDto>>.Ok(Proposals.Values.OrderByDescending(x => x.RegDate).Take(100).ToList()));
    }

    public Task<ApiResponse<CommercialProposalDto>> GetProposalAsync(Guid id)
    {
        lock (Gate) return Task.FromResult(Proposals.TryGetValue(id, out var p) ? ApiResponse<CommercialProposalDto>.Ok(p) : ApiResponse<CommercialProposalDto>.Fail("Proposta não encontrada.", 404));
    }

    public async Task<ApiResponse<CommercialProposalDto>> SaveProposalAsync(Guid? id, CommercialProposalRequest request, bool globalAdmin, string? ip)
    {
        try
        {
            var errors = ValidateProposal(request, globalAdmin);
            if (errors.Count > 0) return ApiResponse<CommercialProposalDto>.Fail("Dados inválidos para proposta.", 400, errors);
            var proposalId = id.GetValueOrDefault(Guid.NewGuid());
            CommercialProposalDto dto;
            lock (Gate)
            {
                var timeline = Proposals.TryGetValue(proposalId, out var existing) ? existing.Timeline.ToList() : new List<string>();
                timeline.Add($"{DateTime.UtcNow:O} - {(id.HasValue ? "Atualizada" : "Criada")}");
                dto = new CommercialProposalDto
                {
                    Id = proposalId,
                    ClienteNome = request.ClienteNome.Trim(),
                    ClienteEmail = request.ClienteEmail.Trim().ToLowerInvariant(),
                    Empresa = request.Empresa.Trim(),
                    Plano = Normalize(request.Plano, "PROFISSIONAL"),
                    Modulos = request.Modulos ?? Array.Empty<string>(),
                    TaxaSetup = request.TaxaSetup,
                    Mensalidade = request.Mensalidade,
                    DescontoPercentual = request.DescontoPercentual,
                    Validade = request.Validade,
                    Sla = request.Sla,
                    PrazoImplantacao = request.PrazoImplantacao,
                    CondicoesComerciais = request.CondicoesComerciais,
                    Observacoes = request.Observacoes,
                    ResponsavelComercial = request.ResponsavelComercial,
                    Status = id.HasValue && existing is not null ? existing.Status : "RASCUNHO",
                    TotalPrimeiroMes = request.TaxaSetup + request.Mensalidade * (1 - request.DescontoPercentual / 100m),
                    RegDate = id.HasValue && existing is not null ? existing.RegDate : DateTime.UtcNow,
                    Timeline = timeline
                };
                Proposals[proposalId] = dto;
            }
            await AuditAsync("PROPOSTA_COMERCIAL", proposalId, id.HasValue ? "ATUALIZAR" : "CRIAR", new { dto.Id, dto.Empresa, dto.Plano, dto.DescontoPercentual }, ip);
            return ApiResponse<CommercialProposalDto>.Ok(dto, "Proposta salva.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar proposta comercial");
            return ApiResponse<CommercialProposalDto>.Fail("Não foi possível salvar proposta.", 500);
        }
    }

    public async Task<ApiResponse<CommercialProposalDto>> ChangeProposalStatusAsync(Guid id, string status, string? reason, string? ip)
    {
        try
        {
            lock (Gate)
            {
                if (!Proposals.TryGetValue(id, out var proposal)) return ApiResponse<CommercialProposalDto>.Fail("Proposta não encontrada.", 404);
                if (status == "APROVADA" && proposal.Validade.Date < DateTime.UtcNow.Date) return ApiResponse<CommercialProposalDto>.Fail("Proposta vencida não pode ser aprovada.", 400);
                if (status == "RECUSADA" && string.IsNullOrWhiteSpace(reason)) return ApiResponse<CommercialProposalDto>.Fail("Motivo da recusa é obrigatório.", 400);
                var timeline = proposal.Timeline.ToList();
                timeline.Add($"{DateTime.UtcNow:O} - {status}{(string.IsNullOrWhiteSpace(reason) ? string.Empty : ": " + reason.Trim())}");
                proposal.Status = status;
                proposal.Timeline = timeline;
                Proposals[id] = proposal;
            }
            await AuditAsync("PROPOSTA_COMERCIAL", id, status, new { id, reason }, ip);
            CommercialProposalDto updated;
            lock (Gate) updated = Proposals[id];
            return ApiResponse<CommercialProposalDto>.Ok(updated, "Status da proposta atualizado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao alterar proposta comercial {Id}", id);
            return ApiResponse<CommercialProposalDto>.Fail("Não foi possível alterar proposta.", 500);
        }
    }

    public async Task<ApiResponse<CommercialProposalDto>> GenerateItemsAsync(Guid id, string? ip)
    {
        lock (Gate)
        {
            if (!Proposals.TryGetValue(id, out var proposal)) return ApiResponse<CommercialProposalDto>.Fail("Proposta não encontrada.", 404);
            var modules = proposal.Modulos.ToList();
            if (!modules.Contains("Onboarding guiado")) modules.Add("Onboarding guiado");
            if (!modules.Contains("Auditoria LGPD")) modules.Add("Auditoria LGPD");
            proposal.Modulos = modules.ToArray();
            proposal.Timeline = proposal.Timeline.Concat(new[] { $"{DateTime.UtcNow:O} - Itens gerados automaticamente" }).ToList();
            Proposals[id] = proposal;
        }
        await AuditAsync("PROPOSTA_COMERCIAL", id, "GERAR_ITENS", new { id }, ip);
        CommercialProposalDto updated;
        lock (Gate) updated = Proposals[id];
        return ApiResponse<CommercialProposalDto>.Ok(updated, "Itens comerciais gerados.");
    }

    public async Task<ApiResponse<CommercialProposalDto>> ConvertProposalAsync(Guid id, string? ip)
    {
        var result = await ChangeProposalStatusAsync(id, "CONVERTIDA_CLIENTE", "Tenant, cliente, assinatura e onboarding gerados em modo demonstrável.", ip);
        if (result.Success) await AuditAsync("CONVERSAO_CLIENTE", id, "CRIAR_TENANT_ASSINATURA_ONBOARDING", new { PropostaId = id }, ip);
        return result;
    }

    public Task<ApiResponse<string>> PreviewProposalAsync(Guid id)
    {
        lock (Gate)
        {
            if (!Proposals.TryGetValue(id, out var p)) return Task.FromResult(ApiResponse<string>.Fail("Proposta não encontrada.", 404));
            var html = $"<article><h1>Proposta PlantãoPro - {System.Net.WebUtility.HtmlEncode(p.Empresa)}</h1><p>Plano {System.Net.WebUtility.HtmlEncode(p.Plano)}</p><p>Setup: {p.TaxaSetup:C} | Mensalidade: {p.Mensalidade:C} | Desconto: {p.DescontoPercentual:N1}%</p><p>SLA: {System.Net.WebUtility.HtmlEncode(p.Sla)}</p><p>Validade: {p.Validade:dd/MM/yyyy}</p></article>";
            return Task.FromResult(ApiResponse<string>.Ok(html));
        }
    }

    public Task<ApiResponse<PortalResumoDto>> AdminSaasResumoAsync() => Task.FromResult(ApiResponse<PortalResumoDto>.Ok(new PortalResumoDto { Titulo = "Admin SaaS MNSOFT", Kpis = Kpis("Leads do mês", Leads.Count, "Propostas abertas", Proposals.Values.LongCount(x => x.Status != "CONVERTIDA_CLIENTE"), "MRR estimado", Proposals.Values.Sum(x => x.Mensalidade)), Alertas = new List<string> { "Clientes em implantação exigem acompanhamento", "Verificar propostas com validade próxima" }, Acoes = new List<string> { "Revisar leads", "Acompanhar propostas", "Monitorar billing" } }));
    public Task<ApiResponse<PortalResumoDto>> ClientePortalResumoAsync(Guid? tenantId) => Task.FromResult(ApiResponse<PortalResumoDto>.Ok(new PortalResumoDto { Titulo = "Portal do cliente contratante", Kpis = Kpis("Plano atual", 1, "Plantões do mês", 128, "Uso de médicos", 73), Alertas = new List<string> { "Uso próximo de 80% do limite de médicos", "Onboarding financeiro pendente" }, Acoes = new List<string> { "Solicitar upgrade", "Configurar white label", "Abrir suporte" } }));
    public Task<ApiResponse<PortalResumoDto>> ParceiroPortalResumoAsync() => Task.FromResult(ApiResponse<PortalResumoDto>.Ok(new PortalResumoDto { Titulo = "Portal do parceiro/revendedor", Kpis = Kpis("Leads gerados", 12, "Clientes ativos", 4, "Comissão prevista", 3400), Alertas = new List<string> { "Duas propostas aguardam aprovação do cliente" }, Acoes = new List<string> { "Gerar lead", "Criar proposta", "Baixar materiais" } }));

    public Task<ApiResponse<IEnumerable<ModuleDto>>> ListModulesAsync()
    {
        lock (Gate) return Task.FromResult(ApiResponse<IEnumerable<ModuleDto>>.Ok(Modules.Values.OrderBy(x => x.Categoria).ThenBy(x => x.Nome).ToList()));
    }

    public Task<ApiResponse<ModuleDto>> GetModuleAsync(Guid id)
    {
        lock (Gate) return Task.FromResult(Modules.TryGetValue(id, out var m) ? ApiResponse<ModuleDto>.Ok(m) : ApiResponse<ModuleDto>.Fail("Módulo não encontrado.", 404));
    }

    public async Task<ApiResponse<ModuleDto>> SaveModuleAsync(Guid? id, UpsertModuleRequest request, string? ip)
    {
        if (string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<ModuleDto>.Fail("Código e nome são obrigatórios.", 400);
        var module = new ModuleDto { Id = id.GetValueOrDefault(Guid.NewGuid()), Codigo = request.Codigo.Trim().ToUpperInvariant(), Nome = request.Nome.Trim(), Descricao = request.Descricao.Trim(), Categoria = Normalize(request.Categoria, "OPERACIONAL"), Global = request.Global, Beta = request.Beta, Oculto = request.Oculto, Habilitado = request.Habilitado };
        lock (Gate) Modules[module.Id] = module;
        await AuditAsync("MODULO_SISTEMA", module.Id, id.HasValue ? "ATUALIZAR" : "CRIAR", new { module.Codigo, module.Habilitado }, ip);
        return ApiResponse<ModuleDto>.Ok(module, "Módulo salvo.");
    }

    public async Task<ApiResponse<ModuleDto>> ToggleModuleForTenantAsync(Guid id, TenantModuleRequest request, bool enabled, string? ip)
    {
        lock (Gate)
        {
            if (!Modules.TryGetValue(id, out var module)) return ApiResponse<ModuleDto>.Fail("Módulo não encontrado.", 404);
            module.Habilitado = enabled;
            Modules[id] = module;
        }
        await AuditAsync("MODULO_TENANT", id, enabled ? "HABILITAR" : "DESABILITAR", new { request.TenantId, request.Justificativa }, ip);
        ModuleDto updated;
        lock (Gate) updated = Modules[id];
        return ApiResponse<ModuleDto>.Ok(updated, enabled ? "Módulo habilitado para tenant." : "Módulo desabilitado para tenant.");
    }

    public Task<ApiResponse<IEnumerable<FeatureFlagDto>>> ListFeatureFlagsAsync()
    {
        lock (Gate) return Task.FromResult(ApiResponse<IEnumerable<FeatureFlagDto>>.Ok(FeatureFlags.Values.OrderBy(x => x.Chave).ToList()));
    }

    public async Task<ApiResponse<FeatureFlagDto>> UpdateFeatureFlagAsync(Guid id, UpdateFeatureFlagRequest request, string? ip)
    {
        lock (Gate)
        {
            if (!FeatureFlags.TryGetValue(id, out var flag)) return ApiResponse<FeatureFlagDto>.Fail("Feature flag não encontrada.", 404);
            flag.Habilitada = request.Habilitada;
            flag.TenantId = request.TenantId;
            FeatureFlags[id] = flag;
        }
        await AuditAsync("FEATURE_FLAG", id, request.Habilitada ? "HABILITAR" : "DESABILITAR", new { request.TenantId, request.Justificativa }, ip);
        FeatureFlagDto updated;
        lock (Gate) updated = FeatureFlags[id];
        return ApiResponse<FeatureFlagDto>.Ok(updated, "Feature flag atualizada.");
    }

    public async Task<ApiResponse<DemoStatusDto>> GenerateDemoDataAsync(string? ip)
    {
        lock (Gate)
        {
            DemoMarkers.Add("Hospital São Lucas Demo");
            DemoMarkers.Add("Clínica Vida Demo");
            DemoMarkers.Add("Rede White Label Demo");
            LastDemoGeneration = DateTime.UtcNow;
        }
        await AuditAsync("DEMO_MODE", Guid.Empty, "GERAR_DADOS", new { Total = 3 }, ip);
        return await DemoStatusAsync();
    }

    public async Task<ApiResponse<DemoStatusDto>> ClearDemoDataAsync(string? ip)
    {
        lock (Gate) DemoMarkers.Clear();
        await AuditAsync("DEMO_MODE", Guid.Empty, "LIMPAR_DADOS", new { ApenasDemo = true }, ip);
        return await DemoStatusAsync();
    }

    public Task<ApiResponse<DemoStatusDto>> DemoStatusAsync()
    {
        lock (Gate) return Task.FromResult(ApiResponse<DemoStatusDto>.Ok(new DemoStatusDto { DadosGerados = DemoMarkers.Count > 0, TenantsDemo = DemoMarkers.Count, LeadsDemo = Leads.Values.LongCount(x => x.Empresa.Contains("Demo", StringComparison.OrdinalIgnoreCase)), UltimaGeracao = LastDemoGeneration, Roteiros = DemoRoutes() }));
    }

    public Task<ApiResponse<IEnumerable<string>>> DemoRoutesAsync() => Task.FromResult(ApiResponse<IEnumerable<string>>.Ok(DemoRoutes()));

    private async Task AuditAsync(string entity, Guid id, string action, object payload, string? ip)
    {
        try { await _audit.LogAsync(null, action, entity, id == Guid.Empty ? null : id, System.Text.Json.JsonSerializer.Serialize(payload), ip: ip); } catch (Exception ex) { _logger.LogWarning(ex, "Falha ao auditar {Entity} {Action}", entity, action); }
    }

    private static LandingSectionDto Section(string key, string title, string description, params string[] benefits) => new LandingSectionDto { Chave = key, Titulo = title, Descricao = description, Beneficios = benefits };
    private static SimulatorQuestionDto Question(string key, string text, string type, params string[] options) => new SimulatorQuestionDto { Chave = key, Pergunta = text, Tipo = type, Opcoes = options };
    private static string Normalize(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();
    private static List<string> ValidateLead(PublicLeadRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Nome)) errors.Add("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@')) errors.Add("E-mail válido é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Telefone) || request.Telefone.Trim().Length < 8) errors.Add("Telefone é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Empresa)) errors.Add("Empresa é obrigatória.");
        return errors;
    }
    private static List<string> ValidateProposal(CommercialProposalRequest request, bool globalAdmin)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.ClienteNome)) errors.Add("Cliente é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.ClienteEmail) || !request.ClienteEmail.Contains('@')) errors.Add("E-mail do cliente é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.Empresa)) errors.Add("Empresa é obrigatória.");
        if (request.Mensalidade < 0 || request.TaxaSetup < 0) errors.Add("Valores não podem ser negativos.");
        if (request.DescontoPercentual > 20m && !globalAdmin) errors.Add("Desconto acima de 20% exige administrador global.");
        if (request.Validade.Date < DateTime.UtcNow.Date) errors.Add("Validade não pode estar vencida na criação.");
        return errors;
    }
    private static IEnumerable<LandingFaqDto> FaqItems() => new List<LandingFaqDto> { new LandingFaqDto { Pergunta = "Posso cadastrar minha empresa sozinho?", Resposta = "Sim, o self-service cria tenant, assinatura, administrador e onboarding." }, new LandingFaqDto { Pergunta = "Existe white label?", Resposta = "Sim, com identidade visual por cliente e templates reaproveitáveis." }, new LandingFaqDto { Pergunta = "A operação é multi-tenant?", Resposta = "Sim, dados e permissões são isolados por tenant." } };
    private static IEnumerable<UseCaseDto> UseCases() => new List<UseCaseDto> { new UseCaseDto { Segmento = "Hospital", Dor = "Cobertura de plantões crítica", Resultado = "Centralização de convites, escalas e pagamentos" }, new UseCaseDto { Segmento = "Clínica", Dor = "Escalas manuais", Resultado = "Operação enxuta com plano self-service" }, new UseCaseDto { Segmento = "Parceiro white label", Dor = "Revender sem construir plataforma", Resultado = "Portal parceiro, propostas e módulos configuráveis" } };
    private static IEnumerable<PortalKpiDto> Kpis(string a, decimal av, string b, decimal bv, string c, decimal cv) => new List<PortalKpiDto> { new PortalKpiDto { Nome = a, Valor = av, Unidade = string.Empty, Severidade = "INFO" }, new PortalKpiDto { Nome = b, Valor = bv, Unidade = string.Empty, Severidade = "ATENCAO" }, new PortalKpiDto { Nome = c, Valor = cv, Unidade = "R$", Severidade = "SUCESSO" } };
    private static IEnumerable<ModuleDto> SeedModules() => new List<ModuleDto> { new ModuleDto { Id = Guid.NewGuid(), Codigo = "PLANTOES", Nome = "Gestão de plantões", Descricao = "Central de plantões, filtros e publicação.", Categoria = "OPERACIONAL", Global = true, Habilitado = true }, new ModuleDto { Id = Guid.NewGuid(), Codigo = "WHITE_LABEL", Nome = "White label", Descricao = "Marca, cores, domínio e templates.", Categoria = "SAAS", Habilitado = true }, new ModuleDto { Id = Guid.NewGuid(), Codigo = "BI_EXECUTIVO", Nome = "BI executivo", Descricao = "Dashboards comercial, operação e CS.", Categoria = "ANALYTICS", Beta = true, Habilitado = true } };
    private static IEnumerable<FeatureFlagDto> SeedFlags() => new List<FeatureFlagDto> { new FeatureFlagDto { Id = Guid.NewGuid(), Chave = "comercial.propostas.preview", Descricao = "Preview HTML imprimível de propostas", Habilitada = true }, new FeatureFlagDto { Id = Guid.NewGuid(), Chave = "demo.seed.operacional", Descricao = "Geração de dados fictícios para demonstração", Habilitada = true } };
    private static IEnumerable<string> DemoRoutes() => new List<string> { "Roteiro hospital: lead -> proposta -> cliente -> plantão -> convite -> pagamento", "Roteiro parceiro: portal -> lead -> proposta white label -> comissão", "Roteiro CS: onboarding -> uso -> alertas -> upgrade" };
}
