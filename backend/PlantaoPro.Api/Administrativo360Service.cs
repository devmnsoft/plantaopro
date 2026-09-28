using Dapper;
using Npgsql;
using PlantaoPro.Api.Administrativo360;

namespace PlantaoPro.Api;

public sealed class Administrativo360Service
{
    private static readonly string[] TiposContrato = { "CLT", "PJ", "ESTAGIO", "TEMPORARIO" };
    private static readonly string[] StatusColaboradores = { "ATIVO", "AFASTADO", "DESLIGADO" };
    // Regra de sobreposição: apenas contratos VIGENTES bloqueiam nova contratação.
    // Encerrados/cancelados preservam histórico, mas não impedem rescindir e readmitir
    // no mesmo período (composição dos estados documentada em PENDENCIAS-ROADMAP).
    private const string ContratoBloqueioSobreposicao = "status='VIGENTE'";

    private readonly IConfiguration configuration;
    private readonly ICurrentUserService currentUser;
    public Administrativo360Service(IConfiguration configuration, ICurrentUserService currentUser) { this.configuration = configuration; this.currentUser = currentUser; }
    private NpgsqlConnection Connection() => new(configuration.GetConnectionString("Default"));
    private Guid Tenant() => currentUser.TenantId ?? throw new UnauthorizedAccessException("Selecione uma organização.");

