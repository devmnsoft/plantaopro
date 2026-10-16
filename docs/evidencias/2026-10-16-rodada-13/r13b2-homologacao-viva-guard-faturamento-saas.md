# Rodada 13 — r13b2 · Homologação ao vivo do fix do guard (BILLING → FATURAMENTO_SAAS)

Data: 2026-10-16 · Slice r13b2 da Rodada 13 · Binários: build Release `41bdd55` (r13b1) · Ambiente local Development (Api `:51976`, Web `:52976`, Postgres `127.0.0.1:5432/plantaopro`) · Método de captura: fallback DOM aprovado (mesma regra do r12b2 — janela desktop em 1º plano indisponível para screenshot; toda captura abaixo é DOM lido da página servida).

## 1. Objetivo e escopo

Provar ao vivo, no tenant SV com o perfil admin (`Administrador Sistema Vertical`, papel `ADMINISTRADOR`), o ciclo completo:

1. **Negação-honesta pré-grant** nas 3 entradas do guard remapeado (`/FaturamentoSaas/Index`, `/Bi/Index`, `/Billing/Faturas`) — agora nomeando o módulo canônico contratável em vez do fantasma.
2. **Grant reversível** `HOMOLOG_R13` (contrato + permissão mínima de leitura) e **re-login** (claims de login).
3. **Liberado**: 200 em `/FaturamentoSaas/Index`, filtro `?status=VENCIDA` refletido, `/FaturamentoSaas/Inadimplencia` 200, shell `/Billing/Faturas` redirecionando para a operação canônica.
4. **Drill-down do card "Receita Vencida" do BI Executivo** (o gap exato reproduzido no r12b2): `/Bi/Index` → card → `/FaturamentoSaas/Index?status=VENCIDA`.
5. **Rollback exato** e net-zero verificado (banco + nova sessão negada de novo).

### Refinamento do plano (vs r13a0 D3)

O D3 previu contrato de 1 módulo (`FATURAMENTO_SAAS`) + reativação de 1 permissão. Em execução, o recon do estado atualizou o escopo para **2 módulos + 2 permissões**, porque o rollback final do r12 removeu o contrato `BI` do SV — sem `BI` a tela `/Bi/Index` (que hospeda o card) negaria antes de exibir o drill-down. Grant real executado (tag `HOMOLOG_R13`, ambos reversíveis):

| Tipo | Alvo | Estado pré | Pós-grant | Rollback |
|---|---|---|---|---|
| `tenant_modulos` (INSERT) | `FATURAMENTO_SAAS` | 0 linhas | ATIVO/habilitado | DELETE |
| `tenant_modulos` (INSERT) | `BI` (somente para hospedar o card) | 0 linhas | ATIVO/habilitado | DELETE |
| `perfil_permissoes` (UPDATE) | `FATURAMENTO_SAAS.VER` | `reg_status='I'`, permitido=true | `'A'` | `'I'` |
| `perfil_permissoes` (UPDATE)` | `BI.VER` | `reg_status='I'`, permitido=true | `'A'` | `'I'` |

Sem insert de `perfil_permissoes` (grants já existiam dormentes — evidência r13a0 F4); sem DDL; escritas `.CONFIRMAR`/`.EXCLUIR` mantidas dormentes (fora do perímetro, D3).

## 2. Ambiente — subida dos servidores (nota de método)

- Api: `r12_api.cmd` (Development, `http://localhost:51976`, connection string explícita) · Web: `r12_web.cmd` (Development, `http://localhost:52976`, `ApiSettings__BaseUrl=http://localhost:51976`).
- Health check pós-subida: `WEB|200` (`/Account/Login`) e `API|200` (`/swagger/index.html`).
- Credenciais: `admin.sistemavertical@plantaopro.local` / perfil `ADMINISTRADOR` (tenant SV `8b0c8e74-…-ca301`, perfil `0941eb60-…-4a5bca`).
- Snapshot pré-grant (`r13b2_pre_grant`): **efetivos SV = 18** (`ADM360,AGENDAMENTOS,CID,CLINICA_DASHBOARD,CLINICA_FINANCEIRO,CONSULTAS,CONVENIOS,FINANCEIRO,PACIENTES,PAINEL_CHAMADA,PENDENCIAS_CLINICAS,PLANOS_SAUDE,PRESCRICOES,RELATORIOS,SAUDE360,TRIAGEM,UNIDADES,USUARIOS`) · `tenant_modulos` FATURAMENTO_SAAS = **0 linhas** · `FATURAMENTO_SAAS.VER` = **I/true** · resíduo `HOMOLOG_R13` = **0**.

