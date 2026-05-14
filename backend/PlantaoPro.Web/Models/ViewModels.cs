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
    public record DashboardDto(int TotalMedicos, int TotalHospitais, int TotalEspecialidades, int TotalPlantoes, int PlantoesAbertos, int PlantoesConfirmados, int PlantoesRealizados, int PlantoesCancelados, int PagamentosPendentes, int PagamentosPagos, decimal ValorPendente, decimal ValorPagoMes, int NotificacoesNaoLidas);
    public record DashboardChartItem(string Label, decimal Valor);
    public record PlantaoDto(Guid Id, Guid HospitalId, Guid EspecialidadeId, DateTime DataInicio, DateTime DataFim, decimal Valor, int Vagas, int VagasDisponiveis, string Tipo, string Status, string Observacoes);
    public record PagamentoDto(Guid Id, Guid EscalaId, Guid MedicoId, Guid PlantaoId, decimal ValorPrevisto, decimal? ValorPago, string Status, DateOnly? DataPrevista, DateOnly? DataPagamento, string? FormaPagamento, string? Observacoes);
    public record NotificacaoDto(Guid Id, string Titulo, string Mensagem, string Tipo, bool Lida, DateTime RegDate);
    public record DashboardOverviewDto(DashboardDto Indicadores, IEnumerable<PlantaoDto> ProximosPlantoes, IEnumerable<PagamentoDto> UltimosPagamentos, IEnumerable<NotificacaoDto> UltimasNotificacoes, IEnumerable<DashboardChartItem> PlantoesPorMes, IEnumerable<DashboardChartItem> PagamentosPorMes, IEnumerable<DashboardChartItem> PlantoesPorEspecialidade, IEnumerable<DashboardChartItem> PlantoesPorHospital);

    public record MedicoDto(Guid Id,string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId,char RegStatus);
    public record HospitalDto(Guid Id,string RazaoSocial,string NomeFantasia,string Cnpj,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel,char RegStatus);
    public record EspecialidadeDto(Guid Id,string Nome,string Descricao,char RegStatus);
    public record PagedResult<T>(IEnumerable<T> Items,int Page,int PageSize,long Total);
    public record EscalaDto(Guid Id,Guid PlantaoId,Guid MedicoId,string Status,string? Justificativa);
    public record ListPageViewModel<T>(IEnumerable<T> Items, string? ErrorMessage = null, string? InfoMessage = null);
}
}
