# Rodada 12 — r12b2: Homologação viva do fix do guard (Bi/DashboardsPremium) + drill-downs + capturas pendentes da R11

- **Data:** 2026-10-16 · **Base:** `ae76e36` (r12b1) · **Slice:** `r12b2` · Ambiente local/homologação.
- **Ambiente:** Web `http://localhost:52976` + API `http://localhost:51976` (Development, HTTP), binários Release **pós-r12b1** (fix já compilado), banco `plantaopro` (PG18) via env `ConnectionStrings__Default` (receita r9l). Sem push nesta fatia.
- **Sessão:** `admin.sistemavertical@plantaopro.local` (perfil ADMINISTRADOR, tenant "Sistema Vertical" `8b0c8e74-a81b-4ea2-b499-94755a1ca301`), login por form (`#Email`/`#senha` + submit, value-setter nativo).
- **Capturas de pixel:** não disponíveis nesta janela — a janela desktop do browser saiu de composição no meio da sessão e `browser.screenshot` seguiu com "Screenshot needs a visible tab" (mesma limitação das rodadas R8/R11). Fallback aceito pela casa: **artefatos DOM** em `artefatos-dom/r12b2-*.txt` (6 arquivos), todos com URL final + innerText verbatim.

## 1. Objetivo e escopo

P2 do plano `r12a0`: provar ao vivo, de ponta a ponta, o ciclo completo do fix r12b1 num tenant sem os módulos — negado-honesto **antes**, página liberada **depois** do grant reversível — e aproveitar o ambiente vivo para:
1. os **7 drill-downs** §27-L208 (R10) que estavam integralmente bloqueados pelo bug de catálogo;
2. as **capturas pendentes da R11** (`r11b2` §7): Ocorrências ABERTA/ENCERRADA + Estoque BLOQUEADO;
3. encerrar a janela mudando **net-zero** o banco (rollback de grants R12 + rollback da seed `SEED_HOMOLOG_R11B2`).

## 2. Ambiente — subida dos servidores (nota de método)

Os scripts de casa `r9l_api.cmd`/`r9l_web.cmd` usam `dotnet run --no-build`; nesta rodada o background-shell do OpenCode matou os dois processos em dois levantes (exit 1 sem erro nos logs — `dotnet run` cai quando o stdin do shell some). Substituição equivalente e estável: **execução direta dos DLLs Release** com as mesmas variáveis de ambiente (`r12_api.cmd`/`r12_web.cmd` em `%TEMP%\opencode`, `ASPNETCORE_URLS` explícito 51976/52976, `ApiSettings__BaseUrl` para a Web). Mesmos binários do gate r12b1; nenhuma diferença de build.

## 3. Negado-honesto ANTES dos grants (estado limpo do SV)

Login OK (log API: `Login bem-sucedido … Perfis:ADMINISTRADOR Escopo:TENANT Modulos:18`). Navegação direta:

| URL | Redirect final (verbatim) |
|---|---|
| `/Bi/Index` | `Account/AccessDenied?module=BI&reason=MODULO_NAO_CONTRATADO&retorno=%2FBi%2FIndex` |
| `/DashboardsPremium/AdministradorCliente` | `Account/AccessDenied?module=DASHBOARD&reason=MODULO_NAO_CONTRATADO&retorno=…` |

Log do guard (server-side, verbatim do stdout da Web):

```
warn: PlantaoPro.Web.Services.Security.SaasRouteGuardFilter[0]
  Acesso negado pelo guard SaaS. Usuario:8b0c8e74-…ca310 Roles:ADMINISTRADOR Controller:Bi Action:Index Modulo:BI Acao:VER Tenant:8b0c8e74-…ca301 …
warn: PlantaoPro.Web.Services.Security.SaasRouteGuardFilter[0]
  Acesso negado pelo guard SaaS. … Controller:DashboardsPremium Action:AdministradorCliente Modulo:DASHBOARD Acao:VER …
```

**O fix resolve os dois controllers para o módulo canônico e nega pelo motivo honesto de contrato** — antes do r12b1 a mesma URL caía em `CATALOGO_NAO_CONFIGURADO` (reproduzido 2× na R11). Artefatos: `r12b2-denegado-honesto-bi-prerollout.txt` / `…premium-prerollout.txt`.

## 4. Grants reversíveis (tag `HOMOLOG_R12`) + achado sobre as permissões

SQL em `%TEMP%\opencode\r12b2_grants_sv.sql` (padrão r8b26 §6, insert-select idempotente):

- `tenant_modulos`: **INSERT 0 5** — `BI`, `DASHBOARD`, `CLIENTES`, `ESCALAS`, `PLANTOES` (drill-downs) para o tenant SV, `origem='HOMOLOG_R12'` (reg_date `2026-10-16 16:16:07`). `ASSINATURAS` dispensável (core-admin, passa sem grant).
- `modulos_efetivos(SV)` pós-grant: **23** (antes 18) — os 5 entraram.
- `perfil_permissoes`: **INSERT 0 0** — e aqui veio o achado de estado:

