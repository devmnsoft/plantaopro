# Relatório de conflitos de schema

## plantaopro.medico_indisponibilidades
- Primeira origem: `database/schema/030_operacao_plantoes.sql`
- Segunda origem: `database/schema/210_v1310_consolidacao_operacao_assistida.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: atualizado_em, codigo, criado_em, dados, id, nome, status, tenant_id
- Colunas segunda: disponibilidade_id, fim, id, inicio, motivo

## plantaopro.saved_views
- Primeira origem: `database/schema/210_v1310_consolidacao_operacao_assistida.sql`
- Segunda origem: `database/migrations/2026_v192_saved_views.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: atualizado_em, compartilhada, configuracao, criado_em, id, modulo, nome, padrao, setor_id, tenant_id, usuario_id
- Colunas segunda: created_at, filters_json, id, is_default, module, name, normalized_name, sort_json, tenant_id, updated_at, user_id

