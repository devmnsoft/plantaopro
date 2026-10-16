# Rodada 12 — Auditoria inicial e plano da sprint (fix do catálogo do guard Bi/DashboardsPremium)

- **Data:** 2026-10-16 · **Base:** `10a2171` (ponta do push da Rodada 11) · **Escopo:** item 1 (P2) da fila R11 — telas `Bi` (BI Executivo) e `DashboardsPremium` órfãs do catálogo do guard (`CATALOGO_NAO_CONFIGURADO` para todo autenticado), achado vivo no `r11b2`. Fora: exposição em menu (decisão de produto), gaps de drill-down de alvo (R10), janela UTC Meu Dia, tiles B18, especialidade B16, pg_dump P5 — seguem na fila §7.
- **Regra de push:** respeitada — commits locais em `main`, push só com autorização ao fim da rodada.

## 1. O que a dívida dizia

`r11b2` §4 (reproduzido 2× ao vivo): navegar para `/Bi/Index` ou `/DashboardsPremium/AdministradorCliente` redireciona para `Account/AccessDenied?module=CATALOGO_NAO_CONFIGURADO` **para qualquer usuário autenticado**, inclusive global admin. Causa raiz: `SaasRouteGuardFilter.ControllerModules` não tem entrada para `BiController` nem para `DashboardsPremiumController`; controllers fora do catálogo são barrados na checagem de catálogo **antes** de qualquer verificação de módulo/papel/contratação. Nenhum `.cshtml` referencia esses controllers (telas sem link de menu) — por isso o furo só apareceu agora. Mesmo padrão dos defeitos já corrigidos pela casa: R6-BlocoA item 1 (`Caixa`→`CLINICA_FINANCEIRO`) e R7-E1 (`Usuario`→`CONTA`).

Impacto real: baixa para usuários (tela nunca linkada), mas **bloqueia integralmente a homologação dos 7 drill-downs** da R10 (§27-L208) e faria a página quebrar para todos no dia em que um menu as expuser.

## 2. Recon por fonte canônica

### 2.1 Catálogo de módulos e permissões (banco de homologação, leitura)
- `modulos_sistema` já contém os dois módulos-alvo, com suíte completa de ações (31 cada, incluindo `.VER`):
  - `BI` → id `a8696229-8a03-4b56-92eb-111209c89489`; permissão `BI.VER` = `2db053e9-…`.
  - `DASHBOARD` → id `80e5f613-134a-41db-8e9d-73a23996173e` ("Dashboard / Indicadores e visão geral"); permissão `DASHBOARD.VER` = `0664b9cc-…`.
- **Não existe** módulo `PREMIUM`/`PERFIL`/`DASHBOARD_PREMIUM`. `CLINICA_DASHBOARD` ("Dashboard Clínico") já é dono de outro domínio — `ClinicaDashboardController`→`CLINICA_DASHBOARD` — logo não pode adotar as telas de perfil (seria confluência de domínios distintos). O módulo `DASHBOARD` está **livre** no guard (nenhum controller o domina) e é semânticamente o dono de dashboards executivos/de perfil.
- Decisão de mapeamento (fonte: semântica do controller + dono atual do módulo):
  - `["Bi"] = "BI"` — BI Executivo é literalmente o módulo `BI`.
  - `["DashboardsPremium"] = "DASHBOARD"` — dashboards executivos de perfil; `DASHBOARD` livre; alternativa `CLINICA_DASHBOARD` descartada (domínio distinto, já dominado).

### 2.2 Resolvente de ação (leitura do guard)
`ResolvePermissionActionGeneric` mapeia `Create/Criar→CRIAR`, `Edit/Editar→EDITAR`, `Delete/Excluir/Cancelar→EXCLUIR`, `Exportar→EXPORTAR`, `MarcarPaga/Confirmar→CONFIRMAR`, e **default `VER`**. As ações dos dois controllers não batem em nenhum verbo de escrita:
- `Bi`: Index, Financeiro, Medicos, Clientes, Sla, Operacional, Clinico, Convenios, Saas, Widgets, Alertas → todas `VER`.
- `DashboardsPremium`: AdminGlobal, AdministradorCliente, Coordenacao, Medico, Financeiro, Saude360 → todas `VER`.
Logo o gate efetivo fica em `BI.VER` / `DASHBOARD.VER` + módulo contratado. **Nenhum `PermissionActionOverrides` necessário.**

### 2.3 Modelos de permissão/módulo em jogo (o que libera o acesso)
`PermissionService.HasPermission(m,a)` + `ModuleAccessService.IsModuleEnabled(m)`:
- Global admin (`IsGlobalAdmin`) passa direto nos dois.
- Módulo em `CoreModules`/`TenantAdministrationModules` (para tenant admin) bypassa contratação — nem `BI` nem `DASHBOARD` estão nesses conjuntos, então as telas seguem **porteadas por contrato** (comportamento correto para tela não-core).
- Demais perfis: exigem claim de módulo `BI`/`DASHBOARD` (via `modulos_efetivos(tenant)` — contrato `tenant_modulos` vigente) **e** permissão de perfil `*.VER` (ou `*.*`). Sem contrato → motivo `MODULO_NAO_CONTRATADO`; com contrato sem permissão → `PERMISSAO_NEGADA`.

