# Relatório de conflitos de schema

## plantaopro.usuarios_perfis
- Primeira origem: `database/schema/010_identity_access.sql`
- Segunda origem: `database/PlantaoPro_PostgreSQL_Completo.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, created_by, id, perfil_id, reg_date, reg_status, reg_update, tenant_id, updated_by, usuario_id
- Colunas segunda: created_by, id, perfil_id, reg_date, reg_status, reg_update, updated_by, usuario_id

## plantaopro.planos
- Primeira origem: `plantaopro.planos`
- Segunda origem: `backend/sql/20260522_saas_multiempresa.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: descricao, destaque, dias_trial, id, limite_convites_mes, limite_hospitais, limite_medicos, limite_plantoes_mes, limite_usuarios, nome, ordem, ordem_exibicao, permite_api, permite_bi, permite_integracoes, permite_mobile, permite_operacao_assistida, permite_perfis_customizados, permite_relatorios, permite_relatorios_avancados, permite_suporte_prioritario, permite_trial, permite_white_label, publico, reg_date, reg_status, reg_update, slug, status, valor_anual, valor_mensal
- Colunas segunda: descricao, id, limite_hospitais, limite_medicos, limite_plantoes_mes, nome, permite_api, permite_notificacao_email, permite_relatorios, reg_date, reg_status, status, valor_mensal

## plantaopro.tenants
- Primeira origem: `plantaopro.tenants`
- Segunda origem: `database/migrations/2026_plantao_pro_self_service_white_label.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, id, nome, plano_id, reg_date, reg_status, reg_update, slug, status, subdominio
- Colunas segunda: cliente_id, criado_por, id, nome, plano_id, reg_date, reg_status, reg_update, slug, status

## plantaopro.modulos_sistema
- Primeira origem: `database/schema/010_identity_access.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_self_service_white_label.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: codigo, created_by, descricao, id, nome, ordem, reg_date, reg_status, reg_update, status, updated_by
- Colunas segunda: codigo, descricao, id, nome, ordem, reg_date, reg_status, reg_update, status

## plantaopro.acoes_sistema
- Primeira origem: `database/schema/010_identity_access.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_self_service_white_label.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: codigo, created_by, descricao, id, nome, ordem, reg_date, reg_status, reg_update, sensivel, status, updated_by
- Colunas segunda: codigo, descricao, id, nome, ordem, reg_date, reg_status, reg_update, status

## plantaopro.permissoes
- Primeira origem: `database/schema/010_identity_access.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_self_service_white_label.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: acao, acao_id, codigo, created_by, descricao, id, modulo, modulo_id, nome, reg_date, reg_status, reg_update, sensivel, status, updated_by
- Colunas segunda: acao_id, codigo, descricao, id, modulo_id, nome, reg_date, reg_status, reg_update, sensivel, status

## plantaopro.perfis
- Primeira origem: `database/PlantaoPro_PostgreSQL_Completo.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_self_service_white_label.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `False`
- Colunas primeira: created_by, descricao, id, nome, reg_date, reg_status, reg_update, updated_by
- Colunas segunda: base_sistema, cliente_id, codigo, customizado, descricao, id, nome, reg_date, reg_status, reg_update, status, tenant_id

## plantaopro.perfil_permissoes
- Primeira origem: `database/schema/010_identity_access.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_self_service_white_label.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: bloqueado_por_plano, created_by, id, perfil_id, permissao_id, permitido, reg_date, reg_status, reg_update, updated_by
- Colunas segunda: bloqueado_por_plano, id, perfil_id, permissao_id, permitido, reg_date, reg_status, reg_update

## plantaopro.usuarios_perfis
- Primeira origem: `database/PlantaoPro_PostgreSQL_Completo.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_self_service_white_label.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `False`
- Colunas primeira: created_by, id, perfil_id, reg_date, reg_status, reg_update, updated_by, usuario_id
- Colunas segunda: cliente_id, id, perfil_id, reg_date, reg_status, reg_update, tenant_id, usuario_id

## plantaopro.usuario_permissoes_especiais
- Primeira origem: `database/schema/010_identity_access.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_self_service_white_label.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, created_by, id, justificativa, permissao_id, permitido, reg_date, reg_status, reg_update, tenant_id, updated_by, usuario_id
- Colunas segunda: cliente_id, id, justificativa, permissao_id, permitido, reg_date, reg_status, reg_update, tenant_id, usuario_id

## plantaopro.pacientes
- Primeira origem: `database/migrations/2026_saude360_base_clinica_minima.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, cns, consentimento_lgpd, consentimento_lgpd_canal, consentimento_lgpd_em, cpf, created_at, created_by, data_nascimento, documento, documento_alternativo, email, endereco, finalidade_tratamento, id, nome, nome_completo, nome_social, observacoes, reg_date, reg_status, reg_update, responsavel_nome, sexo, sexo_genero, status, telefone, tenant_id, updated_at, updated_by
- Colunas segunda: cliente_id, cns, cpf, created_at, created_by, data_nascimento, documento_alternativo, email, endereco, finalidade_tratamento, id, nome, nome_social, observacoes, reg_date, reg_status, reg_update, responsavel_nome, sexo_genero, status, telefone, tenant_id, updated_at, updated_by, ver_dados_sensiveis

## plantaopro.painel_chamada_fila
- Primeira origem: `database/migrations/2026_saude360_base_clinica_minima.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: agendamento_id, chamado_em, cliente_id, created_at, created_by, finalizado_em, guiche, guiche_id, id, paciente_id, paciente_nome, painel_id, prioridade, reg_date, reg_status, sala, sala_id, senha, setor, setor_id, status, tenant_id, triagem_id, updated_at, updated_by
- Colunas segunda: agendamento_id, atendimento_id, chamada_em, cliente_id, created_by, guiche_id, id, paciente_id, paciente_nome, painel_id, prioridade, reg_date, reg_status, reg_update, sala_id, senha, setor_id, status, tenant_id, triagem_id, updated_by

