# Visão geral SaaS PlantãoPro

Esta rodada adiciona módulos funcionais para venda, jornada do cliente, inteligência SaaS baseada em regras, LGPD prática e ajuda interativa.

## Módulos entregues

- API LGPD em `/api/lgpd`.
- API comercial em `/api/comercial`.
- API jornada em `/api/jornada-clientes`.
- API inteligência compatível em `/api/inteligencia`.
- API ajuda em `/api/ajuda`.
- Web: `Lgpd`, `Comercial`, `JornadaClientes`, `Ajuda` e atalhos no layout.

## Banco

A migration `database/migrations/20260607_saas_lgpd_jornada_comercial_ajuda.sql` cria tabelas incrementais no schema `plantaopro` com seeds mínimos de LGPD, desconto e manual.
