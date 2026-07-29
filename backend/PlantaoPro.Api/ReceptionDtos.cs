using System.ComponentModel.DataAnnotations;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class PacienteRequest
{
    [Required, StringLength(200)] public string Nome { get; set; } = string.Empty;
    [StringLength(200)] public string NomeSocial { get; set; } = string.Empty;
    [Required] public DateOnly? DataNascimento { get; set; }
    [StringLength(40)] public string SexoGenero { get; set; } = string.Empty;
    [StringLength(14)] public string Cpf { get; set; } = string.Empty;
    [StringLength(15)] public string Cns { get; set; } = string.Empty;
    [StringLength(80)] public string DocumentoAlternativo { get; set; } = string.Empty;
    [StringLength(30)] public string Telefone { get; set; } = string.Empty;
    [EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [StringLength(500)] public string Endereco { get; set; } = string.Empty;
    [StringLength(200)] public string ResponsavelNome { get; set; } = string.Empty;
    public bool ConsentimentoLgpd { get; set; }

    public Saude360CreateRequest ToClinicalRequest() => new()
    {
        Nome = Nome, NomeSocial = NomeSocial, DataNascimento = DataNascimento,
        SexoGenero = SexoGenero, Cpf = Cpf, Cns = Cns,
        DocumentoAlternativo = DocumentoAlternativo, Telefone = Telefone,
        Email = Email, Endereco = Endereco, ResponsavelNome = ResponsavelNome,
        ConsentimentoLgpd = ConsentimentoLgpd
    };
}

public sealed class AgendamentoRequest
{
    [Required] public Guid? PacienteId { get; set; }
    [Required] public Guid? MedicoId { get; set; }
    [Required] public Guid? UnidadeId { get; set; }
    public Guid? SalaId { get; set; }
    [Required] public DateTime? DataInicio { get; set; }
    [Required] public DateTime? DataFim { get; set; }
    [StringLength(60)] public string Tipo { get; set; } = string.Empty;
    [StringLength(120)] public string Especialidade { get; set; } = string.Empty;
    [StringLength(1000)] public string Observacoes { get; set; } = string.Empty;

    public Saude360CreateRequest ToClinicalRequest() => new()
    {
        PacienteId = PacienteId, MedicoId = MedicoId, UnidadeId = UnidadeId,
        SalaId = SalaId, DataInicio = DataInicio, DataFim = DataFim,
        Tipo = Tipo, Especialidade = Especialidade, Observacoes = Observacoes
    };
}
