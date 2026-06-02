# Suporte e Chamados — Release Candidate

## Objetivo

Permitir que cliente, médico e operação acompanhem incidentes, dúvidas e solicitações com rastreabilidade, prioridade e auditoria.

## Regras funcionais

- Cliente visualiza apenas chamados do próprio cliente.
- Médico visualiza apenas chamados abertos pelo próprio usuário.
- Admin global pode auditar todos os chamados.
- Criação exige título e descrição.
- Resolução exige descrição da solução.
- Cancelamento exige justificativa.
- Chamado crítico deve gerar alerta operacional.
- Mensagens devem gerar notificação ao solicitante ou responsável.

## Fluxo de homologação

1. Entrar como médico demo.
2. Abrir um chamado via API Mobile ou tela Web de suporte.
3. Confirmar protocolo gerado.
4. Entrar como administrador/CS.
5. Responder e resolver o chamado com descrição.
6. Confirmar notificação e auditoria.

## Estrutura SQL incremental

O script `backend/sql/20260602_suporte_mobile_rc.sql` cria de forma segura:

- `plantaopro.chamados_suporte`
- `plantaopro.chamado_mensagens`
- índices por cliente/status, usuário/data e mensagens por chamado

## Pendências conhecidas

- Resposta pelo app pode ser evoluída após o MVP.
- SLA e filas por responsável podem ser adicionados no ciclo pós-RC.
