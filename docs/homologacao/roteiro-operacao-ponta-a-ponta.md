# Roteiro de homologação — operação ponta a ponta

## Objetivo
Validar o ciclo operacional completo do PlantãoPro em pré-produção: criação/publicação de plantão, solicitação/convite médico, confirmação de escala, realização, geração/baixa de pagamento, notificações, auditoria, Central de Escala e Agenda.

## Pré-condições
- Usuários ativos para ADMINISTRADOR_GLOBAL, COORDENACAO, MEDICO e FINANCEIRO.
- Hospital e especialidade ativos no cliente de teste.
- Médico ativo com CRM/UF CRM, especialidade compatível e sem escala conflitante.
- API, Web, Swagger e `/api/health` disponíveis.

## Fluxo crítico
1. Entrar como coordenação.
2. Criar plantão com data futura, valor positivo e vagas maior que zero.
3. Confirmar que o plantão nasceu como `rascunho`.
4. Publicar o plantão e validar hospital/especialidade ativos.
5. Abrir Central de Escala e verificar o plantão aberto.
6. Gerar médicos recomendados em `/api/plantoes/{id}/medicos-recomendados`.
7. Entrar como médico e listar plantões disponíveis.
8. Solicitar plantão ou aceitar convite.
9. Entrar como coordenação e confirmar escala.
10. Validar redução/liberação de vaga conforme status.
11. Marcar escala como realizada.
12. Entrar como financeiro e gerar pagamento apenas para escala realizada.
13. Confirmar pagamento com valor, data e forma.
14. Entrar como médico e conferir pagamento confirmado.
15. Validar notificações, auditoria e indicadores do dashboard.

## Critérios de aceite
- Nenhuma etapa exibe stack trace, SQL bruto, `alert()` ou confirmação nativa.
- Médico não acessa dados de outro médico.
- Usuário de cliente comum não vê dados de outro cliente.
- Conflito crítico bloqueia solicitação, aceite, confirmação e substituição.
- Central de Escala reflete plantões abertos, solicitações e pagamentos pendentes.
