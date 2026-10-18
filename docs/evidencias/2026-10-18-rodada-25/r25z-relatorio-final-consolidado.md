# R25 · r25z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R24 (`39c9129`, suíte 1533).

## Escopo da rodada

Primeira execução da diretriz de UX/texto/acesso (r24a1): inventário-mestre classificado por epic
(r25a0) + quick-win do epic **E8** ("tire todos os textos que sugerem homologação").

## Commits

| Commit | Conteúdo |
|---|---|
| `9036b15` | docs r25a0: inventário-mestre da diretriz (E8 item a item com decisão; whitelist de domínio; base estatística E3/E4/E5/E6/E2) |
| `d0e1344` | r25b1: 15 pontos E8 reescritos (4 culturas resx inclusas) + fato permanente anti-regressão `R25TextosSemHomologacaoContratoTests` (5 fatos) |
| `b55b60c` | r25b1-bis: contratos `R4C11BreadcrumbTrailTests` (breadcrumb "Compatibilidade") e `PosRcUxFinalQaContractTests` (empty state "Somente indicadores reais são exibidos.") alinhados à reescrita |

## E8 aplicado (destaque)

- Banner do layout de autenticação (`Auth.DemoNotice`) reescrito nas 4 culturas: "Ambiente de exemplo: os dados
  exibidos aqui não são dados reais da sua instituição."
- Rotas V112Web: "Demo funcional"/"Homologação visual" → "Compatibilidade 1.12"/"Checagens 1.12"; badge "Demo explícito
  quando indicado" → "Dados de exemplo aparecem sempre marcados"; breadcrumb `V112Web` → "Compatibilidade".
- Placeholders que vazavam o hospital demo ("nfe-santacasa-homologacao", "Hospital demonstração", "Tenant demonstração",
  "Ex: LOTE-DEMO-01") → genéricos/neutros ("CNPJ do emitente", "Sua instituição", "Hospital ou clínica", "Ex: LOTE-0001").
- Estados vazios e serviços sem jargão ("massa demo em ambiente de desenvolvimento", "pacientes demonstrativos",
  "número demonstrativo", "histórico demonstrativo"); bônus E2: 2 textos com "tenant" → "sua instituição/este cliente".
- Whitelist de domínio preservada e travada em fato (NF-e SEFAZ HOMOLOGACAO/PRODUCAO, monitor fiscal "Não Homologada",
  Operação Assistida, marketing comercial, termos contábeis "demonstrativo financeiro", css/config).

## Gates e suíte

- Cada fatia: rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Suíte: duas corridas completas sob carga paralela, cada uma derrubando UM e2e diferente da família documentada de
  deadlock `40P01`/race de contagem (`Db_CentralClientes_SqlCompletaDaFonte_MaterializaItensETotais` na corrida 1 —
  isolate 5× verde; `SaasGovernancaB6Rodada4Tests.G1a_PrefisGlobais_…` na corrida 2 — isolate 5× verde). Contagem final
  estável: **Total 1538** = base 1533 + 5 novos fatos E8. Nenhum dos dois toca superfície desta rodada.
- Observação de casa para a fila de design: o flake da corrida 1 (total < página lida entre os dois selects do
  `QueryMultipleAsync`) indica que `ListCentralAsync` lê itens e totais fora de transação; candidata a snapshot
  `repeatable read`/CTE única quando houver janela no epic de SaaS (E9).

## Recon R26 já concluído (epic E4 — contraste em áreas escuras)

Scanner de adjacência DOM (pilha de tags, ancestor escuro `bg-black|bg-dark|table-black|pp-hero__bg|operational-card`)
executado sobre TODAS as views — script guardado em `%TEMP%\opencode\r26_scan_contraste.ps1`. Resultado: apenas **19
pontos reais** (a contagem bruta de ~200 `text-muted` era majoritariamente sobre superfícies claras):

- `Adm360\Cotacoes\Detalhes.cshtml` L288, 401, 408, 432, 513, 531, 532, 550, 555 (9)
- `Adm360\DocumentosXml\Detalhes.cshtml` L187, 198, 201 (3)
- `Adm360\ValorizacaoPrevia.cshtml` L185, 186 (2)
- `Adm360\Gestao\Dashboard.cshtml` L256, `ContasFinanceiras.cshtml` L190, `ComissoesPendentes.cshtml` L79,
  `OrcamentoNovo.cshtml` L68, `FluxoCaixa.cshtml` L173 (1 cada)

Padrão visto no spot-check: `form-text text-muted` sobre cards escuros (ajudas de formulário — o caso mais lesivo, pois
é texto instrucional), `list-group-item bg-dark text-muted` (estado vazio), labels/valores de resumo escuros.
Correção proposta no r26a0: `text-muted` → `text-white-50` dentro dos containers escuros (Bootstrap nativo, contraste
real sobre fundo preto) + fato-scanner permanente (escopo: as 8 views acima) travando a regressão.

## Estado de casa

- Apps PARADOS, portas livres. `origin/main` sincronizada neste push.
- Fila: **R26 = E4 contraste (19 pontos + fato-scanner)**; R27+ conforme roadmap r24a1.
