# API Mobile MVP — fluxos de homologação

## Login e sessão
1. Enviar e-mail e senha para `POST /api/mobile/auth/login`.
2. Armazenar JWT apenas em storage seguro do app.
3. Enviar `Authorization: Bearer {token}` nos demais endpoints.

## Solicitar plantão
1. App carrega dashboard.
2. App lista plantões disponíveis com paginação.
3. Médico abre detalhe do plantão.
4. Médico solicita plantão.
5. API resolve o médico pelo usuário autenticado, valida `cliente_id` do plantão e delega a solicitação ao serviço operacional de escala.
6. Serviço de escala bloqueia plantão indisponível, duplicidade, médico inativo, especialidade incompatível, conflito de horário e limite semanal.
7. API registra auditoria da solicitação ou do acesso negado.
8. App exibe toast/snackbar com mensagem amigável.

## Convites
1. App lista convites do médico autenticado em `GET /api/mobile/convites?page=1&pageSize=20`.
2. API resolve o médico pelo usuário autenticado e filtra convites por `medico_id`, `cliente_id` e `reg_status`, sem aceitar `medicoId` no payload.
3. Médico abre o detalhe do convite em `GET /api/mobile/convites/{id}`; convites de outro médico retornam `404` amigável.
4. Médico aceita convite em `POST /api/mobile/convites/{id}/aceitar`.
5. API valida se o convite está `ENVIADO` ou `PENDENTE`, revalida vaga, duplicidade, conflito e elegibilidade pelo serviço operacional de escala e atualiza o convite para `ACEITO` quando a solicitação é criada.
6. Médico recusa convite em `POST /api/mobile/convites/{id}/recusar` enviando `{ "motivo": "..." }`.
7. API exige motivo, atualiza o convite para `RECUSADO`, grava data de resposta e registra auditoria sem logar o texto sensível do motivo.
8. App exibe toast/snackbar com a mensagem amigável retornada pelo `ApiResponse<T>`.

## Pagamentos e notificações
1. App lista pagamentos próprios.
2. App destaca pagamentos pendentes e confirmados.
3. App lista notificações.
4. Médico marca notificação como lida.

## Segurança
- Médico nunca informa `medicoId` para acessar dados de terceiros.
- O backend resolve o médico pelo usuário autenticado sempre que possível.
- Payloads de erro não devem expor stack trace ou SQL.
