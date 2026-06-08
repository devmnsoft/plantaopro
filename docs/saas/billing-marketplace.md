# Billing, trial, inadimplência e marketplace

## Implementado
- Fluxos SaaS expõem faturas, assinatura, inadimplência e governança de módulos em APIs e páginas existentes.
- Fluxo comercial demonstrável identifica cobrança como `MANUAL_SANDBOX`, evitando simular gateway real.
- Módulos podem ser listados, detalhados e habilitados/desabilitados para tenant com auditoria.

## Pendências reais
- Garantir unicidade de fatura por competência em banco real.
- Homologar trial vencido, suspensão e reativação com massa de cobrança real.
- Amarrar liberação/bloqueio de módulos a todas as telas operacionais.
