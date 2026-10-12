# R8-A0 — Varredura de evidência: §19–§21 (aprovações, preços/vigência, cancelamento)

Data: 2026-10-11 · Método: exploração read-only do HEAD `2f4533d3`. Caminhos relativos a `C:\MNSOFT\plantaopro\`.
Classificações: pronto com evidência / parcial / quebrado / ausente após busca / não verificado.

## Área A — Aprovações e separação de responsabilidades (§19)

### A1. Mecanismo genérico de aprovação — **parcial**
- **Não existe mecanismo genérico.** Busca `workflow_aprovacao|aprova_solicitacao|pending_aprovacoes|aprovacoes_pendentes` em `database/` → zero.
- Aprovações por domínio (padrão repetido: `aprovado_por/aprovado_em` + transição de estado + idempotency key):
  - Compras: `Adm360SuprimentosControllers.cs:16` (`POST .../compras/{id}/aprovar`), `ComprasRepository.cs:93-108` (só `RASCUNHO→APROVADO`).
  - Ajuste de estoque (inventário): `Adm360SuprimentosControllers.cs:59`, `InventarioRepository.cs:117-162` (movimentos `AJUSTE` na aprovação).
  - Estornos: `CotacoesRepository.cs:1376+` (`EstornarRespostaAsync`, justificativa obrigatória :1379); estorno de pagamento `Api/Data.cs:1989-2007`; migration `2026_10_v2314_adm360_b7_cotacao_cancelamento_estorno_triagem.sql`.
  - Fechamento: `FechamentoOperacionalService.cs:125` (`AprovarAsync`) + máquina `Domain/Financeiro/FechamentoWorkflow.cs:8-20`.
  - Contratuais/SaaS: `SolicitacoesPlanosController.cs:28-45` + `SolicitacoesPlanosService.cs:196-248`; `ModuleContractingController.cs:43`.
  - Orçamento cirúrgico `Adm360OrcamentosController.cs:69-78`; cotação-resposta `CotacoesRepository.cs:772+`; título a pagar `ContasPagarRepository.cs:271+`; substituição de plantão `Fase4OperationalServices.cs:378,411`; autorizações convênio `V116ConsolidacaoServices.cs:177`.
  - Tabelas pontuais: `go_live_aprovacoes` (v1188), `release_aprovacoes`/`manutencao_aprovacoes` (v1189).
- OBS: padronização existe (estado + idempotência + eventos), sem serviço/tabela única nem reuso cruzado; "descontos" só como regra declarativa não aplicada (A2).

### A2. Autoaprovação — **parcial**
- Financeira **existe por construção**: fechamento aprovado gera pagamentos já `'aprovado'` com `processado_automaticamente=true` — `FechamentoOperacionalService.cs:171`.
- Regra configurável declarada mas **não consumada**: `comercial_regras_desconto(exige_aprovacao)` (seed `2026_plantao_pro_saas_lgpd_jornada_inteligente.sql:108,202-203`); busca `exige_aprovacao` no backend → apenas contract test (`SaasComercialLgpdJornadaAjudaContractTests.cs:41`).
- Bloqueada em 1 fluxo: correções de presença (`ExecutionConferenceService.cs`, testado em `V2167ExecutionConferenceContractTests.cs:6`).
- OBS: sem switch global; sem guarda genérica criador≠aprovador para compras/inventário/fechamento.

### A3. Alteração após aprovação invalida? — **parcial**
- Plantões: alteração pós-confirmações bloqueada (`Data.cs:1154` — use cancelamento/substituição com histórico).
- Cotações: resposta aceita exige estorno antes de nova aprovação (`CotacoesRepository.cs:834-836`; teste B7_T16).
- Pagamentos: pago exige estorno antes de ajuste/cancelamento (`Data.cs:1979,1999`); contestação preserva `situacao_anterior`.
- Compras: itens sem endpoint de edição posterior → conteúdo congelado de fato.
- OBS: sem mecanismo uniforme de "revalidar hash do aprovado"; proteção por estado terminal/freeze por domínio. Compra não tem edição de itens nem cancelamento (C3).

### A4. Versão analisada registrada (hash/snapshot)? — **parcial**
- Snapshot imutável: `adm360_cotacao_respostas.snapshot_proposta jsonb NOT NULL` (v2196:164), preenchido com itens+revisão (`CotacoesRepository.cs:839-855`).
- Revisões de orçamento com snapshot (`v2192:81-91`); `consultas.triagem_snapshot` por trigger (`schema/380_v2160_...sql:111-112`); evento de aprovação com SHA-256 (`Adm360EventService.cs:12` + imutabilidade testada em `Administrativo360EventosImutabilidadeTests.cs:131-143,239-287`); `v113_faturas.valor_base_snapshot`.
- OBS: compras/inventário gravam `aprovado_por/em` + bump de `versao`, sem hash do conteúdo.

### A5. Delegação (período/escopo) — **ausente após busca**
- Buscas `delegac|delegat` → só delegates C# e o termo em lista de roles (`GenericClientOperationalContractTests.cs:150-153`). Separação ocorre via policies/roles (`Adm360.InventarioAprovar`, `Adm360.AprovarDespesa`), não por delegação temporal.

### A6. Negativa exige justificativa? — **pronto com evidência**
- `ModuleContractingService.cs:97`; `SolicitacoesPlanosService.cs:131`; fechamento (motivo catalogado + observação 10-500, `FechamentoOperacionalService.cs:139-141`); estorno de cotação `CotacoesRepository.cs:1379`; estorno/contestação/cancelamento de pagamento `Data.cs:1938,1969,1992,2012-2013`; constraint de banco p/ inspeção reprovada (v2191:13); Web força motivo (`EscalasController.cs:27-28`, `Assinaturas/Details.cshtml:18`, `FaturamentoSaasController.cs:162`).
- Ponto fraco residual: substituição de plantão aceita justificativa opcional (`DecisaoSubstituicaoRequest`).

### A7. Repetir aprovação não repete efeitos? — **pronto com evidência (por fluxo)**
- Cotações: `B7_T10_AprovarDuasVezes_RetornaAMesmaRespostaESemDuplicar` (:764-783) + backstop `ux_adm360_resposta_cotacao_nao_resolvida` (v2314:10-18).
- Inventário: mesma chave → no-op; chave diferente → erro ("já aprovado com outra chave") — `InventarioRepository.cs:133-144`.
- Compras: guard de chave + `unique(tenant_id,idempotency_key)` — `ComprasRepository.cs:98-108`.
- SaaS upgrade: `B2_AprovarUpgrade_TrocaPlanoComHistorico_E_Idempotente` (`SaasComercialB4MatrizTests.cs:106`).
- OBS: idempotência por `Idempotency-Key` + estado terminal; sem registro central genérico de efeitos.

## Área B — Preços, contratos e vigência (§20)

### B1. Tabelas de preço com vigência — **parcial**
- Convênios: `convenio_contratos(vigencia_inicio,fim)` + `ck_contrato_vigencia` (`2026_saude360_convenios_planos_saude.sql:34`); **mas** `convenio_tabelas_procedimentos(valor)` **sem vigência** (`schema/440_...sql:81-87`); `convenio_tabelas` só inicio sem fim; autorizações `v116_convenio_autorizacoes` sem vigência.
- Contratos operacionais/unitários: `contratos_operacionais(vigencia_inicio,fim,valor_base)` + `regras_preco_contrato(...vigencia_inicio,fim)` (`schema/150_portal_unidade_contratos_v2100.sql:12-27`).
- Plantões: coluna única `valor` atual (`Data.cs:1159,1336`) — **sem vigência**.
- Produtos/consumíveis: `preco_unitario` — sem vigência. Repasses: `regras_repasse_medico` tem vigência (`schema/440_...sql:104-110`); `v115_regras_repasse` não. AI pricing versionado (`v2308:93-106`). Cotações: oferta congelada por cotação. SaaS módulos: `tenant_modulos(ativado_em/desativado_em)` + `ModuleContractVigencia.cs:24`.
- OBS: cobertura desigual — convênio-tabela de procedimentos e preços de produto/plantão sem vigência formal.

### B2. Histórico de versões das regras — **parcial**
- `adm360_orcamento_revisoes(revisao,motivo,snapshot_json)` (v2192:81-91); `ai_precos_modelos(versao_de,versao_ate)` (v2308:93-106); `v2327_b4_historico_reg_status` (audita ativações, não valores).
- Sem tabela-histórico de preços para `regras_preco_contrato`, `convenio_tabelas_procedimentos` ou `v115_regras_faturamento` (mudam in-place via `reg_status`).

### B3. Critério explícito de escolha da regra — **parcial**
- Contrato operacional: `current_date between vigencia_inicio/fim and status='ativo'` — `UnitPortalServices.cs:33` (não usa `regras_preco_contrato`).
- Módulos: predicados canônicos `modulos_efetivos`/`modulo_efetivo` (testes `R6GranularidadeSaude360Tests.cs:66-107`, `V2197ModuleContractReconciliationTests.cs:18-31`).
- Fechamento: regra fixa nomeada `VALOR_BASE_12H` (`FechamentoContracts.cs:33-34`).
- **`regras_preco_contrato` (especialidade/turno/feriado/período) criada e indexada mas nenhum cálculo a consulta** — sem engine de escolha.

### B4. Valor acordado preservado na operação — **pronto (financeiro) / parcial (restante)**
- Pagamentos plantão: `pagamentos.valor_previsto/valor_apurado/valor_aprovado/valor_hora/horas_referencia/parametros_apuracao jsonb` + `historico_pagamento` (`Data.cs:1882-1893`; geração `FechamentoOperacionalService.cs:171`).
- Faturas clínicas: `valor_base_snapshot` + regra/descontos/acréscimos/glosa (v195:4-11). Compras: itens congelados na criação; cotação: `preco_unitario_ofertado` + snapshot; consultas: `triagem_snapshot`.
- OBS: escalas/convites referenciam `plantoes.valor` vivo; o valor cobrado é snapshotado em `pagamentos`.

### B5. Ambiguidade entre regras equivalentes sobrepostas — **parcial**
- Contratos trabalhistas ADM360: check de sobreposição no serviço (daterange && daterange, `Administrativo360Service.cs:224-225`, testado :511-564); adjacência permitida.
- `ck_contrato_vigencia` em vários; **sem exclusion constraint** no banco para `regras_preco_contrato` nem `convenio_contratos`.

### B6. Componentes de cálculo exibidos (breakdown)? — **pronto (fechamento/comissões)**
- UI "Memória de cálculo": regra, base, horas, vigência, arredondamento — `Web/Views/OperacaoPremium/Fechamentos.cshtml:27` (+ ViewModel/FechamentoContracts).
- Persistidos: `parametros_apuracao jsonb` (:171); faturas com descontos/acréscimos/glosa; comissões com `base_calculo` em relatórios (`Adm360FinanceiroRelatoriosRepository.cs:58`, CSV :86).
- OBS: cálculos fora do ciclo fechamento/financeiro mostram subtotais sem memória estruturada.

## Área C — Cancelamento com análise de efeitos (§21)

### C1. Consequências apresentadas antes do cancelamento — **parcial**
- **Server-side bloqueia, não apresenta**: cancelar plantão conta escalas confirmadas/realizadas c/ pagamento vinculado → 409; cascata controlada (escalas solicitadas + convites cancelados na mesma transação, `Data.cs:1235-1243`); motivo ≥10 chars (:1180-1184).
- Venda ADM360: "Estorne os recebimentos antes de cancelar." (`VendaRepository.cs:376`). Pagamento pago exige estorno prévio.
- UI: confirmações genéricas c/ justificativa (`Plantoes/Details.cshtml:196`, `Financeiro/Details.cshtml:98`); exceção com painel de consequências: `CentralEscala/SubstituicaoDetails.cshtml:9`.
- Sem endpoint tipo "retornar vínculos antes de confirmar".

### C2. Delete físico vs inativação — **pronto com evidência (soft-delete)**
- `delete from` em `Data.cs` → zero; em todo backend só em fixtures de teste; contrato proíbe hard delete de escalas (`Fase4OperationalAutomationContractTests.cs:84`).
- Inativação `reg_status='I'` padronizada (médicos/hospitais/especialidades/departamentos/cargos/usuários/perfis) + histórico em `v2327_b4_historico_reg_status.sql`.
- Sem delete físico de cadastro com histórico protegido no runtime.

### C3. Operações compensatórias ao cancelar — **parcial**
- Estornos: pagamento (pago→aprovado, zera valor_pago, histórico — `Data.cs:1989-2007`), contas a pagar (trava por caixa ativo — `ContasPagarRepository.cs:458-508`), baixa contas a receber (`ContasReceberRepository.cs:339`), cotação (devolve fila preservando eventos — `CotacoesRepository.cs:1371-1435`).
- Cascata controlada no plantão (`Data.cs:1241-1242`).
- **Compra recebida NÃO tem compensação automática** de estoque/títulos nem endpoint de cancelamento de pedido (`ComprasRepository.cs` só Listar/Criar/Aprovar/Receber). Inspeção reprovada roteia destino (v2191:13). Venda exige estorno manual primeiro.
- **R8-B4 (corrigido — lado compra):** cancelamento de pedido de compra com compensação — `ComprasRepository.CancelarAsync` + endpoint `POST api/administrativo360/compras/{id}/cancelar`: reverte estoque por grupo (produto, lote, local, condição, via `SAIDA` espelhando a condição de origem), zera os títulos do recebimento (`CANCELADO`, saldo 0), evento `CANCELAMENTO_RECEBIMENTO` no documento fiscal vinculado; 409 fundamentado quando há consumo/transferência/inspeção no grupo, reserva ativa (linha LIBERADO) ou título pago (exige estorno prévio); idempotente por estado; trilha de auditoria v2347 (`motivo_cancelamento`/`cancelado_em`/`cancelado_por`). Detalhe em `r8b4-cancelamento-compra-recebida.md`.

### C4. Revalidação server-side + falha parcial consistente — **pronto com evidência (transacional)**
- `FOR UPDATE` + guarda de estado: aprovação pedido (`ComprasRepository.cs:99`), recebimento (:142-177), plantão (`Data.cs:1222,1237-1240`), inventário (`InventarioRepository.cs:149-151`); retries SERIALIZABLE (40001/40P01) padronizados.
- Teste: `AprovacaoDeModulo_RevalidaDependenciasAtivasNaTransacao` (`M5SaasBoundaryContractTests.cs:38`); elegibilidade reavaliada no aceite (`Data.cs:1488-1503`).
- OBS: consistência entre módulos depende de ações complementares manuais (estorno antes de cancelar).

### C5. Exceção de admin com justificativa + auditoria — **parcial**
- Justificativa obrigatória nos cancels (plantão ≥10 chars, escala, pagamento, fatura SaaS, venda/inventário/vale/cirurgia/note, assinatura).
- Auditoria em todos os status changes (`audit.LogAsync` `Data.cs:1255,1958,1986,2006`), `historico_escala`, `historico_pagamento`, eventos ADM360 imutáveis c/ hash, `auditoria_financeira_clinica`.
- **Ausente** mecanismo explícito de "exceção de admin" (override/force com trilha própria); exceção prática = estorno/ajuste manual com justificativa + contestação.