## plantaopro.painel_chamada_historico
- Primeira origem: `database/migrations/2026_saude360_base_clinica_minima.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: acao, agendamento_id, cliente_id, created_at, detalhe, detalhes, fila_id, id, paciente_id, reg_date, reg_status, status, tenant_id, updated_at, usuario_id
- Colunas segunda: acao, agendamento_id, cliente_id, created_by, detalhes, fila_id, id, paciente_id, painel_id, reg_date, reg_status, reg_update, status, tenant_id, updated_by, usuario_id

## plantaopro.agendamentos
- Primeira origem: `database/migrations/2026_saude360_base_clinica_minima.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, created_at, created_by, data_fim, data_inicio, especialidade, especialidade_id, hospital_id, id, medico_id, motivo_cancelamento, observacoes, paciente_id, reg_date, reg_status, reg_update, sala_id, status, tenant_id, tipo, unidade_id, updated_at, updated_by, valor
- Colunas segunda: cliente_id, convenio_id, created_by, data_fim, data_inicio, especialidade, id, medico_id, observacoes, paciente_id, plano_saude_id, reg_date, reg_status, reg_update, sala_id, status, tenant_id, tipo, unidade_id, updated_by, valor

## plantaopro.triagens
- Primeira origem: `database/migrations/2026_saude360_base_clinica_minima.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: agendamento_id, alergias_relatadas, classificacao_risco, cliente_id, created_at, created_by, enfermeiro_id, finalizada_em, id, iniciada_em, medicamentos_uso, medico_id, motivo_cancelamento, observacoes, paciente_id, profissional_id, queixa_principal, reg_date, reg_status, reg_update, status, tenant_id, updated_at, updated_by
- Colunas segunda: agendamento_id, alergias_relatadas, altura, classificacao_risco, cliente_id, created_by, enfermeiro_id, frequencia_cardiaca, frequencia_respiratoria, glicemia, id, imc, medicamentos_uso, observacoes, paciente_id, peso, pressao_diastolica, pressao_sistolica, queixa_principal, reg_date, reg_status, reg_update, saturacao, status, temperatura, tenant_id, updated_by

## plantaopro.triagem_sinais_vitais
- Primeira origem: `database/migrations/2026_saude360_base_clinica_minima.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: alergias, altura, cliente_id, created_at, created_by, frequencia_cardiaca, frequencia_respiratoria, glicemia, id, imc, medicamentos_uso, observacoes, paciente_id, peso, pressao_arterial, reg_date, reg_status, saturacao, status, temperatura, tenant_id, triagem_id, updated_at
- Colunas segunda: altura, cliente_id, created_by, frequencia_cardiaca, frequencia_respiratoria, glicemia, id, imc, peso, pressao_diastolica, pressao_sistolica, reg_date, reg_status, reg_update, saturacao, status, temperatura, tenant_id, triagem_id, updated_by

## plantaopro.triagem_classificacoes_risco
- Primeira origem: `database/migrations/2026_saude360_base_clinica_minima.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, codigo, cor, id, nome, prioridade, reg_date, reg_status, status, tempo_alvo_minutos, tenant_id
- Colunas segunda: cliente_id, codigo, cor_hex, created_by, id, nome, prioridade, reg_date, reg_status, reg_update, status, tempo_alvo_minutos, tenant_id, updated_by

## plantaopro.triagem_historico
- Primeira origem: `database/migrations/2026_saude360_base_clinica_minima.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: acao, cliente_id, created_at, detalhe, detalhes, id, paciente_id, reg_date, reg_status, status, tenant_id, triagem_id, updated_at, usuario_id
- Colunas segunda: acao, cliente_id, created_by, detalhes, id, reg_date, reg_status, reg_update, status, tenant_id, triagem_id, updated_by, usuario_id

## plantaopro.pacientes
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, cns, cpf, created_at, created_by, data_nascimento, documento_alternativo, email, endereco, finalidade_tratamento, id, nome, nome_social, observacoes, reg_date, reg_status, reg_update, responsavel_nome, sexo_genero, status, telefone, tenant_id, updated_at, updated_by, ver_dados_sensiveis
- Colunas segunda: cliente_id, cpf, created_at, created_by, data_nascimento, email, id, nome, reg_date, reg_status, status, telefone, updated_at, updated_by

## plantaopro.paciente_contatos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_by, email, id, nome, paciente_id, reg_date, reg_status, reg_update, status, telefone, tenant_id, tipo, updated_by
- Colunas segunda: cliente_id, created_at, created_by, email, id, nome, paciente_id, reg_date, reg_status, status, telefone, tipo, updated_at, updated_by

## plantaopro.paciente_enderecos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: bairro, cep, cidade, cliente_id, created_by, estado, id, logradouro, numero, paciente_id, reg_date, reg_status, reg_update, status, tenant_id, updated_by
- Colunas segunda: bairro, cep, cidade, cliente_id, created_at, created_by, estado, id, logradouro, numero, paciente_id, reg_date, reg_status, status, updated_at, updated_by

## plantaopro.paciente_documentos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_by, emissor, id, numero, paciente_id, reg_date, reg_status, reg_update, status, tenant_id, tipo, updated_by, validade
- Colunas segunda: cliente_id, created_at, created_by, emissor, id, numero, paciente_id, reg_date, reg_status, status, tipo, updated_at, updated_by, validade

## plantaopro.paciente_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: acao, cliente_id, created_by, detalhes, id, paciente_id, reg_date, reg_status, reg_update, status, tenant_id, updated_by, usuario_id
- Colunas segunda: acao, cliente_id, created_at, created_by, detalhes, id, paciente_id, reg_date, reg_status, status, updated_at, updated_by, usuario_id

## plantaopro.paineis_chamada
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_by, exibir_nome_completo, id, nome, permite_tv_publica, reg_date, reg_status, reg_update, status, tenant_id, token_publico, unidade_id, updated_by
- Colunas segunda: cliente_id, created_at, created_by, id, nome, permite_tv_publica, reg_date, reg_status, status, token_publico, unidade_id, updated_at, updated_by

## plantaopro.painel_chamada_configuracoes
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_by, exibir_nome_completo, exibir_somente_senha, id, painel_id, reg_date, reg_status, reg_update, status, tema, tempo_exibicao_segundos, tenant_id, updated_by
- Colunas segunda: cliente_id, created_at, created_by, exibir_nome_completo, id, painel_id, reg_date, reg_status, status, tema, tempo_exibicao_segundos, updated_at, updated_by

