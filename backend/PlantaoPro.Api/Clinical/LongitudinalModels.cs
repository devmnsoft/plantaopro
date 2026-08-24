using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Api.Clinical;

public sealed record PacienteProntuarioDto(PacienteIdentificacaoDto Paciente, ResumoClinicoDto Resumo);
public sealed record PacienteIdentificacaoDto(Guid Id, string Nome, string? NomeSocial, DateOnly? DataNascimento, string? SexoGenero, string? Unidade, string? Telefone, string? Email, string? Convenio);
public sealed record ResumoClinicoDto(IReadOnlyList<PacienteProblemaDto> ProblemasAtivos, IReadOnlyList<PacienteAlergiaDto> Alergias, IReadOnlyList<PacienteMedicamentoDto> MedicamentosEmUso, IReadOnlyList<ConsultaLongitudinalDto> UltimasConsultas, IReadOnlyList<TriagemLongitudinalDto> UltimasTriagens, IReadOnlyList<PrescricaoLongitudinalDto> PrescricoesRecentes, IReadOnlyList<SolicitacaoExameDto> ExamesPendentes, IReadOnlyList<ResultadoExameDto> ResultadosRecentes, IReadOnlyList<EncaminhamentoClinicoDto> EncaminhamentosPendentes, IReadOnlyList<DocumentoClinicoDto> Documentos, IReadOnlyList<AlertaClinicoDto> Alertas);
public sealed record AlertaClinicoDto(string Tipo, string Titulo, string Gravidade);
public sealed record TimelineClinicaDto(Guid Id, string Tipo, DateTime Data, string? Profissional, string? Unidade, string Titulo, string ResumoSeguro, Guid EntidadeId, string EntidadeTipo, bool PermiteDetalhes);
public sealed record ConsultaLongitudinalDto(Guid Id, DateTime Data, string Status, string? Profissional, string? Unidade);
public sealed record TriagemLongitudinalDto(Guid Id, DateTime Data, string? ClassificacaoRisco, string? Profissional);
public sealed record PrescricaoLongitudinalDto(Guid Id, DateTime Data, string Status, string? Profissional, int QuantidadeItens);

public sealed record PacienteProblemaDto(Guid Id, Guid? CidId, string? CidCodigo, string Descricao, string Status, DateOnly DataInicio, DateOnly? DataResolucao, string? Observacao, Guid? OrigemConsultaId, int Versao, DateTime CriadoEm, DateTime? AtualizadoEm);
public sealed record CriarProblemaRequest(Guid? CidId, [Required, MaxLength(500)] string Descricao, DateOnly? DataInicio, string? Observacao, Guid? OrigemConsultaId);
public sealed record AtualizarProblemaRequest([Required, MaxLength(500)] string Descricao, string Status, DateOnly DataInicio, DateOnly? DataResolucao, string? Observacao, int Versao);
public sealed record ResolverProblemaRequest(int Versao, DateOnly? DataResolucao, string? Observacao);

public sealed record PacienteAlergiaDto(Guid Id, string Tipo, string Substancia, string? Descricao, string Gravidade, string? Reacao, string Status, bool Confirmada, Guid? OrigemConsultaId, int Versao, DateTime RegistradoEm);
public sealed record CriarAlergiaRequest([Required] string Tipo, [Required, MaxLength(250)] string Substancia, string? Descricao, string Gravidade = "NAO_INFORMADA", string? Reacao = null, bool Confirmada = false, Guid? OrigemConsultaId = null);
public sealed record AtualizarAlergiaRequest(string Tipo, string Substancia, string? Descricao, string Gravidade, string? Reacao, string Status, bool Confirmada, int Versao);

public sealed record PacienteMedicamentoDto(Guid Id, Guid? MedicamentoId, string MedicamentoDescricao, string? Dose, string? Frequencia, string? Via, DateOnly? InicioEm, DateOnly? FimEm, string Status, string Origem, Guid? ConsultaId, Guid? PrescricaoId, string? Observacao, int Versao, DateTime CriadoEm);
public sealed record CriarMedicamentoUsoRequest(Guid? MedicamentoId, [Required] string MedicamentoDescricao, string? Dose, string? Frequencia, string? Via, DateOnly? InicioEm, string Origem = "INFORMADO_PELO_PACIENTE", Guid? ConsultaId = null, Guid? PrescricaoId = null, string? Observacao = null);
public sealed record AtualizarMedicamentoUsoRequest(string MedicamentoDescricao, string? Dose, string? Frequencia, string? Via, DateOnly? InicioEm, DateOnly? FimEm, string Status, string? Observacao, int Versao);

