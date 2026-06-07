# Relatórios SaaS

Relatórios SaaS devem apoiar gestão executiva e comercial.

## Relatórios previstos

- Clientes por status.
- Assinaturas por status.
- Uso do plano.
- Faturamento por competência.
- Inadimplência.
- Clientes em risco.
- Oportunidades de upgrade.
- Bloqueios por limite.

Exportações devem registrar auditoria e respeitar permissões multiempresa.

## Endpoints implementados

- `GET /api/relatorios/saas/clientes`.
- `GET /api/relatorios/saas/assinaturas`.
- `GET /api/relatorios/saas/faturamento`.
- `GET /api/relatorios/saas/inadimplencia`.
- `GET /api/relatorios/saas/uso-planos`.
- `GET /api/relatorios/saas/clientes-risco`.
- `GET /api/relatorios/saas/upgrade`.
- `GET /api/relatorios/saas/{tipo}/exportar` para CSV auditado.

No Web, os relatórios SaaS foram expostos em `Relatorios/Saas`, `Relatorios/SaasFaturamento`, `Relatorios/SaasClientesRisco` e `Relatorios/SaasUsoPlanos`.
