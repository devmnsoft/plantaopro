# Rodada 20 — Auditoria inicial e plano (r20a0)

- **Data:** 2026-10-18 · **Base:** `7775649` (ponta da R19; `origin/main` em sincronia via push de fim de rodada) · **Escopo:** item #1 da fila R19z — extensão do scanner de casa **F-R15-A** em duas frentes: (A) paths explícitos `Adm360*WebController*` no scan de form-binding e (B) camada **DTO-API × ViewModel-Web** no formato F9d (grafias divergentes entre o DTO que a API serializa e o ViewModel que o Web desserializa — `System.Text.Json` ignora só caixa, não grafia: propriedade com grafia diferente **nunca popula**, falha silenciosa). Design acoplado: ADR-0003 (contrato de mapeamento BFF Web↔Api). Fora de escopo e seguidos na fila R21: whitelist JS-driven das 6 ações (item 2), fantasmas residuais P3 (item 9).

## Achados

### F1 — Superfície e estado atual do scanner (parte A)

O fato permanente `R16ScanFormBindingFonteTests.acoes_post_adm360_parametros_escalares_implicitos_existem_no_form_que_as_aponta` varre apenas arquivos `Administrativo360Controller*.cs` (12 files, baseline 54 pares action×form / 141 parâmetros). Os quatro `Adm360*WebController*` ficam **fora do scan**: `Adm360CotacoesWebController`, `Adm360DocumentosXmlWebController`, `Adm360FiscalWebController`, `Adm360GestaoWebController`.

Superfície nova medida com a heurística idêntica do scanner (script `%TEMP%\opencode\r20_fieldscan_adm360.ps1`, reexecutável): 29 ações `[HttpPost]`, das quais **18 com parâmetros escalar não-nuláveis implícitos** (19 parâmetros) — cada um exigirá campo/rota no form que aponta para a ação:

| Arquivo | Ações (parâmetros) |
|---|---|
| Cotacoes | GerarOrcamento(id) · AprovarResposta(id) · TransmitirResposta(respostaId, cotacaoId) · EstornarResposta(respostaId, cotacaoId, justificativa) · AnaliseIa(id) |
| DocumentosXml | Conferir(id) · Manifestar(id, tipoManifestacao) · AbrirTriagem(documentoId, responsavelId) · ResolverTriagem(documentoId, justificativa) · ExecutarSincronizacao(estabelecimentoId) |
| Fiscal | SalvarConfiguracao(ambiente, bloqueioExterno) · MarcarPronta(id) · Emitir(id) · Reabrir(id) · CancelarNota(id) |
| Gestao | HabilitarCapacidade(capacidade, habilitado) |

As demais 11 ações usam form model complexo (bind por propriedade) ou só parâmetros de rota — mesmas isenções do baseline R16. O pareamento action×form definitivo (e eventuais violações reais) sai na execução r20b1.

### F2 — Superfície BFF: DTO-API × ViewModel-Web (parte B)

Inventário (`%TEMP%\opencode\r20_inventario_bff.ps1`): **90 chamadas `ReadApiResponse<T>` nos 16 files** da família (12 `Administrativo360Controller*` + 4 `Adm360*WebController*`), sobre **~48 tipos Web distintos** (~40 endpoints distintos). Três formas estruturais:

1. **Espelho Web dedicado** (majoritário): `ContaFinanceiraViewModel` (×5), `EstabelecimentoViewModel` (×3), `OrcamentoDetalhesViewModel` (×5), relatórios `Relatorio*ItemViewModel` etc. — a família exposta ao F9d: a grafia é contratada apenas por convenção, e o F9d provou que um `Ativa`×`Ativo` qualquer apaga o dado silenciosamente em toda a view consumidora.
2. **Tipo compartilhado Application→Web** (exceção estrutural): `Adm360FiscalWebController` desserializa **direto em tipos do projeto Application** (`NotaPreEmitidaDetalhesDto`, `ParametrosFiscaisAmbienteDto` — `Application/Administrativo360/FiscalPreEmissoesContracts.cs`; Web referencia Application no csproj). Se a Api devolver exatamente esses records, a família F9d é eliminada **por construção** para eles — pendente de confirmar endpoint a endpoint.
3. **Espelho manual com sufixo próprio**: `DreGerencialDtoWeb` (classe em `Web/Models/Administrativo360Models.cs` L738, ×2 call-sites) — espelho de DTO de relatório mantido à mão, máxima exposição F9d, contrato zero.

### F3 — Divergências do cruzamento (executado em r20b1)

Superfície medida pelo scanner permanente: **90 call-sites / 53 endpoints distintos** (o inventário r20a0 subestimou em ~40 por bug do extrator PowerShell com `List<T>`). Matriz endpoint → DTO Api executada par a par; achados reais — todos tratados cirurgicamente em r20b1:

