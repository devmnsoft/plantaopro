# r4 — Sistema Vertical: tenant SaaS + administrador + provas reais (§4)

**Data:** 2026-10-14 · **Base:** `main @ 4ab9770` (commit local pendente r9a) · **Banco:** `plantaopro` local (PostgreSQL 18, `127.0.0.1:5432`)

## 1. O que o seed cria (e o que não cria)

Script: `database/seeds/development/123_sistema_vertical_plantaopro.sql`

| Objeto | Identidade fixa | Observação |
|---|---|---|
| Tenant SaaS "Sistema Vertical" | `8b0c8e74-a81b-4ea2-b499-94755a1ca301` | código `SISTEMA_VERTICAL`, `dados.planoId` → plano de homologação |
| Cliente comercial PRÓPRIO | `8b0c8e74-a81b-4ea2-b499-94755a1ca302` | `cnpj = NULL` (não inventar documento) |
| Perfil `ADMINISTRADOR` | md5(`sv-profile:ADMINISTRADOR`) | escopo ao próprio tenant (`base_sistema=true`, sem `ADMINISTRADOR_GLOBAL`) |
| Usuário (acesso de TESTE) | `8b0c8e74-a81b-4ea2-b499-94755a1ca310` | `admin.sistemavertical@plantaopro.local` |
| Plano reutilizado | `8b0c8e74-a81b-4ea2-b499-94755a1ca003` | `HOMOLOGACAO_360` — mesma identidade do seed 122; cria só se ausente |
| Módulos contratados | md5(`sv-module:<CODIGO>`) | `ADM360`, `FINANCEIRO`, `RELATORIOS`, `SAUDE360`, `USUARIOS` |
| Permissões | md5(`sv-admin-permission:<codigo>`) | catálogo **escopado ao contrato** (311 permissões) |

**Não cria:** vínculo de `adm360_parceiros` cruzado (SV não é cliente comercial de outros tenants — verificado: `SV_AS_CLIENT_IN_OTHER_TENANT = 0`), privilégio global (exclui `SAAS`/`AUDITORIA_GLOBAL`; escopo de acesso confirmado `ACCESSSCOPE=TENANT` no login).

## 2. Execução real no banco (idempotência)

Dois `psql -v ON_ERROR_STOP=1 -f` consecutivos sobre o mesmo banco:

```
RUN 1 → SET/DO            HASH_AFTER_RUN1=$2a$11$iTTjI7RBZxuEMRYfS5inNuXqQ3EdyX3Eldyfgr2iE7frFkuxfycGG
RUN 2 → SET/DO            HASH_AFTER_RUN2=$2a$11$iTTjI7RBZxuEMRYfS5inNuXqQ3EdyX3Eldyfgr2iE7frFkuxfycGG
PASSWORD_UNCHANGED_ON_RERUN=True

VERIFY: TENANT 1 · CLIENTE 1 · USER_ACTIVO 1 · PERFIS_ATIVOS 1 · PERMISSOES 311
MODULOS  ADM360,FINANCEIRO,RELATORIOS,SAUDE360,USUARIOS
EFETIVOS ADM360,AGENDAMENTOS,CID,CLINICA_DASHBOARD,CLINICA_FINANCEIRO,CONSULTAS,CONVENIOS,
         FINANCEIRO,PACIENTES,PAINEL_CHAMADA,PENDENCIAS_CLINICAS,PLANOS_SAUDE,PRESCRICOES,
         RELATORIOS,SAUDE360,TRIAGEM,UNIDADES,USUARIOS   (expansão do pacote SAUDE360)
SV_AS_CLIENT_IN_OTHER_TENANT 0 · USUARIO_TENANT …ca301 · PLAN_USED Plano Homologação Saúde 360
```

Sem duplicar organização/cliente/perfil/usuário/assinatura e **sem reescrever a senha** no re-run (upsert por e-mail preserva `senha_hash`). O hash é BCrypt cost 11 compatível com `PasswordHashService` (`BCrypt.Net.BCrypt.Verify`).

## 3. Prova 1 — login na API (`POST /api/auth/login`)

- Credenciais corretas → **200**, token emitido; claims: `ROLES=ADMINISTRADOR`, `PRIMARYROLE=ADMINISTRADOR`, `ACCESSSCOPE=TENANT`, `TENANT_CONTEXT_SELECTED=True`, `CLIENTE_STATUS=ATIVO` (tenant selecionado = próprio `…ca301`).
- Senha errada → **401**.
- Tamanho do token com permissões escopadas: `TOKEN_LEN=10164` (claim `permission` = 5.459 chars). Antes do escopo (catálogo completo, 1.761 permissões) era 53.044 chars (claim 35.294) — ver §5.2.

