# Visão geral SaaS PlantãoPro

Esta rodada adiciona módulos funcionais para LGPD, jornada do cliente, comercial SaaS, inteligência baseada em regras e ajuda interativa.

## Módulos implementados

- API LGPD em `/api/lgpd` com política, consentimentos, solicitações, exportação e anonimização controlada.
- API Jornada do Cliente em `/api/jornada-clientes` com avanço, retrocesso, eventos, tarefas e funil.
- API Comercial em `/api/comercial` com leads, oportunidades, propostas, funil, previsão de receita e sugestão de plano.
- API Ajuda em `/api/ajuda` com tópicos, artigos, busca, checklists e feedback.
- Web MVC para `Lgpd`, `JornadaClientes`, `Comercial` e `Ajuda`.

## Banco

A migração incremental `backend/sql/20260607_saas_lgpd_jornada_comercial_ajuda.sql` cria as tabelas no schema `plantaopro` e seeds mínimos para política LGPD, bases legais, retenção e ajuda.