## 3. Negado-honesto ANTES dos grants (após login, sessão tenant)

| Rota | Resultado (URL final) |
|---|---|
| `/FaturamentoSaas/Index` | 302 → `/Account/AccessDenied?module=FATURAMENTO_SAAS&reason=MODULO_NAO_CONTRATADO&retorno=%2FFaturamentoSaas%2FIndex` |
| `/Bi/Index` | 302 → `…module=BI&reason=MODULO_NAO_CONTRATADO…` |
| `/Billing/Faturas` | 302 → `…module=FATURAMENTO_SAAS&reason=MODULO_NAO_CONTRATADO&retorno=%2FBilling%2FFaturas` |

Leitura: a denegação é **honesta e canônica** — nomeia `FATURAMENTO_SAAS` (módulo real, contratável, suíte completa) em vez do fantasma `BILLING` que o catálogo nem possui. O motivo `MODULO_NAO_CONTRATADO` (e não `CATALOGO_NAO_CONFIGURADO`) confirma que o mapeamento do guard resolve. A página de negação exibiu h1 "Acesso negado", tenant "Sistema Vertical", usuário "Administrador Sistema Vertical", MODO Tenant.

## 4. Grants reversíveis aplicados (`HOMOLOG_R13`)

Saída do psql (`r13b2_grants_sv.sql`, transacional, commit único):

```
BEGIN
INSERT 0 2
UPDATE 2
COMMIT
POS_GRANT_EFETIVOS|ADM360,AGENDAMENTOS,BI,CID,CLINICA_DASHBOARD,CLINICA_FINANCEIRO,CONSULTAS,CONVENIOS,FATURAMENTO_SAAS,FINANCEIRO,PACIENTES,PAINEL_CHAMADA,PENDENCIAS_CLINICAS,PLANOS_SAUDE,PRESCRICOES,RELATORIOS,SAUDE360,TRIAGEM,UNIDADES,USUARIOS
POS_GRANT_TM_HOMOLOG_R13|2
POS_GRANT_PERM_BI.VER|A|permitido=true
POS_GRANT_PERM_FATURAMENTO_SAAS.VER|A|permitido=true
```

Efetivos saltam de **18 → 20** com exatamente `BI` e `FATURAMENTO_SAAS` acrescidos. Re-login executado (claims de permissão são materializadas no login; live-check de módulo é por requisição).

## 5. Telas liberadas ao vivo (após grants + re-login)

### 5.1 `/FaturamentoSaas/Index` → HTTP 200

DOM: `title`/`h1` = **"Faturamento SaaS"**; resumo com estado vazio honesto — cards `Receita prevista R$ 0,00 · Receita recebida R$ 0,00 · Abertas 0 · Vencidas 0 · Contestação 0` (sem números demonstrativos inventados; tenant SV sem faturas geradas); tabela sem linhas; filtro de status com as 6 opções (`Todos/ABERTA/PAGA/VENCIDA/CANCELADA/EM_CONTESTACAO`). Os 3 elementos `[role=alert]` presentes na captura são parciais de layout ocultos (`_DetailDrawer.cshtml` drawer `hidden` + alerts de ModelState padrão do layout) — falsos positivos do seletor, não erro da tela. Nota de evidência: os zeros vieram do endpoint `/resumo` (**200**); a lista paginada `/faturas` respondeu **500** na Api — achado novo pré-existente, detalhado no §8 (não afeta o veredicto do guard: a porta é upstream da chamada).

### 5.2 `/FaturamentoSaas/Index?status=VENCIDA` → HTTP 200, filtro refletido

DOM: option selecionada = **`VENCIDA`** — a rota exata do drill-down chega com o filtro já aplicado (fact de contrato espelhado no b1).

### 5.3 `/FaturamentoSaas/Inadimplencia` → HTTP 200 "Inadimplência SaaS"

Segunda tela somente-leitura do controller liberada pelo mesmo grant mínimo.

### 5.4 Shell `/Billing/Faturas` (R5-B6) → 302 → `/FaturamentoSaas/Index` (200)

O guard passa no shell pelo mesmo módulo canônico e a ação redireciona para a operação real (sem tela legada própria).

## 6. Drill-down do card "Receita Vencida" do BI Executivo — o gap do r12b2

