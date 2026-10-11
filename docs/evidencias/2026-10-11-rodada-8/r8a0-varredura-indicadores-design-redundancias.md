# R8-A0 — Varredura de evidência: §27–§29 (indicadores, design de ações complexas, redundâncias)

Data: 2026-10-11 · Método: exploração read-only do HEAD `2f4533d3`. Caminhos relativos a `backend/` salvo indicação.
Classificações: pronto com evidência / parcial / quebrado / ausente após busca / não verificado.

---

## Área A — Indicadores com definição e rastreabilidade (§27)

### A1. Dashboards/KPIs existentes + onde calculados + definição explícita
**ESTADO: PARCIAL** — existem ~7 famílias de painéis; apenas uma tem definição por indicador exposta ao usuário.

| Painel | Indicadores | Cálculo (file:line) | Fórmula/período/fuso/filtros explícitos? |
|---|---|---|---|
| BI Executivo | TotalClientesAtivos, ReceitaMensalEstimada, ReceitaVencida, PlantoesPublicadosMes, PercentualCobertura, EscalasConfirmadas(+Período), PagamentosPendentes/Confirmados, TempoMedioPreenchimentoHoras, ValorPagoPeriodo | `PlantaoPro.Api/BiServices.cs:44-64` (SQL inline) | **Sim**: janela mês no fuso (:45-49), validação de fuso em pg_timezone_names (:36-41), comentário B9 documentando "KPIs de período vs estado atual" (:43). View `Web/Views/Bi/Index.cshtml:19` |
| Meu Dia | Active, Critical, Today, Overdue, Snoozed | `PlantaoPro.Api/ProductivityActionServices.cs:193-209` (CTE `derived`, `count filter`); facade `PlantaoPro.Api/MeuDiaServices.cs:29-34` | Parcial: período = `UtcNow`→`+1 dia` fixo (`MeuDiaServices.cs:29`) — **fuso UTC, não do usuário**; sem definição mostrada na UI (`Web/Views/MeuDia/Index.cshtml:110-115`) |
| Manager Command Center | Today, Uncovered, PendingConfirmation, Critical, AvailableProfessionals, PendingCheckIns, OpenIncidents, PendingReplacements, FinancialPending, CriticalNotifications + risk 0-100 | `PlantaoPro.Api/ManagerCommandCenterService.cs:44-98` (SQL); risco via `Domain/Escalas/ShiftRiskCalculator` (:239-245) | Parcial: limites de período validados (máx. 93 dias, :144-148), `GeneratedAt` exposto (:176; view `Web/Views/CommandCenter/Index.cshtml:15`), mas fórmulas não documentadas na UI; janelas `current_date`/`now()` no fuso do servidor |
| Relatórios Operacionais (Cobertura/Execução/Apuração) | 5 indicadores por tipo | `PlantaoPro.Api/OperationalReportService.cs:81-88` | **Sim, o único com definição completa**: `Definition` textual por indicador (:81-84) + critério de tempo (:79, "intervalo semiaberto no fuso persistido") — renderizado na UI `Web/Views/Relatorios/Operacional.cshtml` (card `<p>@i.Definition</p>`) |
| Dashboards Premium (6 perfis) | Contagens brutas Plantões/Escalas/Médicos/Agendamentos | `PlantaoPro.Api/DashboardPremiumService.cs:20-23` | Não: contagens totais, sem período/filtros; `Fonte="real"` genérica (:41) |
| Dashboard API (`api/dashboard`) | Contagens médicos/hospitais/plantões por status, pagamentos, valores, charts mensais | `PlantaoPro.Api/Data.cs:2339-2357` | Não: `now()` no servidor, sem fuso nem definição |
| Fase6 Dashboard BI | clientes_ativos / plantoes_mes / agendamentos / total_receber | `PlantaoPro.Api/Fase6BiIntegracoesServices.cs:43-58` (KPI tem campo `Explicacao`, :10) | **Inconsistência**: label `"Plantões do mês"` (:56) mas SQL conta **todos** os plantões sem filtro de mês (:53) |
| Resumo Admin 360 | Departamentos, Cargos, ColaboradoresAtivos, ContratosVigentes | `PlantaoPro.Api/Administrativo360Service.cs:26-34` | Não: contagens por tenant, sem período |
| Saúde 360 "resumo" | `ResumoConsultaAsync` = impressão de 1 registro | `PlantaoPro.Api/Saude360ClinicalService.cs:383-387`; `Api/Controllers/WorkflowSaude360Controller.cs:17` | Não é agregado; é detalhe de consulta |

