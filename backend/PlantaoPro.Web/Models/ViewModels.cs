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
    public record DashboardChartItem(string Label, decimal Valor);
    public record PlantaoResumoDto(Guid Id, string HospitalNome, string HospitalCidade, string HospitalEstado, string EspecialidadeNome, DateTime DataInicio, DateTime DataFim, decimal Valor, int Vagas, int VagasDisponiveis, string Tipo, string Status, string? Observacoes);
    public record PagamentoResumoDto(Guid Id, string MedicoNome, string MedicoCrm, string HospitalNome, string EspecialidadeNome, DateTime DataPlantao, decimal ValorPrevisto, decimal? ValorPago, string Status, DateOnly? DataPrevista, DateOnly? DataPagamento, string? FormaPagamento, string? ChavePix, string? Observacoes);
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
    public record PagedResult<T>(IEnumerable<T> Items,int Page,int PageSize,long Total);

    public record AuditoriaDto(DateTime DataHora, string Usuario, string Acao, string Entidade, string Registro, string Ip, string Descricao);
    public record HealthViewModel(string Status,string Ambiente,string Schema,bool BancoConectado,DateTime DataHora,string? Versao,string BaseUrlApi,bool TokenPresente,string UsuarioAutenticado,string SwaggerUrl);
    public record MinhaAgendaViewModel(IEnumerable<PlantaoResumoDto> MeusPlantoes, IEnumerable<PagamentoResumoDto> MeusPagamentos, IEnumerable<NotificacaoDto> MinhasNotificacoes, string? ErrorMessage = null);

    public record EscalaResumoDto(Guid Id,Guid PlantaoId,Guid MedicoId,string MedicoNome,string MedicoCrm,string MedicoUfCrm,string HospitalNome,string EspecialidadeNome,DateTime DataInicio,DateTime DataFim,decimal Valor,string TipoPlantao,string Status,string? Justificativa,DateTime RegDate);


    public record PlantaoDetailsDto(Guid Id, Guid HospitalId, Guid EspecialidadeId, string HospitalNome, string HospitalCidade, string HospitalEstado, string EspecialidadeNome, DateTime DataInicio, DateTime DataFim, decimal Valor, int Vagas, int VagasDisponiveis, string Tipo, string Status, string? Observacoes);
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
        public string Observacoes { get; set; } = string.Empty;
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
