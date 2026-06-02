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
5. API valida vínculo do médico, duplicidade, elegibilidade e conflito.
6. App exibe toast/snackbar com mensagem amigável.

## Convites
1. App lista convites pendentes.
2. Médico aceita convite.
3. API revalida vaga e conflito.
4. Médico pode recusar convite informando motivo quando exigido.

## Pagamentos e notificações
1. App lista pagamentos próprios.
2. App destaca pagamentos pendentes e confirmados.
3. App lista notificações.
4. Médico marca notificação como lida.

## Segurança
- Médico nunca informa `medicoId` para acessar dados de terceiros.
- O backend resolve o médico pelo usuário autenticado sempre que possível.
- Payloads de erro não devem expor stack trace ou SQL.
