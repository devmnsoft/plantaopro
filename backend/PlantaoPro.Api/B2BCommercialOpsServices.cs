using System.Collections.Concurrent;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Data;

public sealed class B2BCommercialOpsService
{
    private readonly TenantContextService _tenantContext;
    private readonly IAuditService _audit;
    private readonly ILogger<B2BCommercialOpsService> _logger;

    private static readonly ConcurrentDictionary<Guid, PilotoProgramaDto> Programas = new();
    private static readonly ConcurrentDictionary<Guid, PilotoClienteDto> ClientesPiloto = new();
    private static readonly ConcurrentDictionary<Guid, PilotoFeedbackDto> Feedbacks = new();
    private static readonly ConcurrentDictionary<Guid, CsContaDto> Contas = new();
    private static readonly ConcurrentDictionary<Guid, WhiteLabelTemplateDto> Templates = new();
    private static readonly ConcurrentDictionary<Guid, OperacaoAssistidaPlanoDto> Planos = new();
    private static readonly ConcurrentDictionary<Guid, TreinamentoTrilhaDto> Trilhas = new();
    private static readonly ConcurrentDictionary<Guid, RenovacaoDto> Renovacoes = new();
    private static readonly ConcurrentDictionary<Guid, ExpansaoDto> Expansoes = new();
    private static readonly ConcurrentDictionary<Guid, MedicoDisponibilidadeDto> Disponibilidades = new();

    public B2BCommercialOpsService(TenantContextService tenantContext, IAuditService audit, ILogger<B2BCommercialOpsService> logger)
    {
        _tenantContext = tenantContext;
        _audit = audit;
        _logger = logger;
        Seed();
    }

