using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Web.Models;

public sealed class ConversaListViewModel
{
    public IEnumerable<ConversaResumoDto> Conversas { get; set; } = Array.Empty<ConversaResumoDto>();
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Search { get; set; }
    public string? Tipo { get; set; }
    public string? Status { get; set; }
    public bool HasItems => (Conversas ?? Array.Empty<ConversaResumoDto>()).Any();
}

public sealed class ConversaResumoDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public Guid? EntidadeId { get; set; }
    public string UltimaMensagem { get; set; } = string.Empty;
    public DateTime? UltimaMensagemEm { get; set; }
    public int NaoLidas { get; set; }
    public string CriadoPor { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class ConversaDetalhesViewModel
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Entidade { get; set; } = string.Empty;
    public Guid? EntidadeId { get; set; }
    public IEnumerable<MensagemConversaDto> Mensagens { get; set; } = Array.Empty<MensagemConversaDto>();
    public IEnumerable<ParticipanteConversaDto> Participantes { get; set; } = Array.Empty<ParticipanteConversaDto>();
    public NovaMensagemViewModel NovaMensagem { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public sealed class MensagemConversaDto
{
    public Guid Id { get; set; }
    public Guid ConversaId { get; set; }
    public Guid RemetenteUsuarioId { get; set; }
    public string RemetenteNome { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? AnexoUrl { get; set; }
    public bool Lida { get; set; }
    public DateTime RegDate { get; set; }
    public bool MinhaMensagem { get; set; }
}

public sealed class ParticipanteConversaDto
{
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public bool Online { get; set; }
    public DateTime? UltimaLeitura { get; set; }
}

public sealed class NovaMensagemViewModel
{
    [Required]
    public Guid ConversaId { get; set; }

    [Required]
    [StringLength(4000)]
    public string Mensagem { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Tipo { get; set; }

    [StringLength(2000)]
    public string? AnexoUrl { get; set; }
}

public sealed class NovaConversaViewModel
{
    [Required]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string Tipo { get; set; } = "SUPORTE";

    [StringLength(80)]
    public string Entidade { get; set; } = string.Empty;

    public Guid? EntidadeId { get; set; }

    [Required]
    [StringLength(4000)]
    public string MensagemInicial { get; set; } = string.Empty;

    public IEnumerable<UsuarioConversaOpcaoDto> UsuariosDisponiveis { get; set; } = Array.Empty<UsuarioConversaOpcaoDto>();
    public List<Guid> ParticipantesSelecionados { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public sealed class UsuarioConversaOpcaoDto
{
    public Guid UsuarioId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public bool Selecionado { get; set; }
}
