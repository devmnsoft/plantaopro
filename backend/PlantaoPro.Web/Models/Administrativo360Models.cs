namespace PlantaoPro.Web.Models;

public sealed record Administrativo360ResumoViewModel(int Departamentos, int Cargos, int ColaboradoresAtivos, int ContratosVigentes);
public sealed record Departamento360ViewModel(Guid Id,string Codigo,string Nome,bool Ativo);
public sealed record Cargo360ViewModel(Guid Id,string Codigo,string Nome,Guid? DepartamentoId,string? Departamento,bool Ativo);
public sealed record Colaborador360ViewModel(Guid Id,string Matricula,string Nome,string Cpf,string Email,Guid CargoId,string Cargo,Guid? DepartamentoId,string? Departamento,string Status);
public sealed record Contrato360ViewModel(Guid Id,Guid ColaboradorId,string Colaborador,string Tipo,DateOnly Inicio,DateOnly? Fim,decimal Salario,int CargaHorariaSemanal,string Status);
public sealed class Administrativo360PageViewModel
{
 public Administrativo360ResumoViewModel Resumo { get; init; }=new(0,0,0,0);
 public IReadOnlyList<Departamento360ViewModel> Departamentos { get; init; }=Array.Empty<Departamento360ViewModel>();
 public IReadOnlyList<Cargo360ViewModel> Cargos { get; init; }=Array.Empty<Cargo360ViewModel>();
 public IReadOnlyList<Colaborador360ViewModel> Colaboradores { get; init; }=Array.Empty<Colaborador360ViewModel>();
 public IReadOnlyList<Contrato360ViewModel> Contratos { get; init; }=Array.Empty<Contrato360ViewModel>();
 public string? Erro { get; init; }
}
