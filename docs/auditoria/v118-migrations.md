# v1.18 — Relatório de migrations

Data UTC: 2026-07-21.

## Implementado

- Criado executor canônico `scripts/apply-canonical-migrations.sh`.
- Criada tabela de controle `plantaopro.schema_migrations` antes da cadeia de scripts.
- Cada script recebe um identificador ordenável e é pulado quando já registrado na tabela de controle.
- O pipeline passou a usar variáveis `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER` e `PGPASSWORD` para o `psql`.
- As chamadas `psql` foram padronizadas com `-v ON_ERROR_STOP=1 -f caminho/do/script.sql`.
- O workflow agora testa instalação limpa, upgrade de versão anterior e reexecução idempotente da cadeia.

## Parcial

- A idempotência interna continua dependendo dos scripts SQL existentes. O executor evita reexecução de scripts já registrados, mas scripts simulados como pré-versão no job de upgrade ainda precisam ser idempotentes por si só quando executados diretamente.

## Futuro

- Dividir scripts muito grandes em migrations menores e com checksums bloqueantes quando houver divergência entre o arquivo aplicado e o arquivo atual.
