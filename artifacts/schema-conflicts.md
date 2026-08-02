# Relatório de conflitos de schema

## plantaopro.medico_indisponibilidades
- Primeira origem: `database/schema/030_operacao_plantoes.sql`
- Segunda origem: `database/schema/210_v1310_consolidacao_operacao_assistida.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: atualizado_em, codigo, criado_em, dados, id, nome, status, tenant_id
- Colunas segunda: disponibilidade_id, fim, id, inicio, motivo
