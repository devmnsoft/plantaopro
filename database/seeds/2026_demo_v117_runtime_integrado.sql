DO $$
DECLARE
    v_cliente uuid := '00000000-0000-4000-8000-000000000113';
    v_tenant uuid := '00000000-0000-4000-8000-000000000113';
BEGIN
    INSERT INTO plantaopro.v116_notificacoes_operacionais (id, cliente_id, tenant_id, descricao, status_operacional, prioridade, perfil_responsavel, created_by)
    VALUES ('11700000-0000-0000-0000-000000000001', v_cliente, v_tenant, 'Pendência operacional demo v1.17 sem dado real', 'NAO_LIDA', 'ALTA', 'FINANCEIRO', 'seed-v117')
    ON CONFLICT (id) DO NOTHING;
    INSERT INTO plantaopro.v116_timelines (id, cliente_id, tenant_id, descricao, status_operacional, entidade, entidade_id, created_by)
    VALUES ('11700000-0000-0000-0000-000000000002', v_cliente, v_tenant, 'Timeline persistente demo v1.17', 'REGISTRADO', 'AUTORIZACAO', '11600000-0000-0000-0000-000000000001', 'seed-v117')
    ON CONFLICT (id) DO NOTHING;
END $$;
