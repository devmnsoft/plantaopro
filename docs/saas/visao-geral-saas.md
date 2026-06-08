# Visão geral SaaS

Esta evolução consolida PlantãoPro como SaaS multiempresa com tabelas incrementais para clientes, planos, recursos, assinaturas, uso, faturas, cobranças, bloqueios, alertas, saúde do cliente, jornada comercial, comercial SaaS, Customer Success, LGPD, ajuda e auditoria.

## Componentes funcionais

- API de jornada do cliente em `/api/jornada-clientes`.
- API comercial em `/api/comercial`.
- API LGPD em `/api/lgpd`.
- API de inteligência em `/api/inteligencia` e dashboard SaaS.
- Web com JornadaClientes, Comercial, Lgpd, Inteligência, SaaS Dashboard e Ajuda.
- Guardas SaaS para limites de médicos, hospitais, plantões, mobile, BI e relatórios avançados.

## Migração funcional consolidada

A migração `database/migrations/2026_plantao_pro_saas_inteligente_funcional.sql` consolida as estruturas de SaaS, comercial, jornada, Customer Success, LGPD, ajuda interativa, logs e auditoria. O script é incremental, usa `CREATE TABLE IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`, índices idempotentes e constraints protegidas por bloco `DO $$` com consulta a `pg_constraint`.
