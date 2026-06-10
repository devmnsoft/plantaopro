# Saúde 360 — prescricao medica

## Objetivo

Este módulo faz parte da Fase 5 do PlantãoPro Saúde 360 e opera com escopo por tenant, permissões por perfil/plano e auditoria.

## Implementação

- API autenticada no backend ASP.NET Core.
- Controller Web MVC com ações reais e service HTTP registrado em DI.
- Tabelas PostgreSQL no schema `plantaopro` com auditoria mínima (`created_by`, `updated_by`, `created_at`, `updated_at`, `reg_date`, `reg_status`).
- Índices por campos operacionais relevantes, incluindo `cliente_id`, `status` e `reg_date`.

## Segurança e LGPD

- Dados são filtrados por `cliente_id`/tenant.
- Ações sensíveis registram auditoria central.
- Conteúdo clínico sensível não deve ser escrito em logs técnicos.
- Perfil financeiro não deve acessar evolução clínica.

## Pendências reais

- Homologar jornada completa em ambiente com banco aplicado.
- Refinar campos específicos por clínica e protocolo assistencial.
- Adicionar integrações externas somente após validação jurídica/operacional.
