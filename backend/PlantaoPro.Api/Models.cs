namespace PlantaoPro.Api.Models;
public record ApiResponse<T>(bool Success,string Message,T? Data,IEnumerable<string>? Errors,int StatusCode,DateTime Timestamp){
    public static ApiResponse<T> Ok(T data,string message="Sucesso")=>new(true,message,data,null,200,DateTime.UtcNow);
    public static ApiResponse<T> Fail(string message,int status=400,IEnumerable<string>? errors=null)=>new(false,message,default,errors,status,DateTime.UtcNow);
}
public record LoginRequest(string Email,string Senha);
public record LoginResponse(string Token,DateTime ExpiresAt,Guid UsuarioId,string Nome,string[] Roles);
public record MedicoDto(Guid Id,string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId,char RegStatus);
public record CreateMedicoRequest(string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId);

public record HospitalDto(Guid Id,string RazaoSocial,string NomeFantasia,string Cnpj,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel,char RegStatus);
public record CreateHospitalRequest(string RazaoSocial,string NomeFantasia,string Cnpj,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel);
public record UpdateHospitalRequest(string RazaoSocial,string NomeFantasia,string Telefone,string Email,string Endereco,string Cidade,string Estado,string Responsavel,char RegStatus);
public record EspecialidadeDto(Guid Id,string Nome,string Descricao,char RegStatus);
public record CreateEspecialidadeRequest(string Nome,string Descricao);
public record UpdateEspecialidadeRequest(string Nome,string Descricao,char RegStatus);
public record PlantaoDto(Guid Id,Guid HospitalId,Guid EspecialidadeId,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,string Tipo,string Status,string Observacoes);
public record CreatePlantaoRequest(Guid HospitalId,Guid EspecialidadeId,DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,string Tipo,string Observacoes);
public record UpdatePlantaoRequest(DateTime DataInicio,DateTime DataFim,decimal Valor,int Vagas,string Tipo,string Observacoes);
public record StatusRequest(string Justificativa);
public record DashboardDto(int TotalMedicos,int TotalHospitais,int TotalEspecialidades,int TotalPlantoes,int PlantoesAbertos,int PlantoesConfirmados,int PlantoesRealizados,int PlantoesCancelados,int PagamentosPendentes,int PagamentosPagos,decimal ValorPendente,decimal ValorPagoMes,int NotificacoesNaoLidas);
