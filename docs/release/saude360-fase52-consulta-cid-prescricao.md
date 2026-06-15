# Release — Saúde 360 Fase 5.2

## Entregas

- Consulta médica com iniciar, finalizar, cancelar, histórico e resumo.
- Campos clínicos para anamnese, exame físico, diagnóstico/CID e conduta.
- Tabelas CID, favoritos e histórico de uso.
- Prescrição médica com itens, modelos, histórico e cancelamentos.
- Views MVC para consultas, CID e prescrições.
- Documentação clínica e roteiro de homologação.

## Banco de dados

Migration: `database/migrations/2026_saude360_cid_prescricao.sql`.

A migration é idempotente, usa schema `plantaopro`, `CREATE TABLE IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`, índices idempotentes e constraints em bloco `DO $$`.

## Observabilidade e segurança

- Endpoints retornam `ApiResponse<T>`.
- Ações críticas usam auditoria clínica.
- Logs técnicos evitam conteúdo clínico sensível.
- Perfis de recepção e financeiro devem permanecer sem acesso à evolução clínica completa.
