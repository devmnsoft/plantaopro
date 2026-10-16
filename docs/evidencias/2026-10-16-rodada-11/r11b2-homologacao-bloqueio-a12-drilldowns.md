# Rodada 11 — r11b2: Homologação webview — bloqueio imediato A12 ao vivo + janela de drill-downs

- **Data:** 2026-10-16 · **Base:** `04a4264` (r11b1) · **Slice:** `r11b2` · Ambiente local/homologação.
- **Ambiente:** Web `http://localhost:52976` + API `http://localhost:51976` (Development, HTTP), build Release **pós-r11b1** (o writer de bloqueio estava no binário), banco `plantaopro` (PG18), conexão via env `ConnectionStrings__Default` KEY=VALUE (receita r9l — scripts `r9l_api.cmd`/`r9l_web.cmd`). Sem push nesta fatia.
- **Sessão:** `admin.sistemavertical@plantaopro.local` (perfil ADMINISTRADOR, tenant "Sistema Vertical" `8b0c8e74-a81b-4ea2-b499-94755a1ca301`), login por form (`#Email`/`#senha` + submit; o primeiro submit pós-startup da API pode falhar em silêncio — receita r9l confirmada: retry resolveu em 1 nova sessão, e login direto em `POST /api/auth/login` com `{Email, Senha}` deu `success:true` antes da navegação).

## 1. Objetivo e escopo

Item P2 da fila R10 (`r10z`): "homologação webview (CRUD Ocorrências Admin360 + clique nos 7 links drill-down)" + evidência visual do P1 entregue no r11b1 (bloqueio imediato aparecendo no saldo). Autorizado pelo usuário ("Homologação agora").

## 2. Mudança de dados (reversível — banco de homologação `plantaopro`)

O tenant SV tinha 3 produtos / 2 lotes mas **zero locais e zero movimentos** ADM360 — sem local, a ocorrência nem seria criável na UI. Seed tagada (script `r11b2_seed.sql`, executado com `ON_ERROR_STOP`):

- 2 locais: `SV-DEP-R11` (INTERNO, "Deposito Central R11 (homologacao)") e `SV-EXT-R11` (EXTERNO, "Externo R11 (homologacao)") — produto/lote reais do tenant (`Seroma de Teste p/ Lote`, lote `LT-2026-001`, validade 2027-01-31).
- 2 movimentos `ENTRADA`/`LIBERADO`: DEP **6**, EXT **4** (idempotency keys `r11b2-seed-dep`/`r11b2-seed-ext`, `origem_tipo='SEED_HOMOLOG_R11B2'`).

Saldo pós-seed verbatim: `DEP|LIBERADO|fisico=6|disp=6` · `EXT|LIBERADO|fisico=4|disp=4`.

Reverter (depois das capturas pendentes):
```sql
delete from plantaopro.adm360_ocorrencias where tenant_id='8b0c8e74-a81b-4ea2-b499-94755a1ca301';
delete from plantaopro.adm360_movimentos where tenant_id='8b0c8e74-a81b-4ea2-b499-94755a1ca301';
delete from plantaopro.adm360_locais where tenant_id='8b0c8e74-a81b-4ea2-b499-94755a1ca301' and codigo in ('SV-DEP-R11','SV-EXT-R11');
```

**Estado mantido de propósito:** ao fim da janela a segunda ocorrência segue **ABERTA** (bloqueio DEP 6 + EXT 2 ativo) para as capturas de pixel pendentes (§6); o rollback acima a remove junto.

## 3. Walkthrough ao vivo — CRUD Ocorrências + bloqueio imediato (P1 r11b1)

Fluxo executado pela UI (`/Administrativo360/Ocorrencias` → form "Abrir Ocorrência" → ações da linha), tenant SV, produto Seroma/LT-2026-001/local DEP, tipo `QUEBRA`, quantidade **8** (saldo total DISPONÍVEL do lote = 6+4 = 10 em 2 locais — escolha forçada para testar a frase da spec "pode abranger saldo interno e externo"):

| Passo (UI) | Situação (DOM) | Saldo no banco (verbatim, probe `adm360_saldos` + movimentos) |
|---|---|---|
| Abrir qtd 8 | linha `Em Aberto`, qtd 8,0000 | `DEP\|BLOQUEADO\|6\|disp=0` · `DEP\|LIBERADO\|0` · `EXT\|BLOQUEADO\|2\|disp=0` · `EXT\|LIBERADO\|2\|disp=2` — 4 pernas `BLOQUEIO_OCORRENCIA` (±6 DEP, ±2 EXT) com motivo `"Bloqueio imediato da ocorrencia QUEBRA - Caixa danificada... (ocorrencia 46c473f9-…)"` |
| Iniciar Tratamento (v1→2) | botão `Resolver` ativo | saldo idêntico — **bloqueio persiste em EM_TRATAMENTO** (nenhuma perna nova) |
| Resolver (v2→3) | badge `Resolvida` + botão `Encerrar` | 4 pernas espelhadas `LIBERACAO_OCORRENCIA` (motivo `"Liberacao do bloqueio da ocorrencia (ocorrencia …)"`); saldo volta a `DEP\|LIBERADO\|6\|disp=6` · `EXT\|LIBERADO\|4\|disp=4`, BLOQUEADO físico 0 — **ciclo net-zero** ("não apaga estoque" vale com bloqueio) |
| Encerrar c/ destino+justificativa (v3→4) | badge `Encerrada` + destino visível na linha: `Descarte assistido (homologacao R11)` | saldo idêntico ao inicial (8 pernas totais: 4 bloqueio + 4 liberação; TRATAR/ENCERRAR sem efeito) — idem ao teste automatizado r11b1 |

