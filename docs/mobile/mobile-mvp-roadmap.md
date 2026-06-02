# API Mobile MVP — roadmap

## Pronto para homologação
- Login JWT mobile.
- Dashboard médico leve.
- Plantões disponíveis com paginação.
- Solicitação de plantão protegida por JWT e delegada ao serviço operacional de escala.
- Convites mobile paginados, isolados por médico/cliente, com aceite revalidando regras operacionais e recusa com motivo obrigatório.
- Minhas escalas, pagamentos e notificações.
- Perfil, disponibilidade e preferências.

## Guardrails de RC
- Sem `medicoId` no payload para o médico consultar dados próprios.
- Sem retorno de senha, hash, segredo, SQL bruto ou stack trace.
- Auditoria em aceite/recusa de convite e acesso negado por plano/cliente.

## Próxima etapa
- Push notifications.
- Chat contextual no app.
- Contestação de pagamento com anexos.
- Offline cache de agenda.
- Biometria local no app.
