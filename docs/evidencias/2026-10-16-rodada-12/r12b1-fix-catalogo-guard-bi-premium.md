# Rodada 12 r12b1 — Fix do catálogo do guard: `Bi` → `BI` e `DashboardsPremium` → `DASHBOARD`

- **Data:** 2026-10-16 · **Base:** `9720677` (r12a0) · **Ponta:** `backend/PlantaoPro.Web/Services/SaasRouteGuardFilter.cs` + nova classe de contrato · **Status:** FECHADO (gate verde abaixo)
- **Fechamento de:** item 1 (P2) da fila R11 (`r11z`) — achado reproduzido 2× ao vivo em `r11b2` §4 da rodada anterior.

## 1. O defeito

`BiController` (BI Executivo) e `DashboardsPremiumController` (dashboards executivos por perfil) não tinham entrada no dicionário `SaasRouteGuardFilter.ControllerModules`. O guard trata controller fora do catálogo como erro de configuração e redireciona **todo usuário autenticado — inclusive global admin** — para `Account/AccessDenied?module=CATALOGO_NAO_CONFIGURADO&reason=CATALOGO_NAO_CONFIGURADO`, **antes** de qualquer verificação de módulo contratado ou permissão de perfil.

Prova viva (R11): artefatos `artefatos-dom/accessdenied-bi.html` e `accessdenied-premium.html` + 2 reproduções ao vivo no tenant SV (`admin.sistemavertical@plantaopro.local`). Impacto concreto além da tela quebrada: os 7 drill-downs da R10 (§27-L208, cards Bi/Premium) só podem ser homologados clicando nesses dois controllers — estavam integralmente bloqueados. Nenhum `.cshtml` referencia esses controllers (telas sem link de menu) — o furo é do mesmo padrão já corrigido pela casa em R6-BlocoA item 1 (`Caixa`→`CLINICA_FINANCEIRO`) e R7-E1 (`Usuario`→`CONTA`).

## 2. Fontes canônicas consultadas (leitura do banco de homologação)

| Item | Fonte | Resultado |
|---|---|---|
| Módulo dono de `/Bi/*` | `modulos_sistema` | `BI` existe — id `a8696229-8a03-4b56-92eb-111209c89489`; suíte completa de ações em `permissoes` (31), incluindo `BI.VER` (`2db053e9-…`) |
| Módulo dono de `/DashboardsPremium/*` | `modulos_sistema` | `DASHBOARD` existe — id `80e5f613-134a-41db-8e9d-73a23996173e` ("Indicadores e visão geral"); suíte completa (31), incluindo `DASHBOARD.VER` (`0664b9cc-…`); **livre no guard** (nenhum outro controller o domina); **não existe** módulo `PREMIUM`/`PERFIL`/`DASHBOARD_PREMIUM` |
| Alternativa rejeitada | semântica + dono atual | `CLINICA_DASHBOARD` ("Dashboard Clínico") é domínio distinto já dominado por `ClinicaDashboardController` — adotá-lo confluiria duas telas diferentes sob um módulo |
| Ações dos controllers | leitura de `BiController.cs` / `DashboardsPremiumController.cs` | todas as ações (`Index/Financeiro/Medicos/Clientes/Sla/Operacional/Clinico/Convenios/Saas/Widgets/Alertas` e `AdminGlobal/AdministradorCliente/Coordenacao/Medico/Financeiro/Saude360`) resolvem para `VER` via `ResolvePermissionActionGeneric` — telas somente leitura, **sem overrides de ação necessários** |
| Core/comum | `ModuleAccessService.CoreModules` | `BI` e `DASHBOARD` **não** são core → seguem porteados por contrato (comportamento desejado para módulos não-core) |
| Estado do tenant SV | `tenant_modulos` / perfil admin | SV não tem `BI` nem `DASHBOARD` contratados; perfil `Administrador Sistema Vertical` (`0941eb60-…`) tem 0/31 de `BI.*` e 0/31 de `DASHBOARD.*` → após o fix, SV segue negado **com motivo honesto** até contratação reversível (homologação r12b2) |

## 3. Mudança aplicada

`SaasRouteGuardFilter.ControllerModules` — 2 entradas novas (com comentário de casa citando o achado e a decisão de dono de módulo), agrupadas junto à família dashboard:

```csharp
["ClinicaDashboard"] = "CLINICA_DASHBOARD",
// R12-B1 (achado r11b2, reproduzido ao vivo 2x): Bi (BI Executivo) era orfa do catalogo — ...
["Bi"] = "BI",
// R12-B1: DashboardsPremium (dashboards executivos por perfil) tinha o mesmo padrao de orfao. ...
["DashboardsPremium"] = "DASHBOARD",
```

