using Dapper;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api;

public sealed class ClienteService
{
    private readonly IConfiguration cfg;
    private readonly IAuditService audit;
    private readonly ILogger<ClienteService> logger;

    public ClienteService(IConfiguration cfg, IAuditService audit, ILogger<ClienteService> logger)
    {
        this.cfg = cfg;
        this.audit = audit;
        this.logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ClienteDto>>> ListAsync()
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var rows = await cn.QueryAsync<ClienteDto>(@"select id,razao_social as RazaoSocial,nome_fantasia as NomeFantasia,cnpj,email,telefone,cidade,estado,plano_id as PlanoId,status,reg_status as RegStatus,reg_date as RegDate,reg_update as RegUpdate from plantaopro.clientes where reg_status='A' order by nome_fantasia");
            return ApiResponse<IEnumerable<ClienteDto>>.Ok(rows);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao listar clientes");
            return ApiResponse<IEnumerable<ClienteDto>>.Fail("Não foi possível listar clientes no momento.", 500);
        }
    }

    public async Task<ApiResponse<ClienteDto>> GetAsync(Guid id)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
            var row = await cn.QueryFirstOrDefaultAsync<ClienteDto>(@"select id,razao_social as RazaoSocial,nome_fantasia as NomeFantasia,cnpj,email,telefone,cidade,estado,plano_id as PlanoId,status,reg_status as RegStatus,reg_date as RegDate,reg_update as RegUpdate from plantaopro.clientes where id=@id", new { id });
            return row is null ? ApiResponse<ClienteDto>.Fail("Cliente não encontrado.",404) : ApiResponse<ClienteDto>.Ok(row);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao detalhar cliente");
            return ApiResponse<ClienteDto>.Fail("Não foi possível detalhar o cliente.", 500);
        }
    }
}
