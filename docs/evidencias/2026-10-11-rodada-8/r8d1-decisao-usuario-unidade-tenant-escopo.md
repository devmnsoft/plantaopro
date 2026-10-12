# R8-D1 — Decisão: escopo operacional do usuário = tenant inteiro (sem mapeamento usuário×unidade)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — DECISÃO sobre o item da fila `r8a0` §4 classificado na matriz §18 como "decisão (necessidade real?)"
- **Data:** 2026-10-12
- **Decisor:** produto (pergunta registrada na sessão de rodada, após os fatos canônicos abaixo)
- **Status:** FECHADA — linha da matriz §18 → "aceitável (decisão R8-D1)".

## 1. Achado (fonte canônica)

Matriz `r8a0-auditoria-inicial-matriz-estado.md`, linha "Usuário com acesso a 1..N unidades no tenant" (§18):

> "**não há** mapeamento usuário×unidade (busca exaustiva) nem claim de unidade | parcial | restrição granular por pessoa ausente — usuário opera em todas as unidades do tenant | **decisão (necessidade real?)** | novo teste se mapear".

Varredura `r8a0-varredura-vinculos-unidades.md`, A7 OBS:

> "Um usuário opera em **todas** as unidades do tenant: não há mapeamento usuário×unidade (busca `usuario_unidade|user_unidade|unidade_usuario|usuario_hospital` → 0 matches). A restrição granular multi-unidade por pessoa está ausente; a mensagem de 'vínculo com a unidade' é sustentada apenas pela guarda de tenant."

## 2. Fatos comprovados (HEAD)

- Modelo multi-unidade existe e é escrito: unidades = `hospitais` (per tenant) + `clinica_unidades_atendimento`/`clinica_salas_atendimento`/`clinica_setores_atendimento` (rota R5-E13, teste `OnboardingE13UnidadesContractTests`).
- A sessão **não carrega claim de unidade**: claims = `uid/sub, role(s), access_scope, context_mode, cliente_id, tenant_id, permissions, module...` (`SessionClaimsBuilder.cs:70-93,117-126`).
- Enforcement server-side de "unidade" = **guarda de tenant sobre a entidade**: `TenantGuardService.PodeAcessarHospitalAsync` → `PodeAcessarEntidadePorClienteAsync(usuarioId, hospitalId, "hospitais","id")` (`TenantServices.cs:95-98`); usada em `Fase4OperationalController.cs:111,123-124,141-142`, `ExecutionConferenceService.cs:39,132,196`, `OperationalReportService.cs:36`; global admin passa sempre.
- Zero tabelas de mapeamento (busca exaustiva dos 4 termos → 0 matches fora de comentários).

### Auditoria de mensagens (feita para esta DEC)

Nenhuma mensagem 403/409 promete vínculo **por pessoa** além do que o guard garante:

| Mensagem | Local | Nível real |
| --- | --- | --- |
| "Você não possui vínculo com a unidade desta solicitação." | `Fase4OperationalController.cs:124,142` | descreve o 403 do guard de tenant sobre o hospital |
| "Você não possui vínculo com a unidade desta execução." | `ExecutionConferenceService.cs:133,197` | idem |
| "Seu vínculo ou elegibilidade profissional mudou." | `Data.cs:1536` | nível **profissional** (médico×usuário×CRM), não de unidade |
| "Usuário não possui vínculo ativo com este tenant." | `SecurityAdministrationServices.cs:119` | explicitamente tenant |
| "Você não possui vínculo ou permissão para esta decisão." | `ConferenciaExecucaoController.cs:61` (Web, fallback 403) | genérica |

## 3. Opções avaliadas

- **(a) tenant = escopo**: documentar como design; custo ~zero; consistente com o enforcement atual e com as mensagens (nenhuma promete mais do que o guard garante).
- **(b) mapeamento usuário×unidade**: nova M:N + atribuição administrativa + verificação server-side por unidade + testes. Só vale com necessidade real de restringir por pessoa em tenant multi-unidade (ex.: grupo com hospitais distintos onde o acesso deve ser limitado a um hospital específico).

## 4. Decisão

**(a)** — O tenant é o escopo operacional dos seus usuários; **não há** restrição granular por pessoa entre unidades do mesmo tenant. Sem alteração de código nesta DEC (a auditoria de mensagens não exigiu ajuste de texto; se vier a necessidade real, o ajuste de wording acompanha a fatia).

## 5. Consequências e critério de reabertura

- Linha 116 da matriz → **aceitável (decisão R8-D1)**.
- Contrato i18n F6 inalterado (244 chaves Adm360) — nenhuma chave tocada.
- Critério de reabertura: surgir necessidade real de restringir acesso por pessoa em tenant multi-unidade → fatia com M:N `usuario_unidades` (+ unidade), write path administrativo, verificação server-side nos endpoints por unidade (claim opcional) e teste de jornada; a mensagem 403 passa a refletir o guard de unidade na época.
- Enforcement permanece a guarda de tenant sobre a entidade; o bypass de global admin mantém-se.
