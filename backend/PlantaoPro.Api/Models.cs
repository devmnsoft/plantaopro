namespace PlantaoPro.Api.Models
{
public record ApiResponse<T>(bool Success,string Message,T? Data,IEnumerable<string>? Errors,int StatusCode,DateTime Timestamp){
    public static ApiResponse<T> Ok(T data,string message="Sucesso")=>new(true,message,data,null,200,DateTime.UtcNow);
    public static ApiResponse<T> Fail(string message,int status=400,IEnumerable<string>? errors=null)=>new(false,message,default,errors,status,DateTime.UtcNow);
}
public record HealthDto(string Application,string Status,string Environment,DateTime TimestampUtc,string Version);
public record LoginRequest(string Email,string Senha);
public record LoginResponse(string Token,DateTime ExpiresAt,Guid UsuarioId,string Nome,string[] Roles, Guid? ClienteId = null);
public record MedicoDto(Guid Id,string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId,string RegStatus);
public record CreateMedicoRequest(string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId);
public record UpdateMedicoRequest(string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId,string RegStatus);
public record HospitalDto(Guid Id,string RazaoSocial,string NomeFantasia,string Cnpj,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel,string RegStatus);
public record CreateHospitalRequest(string RazaoSocial,string NomeFantasia,string Cnpj,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel);
public record UpdateHospitalRequest(string RazaoSocial,string NomeFantasia,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel,string RegStatus);
public record EspecialidadeDto(Guid Id,string Nome,string Descricao,string RegStatus);
public record CreateEspecialidadeRequest(string Nome,string Descricao);
public record UpdateEspecialidadeRequest(string Nome,string Descricao,string RegStatus);
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

public sealed class PlanoComercialDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public decimal ValorMensal { get; set; }
    public int LimiteMedicos { get; set; }
    public int LimiteHospitais { get; set; }
    public int LimitePlantoesMes { get; set; }
    public int LimiteUsuarios { get; set; }
    public int LimiteConvitesMes { get; set; }
    public bool PermiteMobile { get; set; }
    public bool PermiteBi { get; set; }
    public bool PermiteRelatoriosAvancados { get; set; }
    public bool PermiteIntegracoes { get; set; }
    public bool PermiteOperacaoAssistida { get; set; }
    public bool PermiteSuportePrioritario { get; set; }
    public string Status { get; set; } = string.Empty;
}

public record PlanoComercialRequest(string Nome,string Descricao,decimal ValorMensal,int LimiteMedicos,int LimiteHospitais,int LimitePlantoesMes,bool PermiteMobile,bool PermiteBi,bool PermiteRelatoriosAvancados,bool PermiteIntegracoes,int LimiteUsuarios = 0,int LimiteConvitesMes = 0,bool PermiteOperacaoAssistida = false,bool PermiteSuportePrioritario = false);

public sealed class AssinaturaComercialDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public Guid PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal ValorContratado { get; set; }
    public int DiaVencimento { get; set; }
    public string Observacoes { get; set; } = string.Empty;
}

public record AssinaturaComercialRequest(Guid ClienteId,Guid PlanoId,DateTime DataInicio,DateTime DataFim,decimal ValorContratado,int DiaVencimento,string? Observacoes);
public record JustificativaRequest(string Justificativa);
public record MotivoRequest(string Motivo);


public sealed class PlanoRecursoDto
{
    public Guid Id { get; set; }
    public Guid PlanoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Habilitado { get; set; }
    public int? Limite { get; set; }
}

public record PlanoRecursoRequest(string Codigo, string Nome, string? Descricao, bool Habilitado, int? Limite);
public record AlterarPlanoAssinaturaRequest(Guid PlanoId, string Justificativa);
public record ResolverContestacaoFaturaRequest(string Resposta);

public sealed class RelatorioSaasLinhaDto
{
    public Guid? ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Classificacao { get; set; } = string.Empty;
    public long Quantidade { get; set; }
    public decimal Valor { get; set; }
    public DateOnly? Competencia { get; set; }
}

public sealed class UsoPlanoDto
{
    public Guid ClienteId { get; set; }
    public Guid AssinaturaId { get; set; }
    public Guid PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public string AssinaturaStatus { get; set; } = string.Empty;
    public DateTime DataFim { get; set; }
    public int MedicosUsados { get; set; }
    public int MedicosLimite { get; set; }
    public int HospitaisUsados { get; set; }
    public int HospitaisLimite { get; set; }
    public int PlantoesMesUsados { get; set; }
    public int PlantoesMesLimite { get; set; }
    public int UsuariosUsados { get; set; }
    public int UsuariosLimite { get; set; }
    public int ConvitesMesUsados { get; set; }
    public int ConvitesMesLimite { get; set; }
    public bool PermiteMobile { get; set; }
    public bool PermiteBi { get; set; }
    public bool PermiteRelatoriosAvancados { get; set; }
    public bool PermiteIntegracoes { get; set; }
    public bool PermiteOperacaoAssistida { get; set; }
    public bool PermiteSuportePrioritario { get; set; }
}