## plantaopro.painel_chamada_setores
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_by, id, nome, painel_id, reg_date, reg_status, reg_update, status, tenant_id, updated_by
- Colunas segunda: cliente_id, created_at, created_by, id, nome, painel_id, reg_date, reg_status, status, updated_at, updated_by

## plantaopro.painel_chamada_salas
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_by, id, nome, reg_date, reg_status, reg_update, setor_id, status, tenant_id, updated_by
- Colunas segunda: cliente_id, created_at, created_by, id, nome, reg_date, reg_status, setor_id, status, updated_at, updated_by

## plantaopro.painel_chamada_guiches
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_by, id, nome, reg_date, reg_status, reg_update, setor_id, status, tenant_id, updated_by
- Colunas segunda: cliente_id, created_at, created_by, id, nome, reg_date, reg_status, setor_id, status, updated_at, updated_by

## plantaopro.painel_chamada_fila
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: agendamento_id, atendimento_id, chamada_em, cliente_id, created_by, guiche_id, id, paciente_id, paciente_nome, painel_id, prioridade, reg_date, reg_status, reg_update, sala_id, senha, setor_id, status, tenant_id, triagem_id, updated_by
- Colunas segunda: agendamento_id, chamado_em, cliente_id, created_at, created_by, finalizado_em, guiche_id, id, paciente_id, paciente_nome, painel_id, prioridade, reg_date, reg_status, sala_id, senha, setor_id, status, updated_at, updated_by

## plantaopro.painel_chamada_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: acao, agendamento_id, cliente_id, created_by, detalhes, fila_id, id, paciente_id, painel_id, reg_date, reg_status, reg_update, status, tenant_id, updated_by, usuario_id
- Colunas segunda: acao, cliente_id, created_at, created_by, detalhes, fila_id, id, paciente_id, painel_id, reg_date, reg_status, status, updated_at, updated_by, usuario_id

## plantaopro.agendamentos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, convenio_id, created_by, data_fim, data_inicio, especialidade, id, medico_id, observacoes, paciente_id, plano_saude_id, reg_date, reg_status, reg_update, sala_id, status, tenant_id, tipo, unidade_id, updated_by, valor
- Colunas segunda: cliente_id, created_at, created_by, data_fim, data_inicio, especialidade_id, id, medico_id, observacoes, paciente_id, reg_date, reg_status, status, tipo, unidade_id, updated_at, updated_by, valor

## plantaopro.agendamento_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: acao, agendamento_id, cliente_id, created_by, detalhes, id, reg_date, reg_status, reg_update, status, tenant_id, updated_by, usuario_id
- Colunas segunda: acao, agendamento_id, cliente_id, created_at, created_by, detalhes, id, reg_date, reg_status, status, status_anterior, status_novo, updated_at, updated_by, usuario_id

## plantaopro.agendamento_bloqueios
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_by, data_fim, data_inicio, id, medico_id, motivo, reg_date, reg_status, reg_update, status, tenant_id, unidade_id, updated_by
- Colunas segunda: cliente_id, created_at, created_by, data_fim, data_inicio, id, medico_id, motivo, reg_date, reg_status, status, unidade_id, updated_at, updated_by

## plantaopro.agendamento_cancelamentos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: agendamento_id, cliente_id, created_by, id, motivo, reg_date, reg_status, reg_update, status, tenant_id, updated_by, usuario_id
- Colunas segunda: agendamento_id, cliente_id, created_at, created_by, id, motivo, reg_date, reg_status, status, updated_at, updated_by, usuario_id

## plantaopro.agendamento_checkins
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: agendamento_id, cliente_id, created_by, id, observacoes, paciente_id, reg_date, reg_status, reg_update, status, tenant_id, updated_by, usuario_id
- Colunas segunda: agendamento_id, cliente_id, created_at, created_by, id, origem, paciente_id, realizado_em, reg_date, reg_status, status, updated_at, updated_by

## plantaopro.triagens
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: agendamento_id, alergias_relatadas, altura, classificacao_risco, cliente_id, created_by, enfermeiro_id, frequencia_cardiaca, frequencia_respiratoria, glicemia, id, imc, medicamentos_uso, observacoes, paciente_id, peso, pressao_diastolica, pressao_sistolica, queixa_principal, reg_date, reg_status, reg_update, saturacao, status, temperatura, tenant_id, updated_by
- Colunas segunda: agendamento_id, classificacao_risco, cliente_id, created_at, created_by, enfermeiro_id, finalizada_em, id, paciente_id, queixa_principal, reg_date, reg_status, status, updated_at, updated_by

## plantaopro.triagem_sinais_vitais
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: altura, cliente_id, created_by, frequencia_cardiaca, frequencia_respiratoria, glicemia, id, imc, peso, pressao_diastolica, pressao_sistolica, reg_date, reg_status, reg_update, saturacao, status, temperatura, tenant_id, triagem_id, updated_by
- Colunas segunda: cliente_id, created_at, created_by, dor_escala, frequencia_cardiaca, frequencia_respiratoria, glicemia, id, pressao_arterial, reg_date, reg_status, saturacao, status, temperatura, triagem_id, updated_at, updated_by

## plantaopro.triagem_classificacoes_risco
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, codigo, cor_hex, created_by, id, nome, prioridade, reg_date, reg_status, reg_update, status, tempo_alvo_minutos, tenant_id, updated_by
- Colunas segunda: cliente_id, cor, created_at, created_by, id, nome, prioridade, reg_date, reg_status, status, tempo_alvo_minutos, updated_at, updated_by

## plantaopro.triagem_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_base_clinica.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: acao, cliente_id, created_by, detalhes, id, reg_date, reg_status, reg_update, status, tenant_id, triagem_id, updated_by, usuario_id
- Colunas segunda: acao, cliente_id, created_at, created_by, detalhes, id, paciente_id, reg_date, reg_status, status, triagem_id, updated_at, updated_by, usuario_id

## plantaopro.consultas
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_consultas_base.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: agendamento_id, cliente_id, created_at, created_by, data_fim, data_inicio, id, medico_id, motivo_cancelamento, paciente_id, reg_date, reg_status, status, triagem_id, updated_at, updated_by
- Colunas segunda: agendamento_id, cliente_id, created_by, data_fim, data_inicio, especialidade_id, hospital_id, id, medico_id, motivo_atendimento, motivo_cancelamento, observacoes, paciente_id, profissional_id, reg_date, reg_status, reg_update, resumo, status, tenant_id, tipo, triagem_id, unidade_id, updated_by

