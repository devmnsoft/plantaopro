-- Shim legado: as tabelas v116_* não existem no schema atual. Inserções preservadas
-- sob guarda to_regclass para reativar automaticamente se o módulo voltar.
-- Formato validado nesta homologacao (EDB Postgres 18): cada tabela em um bloco DO
-- proprio e UUID de entidade como variavel declarada. Uma coercao de literal uuid
-- em posicao tardia dentro da mesma instrucao falha de forma intermitente neste
-- ambiente (anomalia documentada em evidencias-a360/j11, probes 49-53); o formato
-- acima nao reproduz a falha nas janelas testadas.
DO $$
DECLARE
    v_cliente uuid := '00000000-0000-4000-8000-000000000113';
    v_tenant uuid := '00000000-0000-4000-8000-000000000113';
BEGIN
    IF to_regclass('plantaopro.v116_notificacoes_operacionais') IS NOT NULL THEN
        INSERT INTO plantaopro.v116_notificacoes_operacionais (id, cliente_id, tenant_id, descricao, status_operacional, prioridade, perfil_responsavel, created_by)
        VALUES ('11700000-0000-0000-0000-000000000001', v_cliente, v_tenant, 'Pendência operacional demo v1.17 sem dado real', 'NAO_LIDA', 'ALTA', 'FINANCEIRO', 'seed-v117')
        ON CONFLICT (id) DO NOTHING;
    END IF;
END $$;
DO $$
DECLARE
    v_cliente uuid := '00000000-0000-4000-8000-000000000113';
    v_tenant uuid := '00000000-0000-4000-8000-000000000113';
    v_entidade uuid := '11600000-0000-0000-0000-000000000001';
BEGIN
    IF to_regclass('plantaopro.v116_timelines') IS NOT NULL THEN
        INSERT INTO plantaopro.v116_timelines (id, cliente_id, tenant_id, descricao, status_operacional, entidade, entidade_id, created_by)
        VALUES ('11700000-0000-0000-0000-000000000002', v_cliente, v_tenant, 'Timeline persistente demo v1.17', 'REGISTRADO', 'AUTORIZACAO', v_entidade, 'seed-v117')
        ON CONFLICT (id) DO NOTHING;
    END IF;
END $$;
