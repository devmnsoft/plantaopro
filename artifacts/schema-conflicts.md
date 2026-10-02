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

## plantaopro.perfil_permissoes
- Primeira origem: `database/schema/010_identity_access.sql`
- Segunda origem: `database/migrations/2026_v1270_normalizar_permissoes_e_prontuario.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: bloqueado_por_plano, created_by, id, perfil_id, permissao_id, permitido, reg_date, reg_status, reg_update, updated_by
- Colunas segunda: id, perfil_id, permissao_id, permitido, reg_date, reg_status

## plantaopro.consulta_historico
- Primeira origem: `database/schema/380_v2160_triagem_consulta_jornada.sql`
- Segunda origem: `database/migrations/2026_v1270_normalizar_permissoes_e_prontuario.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, consulta_id, created_by, evento, id, paciente_id, reg_date, reg_status, versao
- Colunas segunda: cliente_id, consulta_id, created_by, evento, id, reg_date, reg_status, versao