Ocorrência nº2 repetiu o abrir (qtd 8, id `0e7b058e-…`) mantendo o estado ABERTA para captura (§6) — mesmo padrão de pernas no verbatim final (probe registrado no log da sessão).

**Estoque (`/Administrativo360/Estoque?condicao=BLOQUEADO`):** linhas `… DEP … BLOQUEADO 6 0 0` e `… EXT … BLOQUEADO 2 0 0` — o estado BLOQUEADO é visível e filtrável na tela existente; o motivo do bloqueio (L296) vive nos movimentos (consultável), caveat de exposição já registrado no r11b1 §5.

## 4. Drill-downs §27-L208 (r10b3) — **ACHADO novo, reprodutível ao vivo**

Tentativa de clicar os 7 links (5 BI + 2 Premium) encontrava primeiro que as **duas telas-fonte são inacessíveis para TODO autenticado**:

| Navegação | Resultado ao vivo (verbatim) |
|---|---|
| `/Bi/Index` | redirect `Account/AccessDenied?module=CATALOGO_NAO_CONFIGURADO&reason=CATALOGO_NAO_CONFIGURADO` |
| `/DashboardsPremium/AdministradorCliente` | redirect idêntico (`CATALOGO_NAO_CONFIGURADO`) |

**Causa raiz:** `SaasRouteGuardFilter.ControllerModules` (catálogo guard→módulo) **não contém entrada para `BiController` nem para `DashboardsPremiumController`**; controllers fora do catálogo caem no redirect `CATALOGO_NAO_CONFIGURADO` (L179–185) **antes** de qualquer checagem de módulo/role — ou seja, vale inclusive para global admin. Nenhum `.cshtml` referencia esses controllers (`asp-controller="Bi"` / `"DashboardsPremium"` zero ocorrências) → as telas estão **dormente** (sem link de menu) — o que explica a não-detecção até hoje.

**Classe:** exatamente o padrão dos defeitos já corrigidos pela casa — R6-BlocoA item 1 (`Caixa` sem entrada no catálogo) e R7-E1 (`Usuario` idem). **Impacto:** baixa para usuários (tela nunca linkada), mas **bloqueia integralmente a homologação dos 7 drill-downs** (dívida §27-L208) e faria a página quebrar para todos no dia em que um menu as expuser. **Classificação:** P2 (defeito latente + bloqueio de pendência).

**Correção proposta (próxima fila):** 1) entradas `["Bi"]="BI"` e `["DashboardsPremium"]=<módulo a decidir>` no catálogo (com comentário de house-style citando esta evidência); 2) ação de permissão correspondente em `ResolvePermissionAction`; 3) decisão de exposição (menu) junto com o módulo dono — sem menu não há link que apontar; 4) contrato de teste estilo R6-BlocoA (todo controller linkável tem entrada no catálogo). Nota de escopo: o tenant SV **não tem** os módulos `BI`, `CLIENTES`, `ASSINATURAS`, `BILLING`, `ESCALAS`, `PLANTOES` (grants SV atuais: `ADM360, FINANCEIRO, RELATORIOS, SAUDE360, USUARIOS`) — após o fix, a homologação dos 7 links ainda exigiria grants reversíveis estilo r8b26 (ambiente, não código).

Artefatos versionados: `artefatos-dom/accessdenied-bi.html` e `accessdenied-premium.html` (páginas completas com a reason no URL).

## 5. Evidência executada / artefatos versionados

- **Sem código novo nesta fatia** (apenas ambiente + dados de homologação + navegação); suíte do r11b1 (1428/1428) permanece vigente — nenhum binário alterado.
- Artefatos DOM completos (outerHTML autenticado, versão 2026-10-16) em `artefatos-dom/`: `ocorrencias-ciclo.html` (linhas `Em Aberto` + `Encerrada` com destino), `estoque-bloqueado.html` (filtro BLOQUEADO selecionado + 2 linhas BLOQUEADO), `accessdenied-bi.html`, `accessdenied-premium.html`.
- **Capturas de pixel (viewport PNG): PENDENTES.** ~9 tentativas de `browser.screenshot` retornaram `Screenshot needs a visible tab` apesar de usuário ter declarado janela visível — a condição exige a janela do OpenCode em **primeiro plano no SO** no exato momento (precedente r9l §4: "a condição oscila durante a sessão"). Estados preservados (servidores sobem; ocorrência nº2 ABERTA com bloqueio ativo) — capturar em rajada assim que houver janela em primeiro plano; as capturas vão para `imagens/` (pasta criada).

## 6. Pendências registradas (saída desta fatia)

1. **Capturas de pixel** da tela de Ocorrências/Estoque (e das 2 AccessDenied) — janela em primeiro plano.
2. **Fix do catálogo do guard** (`Bi` + `DashboardsPremium`) + decisão de exposição — fila P2; desbloqueia o item 3.
3. **Clique nos 7 drill-downs** ao vivo — após 2 (+ grants reversíveis de módulo ao tenant SV).
4. Rollback dos dados §2 depois das capturas.
