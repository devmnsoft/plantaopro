using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        public string Senha { get; set; } = string.Empty;
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a nova senha.")]
        [MinLength(8, ErrorMessage = "A senha deve conter ao menos 8 caracteres.")]
        public string NovaSenha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a nova senha.")]
        [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
        public string ConfirmarSenha { get; set; } = string.Empty;
    }

    public record ApiResponse<T>(bool Success, string Message, T? Data, IEnumerable<string>? Errors, int StatusCode, DateTime Timestamp);
    public record LoginRequest(string Email, string Senha);
    public record LoginResponse(string Token, DateTime ExpiresAt, Guid UsuarioId, string Nome, string[] Roles);
    public record ForgotPasswordRequest(string Email);
    public record ResetPasswordRequest(string Email, string Token, string NovaSenha);
    public record DashboardDto(long TotalMedicos, long TotalHospitais, long TotalEspecialidades, long TotalPlantoes, long PlantoesAbertos, long PlantoesConfirmados, long PlantoesRealizados, long PlantoesCancelados, long PagamentosPendentes, long PagamentosPagos, decimal ValorPendente, decimal ValorPagoMes, long NotificacoesNaoLidas);
    
public record MedicoAreaResumoDto(string MedicoNome,string Crm,string UfCrm,int PlantoesDisponiveis,int SolicitacoesPendentes,int EscalasConfirmadas,int PlantoesRealizados,int PagamentosPendentes,decimal ValorPendente,int NotificacoesNaoLidas);
public record MedicoPlantaoDisponivelDto(Guid PlantaoId,string HospitalNome,string HospitalCidade,string HospitalEstado,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,int VagasDisponiveis,string Tipo,string Status,bool JaSolicitado,bool TemConflitoHorario);
public record MedicoEscalaDto(Guid EscalaId,Guid PlantaoId,string HospitalNome,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,string Status,string? Justificativa);
public record MedicoPagamentoDto(Guid PagamentoId,string HospitalNome,string EspecialidadeNome,DateTime DataPlantao,decimal ValorPrevisto,decimal? ValorPago,string Status,DateOnly? DataPrevista,DateOnly? DataPagamento,string? FormaPagamento);
public record DashboardChartItem(string Label, decimal Valor);
    public record PlantaoResumoDto(Guid Id, string HospitalNome, string HospitalCidade, string HospitalEstado, string EspecialidadeNome, DateTime DataInicio, DateTime DataFim, decimal Valor, int Vagas, int VagasDisponiveis, string Tipo, string Status, string? Observacoes);
    public record PagamentoResumoDto(Guid Id, Guid EscalaId, Guid MedicoId, Guid PlantaoId, string MedicoNome, string MedicoCrm, string HospitalNome, string EspecialidadeNome, DateTime DataPlantao, decimal ValorPrevisto, decimal? ValorPago, string Status, DateOnly? DataPrevista, DateOnly? DataPagamento, string? FormaPagamento, string? ChavePix, string? Observacoes);
    public record PagamentoDetailsDto(Guid Id, Guid EscalaId, Guid MedicoId, Guid PlantaoId, string MedicoNome, string MedicoCrm, string MedicoUfCrm, string MedicoEmail, string MedicoTelefone, string HospitalNome, string HospitalCidade, string HospitalEstado, string EspecialidadeNome, DateTime DataInicioPlantao, DateTime DataFimPlantao, decimal ValorPrevisto, decimal? ValorPago, string Status, DateOnly? DataPrevista, DateOnly? DataPagamento, string? FormaPagamento, string? ChavePix, string? Observacoes, DateTime RegDate);
    public record GerarPagamentoRequest(Guid EscalaId, DateOnly? DataPrevista, string? Observacoes);
    public record ConfirmarPagamentoRequest(decimal ValorPago, DateOnly DataPagamento, string FormaPagamento, string? Observacoes);
    public record CancelarPagamentoRequest(string Justificativa);
    public record NotificacaoDto(Guid Id, string Titulo, string Mensagem, string Tipo, bool Lida, DateTime RegDate);

    public record DashboardOverviewDto(
    DashboardDto Indicadores,
    IEnumerable<PlantaoResumoDto> ProximosPlantoes,
    IEnumerable<PagamentoResumoDto> UltimosPagamentos,
    IEnumerable<NotificacaoDto> UltimasNotificacoes,
    IEnumerable<DashboardChartItem> PlantoesPorMes,
    IEnumerable<DashboardChartItem> PagamentosPorMes,
    IEnumerable<DashboardChartItem> PlantoesPorEspecialidade,
    IEnumerable<DashboardChartItem> PlantoesPorHospital
);
    public record MedicoDto(Guid Id,string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId,string RegStatus);
    public record HospitalDto(Guid Id,string RazaoSocial,string NomeFantasia,string Cnpj,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel,string RegStatus);
    public record EspecialidadeDto(Guid Id,string Nome,string Descricao,string RegStatus);
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public long TotalItems { get; set; }
        public int TotalPages { get; set; }
        public long Total
        {
            get => TotalItems;
            set => TotalItems = value;
        }
        public bool HasItems => Items.Any();

        public static PagedResult<T> Empty(int page = 1, int pageSize = 20) => new()
        {
            Page = page <= 0 ? 1 : page,
            PageSize = pageSize <= 0 ? 20 : pageSize,
            TotalItems = 0,
            TotalPages = 0,
            Items = Array.Empty<T>()
        };
    }

    public sealed class AuditoriaDto
    {
        public Guid Id { get; set; }
        public Guid? UsuarioId { get; set; }
        public Guid? ClienteId { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Entidade { get; set; } = string.Empty;
        public Guid? EntidadeId { get; set; }
        public string Registro { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty;
        public bool Sucesso { get; set; }
        public string IpOrigem { get; set; } = string.Empty;
        public string Ip { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public DateTime DataHora { get; set; }
    }

    public sealed class AuditoriaDetalheDto : AuditoriaDto
    {
        public string Detalhes { get; set; } = "{}";
    }

    public record AuditoriaResumoDto(long AcoesHoje, long FalhasHoje, long AcessosNegados, long Exportacoes);
    public record HealthViewModel(string Status,string Ambiente,string Schema,bool BancoConectado,DateTime DataHora,string? Versao,string BaseUrlApi,bool TokenPresente,string UsuarioAutenticado,string SwaggerUrl);
    public record MinhaAgendaViewModel(IEnumerable<PlantaoResumoDto> MeusPlantoes, IEnumerable<PagamentoResumoDto> MeusPagamentos, IEnumerable<NotificacaoDto> MinhasNotificacoes, string? ErrorMessage = null);

    public record EscalaResumoDto(Guid Id,Guid PlantaoId,Guid MedicoId,string MedicoNome,string MedicoCrm,string MedicoUfCrm,string HospitalNome,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,string TipoPlantao,string Status,string? Justificativa,DateTime RegDate);
    public record EscalaDetailsDto(Guid Id,Guid PlantaoId,Guid MedicoId,string MedicoNome,string MedicoCrm,string MedicoUfCrm,string MedicoEmail,string MedicoTelefone,string HospitalNome,string HospitalCidade,string HospitalEstado,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,string TipoPlantao,string Status,string? Justificativa,DateTime RegDate);


    public record PlantaoDetailsDto(Guid Id, Guid HospitalId, Guid EspecialidadeId, string HospitalNome, string HospitalCidade, string HospitalEstado, string EspecialidadeNome, DateTime DataInicio, DateTime DataFim, decimal Valor, int Vagas, int VagasDisponiveis, string Tipo, string Status, string? Observacoes, string RegStatus, DateTime RegDate);
    public record PlantaoFormViewModel
    {
        public Guid? Id { get; set; }
        [Required] public Guid HospitalId { get; set; }
        [Required] public Guid EspecialidadeId { get; set; }
        [Required] public DateTime DataInicio { get; set; }
        [Required] public DateTime DataFim { get; set; }
        [Range(0.01, double.MaxValue, ErrorMessage = "Valor deve ser maior que zero.")] public decimal Valor { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Vagas deve ser maior que zero.")] public int Vagas { get; set; }
        [Required] public string Tipo { get; set; } = "Presencial";
        public string? Observacoes { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
        public IEnumerable<HospitalDto> Hospitais { get; set; } = Enumerable.Empty<HospitalDto>();
        public IEnumerable<EspecialidadeDto> Especialidades { get; set; } = Enumerable.Empty<EspecialidadeDto>();
    }

    public record StatusActionViewModel(Guid Id, [Required] string Justificativa);
    public record AcceptPlantaoWebRequest(Guid MedicoId);
    public record PageHeaderViewModel(
        string Title,
        string Subtitle,
        string? Icon = null,
        string? ButtonText = null,
        string? ButtonAction = null,
        string? ButtonController = null,
        bool ButtonDisabled = false
    );

    public record EmptyStateViewModel(
        string Icon,
        string Title,
        string Description,
        string? ButtonText = null,
        string? ButtonAction = null,
        string? ButtonController = null,
        bool ButtonDisabled = false
    );

    
    
    public record ClienteDto(Guid Id,string RazaoSocial,string NomeFantasia,string Cnpj,string Email,string Telefone,string Cidade,string Estado,Guid? PlanoId,string Status,string RegStatus,DateTime RegDate,DateTime? RegUpdate);
    public record CreateClienteRequest(string RazaoSocial,string NomeFantasia,string Cnpj,string Email,string Telefone,string Cidade,string Estado,Guid? PlanoId,string Status);
    public record UpdateClienteRequest(string RazaoSocial,string NomeFantasia,string Cnpj,string Email,string Telefone,string Cidade,string Estado,Guid? PlanoId,string Status,string RegStatus);

    public record OperacaoPlantaoCriticoDto(Guid Id,string HospitalNome,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,int VagasDisponiveis,string Status);
    public record OperacaoEscalaPendenteDto(Guid Id,string MedicoNome,string HospitalNome,DateTime DataInicio,DateTime DataFim,string Status);
    public record OperacaoPagamentoPendenteDto(Guid Id,string MedicoNome,string HospitalNome,decimal ValorPrevisto,string Status,DateOnly? DataPrevista);
    public record OperacaoResumoDto(long TotalPlantoesHoje,long TotalPlantoesAbertos,long TotalEscalasSolicitadas,long TotalEscalasConfirmadasHoje,long TotalConflitos,long TotalPagamentosPendentes,decimal ValorPendente,long NotificacoesNaoLidas,IEnumerable<OperacaoPlantaoCriticoDto> PlantoesCriticos,IEnumerable<OperacaoEscalaPendenteDto> EscalasPendentes,IEnumerable<OperacaoPagamentoPendenteDto> PagamentosPendentes)
    {
        public static OperacaoResumoDto Empty() => new(0,0,0,0,0,0,0,0,Array.Empty<OperacaoPlantaoCriticoDto>(),Array.Empty<OperacaoEscalaPendenteDto>(),Array.Empty<OperacaoPagamentoPendenteDto>());
    }
public interface IListPageViewModel
    {
        long Total { get; }
        int Page { get; }
        int PageSize { get; }
        int TotalPages { get; }
    }

    public record ListPageViewModel<T>(
        IEnumerable<T> Items,
        string? ErrorMessage = null,
        string? InfoMessage = null,
        long Total = 0,
        int Page = 1,
        int PageSize = 20) : IListPageViewModel
    {
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
    }

    public record DetailsPageViewModel<T>(
    T? Data,
    string? ErrorMessage = null,
    bool IsPlaceholder = false
);

}

namespace PlantaoPro.Web.Models
{
    public class OnboardingClienteViewModel
    {
        [Required] public string RazaoSocial { get; set; } = string.Empty;
        [Required] public string NomeFantasia { get; set; } = string.Empty;
        [Required] public string Cnpj { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Telefone { get; set; } = string.Empty;
        [Required] public string Cidade { get; set; } = string.Empty;
        [Required] public string Estado { get; set; } = string.Empty;
        [Required] public Guid PlanoId { get; set; }
        [Required] public string Status { get; set; } = "ATIVO";
        [Required] public string UnidadeNome { get; set; } = string.Empty;
        [Required] public string UnidadeTipo { get; set; } = "Matriz";
        [Required] public string UnidadeCidade { get; set; } = string.Empty;
        [Required] public string UnidadeEstado { get; set; } = string.Empty;
        public string? UnidadeResponsavel { get; set; }
        [Required] public string UsuarioNome { get; set; } = string.Empty;
        [Required, EmailAddress] public string UsuarioEmail { get; set; } = string.Empty;
        [Required] public string UsuarioTelefone { get; set; } = string.Empty;
        [Required] public string UsuarioSenha { get; set; } = string.Empty;

        public CreateClienteOnboardingRequest ToRequest() => new(
            RazaoSocial, NomeFantasia, Cnpj, Email, Telefone, Cidade, Estado,
            PlanoId, Status, UnidadeNome, UnidadeTipo, UnidadeCidade, UnidadeEstado,
            UnidadeResponsavel, UsuarioNome, UsuarioEmail, UsuarioTelefone, UsuarioSenha);
    }

    public record CreateClienteOnboardingRequest(
        string RazaoSocial, string NomeFantasia, string Cnpj, string Email, string Telefone, string Cidade, string Estado,
        Guid PlanoId, string Status, string UnidadeNome, string UnidadeTipo, string UnidadeCidade, string UnidadeEstado,
        string? UnidadeResponsavel, string UsuarioNome, string UsuarioEmail, string UsuarioTelefone, string UsuarioSenha);

    public record OnboardingResumoDto(
        Guid ClienteId = default,
        string ClienteNome = "",
        Guid PlanoId = default,
        string PlanoNome = "",
        Guid UnidadeId = default,
        string UnidadeNome = "",
        Guid UsuarioId = default,
        string UsuarioNome = "",
        string UsuarioEmail = "",
        Guid AssinaturaId = default,
        string AssinaturaStatus = "",
        DateTime DataCriacaoAssinatura = default);
}
