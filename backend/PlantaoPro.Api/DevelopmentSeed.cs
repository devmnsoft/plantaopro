using Dapper;
using Npgsql;

namespace PlantaoPro.Api.Data;

public static class DevelopmentSeed
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevelopmentSeed");
        if (!env.IsDevelopment()) return;

        await using var cn = new NpgsqlConnection(cfg.GetConnectionString("Default"));
        await cn.OpenAsync();
        await cn.ExecuteAsync("create schema if not exists plantaopro;");

        var perfis = new[] { "ADMINISTRADOR", "COORDENADOR", "FINANCEIRO" };
        foreach (var nome in perfis)
            await cn.ExecuteAsync("insert into plantaopro.perfis(id,nome,descricao,reg_status,reg_date) select gen_random_uuid(),@nome,@desc,'A',now() where not exists(select 1 from plantaopro.perfis where upper(nome)=upper(@nome))", new { nome, desc = $"Perfil {nome.ToLowerInvariant()}" });

        var admin = await cn.QueryFirstOrDefaultAsync<(Guid Id, string? SenhaHash)>("select id,senha_hash from plantaopro.usuarios where lower(email)=lower(@Email) limit 1", new { Email = "admin@plantaopro.com" });
        var usuarioId = admin.Id == Guid.Empty ? Guid.NewGuid() : admin.Id;
        if (admin.Id == Guid.Empty)
            await cn.ExecuteAsync("insert into plantaopro.usuarios(id,nome,email,senha_hash,reg_status,reg_date) values(@id,'Administrador Geral','admin@plantaopro.com',@hash,'A',now())", new { id = usuarioId, hash = BCrypt.Net.BCrypt.HashPassword("123456") });
        else if (string.IsNullOrWhiteSpace(admin.SenhaHash))
            await cn.ExecuteAsync("update plantaopro.usuarios set senha_hash=@hash,reg_update=now() where id=@id", new { id = usuarioId, hash = BCrypt.Net.BCrypt.HashPassword("123456") });

        var adminPerfilId = await cn.ExecuteScalarAsync<Guid>("select id from plantaopro.perfis where upper(nome)='ADMINISTRADOR' limit 1");
        await cn.ExecuteAsync("insert into plantaopro.usuarios_perfis(id,usuario_id,perfil_id,reg_status,reg_date) select gen_random_uuid(),@u,@p,'A',now() where not exists(select 1 from plantaopro.usuarios_perfis where usuario_id=@u and perfil_id=@p and reg_status='A')", new { u = usuarioId, p = adminPerfilId });

        string[] especialidades = new[] { "Clínica Médica", "Cardiologia", "Ortopedia", "Pediatria", "Ginecologia", "Anestesiologia", "Neurologia", "Cirurgia Geral", "Dermatologia", "Psiquiatria" };
        foreach (var e in especialidades)
            await cn.ExecuteAsync("insert into plantaopro.especialidades(id,nome,descricao,reg_status,reg_date) select gen_random_uuid(),@n,@d,'A',now() where not exists(select 1 from plantaopro.especialidades where nome=@n)", new { n = e, d = $"Especialidade de {e}" });

        for (var i = 1; i <= 5; i++)
            await cn.ExecuteAsync("insert into plantaopro.hospitais(id,razao_social,nome_fantasia,cnpj,telefone,email,endereco,cidade,estado,responsavel,reg_status,reg_date) select gen_random_uuid(),@r,@f,@c,@t,@e,@en,@ci,@uf,@resp,'A',now() where not exists(select 1 from plantaopro.hospitais where cnpj=@c)", new { r = $"Hospital {i} Ltda", f = $"Hospital Vida {i}", c = $"00000000000{i:00}", t = "(11) 3000-0000", e = $"contato{i}@hospitalvida.com", en = $"Rua Central, {100 + i}", ci = "São Paulo", uf = "SP", resp = $"Gestor {i}" });

        for (var i = 1; i <= 20; i++)
            await cn.ExecuteAsync("insert into plantaopro.medicos(id,nome,cpf,crm,uf_crm,email,telefone,cidade,estado,especialidade_id,reg_status,reg_date) select gen_random_uuid(),@n,@cpf,@crm,'SP',@e,@t,'São Paulo','SP',(select id from plantaopro.especialidades order by nome offset @off limit 1),'A',now() where not exists(select 1 from plantaopro.medicos where cpf=@cpf)", new { n = $"Dr(a). Profissional {i}", cpf = $"999999999{i:00}", crm = $"CRM{i:00000}", e = $"medico{i}@plantaopro.com", t = "(11) 98888-8888", off = (i - 1) % 10 });

        await cn.ExecuteAsync(@"insert into plantaopro.plantoes(id,hospital_id,especialidade_id,data_inicio,data_fim,valor,vagas,vagas_disponiveis,tipo,status,observacoes,reg_status,reg_date)
select gen_random_uuid(),h.id,e.id,now()+((x.i||' day')::interval),now()+((x.i||' day')::interval)+interval '12 hour',800 + x.i*25,2,2,'presencial',case when x.i%4=0 then 'confirmado' else 'aberto' end,'Cobertura assistencial de rotina','A',now()
from generate_series(1,30) x(i)
join lateral (select id from plantaopro.hospitais order by nome_fantasia offset ((x.i-1)%5) limit 1) h on true
join lateral (select id from plantaopro.especialidades order by nome offset ((x.i-1)%10) limit 1) e on true
where (select count(1) from plantaopro.plantoes where reg_status='A') < 30;");

        await cn.ExecuteAsync(@"insert into plantaopro.escalas(id,plantao_id,medico_id,status,justificativa,reg_status,reg_date)
select gen_random_uuid(),p.id,m.id,case when row_number() over() % 3 = 0 then 'confirmado' else 'solicitado' end,'Alocação conforme disponibilidade médica','A',now()
from (select id from plantaopro.plantoes order by data_inicio limit 20) p
join lateral (select id from plantaopro.medicos order by nome offset floor(random()*20) limit 1) m on true
where (select count(1) from plantaopro.escalas where reg_status='A') < 20;");

        await cn.ExecuteAsync(@"insert into plantaopro.pagamentos(id,escala_id,medico_id,plantao_id,valor_previsto,valor_pago,status,data_prevista,data_pagamento,forma_pagamento,observacoes,reg_status,reg_date)
select gen_random_uuid(),e.id,e.medico_id,e.plantao_id,1000,case when row_number() over() % 2 = 0 then 1000 else null end,case when row_number() over() % 2 = 0 then 'pago' else 'pendente' end,current_date + 7,case when row_number() over() % 2 = 0 then current_date else null end,case when row_number() over() % 2 = 0 then 'PIX' else null end,'Pagamento padrão contratual','A',now()
from (select id,medico_id,plantao_id from plantaopro.escalas order by reg_date limit 10) e
where (select count(1) from plantaopro.pagamentos where reg_status='A') < 10;");

        await cn.ExecuteAsync(@"insert into plantaopro.notificacoes(id,usuario_id,titulo,mensagem,tipo,lida,reg_status,reg_date)
select gen_random_uuid(),@uid,'Atualização de escala','Sua agenda foi atualizada pela coordenação.','escala',false,'A',now() - ((x.i||' hour')::interval)
from generate_series(1,10) x(i)
where (select count(1) from plantaopro.notificacoes where usuario_id=@uid and reg_status='A') < 10;", new { uid = usuarioId });

        logger.LogInformation("Massa de desenvolvimento atualizada para demonstração comercial.");
    }
}