public record GerarFaturasSaasRequest(DateOnly Competencia);
public record MarcarFaturaPagaRequest(decimal ValorPago,DateOnly DataPagamento,string FormaPagamento,string? Observacoes);

public sealed class FaturamentoSaasResumoDto
{
    public decimal ReceitaPrevista { get; set; }
    public decimal ReceitaRecebida { get; set; }
    public long FaturasAbertas { get; set; }
    public long FaturasVencidas { get; set; }
    public long FaturasEmContestacao { get; set; }
}

public sealed class FaturaSaasDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public Guid AssinaturaId { get; set; }
    public DateOnly Competencia { get; set; }
    public decimal Valor { get; set; }
    public DateOnly Vencimento { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? ValorPago { get; set; }
    public DateOnly? DataPagamento { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;
    public string MotivoCancelamento { get; set; } = string.Empty;
    public string MotivoContestacao { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}

public sealed class InadimplenciaSaasDto
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public long FaturasVencidas { get; set; }
    public decimal ValorVencido { get; set; }
    public DateOnly VencimentoMaisAntigo { get; set; }
}

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
public record CriarConversaRequest(string Titulo,string? Tipo,string? Entidade,Guid? EntidadeId,Guid[] Participantes,string? MensagemInicial);
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

namespace PlantaoPro.Api.Models
{
public sealed class OperacaoAssistidaClienteDto
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public DateTime? InicioPrevisto { get; set; }
    public DateTime? GoLivePrevisto { get; set; }
    public int Percentual { get; set; }
    public string Risco { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
    public long OcorrenciasAbertas { get; set; }
    public long OcorrenciasCriticas { get; set; }
}

public sealed class OperacaoAssistidaChecklistDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public int Ordem { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Concluido { get; set; }
    public DateTime? ConcluidoEm { get; set; }
    public string ConcluidoPor { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
}

public sealed class OperacaoAssistidaOcorrenciaDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Solucao { get; set; } = string.Empty;
    public DateTime DataAbertura { get; set; }
    public DateTime? DataResolucao { get; set; }
}

public sealed class OperacaoAssistidaTreinamentoDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Tema { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Responsavel { get; set; } = string.Empty;
    public string Participantes { get; set; } = string.Empty;
    public DateTime RealizadoEm { get; set; }
    public string Observacoes { get; set; } = string.Empty;
}

public record OperacaoAssistidaDetalheDto(
    OperacaoAssistidaClienteDto Cliente,
    IEnumerable<OperacaoAssistidaChecklistDto> Checklist,
    IEnumerable<OperacaoAssistidaOcorrenciaDto> Ocorrencias,
    IEnumerable<OperacaoAssistidaTreinamentoDto> Treinamentos);

public record ConcluirChecklistOperacaoRequest(string? Responsavel, string? Observacao);
public record ReabrirChecklistOperacaoRequest(string Justificativa);
public record CriarOcorrenciaOperacaoRequest(string Tipo, string Prioridade, string Descricao, string? Responsavel);
public record ResolverOcorrenciaOperacaoRequest(string Solucao, string? Responsavel);
public record RegistrarTreinamentoOperacaoRequest(string Tema, string? Perfil, string? Responsavel, string? Participantes, DateTime? RealizadoEm, string? Observacoes);

public sealed class AssinaturaAtualDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public Guid PlanoId { get; set; }
    public string PlanoNome { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public DateTime? DataTrialFim { get; set; }
    public decimal ValorContratado { get; set; }
    public int DiaVencimento { get; set; }
    public string Periodicidade { get; set; } = string.Empty;
}

public sealed class ClienteSaudeDto
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Classificacao { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool Inadimplente { get; set; }
    public bool UsoAlto { get; set; }
    public bool Inativo { get; set; }
    public bool ElegivelUpgrade { get; set; }
    public IEnumerable<string> Riscos { get; set; } = Array.Empty<string>();
    public IEnumerable<string> Oportunidades { get; set; } = Array.Empty<string>();
    public IEnumerable<string> AcoesRecomendadas { get; set; } = Array.Empty<string>();
}

public sealed class ClienteAlertaSaasDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Severidade { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public bool Resolvido { get; set; }
    public DateTime RegDate { get; set; }
}

public sealed class SaasResumoExecutivoDto
{
    public long ClientesAtivos { get; set; }
    public long ClientesTrial { get; set; }
    public long ClientesSuspensos { get; set; }
    public long ClientesCancelados { get; set; }
    public long ClientesRisco { get; set; }
    public long ClientesCriticos { get; set; }
    public decimal ReceitaPrevistaMes { get; set; }
    public decimal ReceitaRecebidaMes { get; set; }
    public long FaturasAbertas { get; set; }
    public long FaturasVencidas { get; set; }
    public decimal MrrEstimado { get; set; }
    public decimal ChurnEstimado { get; set; }
    public long ClientesProximosLimite { get; set; }
    public long OportunidadesUpgrade { get; set; }
    public long AlertasAbertos { get; set; }
}

public sealed class SaasRecomendacaoDto
{
    public Guid ClienteId { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
}

}