public sealed record SolicitacaoExameItemDto(Guid Id, string? Codigo, string Nome, string Tipo, string? Observacao, string Status);
public sealed class SolicitacaoExameDto { public Guid Id { get; set; } public Guid PacienteId { get; set; } public Guid? ConsultaId { get; set; } public Guid? MedicoId { get; set; } public Guid? UnidadeId { get; set; } public string Status { get; set; } = ""; public string Prioridade { get; set; } = ""; public string IndicacaoClinica { get; set; } = ""; public string? Observacoes { get; set; } public DateTime SolicitadoEm { get; set; } public DateTime? RealizadoEm { get; set; } public IReadOnlyList<SolicitacaoExameItemDto> Itens { get; set; } = Array.Empty<SolicitacaoExameItemDto>(); }
public sealed record CriarSolicitacaoExamesRequest(string Prioridade, [Required] string IndicacaoClinica, string? Observacoes, [Required, MinLength(1)] IReadOnlyList<CriarSolicitacaoExameItemRequest> Itens);
public sealed record CriarSolicitacaoExameItemRequest(string? Codigo, [Required] string Nome, string Tipo = "OUTRO", string? Observacao = null);
public sealed record RegistrarResultadoExameRequest(Guid? ItemId, string Tipo, [Required] string Resumo, [Required] string ResultadoTextual, DateTime RealizadoEm, DateTime? LiberadoEm, string? ProfissionalResponsavel, Guid? DocumentoId);
public sealed record ResultadoExameDto(Guid Id, Guid SolicitacaoId, Guid? ItemId, Guid PacienteId, string Tipo, string Resumo, string ResultadoTextual, DateTime RealizadoEm, DateTime? LiberadoEm, string? ProfissionalResponsavel, Guid? DocumentoId, DateTime CriadoEm);

public sealed record EncaminhamentoClinicoDto(Guid Id, Guid PacienteId, Guid ConsultaId, Guid? EspecialidadeDestinoId, Guid? ProfissionalDestinoId, Guid? UnidadeDestinoId, string Motivo, string ResumoClinico, string Prioridade, string Status, DateTime CriadoEm, DateTime? AgendadoEm, DateTime? ConcluidoEm);
public sealed record CriarEncaminhamentoClinicoRequest(Guid? EspecialidadeDestinoId, Guid? ProfissionalDestinoId, Guid? UnidadeDestinoId, [Required] string Motivo, [Required] string ResumoClinico, string Prioridade = "ROTINA");

public sealed record DocumentoClinicoDto(Guid Id, Guid PacienteId, Guid? ConsultaId, string Tipo, string Titulo, string Conteudo, string Status, int Versao, Guid? EmitidoPor, DateTime? EmitidoEm, string? MotivoCancelamento, string? HashDocumento, string AssinaturaStatus, DateTime CriadoEm, DateTime? AtualizadoEm);
public sealed record CriarDocumentoClinicoRequest(Guid PacienteId, Guid? ConsultaId, [Required] string Tipo, [Required] string Titulo, [Required] string Conteudo, bool IncluirCid = false, int? QuantidadeDias = null, DateOnly? InicioAfastamento = null);
public sealed record CancelarDocumentoClinicoRequest([Required, MinLength(10)] string Motivo, int Versao);

public interface IClinicalDocumentSignatureProvider { string InitialStatus { get; } }
public sealed class NoOpClinicalDocumentSignatureProvider : IClinicalDocumentSignatureProvider { public string InitialStatus => "NAO_ASSINADO"; }
public interface IMedicationSafetyProvider { Task<IReadOnlyList<AlertaClinicoDto>> AvaliarAsync(Guid pacienteId, string medicamento, CancellationToken ct); }
