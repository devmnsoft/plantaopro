-- R5-E13/P2: padronizacao do escopo canonico do nucleo clinico em tenant_id.
-- Contexto (divida medida na evidencia D11): o kernel Saude360 (Saude360ClinicalService) gravava
-- somente cliente_id e deixava tenant_id NULL em 100% das linhas clinicas criadas por rota; dois
-- writers pontuais (favoritar CID e usar modelo de prescricao) ainda escreviam a chave de escopo
-- (cliente_id) dentro da coluna tenant_id, contaminando a semantica. A rota de escrita agora
-- resolve o tenant de origem pelo mapeamento persistido (clientes.tenant_id, com fallback
-- tenants.cliente_id ou o proprio tenant quando o escopo ja e um tenant) e esta migracao retroa a
-- MESMA regra as linhas existentes.
-- Regra de backfill: tenant_id := clientes.tenant_id do cliente do registro, somente onde o valor
-- atual e NULL ou esta contaminado (= cliente_id do proprio registro). Valores distintos
-- intencionais nao sao tocados. Tabelas sem a coluna tenant_id sao ignoradas pelo guard.
-- Aditiva e idempotente: reexecucao nao altera nada (a condicao so casa com NULL/contaminado).
do $$
declare
    t text;
begin
    foreach t in array array[
        'pacientes','paciente_historico','painel_chamada_fila','painel_chamada_historico',
        'agendamentos','triagens','consultas','consulta_historico','cid_tabela','cid_favoritos',
        'cid_uso_historico','prescricoes','prescricao_modelos','prescricao_historico',
        'clinica_contas_receber','clinica_recebimentos','clinica_caixa','convenios',
        'convenio_planos','convenio_autorizacoes','planos_saude','plano_saude_pacientes',
        'convenio_glosas','repasses_medicos_clinicos','clinica_unidades_atendimento'
    ] loop
        if exists (
            select 1 from information_schema.columns
            where table_schema = 'plantaopro' and table_name = t and column_name = 'tenant_id'
        ) then
            execute format(
                'update plantaopro.%I x set tenant_id = m.tenant_id
                 from plantaopro.clientes m
                 where m.id = x.cliente_id and m.tenant_id is not null
                   and (x.tenant_id is null or x.tenant_id = x.cliente_id)', t);
        end if;
    end loop;
end $$;

insert into plantaopro.schema_migrations(id,script_path,checksum,applied_at)
select 'v2336','database/migrations/2026_10_v2336_e13_p2_backfill_tenant_id_nucleo_clinico.sql','runtime-managed',now()
where not exists(select 1 from plantaopro.schema_migrations where id='v2336');