## 4. Prova 2 — abertura do módulo Administrativo 360 (Web BFF, `localhost:52976`)

Execução direta (TCP sem proxy, antiforgery real obtido da página):

```
GET  /Account/Login            → 200 (token antiforgery presente, len 155)
POST /Account/Login            → 302 Location=/ClientePortal/Index
                                 Set-Cookie: 5 cabeçalhos, 10.702 bytes
GET  /Administrativo360/Index  → 200, URL final permanece /Administrativo360/Index
ADM360_HAS_A360_BODYCLASS      = True   (chrome pp-a360)
ADM360_HAS_MODULE_TEXT         = True   ("Administrativo 360")
ADM360_SEES_SYSTEMA_VERTICAL   = True   (organização do próprio tenant visível)
ADM360_HTML_LEN                = 57.132 (snapshot: r4-sv-adm360-index.html)
```

Logs da execução: `r4-sv-web.log` (`Cookie de autenticação criado. UsuarioId:…ca310`, `Redirecionando usuário após login. Perfis:ADMINISTRADOR Destino:ClientePortal/Index`) e `r4-sv-api.log` (`Login processado ... Status:200 Perfil:ADMINISTRADOR`).

**Navegador real** (Chromium do painel de revisão): envio do formulário nativo com cookies e antiforgery do próprio navegador → a sessão autenticou e a aba chegou a `/ClientePortal/Index` — "Portal do cliente". A sessão do navegador permaneceu autenticada (navegação subsequente não retornou para o login).

## 5. Defeitos encontrados e corrigidos durante as provas

### 5.1 Contrato de módulo não resolvido → guard SaaS negava `ADM360:VER`

- **Sintoma:** `SaasRouteGuardFilter: Acesso negado pelo guard SaaS. Usuario:…ca310 Roles:ADMINISTRADOR Controller:Administrativo360 Action:Index Modulo:ADM360 Acao:VER` e `Modules=[]` no login (página ia para `/Account/AccessDenied`).
- **Causa:** o predicado canônico `plantaopro.modulos_efetivos()` (migrations v2339/v2340) só reconhece contratos por `modulo_id` ou `codigo_modulo` — nunca por `codigo` livre. A primeira versão do seed gravava apenas `codigo`.
- **Correção:** o seed resolve `modulo_id` **e** `codigo_modulo` contra `modulos_sistema` (padrão idêntico ao do tenant Clínica Modelo, cujo seed 122 sempre grava `codigo_modulo`). Inclui `SAUDE360` para a jornada de Saúde 360 do §26 — a função expande automaticamente as capacidades-filhas do pacote (18 claims efetivos, lista em §2).

### 5.2 502 no transporte de homologação visual com catálogo de permissões completo

- **Sintoma:** o POST `/Account/Login` era processado de ponta a ponta no servidor (API 200, cookie criado, redirect emitido nos logs), mas o navegador recebia **502 com corpo vazio e sem cabeçalho `Server: Kestrel`** — o `Set-Cookie` não chegava. Com senha errada (resposta pequena) o mesmo caminho respondia 200 normalmente.
- **Diagnóstico:** com 1.761 permissões concedidas, o claim combinado `permission` tem 35.294 chars; o cookie de sessão Web (payload protegido) é fatiado pela `CookieManager` em ~12 `Set-Cookie` ≈ 45 KB de cabeçalhos de resposta — acima do limite do túnel/proxy do navegador de revisão, que aborta com 502. O app em si nunca emite 502 em `/Account/Login` (o mapeamento 502 existe apenas em `/bff/*`) e respondeu corretamente em TCP direto.
- **Correção (no seed, mais fiel ao §4 — "permissões necessárias ao Administrativo 360 e aos módulos disponíveis no plano"):** concessão escopada aos módulos contratados (+ capacidades-filhas de Saúde 360): 311 permissões, claim de 5.459 chars, `Set-Cookie` total 10,7 KB → o navegador real aceitou e abriu o portal. Reexecuções convergem: o seed desativa concessões ativas fora do escopo.

## 6. Captura de tela

**BLOQUEADA (ambiente):** `browser.screenshot` exigiu "visible tab" — a janela do navegador de revisão estava oculta no lado do usuário apesar de foco/abre-revisão tentados. A cobertura funcional acima (API + Web + navegador real com sessão autenticada) está completa; as capturas serão repetidas quando a janela estiver visível (a homologação visual do §26 repete o percurso inteiro com capturas desktop/móvel).

## 7. Acesso de teste (explícito)

| Login | Senha | Uso |
|---|---|---|
| `admin.sistemavertical@plantaopro.local` | `Sv-Homolog-2026!` | **TEST** — homologação §4/§26 apenas; registrado em `docs/usuarios-teste.md` |
