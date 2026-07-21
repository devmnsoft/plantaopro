#!/usr/bin/env bash
set -euo pipefail
: "${PLANTAOPRO_CONNECTION_STRING:?defina PLANTAOPRO_CONNECTION_STRING para PostgreSQL real de teste}"
export ASPNETCORE_ENVIRONMENT=Development
psql "$PLANTAOPRO_CONNECTION_STRING" -v ON_ERROR_STOP=1 <<'SQL'
DROP SCHEMA IF EXISTS plantaopro CASCADE;
CREATE SCHEMA plantaopro;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TABLE plantaopro.usuarios(id uuid primary key default gen_random_uuid(), nome text not null, email text not null, senha_hash text not null, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
CREATE TABLE plantaopro.perfis(id uuid primary key default gen_random_uuid(), nome text not null, reg_status char(1) not null default 'A', reg_date timestamptz not null default now());
INSERT INTO plantaopro.usuarios(nome,email,senha_hash) VALUES('Legado','legado@example.com','legacy-hash');
INSERT INTO plantaopro.perfis(nome) VALUES('Administrador Global');
SQL
dotnet run --project backend/PlantaoPro.Tools.Database -- repair-identity-schema
dotnet run --project backend/PlantaoPro.Tools.Database -- verify | tee artifacts/identity-v1189-verify.txt
grep -q IDENTITY_SCHEMA_READY artifacts/identity-v1189-verify.txt
