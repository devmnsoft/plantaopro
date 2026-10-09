# R5-E13/P2 — Padronização do escopo canônico `tenant_id` no núcleo clínico

Data: 2026-10-09. Contexto: backlog P2 da matriz de aceite (`r5-entrega-matriz-aceitacao.md` §3),
dívida de canonicidade medida na evidência D11.

## 1. O problema (medido, não especulado)

Antes desta entrega, em `plantaopro_test`: `tenant_id` populado era **0** em todo o núcleo clínico
criado por rota (`pacientes` 0/17, `triagens` 0/1) — o kernel `Saude360ClinicalService` gravava
somente `cliente_id`. Pior: dois writers pontuais escreviam a **chave de escopo dentro da coluna
errada** (`tenant_id = cliente_id`), contaminando a semântica: `FavoritarCidAsync` e
`UsarModeloPrescricaoAsync`. O mapeamento de origem existe e foi verificado nos dois sentidos
(`clientes.tenant_id` ↔ `tenants.cliente_id`; demo: cliente …7501 ↔ tenant …7502).

## 2. O que foi implementado

| Camada | Arquivo | Mudança |
|---|---|---|
| Kernel escrita | `backend/PlantaoPro.Api/Saude360ClinicalService.cs` | Novo `ResolverTenantOrigemAsync(cn)`: o tenant de origem vem **exclusivamente do mapeamento persistido** — `clientes.tenant_id` do escopo; fallback `tenants.cliente_id=@escopo`; fallback o próprio tenant quando o escopo já é um tenant. Sem mapeamento → **NULL honesto**, nunca chute de claim nem cópia de `cliente_id` |
| BuildInsert | mesmo arquivo | Todos os **18 ramos** gravam `tenant_id` junto de `cliente_id` (17 recebiam `NULL` estrutural; o de unidades passou do claim cru para o mesmo resolutor — contrato E13-P1 da string SQL permanece íntegro) |
| Descontaminação | mesmo arquivo | `FavoritarCidAsync` e `UsarModeloPrescricaoAsync` deixaram de escrever `@tenantId` em `tenant_id` (padrão `,@tenantId,@tenantId,` varrido do arquivo, travado por asserção) |
| Históricos | mesmo arquivo | Inserções de `paciente_historico`, `painel_chamada_historico`, `prescricao_historico` e `consulta_historico` (as 4 tabelas de histórico do kernel com a coluna) agora registram a origem; `agendamento_historico`/`triagem_historico` não têm a coluna — ficam como estão |
| Backfill | `database/migrations/2026_10_v2336_e13_p2_backfill_tenant_id_nucleo_clinico.sql` | DO-block sobre as 25 tabelas do inventário do kernel, com guard por `information_schema` (só atualiza onde a coluna existe); regra idêntica à do código: `tenant_id := clientes.tenant_id` **somente onde o valor é NULL ou está contaminado (= `cliente_id` do registro)** — valores distintos intencionais nunca são tocados. Auto-registro `'v2336'` em `schema_migrations` (padrão v1310) |

Regra de decisão consciente: `BuildUpdate` **não** reescreve `tenant_id` — origem é imutável; a
leitura continua aceitando as duas chaves (compatibilidade do avaliador D11 preservada, sem
nenhuma consulta de leitura alterada).

## 3. Bug pre-existente medido pelos novos testes (corrigido na mesma v2336)

A rota genérica `POST api/convenios` quebrava em `42703: coluna "created_by" não existe`:
`convenios` e `planos_saude` eram as únicas tabelas do núcleo sem `created_by/updated_by/updated_at`,
embora o INSERT/UPDATE canônico do kernel escrevesse auditoria nelas. A v2336 inclui os
`alter table ... add column if not exists` (aditivo/idempotente), alinhando as duas com as outras 16.

## 4. Suite automatizada (hosts down)

Novos contratos: `backend/PlantaoPro.Tests/Saude360TenantScopeP2ContractTests.cs` — 5 Facts:
(1) todos os 18 inserts do `BuildInsert` contêm `tenant_id` e o arquivo não tem mais contaminação;
(2) o resolutor usa o mapeamento persistido com as 3 fontes canônicas e exatamente 3 chamadas
(criação genérica + os 2 writers descontaminados);
(3) a migração v2336 é condicional (`is null or = cliente_id`), cobre as tabelas-chave e auto-registra;
(4) integração: cliente mapeado → `POST` serviço grava `cliente_id` do escopo + `tenant_id` do
mapeamento; (5) integração: cliente **sem** mapeamento → `tenant_id` NULL honesto.

Total da suíte: 1178 → **1183/1183 verdes** (`dotnet test` completo com hosts down).
Nota de honestidade: `SaasGovernancaB6Rodada4Tests.Dispose` falhou 2 vezes em runs completos
consecutivos e passou isolado e em nova execução completa — categoria conhecida de corrida de limpeza no
banco compartilhado sob paralelismo xUnit (pré-existente à mudança; mesma família dos flaky B4/B5 já
catalogados na matriz).

## 5. Prova ao vivo (API :51976, gestor do tenant demo)

```
POST convenios   -> success=True
  linha: cliente_id=d3f6584c-...-7501 | tenant_id=d3f6584c-...-7502 | created_by=...-7511  # dupla chave + auditoria, via claims reais no login
POST planos-saude -> success=True        # rota antes quebrada em 42703 agora funciona
cleanup ok                               # fixtures removidos fisicamente
```

Backfill medido em `plantaopro_test` (fonte única de conclusão = banco):

| Tabela | antes | depois |
|---|---|---|
| `pacientes` | 0/17 com tenant | 13/17 com tenant demo; as 4 restantes pertencem a clientes sem mapeamento → NULL honesto |
| `triagens` | 0/1 | 1/1 |
| `clinica_unidades_atendimento` | 2/2 corretas (E13-P1) | 2/2 — backfill não tocou valores corretos |

Reexecução da migração: sem alterações (a condição só casa com NULL/contaminado);
`schema_migrations` registra `v2336` uma única vez (`INSERT 0 0` na segunda aplicação).

## 6. Limites conhecidos (sem maquiagem)

- As 4 linhas de `pacientes` sem mapeamento continuam NULL: preencher exigiria decisão de negócio
  (qual tenant elas seriam), não cabe chute técnico.
- Leituras permanecem filtro duplo (`tenant_id=@x or cliente_id=@x`) — a padronização é de **escrita**;
  a migração total de leitura pode ser futura com segurança porque novo legado já nasce canonizado.
- O banco principal `plantaopro` segue aguardando o upgrade formal P0 (v2323→v2336 em lote único).
- `repasses_medicos_clinicos` e os históricos sem a coluna ficam como estão (guard da DO-block).
