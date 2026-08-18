using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Clinical;

public interface IClinicalAccessService { ApiResponse<bool> Authorize(bool write = false); }
public sealed class ClinicalAccessService : IClinicalAccessService
{
    private readonly ICurrentUserService user;
    private static readonly string[] Denied = { "FINANCEIRO", "RECEPCAO", "ADMIN_GLOBAL", "ADMINISTRADOR_GLOBAL" };
    private static readonly string[] Clinical = { "MEDICO", "ENFERMEIRO", "ENFERMAGEM", "TRIAGEM", "AUDITOR_CLINICO" };
    public ClinicalAccessService(ICurrentUserService user) => this.user=user;
    public ApiResponse<bool> Authorize(bool write=false)
    {
        if (user.ClienteId is null && user.TenantId is null) return ApiResponse<bool>.Fail("Contexto clínico da organização é obrigatório.",403);
        if (user.Roles.Any(r=>Denied.Contains(r,StringComparer.OrdinalIgnoreCase))) return ApiResponse<bool>.Fail("Seu perfil não possui acesso ao conteúdo clínico.",403);
        if (!user.Roles.Any(r=>Clinical.Contains(r,StringComparer.OrdinalIgnoreCase))) return ApiResponse<bool>.Fail("Vínculo assistencial ou perfil clínico obrigatório.",403);
        if (write && user.HasRole("AUDITOR_CLINICO")) return ApiResponse<bool>.Fail("Auditoria clínica possui acesso somente para leitura.",403);
        return ApiResponse<bool>.Ok(true);
    }
}

public interface ILongitudinalService
{
    Task<ApiResponse<PacienteProntuarioDto>> ProntuarioAsync(Guid pacienteId,CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<TimelineClinicaDto>>> TimelineAsync(Guid pacienteId,CancellationToken ct);
}
public sealed class LongitudinalService : ILongitudinalService
{
    private readonly ILongitudinalRepository repository; private readonly ICurrentUserService user; private readonly IClinicalAccessService access; private readonly IAuditService audit;
    public LongitudinalService(ILongitudinalRepository repository,ICurrentUserService user,IClinicalAccessService access,IAuditService audit){this.repository=repository;this.user=user;this.access=access;this.audit=audit;}
    private Guid Tenant => user.ClienteId??user.TenantId??Guid.Empty;
    private Task Audit(Guid p,string action,Guid? entity=null)=>audit.RegistrarAsync(user.UserId,Tenant,"prontuario",entity??p,action,new{pacienteId=p,entidadeId=entity,resultado="SUCESSO"},true,null,string.Join(',',user.Roles));
    public async Task<ApiResponse<PacienteProntuarioDto>> ProntuarioAsync(Guid p,CancellationToken ct){var allowed=access.Authorize();if(!allowed.Success)return ApiResponse<PacienteProntuarioDto>.Fail(allowed.Message,allowed.StatusCode);var paciente=await repository.PacienteAsync(Tenant,p,ct);if(paciente is null)return ApiResponse<PacienteProntuarioDto>.Fail("Paciente não encontrado.",404);var resumo=await repository.ResumoAsync(Tenant,p,ct);await Audit(p,"PRONTUARIO_VISUALIZAR");return ApiResponse<PacienteProntuarioDto>.Ok(new(paciente,resumo));}
    public async Task<ApiResponse<IReadOnlyList<TimelineClinicaDto>>> TimelineAsync(Guid p,CancellationToken ct){var allowed=access.Authorize();if(!allowed.Success)return ApiResponse<IReadOnlyList<TimelineClinicaDto>>.Fail(allowed.Message,allowed.StatusCode);if(await repository.PacienteAsync(Tenant,p,ct) is null)return ApiResponse<IReadOnlyList<TimelineClinicaDto>>.Fail("Paciente não encontrado.",404);var value=await repository.TimelineAsync(Tenant,p,ct);await Audit(p,"PRONTUARIO_TIMELINE");return ApiResponse<IReadOnlyList<TimelineClinicaDto>>.Ok(value);}
}
