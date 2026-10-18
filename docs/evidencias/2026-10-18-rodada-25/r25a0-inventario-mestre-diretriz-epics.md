# R25 · r25a0 — Inventário-mestre da diretriz UX/texto/acesso (classificação por epic)

Data: 2026-10-18 · Sessão: `opencode` · Base: R24 encerrada (`39c9129`, suíte 1533 fatos).
Documento-vivo do epic da diretriz (r24a1): classifica os achados por file:line com decisão por item. Rodadas
posteriores (R26+) detalham os epics pendentes com a mesma disciplina.

## E8 — Vestígios de homologação/demo (FOCO desta rodada; quick-win em r25b1)

### Classificar REMOVER/REESCREVER na r25b1 (texto que faz o usuário achar o produto sob teste)

| # | Local | Texto atual | Ação |
|---|---|---|---|
| 1 | `Localization\UiTexts.resx` `Auth.DemoNotice` (+ `UiTexts.en.resx`) | "Ambiente de demonstração ativo. Credenciais não são exibidas por segurança." | Reescrever em linguagem simples ("exemplo", sem jargão) — valor livre no contrato R7DF6 (chaves travadas, valores não) |
| 2 | `Controllers\V112WebController.cs` L9-10, L17 | "Demo funcional" / "Timeline executável da homologação v1.12." / "Homologação visual" / "Validar homologação" / "Ver status da demo" / "boleto demonstrativo com outbox persistida" | Renomear títulos/descrições p/ linguagem de compatibilidade neutra (rotas URL mantidas) |
| 3 | `Views\V112\OperationalPage.cshtml` L2, L15 | title default "Homologação v1.12"; badge "Demo explícito quando indicado" | Idem |
| 4 | `Services\BreadcrumbService.cs` L31 | `["V112Web"] = { "Homologação" }` (breadcrumb visível do usuário) | → "Compatibilidade" |
| 5 | `Services\AssistenteContextualService.cs` L30 | "Use a ação principal ou aplique a massa demo em ambiente de desenvolvimento." | → instrução simples sem "massa demo" |
| 6 | `Controllers\OperacaoInteligenteController.cs` L32 | "exibindo modo demonstração explicitamente habilitado." | → "modo de exemplo habilitado" |
| 7 | `Views\Administrativo360\Coleta.cshtml` L99 | placeholder "Ex: LOTE-DEMO-01" | → "Ex: LOTE-0001" |
| 8 | `Controllers\FaturamentoClinicoController.cs` L39, L48 | "boleto permanece somente demonstrativo." / "Demo Boleto" / "Demonstração sem emissão de cobrança real." | → "O boleto exibido é ilustrativo e não representa cobrança real." (só textos visíveis; nome da ação mantido) |
| 9 | `Controllers\V115FaturamentoWebControllers.cs` L15 | "demo-boleto explícito" | → "boleto de exemplo" |
| 10 | `Views\WhiteLabel\Index.cshtml`+`Preview.cshtml` L22, L35, L36 | placeholder "Hospital demonstração"; fallback "Tenant demonstração" | → input "Hospital ou clínica"; fallback "Sua instituição" (também retira jargão "tenant") |
| 11 | `Views\Agendamentos\AgendaPremium.cshtml` L38 | "A tela não exibe pacientes demonstrativos por padrão." | → "A agenda exibe apenas pacientes reais conforme os filtros atuais." |
| 12 | `Views\DashboardsPremium\Perfil.cshtml` L18 | "Nenhum número demonstrativo foi exibido." | → "Somente indicadores reais são exibidos." |
| 13 | `Views\FaturamentoClinico\Index.cshtml` L50 | "A API não retornou contas para este tenant. Nenhum valor ou histórico demonstrativo foi criado." | → "Nenhum valor ou histórico foi criado para este cliente." (bônus E2: retira "tenant") |
| 14 | `Services\Fase2OperationalFlowService.cs` L41, L58 | "Nenhum indicador ou item demonstrativo é exibido." / "Nenhum dado demonstrativo é usado em páginas operacionais." | → "Nenhum indicador sem fonte operacional é exibido." / "Páginas operacionais exibem apenas dados reais." |
| 15 | `Views\Administrativo360\Fiscal\Configurar.cshtml` L132 | placeholder "nfe-santacasa-homologacao" (vaza o nome do hospital demo) | → "CNPJ do emitente" |

### Whitelist de DOMÍNIO (manter, documentado — NÃO cai no epic)

- **Ambiente fiscal NF-e SEFAZ** (`HOMOLOGACAO`/`PRODUCAO`): `Fiscal\Configurar.cshtml` L125, `Cotacoes\Estabelecimentos.cshtml` L107,
  `Cotacoes\ContasPortal.cshtml` L150 e o default `Administrativo360CotacoesXmlModels.Ambiente = "HOMOLOGACAO"` — conceito de
  domínio da SEFAZ, não sugestão de ambiente de teste do produto. `Adm360FiscalWebController` L455/L469 idem.
