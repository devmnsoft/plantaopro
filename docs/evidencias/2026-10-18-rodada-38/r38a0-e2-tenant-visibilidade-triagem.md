# R38 · r38a0 — E2 parte 1: "tenant" visível só para super-admin — triagem completa por audiência

Data: 2026-10-18 · Sessão: `opencode` · Base: R37 (`befb5b7`, suíte 1561).

## Diretriz (r24a1, epic E2)

"acesso isolado por CPF/CNPJ+email/senha; tenant visível apenas para super-admin". Com D-R24-3, a zona
super-admin (AdminSaas + telas de operação da plataforma para MNSOFT) já é onde "tenant" é linguagem
legítima; o alvo desta rodada é impedir que a palavra e a noção cheguem a **usuários da instituição**.

## Inventário (dump `%TEMP%\opencode\r38_tenant_linhas.txt` — 161 linhas em ~95 views fora de AdminSaas)

Triagem linha a linha separou CHAVE/PROPRIEDADE/JAVASCRIPT (não renderizam a palavra — `UiText.L("...Tenant...")`,
`Model.TenantNome`, `tenantId`, variáveis JS) de TEXTO RENDERIZADO. Alvos confirmados por audiência:

### ZONA INSTITUIÇÃO (usuários da própria organização) — REESCREVER

| Grupo | Ocorrências | Texto-tipo |
|---|---|---|
| Jornada clínica (`Cid/*`, `Consultas/*`, `Prescricoes/*`) | **20 views × 1 literal idêntico** (L9) | "Jornada clínica com tenant, perfil, auditoria LGPD…" → "…com escopo da sua instituição, auditoria LGPD…" |
| `Seguranca/*` (11 views) | alerta compartilhado ×10 + `Index` ×2 | "respeitando tenant, perfis e escopo de leitura" (+ endpoint cru `/api/seguranca` — padrão R34) ; "Gestão multi-tenant de…" → "de usuários da instituição…"; KPI "Sem tenant" → "Sem instituição vinculada" |
| `Shared/_ContextBar.cshtml` | L14, L30 | "limitados ao tenant" → "à sua instituição"; modo exibido "Tenant" → "Instituição" |
| `Shared/_ScreenGuide.cshtml` | 5 pontos | "segregação por tenant", "no tenant atual", "do tenant", "do tenant contratado", "confirme o tenant" |
| `Shared/_ActionBar.cshtml`, `Shared/_PremiumLockCard.cshtml` | 1 cada | "perfil, tenant e plano" / "plano, permissão e tenant" |
| `Ajuda/*` (3 views) | 6 pontos | "consultar outro tenant", "contexto do tenant", "operar no próprio tenant", "pendências do seu tenant", "sujeitos a tenant" |
| `Adm360\DocumentosXml\{Index,Sincronizacao,Triagens}`, `ContratoDetalhes` | 7 pontos | "do tenant", "neste tenant", "usuário do tenant" → "da instituição" |
| PONTOS SOLTOS institucionais | 1 | `Account\AccessDenied` "para o tenant atual"; `Cadastro\Sucesso` "provisiona tenant, cliente…" → "cria a instituição, cliente…"; `HospitalArea\Index` "de outro tenant"; `Integracoes\ApiKeys` "chaves por tenant"; `Financeiro\Details` "no tenant selecionado"; `Relatorios\Index` "do tenant" + chip "Seu tenant" ×4 |

### ZONA SUPER-ADMIN (audiência MNSOFT — operam sobre múltiplos clientes) — KEEP documentado

`Clientes/*`, `Modulos\Tenant.cshtml`, `CommandCenter\Index`, `MeuDia\Index`, `Fase2Operational\Dashboard`
(renderiza valor `TenantScope`, não a palavra), `Home\Dashboard` (card só aparece p/ `IsAdmin()`),
`DashboardsPremium\Perfil` (nota técnica para analistas de KPI).

> **AJUSTE DE PÚBLICO (r38a1, verificado nos controllers)**: `UsuariosController`, `WhiteLabelController`,
> `PerfisController` e `PermissoesController` aceitam `ADMINISTRADOR_GLOBAL, ADMINISTRADOR,
> ADMINISTRADOR_CLIENTE` (+ `Diretor, Suporte, Auditor` no caso de Permissoes) → o **admin da instituição
> acessa essas telas**. Logo, "tenant" renderizado em `Usuarios/*`, `WhiteLabel/*`, `Perfis/*` e
> `Permissoes/*` entra no escopo de REESCREVER (fatias b1/b2), não no keep.

### MARKETING / PÚBLICO — KEEP (whitelist acumulada)

`CommercialDemoWeb\Landing` ("Multi-tenant e white label"), `Shared/SaasComercialPage`.

### CÓDIGO (não renderizado) — KEEP por padrão E1

Nomes de chaves UiText, `Model.Tenant*`, `asp-route-tenantId`, `data-tenant-id`, variáveis JS,
claims `cliente`/`tenant` no `_AppSidebar`/`_UserMenu`/`_TenantContextBadge` (o que aparece na tela é o
VALOR — nome do cliente — e os rótulos já saem "Área da instituição" / "Modo Global MNSOFT").

## Plano de fatias R38

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r38b1 | Jornada clínica 20 views (1 literal ×20) + Seguranca 11 views | 1 fato (negativos exatos por grupo, estilo R7DF) |
| r38b2 | Shared partials (_ContextBar/_ScreenGuide/_ActionBar/_PremiumLockCard) + Ajuda + pontos soltos + variante não-global de `Usuarios\Index` | mesmo fato ampliado |
| r38z | Consolidado + suíte + push | — |

Decisões pendentes (r38a1, próximo turno): (a) modelo de acesso — campos do formulário de usuário
(CPF? CNPJ? email+senha?) e se o isolamento por CPF/CNPJ existe na criação de contas; (b) confirmar o
público real das telas `WhiteLabel/*`, `Perfis/*`, `Permissoes/*` via autorização dos controllers antes
de KEEP ou REWRITE; (c) manter ou simplificar a nota técnica de `DashboardsPremium\Perfil`.

Com R38 + R39 fecha o **E2** (visibilidade de tenant; a separação física de acesso já existe por escopo
de sessão/claim — será auditada na r38a1, não reescrita).