## plantaopro.consulta_anamnese
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_consultas_base.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: alergias, antecedentes, cliente_id, consulta_id, created_at, created_by, historia_doenca_atual, id, queixa_principal, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: alergias, antecedentes_familiares, antecedentes_pessoais, cliente_id, consulta_id, created_by, habitos_vida, historia_doenca_atual, id, medicamentos_uso, observacoes, paciente_id, queixa_principal, reg_date, reg_status, reg_update, tenant_id, updated_by

## plantaopro.consulta_exame_fisico
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_consultas_base.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, consulta_id, created_at, created_by, descricao, id, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: abdomen, aparelho_cardiovascular, aparelho_respiratorio, cliente_id, consulta_id, created_by, descricao_geral, id, neurologico, observacoes, paciente_id, pele, reg_date, reg_status, reg_update, tenant_id, updated_by

## plantaopro.consulta_diagnosticos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_consultas_base.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cid_id, cliente_id, codigo_cid, consulta_id, created_at, created_by, descricao, id, principal, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cid_codigo, cid_descricao, cid_id, cliente_id, consulta_id, created_by, hipotese, id, observacoes, paciente_id, reg_date, reg_status, tenant_id, tipo

## plantaopro.consulta_condutas
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_consultas_base.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, consulta_id, created_at, created_by, descricao, id, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cliente_id, conduta, consulta_id, created_by, id, orientacoes, paciente_id, reg_date, reg_status, reg_update, retorno_em_dias, retorno_recomendado, solicitacao_exames, tenant_id, updated_by

## plantaopro.consulta_encaminhamentos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_consultas_base.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, consulta_id, created_at, created_by, destino, id, motivo, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cliente_id, consulta_id, created_by, descricao, especialidade_destino_id, id, observacoes, paciente_id, prioridade, reg_date, reg_status, tenant_id

## plantaopro.consulta_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_consultas_base.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: acao, cliente_id, consulta_id, created_at, created_by, detalhes, id, paciente_id, reg_date, reg_status, status, updated_at, updated_by, usuario_id
- Colunas segunda: acao, cliente_id, consulta_id, detalhe, id, paciente_id, reg_date, reg_status, tenant_id, usuario_id

## plantaopro.cid_tabela
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_cid_prescricao.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: ativo, cliente_id, codigo, created_at, created_by, descricao, grupo, id, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: categoria, cliente_id, codigo, created_at, created_by, descricao, id, reg_date, reg_status, reg_update, status, tenant_id, updated_at, updated_by

## plantaopro.cid_favoritos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_cid_prescricao.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cid_id, cliente_id, created_at, created_by, id, medico_id, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cid_id, cliente_id, created_at, created_by, id, medico_id, reg_date, reg_status, reg_update, status, tenant_id, updated_at, updated_by, usuario_id

## plantaopro.cid_uso_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_cid_prescricao.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: acao, cid_id, cliente_id, consulta_id, created_at, created_by, id, medico_id, paciente_id, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cid_id, cliente_id, consulta_id, created_at, created_by, id, medico_id, paciente_id, reg_date, reg_status, status, tenant_id, usuario_id

