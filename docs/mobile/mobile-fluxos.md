# Fluxos Mobile

## Fluxo 1: Login e contexto
1. Usuário autentica em `/api/mobile/auth/login`.
2. App carrega `/api/mobile/me` para perfil, permissões e cliente.
3. Dashboard inicial em `/api/mobile/dashboard`.

## Fluxo 2: Captura de plantão
1. Listar em `/api/mobile/plantoes-disponiveis`.
2. Ver detalhe em `/api/mobile/plantoes/{id}`.
3. Solicitar vaga em `/api/mobile/plantoes/{id}/solicitar`.
4. Exibir toast para sucesso/erro com motivo de bloqueio (conflito, duplicidade, permissão).

## Fluxo 3: Gestão de convites
1. Listar convites em `/api/mobile/convites`.
2. Abrir convite em `/api/mobile/convites/{id}`.
3. Aceitar ou recusar (com motivo quando exigido).

## Fluxo 4: Financeiro médico
1. Listar pagamentos.
2. Abrir detalhe de pagamento.
3. Contestar pagamento com justificativa.
4. Acompanhar atualização por notificações.

## Fluxo 5: Preferências e disponibilidade
1. Carregar disponibilidade/preferências atuais.
2. Editar grade semanal e turnos.
3. Persistir via PUT com validações e feedback visual.