### A2. Zero vs "informação indisponível" distinguido na UI?
**ESTADO: PARCIAL**
- EVIDÊNCIA (distingui): `Web/Views/Relatorios/Operacional.cshtml` ("Nenhum registro corresponde aos filtros. Isso não é uma falha de carregamento."); `Web/Views/Home/Dashboard.cshtml:51`; `Web/Views/Pagamentos/Index.cshtml:13`; `Api/DashboardPremiumService.cs:32` ("Dados reais indisponíveis neste ambiente"); partial compartilhado `Web/Views/Shared/_EmptyState.cshtml:5`.
- EVIDÊNCIA (NÃO distingue): `Web/Views/Bi/Index.cshtml:19` usa `??0`/`toFixed` sem `.catch` — falha de API exibe **0 / 0,0%** como valor real; KPIs de `MeuDia/Index.cshtml:111-114` exibem 0 sem estado.

### A3. Percentuais com denominador vazio (guards div-by-zero)
**ESTADO: PRONTO COM EVIDÊNCIA** (todos em SQL; sem helper compartilhado)
- `Api/UnitPortalServices.cs:32` → `/ nullif(sum(p.quantidade_vagas), 0)`
- `Api/Controllers/OperacaoAssistidaController.cs:67,104,477` → `/ nullif(count(1),0)`
- `Api/BiServices.cs:55` → `case when p.vagas > 0 then … else 0 end`
- `Api/Controllers/ImplantacaoController.cs:56` → `checks.Count == 0 ? 0 : ok * 100m / checks.Count`
- `Api/Data.cs:1843` → `Math.Max(1m, horas)` antes da divisão
- OBS: pattern ad-hoc em cada consulta; nenhum utilitário de domínio para "taxa".

### A4. Drill-down dos totais (abrir registros que compõem o indicador)
**ESTADO: PARCIAL**
- Existe: `Web/Views/Relatorios/Operacional.cshtml` — card com link **"Ver registros"** preservando todos os filtros + tabela "Registros de origem" com link **"Abrir origem"** por linha; `Web/Views/CommandCenter/Index.cshtml:138` ("Ver detalhe"); `Web/Views/MeuDia/Index.cshtml:157` (botão PrimaryAction por item).
- Não existe: cards KPI de `Bi/Index.cshtml:12-15` e `MeuDia/Index.cshtml:111-114` são `<article>` **sem link** aos registros base; `DashboardsPremium/Perfil.cshtml` sem drill-down.

### A5. Export com mesmos filtros + consolidado respeita permissões?
**ESTADO: PRONTO COM EVIDÊNCIA (caminhos principais) / PARCIAL (abrangência)**
- `Api/OperationalReportService.cs:47-50` — `CsvAsync` delega a `GetAsync` (mesmo `Sql(kind)`, mesmos filtros) e herda a guarda de permissão `permission.ValidarPermissaoAsync` (:33-34, exige `FinanceiroVer` p/ Apuração), `TenantGuardService.PodeAcessarHospital/Medico` (:36-37) e escopo tenant (:38). Web passa filtros idênticos: `Web/Controllers/RelatoriosController.cs:34`.
- `Api/Controllers/RelatoriosSaasController.cs:98-132` — export reusa `ObterDadosAsync` (mesma fonte, :104), com `assinaturaGuard.PodeUsarRelatoriosAvancadosAsync` + auditoria de negação (:124-128); limitado a 500 linhas.
- OBS: relatórios SaaS não têm parâmetros de filtro (só page); não há export CSV em `Adm360RelatoriosController` (só GETs); exporta CSV de relatórios financeiros em `Web/Controllers/Administrativo360Controller.RelatoriosFinanceiros.cs:86`.

---

## Área B — Design de ações complexas (§28)