| Achado | Classe | Sev. | Defeito em produção | Tratamento |
|---|---|---|---|---|
| **F-R20-A** — `RelatorioComissaoItemViewModel.DataBaixa` × API `DataRecebimento` | (i) grafia divergente | P2 visível | coluna Data e CSV sempre `0001-01-01` | rename p/ `DataRecebimento` (precedente F9d) + view `RelatoriosFinanceiros.cshtml` L228 + export CSV. Header já usava a key resx correta (`Adm360.RelFin.ThDataRecebimento`) — sem resx novo |
| **F-R20-B** — `FluxoCaixaItemViewModel.Tipo` nunca enviado (`FluxoCaixaItemDto` sem o campo) | (ii) prop só no VM | P2 visível | todas as linhas classificadas como badge "Prev. Pagar" | remoção de `Tipo`; classificação derivada dos valores transportados na view (realizado se `RealizadoEntrada>0‖RealizadoSaida>0`; previsto receber se `PrevistoEntrada>0`; senão previsto pagar — ADR-0003 regra 3) |
| **F-R20-C** — `TituloBaixaViewModel.MotivoEstorno` × API `RecebidoPor` | (i) grafia divergente | P3 | motivo do estorno em branco no detalhe do título | rename p/ `RecebidoPor` + `TituloDetalhes.cshtml` L270 |
| **F-R20-D/E** — `TituloReceberDetalhesViewModel.Observacoes+CriadoEm`, `ContaFinanceiraViewModel.CriadoEm` ausentes nos DTOs | (ii) dead field | P3 | consumo zero; `CriadoEm` = mina `0001-01-01` latente | remoção (zero construções manuais) |
| **F-R20-G** — `ExtratoLancamentoViewModel` + `ExtratoContaViewModel` | ghost (tipo morto + divergente) | P3 | nenhum controller desserializa; rota de extrato não ligada; diverge do DTO (`SaldoInicial`×`SaldoAbertura`, `Lancamentos`×`Movimentos`, conta achatada × aninhada) | remoção dos dois tipos |
| **Allowlist** — `FluxoCaixaViewModel.Definicoes`/`TimeCriterion` não emitidos por `FluxoCaixaDto` | espelho aguardando a Api (R8-B27) | — | view degrada suave ("Sem definições informadas pela fonte.") | mantidos com registro no allowlist versionado `EspelhoAguardandoApi` (obsoletos quando a Api emitir) |

**Baseline limpo:** os demais ~47 pares batem 1:1 (posicionais + aninhados), incluindo Ocorrencia360, PreviaValorizacao, Orcamento (OrcamentoItem × OrcamentoItemDetalhe — só nullability difere), lookups (Parceiro/Medico/Produto/Local/Lote), trio DRE, Resumo × `Administrativo360ResumoDto`, Dashboard × `DashboardAdm360Resposta`, Contratos × `ContratoTrabalhoDetalheDto`, Usuarios × `UserListVM`, Cotacao/Orcamento/Venda/Vale/Cirurgia/TituloPagar/ComissaoPendente/DfeSincronizacao/DocumentosXml/Estabelecimento/Inspecao/Inventario/SaldoEstoque/Tarefa.

**Desvendamento estrutural (por que o scanner permanente precisa ler também `PlantaoPro.Api`):** quatro respostas da família vêm de tipos **locais do projeto Api**, fora de Application — `Administrativo360ResumoDto` e `ContratoTrabalhoDetalheDto` (`Api/Administrativo360Contracts.cs`), `DashboardAdm360Resposta` (`Api/Controllers/Adm360GestaoController.cs` L9 — o wire real do dashboard; o `DashboardAdm360Dto` de Application é tipo intermediário de repositório, **não** a forma do wire) e `UserListVM` (`Api/Data.cs` L743).

**Contrato travado (`R20BffMappingContratoTests`, 3 fatos):** dicionário versionado c/ 53 entradas endpoint → (elemento Web, elemento Api); antivacuidade nos dois sentidos (90 chamadas cobertas, zero entrada obsoleta); tipo Api existente com uso visível; props Web ⊆ props Api case-insensitive recursivo — **809 propriedades comparadas, 21 comparações aninhadas**. Parte A estendida no mesmo slice: `R16ScanFormBindingFonteTests` agora cobre os 4 `Adm360*WebController*` (**79 pareamentos form×ação / 203 parâmetros / 76 ações**, pisos travados; `AnaliseIa` whitelisted como JS-driven via `data-ai-url`).

## Decisões

- **D1 (parte A):** estender o glob do fato existente para `Adm360*WebController*.cs` mantendo heurística e semântica idênticas; antivacuidade estendida (o conjunto novo contribui ≥19 parâmetros quando pareado); ações sem form estático entram na whitelist `AcoesSemFormEstatico` só com justificativa documentada (JS-driven ou orfão de fantasia → fila item 2/9).
- **D2 (parte B):** novo fato `R20BffMappingContratoTests`: dicionário versionado `endpoint → tipo Api retornado` (revisado endpoint a endpoint em r20b1, ~40 entradas) + asserções: (i) toda rota-chave existe em controller Api (atributo de rota na fonte); (ii) para cada call-site `ReadApiResponse<T>`, **todas as propriedades de T (case-insensitive) existem nas propriedades do tipo Api** (a invariante anti-F9d); (iii) antivacuidade (≥ N pares, ≥ M propriedades). A matriz completa do audit (incluindo extras unilaterais classificados) fica nesta pasta como evidência.
- **D3 (fixes):** defeito real encontrado → rename p/ a grafia da Api (precedente F9d: `Ativa`→`Ativo`) + ripple verificado + fato de regressão pontual quando aplicável.
- **D4 (design):** ADR-0003 — contrato de mapeamento BFF Web↔Api: os dois padrões permitidos (espelho com grafia idêntica; tipo Application compartilhado), o que é proibido (remapeamento manual com grafia própria sem contrato), onde mora a tradução (no máximo renomeia 1:1 no espelho; transformação de forma fica no controller Web com teste), e como os fatos R16/R20 travam tudo.

## Plano de slices

| Slice | Conteúdo | Gate |
|---|---|---|
| r20b1 | Trilha endpoint→DTO Api (~40), fatos: extensão do scan de form (D1) + `R20BffMappingContratoTests` (D2) + fixes de defeitos reais (D3) + ADR-0003 (D4) | build `--no-incremental` 0E/80W chave-a-chave vs pristino + suíte |
| r20b2 | Smoke vivo apenas se houver fix que afeta UI/fluxo (caso contrário, evidência de gate + matriz) | evidência versionada |
| r20z | Consolidado + fila R21 (item 2: whitelist JS-driven das 6 ações) + push | — |
