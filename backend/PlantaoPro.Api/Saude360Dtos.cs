using System.Text.Json.Serialization;

namespace PlantaoPro.Api.Models;

public sealed class Saude360RegistroDto
{
    public Guid Id { get; set; }
    public Guid? ClienteId { get; set; }
    public Guid? PacienteId { get; set; }
    public Guid? MedicoId { get; set; }
    public Guid? AgendamentoId { get; set; }
    public Guid? ConsultaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
    public Dictionary<string, object?> Dados { get; set; } = new Dictionary<string, object?>();
}

public sealed class Saude360CreateRequest
{
    public Guid? PacienteId { get; set; }
    public Guid? MedicoId { get; set; }
    public Guid? AgendamentoId { get; set; }
    public Guid? ConsultaId { get; set; }
    public Guid? TriagemId { get; set; }
    public Guid? PainelId { get; set; }
    public Guid? ConvenioId { get; set; }
    public Guid? PlanoSaudeId { get; set; }
    public Guid? ProcedimentoId { get; set; }
    public Guid? UnidadeId { get; set; }
    public Guid? SetorId { get; set; }
    public Guid? SalaId { get; set; }
    public Guid? GuicheId { get; set; }
    public Guid? ModeloId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string PacienteNome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string CodigoCid { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Cns { get; set; } = string.Empty;
    public string DocumentoAlternativo { get; set; } = string.Empty;
    public string NomeSocial { get; set; } = string.Empty;
    public string SexoGenero { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string ResponsavelNome { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
    public string ClassificacaoRisco { get; set; } = string.Empty;
    public string QueixaPrincipal { get; set; } = string.Empty;
    public string Alergias { get; set; } = string.Empty;
    public string MedicamentosUso { get; set; } = string.Empty;
    public string FormaPagamento { get; set; } = string.Empty;
    public string NumeroCarteirinha { get; set; } = string.Empty;
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public DateOnly? DataNascimento { get; set; }
    public DateOnly? Vencimento { get; set; }
    public DateOnly? Validade { get; set; }
    public decimal? PressaoSistolica { get; set; }
    public decimal? PressaoDiastolica { get; set; }
    public decimal? FrequenciaCardiaca { get; set; }
    public decimal? FrequenciaRespiratoria { get; set; }
    public decimal? Temperatura { get; set; }
    public decimal? Saturacao { get; set; }
    public decimal? Peso { get; set; }
    public decimal? Altura { get; set; }
    public decimal? Glicemia { get; set; }
    public decimal? Valor { get; set; }
    public bool Principal { get; set; }
}

public sealed class Saude360ActionRequest
{
    public Guid? Id { get; set; }
    public Guid? PainelId { get; set; }
    public Guid? FilaId { get; set; }
    public Guid? PacienteId { get; set; }
    public Guid? AgendamentoId { get; set; }
    public Guid? ConsultaId { get; set; }
    public Guid? MedicoId { get; set; }
    public Guid? ContaReceberId { get; set; }
    public Guid? CaixaId { get; set; }
    public Guid? ConvenioId { get; set; }
    public Guid? PlanoSaudeId { get; set; }
    public Guid? ProcedimentoId { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
    public string FormaPagamento { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
    public decimal? Valor { get; set; }
    public DateTime? NovaDataInicio { get; set; }
    public DateTime? NovaDataFim { get; set; }
}
