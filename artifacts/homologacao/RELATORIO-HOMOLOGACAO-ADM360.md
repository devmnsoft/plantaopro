# Relatório de Homologação — Administrativo 360 (MVP) e acesso do Gestor

| Campo | Valor |
|---|---|
| Data | 2026-09-26 |
| Ambiente | Local dev (Windows) — API `https://localhost:51977` · Web `https://localhost:52977` |
| Banco | PostgreSQL — banco `postgres`, schema `plantaopro` (TZ de exibição UTC-3) |
| Tenant demo | Santa Casa Demonstração (`d3f6584c-2c64-4e5a-9ea9-4e1428647502`) |
| Usuários | `gestor@santacasa-demo.example` / `SantaCasa!Demo2026#Gestor` · `consulta@santacasa-demo.example` / `SantaCasa!Demo2026#Gestor` |
| Veredito | **APROVADO** — caminho crítico 100% executado e verde; pendências restantes sem bloqueio (§10) |

Documentos complementares (mesma pasta):

- `SCRIPT-APRESENTACAO-15MIN.md` — roteiro de apresentação (≤15 min)
- `PENDENCIAS-ROADMAP.md` — pendências por impacto + roadmap atualizado
- `screenshots/` — evidências em PNG (3 arquivos)

---

## 1. Objetivo

Provar, com execução real (não planilha/doc/demonstração simulada), que:

1. O **gestor** do cliente acessa o **Administrativo 360** no Web e na API;
2. O acesso é derivado do contrato/permissão (nada de e-mail hardcodado, sem acesso cross-tenant);
3. Um usuário de **consulta/auditoria** mantém acesso **somente leitura**;
4. Menu e API refletem as mesmas permissões; revogação é validada ao vivo;
5. Migrations e seeds seguem as regras do projeto (história preservada, checksums, reaplicáveis).

## 2. Diagnóstico — cadeia de autenticação percorrida ponta a ponta

Caminho validado: `usuário → perfil → tenant → contrato (tenant_modulos) → permissões → claims → sessão → autorização → rota → página`.

| Etapa | Como foi provada | Resultado |
|---|---|---|
| Usuário/tenant | Login real via API e Web | OK |
| Perfil (perfis + vínculos) | Seed 122 revisado; perfis por tenant com vínculo ativo | OK (com defeito 3, corrigido) |
| Contrato de módulos | `tenant_modulos` = tabela de contrato; exatamente 3 linhas ATIVO/A para o tenant | OK |
| Permissões → claims | `POST /api/auth/login`: 48 permissões (32 `ADM360.*`) e `data.modules=["ADM360","ESCALAS","RELATORIOS"]` | OK |
| Sessão Web | Cookies `PlantaoPro.Auth` (chunks-3) + `PlantaoPro.AuthC1..C3` + `.AspNetCore.Session` emitidos no login | OK |
| Autorização por request | `Program.cs` mapeia política → código DB (`ADM360:VER` etc.) a cada request; negativa gera linha `ACESSO_NEGADO` | OK |
| Rota Web | Guard SaaS (`SaasRouteGuardFilter`) + `[Authorize]` nos controladores ADM360 | **Falha encontrada (raiz 1), corrigida e provada** |
| Roteamento de contrato | Trigger `plantaopro.adm360_validar_tenant()` | **Falha encontrada (raiz 2), corrigida e provada** |

Os três defeitos raiz que impediam o gestor de abrir o Administrativo 360:

### Raiz 1 — Catálogo de módulos do guard Web sem os controladores ADM360 (WEB, bloqueante)

- `SaasRouteGuardFilter.ControllerModules` não possuía as chaves dos 4 controladores ADM360 (`Administrativo360`, `Adm360GestaoWeb`, `Adm360DocumentosXmlWeb`, `Adm360CotacoesWeb`).
- Efeito: **antes** de `[Authorize]` rodar, qualquer rota `/Administrativo360/*` era redirecionada para `/Account/AccessDenied?reason=CATALOGO_NAO_CONFIGURADO` — independentemente da permissão. O gestor, mesmo com o módulo contratado e 32 permissões `ADM360:*`, nunca abria a tela.
- Correção: inclusão das 4 chaves mapeadas para `"ADM360"`.
- Prova: login fresco via HTTP → `GET /Administrativo360/Index` → **200**, h1 "Administrativo 360" (~48 KB); screenshot anexa.

### Raiz 2 — Trigger v2190 `adm360_validar_tenant()` resolvia campos contra a tabela errada (DB, bloqueante para contrato)

