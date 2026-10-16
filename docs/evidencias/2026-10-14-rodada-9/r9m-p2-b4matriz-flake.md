# Rodada 9 — r9m-P2: `SaasComercialB4MatrizTests` crash-idempotência + mitigação de 40P01/40001 transitórios

Data: 2026-10-15 · Base: `5f3fed8` (fechamento rodada 9) · Ambiente local/homologação · Banco de teste: `plantaopro_test` (PostgreSQL 18) · Sem push/merge/publish.

## 1. Objetivo

Fechar o candidato registrado em r9k §6: aplicar a mesma receita de r9k/B14 à `SaasComercialB4MatrizTests`, a classe com mais flakes de deadlock 40P01 nesta rodada (r9f/r9g: `Dispose`; r9j: suíte completa; r9k §4: `B3_DecidirDowngrade…_409`). Padrão observado em todas as ocorrências: impasse transitório sob suíte paralela com RAM baixa (purgas concorrentes entre classes) e **retry isolado sempre verde (12/12)**.

## 2. Mudanças (arquivo único: `backend/PlantaoPro.Tests/SaasComercialB4MatrizTests.cs`, diff ≈ +30/-11)

1. **Pré-limpeza por chave natural na `SementarAsync`** — dentro da transação da semente, antes de `PurgeSql`/`InsertSql`:
   ```sql
   delete from plantaopro.usuarios where lower(email_normalizado)='b4-usuario@exemplo.test';
   ```
   Justificativa: o único fixture desta classe com chave natural de unicidade é o e-mail do usuário (índice único parcial `ux_usuarios_email_normalizado ON usuarios(lower(email_normalizado)) WHERE reg_status='A'`); um órfão de execução derrubada (com GUID diferente do fixture) colidiria no reinsert. `tenants.codigo` não possui índice único (verificado no catálogo) → não há outra chave natural a cobrir. Demais linhas seguem cobertas pela purga por GUID fixo.
2. **`ExecutarComRetryImpasseAsync(Func<Task> acao)`** — envolve os corpos de `SementarAsync` e `PurgarAsync`: em `Npgsql.PostgresException` com `SqlState` `40P01` (deadlock detectado) ou `40001` (falha de serialização), aguarda `Task.Delay(250)` e reexecuta uma única vez; qualquer outra exceção propaga inalterada.
3. **Doc comment da classe** — parágrafo r9m-P2 documentando a receita e sua justificativa.

Nenhuma mudança em C# de produção, API ou DB — somente infraestrutura de teste. Número de métodos de teste inalterado.

## 3. Evidência

| Execução | Resultado |
|---|---|
| Classe isolada (`--filter FullyQualifiedName~SaasComercialB4MatrizTests`, Release) após a mudança | **PASS 12/12** (≈1 s) |
| Histórico da rodada (contexto): retries isolados após cada flake | 12/12 em r9f/r9g, r9j e r9k (§4) |

O wrapper de retry torna o fator ambiental autocontido dentro da classe: sob suíte paralela, semente/purga reexecutam uma vez antes de falhar — coerente com o comportamento observado de retry isolado sempre verde. A pré-limpeza garante idempotência pós-derrubada na única chave natural com índice único (receita de r9k, adaptada: aqui a chave é e-mail, não CNPJ).

## 4. Checks

| Check | Resultado |
|---|---|
| Classe isolada (Release) | **12/12 PASS** |
| Contratos i18n direcionados (R7DF6/R7E1/R7DF1/R7DF4, 14 testes) | **14/14 PASS** |
| Suíte completa (Release) | **1398/1398 PASS** (nova base = 1397 + 1 fact novo do r9m-P4; sem flake 40P01 nesta execução) |

## 5. Migrations

Nenhuma — mudança somente em teste.

## 6. Pendências remanescentes

- O fator ambiental 40P01/40001 persiste como fator da suíte paralela em geral (a mitigação limita o impacto a um retry único dentro desta classe). Se outra classe SaaS com fixtures por chave natural flakar em rodadas futuras, a receita é a mesma (inventariar chaves naturais com índice único parcial + pré-limpeza na semente + wrapper de retry).