    // ============================================================
    // LEITURAS (listagens paginadas no banco: limit/offset + total)
    // ============================================================
    public async Task<Administrativo360ResumoDto> ResumoAsync(CancellationToken ct)
    {
        // count(*) retorna bigint no PostgreSQL; o DTO usa long (compatível, sem cast lossy).
        await using var cn = Connection();
        return await cn.QuerySingleAsync<Administrativo360ResumoDto>(new CommandDefinition(@"select
  (select count(*) from plantaopro.adm_departamentos where tenant_id=@tenant and reg_status='A') as Departamentos,
  (select count(*) from plantaopro.adm_cargos where tenant_id=@tenant and reg_status='A') as Cargos,
  (select count(*) from plantaopro.adm_colaboradores where tenant_id=@tenant and status='ATIVO' and reg_status='A') as ColaboradoresAtivos,
  (select count(*) from plantaopro.adm_contratos_trabalho where tenant_id=@tenant and status='VIGENTE' and reg_status='A') as ContratosVigentes", new { tenant = Tenant() }, cancellationToken: ct));
    }
    public async Task<Administrativo360PaginadoDto<DepartamentoDto>> DepartamentosAsync(int pagina, int tamanho, string? busca, bool somenteAtivos, CancellationToken ct)
    {
        var (p, t) = NormalizarPagina(pagina, tamanho);
        var termo = TermoBusca(busca);
        var filtro = "tenant_id=@tenant and reg_status in ('A','I')" + (somenteAtivos ? " and ativo=true" : string.Empty) + (termo is null ? string.Empty : " and (codigo ilike @busca or nome ilike @busca)");
        var parms = ParametrosPagina(termo, t, p);
        await using var cn = Connection();
        var total = await cn.ExecuteScalarAsync<long>(new CommandDefinition($"select count(*) from plantaopro.adm_departamentos where {filtro}", parms, cancellationToken: ct));
        var itens = (await cn.QueryAsync<DepartamentoDto>(new CommandDefinition($"select id,codigo,nome,ativo from plantaopro.adm_departamentos where {filtro} order by nome limit @tamanho offset @offset", parms, cancellationToken: ct))).AsList();
        return new Administrativo360PaginadoDto<DepartamentoDto>(itens, p, t, total, TotalPaginas(total, t));
    }
    public async Task<Administrativo360PaginadoDto<CargoDto>> CargosAsync(int pagina, int tamanho, string? busca, bool somenteAtivos, CancellationToken ct)
    {
        var (p, t) = NormalizarPagina(pagina, tamanho);
        var termo = TermoBusca(busca);
        var filtro = "c.tenant_id=@tenant and c.reg_status in ('A','I')" + (somenteAtivos ? " and c.ativo=true" : string.Empty) + (termo is null ? string.Empty : " and (c.codigo ilike @busca or c.nome ilike @busca or d.nome ilike @busca)");
        var parms = ParametrosPagina(termo, t, p);
        await using var cn = Connection();
        var total = await cn.ExecuteScalarAsync<long>(new CommandDefinition($"select count(*) from plantaopro.adm_cargos c left join plantaopro.adm_departamentos d on d.id=c.departamento_id where {filtro}", parms, cancellationToken: ct));
        var itens = (await cn.QueryAsync<CargoDto>(new CommandDefinition($"select c.id,c.codigo,c.nome,c.departamento_id as DepartamentoId,d.nome as Departamento,c.ativo from plantaopro.adm_cargos c left join plantaopro.adm_departamentos d on d.id=c.departamento_id where {filtro} order by c.nome limit @tamanho offset @offset", parms, cancellationToken: ct))).AsList();
        return new Administrativo360PaginadoDto<CargoDto>(itens, p, t, total, TotalPaginas(total, t));
    }
    public async Task<Administrativo360PaginadoDto<ColaboradorDto>> ColaboradoresAsync(int pagina, int tamanho, string? busca, string? status, CancellationToken ct)
    {
        var (p, t) = NormalizarPagina(pagina, tamanho);
        var termo = TermoBusca(busca);
        var alvo = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
        var filtro = "p.tenant_id=@tenant and p.reg_status='A'"
            + (termo is null ? string.Empty : " and (p.nome ilike @busca or p.matricula ilike @busca or p.cpf ilike @busca or p.email ilike @busca)")
            + (alvo is not null && StatusColaboradores.Contains(alvo) ? " and p.status=@status" : string.Empty);
        var parms = ParametrosPagina(termo, t, p);
        if (alvo is not null && StatusColaboradores.Contains(alvo)) parms.Add("status", alvo);
        await using var cn = Connection();
        var total = await cn.ExecuteScalarAsync<long>(new CommandDefinition($"select count(*) from plantaopro.adm_colaboradores p where {filtro}", parms, cancellationToken: ct));
        var itens = (await cn.QueryAsync<ColaboradorDto>(new CommandDefinition($"select p.id,p.matricula,p.nome,p.cpf,p.email,p.cargo_id as CargoId,c.nome as Cargo,c.departamento_id as DepartamentoId,d.nome as Departamento,p.status from plantaopro.adm_colaboradores p join plantaopro.adm_cargos c on c.id=p.cargo_id left join plantaopro.adm_departamentos d on d.id=c.departamento_id where {filtro} order by p.nome limit @tamanho offset @offset", parms, cancellationToken: ct))).AsList();
        return new Administrativo360PaginadoDto<ColaboradorDto>(itens, p, t, total, TotalPaginas(total, t));
    }
    public async Task<Administrativo360PaginadoDto<ContratoTrabalhoDto>> ContratosAsync(int pagina, int tamanho, string? busca, string? status, CancellationToken ct)
    {
        // LEFT JOIN preserva contratos cujo colaborador foi inativado (histórico visível).
        // carga_horaria_semanal é smallint no banco: projetado explicitamente como integer
        // para casar com o contrato público (int CargaHorariaSemanal).
        var (p, t) = NormalizarPagina(pagina, tamanho);
        var termo = TermoBusca(busca);
        var alvo = string.IsNullOrWhiteSpace(status) ? null : status.Trim().ToUpperInvariant();
        var filtro = "x.tenant_id=@tenant and x.reg_status='A'"
            + (termo is null ? string.Empty : " and (p.nome ilike @busca or p.matricula ilike @busca or x.tipo ilike @busca)")
            + (alvo is not null && ("VIGENTE" == alvo || "ENCERRADO" == alvo || "CANCELADO" == alvo) ? " and x.status=@status" : string.Empty);
        var parms = ParametrosPagina(termo, t, p);
        if (alvo is not null && ("VIGENTE" == alvo || "ENCERRADO" == alvo || "CANCELADO" == alvo)) parms.Add("status", alvo);
        await using var cn = Connection();
        var total = await cn.ExecuteScalarAsync<long>(new CommandDefinition($"select count(*) from plantaopro.adm_contratos_trabalho x left join plantaopro.adm_colaboradores p on p.id=x.colaborador_id where {filtro}", parms, cancellationToken: ct));
        var itens = (await cn.QueryAsync<ContratoTrabalhoDto>(new CommandDefinition($"select x.id,x.colaborador_id as ColaboradorId,coalesce(p.nome,'(colaborador inativo)') as Colaborador,x.tipo,x.inicio,x.fim,x.salario,x.carga_horaria_semanal::integer as CargaHorariaSemanal,x.status from plantaopro.adm_contratos_trabalho x left join plantaopro.adm_colaboradores p on p.id=x.colaborador_id where {filtro} order by x.inicio desc,x.id desc limit @tamanho offset @offset", parms, cancellationToken: ct))).AsList();
        return new Administrativo360PaginadoDto<ContratoTrabalhoDto>(itens, p, t, total, TotalPaginas(total, t));
    }
    public async Task<ContratoTrabalhoDetalheDto> ContratoDetalheAsync(Guid id,CancellationToken ct)
    {
        await using var cn=Connection();
        var dto=await cn.QueryFirstOrDefaultAsync<ContratoTrabalhoDetalheDto>(new CommandDefinition(@"select x.id,x.colaborador_id as ColaboradorId,coalesce(p.nome,'(colaborador inativo)') as Colaborador,coalesce(p.matricula,'—') as Matricula,x.tipo,x.inicio,x.fim,x.salario,x.carga_horaria_semanal::integer as CargaHorariaSemanal,x.status,x.reg_date as CriadoEm from plantaopro.adm_contratos_trabalho x left join plantaopro.adm_colaboradores p on p.id=x.colaborador_id where x.id=@id and x.tenant_id=@tenant and x.reg_status='A'",new{id,tenant=Tenant()},cancellationToken:ct));
        if(dto is null) throw new Administrativo360BusinessException("Contrato não encontrado ou pertencente a outra organização.");
        return dto;
    }

    // ============================================================
    // DEPARTAMENTOS
    // ============================================================
    public async Task<DepartamentoDto> CriarDepartamentoAsync(DepartamentoRequest r,CancellationToken ct)
    {
        await using var cn=Connection();
        try
        {
            var row=await cn.QueryFirstOrDefaultAsync<DepartamentoDto>(new CommandDefinition(@"insert into plantaopro.adm_departamentos(id,tenant_id,codigo,nome,created_by) values(gen_random_uuid(),@tenant,upper(trim(@Codigo)),trim(@Nome),@user) returning id,codigo,nome,ativo",new{tenant=Tenant(),r.Codigo,r.Nome,user=currentUser.UserId},cancellationToken:ct));
            return row ?? throw new Administrativo360BusinessException("Não foi possível concluir o cadastro do departamento. Tente novamente.");
        }
        catch(PostgresException ex) when(ex.SqlState=="23505") { throw new Administrativo360BusinessException("Já existe um departamento ativo com este código."); }
    }
    public async Task<DepartamentoDto> AtualizarDepartamentoAsync(Guid id,DepartamentoRequest r,CancellationToken ct)
    {
        await using var cn=Connection();
        try
        {
            var row=await cn.QueryFirstOrDefaultAsync<DepartamentoDto>(new CommandDefinition(@"update plantaopro.adm_departamentos set codigo=upper(trim(@Codigo)),nome=trim(@Nome),reg_update=now() where id=@id and tenant_id=@tenant and reg_status='A' returning id,codigo,nome,ativo",new{id,tenant=Tenant(),r.Codigo,r.Nome},cancellationToken:ct));
            return row ?? throw new Administrativo360BusinessException("Departamento não encontrado ou já inativo.");
        }
        catch(PostgresException ex) when(ex.SqlState=="23505") { throw new Administrativo360BusinessException("Já existe um departamento ativo com este código."); }
    }
    public async Task AlterarAtivacaoDepartamentoAsync(Guid id,bool ativar,CancellationToken ct) =>
        await ExecutarStatusAsync(new CommandDefinition(
            ativar
                ? "update plantaopro.adm_departamentos set reg_status='A',ativo=true,reg_update=now() where id=@id and tenant_id=@tenant and reg_status='I'"
                : "update plantaopro.adm_departamentos set reg_status='I',ativo=false,reg_update=now() where id=@id and tenant_id=@tenant and reg_status='A'",
            new{id,tenant=Tenant()},cancellationToken:ct),
            ativar?"Departamento não encontrado ou já ativo.":"Departamento não encontrado ou já inativo.",ct);

    // ============================================================
    // CARGOS
    // ============================================================
    public async Task<CargoDto> CriarCargoAsync(CargoRequest r,CancellationToken ct)
    {
        if(r.DepartamentoId.HasValue && !await DepartamentoElegivelAsync(r.DepartamentoId.Value))
            throw new Administrativo360BusinessException("Departamento inexistente, inativo ou pertencente a outra organização.");
        await using var cn=Connection();
        try
        {
            var row=await cn.QueryFirstOrDefaultAsync<CargoDto>(new CommandDefinition(@"with novo as (insert into plantaopro.adm_cargos(id,tenant_id,codigo,nome,departamento_id,created_by) select gen_random_uuid(),@tenant,upper(trim(@Codigo)),trim(@Nome),@DepartamentoId,@user where @DepartamentoId is null or exists(select 1 from plantaopro.adm_departamentos where id=@DepartamentoId and tenant_id=@tenant and reg_status='A' and ativo) returning *) select n.id,n.codigo,n.nome,n.departamento_id as DepartamentoId,d.nome as Departamento,n.ativo from novo n left join plantaopro.adm_departamentos d on d.id=n.departamento_id",new{tenant=Tenant(),r.Codigo,r.Nome,r.DepartamentoId,user=currentUser.UserId},cancellationToken:ct));
            return row ?? throw new Administrativo360BusinessException("Não foi possível concluir o cadastro do cargo. Tente novamente.");
        }
        catch(PostgresException ex) when(ex.SqlState=="23505") { throw new Administrativo360BusinessException("Já existe um cargo ativo com este código."); }
    }
    public async Task<CargoDto> AtualizarCargoAsync(Guid id,CargoRequest r,CancellationToken ct)
    {
        if(r.DepartamentoId.HasValue && !await DepartamentoElegivelAsync(r.DepartamentoId.Value))
            throw new Administrativo360BusinessException("Departamento inexistente, inativo ou pertencente a outra organização.");
        await using var cn=Connection();
        try
        {
            var row=await cn.QueryFirstOrDefaultAsync<CargoDto>(new CommandDefinition(@"update plantaopro.adm_cargos set codigo=upper(trim(@Codigo)),nome=trim(@Nome),departamento_id=@DepartamentoId,reg_update=now() where id=@id and tenant_id=@tenant and reg_status='A' returning id,codigo,nome,departamento_id as DepartamentoId,ativo",new{id,tenant=Tenant(),r.Codigo,r.Nome,r.DepartamentoId},cancellationToken:ct));
            return row ?? throw new Administrativo360BusinessException("Cargo não encontrado ou já inativo.");
        }
        catch(PostgresException ex) when(ex.SqlState=="23505") { throw new Administrativo360BusinessException("Já existe um cargo ativo com este código."); }
    }
    public async Task AlterarAtivacaoCargoAsync(Guid id,bool ativar,CancellationToken ct) =>
        await ExecutarStatusAsync(new CommandDefinition(
            ativar
                ? "update plantaopro.adm_cargos set reg_status='A',ativo=true,reg_update=now() where id=@id and tenant_id=@tenant and reg_status='I'"
                : "update plantaopro.adm_cargos set reg_status='I',ativo=false,reg_update=now() where id=@id and tenant_id=@tenant and reg_status='A'",
            new{id,tenant=Tenant()},cancellationToken:ct),
            ativar?"Cargo não encontrado ou já ativo.":"Cargo não encontrado ou já inativo.",ct);

    // ============================================================
    // COLABORADORES
    // ============================================================
    public async Task<ColaboradorDto> CriarColaboradorAsync(ColaboradorRequest r,CancellationToken ct)
    {
        if(!await CargoElegivelAsync(r.CargoId))
            throw new Administrativo360BusinessException("Cargo inexistente ou inativo. Cadastre ou ative o cargo antes de vincular o colaborador.");
        await using var cn=Connection();
        try
        {
            var row=await cn.QueryFirstOrDefaultAsync<ColaboradorDto>(new CommandDefinition(@"with novo as (insert into plantaopro.adm_colaboradores(id,tenant_id,matricula,nome,cpf,email,cargo_id,created_by) select gen_random_uuid(),@tenant,upper(trim(@Matricula)),trim(@Nome),@Cpf,lower(trim(@Email)),@CargoId,@user where exists(select 1 from plantaopro.adm_cargos where id=@CargoId and tenant_id=@tenant and ativo and reg_status='A') returning *) select n.id,n.matricula,n.nome,n.cpf,n.email,n.cargo_id as CargoId,c.nome as Cargo,c.departamento_id as DepartamentoId,d.nome as Departamento,n.status from novo n join plantaopro.adm_cargos c on c.id=n.cargo_id left join plantaopro.adm_departamentos d on d.id=c.departamento_id",new{tenant=Tenant(),r.Matricula,r.Nome,r.Cpf,r.Email,r.CargoId,user=currentUser.UserId},cancellationToken:ct));
            return row ?? throw new Administrativo360BusinessException("Não foi possível concluir o cadastro do colaborador. Tente novamente.");
        }
        catch(PostgresException ex) when(ex.SqlState=="23505")
        {
            throw new Administrativo360BusinessException(ex.ConstraintName?.Contains("cpf",System.StringComparison.OrdinalIgnoreCase)==true
                ? "Já existe colaborador ativo com este CPF."
                : "Já existe colaborador ativo com esta matrícula.");
        }
    }
    public async Task<ColaboradorDto> AtualizarColaboradorAsync(Guid id,ColaboradorUpdateRequest r,CancellationToken ct)
    {
        await using var cn=Connection();
        var row=await cn.QueryFirstOrDefaultAsync<ColaboradorDto>(new CommandDefinition(@"with atual as (update plantaopro.adm_colaboradores set nome=trim(@Nome),email=lower(trim(@Email)),reg_update=now() where id=@id and tenant_id=@tenant and reg_status='A' returning *) select n.id,n.matricula,n.nome,n.cpf,n.email,n.cargo_id as CargoId,c.nome as Cargo,c.departamento_id as DepartamentoId,d.nome as Departamento,n.status from atual n join plantaopro.adm_cargos c on c.id=n.cargo_id left join plantaopro.adm_departamentos d on d.id=c.departamento_id",new{id,tenant=Tenant(),r.Nome,r.Email},cancellationToken:ct));
        return row ?? throw new Administrativo360BusinessException("Colaborador não encontrado ou já inativo.");
    }
    public async Task AlterarStatusColaboradorAsync(Guid id,string status,CancellationToken ct)
    {
        var alvo=status.Trim().ToUpperInvariant();
        if(!StatusColaboradores.Contains(alvo)) throw new Administrativo360BusinessException("Status inválido para o colaborador.");
        await using var cn=Connection();
        var afeto=await cn.ExecuteAsync(new CommandDefinition("update plantaopro.adm_colaboradores set status=@status,reg_update=now() where id=@id and tenant_id=@tenant and reg_status='A'",new{id,status=alvo,tenant=Tenant()},cancellationToken:ct));
        if(afeto==0) throw new Administrativo360BusinessException("Colaborador não encontrado ou pertencente a outra organização.");
    }

    // ============================================================
    // CONTRATOS DE TRABALHO
    // ============================================================
    public async Task<ContratoTrabalhoDto> ContratarAsync(ContratoTrabalhoRequest r,CancellationToken ct)
    {
        var tipo=r.Tipo.Trim().ToUpperInvariant();
        if(!TiposContrato.Contains(tipo)) throw new Administrativo360BusinessException("Tipo de contrato inválido.");
        if(r.Inicio==default) throw new Administrativo360BusinessException("Informe a data de início do contrato.");
        if(r.Fim.HasValue&&r.Fim<r.Inicio) throw new Administrativo360BusinessException("Data final anterior ao início do contrato.");
        if(r.Salario<=0||r.Salario>999999999m) throw new Administrativo360BusinessException("Salário inválido.");
        if(r.CargaHorariaSemanal<1||r.CargaHorariaSemanal>60) throw new Administrativo360BusinessException("Carga horária semanal deve estar entre 1 e 60 horas.");
        if(r.ColaboradorId==default) throw new Administrativo360BusinessException("Selecione o colaborador.");

        await using var cn=Connection();
        await cn.OpenAsync(ct);
        await using var tx=await cn.BeginTransactionAsync(ct);
        var tenant=Tenant();
        try
        {
            // Sinaliza contratações concorrentes do mesmo colaborador dentro da transação:
            // a consulta prévia de sobreposição sozinha não garante exclusividade.
            await cn.ExecuteAsync(new CommandDefinition("select pg_advisory_xact_lock(hashtext('adm360_contrato_'||@colaboradorId::text))",new{colaboradorId=r.ColaboradorId},tx,cancellationToken:ct));
            var existe=await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.adm_colaboradores where id=@id and tenant_id=@tenant and status='ATIVO' and reg_status='A')",new{id=r.ColaboradorId,tenant},tx,cancellationToken:ct));
            if(!existe) throw new Administrativo360BusinessException("Colaborador não pertence à organização, está inativo ou foi desligado.");
            var sobreposto=await cn.ExecuteScalarAsync<bool>(new CommandDefinition($@"select exists(select 1 from plantaopro.adm_contratos_trabalho where tenant_id=@tenant and colaborador_id=@id and reg_status='A' and {ContratoBloqueioSobreposicao} and daterange(inicio,coalesce(fim,'infinity'::date),'[]') && daterange(@inicio,coalesce(@fim,'infinity'::date),'[]'))",new{tenant,id=r.ColaboradorId,inicio=r.Inicio,fim=r.Fim},tx,cancellationToken:ct));
            if(sobreposto) throw new Administrativo360BusinessException("Já existe contrato vigente com período sobreposto para este colaborador.");
            var dto=await cn.QuerySingleAsync<ContratoTrabalhoDto>(new CommandDefinition(@"with novo as (insert into plantaopro.adm_contratos_trabalho(id,tenant_id,colaborador_id,tipo,inicio,fim,salario,carga_horaria_semanal,created_by) values(gen_random_uuid(),@tenant,@ColaboradorId,@Tipo,@Inicio,@Fim,@Salario,@CargaHorariaSemanal,@user) returning *) select n.id,n.colaborador_id as ColaboradorId,coalesce(p.nome,'(colaborador inativo)') as Colaborador,n.tipo,n.inicio,n.fim,n.salario,n.carga_horaria_semanal::integer as CargaHorariaSemanal,n.status from novo n left join plantaopro.adm_colaboradores p on p.id=n.colaborador_id",new{tenant,ColaboradorId=r.ColaboradorId,Tipo=tipo,r.Inicio,r.Fim,r.Salario,r.CargaHorariaSemanal,user=currentUser.UserId},tx,cancellationToken:ct));
            await tx.CommitAsync(ct);
            return dto;
        }
        catch(Administrativo360BusinessException){ await tx.RollbackAsync(ct); throw; }
        catch(Exception){ await tx.RollbackAsync(ct); throw; }
    }
    public async Task EncerrarContratoAsync(Guid id,DateOnly? fim,CancellationToken ct)
    {
        if(fim.HasValue)
        {
            var inicio=await BuscarInicioDoContratoAsync(id,fim,ct);
            if(fim<inicio) throw new Administrativo360BusinessException("Data final anterior ao início do contrato.");
        }
        await ExecutarTransicaoContratoAsync(id,"ENCERRADO",fim,ct);
    }
    public async Task CancelarContratoAsync(Guid id,CancellationToken ct) =>
        await ExecutarTransicaoContratoAsync(id,"CANCELADO",null,ct);

    // ============================================================
    // SUPORTE
    // ============================================================
    private static (int Pagina, int Tamanho) NormalizarPagina(int pagina, int tamanho) => (Math.Max(1, pagina), Math.Clamp(tamanho <= 0 ? 50 : tamanho, 1, 100));
    private static int TotalPaginas(long total, int tamanho) => tamanho <= 0 ? 0 : (int)Math.Ceiling(total / (double)tamanho);
    private static string? TermoBusca(string? busca) => string.IsNullOrWhiteSpace(busca) ? null : $"%{busca.Trim()}%";
    private DynamicParameters ParametrosPagina(string? termo, int tamanho, int pagina)
    {
        var parms = new DynamicParameters();
        parms.Add("tenant", Tenant());
        if (termo != null) parms.Add("busca", termo);
        parms.Add("tamanho", tamanho);
        parms.Add("offset", (long)(pagina - 1) * tamanho);
        return parms;
    }

    private async Task<bool> DepartamentoElegivelAsync(Guid departamentoId)
    {
        await using var cn=Connection();
        return await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.adm_departamentos where id=@id and tenant_id=@tenant and reg_status='A' and ativo)",new{id=departamentoId,tenant=Tenant()}));
    }
    private async Task<bool> CargoElegivelAsync(Guid cargoId)
    {
        await using var cn=Connection();
        return await cn.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from plantaopro.adm_cargos where id=@id and tenant_id=@tenant and reg_status='A' and ativo)",new{id=cargoId,tenant=Tenant()}));
    }
    private async Task<DateOnly> BuscarInicioDoContratoAsync(Guid id,DateOnly? fim,CancellationToken ct)
    {
        await using var cn=Connection();
        var inicio=await cn.ExecuteScalarAsync<DateOnly?>(new CommandDefinition("select inicio from plantaopro.adm_contratos_trabalho where id=@id and tenant_id=@tenant and reg_status='A' and status='VIGENTE'",new{id,tenant=Tenant()},cancellationToken:ct));
        if(inicio is null) throw new Administrativo360BusinessException("Somente contratos vigentes podem ser encerrados.");
        return inicio.Value;
    }
    private async Task ExecutarTransicaoContratoAsync(Guid id,string status,DateOnly? fim,CancellationToken ct)
    {
        await using var cn=Connection();
        var sql=status=="ENCERRADO"
            ? "update plantaopro.adm_contratos_trabalho set status='ENCERRADO',fim=coalesce(@Fim,date_trunc('day',now())::date),reg_update=now() where id=@id and tenant_id=@tenant and reg_status='A' and status='VIGENTE'"
            : "update plantaopro.adm_contratos_trabalho set status='CANCELADO',reg_update=now() where id=@id and tenant_id=@tenant and reg_status='A' and status='VIGENTE'";
        var afeto=await cn.ExecuteAsync(new CommandDefinition(sql,new{id,fim,tenant=Tenant()},cancellationToken:ct));
        if(afeto==0) throw new Administrativo360BusinessException("Somente contratos vigentes podem ser encerrados ou cancelados.");
    }
    private async Task ExecutarStatusAsync(CommandDefinition comando,string mensagemZeroLinhas,CancellationToken ct)
    {
        await using var cn=Connection();
        var afeto=await cn.ExecuteAsync(comando);
        if(afeto==0) throw new Administrativo360BusinessException(mensagemZeroLinhas);
    }
}