Sem DDL, sem mudança em API/controller/view/resx, sem `PermissionActionOverrides`. Contrato i18n (650 chaves) inalterado.

## 4. Contrato anti-regressão (novo arquivo)

`backend/PlantaoPro.Tests/R12GuardBiPremiumCatalogTests.cs` — **+9 testes** (esperado da suíte: 1428 → 1437):

| Fato | Trava |
|---|---|
| `Guard_MapeiaBiEPremiumParaModulosCanonicosDoCatalogo` | source-scan (estilo E13/R6): as 2 entradas existem no guard — regressão ao estado órfão quebra a suíte |
| `TelasAlvo_ViewsExistentes_NaoHaRotaParaViewAusente` | source-scan: `Views/Bi/Index.cshtml` e `Views/DashboardsPremium/Perfil.cshtml` existem (rota resolvida não aponta para view ausente) |
| `Bi_SemModuloContratado_NegaComModuloNaoContratado_NaoCatalogoInexistente` | comportamento (web-bff): sessão sem claim `BI` → redirect `AccessDenied` com `module=BI&reason=MODULO_NAO_CONTRATADO` e **sem** `CATALOGO_NAO_CONFIGURADO` — o defeito original volta a dar falha aqui |
| `Bi_ComModuloSemPermissao_NegaComPermissaoNegada_NaoSucessoFalso` | claim `BI` presente, sem permissão `BI.*` → `reason=PERMISSAO_NEGADA` (separação de motivos R6-BlocoA preservada; nunca sucesso falso) |
| `Bi_ComModuloEPermissao_RenderizaPaginaBiExecutivo` | `BI` + `BI.*` → **200** e HTML contém "BI Executivo" |
| `DashboardsPremium_SemModuloContratado_NegaComModuloNaoContratado_NaoCatalogoInexistente` | idem tripla de negação para a segunda tela (`module=DASHBOARD`) |
| `DashboardsPremium_ComModuloEPermissao_RenderizaPerfilAdministradorCliente` | `DASHBOARD` + `DASHBOARD.*` → **200**, h1 "Dashboard Administrador Cliente", estado vazio honesto ("Sem indicadores reais") quando a API não traz KPIs — stub responde envelope canônico |
| `GlobalAdmin_SemClaimsDeModuloOuPermissao_AceitaTela[/Bi/Index][Premium]` (theory) | bypass canônico de global admin **preservado**: no defeito original ele caia em `CATALOGO_NAO_CONFIGURADO`; agora 200 sem claims de módulo |

Coleção `web-bff` (live-check desligado, decisão determinística pelos claims da sessão, fábrica única serializada). Asserções de motivo usam exatamente os códigos canônicos do guard — uma regressão silenciosa para `CATALOGO_NAO_CONFIGURADO` falha 4 fatos diferentes.

## 5. Gate (Release)

| Porta | Resultado |
|---|---|
| Build Release | `BUILD_EXIT 0` · 0 erros (aviso: a 1ª soltura foi interrompida pelo restart do servidor OpenCode com log vazio e `TEST_EXIT -1`; execução definitiva abaixo) |
| Suíte full | `Total de testes: 1437` (1428 base R11 + 9 novos) · **Aprovados: 1436** · 1 falha = **flake catálogo ① conhecido** (`ProvisionamentoB5Tests.P1_Finalizar_CriaMundoTodo_E_AdminAutentica` — `Npgsql 40P01 deadlock detectado` no `PurgarAsync()` do teardown/`Dispose()`, infra de teste, sem interseção com o slice) · log `Temp\opencode\r12b1_suite.log` |
| Flake isolado (regra de casa: 5× antes de tratar como regressão) | **5 passes × 11/11 aprovados, 0 falhas** (`Temp\opencode\r12b1_flake_pass1..5.log`) → flake ambiental confirmado, não regressão |
| Fatos novos R12 | **9/9 aprovados** tanto na full (linhas do log citadas acima) quanto em isolate (`--filter FullyQualifiedName~R12GuardBiPremium`) |

## 6. Decisões registradas

- **Exposição em menu = decisão de produto (fila)**: o fix torna as rotas *resolúveis e porteadas por contrato*; nenhum link de menu foi inventado (regra de casa). Até a decisão, `/Bi` e `/DashboardsPremium/*` são acessíveis por URL direta para sessões contratadas.
- **Porteadas por contrato (não-core)**: coerente com o restante do catálogo — sem contrato o motivo passa a ser o honesto `MODULO_NAO_CONTRATADO`; com contrato sem permissão, `PERMISSAO_NEGADA`.
- **Alternativa rejeitada** `DashboardsPremium`→`CLINICA_DASHBOARD` (confluiria domínios) e criação de módulo novo `PREMIUM` (exigiria DDL + expansão de contrato — fora do escopo de um fix de catálogo).
