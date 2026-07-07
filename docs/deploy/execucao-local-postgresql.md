# Execução local com PostgreSQL

Status: **implementado sem validação em ambiente real neste container** porque Docker/psql podem não estar disponíveis no executor automatizado.

## Subir banco

```bash
docker compose up -d
docker compose ps
docker exec -it plantaopro-postgres psql -U postgres -d plantaopro
```

O `docker-compose.yml` usa PostgreSQL 16, database `plantaopro`, volume `plantaopro_pgdata` e porta local `5432`. A senha `123456` é apenas padrão de desenvolvimento e pode ser alterada por `PLANTAOPRO_POSTGRES_PASSWORD`.

## Aplicar schema, migrations e seeds

Dependência: cliente `psql` instalado na máquina.

```bash
scripts/database/apply-local-postgres.sh --host localhost --port 5432 --database plantaopro --user postgres --password 123456
```

PowerShell:

```powershell
./scripts/database/apply-local-postgres.ps1 -HostName localhost -Port 5432 -Database plantaopro -User postgres -Password 123456
```

## Rodada 2026-07-07 — validação local PostgreSQL

Resultado: **Bloqueado por ambiente**.

- `docker compose config`: não executou porque `docker não encontrado`.
- `docker compose up -d`: não executou porque `docker não encontrado`.
- `bash scripts/database/apply-local-postgres.sh`: iniciou validação e falhou corretamente com `psql não encontrado`.

Comando reproduzível em ambiente preparado:

```bash
docker compose config
docker compose up -d
bash scripts/database/apply-local-postgres.sh
```
