-- Massa demo idempotente para jornada Saúde 360 e API Keys.
-- Não contém API Key em texto puro; o valor abaixo é apenas hash demo não reversível.
-- Shim legado: colunas da tabela cid alinhadas ao shape atual (descricao movido para
-- jsonb dados); api_chaves não existe no schema atual, inserção preservada sob guarda.

do $$
begin
    if to_regclass('plantaopro.cid') is not null then
        insert into plantaopro.cid(codigo, nome, status, dados)
        select 'Z00.0', 'Exame médico geral demo', 'ATIVO', '{"descricao":"Exame médico geral demo"}'::jsonb
        where not exists (select 1 from plantaopro.cid where codigo = 'Z00.0');
    end if;

    if to_regclass('plantaopro.api_chaves') is not null then
        insert into plantaopro.api_chaves (id, tenant_id, cliente_id, nome, api_key_hash, escopos, status, reg_date, reg_status)
        select gen_random_uuid(), c.tenant_id, c.id, 'Demo integração revogada', repeat('0', 64), 'pacientes:read,agendamentos:read', 'REVOGADA', now(), 'A'
        from plantaopro.clientes c
        where not exists (select 1 from plantaopro.api_chaves a where a.cliente_id = c.id and a.nome = 'Demo integração revogada')
        limit 1;
    end if;
end $$;
