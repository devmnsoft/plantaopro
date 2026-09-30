using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Api.Administrativo360;

public sealed record DepartamentoDto(Guid Id, string Codigo, string Nome, bool Ativo);
public sealed record CargoDto(Guid Id, string Codigo, string Nome, Guid? DepartamentoId, string? Departamento, bool Ativo);
public sealed record ColaboradorDto(Guid Id, string Matricula, string Nome, string Cpf, string Email, Guid CargoId, string Cargo, Guid? DepartamentoId, string? Departamento, string Status);
// Contrato externo preserva CargaHorariaSemanal como int (já publicado em JSON).
// A carga horária é smallint no PostgreSQL; as consultas projetam explicitamente ::integer
// (ver Administrativo360Service) para manter a materialização Dapper compatível sem cast de leitura.
public sealed record ContratoTrabalhoDto(Guid Id, Guid ColaboradorId, string Colaborador, string Tipo, DateOnly Inicio, DateOnly? Fim, decimal Salario, int CargaHorariaSemanal, string Status);
// COUNT(*) no PostgreSQL retorna bigint (Int64): os contadores usam long para manter a
// representação de leitura compatível com o banco, sem cast lossy para int.
public sealed record Administrativo360ResumoDto(long Departamentos, long Cargos, long ColaboradoresAtivos, long ContratosVigentes);
// Envelope de paginação das listagens: limit/offset aplicados no banco e contagem total.
public sealed record Administrativo360PaginadoDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total, int TotalPages);
public sealed record ContratoTrabalhoDetalheDto(Guid Id, Guid ColaboradorId, string Colaborador, string Matricula, string Tipo, DateOnly Inicio, DateOnly? Fim, decimal Salario, int CargaHorariaSemanal, string Status, DateTime CriadoEm);

public sealed class DepartamentoRequest
{
    [Required, MaxLength(30)] public string Codigo { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Nome { get; set; } = string.Empty;
}
public sealed class CargoRequest
{
    [Required, MaxLength(30)] public string Codigo { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Nome { get; set; } = string.Empty;
    public Guid? DepartamentoId { get; set; }
}
public sealed class ColaboradorRequest
{
    [Required, MaxLength(30)] public string Matricula { get; set; } = string.Empty;
    [Required, MaxLength(160)] public string Nome { get; set; } = string.Empty;
    [Required, RegularExpression(@"^\d{11}$")] public string Cpf { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(180)] public string Email { get; set; } = string.Empty;
    public Guid CargoId { get; set; }
}
// Edição restrita de cadastro do colaborador: identidade (matrícula/CPF) e vínculo
// funcional (cargo) mudam por processo próprio; aqui preservam-se os dados de contato.
public sealed class ColaboradorUpdateRequest
{
    [Required, MaxLength(160)] public string Nome { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(180)] public string Email { get; set; } = string.Empty;
}
public sealed class ColaboradorStatusRequest
{
    [Required, RegularExpression(@"ATIVO|AFASTADO|DESLIGADO")] public string Status { get; set; } = "ATIVO";
}
public sealed class ContratoTrabalhoRequest
{
    public Guid ColaboradorId { get; set; }
    [Required, RegularExpression("CLT|PJ|ESTAGIO|TEMPORARIO")] public string Tipo { get; set; } = "CLT";
    public DateOnly Inicio { get; set; }
    public DateOnly? Fim { get; set; }
    [Range(0.01, 999999999)] public decimal Salario { get; set; }
    [Range(1, 60)] public int CargaHorariaSemanal { get; set; } = 40;
}
// Encerramento: a data final pode ser informada; quando omitida usa-se o dia atual.
// Não se edita a vigência de contratos já encerrados/cancelados (histórico preservado).
public sealed class EncerrarContratoRequest
{
    public DateOnly? Fim { get; set; }
}
