namespace PlantaoPro.Api.Models;
public record ApiResponse<T>(bool Success,string Message,T? Data,IEnumerable<string>? Errors,int StatusCode,DateTime Timestamp){
    public static ApiResponse<T> Ok(T data,string message="Sucesso")=>new(true,message,data,null,200,DateTime.UtcNow);
    public static ApiResponse<T> Fail(string message,int status=400,IEnumerable<string>? errors=null)=>new(false,message,default,errors,status,DateTime.UtcNow);
}
public record LoginRequest(string Email,string Senha);
public record LoginResponse(string Token,DateTime ExpiresAt,Guid UsuarioId,string Nome,string[] Roles);
public record MedicoDto(Guid Id,string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId,char RegStatus);
public record CreateMedicoRequest(string Nome,string Cpf,string Crm,string UfCrm,string Email,string Telefone,string Cidade,string Estado,Guid EspecialidadeId);
