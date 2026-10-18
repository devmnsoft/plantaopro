# R24 · r24z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R23 (`beb21d1`, suíte 1529).

## Escopo da rodada

Fechamento dos dois últimos P3 pendentes do R14 (**F-R14-D** e **F-R14-B**) + registro formal da **diretriz de UX/texto/acesso**
com roadmap de 30 sprints (R24→R53). Sem mudanças em superfície de DTO/view (não houve necessidade de extensão ADR-0003).

## Commits

| Commit | Conteúdo |
|---|---|
| `96ff4a0` | docs r24a0/r24a1: auditoria F-R14-B/D com decisões D-R24-1..3 + registro da diretriz (epics E0–E9, roadmap 30 rodadas) |
| `bdf87ec` | r24b1: `RelatoriosSaasController` — total real de paginação (count subquery compartilhada, listagem+export) + fato do `[Authorize]` do backoffice global |
| `b7b98c3` | r24b2: `BaseWebController.BuildApiErrorMessage` — ramo 500 unificado em repasse com fallback; contratos R16/R14 atualizados |

## Decisões versionadas (r24a0)

- **D-R24-1 (F-R14-D)**: 500 da chamada simples agora repassa a mensagem da API com fallback ao literal fixo — direção do
  precedente aprovado ao vivo na r14b2 (parser paginado), que quebra apenas uma asserção em vez de duas. Novos fatos:
  repasse com mensagem + fallback honesto (corpo sem nenhuma chave reconhecida de mensagem).
- **D-R24-2 (F-R14-B parte 1)**: `ExecutarPaginadoAsync` com `select count(*)::bigint from (sql) as __contagem` nos dois
  caminhos (listagem clamp 1..100, export clamp 1..500); query de itens e sufixo `limit @s offset @offset` idênticos — o
  contrato R14 de SQL exata permanece verde sem alteração.
- **D-R24-3 (F-R14-B parte 2)**: relatórios SaaS mantêm visibilidade GLOBAL por design (backoffice da operadora; papéis de
  plataforma/operacional), com `[Authorize]` travado em fato. O escopo fino de "tenant visível só para super admin" é epic E2
  (R38/R39) da diretriz — decidido não fazer meio-termo nesta rodada.

## Evidência

- **Fatos novos (4)**: `R24RelatoriosSaaSContratoTests` — (1) fonte: count real presente / `LongCount` ausente / sufixo
  preservado; (2) fonte: `[Authorize]` do backoffice global travado (AdminGlobal+Financeiro; sem papel genérico de tenant);
  (3) banco: seed determinístico 125 clientes × 1 fatura na competência exclusiva 2099-01-15 → total real 125 via subquery
  (inalcançável pelo wrapper antigo) e páginas 50/25 corretas.
- **Fatos atualizados (3)**: R16 `GET_500_…` dividido em 2 (repasse + fallback); R14 `IndexDeFaturamento_ResumoEmFalha_…`
  passa a esperar a mensagem atribuída e negar o literal.
- **Contraintuitivo encontrado no seed** (documentado para R25+): uniques parciais `ux_clientes_cnpj_ativo` e
  `ux_faturas_saas_cliente_competencia_ativa` — 1 fatura ativa por cliente/competência; CNPJ único entre clientes ativos.

## Gates e suíte

- Cada fatia (r24b1, r24b2): rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Suíte final: **Total 1533** (= base 1529 + 4 novos fatos, sem perdas). Na corrida completa sob carga paralela caíram 2 e2e
  de banco da família conhecida de deadlock `40P01`
  (`Administrativo360OcorrenciasA12Tests.R11_Abrir_BloqueioImediato_…`, `ProvisionamentoB5Tests.P5_ProvisionarManual_Trial_…`)
  — nenhum toca a superfície desta rodada. Protocolo aplicado: **isolate 5× verde para cada um** (10/10 corridas isoladas).
- Classes-alvo verificadas pontualmente: R14+R16+R24 = 16/16.

## Recon já iniciado para a R25 (inventário-mestre da diretriz)

- **E8 (vestígios de homologação)**: scan inicial encontrou 60+ ocorrências de homolog/demonstra/demo/smoke/seed em Web.
  Classificação pendente em r25a0: o termo **"Homologação" do ambiente fiscal NF-e (HOMOLOGACAO/PRODUCAO) é conceito de domínio
  SEFAZ e NÃO cai no epic** (manter); sim: "Ambiente de demonstração ativo" (UiTexts.resx), "demo-boleto", rotas
  "Homologação v1.12"/"Demo funcional", "massa demo", placeholders "Hospital/Tenant demonstração", etc.
- **E4 (contraste)**: estatística por view coletada — maior concentração de `text-muted` em contexto escuro:
  `Adm360\Gestao\Dashboard` (35 muted/27 dark), `Adm360\Cotacoes\Detalhes` (27/27), `Adm360\RelatoriosFinanceiros` (19/23),
  `Adm360\FluxoCaixa` (15/26), `Adm360\ValorizacaoPrevia` (11/23), `Adm360\TituloDetalhes` (10/17). Varredura fina
  (adjacência elemento-a-elemento) + fix + fato-scanner ficam no r26.

## Estado de casa

- Apps PARADOS, portas 51976/52976 livres (estado de casa pós-smoke R23).
- `origin/main` sincronizada ao push desta linha (ver commit r24z).
- Fila: **R25 = r25a0 inventário-mestre classificado por epic (file:line) + quick-win E8**.