## plantaopro.consulta_historico
- Primeira origem: `database/migrations/2026_saude360_consultas_base.sql`
- Segunda origem: `database/migrations/2026_saude360_cid_prescricao.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: acao, cliente_id, consulta_id, detalhe, id, paciente_id, reg_date, reg_status, tenant_id, usuario_id
- Colunas segunda: acao, cliente_id, consulta_id, created_at, created_by, detalhe, detalhes, id, paciente_id, reg_date, reg_status, status, tenant_id, usuario_id

## plantaopro.prescricoes
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_cid_prescricao.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cancelada_em, cliente_id, consulta_id, created_at, created_by, finalizada_em, id, medico_id, modelo_id, paciente_id, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cancelada_em, cliente_id, consulta_id, created_at, created_by, finalizada_em, id, medico_id, modelo_id, orientacoes, paciente_id, reg_date, reg_status, reg_update, status, tenant_id, updated_at, updated_by

## plantaopro.prescricao_itens
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_cid_prescricao.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_at, created_by, duracao, id, medicamento, posologia, prescricao_id, quantidade, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cliente_id, created_at, created_by, duracao, frequencia, id, medicamento, ordem, orientacoes, posologia, prescricao_id, reg_date, reg_status, reg_update, status, tenant_id, updated_at, updated_by

## plantaopro.prescricao_modelos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_cid_prescricao.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, created_at, created_by, descricao, id, itens, medico_id, nome, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cliente_id, conteudo, created_at, created_by, descricao, id, medico_id, nome, reg_date, reg_status, reg_update, status, tenant_id, updated_at, updated_by

## plantaopro.prescricao_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_cid_prescricao.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: acao, cliente_id, created_at, created_by, detalhes, id, prescricao_id, reg_date, reg_status, status, updated_at, updated_by, usuario_id
- Colunas segunda: acao, cliente_id, created_at, created_by, detalhes, id, prescricao_id, reg_date, reg_status, status, tenant_id, usuario_id

## plantaopro.prescricao_cancelamentos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_cid_prescricao.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_at, created_by, id, justificativa, prescricao_id, reg_date, reg_status, status, updated_at, updated_by, usuario_id
- Colunas segunda: cliente_id, created_at, created_by, id, justificativa, prescricao_id, reg_date, reg_status, status, tenant_id, usuario_id

## plantaopro.convenios
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, cnpj, codigo_ans, created_at, created_by, id, nome, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cliente_id, cnpj, codigo, codigo_ans, created_at, created_by, email, id, nome, observacoes, reg_date, reg_status, reg_update, responsavel, status, telefone, tenant_id, updated_at, updated_by

## plantaopro.convenio_contratos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, convenio_id, created_at, created_by, id, numero, reg_date, reg_status, status, updated_at, updated_by, vigencia_fim, vigencia_inicio
- Colunas segunda: cliente_id, convenio_id, created_by, id, numero_contrato, observacoes, reg_date, reg_status, reg_update, status, tenant_id, updated_by, vigencia_fim, vigencia_inicio

## plantaopro.convenio_planos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, convenio_id, created_at, created_by, id, nome, reg_date, reg_status, registro_ans, status, updated_at, updated_by
- Colunas segunda: cliente_id, codigo, convenio_id, coparticipacao_percentual, created_at, created_by, id, nome, observacoes, reg_date, reg_status, reg_update, registro_ans, status, tenant_id, tipo_acomodacao, updated_at, updated_by

## plantaopro.convenio_tabelas
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, convenio_id, created_at, created_by, id, nome, reg_date, reg_status, status, updated_at, updated_by, vigencia_inicio
- Colunas segunda: cliente_id, codigo, convenio_id, id, nome, reg_date, reg_status, status, tenant_id

## plantaopro.convenio_procedimentos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, codigo, convenio_id, created_at, created_by, descricao, id, reg_date, reg_status, status, tabela_id, updated_at, updated_by, valor
- Colunas segunda: cliente_id, codigo, convenio_id, descricao, id, reg_date, reg_status, status, tabela_id, tenant_id, valor

## plantaopro.convenio_autorizacoes
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: agendamento_id, cliente_id, consulta_id, convenio_id, created_at, created_by, id, motivo, numero_guia, paciente_id, procedimento_id, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: agendamento_id, cliente_id, consulta_id, convenio_id, created_at, created_by, id, motivo, motivo_negativa, numero_guia, paciente_id, plano_saude_id, procedimento, procedimento_id, reg_date, reg_status, reg_update, senha_autorizacao, status, tenant_id, updated_at, updated_by, validade, valor_autorizado

## plantaopro.convenio_glosas
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: autorizacao_id, cliente_id, convenio_id, created_at, created_by, id, motivo, reg_date, reg_status, status, updated_at, updated_by, valor
- Colunas segunda: cliente_id, convenio_id, faturamento_id, id, motivo, reg_date, reg_status, status, tenant_id, valor

## plantaopro.convenio_faturamentos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, competencia, convenio_id, created_at, created_by, id, reg_date, reg_status, status, updated_at, updated_by, valor_total
- Colunas segunda: cliente_id, competencia, convenio_id, id, reg_date, reg_status, status, tenant_id, valor_total

## plantaopro.planos_saude
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, created_at, created_by, id, nome, operadora, reg_date, reg_status, registro_ans, status, updated_at, updated_by
- Colunas segunda: cliente_id, codigo, convenio_id, created_at, created_by, id, nome, observacoes, operadora, reg_date, reg_status, reg_update, registro_ans, status, tenant_id, tipo, updated_at, updated_by

## plantaopro.plano_saude_coberturas
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_at, created_by, descricao, id, limite, plano_saude_id, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cliente_id, descricao, id, plano_saude_id, reg_date, reg_status, status, tenant_id

## plantaopro.plano_saude_pacientes
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_at, created_by, id, numero_carteirinha, paciente_id, plano_saude_id, principal, reg_date, reg_status, status, updated_at, updated_by, validade
- Colunas segunda: cliente_id, convenio_id, created_at, created_by, dependente, id, numero_carteirinha, observacoes, paciente_id, plano_saude_id, principal, reg_date, reg_status, reg_update, status, tenant_id, titular_documento, titular_nome, updated_at, updated_by, validade

## plantaopro.plano_saude_autorizacoes
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_at, created_by, id, motivo, paciente_id, plano_saude_id, procedimento, reg_date, reg_status, status, updated_at, updated_by
- Colunas segunda: cliente_id, id, paciente_id, plano_saude_id, procedimento, reg_date, reg_status, status, tenant_id

## plantaopro.plano_saude_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_convenios_planos_saude.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: acao, cliente_id, created_at, created_by, detalhes, id, paciente_id, plano_saude_id, reg_date, reg_status, status, updated_at, updated_by, usuario_id
- Colunas segunda: acao, cliente_id, detalhes, id, plano_saude_id, reg_date, reg_status, tenant_id

## plantaopro.clinica_contas_receber
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_financeiro_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: agendamento_id, cliente_id, consulta_id, created_at, created_by, descricao, id, paciente_id, reg_date, reg_status, status, updated_at, updated_by, valor, vencimento
- Colunas segunda: agendamento_id, cliente_id, consulta_id, convenio_id, created_at, created_by, descricao, id, medico_id, observacoes, origem, paciente_id, plano_saude_id, reg_date, reg_status, reg_update, status, tenant_id, tipo_recebimento, updated_at, updated_by, valor_desconto, valor_pago, valor_pendente, valor_total, vencimento

## plantaopro.clinica_recebimentos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_financeiro_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, conta_receber_id, created_at, created_by, forma_pagamento, id, recebido_em, reg_date, reg_status, status, updated_at, updated_by, valor
- Colunas segunda: cliente_id, comprovante, conta_receber_id, created_at, created_by, data_recebimento, forma_pagamento, id, observacoes, paciente_id, reg_date, reg_status, reg_update, status, tenant_id, updated_at, updated_by, valor

## plantaopro.clinica_caixa
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_financeiro_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, created_at, created_by, data_abertura, data_fechamento, id, reg_date, reg_status, saldo_final, saldo_inicial, status, updated_at, updated_by
- Colunas segunda: cliente_id, created_at, created_by, data_abertura, data_fechamento, id, observacoes, reg_date, reg_status, reg_update, saldo_final, saldo_inicial, status, tenant_id, total_entradas, total_saidas, unidade_id, updated_at, updated_by, usuario_abertura_id, usuario_fechamento_id

## plantaopro.clinica_fechamentos_caixa
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_financeiro_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: caixa_id, cliente_id, created_at, created_by, divergencia, id, observacoes, reg_date, reg_status, status, updated_at, updated_by, valor_informado
- Colunas segunda: caixa_id, cliente_id, divergencia, id, reg_date, reg_status, saldo_informado, status, tenant_id

## plantaopro.clinica_repasses
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_financeiro_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, competencia, created_at, created_by, id, medico_id, reg_date, reg_status, status, updated_at, updated_by, valor
- Colunas segunda: cliente_id, id, medico_id, reg_date, reg_status, status, tenant_id, valor

## plantaopro.clinica_lancamentos
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_financeiro_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: caixa_id, cliente_id, created_at, created_by, descricao, id, reg_date, reg_status, status, tipo, updated_at, updated_by, valor
- Colunas segunda: caixa_id, cliente_id, descricao, id, reg_date, reg_status, status, tenant_id, tipo, valor

## plantaopro.clinica_glosas
- Primeira origem: `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`
- Segunda origem: `database/migrations/2026_saude360_financeiro_clinica.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, convenio_id, created_at, created_by, faturamento_id, id, motivo, reg_date, reg_status, status, updated_at, updated_by, valor
- Colunas segunda: cliente_id, conta_receber_id, convenio_id, id, motivo, reg_date, reg_status, status, tenant_id, valor