### B1. Multi-step wizards com passos visíveis
**ESTADO: PARCIAL**
- `Web/Views/Onboarding/NovoCliente.cshtml:8-10` — stepper `pp-stepper` com 5 passos visíveis (`data-step=1..5`, `aria-current="step"`): Empresa/Plano/Administrador/Revisão/Conclusão; sidebar "Antes de concluir" (:50). **Porém é um único form POST** (:11, :48) — etapas visuais, sem navegação por passo real. Controller descreve como "wizard MNSOFT" (`Web/Controllers/OnboardingController.cs:13`).
- Importações: `Web/Views/Administrativo360/DocumentosXml/Importar.cshtml:28-56` — form único (múltiplos docs por arquivo, hash SHA-256), **não** wizard. CID import = endpoint API único (`Saude360ClinicalService.cs:412-449`).
- Infraestrutura CSS existe: `pp-stepper` (v2140-guided-journeys.css:3), `pp-wizard-layout`; etapas de Implantação = checklist, não wizard (`Web/Views/Implantacao/Index.cshtml:30`).

### B2. Summary-before-confirm para ações de grande efeito
**ESTADO: PRONTO COM EVIDÊNCIA (padrão geral) / AUSENTE (2 casos específicos)**
- Padrão modal `data-confirm` + partial `Web/Views/Shared/_ConfirmModal.cshtml`; usos com efeito e mensagem descritiva: `Escalas/Details.cshtml:63`, `Financeiro/Details.cshtml:98`, `Assinaturas/Details.cshtml:18` (motivo obrigatório 500 chars), `Plantoes/Details.cshtml:196`, `Administrativo360/Inventarios.cshtml:93,105`, `PedidosCompra.cshtml:128`, `MinhaAgenda/Presencas.cshtml:12`, `Usuarios/Form.cshtml:23` (impacto: revogação de sessões).
- Decisões amplas exigem justificativa + revalidação de versão: `ConferenciaExecucao/Index.cshtml:23`, `CentralEscala/SubstituicaoDetails.cshtml:10`.
- **Ausente** — "cancelar compra recebida": `Administrativo360Controller.ComprasRecebimentos.cs` tem ações criar/aprovar/receber/inspecionar/ocorrências (:10-232) mas **nenhuma ação de cancelamento de recebimento**. Buscas: `CancelarCompra|cancelar.*compra|compra.*cancelad` → 0 matches.
- **Ausente** — "mesclar paciente": busca `[Mm]esclar` em *.cs/*.cshtml/*.sql do repo inteiro → 0 matches.

### B3. Unsaved-changes warning (beforeunload/dirty tracking)
**ESTADO: PRONTO COM EVIDÊNCIA**
- Implementação central: `Web/wwwroot/js/form-experience.js:40-55` — dirty flag, banner `.pp-unsaved-changes-banner`, `beforeunload → event.preventDefault()` (:55).
- Variantes: `clinical-workspace.js:115-117` (autosave 1.8s + beforeunload).
- Ativo em ≥12 forms com `data-unsaved-form`: Pacientes/_Form, Agendamentos/_Form, Assinaturas/Create, Hospitais/_HospitalForm, Medicos/_MedicoForm, Especialidades/_EspecialidadeForm, Plantoes/_PlantaoForm, Saude360/Formulario, Usuarios/Form, Cadastro/Cadastro, MinhaAssinatura/Modulos, Perfis/Form.
- OBS: só aplica a forms marcados; sem cobertura automática.

### B4. Draft state com indicador explícito (rascunho de consultas clínicas?)
**ESTADO: PARCIAL**
- Rascunho existe em: `plantoes.status ∈ ('rascunho',…)` (`backend/sql/20260605_beta_homologavel_constraints.sql:33`; "rascunho" excluído de KPIs em `BiServices.cs:54`); `adm360_pedidos.situacao RASCUNHO` (`database/migrations/2026_09_v2191…sql:8`); orçamentos cirúrgicos (`v2192:46`, regra `PodeEditar("RASCUNHO")` testada em `Tests/Administrativo360EstoqueEOrcamentosTests.cs:105`); vales (`v2193:61`).
- **Consultas clínicas NÃO têm rascunho**: `database/migrations/2026_saude360_consultas_base.sql:18` → `status text not null default 'AGUARDANDO'` (fluxo AGUARDANDO/EM_ATENDIMENTO/FINALIZADA/CANCELADA por `Saude360ClinicalService.cs:318`); nenhuma coluna "rascunho". Buscas: `'RASCUNHO'|'rascunho'` repo-wide + migration de consultas + views `Views/Saude360` → sem hit para consultas.

### B5. Recuperação após erro sem perder form (round-trip preserva valores)
**ESTADO: PRONTO COM EVIDÊNCIA (spot-check 3 forms OK)**
- Padrão dominante `if (!ModelState.IsValid) return View(model)`: EspecialidadesController :24,47; HospitaisController :23,45; EscalasController :46; AssinaturasController :36-114; ConfiguracoesController :59; FinanceiroController :35 (grep `return View(model)` → 40+ hits).
- Spot-checks: (1) Especialidades/Hospitais — model reidratado; (2) decisão de conferência — `TempData.Peek("DecisionReason")` preserva justificativa após erro (`ConferenciaExecucao/Index.cshtml:23`); (3) Substituição — `@savedReason` preserva valor do textarea (`CentralEscala/SubstituicaoDetails.cshtml:10`).
- OBS: MVC server-side; AJAX forms (`data-ajax-form="true"`) dependem do JS próprio — não verificado se reidrata campos em falha 5xx.

### B6. Diferenciação visual visualizar/editar/aprovar/executar
**ESTADO: PARCIAL**
- EVIDÊNCIA: verbo→estilo consistente em domínios novos: aprovar = `btn-primary`/`btn-outline-success` + ícone check (`Inventarios.cshtml:100`, `SubstituicaoDetails.cshtml:10`, `PedidosCompra.cshtml:134`); recusar/cancelar = `btn-outline-danger` + `data-confirm-type="danger"`; visualizar = link suave `button-subtle` / "Ver detalhe →" (`Convites/Index.cshtml:20`, `CommandCenter/Index.cshtml:138`); badges de status por partial `_StatusBadge`.
- Contraste: domínios antigos usam `btn-primary` para ações de risco e botões cinza genéricos `disabled title="Ação indisponível"` (`Relatorios/Sla.cshtml:6`, `_PageHeader.cshtml:31`). Sem regra no design system CSS mapeando verbo→cor.

### B7. Botões disabled explicam O PORQUÊ + servidor continua autorizando?
**ESTADO: PARCIAL**
- Tooltips existem mas a maioria é **genérica** ("Ação indisponível"): `Usuario/Index.cshtml:25`, `_EmptyState.cshtml:27,38`, `_ActionToolbar.cshtml:21,31`, `_FilterPanel.cshtml:18`, `_ModuleBanner.cshtml:25`, `Inteligencia/Dashboard.cshtml:19`.
- Casos com motivo específico: `Convites/Index.cshtml:20` (`title="Reenvio não está contratado neste endpoint."`), `MinhaAgenda/PlantoesDisponiveis.cshtml:7` (button muda texto por motivo), partials `_ModuleLockedNotice`/`_PremiumLockCard` (motivo contratual).
- Autorização server-side independente existe: `[Authorize(Roles=…)]` (ex.: `CommandCenterController.cs:8`), `PermissionGuardService.ValidarPermissaoAsync` + `TenantGuardService` (`OperationalReportService.cs:33-37`), guards de contrato de módulo.

### B8. Operações em lote: seleção/contagem/efeito/resultado por item?
**ESTADO: PARCIAL (1 fluxo real)**
- `Web/Views/Administrativo360/ComissoesPendentes.cshtml:98-147` — select-all + checkboxes por linha + contagem no badge + painel de confirmação "Gerar Obrigação a Pagar (AP)" com vendedor/efeto; servidor processa em lote transacional idempotente (`ContasPagarRepository.cs:686-777`: soma :722, update bulk :772-777).
- Não verificado relatório de resultado item-a-item após a operação. Demais listagens são operações item a item.

### B9. Tabelas com texto longo + muitos registros (paginação? truncation?)
**ESTADO: PARCIAL**
- Paginação: parcial `_Pagination.cshtml`; inputs hidden de página mantêm filtros nos forms (`Administrativo360/Cargos.cshtml:43,173,214`, `Colaboradores.cshtml:229,261`, `Departamentos.cshtml:43`); limite padronizado no backend max 100/lote (`Administrativo360Service.cs:248`).
- Truncation CSS: `.pp-truncate {overflow:hidden;text-overflow:ellipsis}` (`wwwroot/css/design-system/utilities.css:3`) + ellipsis em topbar/sidebar/badges — porém **nas células das tabelas de dados** o tratamento é `table-responsive`/`min-width:800px` (scroll horizontal), sem truncation por célula padrão.

---

## Área C — Redundâncias potenciais (§29) — mapa de candidatos (sem conclusão)

### C1. Cadastros duplicados/sobrepostos
1. **CID schema em 6 definições divergentes** de `create table if not exists plantaopro.cid_tabela`: `database/seeds/2026_demo_comercial_saude360.sql:10`, `database/seeds/2026_saude360_demo_seed.sql:17`, `database/migrations/2026_fix_cid_tabela_schema.sql:3`, `2026_plantao_pro_saude360_modulos_clinicos.sql:663`, `2026_saude360_cid_oficial.sql:6` (com versao/fonte/vigência), `2026_saude360_cid_prescricao.sql:4`. Consumidor: `Saude360ClinicalService.cs:412-449` (import). Risco: primeiro `if not exists` define o schema real.
2. **`parceiros` (B2B) vs `adm360_parceiros`**: `database/migrations/2026_plantao_pro_white_label_b2b_launch.sql:1348-1362` (tabela genérica jsonb) vs master de fornecedores/pagadores/hospitais por flags Fornecedor/EhCliente/EhPagador/EhHospital (`Web/Views/Administrativo360/Parceiros.cshtml:236-254`). Sem tabela `fornecedores` nem `credenciadas`. Propósitos distintos, sobreposição semântica de "parceiro".
3. **consultas: especialidade text + especialidade_id FK** coexistindo (`2026_saude360_consultas_base.sql:15`; `Saude360ClinicalService.cs:711` grava `especialidade` text).

### C2. Serviços quase-duplicados
1. **5 calculadoras de dashboard/KPI no Api**: `BiService` (BiServices.cs:8), `DashboardPremiumService` (:7), `Fase6BiIntegracoesService.DashboardAsync` (:34-63), `DashboardService` (Data.cs:2333-2359), + endpoints `api/operacoes/dashboard`/`api/v114` — todas fazem `count(1) from plantaopro.X where reg_status='A' and tenant` com pequenas variações.
2. `OnboardingService` (cria tenant+JWT) vs `OnboardingJornadaService` (jornada por contrato) — **propósitos distintos**, nomes confundíveis.
3. `SaasServices.cs` (classe `ClienteService`) vs `SaasCoreServices.cs` — ambos CRUD SaaS raw SQL.
4. `MeuDiaService` é facade documentada sobre `ProductivityActionService` (`MeuDiaServices.cs:5-15`) — candidata a consolidação segura.
5. `B2BCommercialOpsService`/`CommercialDemoService`/`B2BLaunchService.ListarAsync(kind)` — 3 serviços de painéis comerciais com `ListarAsync("…-indicadores")` genérico sobre a mesma infra de launch.

### C3. DTOs duplicados Api vs Web (payload escrito 2×)
1. `ManagerCommandCenterDto`+`CoverageItem` (Api `ManagerCommandCenterService.cs:13-40`) ↔ `ManagerCommandCenterViewModel`+`ManagerCoverage` (`Web/Models/ManagerCommandCenterViewModel.cs:5`) — mirror serializado à mão (`Web/Services/ManagerCommandCenterWebService.cs:8`).
2. `Administrativo360ResumoDto` ↔ `Administrativo360ResumoViewModel`.
3. `ProductivityPageDto` ↔ `ProductivityPageViewModel`.
4. `DashboardPremiumDto` ↔ `DashboardPremiumApiViewModel`.
- OBS: espelhamento padrão do BFF; safe a consolidar só se os VMs não ganharem campos de view.

### C4. Rotas sobrepostas
- Sem conflito de verb+path idêntico dentro de um mesmo controller (verificado amostra).
- Candidatos de sobrecarga funcional: **6+ endpoints "dashboard" coexistem** (`api/dashboard` Data.cs, `api/dashboards/{admin-global|admin-cliente|coordenacao|medico|financeiro|saude360}` `DashboardsController.cs:16-21`, `api/bi/resumo-executivo`, `api/operacoes/dashboard`, `api/administrativo360/gestao/dashboard`, `api/v114/…`) com contagens sobrepostas.

### C5. Menus apontando para o mesmo alvo
- `Web/Views/Shared/_AppSidebar.cshtml`: **Usuarios.Index ×2** (linhas 75 e 89; labels "Usuários" vs "Usuários e acessos") e **Perfis.Index ×2** (linhas 76 e 90; "Perfis" vs "Perfis globais"). As seções "Administração do Cliente" (`canManage`) e "Administração MNSOFT" (`isGlobalAdmin`) podem renderizar juntas para super-admin → duplicação visível real.
- MinhaCentral.Index ×2 (linhas 22 logo + 29 item) — intencional (logo). Sem itens para alvo vazio (`#`) encontrados.

### C6. Regras financeiras duplicadas
1. **Comissão**: cálculo/apropriação aparece em (a) `ContasReceberRepository.cs:286` ("Apropriação proporcional de comissão" na baixa), (b) geração de título `GerarDeComissaoAsync` `ContasPagarRepository.cs:686-777`, (c) relatórios `Adm360FinanceiroRelatoriosRepository.cs:22-95` (somando `comissao_prevista`/`valor_comissao` de novo), (d) UI `TituloDetalhes.cshtml:42`. Verificar se (a) e (c) usam a mesma fórmula.
2. **Cálculo proporcional/temporal**: `Data.cs:1843` (`Math.Max(1m, horas)` p/ rate/hora) vs `backend/sql/20260521_premium_automacao_kpis.sql:25`.
- Proração mensal por dias de competência: **não localizada** (buscas `prorate|proporcional|/ 30\.0|DiasNoMes|daysInMonth`).

### C7. Markup visual repetido (sem partial compartilhado)
1. **Cards de KPI escritos à mão** em `Bi/Index.cshtml:5-16`, `MeuDia/Index.cshtml:110-115`, `CommandCenter/Index.cshtml:63-80`, `Operacoes/Index.cshtml:4` — embora existam partials `_KpiCard.cshtml` (usado só em `Clientes/Inteligencia.cshtml:13`) e `_CoverageCard.cshtml`.
2. **Placeholder "Indicador/Valor — Sem dados no período selecionado" idêntico** em 5 views: `Views/Relatorios/{Sla,ProdutividadeMedica,FaturamentoSaas,Convites,Cobertura}.cshtml:5-6`.
3. Views-BI-fantasma de 1 linha: `Views/Bi/{Financeiro,Medicos,Clientes,Sla}.cshtml` (só alert "em evolução") servidas por ações distintas de `Web/Controllers/BiController.cs:12-18` (8 ações → 4 views, várias reusando `View("Index")`).

### Top 10 candidatos de maior impacto (C)
1. 5 calculadoras de dashboard/KPI (C2.1 + C4) — maior divergência real já encontrada ("Plantões do mês" sem filtro de mês, `Fase6BiIntegracoesServices.cs:53,56`).
2. CID: 6 criações divergentes da mesma tabela (C1.1) — risco de drift de schema.
3. Mirror de DTOs Api↔Web ×4 (C3) — manutenção dupla de payload.
4. Placeholder de relatórios repetido em 5+5 views (C7.2/C7.3) + BiController 8 ações/4 views.
5. Menu _AppSidebar com Usuarios/Perfis duplicados (C5, :75/:76 vs :89/:90).
6. Comissão em 4 locais (C6.1) — validar canonicidade da fórmula.
7. `parceiros` B2B vs `adm360_parceiros` (C1.2).
8. consultas.especialidade text + especialidade_id (C1.3).
9. MeuDiaService facade sobre ProductivityActionService (C2.4).
10. Cards KPI manuais vs `_KpiCard` (C7.1).
