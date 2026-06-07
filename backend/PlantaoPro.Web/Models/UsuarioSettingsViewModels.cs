using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Web.Models.ViewModels;

/// <summary>
/// ViewModel utilizado pela tela de edição de perfil do usuário autenticado.
/// </summary>
public class UserSettingsViewModel
{
    [Required(ErrorMessage = "Informe o nome.")]
    [StringLength(140, ErrorMessage = "O nome deve ter no máximo 140 caracteres.")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [StringLength(160, ErrorMessage = "O e-mail deve ter no máximo 160 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Telefone inválido.")]
    [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres.")]
    public string? Telefone { get; set; }

    [Required(ErrorMessage = "Selecione a preferência de notificação.")]
    public string PreferenciasNotificacao { get; set; } = "Email";
}

/// <summary>
/// ViewModel da tela de alteração de senha com validações de confirmação.
/// </summary>
public class AlterarSenhaViewModel
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    [DataType(DataType.Password)]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [MinLength(8, ErrorMessage = "A nova senha deve ter pelo menos 8 caracteres.")]
    [DataType(DataType.Password)]
    public string NovaSenha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
    [DataType(DataType.Password)]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

public record UserSettingsDtoWeb(Guid Id, string Nome, string Email, string? Telefone, string PreferenciasNotificacao);

/// <summary>
/// Item exibido na listagem administrativa de usuários.
/// </summary>
public record UserListItemDto(Guid Id, string Username, string Email, string Role, bool Locked);

/// <summary>
/// ViewModel da listagem administrativa de usuários com suporte a paginação.
/// </summary>
public record UserListVMWeb(
    IEnumerable<UserListItemDto> Items,
    string? ErrorMessage = null,
    string? InfoMessage = null,
    long Total = 0,
    int Page = 1,
    int PageSize = 20)
    : ListPageViewModel<UserListItemDto>(
        Items ?? Enumerable.Empty<UserListItemDto>(),
        ErrorMessage,
        InfoMessage,
        Total,
        Page,
        PageSize);
