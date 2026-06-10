namespace PlantaoPro.Api.Models;

public sealed class Saude360ItemDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? ClienteId { get; set; }
    public Guid? PacienteId { get; set; }
    public Guid? MedicoId { get; set; }
    public Guid? AgendamentoId { get; set; }
    public Guid? ConsultaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
}

public sealed class PacienteRequest
{
    public string Nome { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public DateTime? DataNascimento { get; set; }
    public string Telefone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class PainelChamadaAcaoRequest
{
    public Guid? PainelId { get; set; }
    public Guid? FilaId { get; set; }
    public Guid? AgendamentoId { get; set; }
    public Guid? PacienteId { get; set; }
    public Guid? SetorId { get; set; }
    public Guid? SalaId { get; set; }
    public Guid? GuicheId { get; set; }
    public string Senha { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class AgendamentoRequest
{
    public Guid PacienteId { get; set; }
    public Guid? MedicoId { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Observacoes { get; set; } = string.Empty;
    public decimal? ValorPrevisto { get; set; }
}

public sealed class AgendamentoCancelamentoRequest { public string Motivo { get; set; } = string.Empty; }
public sealed class AgendamentoReagendamentoRequest
{
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string Motivo { get; set; } = string.Empty;
}

public sealed class TriagemRequest
{
    public Guid PacienteId { get; set; }
    public Guid? AgendamentoId { get; set; }
    public string ClassificacaoRisco { get; set; } = string.Empty;
    public string QueixaPrincipal { get; set; } = string.Empty;
    public string SinaisVitais { get; set; } = string.Empty;
}

public sealed class ConsultaRequest
{
    public Guid PacienteId { get; set; }
    public Guid? MedicoId { get; set; }
    public Guid? AgendamentoId { get; set; }
    public Guid? TriagemId { get; set; }
    public string Anamnese { get; set; } = string.Empty;
    public string ExameFisico { get; set; } = string.Empty;
    public string Conduta { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class CidRequest
{
    public string Codigo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Capitulo { get; set; } = string.Empty;
}

public sealed class PrescricaoRequest
{
    public Guid PacienteId { get; set; }
    public Guid? ConsultaId { get; set; }
    public Guid? MedicoId { get; set; }
    public string Orientacoes { get; set; } = string.Empty;
    public string ItensResumo { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
}

public sealed class PrescricaoModeloRequest
{
    public string Nome { get; set; } = string.Empty;
    public string ItensResumo { get; set; } = string.Empty;
}

public sealed class ClinicaRecebimentoRequest
{
    public Guid? ContaReceberId { get; set; }
    public decimal Valor { get; set; }
    public string FormaPagamento { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
}

public sealed class ConvenioRequest
{
    public string Nome { get; set; } = string.Empty;
    public string RegistroAns { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class ConvenioAutorizacaoRequest
{
    public Guid? ConvenioId { get; set; }
    public Guid? PacienteId { get; set; }
    public Guid? ProcedimentoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class PlanoSaudeRequest
{
    public string Nome { get; set; } = string.Empty;
    public Guid? ConvenioId { get; set; }
    public string CoberturaResumo { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
}

public sealed class PlanoSaudePacienteRequest
{
    public Guid PlanoSaudeId { get; set; }
    public string NumeroCarteirinha { get; set; } = string.Empty;
    public DateTime? ValidadeCarteirinha { get; set; }
    public bool Principal { get; set; }
}
