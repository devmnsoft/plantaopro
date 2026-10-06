# Auditoria Inicial — Rodada 3 (2026-10-06)

**Objetivo**: fechar o item 1 do novo escopo (auditoria inicial) — confirmar estado do repositório, ler as instruções, atualizar referências antigas e **classificar cada jornada** (implementada / validada / parcial / quebrada / não executada), separando relatório de comprovação.

## 1. Estado do repositório

| Item | Estado |
|---|---|
| HEAD (`main`) | `7f43af2` |
| `origin/main` | `7f43af2` (ahead=0) — **toda a rodada 2 publicada em 2026-10-06** |
| Baseline da rodada | `82b8c26` |
| Alterações locais preservadas (não versionar) | 4 `appsettings*.json` com connection string local (`plantaopro_test`) + pastas untracked `Properties\PublishProfiles\` (Api e Web) |
| Nota de hash | `6cd815b` (citado em versões anteriores de `entrega-final`) é o commit original de entrega final, depois amendado para `7f43af2` (confirmado no reflog) |
| Suíte canônica | 839 testes verdes na rodada anterior; flakes conhecidos `Aceite12_XmlRepetido_NaoDuplica` e gate de recebimento `A3/G5` sob paralelismo (agora exige correção pela causa) |

## 2. Referências antigas atualizadas nesta auditoria

| Documento | Alteração |
|---|---|
| `docs/homologacao/entrega-final-2026-10-05.md` | Header corrigido (HEAD `7f43af2`, publicação completa, nota sobre `6cd815b`); §6 item 10 fechado (push realizado) |
| `docs/homologacao/vereditos-homologacao-2026-10-03.md` | Nota de atualização de publicação no cabeçalho |
| `docs/homologacao/matriz-modulos-homologacao-2026-10-03.md` | Nota de atualização de publicação no cabeçalho |
| `docs/evidencias/2026-10-05-wp-b-jornadas/jornadas.md` | Linha de baseline anotada com publicação atual |
| `docs/evidencias/2026-10-05-wp-c-design/matriz-wp-c.md` | Linha de baseline anotada com publicação atual |

## 3. Mapas primários desta auditoria (exploração somente leitura)

- `docs/evidencias/2026-10-06-rodada-3/mapas/mapa-adm360.md`
- `docs/evidencias/2026-10-06-rodada-3/mapas/mapa-saude360.md`
- `docs/evidencias/2026-10-06-rodada-3/mapas/mapa-plantoes-meudia-bi.md`
- `docs/evidencias/2026-10-06-rodada-3/mapas/mapa-governanca-saas.md`
- `docs/evidencias/2026-10-06-rodada-3/mapas/mapa-monetario.md` (root cause do gate financeiro)

## 4. Classificação das jornadas

Legenda: **VALIDADA** = jornada real executada com persistência confirmada · **IMPLEMENTADA** = existe em código com testes, mas sem jornada real nesta árvore · **PARCIAL** = existe incompleto ou com efeito ausente · **QUEBRADA** = defeito conhecido que impede uso como projetado · **NÃO EXECUTADA/AUSENTE** = não existe.

### ADM360 (cotação → estorno) — item 6

| Etapa | Classificação | Evidência / observação |
|---|---|---|
| cotação | IMPLEMENTADA | testes de domínio/transmissão; jornada real não executada |
| orçamento/aprovação (reserva FEFO) | IMPLEMENTADA | idempotente; sem trava global de custo |
| compra/pedido | VALIDADA | jornada M3.7 real (pedido PC-32) + testes |
| documento/conferência XML-NF-e | VALIDADA | M3.1–M3.6 reais (quarentena tipada, dedup) |
| recebimento parcial | VALIDADA (parcial no confronto) | M3.7 confirmação real; **sem confronto automático NF-e × qtd recebida** |
| qualidade/inspeção | VALIDADA | M3.8 real |
| estoque/lote | IMPLEMENTADA | efeitos colaterais validados em M3 |
| vale/consumo | IMPLEMENTADA | Gates A cobertos por teste; jornada não executada |
| valorização/venda | IMPLEMENTADA | Blocos B/C (centavos exatos) testados |
| título/recebimento/comissão/caixa | IMPLEMENTADA | Blocos + fechamento testados; FecharCaixa sem idempotência (risco de corrida) |
| estorno | PARCIAL | **só integral** (`FinanceiroVendas.cs:115-137`) |
| divergências (decisão/efeitos) | PARCIAL | quarentena taxonômica existe; sem tipo declarada nem decisão "aceitar divergência" |
| devolução parcial (compra) | AUSENTE | só consignação tem devolução parcial |
| pendências com responsável+prazo | AUSENTE no módulo | board genérico existe mas não consome tabelas adm360 |
| rastreabilidade ponta a ponta | PARCIAL | elos existem nas chaves; falta view/CTE unificado |
| consulta "efeitos da operação" | PARCIAL | relatórios separados; sem endpoint consolidado |
| próxima ação por estado | PARCIAL/AUSENTE como contrato | `@if` ad-hoc por view; UI×API podem divergir |

### Saúde 360 (paciente → recebimento) — item 7

| Etapa | Classificação | Evidência / observação |
|---|---|---|
| paciente | IMPLEMENTADA | LGPD/CPF validados por teste; jornada real não executada |
| plano/convênio | PARCIAL | CRUD existe; sem contrato/tabela efetiva; pré-autorização não integra ao fluxo |
| agendamento | IMPLEMENTADA | máquina de estados + conflito 409 no create |
| check-in/chamada | IMPLEMENTADA/PARCIAL | fila+senha OK; painel sem filtro por unidade/profissional; chamada não altera status |
| triagem | IMPLEMENTADA | IMC, risco obrigatório p/ finalizar, concorrência testada (F1b) |
| consulta | IMPLEMENTADA | lock otimista, CIDs, workspace; sem reabertura pós-FINALIZADA (só adendo) |
| prescrição/documentos | PARCIAL | texto livre; documentos não são primeira classe da jornada |
| finalização | IMPLEMENTADA | pendências impeditivas + tx + geração financeira automática idempotente |
| faturamento | PARCIAL | conta gerada em `clinica_contas_receber`; tela principal consome módulo v115 distinto; sem guias/lotes |
| recebimento | PARCIAL/QUEBRADA (baixa) | insert CONFIRMADO existe, mas **não baixa `valor_pendente` nem alimenta caixa** automaticamente |
| reagendamento | PARCIAL (estado morto) | `REAGENDADO` sem transição de saída; sem revalidação de conflito; `NovaDataInicio/Fim` não consumidos |
| pré-autorização pendente bloqueando | AUSENTE | `FinalizarAsync` nunca consulta `convenio_autorizacoes` |
| fila por unidade/profissional | PARCIAL | fila de consulta filtra; triagem/painel não expõem filtro nas views |
| conflito de agenda | PARCIAL | check-then-insert + catch; **sem constraint exclução no banco**; update/reagendar fora do check |
| plano principal | IMPLEMENTADA | índice parcial único v2306 + troca atômica |
| formulários por papel | PARCIAL | financeiro renderiza o form genérico de triagem **com campos clínicos** (`Receber.cshtml:5` → `Formulario.cshtml:76-85`); agendamento/paciente já são específicos |

### Plantões / Meu Dia / BI — item 8

| Etapa | Classificação | Evidência / observação |
|---|---|---|
| publicar→convite→aceite→escala→execução→conferência | IMPLEMENTADA | contratos V2157–V2170 sólidos (vaga última sob concorrência, locks, idempotência) |
| fechamento/apuração | IMPLEMENTADA | `FechamentoOperacionalService` com tipos de divergência e bloqueios |
| pagamento | IMPLEMENTADA | dois caminhos de criação coexistem sem trava mútua visível (risco) |
| contestação | PARCIAL/QUEBRADA | **nada atualiza `pagamento.status='contestado'`** (`Data.cs:1912` só comentário); aberta em 'pendente' fica irremediável até aprovado/pago |
| plantão 'encerrado' | QUEBRADA/AUSENTE | estado existe na máquina de domínio, **nenhum endpoint o aplica** (alias de /realizar) |
| substituição | PARCIAL | dois mecanismos paralelos (endpoint legacy × `substituicoes_plantao` atômica); agregados contam escalas recusada/cancelada como ocupação |
| ausência/falta | PARCIAL | fluxo existe; **sem liberação/reatribuição de vaga**; sem teste funcional |
| Meu Dia | PARCIAL | dados REAIS (CTEs), mas prioridade estática sem justificativa, prazo nulo p/ maioria, responsável não renderizado, **agenda "fake"** por comparação de datas e agenda admin 100% templates |
| BI | PARCIAL/QUEBRADA (filtros) | KPIs acumulados disfarçados de mensais; filtros unidade/hospital/médico **ignorados no SQL** (`ReportQueryService`); tenant nulo vaza para todos; competência por `reg_date`; schema com `timestamp`×`timestamptz` mistos e sem `at_time_zone` |

### IA — item 9

- Camada IMPLEMENTADA (reservas, orçamento, cota, fallback, governança — WP-A2, suíte `AiRodada2GovernancaTests`).
- Inferência real com provedor externo: **NÃO EXECUTADA/HOMOLOGAÇÃO PENDENTE** (sem chave real no ambiente; `NAO_CONFIGURADO` declarado) — regra mantida: sem chave, não declarar homologação externa.
- Novas jornadas pedidas (explicação de pendências, resumo de divergências, comparação de cotações, explicação de indicadores): **AUSENTES**.

### Segurança/governança (itens 4–5) — resumo do mapa

- Gaps críticos identificados: (1) módulo AGENDADO sem ativação automática; (2) Web opera com **claims congeladas no cookie** (revogação não atinge sessão aberta até RefreshContext/401 BFF); (3) escopo global por **nome de perfil** (`SUPER_ADMIN` em lista literal); (4) corrida da última assinatura sem trava (500 genérico no lugar de 409); (5) **teste de isolamento com mesmo módulo nos dois tenants NÃO EXISTE**; (6) TestSignin protegido só por ambiente (404 fora de Development/Testing — funcional, mas sem flag de configuração).

## 5. Gate financeiro (item 2) — root cause confirmado

**Causa raiz**: binding de `decimal` do MVC em **cultura invariante** sobre campo `type="number"` sem máscara. `"12,50"` chega ao servidor bruto (Enter antes do blur / Safari / webview) e `NumberStyles.Number` + InvariantCulture interpreta a vírgula como **separador de milhar** → **1250**, silenciosamente (verificado empiricamente: `Convert.ChangeType("12,50", decimal, InvariantCulture) == 1250`). Ponto de entrada: `Administrativo360Controller.Cadastros.cs:202` e os ~40 demais inputs monetários do módulo; canal IA: `AssistenteIaController.cs:75`. JSON/Dapper apenas propagam. Detalhes e inventário completo: `mapa-monetario.md`.

**Contrato a implementar (WS-A2)**: parser central de valores numéricos humanos (pt-BR + invariante, regras explícicas p/ ambíguos), binder aplicado a todos os `decimal` do Web, correção do canal IA, apresentação única `N2` pt-BR, matriz de testes (12,50 / 1.234,56 / zero / limites / casas / inválidos / ambíguos) em inclusão/edição/releitura + reconciliação histórica **diagnóstica** (sem ÷100 automático).

## 6. Plano de execução da rodada 3

| WS | Item | Escopo-resumo |
|---|---|---|
| WS-A2 (GATE) | 2 | Integridade monetária/cultural: parser+binder central, canal IA, apresentação única, testes de matriz, reconciliação histórica com evidência |
| WS-A3 | 3 | Documentos/recebimentos/eventos: unicidade documental no banco, idempotência concorrente, decisão placeholder×físico explícita, `sequencia_evento` ordenado+único, migrations com pré/pós-condições |
| WS-A4 | 4 | Segurança/isolamento: suíte bidirecional de dois tenants com o mesmo módulo (leitura/escrita/exclusão/arquivos/exportação/entidades no corpo), revalidação TestSignin (flag off por padrão), flaky pela causa |
| WS-B5 | 5 | Governança SaaS: escopos sem nome genérico, cadeia de decisão completa, AGENDADO automático, claims revogáveis na Web, limites nos serviços + teste de último recurso, upgrade/downgrade, semânticas explícitas |
| WS-B6 | 6 | ADM360: divergências c/ decisão+efeitos, devolução parcial, pendências c/ responsável+prazo, rastreabilidade unificada, consulta de efeitos, ações permitidas por estado |
| WS-B7 | 7 | Saúde 360: reagendamento c/ efeitos+conflito, pré-autorização como pendência bloqueante (com override explícito), filas por unidade/profissional, constraint de agenda, baixa automática no recebimento, form financeiro específico |
| WS-B8 | 8 | Plantões/Meu Dia/BI: 'encerrado' alcançável, contestação c/ estado próprio, ausência com reatribuição, Meu Dia (prioridade explicável/prazo/responsável/agenda real), BI (período vs acumulado, filtros no SQL, tenant, competência, fuso documentado) |
| WS-B9 | 9 | IA: revalidar reservas/orçamento/fallback/concorrência + jornadas de explicação (pendências, divergências, cotações, indicadores) com fontes/período/truncamento |
| WS-C10 | 10 | Design: breadcrumbs humanos, tokens consolidados, layout autenticação×aplicação, viewports reais 1440/768/390, acessibilidade, mobile compacto |
| WS-C11 | 11 | Entrega: build/testes/integração, instalação limpa+upgrade, regressão das jornadas, manifestos/checksums, matriz plano×módulo×perfil×ação×escopo, reconciliações, roteiro por perfil, backlog, classificação APROVADO/FALHOU/BLOQUEADO/NÃO EXECUTADO |

Ordem: WS-A2 → A3 → A4 (bloqueiam aprovações financeiras/contratuais) → B5 → B6 → B7 → B8 → B9 → C10 → C11. Um commit por WS (mensagens PT-BR ASCII), suíte verde antes de cada commit; produção só após decisão explícita.
