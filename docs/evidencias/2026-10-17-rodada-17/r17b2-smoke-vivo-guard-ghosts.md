# R17b2 — Smoke vivo: fantasmas do catálogo guard (D1/D2/D3)

**Data:** 2026-10-17 · **Slice:** r17b2 (plano em `r17a0-auditoria-inicial-plano-sprint.md` §4) · **Commits cobertos:** `34f2a42` (r17b1)

## 0. Ambiente e método

- Api `http://localhost:51976` + Web `http://localhost:52976`, `dotnet run -c Release --no-build` sobre o binário do gate do r17b1 (build `--no-incremental` 0E/80W, suíte 1489/1489).
- Browser desktop controlado por script (DOM capture via evaluate); logs de servidor preservados (`%TEMP%\opencode\sh_*.out`).
- Contas (`docs/usuarios-teste.md`): médico `medico@plantaopro.local` (user `…ca012`, tenant `…ca001`) / SV admin `admin.sistemavertical@plantaopro.local` (user `…ca310`, tenant `…ca301`).
- Banco: grant reversível via 1 linha em `tenant_modulos` com tag `HOMOLOG_R17`; verificação de efetividade pela função canônica `modulos_efetivos(uuid)` (v2339).

## 1. S1 — `MINHA_AGENDA` como módulo comum (D2): médico

| Passo | Resultado vivo | Veredito |
|---|---|---|
| Login médico | landing `/MeuDia` — log Web: `Módulo do destino do perfil não habilitado (MEDICO_AREA); usando Meu Dia como landing.` (fallback wpa1) | ✓ esperado |
| Cards do Meu Dia (`@if(isPhysician)`) | 3 cards renderizados: `/MinhaAgenda/Index`, `/MinhaAgenda/Presencas`, `/MinhaAgenda/MeusPagamentos` | ✓ esperado |
| **Antes do fix** (binário pristine `505d18a` do stash, captura incidental valiosa) | clique "Minha Agenda" ⇒ `AccessDenied?module=MINHA_AGENDA&reason=MODULO_NAO_CONTRATADO` — guard log `Controller:MinhaAgenda Action:Index Modulo:MINHA_AGENDA Acao:VER` | ✓ reprodutível |
| **Depois do fix** (binário r17b1) | `/MinhaAgenda/Index` ⇒ **200** (view "Meu Dia" hero); `/MinhaAgenda/Presencas` ⇒ **200** ("Execução dos meus plantões"); `/MinhaAgenda/MeusPagamentos` ⇒ **200** ("Meus demonstrativos") | ✓ D2 ao vivo |

Nota de dados (não-regressão da rodada): no usuário-homologação, o sub-endpoint `api/medico-area/presencas` respondeu 404 `Profissional não encontrado` (payload amigável; view tratou com aviso) — o card/página ainda renderiza 200. É estado do seed deste médico, não efeito do catálogo.

## 2. S3 — `MEDICO_AREA` contratado-somente (D3)

- GET `/MedicoArea/Index` como médico ⇒ **302** `AccessDenied?module=MEDICO_AREA&reason=MODULO_NAO_CONTRATADO&retorno=%2FMedicoArea%2FIndex`; guard log idêntico em `Modulo:MEDICO_AREA`. O motivo canônico permanece honesto (stub Fase2 intacto). ✓

## 3. S2 — `CENTRAL_ESCALA` → dono canônico `ESCALAS` (D1): SV admin

Sequência completa (todas as etapas com evidência acima em DOM + logs guard):