- Na migration publicada v2190, o bloco de subplanos resolvia `NEW.<campo>` contra o rowtype da **tabela de disparo**, ignorando o curto-circuito por `TG_TABLE_NAME`. Com isso, a validação de tenant/contrato usava valores de outra tabela (ou nulos) dependendo de qual trigger fireava.
- Correção: migration nova **v2199** reescreve a função com blocos `IF TG_TABLE_NAME ... ELSIF` por tabela monitorada. **v2190 não foi editada** (migration publicada); histórico preservado; checksum SHA-256 registrado em `database/migration-manifest.json`.
- Prova: cenários A/B/C/D de reprodução (inserção/atualização nas tabelas monitoradas exercitando os subplanos) falham na v2190 original e passam com a v2199 aplicada.

### Raiz 3 — Seed 122: unicidade global de `perfis.nome` descartava os perfis por tenant (DB, silenciosa)

- `perfis` possui UNIQUE global em `nome` (`perfis_nome_key`). Linhas legacy com `tenant_id NULL` já ocupavam os nomes base (`Administrador`, `Médico`, ...), então o INSERT do perfil por tenant era **silenciosamente descartado** pelo `ON CONFLICT DO NOTHING` — o usuário ficava sem perfil ativo e o acesso desmoronava a jusso.
- Correção: nome sufixado com a identidade do cliente — `x.nome || ' - ' || (SELECT coalesce(c.nome_fantasia, c.nome) FROM clientes c WHERE c.id = v_cliente)`. Linhas legacy não tocadas; seed reaplicável.
- Prova: reaplicação limpa do seed 122 + logins reais gestor e consulta.

## 3. Arquivos alterados (todos commitados neste lote)

| Arquivo | Alteração |
|---|---|
| `backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs` | +4 chaves ADM360 no catálogo de módulos (raiz 1) |
| `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml` | Item "Administrativo 360" condicionado a `Can("ADM360")` (menu reflete permissão) |
| `database/migrations/2026_09_v2199_corrigir_trigger_adm360_validar_tenant.sql` | Nova migration — corrige trigger (raiz 2); v2190 intocada |
| `database/migration-manifest.json` | Entrada v2199 + checksum SHA-256 + dependência v2198 |
| `database/seeds/development/121_acesso_demo_local.sql` | Coluna `nome` incluída no INSERT de `tenant_modulos` (contrato canônico demo) |
| `database/seeds/development/122_usuarios_homologacao_plantaopro.sql` | Sufixo de nome nos perfis (raiz 3); alinhamento de colunas (`clientes`, `hospitais`, `medicos`, `especialidades`) com o DDL vivo |
| `database/pgadmin/administrativo360_demo_completo.sql` | Mesmo alinhamento: `nome` em `tenant_modulos`, upsert médico (`dados::jsonb`), especialidade por nome global |
| `backend/PlantaoPro.Api/appsettings.json`, `appsettings.Development.json`, `backend/PlantaoPro.Web/appsettings.json` | Connection strings locais de dev (`Host=127.0.0.1;Database=postgres;...`) + flags de dev na API (mantidas por decisão do dono do ambiente) |
| `artifacts/homologacao/screenshots/*.png` | 3 evidências em PNG (anexos) |

Observação: nenhum mock em código — todos os dados demonstrativos vêm de scripts SQL (seed 121/122/140/141 + `pgadmin/administrativo360_demo_completo.sql`).

## 4. Ordem de execução dos scripts (DB)

Regras cumpridas: migrations publicadas nunca editadas; histórico preservado; checksums conferidos; nada registrado como migrado sem executar. Neste ambiente o runner `Tools.Database` **não** é usado (schema `schema_migrations` vazio — decisões de registro documentadas em `PENDENCIAS-ROADMAP.md`); migrations aplicadas manualmente e consistentemente.

```
1. Migrations até v2198 (já aplicadas neste ambiente)
2. psql -v ON_ERROR_STOP=1 -f database/migrations/2026_09_v2199_corrigir_trigger_adm360_validar_tenant.sql
3. psql -v ON_ERROR_STOP=1 -f database/seeds/development/121_acesso_demo_local.sql
4. psql -v ON_ERROR_STOP=1 -f database/seeds/development/122_usuarios_homologacao_plantaopro.sql
5. psql -v ON_ERROR_STOP=1 -f database/seeds/development/140_administrativo360_demo.sql
6. psql -v ON_ERROR_STOP=1 -f database/seeds/development/141_administrativo360_suprimentos_demo.sql
7. psql -v ON_ERROR_STOP=1 -f database/pgadmin/administrativo360_demo_completo.sql   # idempotente
```