### 2.4 Estado do tenant SV (contexto da homologação)
- Módulos efetivos de SV (`modulos_efetivos`): 18 módulos (ADM360, AGENDAMENTOS, CID, CLINICA_DASHBOARD, CLINICA_FINANCEIRO, CONSULTAS, CONVENIOS, FINANCEIRO, PACIENTES, PAINEL_CHAMADA, PENDENCIAS_CLINICAS, PLANOS_SAUDE, PRESCRICOES, RELATORIOS, SAUDE360, TRIAGEM, UNIDADES, USUARIOS). **Sem `BI`, sem `DASHBOARD`.**
- Perfil do admin SV = `Administrador Sistema Vertical` (perfil tenant `0941eb60-…`): hoje `0/31` de `BI.*` e `0/31` de `DASHBOARD.*`.
- Consequência: mesmo com o fix, SV segue negado até contratar `BI`+`DASHBOARD` e ganhar as permissões do perfil (grant reversível, estilo r8b26 — ver §5).

## 3. Mudança planejada (código)
1. `SaasRouteGuardFilter.cs` — adicionar ao dicionário `ControllerModules`:
   - `["Bi"] = "BI"`
   - `["DashboardsPremium"] = "DASHBOARD"`
   com comentário de casa citando o achado `r11b2` e a decisão de dono de módulo (mesmo formato dos comentários de `Caixa`/`Usuario`/`ClinicaUnidades`).
2. **Sem** overrides de ação, **sem** mudança de view/controller/API/DDL/resx (nenhuma literal nova de UI).

## 4. Contrato de teste (anti-regressão)
Classe nova `R12GuardBiPremiumCatalogTests` (fonte-scan estilo E13/R6 + comportamento web estilo R6A1/D9):
- `Guard_MapeiaBiEPremiumParaModulosCanonicos` — source-scan: o guard contém `["Bi"] = "BI"` e `["DashboardsPremium"] = "DASHBOARD"`.
- `Telas_ExistemEAsAcoesResolvemParaLeitura` — as views alvo existem (`Views/Bi/Index.cshtml`, `Views/DashboardsPremium/Perfil.cshtml`).
- Comportamento (coleção `web-bff`, sessão com claims controlados):
  - `/Bi/Index` **sem** claim `BI` → redirect `/Account/AccessDenied` com `reason=MODULO_NAO_CONTRATADO` (e **não** `CATALOGO_NAO_CONFIGURADO`).
  - `/Bi/Index` **com** `BI` mas **sem** `BI.VER` → `reason=PERMISSAO_NEGADA`.
  - `/Bi/Index` **com** `BI` + `BI.VER` → `200` e corpo contém o título "BI Executivo".
  - Idem tripla para `/DashboardsPremium/AdministradorCliente` (módulo `DASHBOARD`, permissão `DASHBOARD.VER`; `200` contém "Administrador Cliente").
  - Global admin **sem** claims de módulo → `200` em ambos (bypass canônico preservado).

## 5. Gate de suíte (Release)
| Porta | Resultado | Nota |
|---|---|---|
| Baseline (`10a2171`) | **1428/1428 limpo** | build `EXIT 0`; o flake `R7OB…O3` (catálogo ①) não repetiu nesta base |
| Pós-r12b1 | **Total 1437 · Aprovados 1436 · 1 falha = flake catálogo ① conhecido** (`ProvisionamentoB5Tests.P1_Finalizar…`, `40P01 deadlock` no teardown `PurgarAsync()` — isolate **5× → 11/11 em cada pass**) | Δ registrado **+9 fatos** (classe `R12GuardBiPremiumCatalogTests` + 2 classes); detalhes no `r12b1-fix-catalogo-guard-bi-premium.md` §5 |

## 6. Slices da rodada
| Slice | Entrega | Status |
|---|---|---|
| `r12b1` | Fix do catálogo do guard (`Bi`→`BI`, `DashboardsPremium`→`DASHBOARD`) + classe de contrato + gate de suíte | FEITO (commitado `ae76e36`) |
| `r12b2` | Homologação viva: subir Api/Web, grant reversível `BI`+`DASHBOARD` ao SV, validar `/Bi` e `/DashboardsPremium` (negado→liberado), e aproveitar o ambiente para capturas de pixel pendentes da R11 | FEITO — ciclo negado-honesto→liberado provado ao vivo; 4/7 drill-downs abertos; capturas R11 fechadas (fallback DOM); net-zero verificado; achado P3 BILLING. Ver `r12b2-homologacao-viva-guard-bi-premium-drilldowns.md` |
| `r12z` | Relatório final consolidado + fila próxima | PENDENTE |

## 7. Decisões / fronteiras registradas
- **Exposição em menu fora de escopo**: este fix torna as rotas *resolúveis e porteadas por contrato*, mas **não inventa link de menu** (regra de casa: UI nova exige decisão de produto). Até que um menu aponte para elas, `/Bi` e `/DashboardsPremium/*` são acessíveis por URL direta quando contratados. A decisão de onde expor segue na fila.
- **Porteada por contrato (não core)**: `BI`/`DASHBOARD` seguem o gate de contratação como qualquer módulo não-core — comportamento desejado, coerente com o resto do catálogo.
- **Alternativa rejeitada** `DashboardsPremium`→`CLINICA_DASHBOARD`: confluiria telas de perfil multi-domínio sob um módulo que já é dono de outra tela (`ClinicaDashboard`); e rejeitado o criação de um novo módulo `PREMIUM` (exigiria DDL + expansão de contrato — fora de escopo para um fix de catálogo).
