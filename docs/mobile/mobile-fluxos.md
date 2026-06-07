# Fluxos Mobile MVP

## Login e sessão
1. Médico abre app.
2. App chama `POST /api/mobile/auth/login`.
3. Token é salvo em SecureStore.
4. App chama `GET /api/mobile/me`.
5. App carrega dashboard.

Resultado esperado: sessão autenticada, sem exposição de token em log ou tela.

## Solicitação de plantão
1. Médico abre Plantões Disponíveis.
2. App lista `GET /api/mobile/plantoes-disponiveis`.
3. Médico abre detalhe `GET /api/mobile/plantoes/{id}`.
4. Médico confirma solicitação.
5. API valida vaga, duplicidade, especialidade, médico ativo e conflito crítico.
6. API registra auditoria e gera notificação.

Resultado esperado: escala SOLICITADA ou mensagem amigável de bloqueio.

## Convite
1. Médico abre Convites.
2. App lista `GET /api/mobile/convites`.
3. Médico aceita ou recusa.
4. Aceite revalida vaga/conflito; recusa pode exigir motivo.
5. API registra auditoria e notificação.

## Pagamentos
1. Médico abre Meus Pagamentos.
2. App chama `GET /api/mobile/meus-pagamentos`.
3. App exibe pendentes, confirmados, cancelados e contestados.

## Notificações
1. App carrega `GET /api/mobile/notificacoes`.
2. Médico abre item e chama `PUT /api/mobile/notificacoes/{id}/lida`.
3. Badge usa `GET /api/mobile/notificacoes/contador`.

## Suporte
1. Médico abre Suporte.
2. App lista chamados.
3. Médico cria chamado via `POST /api/mobile/suporte/chamados`.
4. Chamado crítico gera alerta operacional.
