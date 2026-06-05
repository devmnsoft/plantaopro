# Checklist CRUD/AJAX Beta — 2026-06-05

## Correções consolidadas neste incremento

- Especialidades: criação, edição e inativação no MVC, com validação client/server, toast global, modal de confirmação e auditoria via API.
- Médicos: criação, edição e inativação no MVC, com carga segura de especialidades, validação client/server, toast global, modal de confirmação e auditoria via API.
- Hospitais: criação, edição e inativação no MVC, com validação client/server, toast global, modal de confirmação e auditoria via API.
- Desserialização Web/API: leitura tolerante para envelopes `data`, `payload`, `result`, `value`, coleções em `items/results/records` e paginação normalizada.
- UX/AJAX: formulários `data-ajax-form` passam a redirecionar corretamente após redirects HTTP seguidos pelo `fetch`, mantendo feedback de toast.

## Roteiro manual obrigatório

1. Autenticar como Admin Global ou Coordenação.
2. Acessar **Especialidades**, criar registro, editar descrição/status e inativar via modal.
3. Acessar **Hospitais**, criar registro com CNPJ/UF, editar contato/endereço e inativar via modal.
4. Acessar **Médicos**, criar registro vinculado a especialidade, editar CRM/contato e inativar via modal.
5. Validar mensagens obrigatórias com campos vazios e confirmar que os toasts aparecem após sucesso/erro.
6. Confirmar no banco que auditoria registra CREATE/UPDATE/DELETE para `especialidades`, `hospitais` e `medicos`.

## Pendências de homologação ampliada

- Repetir o mesmo padrão de CRUD/AJAX nas áreas de Clientes, Planos, Assinaturas, Usuários, Suporte, Pagamentos, Notificações e Comunicação.
- Acrescentar testes automatizados assim que o SDK .NET estiver disponível no ambiente de CI/agente.
- Executar roteiro mobile ponta a ponta com JWT e paginação real contra base de homologação.
