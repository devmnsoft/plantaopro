# Pacientes

O cadastro de pacientes é multi-tenant e usa `cliente_id`/`tenant_id`, `reg_status`, `reg_date`, `created_by`, `updated_by` e trilha em `paciente_historico`.

## Campos mínimos

Nome completo, nome social, nascimento, sexo/gênero, CPF opcional, CNS, documento alternativo, telefone, e-mail, endereço, responsável e observações administrativas.

## Regras

- CPF informado não pode duplicar dentro do mesmo tenant.
- CPF vazio é permitido para estrangeiros ou pacientes sem documento.
- Acesso a resumo clínico é auditado.
- Logs técnicos não devem registrar conteúdo clínico sensível.
