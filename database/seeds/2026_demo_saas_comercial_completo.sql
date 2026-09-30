-- Shim legado: saas_clientes e suporte_sla_politicas não existem no schema atual.
-- Inserções originais preservadas sob guarda to_regclass para reativar se as tabelas voltarem.

do $$
begin
    if to_regclass('plantaopro.saas_clientes') is not null then
        -- j11 WP8-B: idempotencia real por email. A PK e gen_random_uuid(), entao o
        -- "on conflict do nothing" sem alvo nunca disparava (nao havia colisão de chave)
        -- e cada execucao duplicava as 3 linhas (drift saas_clientes +3 por run).
        insert into plantaopro.saas_clientes(nome_fantasia,razao_social,cnpj,email,telefone,responsavel,segmento,cidade,estado,status)
        select v.nome_fantasia, v.razao_social, v.cnpj, v.email, v.telefone, v.responsavel, v.segmento, v.cidade, v.estado, v.status
        from (values
            ('Clínica Aurora Demo','Clinica Aurora Demo Ltda','00.000.000/0000-00','aurora.demo@plantaopro.local','(00) 0000-0000','Ana Demo','Clínica','São Paulo','SP','ATIVO'),
            ('Hospital Horizonte Demo','Hospital Horizonte Demo S.A.','11.111.111/1111-11','horizonte.demo@plantaopro.local','(00) 1111-1111','Bruno Demo','Hospital','Rio de Janeiro','RJ','ATIVO'),
            ('Rede Vida Demo','Rede Vida Demo Ltda','22.222.222/2222-22','vida.demo@plantaopro.local','(00) 2222-2222','Carla Demo','Rede','Belo Horizonte','MG','ATIVO')
        ) v(nome_fantasia,razao_social,cnpj,email,telefone,responsavel,segmento,cidade,estado,status)
        where not exists (select 1 from plantaopro.saas_clientes c where c.email = v.email);
    end if;

    if to_regclass('plantaopro.suporte_sla_politicas') is not null then
        insert into plantaopro.suporte_sla_politicas(prioridade,primeira_resposta_horas,resolucao_horas) values ('Baixa',24,72),('Média',8,48),('Alta',4,24),('Crítica',1,8) on conflict (prioridade) do update set primeira_resposta_horas=excluded.primeira_resposta_horas, resolucao_horas=excluded.resolucao_horas;
    end if;
end $$;
