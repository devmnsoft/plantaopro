# QA Billing, Planos e Bloqueios

## Correções desta rodada

- Menu Admin SaaS e menu Financeiro agora direcionam faturas para `Billing/Faturas`.
- `Billing/Faturas` redireciona para a tela consolidada de faturamento SaaS, preservando controller sem link quebrado.
- `MinhaAssinatura` entrou no guard de rotas.

## Checklist

- [ ] Plano atual visível.
- [ ] Assinatura atual visível.
- [ ] Uso por usuários, médicos, hospitais e plantões visível.
- [ ] Faturas listadas com status.
- [ ] Inadimplência bloqueia recursos conforme regra.
- [ ] Trial mostra prazo e CTA.
- [ ] Upgrade/downgrade/cancelamento possuem fluxo sem exception técnica.
- [ ] API bloqueia limites além da UI.