## plantaopro.assinatura_historico
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: assinatura_id, cliente_id, id, motivo, reg_date, reg_status, status_anterior, status_novo, tenant_id, usuario_id
- Colunas segunda: acao, assinatura_id, cliente_id, id, justificativa, plano_id_anterior, plano_id_novo, reg_date, reg_status, status_anterior, status_novo, usuario_id

## plantaopro.assinatura_uso
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: assinatura_id, cliente_id, competencia, convites_usados, hospitais_usados, id, medicos_usados, plantoes_usados, reg_date, reg_status, reg_update, tenant_id, usuarios_usados
- Colunas segunda: assinatura_id, cliente_id, competencia, id, origem, quantidade, recurso, reg_date, reg_status

## plantaopro.clientes
- Primeira origem: `backend/sql/20260522_saas_multiempresa.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: cidade, cnpj, email, estado, id, nome_fantasia, plano_id, razao_social, reg_date, reg_status, reg_update, status, telefone
- Colunas segunda: cnpj, id, nome_fantasia, razao_social, reg_date, reg_status, reg_update, status

## plantaopro.planos
- Primeira origem: `backend/sql/20260522_saas_multiempresa.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: descricao, id, limite_hospitais, limite_medicos, limite_plantoes_mes, nome, permite_api, permite_notificacao_email, permite_relatorios, reg_date, reg_status, status, valor_mensal
- Colunas segunda: descricao, id, limite_hospitais, limite_medicos, limite_plantoes_mes, nome, operacao_assistida, possui_bi, possui_mobile, possui_relatorios_avancados, reg_date, reg_status, reg_update, status, suporte_prioritario, valor_mensal

## plantaopro.plano_recursos
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: codigo, descricao, habilitado, id, limite, nome, plano_id, reg_date, reg_status, reg_update
- Colunas segunda: habilitado, id, limite, plano_id, recurso, reg_date, reg_status

## plantaopro.assinaturas
- Primeira origem: `plantaopro.assinaturas`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, data_fim, data_inicio, data_trial_fim, dia_vencimento, id, observacoes, periodicidade, plano_id, reg_date, reg_status, reg_update, status, tenant_id, valor_contratado, valor_mensal
- Colunas segunda: cliente_id, data_fim, data_inicio, id, plano_id, reg_date, reg_status, reg_update, status, valor_mensal

## plantaopro.assinatura_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: acao, assinatura_id, cliente_id, id, justificativa, plano_id_anterior, plano_id_novo, reg_date, reg_status, status_anterior, status_novo, usuario_id
- Colunas segunda: assinatura_id, cliente_id, id, plano_id, reg_date, reg_status, resumo, tipo, usuario_id

## plantaopro.assinatura_uso
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: assinatura_id, cliente_id, competencia, id, origem, quantidade, recurso, reg_date, reg_status
- Colunas segunda: assinatura_id, cliente_id, competencia, hospitais_usados, id, medicos_usados, plantoes_mes_usados, reg_date, reg_status, reg_update

## plantaopro.faturas_saas
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: assinatura_id, atualizado_em, cliente_id, competencia, criado_em, data_pagamento, forma_pagamento, id, motivo_cancelamento, motivo_contestacao, reg_date, reg_status, resposta_contestacao, status, valor, valor_pago, vencimento
- Colunas segunda: assinatura_id, cliente_id, competencia, id, reg_date, reg_status, reg_update, status, valor_total, vencimento

## plantaopro.fatura_itens
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: criado_em, descricao, fatura_id, id, quantidade, reg_date, reg_status, valor_total, valor_unitario
- Colunas segunda: descricao, fatura_id, id, quantidade, reg_date, reg_status, valor_total, valor_unitario

## plantaopro.pagamentos_saas
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, criado_em, data_pagamento, fatura_id, forma_pagamento, id, observacoes, reg_date, reg_status, valor_pago
- Colunas segunda: cliente_id, data_pagamento, fatura_id, id, metodo, reg_date, reg_status, status, valor_pago

## plantaopro.cobranca_eventos
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: canal, cliente_id, fatura_id, id, mensagem, reg_date, reg_status, sucesso, tipo, usuario_id
- Colunas segunda: cliente_id, fatura_id, id, mensagem, reg_date, reg_status, tipo

## plantaopro.cliente_bloqueios
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, id, motivo, origem, reg_date, reg_status, tipo, usuario_id
- Colunas segunda: ativo, cliente_id, id, motivo, origem, reg_date, reg_status, resolvido_em, tipo

## plantaopro.cliente_alertas
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, id, mensagem, reg_date, reg_status, resolvido, resolvido_em, severidade, tipo, titulo, usuario_resolucao_id
- Colunas segunda: cliente_id, id, mensagem, reg_date, reg_status, resolvido, resolvido_em, severidade, tipo, titulo

## plantaopro.cliente_limites_uso
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: assinatura_id, cliente_id, competencia, id, limite, percentual, recurso, reg_date, reg_status, reg_update, usado
- Colunas segunda: cliente_id, id, limite, percentual, recurso, reg_date, reg_status, usado

## plantaopro.customer_success_planos_acao
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: atualizado_em, cliente_id, concluido_em, criado_em, descricao, id, prazo, prioridade, reg_date, reg_status, responsavel, status, titulo
- Colunas segunda: cliente_id, concluido_em, descricao, id, prazo, prioridade, reg_date, reg_status, responsavel, status, titulo