1. `/Bi/Index` → HTTP 200 "BI Executivo" (com o contrato `BI` temporário).
2. DOM do card: `<a href="/FaturamentoSaas/Index?status=VENCIDA">Ver registros</a>`.
3. **Clique no card** → navegação real → DOM final: `url=/FaturamentoSaas/Index?status=VENCIDA`, `title/h1="Faturamento SaaS"`, option selecionada `VENCIDA`.

Ciclo completo card → tela funcionando end-to-end; o defeito reproduzido no r12b2 (`MODULO_NAO_CONTRATADO` ao clicar no card) está **resolvido ao vivo**.

## 7. Rollback — net-zero verificado

Saída do psql (`r13b2_rollback_sv.sql`):

```
BEGIN
DELETE 2
UPDATE 2
COMMIT
ROLLBACK_TM_HOMOLOG_R13_RESIDUO|0
ROLLBACK_PERM_BI.VER|I|permitido=true
ROLLBACK_PERM_FATURAMENTO_SAAS.VER|I|permitido=true
ROLLBACK_EFETIVOS|ADM360,AGENDAMENTOS,CID,CLINICA_DASHBOARD,CLINICA_FINANCEIRO,CONSULTAS,CONVENIOS,FINANCEIRO,PACIENTES,PAINEL_CHAMADA,PENDENCIAS_CLINICAS,PLANOS_SAUDE,PRESCRICOES,RELATORIOS,SAUDE360,TRIAGEM,UNIDADES,USUARIOS
```

Efetivos retornaram aos **18 exatos pré-grant**; resíduos `HOMOLOG_R13` = 0; permissões voltadas ao dormimento original (`I`, permitido=true intacto).

**Confirmação na UI pós-rollback** (nova sessão: logout → login → navegação): `/FaturamentoSaas/Index` → 302 `module=FATURAMENTO_SAAS&reason=MODULO_NAO_CONTRATADO` novamente. Estado restaurado, duplamente evidenciado.

## 8. Notas de método / ruído capturado (sem impacto no veredicto)

- **Login race mecânico**: na transição pós-rollback, uma navegação encadeada cancelou o POST `/Account/Login` e devolveu `Login?ReturnUrl=…`; refeito em passos separados e assentou (`/ClientePortal/Index`). Sem relação com o fix.
- Limitação conhecida de configuração local (telas fazem `fetch('/api/…')` presumindo origem única, achado r12b2) permanece classificada e não bloqueia: todos os dados exibidos vieram do envelope server-side dos controllers.

## 9. Achados novos → fila R14 (registrados, não bloqueiam o fix)

- **F13-A (P3, Api) — `GET /api/faturamento-saas/faturas` responde 500 com `Npgsql 42P08`:** `não foi possível determinar o tipo de dados do parâmetro $5` (POSITION 172), em `SaasCommercialController.cs` L603–605 — a ação `Faturas` passa `competencia` nulo (`DateOnly?`) no parâmetro `$5` dos dois SQLs (count e lista). Reproduzido 3× na janela (20:58/21:00 UTC), com e sem `?status=VENCIDA`; o tenant é não-global (caminho normal de tenant admin). Pré-existente e fora do perímetro do guard (a porta é upstream da chamada). Endpoints vizinhos saudáveis na mesma janela: `/resumo` 200 · `/inadimplencia` 200. Hipótese de correção p/ R14: tipar o placeholder (`@competencia::date`) ou reestruturar o teste de null sem depender de inferência.
- **F13-B (P3, Web, honestidade) — banner de erro do Index nunca renderizado:** `FaturamentoSaasController.Index` (L26) seta `ViewBag.ErrorMessage = resumo.Error ?? faturas.Error`, mas `Views/FaturamentoSaas/Index.cshtml` **nunca lê `ViewBag.ErrorMessage`** — com o F13-A ativo, a tela degrada silenciosamente para "tabela vazia + zeros" sem banner honesto. Padrão de casa nos demais 18 casos de uso do ViewBag (CentralEscala, Clientes, Operacao, Perfis, Usuarios, etc., greppado nesta rodada) é renderizar `alert-warning/danger`. Correção de 1 linha + fato de contrato; fila R14.

## 10. Veredicto do slice

**Homologação APROVADA.** O fix r13b1 comporta exatamente o projetado: denegações honestas nomeando o módulo canônico, liberação com contrato mínimo reversível, drill-down do BI end-to-end, shell de redirect coberta, rollback net-zero duplamente verificado (banco + UI). Banco devolvido ao estado pré-rodada. Servidores encerrados após as capturas (PIDs 51748/40620).
