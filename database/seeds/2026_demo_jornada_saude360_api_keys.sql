-- Massa demo idempotente para jornada Saúde 360 e API Keys.
-- Não contém API Key em texto puro; o valor abaixo é apenas hash demo não reversível.

insert into plantaopro.cid (id, codigo, descricao, reg_date, reg_status)
select gen_random_uuid(), 'Z00.0', 'Exame médico geral demo', now(), 'A'
where exists (select 1 from information_schema.tables where table_schema = 'plantaopro' and table_name = 'cid')
  and not exists (select 1 from plantaopro.cid where codigo = 'Z00.0');

insert into plantaopro.api_chaves (id, tenant_id, cliente_id, nome, api_key_hash, escopos, status, reg_date, reg_status)
select gen_random_uuid(), c.tenant_id, c.id, 'Demo integração revogada', repeat('0', 64), 'pacientes:read,agendamentos:read', 'REVOGADA', now(), 'A'
from plantaopro.clientes c
where not exists (select 1 from plantaopro.api_chaves a where a.cliente_id = c.id and a.nome = 'Demo integração revogada')
limit 1;
