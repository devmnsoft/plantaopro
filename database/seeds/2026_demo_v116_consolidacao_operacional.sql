CREATE SCHEMA IF NOT EXISTS plantaopro;
DO $$
DECLARE
    v_tenant uuid := '11111111-1111-1111-1111-111111111111';
    v_cliente uuid := '22222222-2222-2222-2222-222222222222';
BEGIN
    INSERT INTO plantaopro.v116_convenio_autorizacoes (id, cliente_id, tenant_id, descricao, status_operacional, created_by)
    VALUES ('11600000-0000-0000-0000-000000000001', v_cliente, v_tenant, 'Autorização demo pendente sem dados reais', 'PENDENTE', 'seed-v116'),
           ('11600000-0000-0000-0000-000000000002', v_cliente, v_tenant, 'Autorização demo aprovada sem dados reais', 'APROVADA', 'seed-v116')
    ON CONFLICT (id) DO NOTHING;
    INSERT INTO plantaopro.v116_convenio_guias (id, cliente_id, tenant_id, descricao, status_operacional, created_by) VALUES ('11600000-0000-0000-0000-000000000003', v_cliente, v_tenant, 'Guia demo de atendimento', 'GERADA', 'seed-v116') ON CONFLICT (id) DO NOTHING;
    INSERT INTO plantaopro.v116_faturamento_lotes (id, cliente_id, tenant_id, descricao, status_operacional, valor, created_by) VALUES ('11600000-0000-0000-0000-000000000004', v_cliente, v_tenant, 'Lote demo de faturamento', 'ABERTO', 360.00, 'seed-v116') ON CONFLICT (id) DO NOTHING;
    INSERT INTO plantaopro.v116_caixas (id, cliente_id, tenant_id, descricao, status_operacional, created_by) VALUES ('11600000-0000-0000-0000-000000000005', v_cliente, v_tenant, 'Caixa demo aberto', 'ABERTO', 'seed-v116') ON CONFLICT (id) DO NOTHING;
    INSERT INTO plantaopro.v116_caixa_movimentos (id, cliente_id, tenant_id, descricao, status_operacional, valor, created_by) VALUES ('11600000-0000-0000-0000-000000000006', v_cliente, v_tenant, 'Movimento demo de recebimento', 'RECEBIDO', 120.00, 'seed-v116') ON CONFLICT (id) DO NOTHING;
    INSERT INTO plantaopro.v116_recebimentos_parciais (id, cliente_id, tenant_id, descricao, status_operacional, valor, created_by) VALUES ('11600000-0000-0000-0000-000000000007', v_cliente, v_tenant, 'Recebimento parcial demo', 'PARCIAL', 80.00, 'seed-v116') ON CONFLICT (id) DO NOTHING;
    INSERT INTO plantaopro.v116_timelines (id, cliente_id, tenant_id, descricao, status_operacional, created_by) VALUES ('11600000-0000-0000-0000-000000000008', v_cliente, v_tenant, 'Timeline demo atendimento', 'REGISTRADO', 'seed-v116'), ('11600000-0000-0000-0000-000000000009', v_cliente, v_tenant, 'Timeline demo financeira', 'REGISTRADO', 'seed-v116') ON CONFLICT (id) DO NOTHING;
    INSERT INTO plantaopro.v116_notificacoes_operacionais (id, cliente_id, tenant_id, descricao, status_operacional, created_by) VALUES ('11600000-0000-0000-0000-000000000010', v_cliente, v_tenant, 'Notificação operacional demo', 'NAO_LIDA', 'seed-v116') ON CONFLICT (id) DO NOTHING;
    INSERT INTO plantaopro.v116_relatorios_execucoes (id, cliente_id, tenant_id, descricao, status_operacional, created_by) VALUES ('11600000-0000-0000-0000-000000000011', v_cliente, v_tenant, 'Execução demo relatório executivo', 'EXECUTADO', 'seed-v116') ON CONFLICT (id) DO NOTHING;
    INSERT INTO plantaopro.v116_integracao_provedores (id, cliente_id, tenant_id, descricao, status_operacional, created_by) VALUES ('11600000-0000-0000-0000-000000000012', v_cliente, v_tenant, 'Provedor externo demo', 'DEPENDENTE_CONFIGURACAO', 'seed-v116') ON CONFLICT (id) DO NOTHING;
END $$;
