# Roadmap Mobile MVP — Release Candidate

## Pronto para homologação

- Login JWT mobile.
- Perfil, dashboard, plantões disponíveis, detalhes e solicitação.
- Convites com aceite/recusa.
- Minhas escalas, meus pagamentos e notificações.
- Disponibilidade e preferências como contratos MVP.
- Suporte mobile com listagem e abertura de chamado.
- Bloqueio amigável quando plano não permite mobile.

## Próximos incrementos controlados

1. Persistência completa de disponibilidade por faixa semanal.
2. Preferências especializadas por canal, horário silencioso e lembrete.
3. Timeline detalhada do chamado e resposta pelo app.
4. Contestação de pagamento pelo app.
5. Push notification e log de envio por dispositivo.
6. Cache offline somente leitura para agenda e pagamentos.

## Critérios para iniciar app nativo

- Swagger validado com usuário médico demo.
- Contratos acima congelados para MVP.
- Sem campos sensíveis nos DTOs.
- Teste sem token retornando 401.
- Teste de plantão/chamado de outro cliente retornando 403 ou não listado.