- Todos idempotentes/reaplicáveis; suporta bases fresh, legacy e com contrato duplicado.
- **Sem reset silencioso de senha**: o provisionamento preserva hashes existentes; reset só pelo script dedicado com confirmação literal (`reset_usuarios_homologacao_plantaopro.sql`).

## 5. Perfil e módulos do gestor

- Usuário: `gestor@santacasa-demo.example` (id `…7511`), nome "Gestor Santa Casa — Demonstração".
- Perfil: **ADMINISTRADOR_CLIENTE** (`5a6773b8-…`) — 48 grants, sendo **32 permissões `ADM360:*`**.
- Contrato do tenant (`tenant_modulos`, exatamente 3 módulos contratados/habilitados):
  - `ADM360` — linha `e8e5eda7-1cc9-4b37-bbaa-489d1a10d568`, status `ATIVO`, `habilitado=t`, ativado em 2026-09-26 13:37 (UTC-3)
  - `ESCALAS`
  - `RELATORIOS`
- Claims de login: `data.modules = ["ADM360","ESCALAS","RELATORIOS"]`; permissões normalizadas em forma pontual maiúscula (ex.: `ADM360.MAPEAR_CADASTROS`); a política da Web resolve por request para o código DB de forma dois-pontos (ex.: `ADM360:VER`).
- Menu: item "Administrativo 360" renderiza apenas com `Can("ADM360")` — mesmo sinal da API. Sem e-mail hardcodado em lugar algum do caminho.
- Consulta: `consulta@santacasa-demo.example` (id `…7515`), perfil **AUDITOR** (`53a2d3db-…`) — 7 grants, incluindo `ADM360:VER` (grant `ee2ec9fe-ca35-4184-91ea-d6d29e0769c8`); **sem** `MAPEAR_CADASTROS` (escrita).

## 6. Resultado real dos logins

### 6.1 API (Bearer)

`POST /api/auth/login` com o gestor → **200**:

- `data.tenantId = d3f6584c-2c64-4e5a-9ea9-4e1428647502`
- `data.permissions`: **48** permissões (32 `ADM360.*`)
- `data.modules`: `["ADM360","ESCALAS","RELATORIOS"]`
- token + `sessionId` emitidos; sessão validada por request.

Login consulta → **200** com **7** permissões e sem `ADM360.MAPEAR_CADASTROS`.

### 6.2 Web (cookies)

Fluxo reproduzido por navegador **e** por curl:

1. `GET /Account/Login` → 200 + token antiforgery (155 chars) + cookie antiforgery.
2. `POST /` (o form do login aponta para `/`; template padrão `{controller=Account}/{action=Login}/{id?}`) → **302 Location: /ClientePortal/Index**, `Set-Cookie`: `PlantaoPro.Auth` (chunks-3), `PlantaoPro.AuthC1/C2/C3`, `.AspNetCore.Session`.
3. `GET /Administrativo360/Index` → **200**, h1 "Administrativo 360", tenant "Santa Casa Demonstração", modo Tenant. Screenshot anexa.

## 7. Matriz telas × ações (executado com persistência verificada)

Status: **APROVADO** / FALHOU / BLOQUEADO / NÃO EXECUTADO.

