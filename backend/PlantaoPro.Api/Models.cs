namespace PlantaoPro.Api.Models
{
public record ApiResponse<T>(bool Success,string Message,T? Data,IEnumerable<string>? Errors,int StatusCode,DateTime Timestamp){
    public static ApiResponse<T> Ok(T data,string message="Sucesso")=>new(true,message,data,null,200,DateTime.UtcNow);
    public static ApiResponse<T> Fail(string message,int status=400,IEnumerable<string>? errors=null)=>new(false,message,default,errors,status,DateTime.UtcNow);
}
public record LoginRequest(string Email,string Senha);
public record LoginResponse(string Token,DateTime ExpiresAt,Guid UsuarioId,string Nome,string[] Roles);
public record MedicoDto(Guid Id,string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId,string RegStatus);
public record CreateMedicoRequest(string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId);
public record HospitalDto(Guid Id,string RazaoSocial,string NomeFantasia,string Cnpj,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel,string RegStatus);
public record CreateHospitalRequest(string RazaoSocial,string NomeFantasia,string Cnpj,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel);
public record UpdateHospitalRequest(string RazaoSocial,string NomeFantasia,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel,string RegStatus);
public record EspecialidadeDto(Guid Id,string Nome,string Descricao,string RegStatus);
public record CreateEspecialidadeRequest(string Nome,string Descricao);
public record PlantaoDto(Guid Id,Guid HospitalId,Guid EspecialidadeId,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,int VagasDisponiveis,string Tipo,string Status,string Observacoes);
public record PlantaoResumoDto(Guid Id,string HospitalNome,string HospitalCidade,string HospitalEstado,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,int VagasDisponiveis,string Tipo,string Status);
public record CreatePlantaoRequest(Guid HospitalId,Guid EspecialidadeId,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,string Tipo,string? Observacoes);
public record UpdatePlantaoRequest(Guid HospitalId,Guid EspecialidadeId,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,string Tipo,string? Observacoes);
public record PlantaoFilterRequest(Guid? HospitalId,Guid? EspecialidadeId,string? Status,DateTime? DataInicio,DateTime? DataFim,string? Cidade,string? Estado,int Page=1,int PageSize=20);
public record PagedResult<T>(IEnumerable<T> Items,int Page,int PageSize,long Total);
public record CancelarPlantaoRequest(string Justificativa);
public record StatusRequest(string Justificativa);
public record PlantaoDetailsDto(Guid Id,Guid HospitalId,Guid EspecialidadeId,string HospitalNome,string HospitalCidade,string HospitalEstado,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,int VagasDisponiveis,string Tipo,string Status,string? Observacoes,string RegStatus,DateTime RegDate);
public record EscalaDto(Guid Id,Guid PlantaoId,Guid MedicoId,string Status,string? Justificativa);
public record EscalaResumoDto(Guid Id,Guid PlantaoId,Guid MedicoId,string MedicoNome,string MedicoCrm,string MedicoUfCrm,string HospitalNome,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,string TipoPlantao,string Status,string? Justificativa,DateTime RegDate);
public record EscalaDetailsDto(Guid Id,Guid PlantaoId,Guid MedicoId,string MedicoNome,string MedicoCrm,string MedicoUfCrm,string MedicoEmail,string MedicoTelefone,string HospitalNome,string HospitalCidade,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,string Status,string? Justificativa);
public record SolicitarPlantaoRequest(Guid MedicoId);
public record RecusarEscalaRequest(string Justificativa);
public record CancelarEscalaRequest(string Justificativa);
public record SubstituirEscalaRequest(Guid NovoMedicoId,string Justificativa);
public record GerarPagamentoRequest(Guid EscalaId,DateOnly? DataPrevista,string? Observacoes);
public record ConfirmarPagamentoRequest(decimal ValorPago,DateOnly DataPagamento,string FormaPagamento,string? Observacoes);
public record PagamentoDetailsDto(Guid Id,Guid EscalaId,Guid MedicoId,Guid PlantaoId,string MedicoNome,string MedicoCrm,string MedicoUfCrm,string MedicoEmail,string MedicoTelefone,string HospitalNome,string EspecialidadeNome,DateTime DataPlantao,decimal ValorPrevisto,decimal? ValorPago,string Status,DateOnly? DataPrevista,DateOnly? DataPagamento,string? FormaPagamento,string? Observacoes);
public record PagamentoResumoDto(Guid Id,Guid EscalaId,Guid MedicoId,Guid PlantaoId,string MedicoNome,string MedicoCrm,string HospitalNome,string EspecialidadeNome,DateTime DataPlantao,decimal ValorPrevisto,decimal? ValorPago,string Status,DateOnly? DataPrevista);
public record DashboardDto(long TotalMedicos,long TotalHospitais,long TotalEspecialidades,long TotalPlantoes,long PlantoesAbertos,long PlantoesConfirmados,long PlantoesRealizados,long PlantoesCancelados,long PagamentosPendentes,long PagamentosPagos,decimal ValorPendente,decimal ValorPagoMes,long NotificacoesNaoLidas);
public record NotificacaoDto(Guid Id,string Titulo,string Mensagem,string Tipo,bool Lida,DateTime RegDate);

public record EscalaFilterRequest(Guid? MedicoId,Guid? PlantaoId,string? Status,DateTime? DataInicio,DateTime? DataFim,Guid? HospitalId,Guid? EspecialidadeId,int Page=1,int PageSize=20);
public record AcceptPlantaoRequest(Guid MedicoId);
public record ConfirmEscalaRequest(string? Justificativa);
public record CancelEscalaRequest(string Justificativa);
public record ReplaceEscalaRequest(Guid NovoMedicoId,string Justificativa);
public record CompleteEscalaRequest(string? Justificativa);
public record PagamentoFilterRequest(Guid? MedicoId,Guid? HospitalId,string? Status,DateTime? DataInicio,DateTime? DataFim,Guid? EspecialidadeId,int Page=1,int PageSize=20);
public record CancelarPagamentoRequest(string Justificativa);
public record NotificationFilterRequest(string? Tipo,bool? Lida,DateTime? DataInicio,DateTime? DataFim,int Page=1,int PageSize=20);

public record MedicoAreaResumoDto(string MedicoNome,string Crm,string UfCrm,int PlantoesDisponiveis,int SolicitacoesPendentes,int EscalasConfirmadas,int PlantoesRealizados,int PagamentosPendentes,decimal ValorPendente,int NotificacoesNaoLidas);
public record MedicoPlantaoDisponivelDto(Guid PlantaoId,string HospitalNome,string HospitalCidade,string HospitalEstado,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,int VagasDisponiveis,string Tipo,string Status,bool JaSolicitado,bool TemConflitoHorario);
public record MedicoEscalaDto(Guid EscalaId,Guid PlantaoId,string HospitalNome,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,string Status,string? Justificativa);
public record MedicoPagamentoDto(Guid PagamentoId,string HospitalNome,string EspecialidadeNome,DateTime DataPlantao,decimal ValorPrevisto,decimal? ValorPago,string Status,DateOnly? DataPrevista,DateOnly? DataPagamento,string? FormaPagamento);
public record DashboardChartItem(string Label,decimal Valor);
public record MedicoPlantaoRecomendacaoDto(Guid PlantaoId,string HospitalNome,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,decimal Score,string MotivoRecomendacao);
public record AlertaOperacionalDto(string Tipo,string Titulo,string Descricao,string Severidade);
public record RolePermissionDto(string Role,IEnumerable<string> Permissions);
public record NotificationPreferenceDto(string Tipo,bool InApp,bool Email);
public record UpsertNotificationPreferenceRequest(string Tipo,bool InApp,bool Email);
public record PremiumKpiCardDto(string Chave,string Titulo,decimal Valor,string Cor,string Icone,string Indicador,string Tooltip);
public record PremiumOperacoesResumoDto(
    IEnumerable<PremiumKpiCardDto> Kpis,
    IEnumerable<AlertaOperacionalDto> Alertas,
    IEnumerable<DashboardChartItem> SeriePlantoes,
    IEnumerable<DashboardChartItem> SeriePagamentos);

public record ClienteDto(Guid Id,string RazaoSocial,string NomeFantasia,string Cnpj,string Email,string Telefone,string Cidade,string Estado,Guid? PlanoId,string Status,string RegStatus,DateTime RegDate,DateTime? RegUpdate);
public record CreateClienteRequest(string RazaoSocial,string NomeFantasia,string Cnpj,string Email,string Telefone,string Cidade,string Estado,Guid? PlanoId,string Status);
public record UpdateClienteRequest(string RazaoSocial,string NomeFantasia,string Cnpj,string Email,string Telefone,string Cidade,string Estado,Guid? PlanoId,string Status,string RegStatus);

public record OperacaoPlantaoCriticoDto(Guid Id,string HospitalNome,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,int VagasDisponiveis,string Status);
public record OperacaoEscalaPendenteDto(Guid Id,string MedicoNome,string HospitalNome,DateTime DataInicio,DateTime DataFim,string Status);
public record OperacaoPagamentoPendenteDto(Guid Id,string MedicoNome,string HospitalNome,decimal ValorPrevisto,string Status,DateOnly? DataPrevista);
public record OperacaoResumoDto(long TotalPlantoesHoje,long TotalPlantoesAbertos,long TotalEscalasSolicitadas,long TotalEscalasConfirmadasHoje,long TotalConflitos,long TotalPagamentosPendentes,decimal ValorPendente, IEnumerable<OperacaoPlantaoCriticoDto>? PlantoesCriticos = null, IEnumerable<OperacaoEscalaPendenteDto>? EscalasPendentes = null, IEnumerable<OperacaoPagamentoPendenteDto>? PagamentosPendentes = null);
public record DashboardOverviewDto(DashboardDto Indicadores,IEnumerable<PlantaoResumoDto> ProximosPlantoes,IEnumerable<PagamentoResumoDto> UltimosPagamentos,IEnumerable<NotificacaoDto> UltimasNotificacoes,IEnumerable<DashboardChartItem> PlantoesPorMes,IEnumerable<DashboardChartItem> PagamentosPorMes,IEnumerable<DashboardChartItem> PlantoesPorEspecialidade,IEnumerable<DashboardChartItem> PlantoesPorHospital);

// ============================================================================
// ONBOARDING SAAS
// ============================================================================
public record PlanoDto(Guid Id,string Nome,string Descricao,decimal ValorMensal,int LimiteMedicos,int LimiteHospitais,int LimitePlantoesMes,bool PermiteRelatorios,bool PermiteApi,string Status);

public record AssinaturaDto(Guid Id,Guid ClienteId,Guid PlanoId,DateTime DataInicio,DateTime DataFim,string Status,decimal ValorContratado,int DiaVencimento,string? Observacoes);

public record UnidadeDto(Guid Id,Guid ClienteId,string Nome,string Tipo,string Cidade,string Estado,string? Responsavel,string Status);
public record CreateUnidadeRequest(Guid ClienteId,string Nome,string Tipo,string Cidade,string Estado,string? Responsavel);
public record UpdateUnidadeRequest(string Nome,string Tipo,string Cidade,string Estado,string? Responsavel);

// Onboarding - Etapa 1: Dados do Cliente
public record OnboardingEtapa1Request(string RazaoSocial,string NomeFantasia,string Cnpj,string Email,string Telefone,string Cidade,string Estado);

// Onboarding - Etapa 2: Selecionar Plano
public record OnboardingEtapa2Request(Guid PlanoId,string Status);

// Onboarding - Etapa 3: Unidade Inicial
public record OnboardingEtapa3Request(string UnidadeNome,string UnidadeTipo,string UnidadeCidade,string UnidadeEstado,string? UnidadeResponsavel);

// Onboarding - Etapa 4: Usuário Administrador
public record OnboardingEtapa4Request(string UsuarioNome,string UsuarioEmail,string UsuarioTelefone,string UsuarioSenha);

// Onboarding - Request completo (usar em wizard frontend)
public record CreateClienteOnboardingRequest(
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string Email,
    string Telefone,
    string Cidade,
    string Estado,
    Guid PlanoId,
    string Status,
    string UnidadeNome,
    string UnidadeTipo,
    string UnidadeCidade,
    string UnidadeEstado,
    string? UnidadeResponsavel,
    string UsuarioNome,
    string UsuarioEmail,
    string UsuarioTelefone,
    string UsuarioSenha
);

// Onboarding - Response com resumo
public record OnboardingResumoDto(
    Guid ClienteId,
    string ClienteNome,
    Guid PlanoId,
    string PlanoNome,
    Guid UnidadeId,
    string UnidadeNome,
    Guid UsuarioId,
    string UsuarioNome,
    string UsuarioEmail,
    Guid AssinaturaId,
    string AssinaturaStatus,
    DateTime DataCriacaoAssinatura
);
}