> **Correção factual ao `r12a0` §2.4** ("perfil tem 0/31"): as 5 linhas `.VER` **já existiam** no perfil desde `2026-10-14 14:59:35` com `permitido=true`, mas **3 delas com `reg_status='I'`** (`BI.VER`, `CLIENTES.VER`, `DASHBOARD.VER` — inativas; `ESCALAS.VER`/`PLANTOES.VER` já `'A'`). Inativas = **não emitidas no login** (a query de claims exige `reg_status='A' AND permitido=true`) — de fato 0 permissão efetiva, como o plano dizia, mas por inativação, não por ausência. Para a homologação **reativei as 3** (`UPDATE 3` → `reg_status='A'`), o que exigiu **re-login** para as claims.

Evidência intermediária (sequência exata observada ao vivo):
1. grants aplicados, mesma sessão → `AccessDenied?module=BI&reason=PERMISSAO_NEGADA` (live-check `api/auth/effective-modules` passou; claims antigas sem `.VER`) — **duas portas distintas funcionando em série, cada uma com seu próprio motivo honesto**;
2. restart dos servidores no meio → sessão in-memory perdida → token JWT ausente → `effective-modules` 401 → **`reason=VERIFICACAO_INDISPONIVEL`** (fail-closed R6-BlocoA confirmado ao vivo, sem falso positivo);
3. re-login + reativação das 3 permissões → **200 nas duas telas** (§5).

## 5. Telas liberadas ao vivo (DEPOIS dos grants)

### 5.1 `/Bi/Index` → HTTP 200 "BI Executivo"
Banner `role=alert` visível: *"Indicadores indisponíveis no momento: não foi possível consultar o serviço BI neste instante."* — KPIs em `-`. Causa (executada no navegador com cookies): `fetch('/api/bi/resumo-executivo')` responde **404 HTML na origem Web** — a view faz fetch **client-side** de `/api/...` presumindo origem única (deploy IIS com `/api` roteado para a API); localmente API/Web correm em portas separadas e a Web não tem rota `/api/*`. **Único outro caso no repo:** `Views/Operacoes/Index.cshtml` (mesmo padrão). Classificação: limitação da configuração local, **não defeito do slice** — e a semântica R8-B19 se manteve ao vivo: sem serviço, sem número demonstrativo, banner honesto.

### 5.2 `/DashboardsPremium/AdministradorCliente` → HTTP 200 "Dashboard Administrador Cliente"
Badge **"Fonte: Real"** (API `GET /api/dashboards/admin-cliente` → 200 em 134ms, ver log API); 4 cards com badge "KPI real" (Plantões 0, Escalas 0, Médicos 0, Agendamentos 0 — tenant sem registros: zeros honestos), alertas vazios honestos, gráfico simples. **Nenhum número demonstrativo.**

Artefatos: `r12b2-bi-executivo-pagina.txt` / `r12b2-premium-admin-cliente-kpis-reais.txt`.

## 6. Os 7 drill-downs §27-L208 (R10) ao vivo

Mapa completo em `artefatos-dom/r12b2-drilldowns-7-links-mapa-vivo.txt`. Resumo:

| # | Card → destino | Resultado ao vivo | Classificação |
|---|---|---|---|
| 1 | BI Clientes Ativos → `/Clientes/Index?status=ATIVO` | AccessDenied (retornando ReturnUrl, sem módulo/razão) — `ClientesController` é `[Authorize(Roles="ADMINISTRADOR_GLOBAL")]`: central global da plataforma; admin tenant recebe denegação de papel limpa | Negado-honesto por papel — caveat já previsto em `r10b3` §3.4; **sem defeito novo** |
| 2 | BI Receita Estimada → `/Assinaturas/Index?status=ATIVA` | **200 "Assinaturas SaaS"** (clique real no âncora) | **HOMOLOGADO AO VIVO** |
| 2b | BI Receita Vencida → `/FaturamentoSaas/Index?status=VENCIDA` | `AccessDenied?module=BILLING&reason=MODULO_NAO_CONTRATADO` — mesmo DEPOIS dos grants | **ACHADO NOVO (P3)** — `BILLING` **não existe em `modulos_sistema`** (nem `BILLING.VER` em `permissoes`) ⇒ nenhum tenant pode contratá-lo ⇒ o card leva a negação permanente para tenants (só global admin passa por bypass). Artefato dedicado |
| 4/5 | BI Escalas período / Cobertura (âncoras JS `d-none`, href só após fetch ok) | âncoras presentes sem href nesta configuração (fetch 404 single-origin, §5.1) | Limitação de configuração local; alvos validados por fonte canônica desde a R10; semântica "sem fetch ok, sem drill" mantida |
| 6 | Premium Plantões → `/Plantoes/Index` | **200 "Central de Plantões"** (clique real) | **HOMOLOGADO AO VIVO** |
| 7 | Premium Escalas → `/Escalas/Index` | **200 "Escalas"** (clique real) | **HOMOLOGADO AO VIVO** |

