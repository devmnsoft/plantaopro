# Fluxos Mobile — Release Candidate

## Fluxo 1 — Solicitação de plantão

1. Médico autentica no app por `/api/mobile/auth/login`.
2. App carrega `/api/mobile/dashboard`.
3. Médico abre `/api/mobile/plantoes-disponiveis`.
4. Médico consulta `/api/mobile/plantoes/{id}`.
5. Médico solicita em `/api/mobile/plantoes/{id}/solicitar`.
6. Backend valida plano mobile, identidade do médico, cliente, duplicidade, conflito e vaga.
7. A solicitação aparece em `/api/mobile/minhas-escalas`.
8. Coordenação confirma pela Web.
9. Médico recebe notificação em `/api/mobile/notificacoes`.

Critério de aprovação: médico não acessa plantão de outro cliente e não consegue solicitar plantão duplicado ou conflitante.

## Fluxo 2 — Convite

1. Coordenação convida médico elegível pela Web.
2. App lista `/api/mobile/convites`.
3. Médico aceita `/api/mobile/convites/{id}/aceitar` ou recusa `/api/mobile/convites/{id}/recusar` com motivo.
4. Backend revalida vaga e conflito no aceite.
5. A ação registra auditoria e gera notificação operacional.

Critério de aprovação: convite expirado, já respondido ou de outro médico não pode ser aceito.

## Fluxo 3 — Pagamento

1. Coordenação marca escala como realizada.
2. Financeiro gera e confirma pagamento.
3. Médico consulta `/api/mobile/meus-pagamentos`.
4. Médico vê status, valor previsto, valor pago, forma e data quando disponíveis.

Critério de aprovação: médico vê somente pagamentos próprios.

## Fluxo 4 — Suporte mobile

1. Médico abre tela de suporte no app.
2. App lista `/api/mobile/suporte/chamados`.
3. Médico cria chamado com título e descrição.
4. Backend gera protocolo `SUP-*`, registra cliente/usuário e audita a criação.
5. Admin/CS trata o chamado na operação Web.

Critério de aprovação: chamado de outro usuário/cliente não aparece para o médico.
