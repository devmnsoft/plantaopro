# Faturamento SaaS

## Objetivo
Orientar homologação e operação do faturamento SaaS básico do PlantãoPro na Beta Comercial Controlada.

## Entidades e status
- Plano: define limites comerciais, mobile, BI, relatórios e integrações.
- Assinatura: vincula cliente ao plano; deve existir no máximo uma ativa por cliente.
- Fatura SaaS: status `ABERTA`, `PAGA`, `VENCIDA`, `CANCELADA` ou `EM_CONTESTACAO`.
- Pagamento SaaS: registra valor, data, forma e referência de liquidação.

## Regras obrigatórias
- Plano inativo não gera nova assinatura.
- Cliente suspenso/cancelado não opera.
- Fatura paga exige valor, data e forma.
- Cancelamento exige justificativa.
- Contestação exige motivo.
- Fatura vencida alimenta inadimplência e Customer Success.
- Ações críticas registram auditoria e notificação.

## Como testar
1. Criar plano ativo.
2. Criar cliente e assinatura ativa.
3. Gerar fatura mensal.
4. Marcar como paga com valor/data/forma.
5. Criar fatura vencida ou simular inadimplência.
6. Suspender cliente com justificativa.
7. Validar bloqueio de publicação de plantão.
8. Reativar cliente e validar liberação.

## Resultado esperado
Faturamento básico demonstrável, auditável e conectado aos bloqueios comerciais do SaaS.
