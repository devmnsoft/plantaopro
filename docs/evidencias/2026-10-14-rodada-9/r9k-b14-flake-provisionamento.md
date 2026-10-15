# Rodada 9 — r9k: B14 flake — `ProvisionamentoB5Tests` crash-idempotente (limpeza de resíduos por CNPJ)

Data: 2026-10-15 · Base: `b02dca2` (r9j) · Ambiente local/homologação · Banco de teste: `plantaopro_test` (PostgreSQL 18) · Sem push/merge/publish.

## 1. Objetivo

Fechar a pendência R8-B14: `ProvisionamentoB5Tests` não era 100% crash-idempotente. A semente (`SementarAsync`) purgava apenas os fixtures de GUID fixo; se a execução era derrubada no meio de um teste (OOM / deadlock 40P01 com RAM baixa e hosts concorrentes — o mesmo ambiente que produziu o flake de `SaasComercialB4MatrizTests.Dispose()` em r9f/r9g), o mundo criado **por CNPJ** ficava órfão e a execução seguinte falhava com `"CNPJ já cadastrado."` até limpeza manual (registrado em R8-B14 §5).

## 2. Mudanças (arquivo único: `backend/PlantaoPro.Tests/ProvisionamentoB5Tests.cs`, +23 linhas)

1. **`CnpjsDosTestes`** — inventário completo (grep `"[0-9]{14}"`) dos 7 CNPJs criados pelos testes: P1 `99988877000166`, P2 e-mail dup `55443322000155`, P2 CNPJ dup / fixture `CnpjDup` `11222333000181`, P3 corrida `77665544000132`, P4 plano inativo `00112233000144`, P5 trial `33445566000177`, P5 manual `66554433000188`.
2. **`SementarAsync`** — antes da purga/insert dos GUIDs fixos, chama `LimparPorCnpjAsync(c)` para cada CNPJ do inventário. A fixture `B5ClienteDup` (que carrega `CnpjDup`) é removida nessa passada e recriada pelo insert da semente — sem efeito colateral.
3. **`LimparPorCnpjAsync`** — varredura final: delete direto de `cadastro_cliente_solicitacoes` órfãs do CNPJ (o `QueryFirstOrDefaultAsync` interno pega só a primeira solicitação; uma execução derrubada pode deixar resíduo fora do mundo completo).

Nenhuma mudança em C# de produção, API ou DB — somente infraestrutura de teste.

## 3. Evidência — experimento controlado (antes/depois) com órfãos plantados

Órfãos plantados em `plantaopro_test` (simulando execução derrubada entre criação e cleanup):
- `plantaopro.clientes(id='aaaaaaaa-…-eeeeeeeeeee1', cnpj='99988877000166')`
- `plantaopro.tenants(id='aaaaaaaa-…-eeeeeeeeeee2', cliente_id=cliente órfão, nome='B5 Orphan simulacao r9k')`
- `plantaopro.cadastro_cliente_solicitacoes(cnpj='77665544000132')`

| Execução | Resultado |
|---|---|
| **ANTES** (código HEAD via `git stash`) + órfãos plantados → `P1_Finalizar_CriaMundoTodo_E_AdminAutentica` | **FALHOU** com exatamente `CNPJ já cadastrado.` (400) — reproduz o modo de falha R8-B14 §5 |
| **DEPOIS** (`git stash pop` + rebuild) + mesmos órfãos → P1 isolado | **PASSOU** (2 s); contagem pós-execução dos 3 órfãos = `0\|0\|0` (varridos pela nova limpeza de semente) |
| Classe inteira isolada (depois) | **PASSOU 11/11** (3 s) |

### Descoberta colateral (comportamento existente, documentado)

Ao plantar os órfãos, a segunda inserção ativa de `cadastro_cliente_solicitacoes` com o mesmo CNPJ foi rejeitada: `duplicar valor da chave viola a restrição de unicidade "ux_cadastro_cliente_solicitacoes_cnpj_ativo"`. Ou seja, um índice único parcial (CNPJ normalizado × ativo) já impede duas solicitações **ativas** com o mesmo CNPJ — a varredura da mudança 3 cobre apenas o resíduo inativo/órfão, sem alterar comportamento.

## 4. Checks executadas

| Check | Resultado |
|---|---|
| `dotnet build PlantaoPro.sln -c Release` | **PASSOU** — 0 erros (warnings pré-existentes) |
| Suíte completa (`--no-build`, 1397 testes) | **1396 aprovados / 1 falha transitória**: `SaasComercialB4MatrizTests.B3_DecidirDowngrade_EmSolicitacaoDeCancelamento_409` — `Npgsql.PostgresException 40P01: impasse detectado` (mesma família ambiental do flake de r9j, agora em outro método da mesma classe — característica de deadlock concorrente sob RAM baixa, não do código do teste) |
| Retry isolado `SaasComercialB4MatrizTests` | **PASSOU 12/12** (~1 s) |
| `ProvisionamentoB5Tests` na suíte completa | 11/11 verdes (dentro dos 1396) |

Resultado efetivo da suíte: **1397/1397**.

## 5. Migrations

Nenhuma — mudança somente em teste.

## 6. Pendências remanescentes

- O deadlock 40P01 sob RAM baixa persiste como fator **ambiental** (purgas concorrentes entre classes na suíte paralela). O impacto em cascata que motivou B14 ("CNPJ já cadastrado" na execução seguinte) foi eliminado: após qualquer derrubada, o retry isolado de qualquer classe B5 é verde sem limpeza manual.
- Candidato registrado (fora do escopo desta fatia): tratar `SaasComercialB4MatrizTests` (e outras classes SaaS com fixtures por chave natural) com a mesma receita de pré-limpeza na semente — 2ª ocorrência de 40P01 nesta rodada (método diferente a cada execução).
