using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Clinical;

public interface IClinicalAccessService
{
    ApiResponse<bool> Authorize(bool write = false);
    Task<ApiResponse<bool>> AuthorizePatientAsync(Guid pacienteId, bool write, CancellationToken ct);
}
public sealed class ClinicalAccessService : IClinicalAccessService
{
    private readonly ICurrentUserService user;
    private readonly ILongitudinalRepository repository;
    private static readonly string[] Denied = { "FINANCEIRO", "RECEPCAO", "ADMIN_GLOBAL", "ADMINISTRADOR_GLOBAL" };
    private static readonly string[] Clinical = { "MEDICO", "ENFERMEIRO", "ENFERMAGEM", "TRIAGEM", "AUDITOR_CLINICO" };
    public ClinicalAccessService(ICurrentUserService user, ILongitudinalRepository repository)
    {
        this.user = user;
        this.repository = repository;
    }
    public ApiResponse<bool> Authorize(bool write=false)
    {
        if (user.ClienteId is null && user.TenantId is null) return ApiResponse<bool>.Fail("Contexto clínico da organização é obrigatório.",403);
        if (user.Roles.Any(r=>Denied.Contains(r,StringComparer.OrdinalIgnoreCase))) return ApiResponse<bool>.Fail("Seu perfil não possui acesso ao conteúdo clínico.",403);
        if (!user.Roles.Any(r=>Clinical.Contains(r,StringComparer.OrdinalIgnoreCase))) return ApiResponse<bool>.Fail("Vínculo assistencial ou perfil clínico obrigatório.",403);
        if (write && user.HasRole("AUDITOR_CLINICO")) return ApiResponse<bool>.Fail("Auditoria clínica possui acesso somente para leitura.",403);
        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<bool>> AuthorizePatientAsync(Guid pacienteId, bool write, CancellationToken ct)
    {
        var roleAccess = Authorize(write);
        if (!roleAccess.Success) return roleAccess;
        var tenantId = user.ClienteId ?? user.TenantId;
        if (!tenantId.HasValue || !user.UserId.HasValue)
            return ApiResponse<bool>.Fail("Contexto clínico inválido.", 403);
        if (await repository.PacienteAsync(tenantId.Value, pacienteId, ct) is null)
            return ApiResponse<bool>.Fail("Paciente não encontrado.", 404);

        // O vínculo individual é obrigatório para médicos. Os demais perfis clínicos
        // continuam limitados pelas policies e pelo tenant/unidade do fluxo assistencial.
        if (user.IsDoctor() && !await repository.PossuiVinculoAssistencialAsync(tenantId.Value, pacienteId, user.UserId.Value, ct))
            return ApiResponse<bool>.Fail("Não existe vínculo assistencial ativo com este paciente.", 403);
        return ApiResponse<bool>.Ok(true);
    }
}

public interface ILongitudinalService
{
    Task<ApiResponse<PacienteProntuarioDto>> ProntuarioAsync(Guid pacienteId,CancellationToken ct);
    Task<ApiResponse<IReadOnlyList<TimelineClinicaDto>>> TimelineAsync(Guid pacienteId,string? tipo,int page,int pageSize,CancellationToken ct);
}
public sealed class LongitudinalService : ILongitudinalService
{
    private readonly ILongitudinalRepository repository; private readonly ICurrentUserService user; private readonly IClinicalAccessService access; private readonly IAuditService audit;
    public LongitudinalService(ILongitudinalRepository repository,ICurrentUserService user,IClinicalAccessService access,IAuditService audit){this.repository=repository;this.user=user;this.access=access;this.audit=audit;}
    private Guid Tenant => user.ClienteId??user.TenantId??Guid.Empty;
    private Task Audit(Guid p,string action,Guid? entity=null)=>audit.RegistrarAsync(user.UserId,Tenant,"prontuario",entity??p,action,new{pacienteId=p,entidadeId=entity,resultado="SUCESSO"},true,null,string.Join(',',user.Roles));
    public async Task<ApiResponse<PacienteProntuarioDto>> ProntuarioAsync(Guid p,CancellationToken ct){var allowed=await access.AuthorizePatientAsync(p,false,ct);if(!allowed.Success)return ApiResponse<PacienteProntuarioDto>.Fail(allowed.Message,allowed.StatusCode);var paciente=await repository.PacienteAsync(Tenant,p,ct);var resumo=await repository.ResumoAsync(Tenant,p,ct);await Audit(p,"PRONTUARIO_VISUALIZAR");return ApiResponse<PacienteProntuarioDto>.Ok(new(paciente!,resumo));}
    public async Task<ApiResponse<IReadOnlyList<TimelineClinicaDto>>> TimelineAsync(Guid p,string? tipo,int page,int pageSize,CancellationToken ct){var allowed=await access.AuthorizePatientAsync(p,false,ct);if(!allowed.Success)return ApiResponse<IReadOnlyList<TimelineClinicaDto>>.Fail(allowed.Message,allowed.StatusCode);var value=await repository.TimelineAsync(Tenant,p,tipo,page,pageSize,ct);await Audit(p,"PRONTUARIO_TIMELINE");return ApiResponse<IReadOnlyList<TimelineClinicaDto>>.Ok(value);}
}
