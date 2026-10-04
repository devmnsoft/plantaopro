# Evidência WP-A4 — `CREATE ROLE` fora da aplicação operacional

**Data:** 2026-10-04 | **Banco:** PostgreSQL 18 (`plantaopro_test`) | **Repositório:** `C:\MNSOFT\plantaopro`

## Requisito

A criação de papéis do banco de dados **não pode** acontecer na aplicação operacional
(`PlantaoPro.Api` / `PlantaoPro.Web`) em tempo de execução. O DDL de papel deve existir
apenas nos artefatos de instalação/migração, executados pela ferramenta canônica
`PlantaoPro.Tools.Database` (ferramenta de dev/migração, não é o app operacional).

## Método (comandos reproduzíveis a partir da raiz do repositório)

```powershell
# 1) DDL de papel em qualquer código C# do backend (exclui bin/obj)
Get-ChildItem backend -Recurse -Include *.cs -File |
  Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' } |
  Select-String -Pattern 'CREATE ROLE|CREATE USER|CREATE GROUP|SET ROLE|GRANT ROLE|ALTER ROLE'

# 2) Padrões equivalentes em regex (backend/**/*.cs)
Select-String -Path backend -Pattern 'CREATE\s+(ROLE|USER|GROUP)\b' -Include *.cs -Recurse
Select-String -Path backend -Pattern '\b(SET ROLE|GRANT (ROLE|ADMIN OPTION)|ALTER ROLE)\b' -Include *.cs -Recurse

# 3) Onde CREATE ROLE realmente existe no banco de dados (fontes SQL)
Get-ChildItem database -Recurse -Include *.sql -File | Select-String -Pattern 'CREATE ROLE|CREATE USER|GRANT ROLE'
```

## Resultados

| Verificação | Resultado |
|---|---|
| DDL de papel em C# (API, Web, Application, Domain, Infrastructure, CrossCutting, Tests, Tools) | **0 ocorrências** |
| `CREATE ROLE` em SQL de migração | **1 ocorrência**: `database/migrations/2026_10_v2305_administrativo360_imutabilidade_por_papel.sql` (L31) |
| Cópias derivadas do mesmo bloco (não são fontes independentes) | `database/scrpt_completo.sql` (L6330) e `database/pgadmin/instalar_no_banco_atual.sql` (L6336) — gerados a partir das migrações |

O único papel criado em todo o banco é `plantaopro_maintenance`:

```sql
CREATE ROLE plantaopro_maintenance NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
```

- Bloqueado por `DO` (idempotente): já existe → ignora.
- Papel **marcador** (NOLOGIN NOINHERIT): não concede privilégios em tabelas; só
  habilita a função restrita `plantaopro.fn_adm360_bypass_habilitado()` quando o
  `session_user` é superuser ou membro do papel.
- Executado **somente** pelos comandos `install`/`upgrade` do
  `PlantaoPro.Tools.Database` (transação de migração), nunca em runtime do API/Web.

## Papel operacional vs. provisionamento

| Ambiente | Conta de conexão da aplicação | Quem cria papéis |
|---|---|---|
| Dev local / CI | `postgres` (superuser) via user-secrets (`plantaopro-api-development`, `plantaopro-web-development`) — `appsettings` sem segredos (regra S-11) | Só o tool de migração, durante `install`/`upgrade` |
| Produção (alvo) | Conta operacional provisionada externamente pelo administrador do banco | Administrador/provisionamento; se a manutenção exigir bypass de imutabilidade, a conta deve ser membro de `plantaopro_maintenance` (sem conceder `CREATEROLE` à aplicação) |

A aplicação usa apenas `SELECT set_config('plantao.bypass_imutabilidade_adm360','on',true)`
na mesma transação da escrita protegida — privilégio comum a qualquer papel não-superuser
(PG18: GUC placeholder de 2 segmentos), por isso a restrição está dentro dos triggers
(v2305) e não depende de `GRANT ... SET ON PARAMETER` (ver comentário da migração e
evidência `wpa4-evidencia-2305-saida.log`).

## Conclusão

**Atendido.** Nenhum `CREATE ROLE`/`CREATE USER`/`ALTER ROLE`/`GRANT ROLE`/`SET ROLE`
em código C# da aplicação ou dos tools; o único DDL de papel está na migração v2305,
executado exclusivamente no caminho de instalação/migração, com o `CREATEROLE` exigido
do executor do tool (superuser em dev/CI) e não da aplicação operacional.