1. **Pré-grant** — `modulos_efetivos('…ca301')`: `ESCALAS = AUSENTE`. GET `/CentralEscala/Index` ⇒ 302 `AccessDenied?module=ESCALAS&reason=MODULO_NAO_CONTRATADO` (guard log `Controller:CentralEscala Action:Index Modulo:ESCALAS Acao:VER`). **O código fantasma `CENTRAL_ESCALA` não aparece mais em lugar nenhum da superfície** (dicionário, sidebar, denegação). ✓
2. **Grant reversível** — insert 1 linha `tenant_modulos` (id `673d67e6-c8be-444b-b1b2-bd45290ef4b1`, tag `HOMOLOG_R17`, modulo_id `27745fa8-…` = `ESCALAS` ativo em `modulos_sistema`). `modulos_efetivos` pós: `ESCALAS` presente. ✓
3. **Sem re-login** — mesmo cookie/sessão ⇒ `AccessDenied?module=ESCALAS&reason=PERMISSAO_NEGADA`. Leitura: a porta 1 (módulo) abriu via live-check efetivo; a porta 2 (permissão `ESCALAS.VER` nas claims congeladas do login) ainda negava. Confirma a semântica de duas linhas de defesa e que as claims são snapshot de login (grants `ESCALAS.*` dormentes do perfil só entram nas claims no próximo login — consistente com F3 do r17a0). ✓ comportamento canônico
4. **Pós re-login** — `/CentralEscala/Index` ⇒ **200**; sidebar agora exibe **Cobertura** e **Escalas** (gate `Can("ESCALAS")`). A BFF chamando `api/central-escala/resumo` recebeu **500** do backend e a view renderizou o empty-state com mensagem honesta (`Não foi possível carregar a central operacional no momento`), sem tela em branco. (Classificação do 500: §5.) ✓ guard/contrato
5. **Rollback** — `DELETE` da linha única; `modulos_efetivos` de volta a `AUSENTE` (net-zero na função canônica). Re-login ⇒ mesma reprovação do passo 1, sem link no sidebar. ✓ net-zero

### Detalhe operacional capturado
GET `/CentralEscala` (sem `/Index`) responde **404 "Não encontrado"** — controller sem attribute-route não casa a rota sem action default. Impacto real nulo: todo link é gerado por tag-helper (`asp-controller="CentralEscala" asp-action="Index"` → `/CentralEscala/Index`). Registro p/ eventual normalização no backlog (fila R18, junto com o F7 — é mais um item de "superfície de Cobertura/Operação").

## 4. Lixo de processo (lição de casa, versionada)

O primeiro ciclo do smoke rodou acidentalmente sobre binários do build pristine (`git stash push/pop` + build limpo reescreveram o `bin/Release` entre o gate e a subida dos apps; `dotnet run --no-build` usou o que estava lá). Consequência útil: produziu a **reprodução "antes do fix"** do item S1 de forma orgânica. Correção: rebuild + restart antes de repetir. Regra reforçada: **entre `dotnet build` e `dotnet run --no-build` não pode haver outro build** (especialmente após `stash`/`checkout`).

## 5. Classificação dos avisos/erros observados (sem regressão da rodada)

| Item | Medição | Classificação |
|---|---|---|
| Build gate r17b1 | `--no-incremental` 0 erros / 80 avisos; comparativo chave-a-chave vs build pristine `505d18a`: **0 novos / 0 sumidos** | baseline íntegro (snapshot antigo `r16b1_warn_base.txt` era extração lossy por quebra de linha de mensagens xUnit longas — documentado, sem ação) |
| Suíte full | 1489/1489 (baseline 1476 + 13 novos fatos) | verde |
| Flake `Administrativo360OrganizacaoTests.Resumo_…` (Expected 6 / Actual 5) | caiu uma vez na suíte paralela, passou na primeira; **isolate 5× = 5× aprovado** | flake de família conhecida (paralelismo), pré-existente; não bloqueia commit |
| `api/central-escala/resumo` 500 (SV, pós-grant) | view 200 com empty-state honesto | ver §3 passo 4; diagnóstico de raiz vai para fila R18 (item novo, baixo risco — camada de dados/backend de escala, fora do escopo de catálogos da R17) |

## 6. Veredito r17b2

**Aprovado.** As três decisões da rodada estão vivas e verificadas ponta a ponta com motivos canônicos e sem resíduo em banco (net-zero via `modulos_efetivos`). Nenhum código foi alterado nesta slice; esta evidência é o artefato versionado do smoke.
