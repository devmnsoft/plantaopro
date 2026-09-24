using System.ComponentModel.DataAnnotations;

namespace PlantaoPro.Api.Administrativo360;

public sealed record DepartamentoDto(Guid Id, string Codigo, string Nome, bool Ativo);
public sealed record CargoDto(Guid Id, string Codigo, string Nome, Guid? DepartamentoId, string? Departamento, bool Ativo);
public sealed record ColaboradorDto(Guid Id, string Matricula, string Nome, string Cpf, string Email, Guid CargoId, string Cargo, Guid? DepartamentoId, string? Departamento, string Status);
public sealed record ContratoTrabalhoDto(Guid Id, Guid ColaboradorId, string Colaborador, string Tipo, DateOnly Inicio, DateOnly? Fim, decimal Salario, int CargaHorariaSemanal, string Status);
public sealed record Administrativo360ResumoDto(int Departamentos, int Cargos, int ColaboradoresAtivos, int ContratosVigentes);

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
public sealed class ContratoTrabalhoRequest
{
    public Guid ColaboradorId { get; set; }
    [Required, RegularExpression("CLT|PJ|ESTAGIO|TEMPORARIO")] public string Tipo { get; set; } = "CLT";
    public DateOnly Inicio { get; set; }
    public DateOnly? Fim { get; set; }
    [Range(0.01, 999999999)] public decimal Salario { get; set; }
    [Range(1, 60)] public int CargaHorariaSemanal { get; set; } = 40;
}
