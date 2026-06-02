namespace PlantaoPro.Api.Models
{
public record ApiResponse<T>(bool Success,string Message,T? Data,IEnumerable<string>? Errors,int StatusCode,DateTime Timestamp){
    public static ApiResponse<T> Ok(T data,string message="Sucesso")=>new(true,message,data,null,200,DateTime.UtcNow);
    public static ApiResponse<T> Fail(string message,int status=400,IEnumerable<string>? errors=null)=>new(false,message,default,errors,status,DateTime.UtcNow);
}
public record LoginRequest(string Email,string Senha);
public record LoginResponse(string Token,DateTime ExpiresAt,Guid UsuarioId,string Nome,string[] Roles, Guid? ClienteId = null);
public record MedicoDto(Guid Id,string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId,string RegStatus);
public record CreateMedicoRequest(string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId);
public record HospitalDto(Guid Id,string RazaoSocial,string NomeFantasia,string Cnpj,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel,string RegStatus);
public record CreateHospitalRequest(string RazaoSocial,string NomeFantasia,string Cnpj,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel);
public record UpdateHospitalRequest(string RazaoSocial,string NomeFantasia,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel,string RegStatus);
public record EspecialidadeDto(Guid Id,string Nome,string Descricao,string RegStatus);
public record CreateEspecialidadeRequest(string Nome,string Descricao);
public record PlantaoDto(Guid Id,Guid HospitalId,Guid EspecialidadeId,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,int VagasDisponiveis,string Tipo,string Status,string Observacoes);
public sealed class PlantaoResumoDto
{
    public Guid Id { get; set; }
    public string HospitalNome { get; set; } = string.Empty;
    public string HospitalCidade { get; set; } = string.Empty;
    public string HospitalEstado { get; set; } = string.Empty;
    public string EspecialidadeNome { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public decimal Valor { get; set; }
    public int Vagas { get; set; }
    public int VagasDisponiveis { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
}
public record CreatePlantaoRequest(Guid HospitalId,Guid EspecialidadeId,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,string Tipo,string? Observacoes);
public record UpdatePlantaoRequest(Guid HospitalId,Guid EspecialidadeId,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,string Tipo,string? Observacoes);
public record PlantaoFilterRequest(Guid? HospitalId,Guid? EspecialidadeId,string? Status,DateTime? DataInicio,DateTime? DataFim,string? Cidade,string? Estado,int Page=1,int PageSize=20);
public record PagedResult<T>(IEnumerable<T> Items,int Page,int PageSize,long Total);
public record CancelarPlantaoRequest(string Justificativa);
public record StatusRequest(string Justificativa);
public record PlantaoDetailsDto(Guid Id,Guid HospitalId,Guid EspecialidadeId,string HospitalNome,string HospitalCidade,string HospitalEstado,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,int VagasDisponiveis,string Tipo,string Status,string? Observacoes,string RegStatus,DateTime RegDate);
public record PlantaoHistoricoDto(Guid Id,Guid PlantaoId,string StatusAnterior,string StatusNovo,string Justificativa,Guid? UsuarioId,DateTime RegDate);
public record PlantaoConviteDto(Guid Id,Guid PlantaoId,Guid MedicoId,string MedicoNome,string Status,string Mensagem,DateTime DataEnvio,DateTime? DataResposta,string MotivoRecusa);
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
public sealed class PagamentoResumoDto
{
    public Guid Id { get; set; }
    public Guid EscalaId { get; set; }
    public Guid MedicoId { get; set; }
    public Guid PlantaoId { get; set; }
    public string MedicoNome { get; set; } = string.Empty;
    public string MedicoCrm { get; set; } = string.Empty;
    public string HospitalNome { get; set; } = string.Empty;
    public string EspecialidadeNome { get; set; } = string.Empty;
    public DateTime DataPlantao { get; set; }
    public decimal ValorPrevisto { get; set; }
    public decimal ValorPago { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? DataPrevista { get; set; }
    public DateOnly? DataPagamento { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;
    public string ChavePix { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
}
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


public sealed class ConflitoHorarioResultadoDto
{
    public bool PossuiConflito { get; set; }
    public long TotalConflitos { get; set; }
    public string Grau { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public IEnumerable<PlantaoPro.Api.ConflitoHorarioDetalheDto> Conflitos { get; set; } = Array.Empty<PlantaoPro.Api.ConflitoHorarioDetalheDto>();
}

public record VerificarConflitoRequest(Guid MedicoId,DateTime DataInicio,DateTime DataFim,Guid? EscalaIgnoradaId);

public sealed class MedicoRecomendadoDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Crm { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
    public decimal ScoreRecomendacao { get; set; }
    public IEnumerable<string> Motivos { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Alertas { get; set; } = Array.Empty<string>();
    public bool PossuiConflito { get; set; }
    public bool Disponivel { get; set; }
    public bool JaConvidado { get; set; }
    public bool JaEscalado { get; set; }
}

public record ConvidarRecomendadosRequest(IEnumerable<Guid> MedicoIds,string? Mensagem);

public sealed class CentralEscalaPendenciaDto
{
    public string Tipo { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public Guid? ReferenciaId { get; set; }
    public DateTime? DataReferencia { get; set; }
}

public sealed class CentralEscalaAcaoRecomendadaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Severidade { get; set; } = string.Empty;
    public string UrlSugerida { get; set; } = string.Empty;
    public Guid? ReferenciaId { get; set; }
}

public record OperacaoPlantaoCriticoDto(Guid Id,string HospitalNome,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,int VagasDisponiveis,string Status);
public record OperacaoEscalaPendenteDto(Guid Id,string MedicoNome,string HospitalNome,DateTime DataInicio,DateTime DataFim,string Status);
public record OperacaoPagamentoPendenteDto(Guid Id,string MedicoNome,string HospitalNome,decimal ValorPrevisto,string Status,DateOnly? DataPrevista);
public sealed class OperacaoResumoDto
{
    public long TotalPlantoesHoje { get; set; }
    public long TotalPlantoesAbertos { get; set; }
    public long TotalEscalasSolicitadas { get; set; }
    public long TotalEscalasConfirmadasHoje { get; set; }
    public long TotalConflitos { get; set; }
    public long TotalPagamentosPendentes { get; set; }
    public decimal ValorPendente { get; set; }
    public long NotificacoesNaoLidas { get; set; }
    public OperacaoPlantaoCriticoDto[] PlantoesCriticos { get; set; } = Array.Empty<OperacaoPlantaoCriticoDto>();
    public OperacaoEscalaPendenteDto[] EscalasPendentes { get; set; } = Array.Empty<OperacaoEscalaPendenteDto>();
    public OperacaoPagamentoPendenteDto[] PagamentosPendentes { get; set; } = Array.Empty<OperacaoPagamentoPendenteDto>();
}
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

public sealed class ConversaListDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime UltimaAtualizacao { get; set; }
    public long NaoLidas { get; set; }
}
public record CriarConversaRequest(string Titulo,string? Tipo,string? Entidade,Guid? EntidadeId,Guid[] Participantes);
public record EnviarMensagemRequest(string Mensagem);

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
public sealed class BiResumoExecutivoDto
{
    public long TotalClientesAtivos { get; set; }
    public decimal ReceitaMensalEstimada { get; set; }
    public decimal ReceitaVencida { get; set; }
    public long PlantoesPublicadosMes { get; set; }
    public decimal PercentualCobertura { get; set; }
    public long EscalasConfirmadas { get; set; }
    public long EscalasCanceladas { get; set; }
    public long PagamentosPendentes { get; set; }
    public long PagamentosConfirmados { get; set; }
    public decimal TempoMedioPreenchimentoHoras { get; set; }
}

}