- **Monitor fiscal** `Gestao\Monitor.cshtml` L67 "Bloqueada / Não Homologada" — situação fiscal do acesso.
- **Operação Assistida** (`OperacaoAssistida\*`): módulo de implantação com fase de homologação por cliente — conteúdo de
  domínio do serviço SaaS (revisitado na passada de terminologia E1/R28 junto com o redesign do módulo).
- **Marketing comercial** (`CommercialDemoWeb`, `B2BLaunch`): "Agendar demonstração", "roteiro de demo" — o produto vende demo
  comercial a prospectos; a palavra é correta no contexto.
- **Arquivos/nomes técnicos** (não-texto de UI): css `v2151-wp-c-homologacao.css`, `demo-access.css`; chaves de config
  `Demo:Enabled`/`DemoSeed:Enabled` (flags de desenvolvimento); nome de método `OperacaoInteligenteViewModel.Demo()` (código).
- **"Demonstrativo financeiro"** (`MeusPagamentos`, `Fechamentos`): termo contábil (extrato), não "demo".

### Adiado com destino definido

- Jargão "tenant" visível ao usuário (motivos de login `Login.ReasonTenant*`, textos de Ajuda/*, "Tenants Ativos" em CommandCenter)
  → **E1 (R27)** para textos de ajuda/login e **E2 (R39)** para validação de visibilidade global.
- `appsettings.json` "Demo"/"DemoSeed" e `_AuthLayout` L32: mecânica mantida (banner só aparece se a flag estiver ligada);
  o TEXTO do banner é o item 1 acima.

## Base estatística dos demais epics (para as rodadas dedicadas)

- **E4 (contraste)**: maior concentração `text-muted` × contexto escuro (bg-black/bg-dark) — `Adm360\Gestao\Dashboard` 35/27,
  `Adm360\Cotacoes\Detalhes` 27/27, `Adm360\RelatoriosFinanceiros` 19/23, `Adm360\FluxoCaixa` 15/26, `Adm360\ValorizacaoPrevia`
  11/23, `Adm360\TituloDetalhes` 10/17, `Adm360\DocumentosXml\Detalhes` 12/20, `Adm360\TitulosPagar` 7/12,
  `Adm360\ContasFinanceiras` 7/19, `Adm360\OrcamentoDetalhes` 7/11. Varredura fina elemento-a-elemento + fix + fato-scanner → R26.
- **E3 (instruções por página)**: cobertura atual fraca — apenas 6 views usam `usage-help` e 65 usam `pp-screen-guide` contra
  dezenas de telas operacionais sem nenhum bloco "como usar". Componentes já existem (padrão estável) → R35-R37 por área.
- **E5 (formulários)**: 3169 ocorrências de `<form>`, 308 `required`, 32 `type="checkbox"` e ZERO `asp-for CheckBox` — checkbox
  list não usa componente padronizado em lugar nenhum (auditoria padrão/validação → R30-R31).
- **E6 (listas nome/id + números progressivos)**: scan de `<option ...>@X.Id</option>` (label = id cru) não encontrou forma
  simples; 1 exibição de GUID truncado em UI: `MinhaAgenda\MeusPagamentos.cshtml` L9
  (`FechamentoId.Value.ToString("N")[..8]`) — candidato a número sequencial visível (R33). Detalhamento completo → R32.
- **E2 (acesso/tenant)**: pontos fortes já existentes: `AdminSaas\Index` já declara "Somente o Super Administrador visualiza
  todos os tenants"; `Convite\Aceitar` mostra o próprio TenantNome (correto — é o tenant do convidado). Pontos a auditar em R38/R39:
  `AssistenteIa\Index` L223 exibe `TenantId` truncado (técnico visível), `DashboardsPremium\Perfil` comenta que
  "listagem api/medicos é global, sem escopo de tenant" (código), relatórios SaaS (D-R24-3, já documentado).
- **E1 (linguagem simples)**: valores de resx livres (R7DF6 trava só chaves = 653 Adm360 nos 4 resx) — reescritas sem bump de
  contador. Zonas pré-conversão (Suprimentos/Administrativo360 em literal pt-BR) seguem estilo local.
- **Sanidade do contrato da casa**: `href="#"` = 0 em Views (continua ok).

## Plano de fatias R25

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r25b1 | Quick-win E8: os 15 itens da tabela (valores resx + literais pt-BR + breadcrumbs) | scanner-fato permanente: os literais removidos não reaparecem em Views/Localization/Services; whitelist de domínio documentada no fato |
| r25z | Consolidado + suíte + push | base 1533 crescendo |

Gate como sempre: rebuild real → `r20b1_gate.ps1` (160 vs pristino); flake ⇒ isolate 5×.