## plantaopro.lgpd_consentimentos
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: aceito, cliente_id, finalidade, id, ip_origem, origem, politica_id, reg_date, reg_status, tenant_id, titular_email, user_agent, usuario_id, versao_politica
- Colunas segunda: base_legal, cliente_id, consentido, finalidade, id, ip, reg_date, reg_status, user_agent, usuario_id, versao_politica

## plantaopro.lgpd_solicitacoes_titular
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, descricao, id, prazo_resposta, reg_date, reg_status, reg_update, status, tenant_id, tipo, usuario_id
- Colunas segunda: cliente_id, descricao, id, reg_date, reg_status, respondida_em, resposta, status, tipo, usuario_id

## plantaopro.lgpd_eventos_privacidade
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, descricao, id, reg_date, reg_status, severidade, tenant_id, tipo, usuario_id
- Colunas segunda: acao, cliente_id, detalhes, id, ip, reg_date, reg_status, usuario_id

## plantaopro.lgpd_politicas
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: conteudo, id, publicada_em, reg_date, reg_status, reg_update, status, tenant_id, titulo, versao, vigente
- Colunas segunda: conteudo, id, reg_date, reg_status, titulo, versao, vigente_desde

## plantaopro.planos
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: descricao, id, limite_hospitais, limite_medicos, limite_plantoes_mes, nome, operacao_assistida, possui_bi, possui_mobile, possui_relatorios_avancados, reg_date, reg_status, reg_update, status, suporte_prioritario, valor_mensal
- Colunas segunda: descricao, id, limite_convites_mes, limite_hospitais, limite_medicos, limite_plantoes_mes, limite_usuarios, nome, permite_bi, permite_integracoes, permite_mobile, permite_operacao_assistida, permite_relatorios_avancados, permite_suporte_prioritario, reg_date, reg_status, reg_update, status, valor_mensal

## plantaopro.plano_recursos
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: habilitado, id, limite, plano_id, recurso, reg_date, reg_status
- Colunas segunda: habilitado, id, limite, plano_id, recurso, reg_date, reg_status, reg_update

## plantaopro.assinaturas
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, data_fim, data_inicio, id, plano_id, reg_date, reg_status, reg_update, status, valor_mensal
- Colunas segunda: cliente_id, data_fim, data_inicio, data_trial_fim, dia_vencimento, id, periodicidade, plano_id, reg_date, reg_status, reg_update, status, valor_contratado

## plantaopro.assinatura_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: assinatura_id, cliente_id, id, plano_id, reg_date, reg_status, resumo, tipo, usuario_id
- Colunas segunda: assinatura_id, cliente_id, id, motivo, plano_id, reg_date, reg_status, status_anterior, status_novo, usuario_id

## plantaopro.assinatura_uso
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: assinatura_id, cliente_id, competencia, hospitais_usados, id, medicos_usados, plantoes_mes_usados, reg_date, reg_status, reg_update
- Colunas segunda: assinatura_id, cliente_id, competencia, id, quantidade, recurso, reg_date, reg_status

## plantaopro.pagamentos_saas
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, data_pagamento, fatura_id, id, metodo, reg_date, reg_status, status, valor_pago
- Colunas segunda: cliente_id, data_pagamento, fatura_id, id, metodo, reg_date, reg_status, reg_update, status, valor_pago

## plantaopro.cliente_bloqueios
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: ativo, cliente_id, id, motivo, origem, reg_date, reg_status, resolvido_em, tipo
- Colunas segunda: cliente_id, id, motivo, origem, reg_date, reg_status, reg_update, resolvido, tipo

## plantaopro.cliente_alertas
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, id, mensagem, reg_date, reg_status, resolvido, resolvido_em, severidade, tipo, titulo
- Colunas segunda: cliente_id, id, mensagem, reg_date, reg_status, reg_update, resolvido, severidade, tipo, titulo

## plantaopro.cliente_limites_uso
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, id, limite, percentual, recurso, reg_date, reg_status, usado
- Colunas segunda: cliente_id, competencia, id, limite, recurso, reg_date, reg_status, reg_update, utilizado

## plantaopro.cliente_saude_historico
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: classificacao, cliente_id, id, oportunidades, reg_date, reg_status, riscos, score
- Colunas segunda: acoes, classificacao, cliente_id, id, oportunidades, reg_date, reg_status, riscos, score

## plantaopro.jornada_cliente_responsaveis
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: ativo, cliente_id, id, nome, papel, reg_date, reg_status, usuario_id
- Colunas segunda: cliente_id, id, nome, papel, reg_date, reg_status, usuario_id

## plantaopro.comercial_leads
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: email, empresa, id, nome, plano_recomendado, reg_date, reg_status, reg_update, status, telefone
- Colunas segunda: email, empresa, hospitais_desejados, id, medicos_desejados, nome, operacao_assistida, plano_recomendado, plantoes_mes, precisa_bi, precisa_mobile, reg_date, reg_status, reg_update, status, suporte_prioritario, telefone

## plantaopro.comercial_propostas
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: desconto_percentual, id, numero, oportunidade_id, reg_date, reg_status, reg_update, status, validade, valor_total
- Colunas segunda: aprovada_em, desconto_percentual, enviada_em, id, motivo_recusa, numero, oportunidade_id, recusada_em, reg_date, reg_status, reg_update, status, validade, valor_total

## plantaopro.comercial_motivos_perda
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: ativo, descricao, id, reg_date, reg_status
- Colunas segunda: ativo, id, motivo, reg_date, reg_status

## plantaopro.comercial_regras_desconto
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: exige_admin_global, id, percentual_maximo, plano_id, reg_date, reg_status
- Colunas segunda: exige_aprovacao, id, percentual_maximo, perfil, reg_date, reg_status

## plantaopro.customer_success_interacoes
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, data_interacao, descricao, id, proxima_acao, reg_date, reg_status, resumo, risco, tipo, usuario_id
- Colunas segunda: cliente_id, id, reg_date, reg_status, resumo, tipo, usuario_id

## plantaopro.customer_success_planos_acao
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, concluido_em, descricao, id, prazo, prioridade, reg_date, reg_status, responsavel, status, titulo
- Colunas segunda: cliente_id, id, objetivo, reg_date, reg_status, reg_update, responsavel, status, titulo, vencimento