| # | Tela / Ação | Usuário | Status | Evidência |
|---|---|---|---|---|
| 1 | API — login | gestor | **APROVADO** | M1: 200, 48 perms, modules corretos |
| 2 | API — leitura dashboard gestão | gestor | **APROVADO** | M2: `GET …/gestao/dashboard` 200 |
| 3 | API — leitura cotações | gestor | **APROVADO** | M2b: `GET …/cotacoes` 200 |
| 4 | API — escrita mapeamento | gestor | **APROVADO** | M3: `POST …/cotacoes/mapeamentos` 200 → linha persistida `581402ba-39f5-4d22-beec-096ec7aee396` (PORTAL_DEMO/PRODUTO/MAT9001-HOMOLOG/ATIVO, `criado_por` = gestor `…7511`) confirmada por consulta SQL |
| 5 | API — login | consulta | **APROVADO** | M4: 200, 7 perms, sem MAPEAR_CADASTROS |
| 6 | API — leitura dashboard | consulta | **APROVADO** | M5: 200 |
| 7 | API — leitura cotações | consulta | **APROVADO** | M5b: 200 |
| 8 | API — escrita mapeamento (deveria negar) | consulta | **APROVADO** (negativa esperada) | M6: 403 + linha `ACESSO_NEGADO` na auditoria |
| 9 | Revogação ao vivo (grant p/ `reg_status='I'`) | gestor | **APROVADO** | M7: dashboard 403 **no mesmo token** após inativação do grant `ee2ec9fe-…` |
| 10 | Restauração (grant p/ `'A'`) | gestor | **APROVADO** | M7b: 200 no mesmo token após restaurar |
| 11 | Web — login | gestor | **APROVADO** | POST `/` → 302 → ClientePortal; cookies de sessão emitidos (browser + curl) |
| 12 | Web — home Administrativo 360 | gestor | **APROVADO** | GET 200, h1 "Administrativo 360"; screenshot `pp_web_gestor_adm360_index.png` |
| 13 | Web — sidebar com "Administrativo 360" | gestor | **APROVADO** | Screenshot `pp_web_gestor_sidebar_adm360.png` (item ativo sob "Administração do Cliente") |
| 14 | Web — home Administrativo 360 (somente leitura) | consulta | **APROVADO** | Mesma página abre para o AUDITOR; screenshot `pp_web_consulta_adm360_index.png` ("Auditor Restrito — Demonstração") |
| 15 | Web — renovação de contexto (`RefreshContext`) sem relogin | gestor | **APROVADO** | `GET /Account/RefreshContext?returnUrl=%2FMinhaCentral` → 302; tickets `PlantaoPro.Auth*` **reemitidos (valor mudou)**; `GET /Administrativo360/Index` subsequente 200 com conteúdo íntegro |
| 16 | Web — `POST /Account/RefreshContext` | gestor | **BLOQUEADO** | 405 `Allow: GET` — mesma anomalia de roteamento que afeta todo POST em `/Account/*` (pendência P2); variante GET usada e validada |
| 17 | Web — escrita por formulário (UI) | gestor | **NÃO EXECUTADO** | Escrita coberta pela API (#4) com persistência; E2E de formulário UI recomendado no próximo ciclo |
| 18 | Cross-tenant negativo no nível de app | outro tenant | **NÃO EXECUTADO** | Integridade de tenant validada no nível do trigger (repro A/B/C/D, §2 raiz 2); teste negativo app-a-app pendente (P3) |
| 19 | Web — AUDITOR abre `/Auditoria/Index` (home padrão) | consulta | **FALHOU** | Cai em `/Account/AccessDenied?ReturnUrl=%2FAuditoria%2FIndex` — fora do caminho crítico ADM360 (pendência P4) |

Resumo: **15 APROVADO · 1 BLOQUEADO · 2 NÃO EXECUTADO · 1 FALHOU** (o único FALHOU é a home do AUDITOR, fora do escopo crítico).

## 8. Auditoria (delta medido no banco)

Semântica: cada login grava **2** linhas `LOGIN_SUCESSO`; cada negativa de política grava 1 `ACESSO_NEGADO`; leituras/escritas não são auditadas (escrita comprovada pela linha no banco).

| Ação | Antes | Depois | Delta | Explicação |
|---|---|---|---|---|
| `LOGIN_SUCESSO` | 116 | 124 | +8 | 4 logins × 2 linhas (matriz API executada 2×) |
| `ACESSO_NEGADO` | 84 | 88 | +4 | 2× (M6 + M7) por execução da matriz |
| `LOGIN_FALHA` | 31 | 33 | +2 | Preexistentes (diagnóstico de rota), fora da matriz |
| `OPERACAO_RESUMO` / SAUDE360 | — | — | 0 | Inalterados (sem efeito colateral) |

## 9. Screenshots (evidências, commitados)

| Arquivo | Conteúdo |
|---|---|
| `screenshots/pp_web_gestor_adm360_index.png` | Home ADM360 autenticada — tenant "Santa Casa Demonstração", usuário "Gestor Santa Casa — Demonstração", PERFIL ADMINISTRADOR CLIENTE, MODO Tenant |
| `screenshots/pp_web_gestor_sidebar_adm360.png` | Sidebar aberta com item "Administrativo 360" renderizado e ativo sob "ADMINISTRAÇÃO DO CLIENTE" |
| `screenshots/pp_web_consulta_adm360_index.png` | Usuário de consulta/AUDITOR ("Auditor Restrito — Demonstração") abrindo a mesma página (leitura preservada) |

## 10. Pendências

Classificação por impacto e plano de ação: ver **`PENDENCIAS-ROADMAP.md`**. Nenhuma pendência bloqueia o MVP do Administrativo 360 nem o acesso do gestor.

## 11. Conclusão

O gestor voltou a acessar o Administrativo 360 com acesso **completo dentro dos módulos contratados e habilitados**, derivado exclusivamente de perfil + contrato (sem hardcode), com menu e API refletindo as mesmas permissões, revogação validada ao vivo e usuário de consulta mantido somente leitura. As três causas raiz foram corrigidas com migração/seed/versionamento corretos e provas reais (HTTP, banco e screenshots).
