# R51 · r51z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R50 (`8e42619`, suíte 1602). Plano: `r51a0-ajuda-central-por-area-plano.md`.

## Escopo da rodada

Roadmap R51: **Ajuda central: "como usar" por área (extensão do E3)** — dar manual "como usar" a cada área
que ainda não tinha, dentro da Central de Ajuda já existente e filtrada por perfil.

## O que foi entregue

### 1. Diagnóstico do gap

A Central de Ajuda (`AjudaController`) já existia com 6 tópicos/13 artigos, filtrados pelo papel primário da
sessão (`RoleCatalog`: maior prioridade vence — `ADMINISTRADOR_CLIENTE`=700 > `ADMINISTRADOR`=650). O buraco:
a família administrador de tenant — o usuário que efetivamente opera o produto (Administrativo360 + Minha
Central) — **não via nenhum tópico próprio**: só "Privacidade LGPD" (TODOS). E as rotinas transversais novas
(fatura vencida R48, notificações R49) não tinham conteúdo TODOS.

### 2. Mudanças (D-R51-1..2, todas em `AjudaController.cs`)

- **D-R51-1**: `NormalizarPerfil` agora mapeia `ADMINISTRADOR` → `ADMINISTRADOR_CLIENTE` (família do mesmo
  escopo Tenant, mesmo `DefaultRoute /ClientePortal`; onboarding atribui os dois perfis). Só a ajuda é afetada:
  um único manual para a família, em vez de nenhum.
- **D-R51-2**: 2 tópicos novos + 7 artigos novos no helper `Artigo(...)` existente:
  - Tópico `instituicao` (Perfil `ADMINISTRADOR_CLIENTE`, ícone buildings) — "Operação da instituição":
    criar orçamento → Orçamentos; registrar pedido de compra → PedidosCompra; acompanhar estoque e
    inventário → Estoque; controlar títulos a receber/pagar → TitulosReceber; relatórios financeiros →
    RelatoriosFinanceiros.
  - Tópico `central` (Perfil `TODOS`, ícone sino) — "Minha Central e avisos": resolver fatura vencida →
    MinhaCentral/Index (fluxo do R48: aviso, valor, competência, pagamento); configurar notificações →
    Notificacoes/Preferencias.
- Copy no tom da casa (R50): frases curtas, imperativo/indicativo, sem jargão banido, sem exclamação, sem
  id/código cru. Todas as rotas de link confirmadas nos controllers antes de escrever.

### 3. Contrato permanente — `R51AjudaContratoTests` (3 fatos)

1. `TopicoInstituicao_CobrePerfilAdministradorCliente` — tópico presente + >= 5 artigos do perfil.
2. `TopicoTodos_CobreFaturaVencidaENotificacoes` — artigos `central-fatura`/`central-notificacoes` com texto
   de "Fatura vencida" em linguagem simples.
3. `FamiliaAdministrador_CompartilhaManual` — a normalização D-R51-1 permanece.

## Evidências

- Build real `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` → **exit 0**; gate de casa
  multiset vs `r17_pristine_build.log` → **160/160, DIFF=NENHUMA**.
- Classes dirigidas (R51 + R50 + R49 + higiene + R7DF6 i18n + R39 tenant): **28/28 verde**.
- Suíte integral: **1605/1605** (1602 base R50 + 3 fatos novos). A execução em background
  (`%TEMP%\opencode\r51_suite.log`, exit 1) caiu em **3 testes de carga no banco sob parallelismo**
  (`R19DapperBindingGuardTests.Db_CentralClientes_…` — família flake documentada — mais
  `Administrativo360OcorrenciasA12Tests.A12_…` e `Administrativo360CotacoesXmlDashboardTests.Aceite17_…`);
  isolamento imediato dos três → **3/3 verde**, e a suíte integral em foreground fechou **verde 1605/1605**.

## Pendências documentadas (pra frente)

- Artigos para perfis operacionais ainda sem manuais próprios além dos existentes (ex.: `DIRETOR`,
  `RECEPCAO`, `PARCEIRO`) — expansão de conteúdo, não de infra.
- Tour guiado por área (o botão "Fazer tour" da central existe — `data-tour-start="help-center"`) pode
  consumir os artigos como roteiro em R52+.

## Commits

| Commit | Conteúdo |
|---|---|
| (a seguir) `web,tests (rodada 51 r51b1)` | ajuda por área: 2 tópicos + 7 artigos + D-R51-1 + contrato |
| (este) `docs (rodada 51 r51z)` | este relatório |