## plantaopro.customer_success_riscos
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, descricao, id, reg_date, reg_status, severidade, status, tipo
- Colunas segunda: cliente_id, descricao, id, reg_date, reg_status, reg_update, severidade, status, tipo

## plantaopro.customer_success_tarefas
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, concluida_em, id, reg_date, reg_status, responsavel, status, titulo, vencimento
- Colunas segunda: cliente_id, id, reg_date, reg_status, reg_update, responsavel, status, titulo, vencimento

## plantaopro.lgpd_politicas
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: conteudo, id, reg_date, reg_status, titulo, versao, vigente_desde
- Colunas segunda: conteudo, id, publicada, reg_date, reg_status, reg_update, titulo, versao, vigente_desde

## plantaopro.lgpd_consentimentos
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `True`
- Colunas primeira: base_legal, cliente_id, consentido, finalidade, id, ip, reg_date, reg_status, user_agent, usuario_id, versao_politica
- Colunas segunda: base_legal, cliente_id, consentido, finalidade, id, ip, reg_date, reg_status, reg_update, user_agent, usuario_id, versao_politica

## plantaopro.lgpd_solicitacoes_titular
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, descricao, id, reg_date, reg_status, respondida_em, resposta, status, tipo, usuario_id
- Colunas segunda: cliente_id, descricao, id, reg_date, reg_status, reg_update, respondida_em, respondida_por, resposta, status, tipo, usuario_id

## plantaopro.lgpd_retencao_dados
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: base_legal, entidade, id, observacao, prazo_meses, reg_date, reg_status
- Colunas segunda: base_legal, categoria, id, prazo, reg_date, reg_status, regra

## plantaopro.lgpd_exportacoes_dados
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: arquivo, cliente_id, id, reg_date, reg_status, status, usuario_id
- Colunas segunda: arquivo_url, cliente_id, id, ip, reg_date, reg_status, status, usuario_id

## plantaopro.lgpd_anonimizacoes
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, id, motivo, reg_date, reg_status, status, usuario_id
- Colunas segunda: id, motivo, reg_date, reg_status, status, usuario_id

## plantaopro.ajuda_topicos
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: descricao, id, ordem, perfil, reg_date, reg_status, titulo
- Colunas segunda: descricao, id, ordem, perfil, reg_date, reg_status, reg_update, titulo

## plantaopro.ajuda_artigos
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: conteudo, id, link_acao, ordem, perfil, reg_date, reg_status, titulo, topico_id
- Colunas segunda: conteudo, id, link_acao, ordem, perfil, reg_date, reg_status, reg_update, titulo, topico_id

## plantaopro.ajuda_checklists
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: id, link_acao, ordem, perfil, reg_date, reg_status, titulo
- Colunas segunda: concluido, id, ordem, perfil, reg_date, reg_status, reg_update, titulo

## plantaopro.eventos_sistema
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: cliente_id, correlation_id, id, mensagem, reg_date, reg_status, tipo, usuario_id
- Colunas segunda: cliente_id, correlation_id, entidade, entidade_id, id, mensagem, reg_date, reg_status, tipo, usuario_id

## plantaopro.logs_operacionais
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: acao, cliente_id, correlation_id, entidade, entidade_id, id, ip, mensagem, perfil, reg_date, reg_status, sucesso, user_agent, usuario_id
- Colunas segunda: acao, cliente_id, correlation_id, dados_antes, dados_depois, entidade, entidade_id, id, ip, mensagem, perfil, reg_date, reg_status, sucesso, user_agent, usuario_id

## plantaopro.auditoria_lgpd_eventos
- Primeira origem: `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql`
- Segunda origem: `database/migrations/2026_plantao_pro_saas_inteligente_auditavel.sql`
- Canônico no manifesto: `False`
- ALTER compatibilidade: `False`
- Colunas primeira: acao, cliente_id, correlation_id, entidade, entidade_id, id, ip, mensagem, reg_date, reg_status, sucesso, usuario_id
- Colunas segunda: acao, base_legal, cliente_id, correlation_id, finalidade, id, ip, mensagem, reg_date, reg_status, sucesso, usuario_id

## plantaopro.modulos_sistema
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: codigo, descricao, id, nome, ordem, reg_date, reg_status, reg_update, status
- Colunas segunda: codigo, created_by, descricao, id, nome, ordem, reg_date, reg_status, reg_update, status, updated_by

## plantaopro.acoes_sistema
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: codigo, descricao, id, nome, ordem, reg_date, reg_status, reg_update, status
- Colunas segunda: codigo, created_by, descricao, id, nome, ordem, reg_date, reg_status, reg_update, sensivel, status, updated_by

## plantaopro.permissoes
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: acao_id, codigo, descricao, id, modulo_id, nome, reg_date, reg_status, reg_update, sensivel, status
- Colunas segunda: acao, acao_id, codigo, created_by, descricao, id, modulo, modulo_id, nome, reg_date, reg_status, reg_update, sensivel, status, updated_by

## plantaopro.perfil_permissoes
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: bloqueado_por_plano, id, perfil_id, permissao_id, permitido, reg_date, reg_status, reg_update
- Colunas segunda: bloqueado_por_plano, created_by, id, perfil_id, permissao_id, permitido, reg_date, reg_status, reg_update, updated_by

## plantaopro.usuarios_perfis
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, id, perfil_id, reg_date, reg_status, reg_update, tenant_id, usuario_id
- Colunas segunda: cliente_id, created_by, id, perfil_id, reg_date, reg_status, reg_update, tenant_id, updated_by, usuario_id

## plantaopro.usuario_permissoes_especiais
- Primeira origem: `database/migrations/2026_plantao_pro_white_label_self_service.sql`
- Segunda origem: `database/migrations/2026_v1186_schema_permissoes_compatibilidade.sql`
- Canônico no manifesto: `True`
- ALTER compatibilidade: `True`
- Colunas primeira: cliente_id, id, justificativa, permissao_id, permitido, reg_date, reg_status, reg_update, tenant_id, usuario_id
- Colunas segunda: cliente_id, created_by, id, justificativa, permissao_id, permitido, reg_date, reg_status, reg_update, tenant_id, updated_by, usuario_id

