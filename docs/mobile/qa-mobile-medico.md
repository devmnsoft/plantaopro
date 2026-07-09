# QA mobile médico

Status geral: **Parcial / Funcional pendente QA**.

## Checklist obrigatório

- Usar `EXPO_PUBLIC_API_BASE_URL`; não usar URL fixa de produção.
- Não registrar token em log.
- Não usar `Alert.alert`; confirmações devem usar componentes próprios.
- Exibir loading, empty state e error state.
- Botões grandes e navegação mobile-first.

## Fluxos

| Fluxo | Endpoint esperado | Status |
|---|---|---|
| Plantões disponíveis: listar/detalhe/solicitar | `mobile/plantoes-disponiveis`, `mobile/plantoes/{id}`, `mobile/plantoes/{id}/solicitar` | Funcional pendente QA |
| Convites: listar/detalhe/aceitar/recusar | `mobile/convites` | Funcional pendente QA |
| Escalas confirmadas/realizadas | `mobile/minhas-escalas` | Funcional pendente QA |
| Pagamentos e contestação | `mobile/meus-pagamentos` | Parcial |
| Notificações e marcação de leitura | `mobile/notificacoes` | Funcional pendente QA |
| Disponibilidade e preferências | `mobile/disponibilidade`, `mobile/preferencias` | Parcial |

Validação Expo/Metro real permanece **Bloqueado por ambiente** quando dependências Node/Expo não estiverem instaladas.