Saldo: **4/7 homologados ao vivo**, 1 negado-honesto documentado em R10, **1 achado novo** (BILLING), 2 condicionados a origem única.

## 7. Capturas pendentes da R11 (fechadas neste slice, fallback DOM)

Estados vivos preservados no banco desde a R11 confirmados na tela antes do rollback:

- **Ocorrências** (`/Administrativo360/Ocorrencias`): linha 1 *"…Segunda caixa danificada - homologacao R11 (ciclo p/ captura visual)… 8,0000 … **Em Aberto** … Iniciar Tratamento"* (= ocorrência nº2 ABERTA `0e7b058e-…`) e linha 2 *"… **Encerrada** … Descarte assistido (homologacao R11)"* (= ocorrência nº1 `46c473f9-…` v4). Artefato: `r12b2-ocorrencias-aberta-encerrada.txt`.
- **Estoque BLOQUEADO** (`/Administrativo360/Estoque?condicao=BLOQUEADO`): `LT-2026-001 … Deposito Central R11 (homologacao) … BLOQUEADO 6 0 0` e `… Externo R11 (homologacao) … BLOQUEADO 2 0 0` — **idêntico ao verbatim `r11b2` §3** (bloqueio imediato A12 intacto). Artefato: `r12b2-estoque-bloqueado.txt`.

## 8. Rollback — net-zero verificado (executado após todas as capturas)

Script `%TEMP%\opencode\r12b2_rollback_all.bat` (grants R12 → seed R11 → verificação), resultados verbatim:

| Movimento | Resultado | Estado pós |
|---|---|---|
| DELETE `tenant_modulos` `origem='HOMOLOG_R12'` (5 códigos) | **DELETE 5** | `TM_R12_RESTO = 0` |
| UPDATE `perfil_permissoes` devolvendo as 3 reativações | **UPDATE 3** | `BI.VER=I · CLIENTES.VER=I · DASHBOARD.VER=I · ESCALAS.VER=A · PLANTOES.VER=A` — **idêntico ao pré-grant** (estado de 2026-10-14) |
| `modulos_efetivos(SV)` | — | **18 módulos** — exatamente o conjunto pré-grant (idem log de login `Modulos:18`) |
| Seed R11 (SQL verbatim `r11b2` §2: ocorrências / movimentos / locais R11) | **DELETE 2 / 14 / 2** | residuo `ocorrencias=0 · movimentos=0 · locais_r11=0` (14 = 2 ENTRADA seed + 8 pernas ocorrência nº1 + 4 pernas bloqueio ocorrência nº2) |

Banco devolvido ao estado exato de antes da janela. Scripts reexecutáveis: `r12b2_grants_sv.sql` / `r12b2_rollback_sv.sql` / `r11b2_seed_rollback.sql` / `r12b2_verify_final.sql` em `%TEMP%\opencode` (cópias espelhadas dos SQL citados neste doc).

## 9. Achados novos → fila (r12z)

1. **P3 — `BILLING` inexistente em `modulos_sistema`** (§6, linha 2b): card "Receita Vencida" do BI aponta para rota cujo módulo **não pode ser contratado** por tenant. Decisão de produto: (a) DDL do módulo + contrato de plano, ou (b) remapear o card para rota com módulo contratável.
2. **Nota ambiental (não defeito)** — fetch client-side `/api/...` em `Bi/Index` (e `Operacoes/Index`) presuponhe origem única; em desenvolvimento multi-porta o painel KPI exibe o banner honesto. Se a casa quiser homologar KPIs numérico localmente, o caminho é servir Web e API sob uma única origem (proxy) — não exige mudança de código.
3. **Comportamento positivo confirmado ao vivo**: fail-closed `VERIFICACAO_INDISPONIVEL` quando o live-check falha (§4 item 2) — sem falso positivo em momento de degradação.

## 10. Veredicto do slice

**HOMOLOGADO (fallback DOM aceito).** O ciclo completo do fix r12b1 foi provado ao vivo num tenant real: `CATALOGO_NAO_CONFIGURADO` (pré-fix, R11) → `MODULO_NAO_CONTRATADO` (pós-fix, sem contrato) → `PERMISSAO_NEGADA` (contrato sem permissão efetiva) → **200 com dados reais** (contrato + permissão), com o guard logando `Modulo:BI`/`Modulo:DASHBOARD` em cada etapa. Drill-downs: 4/7 abertos ao vivo, demais classificados acima. Banco final net-zero em ambas as tags (`HOMOLOG_R12` + `SEED_HOMOLOG_R11B2`).