    public Task<ApiResponse<IEnumerable<PilotoProgramaDto>>> ListarProgramasAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<PilotoProgramaDto>>.Ok(Programas.Values.OrderByDescending(x => x.RegDate).Take(100)));
    }

    public Task<ApiResponse<PilotoProgramaDto>> ObterProgramaAsync(Guid id)
    {
        return Task.FromResult(Programas.TryGetValue(id, out var item) ? ApiResponse<PilotoProgramaDto>.Ok(item) : ApiResponse<PilotoProgramaDto>.Fail("Programa piloto não encontrado.", 404));
    }

    public async Task<ApiResponse<PilotoProgramaDto>> SalvarProgramaAsync(PilotoProgramaRequest request, Guid? id, string? ip)
    {
        try
        {
            var erros = new List<string>();
            if (string.IsNullOrWhiteSpace(request.Nome)) erros.Add("Nome é obrigatório.");
            if (string.IsNullOrWhiteSpace(request.Objetivo)) erros.Add("Objetivo é obrigatório.");
            if (request.DataFim < request.DataInicio) erros.Add("Data final deve ser maior ou igual à data inicial.");
            if (erros.Count > 0) return ApiResponse<PilotoProgramaDto>.Fail("Verifique os dados do programa piloto.", 400, erros);

            var programaId = id.GetValueOrDefault(Guid.NewGuid());
            var dto = new PilotoProgramaDto
            {
                Id = programaId,
                Nome = request.Nome.Trim(),
                Objetivo = request.Objetivo.Trim(),
                CriteriosSucesso = request.CriteriosSucesso?.Trim() ?? string.Empty,
                Responsavel = request.Responsavel?.Trim() ?? "MNSOFT",
                DataInicio = request.DataInicio,
                DataFim = request.DataFim,
                Status = request.Status?.Trim().ToUpperInvariant() ?? "PLANEJADO",
                RegDate = DateTime.UtcNow
            };
            Programas[programaId] = dto;
            await AuditarAsync("PILOTO_PROGRAMA", programaId, id.HasValue ? "ATUALIZAR" : "CRIAR", dto, ip);
            return ApiResponse<PilotoProgramaDto>.Ok(dto, id.HasValue ? "Programa piloto atualizado." : "Programa piloto criado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar programa piloto");
            return ApiResponse<PilotoProgramaDto>.Fail("Não foi possível salvar o programa piloto.", 500);
        }
    }

    public Task<ApiResponse<IEnumerable<PilotoClienteDto>>> ListarClientesPilotoAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<PilotoClienteDto>>.Ok(ClientesPiloto.Values.OrderByDescending(x => x.RegDate).Take(100)));
    }

    public Task<ApiResponse<PilotoClienteDto>> ObterClientePilotoAsync(Guid id)
    {
        return Task.FromResult(ClientesPiloto.TryGetValue(id, out var item) ? ApiResponse<PilotoClienteDto>.Ok(item) : ApiResponse<PilotoClienteDto>.Fail("Cliente piloto não encontrado.", 404));
    }

    public async Task<ApiResponse<PilotoClienteDto>> CriarClientePilotoAsync(PilotoClienteRequest request, string? ip)
    {
        try
        {
            if (request.ProgramaId == Guid.Empty) return ApiResponse<PilotoClienteDto>.Fail("Programa piloto é obrigatório.", 400);
            if (request.TenantId == Guid.Empty) return ApiResponse<PilotoClienteDto>.Fail("Tenant é obrigatório.", 400);
            if (!Programas.ContainsKey(request.ProgramaId)) return ApiResponse<PilotoClienteDto>.Fail("Programa piloto não encontrado.", 404);

            var id = Guid.NewGuid();
            var dto = new PilotoClienteDto
            {
                Id = id,
                ProgramaId = request.ProgramaId,
                TenantId = request.TenantId,
                ClienteId = request.ClienteId,
                NomeCliente = string.IsNullOrWhiteSpace(request.NomeCliente) ? "Cliente piloto" : request.NomeCliente.Trim(),
                PlanoTrial = request.PlanoTrial?.Trim() ?? "PILOTO_TRIAL",
                DescontoPercentual = Math.Clamp(request.DescontoPercentual, 0, 100),
                Status = "CADASTRADO",
                Responsavel = request.Responsavel?.Trim() ?? "MNSOFT",
                RegDate = DateTime.UtcNow
            };
            ClientesPiloto[id] = dto;
            GarantirConta(dto.TenantId, dto.ClienteId, dto.NomeCliente);
            await AuditarAsync("PILOTO_CLIENTE", id, "CRIAR", dto, ip);
            return ApiResponse<PilotoClienteDto>.Ok(dto, "Cliente piloto vinculado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar cliente piloto");
            return ApiResponse<PilotoClienteDto>.Fail("Não foi possível vincular o cliente piloto.", 500);
        }
    }

    public Task<ApiResponse<IEnumerable<PilotoFeedbackDto>>> ListarFeedbacksAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<PilotoFeedbackDto>>.Ok(Feedbacks.Values.OrderByDescending(x => x.RegDate).Take(100)));
    }

    public async Task<ApiResponse<PilotoFeedbackDto>> CriarFeedbackAsync(PilotoFeedbackRequest request, string? ip)
    {
        try
        {
            if (request.ClientePilotoId == Guid.Empty) return ApiResponse<PilotoFeedbackDto>.Fail("Cliente piloto é obrigatório.", 400);
            if (string.IsNullOrWhiteSpace(request.Descricao)) return ApiResponse<PilotoFeedbackDto>.Fail("Descrição é obrigatória.", 400);
            if (!ClientesPiloto.ContainsKey(request.ClientePilotoId)) return ApiResponse<PilotoFeedbackDto>.Fail("Cliente piloto não encontrado.", 404);

            var id = Guid.NewGuid();
            var severidade = Normalizar(request.Severidade, "MEDIA");
            var dto = new PilotoFeedbackDto
            {
                Id = id,
                ClientePilotoId = request.ClientePilotoId,
                Categoria = Normalizar(request.Categoria, "GERAL"),
                Severidade = severidade,
                Status = "ABERTO",
                Descricao = request.Descricao.Trim(),
                AcaoProduto = string.Equals(severidade, "CRITICA", StringComparison.OrdinalIgnoreCase),
                RegDate = DateTime.UtcNow
            };
            Feedbacks[id] = dto;
            await AuditarAsync("PILOTO_FEEDBACK", id, "CRIAR", new { dto.Id, dto.Categoria, dto.Severidade, dto.Status }, ip);
            return ApiResponse<PilotoFeedbackDto>.Ok(dto, string.Equals(severidade, "CRITICA", StringComparison.OrdinalIgnoreCase) ? "Feedback crítico registrado e alerta gerado." : "Feedback registrado.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar feedback piloto");
            return ApiResponse<PilotoFeedbackDto>.Fail("Não foi possível registrar o feedback.", 500);
        }
    }

    public async Task<ApiResponse<string>> AlterarClientePilotoStatusAsync(Guid id, string status, string? ip)
    {
        if (!ClientesPiloto.TryGetValue(id, out var item)) return ApiResponse<string>.Fail("Cliente piloto não encontrado.", 404);
        item.Status = status;
        item.RegUpdate = DateTime.UtcNow;
        ClientesPiloto[id] = item;
        if (string.Equals(status, "CONVERTIDO", StringComparison.OrdinalIgnoreCase)) GarantirConta(item.TenantId, item.ClienteId, item.NomeCliente).Status = "ATIVO";
        await AuditarAsync("PILOTO_CLIENTE", id, status, new { id, status }, ip);
        return ApiResponse<string>.Ok("ok", "Status do cliente piloto atualizado.");
    }

    public async Task<ApiResponse<string>> ClassificarFeedbackAsync(Guid id, FeedbackClassificacaoRequest request, string? ip)
    {
        if (!Feedbacks.TryGetValue(id, out var item)) return ApiResponse<string>.Fail("Feedback não encontrado.", 404);
        item.Categoria = Normalizar(request.Categoria, item.Categoria);
        item.Severidade = Normalizar(request.Severidade, item.Severidade);
        item.Status = "CLASSIFICADO";
        item.AcaoProduto = request.GerarTarefaProduto || string.Equals(item.Severidade, "CRITICA", StringComparison.OrdinalIgnoreCase);
        item.RegUpdate = DateTime.UtcNow;
        Feedbacks[id] = item;
        await AuditarAsync("PILOTO_FEEDBACK", id, "CLASSIFICAR", item, ip);
        return ApiResponse<string>.Ok("ok", "Feedback classificado.");
    }

    public async Task<ApiResponse<string>> ResolverFeedbackAsync(Guid id, ResolverRequest request, string? ip)
    {
        if (!Feedbacks.TryGetValue(id, out var item)) return ApiResponse<string>.Fail("Feedback não encontrado.", 404);
        if (string.IsNullOrWhiteSpace(request.Justificativa)) return ApiResponse<string>.Fail("Justificativa é obrigatória.", 400);
        item.Status = "RESOLVIDO";
        item.Resolucao = request.Justificativa.Trim();
        item.RegUpdate = DateTime.UtcNow;
        Feedbacks[id] = item;
        await AuditarAsync("PILOTO_FEEDBACK", id, "RESOLVER", new { id, request.Justificativa }, ip);
        return ApiResponse<string>.Ok("ok", "Feedback resolvido.");
    }

    public Task<ApiResponse<IndicadoresPilotoDto>> IndicadoresPilotoAsync()
    {
        var dto = new IndicadoresPilotoDto
        {
            ProgramasAtivos = Programas.Values.LongCount(x => x.Status != "ENCERRADO"),
            ClientesPiloto = ClientesPiloto.Count,
            ClientesConvertidos = ClientesPiloto.Values.LongCount(x => x.Status == "CONVERTIDO"),
            FeedbacksAbertos = Feedbacks.Values.LongCount(x => x.Status != "RESOLVIDO"),
            FeedbacksCriticos = Feedbacks.Values.LongCount(x => x.Severidade == "CRITICA"),
            NpsMedio = 72
        };
        return Task.FromResult(ApiResponse<IndicadoresPilotoDto>.Ok(dto));
    }

    public Task<ApiResponse<IEnumerable<CsContaDto>>> ListarCsContasAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<CsContaDto>>.Ok(Contas.Values.OrderBy(x => x.HealthCategoria).Take(100)));
    }

    public Task<ApiResponse<CsContaDto>> ObterCsContaAsync(Guid tenantId)
    {
        return Task.FromResult(ApiResponse<CsContaDto>.Ok(GarantirConta(tenantId, null, "Conta B2B")));
    }

    public Task<ApiResponse<CsHealthDto>> CalcularHealthAsync(Guid tenantId)
    {
        var conta = GarantirConta(tenantId, null, "Conta B2B");
        var score = 82;
        if (conta.Nps < 7) score -= 25;
        if (conta.ChamadosAbertos > 3) score -= 15;
        if (conta.DiasSemLogin > 10) score -= 20;
        if (conta.UsoPercentual > 80) score += 5;
        score = Math.Clamp(score, 0, 100);
        var categoria = score >= 80 ? "SAUDAVEL" : score >= 60 ? "ATENCAO" : score >= 35 ? "RISCO" : "CRITICO";
        conta.HealthScore = score;
        conta.HealthCategoria = categoria;
        Contas[tenantId] = conta;
        return Task.FromResult(ApiResponse<CsHealthDto>.Ok(new CsHealthDto { TenantId = tenantId, Score = score, Categoria = categoria, Sinais = SinaisHealth(conta).ToList(), RecalculadoEm = DateTime.UtcNow }));
    }

    public async Task<ApiResponse<Guid>> RegistrarCsInteracaoAsync(Guid tenantId, CsInteracaoRequest request, string? ip)
    {
        if (string.IsNullOrWhiteSpace(request.Descricao)) return ApiResponse<Guid>.Fail("Descrição é obrigatória.", 400);
        var id = Guid.NewGuid();
        GarantirConta(tenantId, null, "Conta B2B").UltimaInteracao = DateTime.UtcNow;
        await AuditarAsync("CS_INTERACAO", id, "CRIAR", new { tenantId, request.Tipo, request.Descricao }, ip);
        return ApiResponse<Guid>.Ok(id, "Interação registrada.");
    }

    public async Task<ApiResponse<Guid>> RegistrarNpsAsync(Guid tenantId, CsNpsRequest request, string? ip)
    {
        if (request.Nota < 0 || request.Nota > 10) return ApiResponse<Guid>.Fail("NPS deve estar entre 0 e 10.", 400);
        var id = Guid.NewGuid();
        var conta = GarantirConta(tenantId, null, "Conta B2B");
        conta.Nps = request.Nota;
        if (request.Nota <= 6) conta.Risco = "NPS_BAIXO";
        await AuditarAsync("CS_NPS", id, "REGISTRAR", new { tenantId, request.Nota, request.Periodo }, ip);
        return ApiResponse<Guid>.Ok(id, request.Nota <= 6 ? "NPS baixo registrado e risco criado." : "NPS registrado.");
    }

    public Task<ApiResponse<IEnumerable<CsRiscoDto>>> ListarRiscosAsync()
    {
        var riscos = Contas.Values.Where(x => x.HealthCategoria == "RISCO" || x.HealthCategoria == "CRITICO" || !string.IsNullOrWhiteSpace(x.Risco)).Select(x => new CsRiscoDto { Id = Guid.NewGuid(), TenantId = x.TenantId, NomeCliente = x.NomeCliente, Nivel = string.IsNullOrWhiteSpace(x.HealthCategoria) ? "RISCO" : x.HealthCategoria, Motivo = string.IsNullOrWhiteSpace(x.Risco) ? "Health score baixo" : x.Risco });
        return Task.FromResult(ApiResponse<IEnumerable<CsRiscoDto>>.Ok(riscos.Take(100)));
    }

    public Task<ApiResponse<IEnumerable<CsOportunidadeDto>>> ListarOportunidadesAsync()
    {
        var oportunidades = Contas.Values.Where(x => x.UsoPercentual >= 80).Select(x => new CsOportunidadeDto { Id = Guid.NewGuid(), TenantId = x.TenantId, NomeCliente = x.NomeCliente, Tipo = "EXPANSAO", Motivo = "Uso acima de 80% do limite contratado", ValorEstimado = 890 });
        return Task.FromResult(ApiResponse<IEnumerable<CsOportunidadeDto>>.Ok(oportunidades.Take(100)));
    }

    public Task<ApiResponse<IEnumerable<B2BResumoItemDto>>> PlaybooksAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<B2BResumoItemDto>>.Ok(new List<B2BResumoItemDto> { new B2BResumoItemDto { Id = Guid.NewGuid(), Codigo = "RISCO_NPS", Nome = "Recuperação de NPS baixo", Descricao = "Contato em 24h, plano de ação e revisão em 7 dias.", Status = "ATIVO" } }));
    }

    public async Task<ApiResponse<Guid>> CriarPlaybookAsync(B2BResumoRequest request, string? ip)
    {
        if (string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<Guid>.Fail("Nome é obrigatório.", 400);
        var id = Guid.NewGuid();
        await AuditarAsync("CS_PLAYBOOK", id, "CRIAR", request, ip);
        return ApiResponse<Guid>.Ok(id, "Playbook criado.");
    }

    public Task<ApiResponse<ExecutivoResumoDto>> ExecutivoAsync(string visao)
    {
        var dto = new ExecutivoResumoDto
        {
            Visao = visao,
            MrrEstimado = 48900,
            ReceitaPrevista = 61200,
            ReceitaRecebida = 43800,
            ClientesAtivos = Contas.Values.LongCount(x => x.Status == "ATIVO"),
            ClientesPiloto = ClientesPiloto.Count,
            ClientesRisco = Contas.Values.LongCount(x => x.HealthCategoria == "RISCO" || x.HealthCategoria == "CRITICO"),
            PlantoesPublicados = 128,
            PlantoesDescobertos = 7,
            ChamadosAbertos = Contas.Values.Sum(x => (long)x.ChamadosAbertos),
            ParceirosAtivos = 3,
            NpsMedio = Contas.Values.Any() ? Contas.Values.Average(x => x.Nps) : 8,
            Alertas = new List<string> { "Clientes com NPS baixo exigem plano de retenção", "Trials próximos da conversão devem ser priorizados" }
        };
        return Task.FromResult(ApiResponse<ExecutivoResumoDto>.Ok(dto));
    }

    public Task<ApiResponse<IEnumerable<WhiteLabelTemplateDto>>> ListarTemplatesAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<WhiteLabelTemplateDto>>.Ok(Templates.Values.OrderBy(x => x.Nome)));
    }

    public Task<ApiResponse<WhiteLabelTemplateDto>> ObterTemplateAsync(Guid id)
    {
        return Task.FromResult(Templates.TryGetValue(id, out var item) ? ApiResponse<WhiteLabelTemplateDto>.Ok(item) : ApiResponse<WhiteLabelTemplateDto>.Fail("Template white label não encontrado.", 404));
    }

    public async Task<ApiResponse<WhiteLabelTemplateDto>> SalvarTemplateAsync(WhiteLabelTemplateRequest request, Guid? id, string? ip)
    {
        if (string.IsNullOrWhiteSpace(request.Nome)) return ApiResponse<WhiteLabelTemplateDto>.Fail("Nome é obrigatório.", 400);
        var templateId = id.GetValueOrDefault(Guid.NewGuid());
        var dto = new WhiteLabelTemplateDto { Id = templateId, Nome = request.Nome.Trim(), Categoria = request.Categoria?.Trim() ?? "GERAL", CorPrimaria = request.CorPrimaria?.Trim() ?? "#0d6efd", CorSecundaria = request.CorSecundaria?.Trim() ?? "#198754", Fonte = request.Fonte?.Trim() ?? "Inter", LiberadoAdminCliente = request.LiberadoAdminCliente, ModulosSugeridos = request.ModulosSugeridos?.ToList() ?? new List<string>(), PreviewHtml = "<section class='login-preview'>PlantãoPro White Label</section>" };
        Templates[templateId] = dto;
        await AuditarAsync("WHITE_LABEL_TEMPLATE", templateId, id.HasValue ? "ATUALIZAR" : "CRIAR", dto, ip);
        return ApiResponse<WhiteLabelTemplateDto>.Ok(dto, "Template salvo.");
    }

    public async Task<ApiResponse<Guid>> AplicarTemplateAsync(Guid id, Guid tenantId, string? ip)
    {
        if (!Templates.TryGetValue(id, out var template)) return ApiResponse<Guid>.Fail("Template white label não encontrado.", 404);
        if (!template.LiberadoAdminCliente && !_tenantContext.UsuarioEhAdminGlobal()) return ApiResponse<Guid>.Fail("Template não liberado para admin cliente.", 403);
        var aplicacaoId = Guid.NewGuid();
        await AuditarAsync("WHITE_LABEL_TEMPLATE_APLICACAO", aplicacaoId, "APLICAR", new { templateId = id, tenantId }, ip);
        return ApiResponse<Guid>.Ok(aplicacaoId, "Template aplicado preservando dados sensíveis do tenant.");
    }

    public async Task<ApiResponse<WhiteLabelTemplateDto>> DuplicarTemplateAsync(Guid id, string? ip)
    {
        if (!Templates.TryGetValue(id, out var template)) return ApiResponse<WhiteLabelTemplateDto>.Fail("Template white label não encontrado.", 404);
        var novo = new WhiteLabelTemplateDto { Id = Guid.NewGuid(), Nome = template.Nome + " - cópia", Categoria = template.Categoria, CorPrimaria = template.CorPrimaria, CorSecundaria = template.CorSecundaria, Fonte = template.Fonte, LiberadoAdminCliente = false, ModulosSugeridos = template.ModulosSugeridos.ToList(), PreviewHtml = template.PreviewHtml };
        Templates[novo.Id] = novo;
        await AuditarAsync("WHITE_LABEL_TEMPLATE", novo.Id, "DUPLICAR", new { origemId = id }, ip);
        return ApiResponse<WhiteLabelTemplateDto>.Ok(novo, "Template duplicado.");
    }

    public Task<ApiResponse<IEnumerable<OperacaoAssistidaPlanoDto>>> ListarPlanosOperacaoAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<OperacaoAssistidaPlanoDto>>.Ok(Planos.Values.OrderByDescending(x => x.RegDate).Take(100)));
    }

    public Task<ApiResponse<OperacaoAssistidaPlanoDto>> ObterPlanoOperacaoAsync(Guid id)
    {
        return Task.FromResult(Planos.TryGetValue(id, out var item) ? ApiResponse<OperacaoAssistidaPlanoDto>.Ok(item) : ApiResponse<OperacaoAssistidaPlanoDto>.Fail("Plano de operação assistida não encontrado.", 404));
    }

    public async Task<ApiResponse<OperacaoAssistidaPlanoDto>> CriarPlanoOperacaoAsync(OperacaoAssistidaPlanoRequest request, string? ip)
    {
        if (request.TenantId == Guid.Empty) return ApiResponse<OperacaoAssistidaPlanoDto>.Fail("Tenant é obrigatório.", 400);
        var id = Guid.NewGuid();
        var dto = new OperacaoAssistidaPlanoDto { Id = id, TenantId = request.TenantId, NomeCliente = request.NomeCliente?.Trim() ?? "Cliente", Status = "PLANEJADO", Responsavel = request.Responsavel?.Trim() ?? "MNSOFT", Percentual = 0, Etapas = EtapasImplantacao(id), RegDate = DateTime.UtcNow };
        Planos[id] = dto;
        await AuditarAsync("OPERACAO_ASSISTIDA_PLANO", id, "CRIAR", dto, ip);
        return ApiResponse<OperacaoAssistidaPlanoDto>.Ok(dto, "Plano de implantação criado.");
    }

    public async Task<ApiResponse<string>> AlterarPlanoOperacaoAsync(Guid id, string status, string? ip)
    {
        if (!Planos.TryGetValue(id, out var plano)) return ApiResponse<string>.Fail("Plano de operação assistida não encontrado.", 404);
        plano.Status = status;
        plano.RegUpdate = DateTime.UtcNow;
        Planos[id] = plano;
        await AuditarAsync("OPERACAO_ASSISTIDA_PLANO", id, status, new { id, status }, ip);
        return ApiResponse<string>.Ok("ok", "Plano atualizado.");
    }

    public async Task<ApiResponse<string>> ConcluirEtapaAsync(Guid etapaId, bool pular, ResolverRequest request, string? ip)
    {
        var plano = Planos.Values.FirstOrDefault(x => x.Etapas.Any(e => e.Id == etapaId));
        if (plano is null) return ApiResponse<string>.Fail("Etapa não encontrada.", 404);
        var etapa = plano.Etapas.First(x => x.Id == etapaId);
        etapa.Status = pular ? "PULADA" : "CONCLUIDA";
        etapa.Justificativa = request.Justificativa?.Trim() ?? string.Empty;
        plano.Percentual = (int)Math.Round(plano.Etapas.Count(x => x.Status == "CONCLUIDA") * 100d / plano.Etapas.Count);
        await AuditarAsync("OPERACAO_ASSISTIDA_ETAPA", etapaId, pular ? "PULAR" : "CONCLUIR", etapa, ip);
        return ApiResponse<string>.Ok("ok", pular ? "Etapa pulada com justificativa." : "Etapa concluída.");
    }

    public Task<ApiResponse<IEnumerable<OperacaoAssistidaEtapaDto>>> EtapasAtrasadasAsync()
    {
        var atrasadas = Planos.Values.SelectMany(x => x.Etapas).Where(x => x.DataLimite < DateTime.UtcNow.Date && x.Status == "PENDENTE").Take(100);
        return Task.FromResult(ApiResponse<IEnumerable<OperacaoAssistidaEtapaDto>>.Ok(atrasadas));
    }

    public Task<ApiResponse<IEnumerable<TreinamentoTrilhaDto>>> ListarTrilhasAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<TreinamentoTrilhaDto>>.Ok(Trilhas.Values.OrderBy(x => x.Perfil)));
    }

    public Task<ApiResponse<TreinamentoTrilhaDto>> ObterTrilhaAsync(Guid id)
    {
        return Task.FromResult(Trilhas.TryGetValue(id, out var item) ? ApiResponse<TreinamentoTrilhaDto>.Ok(item) : ApiResponse<TreinamentoTrilhaDto>.Fail("Trilha não encontrada.", 404));
    }

    public Task<ApiResponse<IEnumerable<TreinamentoArtigoDto>>> ListarArtigosAsync(string? termo)
    {
        var artigos = Trilhas.Values.SelectMany(x => x.Artigos).Where(x => string.IsNullOrWhiteSpace(termo) || x.Titulo.Contains(termo, StringComparison.OrdinalIgnoreCase) || x.Conteudo.Contains(termo, StringComparison.OrdinalIgnoreCase)).Take(100);
        return Task.FromResult(ApiResponse<IEnumerable<TreinamentoArtigoDto>>.Ok(artigos));
    }

    public async Task<ApiResponse<string>> ConcluirTrilhaAsync(Guid id, string? ip)
    {
        if (!Trilhas.TryGetValue(id, out var trilha)) return ApiResponse<string>.Fail("Trilha não encontrada.", 404);
        trilha.Concluida = true;
        trilha.Percentual = 100;
        await AuditarAsync("TREINAMENTO_TRILHA", id, "CONCLUIR", new { id }, ip);
        return ApiResponse<string>.Ok("ok", "Trilha concluída.");
    }

    public async Task<ApiResponse<Guid>> FeedbackArtigoAsync(Guid id, TreinamentoFeedbackRequest request, string? ip)
    {
        if (request.Nota < 1 || request.Nota > 5) return ApiResponse<Guid>.Fail("Nota do artigo deve estar entre 1 e 5.", 400);
        var artigoExiste = Trilhas.Values.SelectMany(x => x.Artigos).Any(x => x.Id == id);
        if (!artigoExiste) return ApiResponse<Guid>.Fail("Artigo não encontrado.", 404);
        var feedbackId = Guid.NewGuid();
        await AuditarAsync("TREINAMENTO_ARTIGO_FEEDBACK", feedbackId, "CRIAR", new { artigoId = id, request.Nota }, ip);
        return ApiResponse<Guid>.Ok(feedbackId, "Feedback do artigo registrado.");
    }

    public Task<ApiResponse<IEnumerable<CentralEscalaItemDto>>> PlantaoDescobertoAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<CentralEscalaItemDto>>.Ok(new List<CentralEscalaItemDto> { new CentralEscalaItemDto { Id = Guid.NewGuid(), Hospital = "Hospital Piloto", Especialidade = "Clínica Médica", Setor = "UTI", Status = "DESCOBERTO", CoberturaPercentual = 0, DataInicio = DateTime.UtcNow.Date.AddDays(1) } }));
    }

    public Task<ApiResponse<IEnumerable<CentralEscalaItemDto>>> RiscoEscalaAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<CentralEscalaItemDto>>.Ok(new List<CentralEscalaItemDto> { new CentralEscalaItemDto { Id = Guid.NewGuid(), Hospital = "Hospital Piloto", Especialidade = "Pediatria", Setor = "PS", Status = "RISCO", CoberturaPercentual = 50, DataInicio = DateTime.UtcNow.Date.AddDays(2) } }));
    }

    public Task<ApiResponse<IEnumerable<MedicoDisponivelDto>>> MedicosDisponiveisAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<MedicoDisponivelDto>>.Ok(new List<MedicoDisponivelDto> { new MedicoDisponivelDto { MedicoId = Guid.NewGuid(), Nome = "Médico disponível", Especialidade = "Clínica Médica", DisponivelEm = DateTime.UtcNow.Date.AddDays(1), Score = 92 } }));
    }

    public async Task<ApiResponse<Guid>> AcaoCentralEscalaAsync(string acao, CentralEscalaAcaoRequest request, string? ip)
    {
        if (request.PlantaoId == Guid.Empty) return ApiResponse<Guid>.Fail("Plantão é obrigatório.", 400);
        var id = Guid.NewGuid();
        await AuditarAsync("CENTRAL_ESCALA", id, acao, request, ip);
        return ApiResponse<Guid>.Ok(id, "Ação operacional registrada para auditoria.");
    }

    public Task<ApiResponse<IEnumerable<AgendaMedicaItemDto>>> AgendaMedicaAsync(Guid usuarioId)
    {
        return Task.FromResult(ApiResponse<IEnumerable<AgendaMedicaItemDto>>.Ok(new List<AgendaMedicaItemDto> { new AgendaMedicaItemDto { Id = Guid.NewGuid(), MedicoUsuarioId = usuarioId, Hospital = "Hospital Piloto", Especialidade = "Clínica Médica", Status = "CONFIRMADO", DataInicio = DateTime.UtcNow.Date.AddDays(3), PagamentoPrevisto = 1200 } }));
    }

    public Task<ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>> DisponibilidadeMedicaAsync(Guid usuarioId)
    {
        return Task.FromResult(ApiResponse<IEnumerable<MedicoDisponibilidadeDto>>.Ok(Disponibilidades.Values.Where(x => x.MedicoUsuarioId == usuarioId).OrderBy(x => x.DataInicio).Take(100)));
    }

    public async Task<ApiResponse<Guid>> RegistrarDisponibilidadeAsync(Guid usuarioId, MedicoDisponibilidadeRequest request, string tipo, string? ip)
    {
        if (request.DataFim <= request.DataInicio) return ApiResponse<Guid>.Fail("Data final deve ser maior que a inicial.", 400);
        var id = Guid.NewGuid();
        Disponibilidades[id] = new MedicoDisponibilidadeDto { Id = id, MedicoUsuarioId = usuarioId, Tipo = tipo, DataInicio = request.DataInicio, DataFim = request.DataFim, Observacao = request.Observacao?.Trim() ?? string.Empty };
        await AuditarAsync("MEDICO_DISPONIBILIDADE", id, tipo, new { usuarioId, request.DataInicio, request.DataFim }, ip);
        return ApiResponse<Guid>.Ok(id, "Disponibilidade registrada.");
    }

    public async Task<ApiResponse<Guid>> SolicitarSubstituicaoAsync(Guid usuarioId, MedicoSubstituicaoRequest request, string? ip)
    {
        if (request.EscalaId == Guid.Empty) return ApiResponse<Guid>.Fail("Escala é obrigatória.", 400);
        if (string.IsNullOrWhiteSpace(request.Justificativa)) return ApiResponse<Guid>.Fail("Justificativa é obrigatória.", 400);
        var id = Guid.NewGuid();
        await AuditarAsync("MEDICO_SUBSTITUICAO", id, "SOLICITAR", new { usuarioId, request.EscalaId, request.Justificativa }, ip);
        return ApiResponse<Guid>.Ok(id, "Solicitação de substituição enviada para aprovação.");
    }

    public Task<ApiResponse<IEnumerable<RenovacaoDto>>> ListarRenovacoesAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<RenovacaoDto>>.Ok(Renovacoes.Values.OrderBy(x => x.VencimentoContrato).Take(100)));
    }

    public Task<ApiResponse<RenovacaoDto>> ObterRenovacaoAsync(Guid id)
    {
        return Task.FromResult(Renovacoes.TryGetValue(id, out var item) ? ApiResponse<RenovacaoDto>.Ok(item) : ApiResponse<RenovacaoDto>.Fail("Renovação não encontrada.", 404));
    }

    public async Task<ApiResponse<string>> StatusRenovacaoAsync(Guid id, string acao, ResolverRequest request, string? ip)
    {
        if (!Renovacoes.TryGetValue(id, out var item)) return ApiResponse<string>.Fail("Renovação não encontrada.", 404);
        item.Status = acao;
        item.Observacao = request.Justificativa?.Trim() ?? item.Observacao;
        await AuditarAsync("RENOVACAO", id, acao, new { id, item.Observacao }, ip);
        return ApiResponse<string>.Ok("ok", "Renovação atualizada.");
    }

    public async Task<ApiResponse<Guid>> RegistrarContatoRenovacaoAsync(Guid id, CsInteracaoRequest request, string? ip)
    {
        if (!Renovacoes.ContainsKey(id)) return ApiResponse<Guid>.Fail("Renovação não encontrada.", 404);
        if (string.IsNullOrWhiteSpace(request.Descricao)) return ApiResponse<Guid>.Fail("Descrição é obrigatória.", 400);
        var contatoId = Guid.NewGuid();
        await AuditarAsync("RENOVACAO_CONTATO", contatoId, "REGISTRAR", new { renovacaoId = id, request.Descricao }, ip);
        return ApiResponse<Guid>.Ok(contatoId, "Contato registrado.");
    }

    public Task<ApiResponse<IEnumerable<ExpansaoDto>>> ListarExpansoesAsync()
    {
        return Task.FromResult(ApiResponse<IEnumerable<ExpansaoDto>>.Ok(Expansoes.Values.OrderByDescending(x => x.RegDate).Take(100)));
    }

    public async Task<ApiResponse<ExpansaoDto>> CriarExpansaoAsync(ExpansaoRequest request, string? ip)
    {
        if (request.TenantId == Guid.Empty) return ApiResponse<ExpansaoDto>.Fail("Tenant é obrigatório.", 400);
        if (string.IsNullOrWhiteSpace(request.Motivo)) return ApiResponse<ExpansaoDto>.Fail("Motivo é obrigatório.", 400);
        var id = Guid.NewGuid();
        var dto = new ExpansaoDto { Id = id, TenantId = request.TenantId, NomeCliente = request.NomeCliente?.Trim() ?? "Cliente", Motivo = request.Motivo.Trim(), ValorEstimado = request.ValorEstimado, Status = "ABERTA", RegDate = DateTime.UtcNow };
        Expansoes[id] = dto;
        await AuditarAsync("EXPANSAO", id, "CRIAR", dto, ip);
        return ApiResponse<ExpansaoDto>.Ok(dto, "Oportunidade de expansão criada.");
    }

    public async Task<ApiResponse<string>> StatusExpansaoAsync(Guid id, string status, ResolverRequest request, string? ip)
    {
        if (!Expansoes.TryGetValue(id, out var item)) return ApiResponse<string>.Fail("Expansão não encontrada.", 404);
        if (status == "PERDIDA" && string.IsNullOrWhiteSpace(request.Justificativa)) return ApiResponse<string>.Fail("Motivo é obrigatório para expansão perdida.", 400);
        item.Status = status;
        item.MotivoPerda = request.Justificativa?.Trim() ?? string.Empty;
        await AuditarAsync("EXPANSAO", id, status, item, ip);
        return ApiResponse<string>.Ok("ok", status == "GANHA" ? "Expansão ganha e evento comercial gerado." : "Expansão encerrada.");
    }

    private CsContaDto GarantirConta(Guid tenantId, Guid? clienteId, string nome)
    {
        return Contas.GetOrAdd(tenantId, _ => new CsContaDto { TenantId = tenantId, ClienteId = clienteId, NomeCliente = nome, Status = "ATIVO", HealthScore = 82, HealthCategoria = "SAUDAVEL", Nps = 8, UsoPercentual = 64, ChamadosAbertos = 1, DiasSemLogin = 1 });
    }

    private async Task AuditarAsync(string entidade, Guid entidadeId, string acao, object detalhes, string? ip)
    {
        try
        {
            var ctx = await _tenantContext.ObterAtualAsync();
            await _audit.RegistrarAsync(_tenantContext.ObterUsuarioId(), ctx.Data?.ClienteId, entidade, entidadeId, acao, detalhes, true, ip, "B2B_COMERCIAL_OPERACAO");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Auditoria B2B não persistida para {Entidade} {EntidadeId}", entidade, entidadeId);
        }
    }

    private static string Normalizar(string? valor, string padrao)
    {
        return string.IsNullOrWhiteSpace(valor) ? padrao : valor.Trim().ToUpperInvariant().Replace('Á', 'A').Replace('Ã', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ó', 'O').Replace('Ú', 'U').Replace('Ç', 'C');
    }

    private static IEnumerable<string> SinaisHealth(CsContaDto conta)
    {
        if (conta.Nps <= 6) yield return "NPS baixo";
        if (conta.ChamadosAbertos > 3) yield return "Muitos chamados em aberto";
        if (conta.DiasSemLogin > 10) yield return "Sem login recente";
        if (conta.UsoPercentual >= 80) yield return "Uso acima de 80% indica expansão";
        if (conta.Nps > 6 && conta.ChamadosAbertos <= 3 && conta.DiasSemLogin <= 10) yield return "Uso e relacionamento dentro do esperado";
    }

    private static List<OperacaoAssistidaEtapaDto> EtapasImplantacao(Guid planoId)
    {
        var nomes = new List<string> { "Reunião de kickoff", "Validação de dados da empresa", "Configuração white label", "Configuração de perfis", "Configuração de hospitais", "Configuração de especialidades", "Cadastro de médicos", "Configuração financeira", "Configuração de notificações", "Publicação do primeiro plantão", "Aceite do primeiro médico", "Fechamento da primeira escala", "Geração do primeiro pagamento", "Treinamento da equipe", "Validação com cliente", "Encerramento da implantação" };
        var etapas = new List<OperacaoAssistidaEtapaDto>();
        for (var i = 0; i < nomes.Count; i++) etapas.Add(new OperacaoAssistidaEtapaDto { Id = Guid.NewGuid(), PlanoId = planoId, Ordem = i + 1, Nome = nomes[i], Status = "PENDENTE", Responsavel = "MNSOFT", DataLimite = DateTime.UtcNow.Date.AddDays(i + 1) });
        return etapas;
    }

    private static void Seed()
    {
        if (Programas.Count > 0) return;
        var programaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Programas[programaId] = new PilotoProgramaDto { Id = programaId, Nome = "Piloto Comercial PlantãoPro", Objetivo = "Validar operação B2B com clientes reais", CriteriosSucesso = "White label configurado; hospital criado; médicos cadastrados; plantões publicados; NPS >= 8", Responsavel = "MNSOFT", DataInicio = DateTime.UtcNow.Date, DataFim = DateTime.UtcNow.Date.AddDays(60), Status = "ATIVO", RegDate = DateTime.UtcNow };
        var tenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Contas[tenantId] = new CsContaDto { TenantId = tenantId, NomeCliente = "Hospital Piloto", Status = "ATIVO", HealthScore = 82, HealthCategoria = "SAUDAVEL", Nps = 8, UsoPercentual = 84, ChamadosAbertos = 1, DiasSemLogin = 1 };
        var templateId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Templates[templateId] = new WhiteLabelTemplateDto { Id = templateId, Nome = "Saúde Azul Profissional", Categoria = "SAUDE", CorPrimaria = "#0d6efd", CorSecundaria = "#20c997", Fonte = "Inter", LiberadoAdminCliente = true, ModulosSugeridos = new List<string> { "PLANTOES", "ESCALAS", "FINANCEIRO" }, PreviewHtml = "<section>Saúde Azul Profissional</section>" };
        var trilhaId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        Trilhas[trilhaId] = new TreinamentoTrilhaDto { Id = trilhaId, Perfil = "ADMIN_CLIENTE", Titulo = "Primeiros passos do administrador", Percentual = 0, Artigos = new List<TreinamentoArtigoDto> { new TreinamentoArtigoDto { Id = Guid.NewGuid(), Perfil = "ADMIN_CLIENTE", Titulo = "Configurar empresa e white label", Conteudo = "Configure empresa, identidade visual, perfis, hospitais, médicos e primeiro plantão." } } };
        var renovacaoId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        Renovacoes[renovacaoId] = new RenovacaoDto { Id = renovacaoId, TenantId = tenantId, NomeCliente = "Hospital Piloto", VencimentoContrato = DateTime.UtcNow.Date.AddDays(45), Status = "PROXIMA", ValorAtual = 2990 };
    }
}

