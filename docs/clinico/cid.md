# CID no Saúde 360

A tabela oficial do módulo é `plantaopro.cid_tabela`. A listagem usa `codigo` e `descricao`; não existe dependência de coluna `nome`.

Campos mínimos: `id`, `codigo`, `descricao`, `categoria`, `capitulo`, `status`, `reg_status` e `reg_date`.

A migration `database/migrations/2026_fix_cid_tabela_schema.sql` padroniza o schema e adiciona CIDs demo de forma idempotente.
