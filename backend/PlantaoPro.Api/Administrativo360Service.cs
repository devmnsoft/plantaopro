using Dapper;
using Npgsql;
using PlantaoPro.Api.Administrativo360;

namespace PlantaoPro.Api;

public sealed class Administrativo360Service
{
    private readonly IConfiguration configuration;
    private readonly ICurrentUserService currentUser;
    public Administrativo360Service(IConfiguration configuration, ICurrentUserService currentUser) { this.configuration = configuration; this.currentUser = currentUser; }
    private NpgsqlConnection Connection() => new(configuration.GetConnectionString("Default"));
    private Guid Tenant() => currentUser.TenantId ?? throw new UnauthorizedAccessException("Selecione uma organização.");

    public async Task<Administrativo360ResumoDto> ResumoAsync(CancellationToken ct)
    {
        await using var cn = Connection();
        return await cn.QuerySingleAsync<Administrativo360ResumoDto>(new CommandDefinition(@"select
 (select count(*) from plantaopro.adm_departamentos where tenant_id=@tenant and reg_status='A') as Departamentos,
 (select count(*) from plantaopro.adm_cargos where tenant_id=@tenant and reg_status='A') as Cargos,
 (select count(*) from plantaopro.adm_colaboradores where tenant_id=@tenant and status='ATIVO' and reg_status='A') as ColaboradoresAtivos,
 (select count(*) from plantaopro.adm_contratos_trabalho where tenant_id=@tenant and status='VIGENTE' and reg_status='A') as ContratosVigentes", new { tenant = Tenant() }, cancellationToken: ct));
    }
    public async Task<IReadOnlyList<DepartamentoDto>> DepartamentosAsync(CancellationToken ct) { await using var cn=Connection(); return (await cn.QueryAsync<DepartamentoDto>(new CommandDefinition("select id,codigo,nome,ativo from plantaopro.adm_departamentos where tenant_id=@tenant and reg_status='A' order by nome",new{tenant=Tenant()},cancellationToken:ct))).AsList(); }
    public async Task<IReadOnlyList<CargoDto>> CargosAsync(CancellationToken ct) { await using var cn=Connection(); return (await cn.QueryAsync<CargoDto>(new CommandDefinition("select c.id,c.codigo,c.nome,c.departamento_id as DepartamentoId,d.nome as Departamento,c.ativo from plantaopro.adm_cargos c left join plantaopro.adm_departamentos d on d.id=c.departamento_id where c.tenant_id=@tenant and c.reg_status='A' order by c.nome",new{tenant=Tenant()},cancellationToken:ct))).AsList(); }
    public async Task<IReadOnlyList<ColaboradorDto>> ColaboradoresAsync(CancellationToken ct) { await using var cn=Connection(); return (await cn.QueryAsync<ColaboradorDto>(new CommandDefinition(@"select p.id,p.matricula,p.nome,p.cpf,p.email,p.cargo_id as CargoId,c.nome as Cargo,c.departamento_id as DepartamentoId,d.nome as Departamento,p.status from plantaopro.adm_colaboradores p join plantaopro.adm_cargos c on c.id=p.cargo_id left join plantaopro.adm_departamentos d on d.id=c.departamento_id where p.tenant_id=@tenant and p.reg_status='A' order by p.nome",new{tenant=Tenant()},cancellationToken:ct))).AsList(); }
    public async Task<IReadOnlyList<ContratoTrabalhoDto>> ContratosAsync(CancellationToken ct) { await using var cn=Connection(); return (await cn.QueryAsync<ContratoTrabalhoDto>(new CommandDefinition(@"select x.id,x.colaborador_id as ColaboradorId,p.nome as Colaborador,x.tipo,x.inicio,x.fim,x.salario,x.carga_horaria_semanal as CargaHorariaSemanal,x.status from plantaopro.adm_contratos_trabalho x join plantaopro.adm_colaboradores p on p.id=x.colaborador_id where x.tenant_id=@tenant and x.reg_status='A' order by x.inicio desc",new{tenant=Tenant()},cancellationToken:ct))).AsList(); }
    public async Task<DepartamentoDto> CriarDepartamentoAsync(DepartamentoRequest r,CancellationToken ct) { await using var cn=Connection(); return await cn.QuerySingleAsync<DepartamentoDto>(new CommandDefinition(@"insert into plantaopro.adm_departamentos(id,tenant_id,codigo,nome,created_by) values(gen_random_uuid(),@tenant,upper(trim(@Codigo)),trim(@Nome),@user) returning id,codigo,nome,ativo",new{tenant=Tenant(),r.Codigo,r.Nome,user=currentUser.UserId},cancellationToken:ct)); }
    public async Task<CargoDto> CriarCargoAsync(CargoRequest r,CancellationToken ct) { await using var cn=Connection(); return await cn.QuerySingleAsync<CargoDto>(new CommandDefinition(@"with novo as (insert into plantaopro.adm_cargos(id,tenant_id,codigo,nome,departamento_id,created_by) select gen_random_uuid(),@tenant,upper(trim(@Codigo)),trim(@Nome),@DepartamentoId,@user where @DepartamentoId is null or exists(select 1 from plantaopro.adm_departamentos where id=@DepartamentoId and tenant_id=@tenant and reg_status='A') returning *) select n.id,n.codigo,n.nome,n.departamento_id as DepartamentoId,d.nome as Departamento,n.ativo from novo n left join plantaopro.adm_departamentos d on d.id=n.departamento_id",new{tenant=Tenant(),r.Codigo,r.Nome,r.DepartamentoId,user=currentUser.UserId},cancellationToken:ct)); }
    public async Task<ColaboradorDto> CriarColaboradorAsync(ColaboradorRequest r,CancellationToken ct) { await using var cn=Connection(); return await cn.QuerySingleAsync<ColaboradorDto>(new CommandDefinition(@"with novo as (insert into plantaopro.adm_colaboradores(id,tenant_id,matricula,nome,cpf,email,cargo_id,created_by) select gen_random_uuid(),@tenant,upper(trim(@Matricula)),trim(@Nome),@Cpf,lower(trim(@Email)),@CargoId,@user where exists(select 1 from plantaopro.adm_cargos where id=@CargoId and tenant_id=@tenant and ativo and reg_status='A') returning *) select n.id,n.matricula,n.nome,n.cpf,n.email,n.cargo_id as CargoId,c.nome as Cargo,c.departamento_id as DepartamentoId,d.nome as Departamento,n.status from novo n join plantaopro.adm_cargos c on c.id=n.cargo_id left join plantaopro.adm_departamentos d on d.id=c.departamento_id",new{tenant=Tenant(),r.Matricula,r.Nome,r.Cpf,r.Email,r.CargoId,user=currentUser.UserId},cancellationToken:ct)); }
    public async Task<ContratoTrabalhoDto> ContratarAsync(ContratoTrabalhoRequest r,CancellationToken ct)
    {
        if(r.Inicio==default || (r.Fim.HasValue&&r.Fim<r.Inicio)) throw new ArgumentException("Vigência do contrato inválida.");
        await using var cn=Connection(); await cn.OpenAsync(ct); await using var tx=await cn.BeginTransactionAsync(ct);
        var tenant=Tenant();
        var exists=await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.adm_colaboradores where id=@id and tenant_id=@tenant and status='ATIVO' and reg_status='A')",new{id=r.ColaboradorId,tenant},tx,cancellationToken:ct));
        if(!exists) throw new ArgumentException("Colaborador não pertence à organização ou está inativo.");
        var overlap=await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.adm_contratos_trabalho where colaborador_id=@id and reg_status='A' and daterange(inicio,coalesce(fim,'infinity'::date),'[]') && daterange(@inicio,coalesce(@fim,'infinity'::date),'[]'))",new{id=r.ColaboradorId,inicio=r.Inicio,fim=r.Fim},tx,cancellationToken:ct));
        if(overlap) throw new InvalidOperationException("Já existe contrato no período informado.");
        var dto=await cn.QuerySingleAsync<ContratoTrabalhoDto>(new CommandDefinition(@"with novo as (insert into plantaopro.adm_contratos_trabalho(id,tenant_id,colaborador_id,tipo,inicio,fim,salario,carga_horaria_semanal,created_by) values(gen_random_uuid(),@tenant,@ColaboradorId,@Tipo,@Inicio,@Fim,@Salario,@CargaHorariaSemanal,@user) returning *) select n.id,n.colaborador_id as ColaboradorId,p.nome as Colaborador,n.tipo,n.inicio,n.fim,n.salario,n.carga_horaria_semanal as CargaHorariaSemanal,n.status from novo n join plantaopro.adm_colaboradores p on p.id=n.colaborador_id",new{tenant,r.ColaboradorId,Tipo=r.Tipo.ToUpperInvariant(),r.Inicio,r.Fim,r.Salario,r.CargaHorariaSemanal,user=currentUser.UserId},tx,cancellationToken:ct));
        await tx.CommitAsync(ct); return dto;
    }
}