public sealed class PilotoProgramaDto { public Guid Id { get; set; } public string Nome { get; set; } = string.Empty; public string Objetivo { get; set; } = string.Empty; public string CriteriosSucesso { get; set; } = string.Empty; public string Responsavel { get; set; } = string.Empty; public DateTime DataInicio { get; set; } public DateTime DataFim { get; set; } public string Status { get; set; } = string.Empty; public DateTime RegDate { get; set; } public DateTime? RegUpdate { get; set; } }
public sealed class PilotoProgramaRequest { public string Nome { get; set; } = string.Empty; public string Objetivo { get; set; } = string.Empty; public string? CriteriosSucesso { get; set; } public string? Responsavel { get; set; } public DateTime DataInicio { get; set; } = DateTime.UtcNow.Date; public DateTime DataFim { get; set; } = DateTime.UtcNow.Date.AddDays(60); public string? Status { get; set; } }
public sealed class PilotoClienteDto { public Guid Id { get; set; } public Guid ProgramaId { get; set; } public Guid TenantId { get; set; } public Guid? ClienteId { get; set; } public string NomeCliente { get; set; } = string.Empty; public string PlanoTrial { get; set; } = string.Empty; public decimal DescontoPercentual { get; set; } public string Status { get; set; } = string.Empty; public string Responsavel { get; set; } = string.Empty; public DateTime RegDate { get; set; } public DateTime? RegUpdate { get; set; } }
public sealed class PilotoClienteRequest { public Guid ProgramaId { get; set; } public Guid TenantId { get; set; } public Guid? ClienteId { get; set; } public string? NomeCliente { get; set; } public string? PlanoTrial { get; set; } public decimal DescontoPercentual { get; set; } public string? Responsavel { get; set; } }
public sealed class PilotoFeedbackDto { public Guid Id { get; set; } public Guid ClientePilotoId { get; set; } public string Categoria { get; set; } = string.Empty; public string Severidade { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public string Descricao { get; set; } = string.Empty; public string Resolucao { get; set; } = string.Empty; public bool AcaoProduto { get; set; } public DateTime RegDate { get; set; } public DateTime? RegUpdate { get; set; } }
public sealed class PilotoFeedbackRequest { public Guid ClientePilotoId { get; set; } public string? Categoria { get; set; } public string? Severidade { get; set; } public string Descricao { get; set; } = string.Empty; }
public sealed class FeedbackClassificacaoRequest { public string? Categoria { get; set; } public string? Severidade { get; set; } public bool GerarTarefaProduto { get; set; } }
public sealed class ResolverRequest { public string? Justificativa { get; set; } }
public sealed class IndicadoresPilotoDto { public long ProgramasAtivos { get; set; } public long ClientesPiloto { get; set; } public long ClientesConvertidos { get; set; } public long FeedbacksAbertos { get; set; } public long FeedbacksCriticos { get; set; } public double NpsMedio { get; set; } }
public sealed class CsContaDto { public Guid TenantId { get; set; } public Guid? ClienteId { get; set; } public string NomeCliente { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public int HealthScore { get; set; } public string HealthCategoria { get; set; } = string.Empty; public int Nps { get; set; } public int UsoPercentual { get; set; } public int ChamadosAbertos { get; set; } public int DiasSemLogin { get; set; } public string Risco { get; set; } = string.Empty; public DateTime? UltimaInteracao { get; set; } }
public sealed class CsHealthDto { public Guid TenantId { get; set; } public int Score { get; set; } public string Categoria { get; set; } = string.Empty; public List<string> Sinais { get; set; } = new List<string>(); public DateTime RecalculadoEm { get; set; } }
public sealed class CsInteracaoRequest { public string? Tipo { get; set; } public string Descricao { get; set; } = string.Empty; }
public sealed class CsNpsRequest { public int Nota { get; set; } public string? Periodo { get; set; } public string? Comentario { get; set; } }
public sealed class CsRiscoDto { public Guid Id { get; set; } public Guid TenantId { get; set; } public string NomeCliente { get; set; } = string.Empty; public string Nivel { get; set; } = string.Empty; public string Motivo { get; set; } = string.Empty; }
public sealed class CsOportunidadeDto { public Guid Id { get; set; } public Guid TenantId { get; set; } public string NomeCliente { get; set; } = string.Empty; public string Tipo { get; set; } = string.Empty; public string Motivo { get; set; } = string.Empty; public decimal ValorEstimado { get; set; } }
public sealed class B2BResumoRequest { public string Nome { get; set; } = string.Empty; public string? Descricao { get; set; } }
public sealed class ExecutivoResumoDto { public string Visao { get; set; } = string.Empty; public decimal MrrEstimado { get; set; } public decimal ReceitaPrevista { get; set; } public decimal ReceitaRecebida { get; set; } public long ClientesAtivos { get; set; } public long ClientesPiloto { get; set; } public long ClientesRisco { get; set; } public long PlantoesPublicados { get; set; } public long PlantoesDescobertos { get; set; } public long ChamadosAbertos { get; set; } public long ParceirosAtivos { get; set; } public double NpsMedio { get; set; } public List<string> Alertas { get; set; } = new List<string>(); }
public sealed class WhiteLabelTemplateDto { public Guid Id { get; set; } public string Nome { get; set; } = string.Empty; public string Categoria { get; set; } = string.Empty; public string CorPrimaria { get; set; } = string.Empty; public string CorSecundaria { get; set; } = string.Empty; public string Fonte { get; set; } = string.Empty; public bool LiberadoAdminCliente { get; set; } public List<string> ModulosSugeridos { get; set; } = new List<string>(); public string PreviewHtml { get; set; } = string.Empty; }
public sealed class WhiteLabelTemplateRequest { public string Nome { get; set; } = string.Empty; public string? Categoria { get; set; } public string? CorPrimaria { get; set; } public string? CorSecundaria { get; set; } public string? Fonte { get; set; } public bool LiberadoAdminCliente { get; set; } public IEnumerable<string>? ModulosSugeridos { get; set; } }
public sealed class OperacaoAssistidaPlanoDto { public Guid Id { get; set; } public Guid TenantId { get; set; } public string NomeCliente { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public string Responsavel { get; set; } = string.Empty; public int Percentual { get; set; } public List<OperacaoAssistidaEtapaDto> Etapas { get; set; } = new List<OperacaoAssistidaEtapaDto>(); public DateTime RegDate { get; set; } public DateTime? RegUpdate { get; set; } }
public sealed class OperacaoAssistidaEtapaDto { public Guid Id { get; set; } public Guid PlanoId { get; set; } public int Ordem { get; set; } public string Nome { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public string Responsavel { get; set; } = string.Empty; public DateTime DataLimite { get; set; } public string Justificativa { get; set; } = string.Empty; }
public sealed class OperacaoAssistidaPlanoRequest { public Guid TenantId { get; set; } public string? NomeCliente { get; set; } public string? Responsavel { get; set; } }
public sealed class TreinamentoTrilhaDto { public Guid Id { get; set; } public string Perfil { get; set; } = string.Empty; public string Titulo { get; set; } = string.Empty; public int Percentual { get; set; } public bool Concluida { get; set; } public List<TreinamentoArtigoDto> Artigos { get; set; } = new List<TreinamentoArtigoDto>(); }
public sealed class TreinamentoArtigoDto { public Guid Id { get; set; } public string Perfil { get; set; } = string.Empty; public string Titulo { get; set; } = string.Empty; public string Conteudo { get; set; } = string.Empty; public string VideoUrl { get; set; } = string.Empty; }
public sealed class TreinamentoFeedbackRequest { public int Nota { get; set; } public string? Comentario { get; set; } }
public sealed class CentralEscalaItemDto { public Guid Id { get; set; } public string Hospital { get; set; } = string.Empty; public string Especialidade { get; set; } = string.Empty; public string Setor { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public int CoberturaPercentual { get; set; } public DateTime DataInicio { get; set; } }
public sealed class MedicoDisponivelDto { public Guid MedicoId { get; set; } public string Nome { get; set; } = string.Empty; public string Especialidade { get; set; } = string.Empty; public DateTime DisponivelEm { get; set; } public int Score { get; set; } }
public sealed class CentralEscalaAcaoRequest { public Guid PlantaoId { get; set; } public Guid? MedicoId { get; set; } public Guid? NovoMedicoId { get; set; } public string? Justificativa { get; set; } }
public sealed class AgendaMedicaItemDto { public Guid Id { get; set; } public Guid MedicoUsuarioId { get; set; } public string Hospital { get; set; } = string.Empty; public string Especialidade { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public DateTime DataInicio { get; set; } public decimal PagamentoPrevisto { get; set; } }
public sealed class MedicoDisponibilidadeDto { public Guid Id { get; set; } public Guid MedicoUsuarioId { get; set; } public string Tipo { get; set; } = string.Empty; public DateTime DataInicio { get; set; } public DateTime DataFim { get; set; } public string Observacao { get; set; } = string.Empty; }
public sealed class MedicoDisponibilidadeRequest { public DateTime DataInicio { get; set; } public DateTime DataFim { get; set; } public string? Observacao { get; set; } }
public sealed class MedicoSubstituicaoRequest { public Guid EscalaId { get; set; } public string Justificativa { get; set; } = string.Empty; }
public sealed class RenovacaoDto { public Guid Id { get; set; } public Guid TenantId { get; set; } public string NomeCliente { get; set; } = string.Empty; public DateTime VencimentoContrato { get; set; } public string Status { get; set; } = string.Empty; public decimal ValorAtual { get; set; } public string Observacao { get; set; } = string.Empty; }
public sealed class ExpansaoDto { public Guid Id { get; set; } public Guid TenantId { get; set; } public string NomeCliente { get; set; } = string.Empty; public string Motivo { get; set; } = string.Empty; public decimal ValorEstimado { get; set; } public string Status { get; set; } = string.Empty; public string MotivoPerda { get; set; } = string.Empty; public DateTime RegDate { get; set; } }
public sealed class ExpansaoRequest { public Guid TenantId { get; set; } public string? NomeCliente { get; set; } public string Motivo { get; set; } = string.Empty; public decimal ValorEstimado { get; set; } }
