# WS-A2 — Integridade monetária (GATE do item 2)

> Entregue em 2026-10-06. Base de origem: `origin/main` = `7f43af2` (+ `6b6ca3d` auditoria inicial).
> Contrato do gate: **nenhum fluxo financeiro é aprovado enquanto existir multiplicação/alteração silenciosa de valores**.
> Mapa-fonte da investigação: `mapas/mapa-monetario.md` (root cause com file:line).

## 1. Root cause — duas causas independentes, ambas corrigidas na raiz

1. **Parse invariant (o "×100" clássico).** O binder de `decimal` do MVC lia o texto digitado em
   `InvariantCulture`: `"12,50"` → vírgula interpretada como separador de milhar → **1250**.
   Primeiro ponto afetado: `backend\PlantaoPro.Web\Controllers\Administrativo360Controller.Cadastros.cs:202`
   (`SalvarProduto`). Segundo ponto: canal de linguagem livre da IA,
   `backend\PlantaoPro.Web\Controllers\AssistenteIaController.cs:78`.
2. **Falha silenciosa de ModelState (por que ninguém via).** No MVC clássico (sem `[ApiController]`)
   um campo decimal que falha no binding **não interrompe a ação**: liga como `default(0)` e o fluxo
   prossegue até o redirect de sucesso como se nada tivesse acontecido. É por isso que o caso ambíguo
   devolvia um 302 "sucesso" em vez de validação visível ao usuário.

Nenhum ÷/×100 nos serviços de negócio foi encontrado na auditoria inicial; a corrupção acontecia **apenas
nos pontos de entrada de texto** (formulários Web e canal IA) — o que concentra o reparo em binding +
contrato de rejeição, sem mexer nos cálculos decimais já corretos (colunas `numeric(18,4)`).

## 2. Contrato implementado

| Camada | Arquivo | Papel |
|---|---|---|
| Parser central | `backend\PlantaoPro.CrossCutting\Localization\ValorHumano.cs` | Fonte única de regras para converter texto humano → `decimal`, com mensagem humana de erro (`TentarConverter`) e apresentação pt-BR N2 que nunca altera o valor (`Format`). |
| Binding Web | `backend\PlantaoPro.Web\Models\ValorHumanoModelBinder.cs` | Provider+binder global para todo `decimal`/`decimal?` do Web, registrado em `Program.cs:23` (`ModelBinderProviders.Insert(0, ...)`). |
| Short-circuit | `backend\PlantaoPro.Web\Services\Mvc\ModelStateInvalidoFiltro.cs` | Filtro global (`Program.cs:26` + `:56`): ModelState inválido ⇒ ação **não executa**, **nenhuma chamada à API é feita**; volta à página de origem (PRG padrão da casa) com mensagem humana em `TempData["Error"]`; sem `Referer` responde 400. Cede prioridade a outros filtros que já decidiram (guarda SaaS). |
| Canal IA | `backend\PlantaoPro.Web\Controllers\AssistenteIaController.cs` | Textos monetários extraídos de linguagem livre passam pelo mesmo `ValorHumano.TentarConverter`. |
| Localização | `backend\PlantaoPro.Web\Program.cs:31` | `AddRequestLocalization` pt-BR para views/model-binding. JSON de contrato (STJ) e rotas `/bff/*` seguem invariant, preservando o contrato máquina→máquina. |
| Apresentação | `Administrativo360Controller.Financeiro.cs`, `.Relatorios.cs`, `.RelatoriosFinanceiros.cs` | Exportações/páginas financeiras exibem via `ValorHumano.Format` (pt-BR N2) **somente na exibição** — o valor persistido é inalterado. |

### Regras de parse (fonte única, cobertas por teste)

| Entrada | Resultado | Regra |
|---|---|---|
| `"12,50"` / `"12.5"` / `"12.50"` / `"12,5"` | `12.50` | separador único com fração de 1–4 casas = decimal |
| `"1.234,56"` / `"1,234.56"` / `"1 234,56"` / `"R$ 1.234,56"` / `"$ 12.50"` | `1234.56` / `12.50` | dois tipos de separador: o último é o decimal; `R$`/`$`/espaços ignorados |
| `"1.234.567"` | `1234567` | separador único repetido = agrupamento de milhar (último grupo deve ter 3 dígitos) |
| `"0"` / `"0,00"` | `0` | zero passa como zero (nunca virado nulo/ausente em silêncio) |
| `"999999999999.9999"` | `999999999999.9999` | teto = precisão do schema `numeric(18,4)` |
| `"1.234"` / `"12,345"` | **rejeitado** | separador único com exatamente 3 casas = ambíguo (milhar ou centavos); mensagem exibe o valor e sugere `"1.234,00"` |
| `"12,"` / `"12."` / `"."` / `","` / `"-"` | **rejeitado** | fracao vazia/sinal só = entrada incompleta, nunca chutada |
| `"12,50000"` | **rejeitado** | mais de 4 casas (precisão do sistema) |
| `"1.000.000.000.000"` | **rejeitado** | excede o teto `numeric(18,4)` |
| `null` / `""` / `"abc"` / `"1e5"` / `"12a,50"` / `"1.2.3"` | **rejeitado** | inválido, com mensagem humana (sem "Parameter 'x'") |

Sinal apenas inicial (`"-3,14"`, `"+5"`). **Nunca há multiplicação/divisão por 100: o valor digitado é o valor persistido.**

## 3. Verificação

### 3.1 Unit — `backend\PlantaoPro.Tests\ValorHumanoTests.cs` (novo)
Matriz completa do §2: 25 casos válidos, 25 inválidos (todos rejeitados com mensagem humana não-vazia),
motivo ambíguo com exemplo, teto com mensagem, e 4 casos de `Format` (apresentação pt-BR). **VERDE.**

### 3.2 Gate de integração — `backend\PlantaoPro.Tests\Administrativo360GateMonetarioTests.cs` (novo, 7/7 VERDE)
BFF real (`WebApplicationFactory` rodando o `Program.cs` real, sessão ADM360 via kit de teste) com stub
determinístico da API; as asserções valem sobre o **payload JSON BFF→API** e o status HTTP:

| Cenário | Contrato verificado |
|---|---|
| Inclusão `"12,50"` | 302 PRG + payload contém `"precoCusto":12.5` e **nunca** a substring `1250` |
| Edição (mesmo fluxo, id presente) | id no payload + `"precoCusto":12.5` + sem `1250` |
| `"1.234,56"` | payload `"precoCusto":1234.56` |
| `"0"` | payload `"precoCusto":0` |
| `"1.234"` (ambíguo) | 302 p/ `/Administrativo360/Produtos` + **stub não recebeu nenhuma requisição** (nada chega ao sistema) |
| `"1.000.000.000.000"` (limite) | idem: 302 + stub vazio |
| Exportação CSV financeira | CSV contém `"1.234,56"` (pt-BR N2) e não contém `"1234.56"` — apresentação sem alterar valor |

Inclusão, edição e releitura estão cobertos (inclusão/edição pelo POST de produto; releitura pela exportação
que lê os valores persistidos e os apresenta).

### 3.3 Suíte completa
`dotnet test backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj` → **902/902 VERDE** (um rerun limpo após o
flake A3/G5 conhecido sob paralelismo — raiz tratada em WS-A4; execução isolada do gate sempre verde).

## 4. Reconciliação histórica do banco de testes (`plantaopro_test`)

Método: heurística de **inteiro ≥ 100** nas colunas monetárias (`c = FLOOR(c) AND c >= 100`) com
atribuição de tenant e timestamps (scripts `scripts\local\ws-a2-reconcilia-{1..5}*.ps1`). Fase 1 mapeou
237 colunas numéricas; o varrimento cobriu a família ADM360 (46 pares tabela|coluna) e, em fase 4,
famílias saúde/faturamento (`clinica_*`, `v113_*`, `v115_*`, `v116_*`, `pagamentos_*`).

### 4.1 Confirmada com evidência documental (corrigida, auditável)

| Linha | Antes | Depois | Evidência de origem |
|---|---|---|---|
| `adm360_produtos` `0d96cc88-7577-4156-a5de-b94a4436092c` (SKU `WPB-001`, tenant demo `d3f6584c…`) | `1250.0000` | `12.5000` | Finding #2 de `docs\evidencias\2026-10-05-wp-b-jornadas\jornadas.md`: "o '12,50' digitado virou 1250 — valor ×100 no banco"; linha M2.5 registrada como "PASS (com finding decimal)"; captura `m2-5-produto-criado-sucesso.png`. Produto **não** criado por código de teste (grep `WPB-001` nos testes: sem ocorrências). |

Auditoria da correção: `ws-a2-reconcilia-5-correcao.ps1` — `UPDATE … AND preco_custo = 1250.0000`
(condição guardada), `UPDATE 1`, `created_at` preservado (`2026-10-05 07:05:56 -03`). Nenhum teste depende
do valor antigo (verificado por grep).

### 4.2 Artefatos da suíte de testes (padrão identificável; canal imune ao bug)

- `SKU-S20 / Material S20 / 100.0000` repetida em dezenas de tenants sintéticos criados a segundos de
  diferença (09-24 19:55 → 09-25 09:35); centenas de valores inteiros repetidos (100/200/300/400/500/600/1000)
  em `vendas`, `orcamentos`, `titulos_pagar`, `vales`, `movimentos` e itens desde 09-24 → execuções
  repetidas da suíte. Criados por **JSON numérico direto à API** — canal sem ambiguidade, não afetado
  pelo binder de texto.
- `PROD-T2-ISOLADO` (tenant `8b0c8e74…`, teste de isolamento), tenant sintético `11111111-1111-…`
  (linhas `v116` de 09-30 10:48), tenant `d42a0f10-…` (criado 10-06 02:43, run da suíte desta rodada).

### 4.3 Executações manuais/jornada no ambiente demo (tenant `d3f6584c…` = SantaCasa)

Clusters (mesmo minuto, múltiplas tabelas relacionadas = jornada completa executada):
09-25 10:50 (STENT-DES-001 `2500`, GUIA-ANGIO-002 `450` + orçamento/documentos conexos);
09-28 18:0x (venda `2400` + comissão, vale `1200`); 09-30 07:47–07:48 (títulos a pagar
`350/110/150/1200/1500`, venda `1200`, baixas/estornos); 10-05 07:05–07:35 (jornada WP-B:
`WPB-001` `1250` — a única confirmada — e título `110` = **2 × 55,00 coerente** com M3.7, sem corrupção
detectada naquele fluxo).

Classificação: valores inteiros em reais **não são distinguíveis pela heurística** (um `2500` legítimo é
indistinguível de um `25,00` corrompido sem fonte do que foi digitado). Somente `WPB-001` possui prova
documental; os demais permanecem como legítimos ("plausível, sem evidência em contrário").

### 4.4 Família saúde/faturamento (~20 linhas)

| Origem (tenant/criado) | Linhas | Classificação |
|---|---|---|
| regras `v115` (sem tenant_id, 09-28 03:40) | `valor_base` 180/1200/250 · `valor_fixo` 900 | Definição de regra de faturamento/repasse (seed/regra) — valores de configuração, sem assinatura de bug |
| Jornada saúde (09-28 04:22) | `contas_receber` 180/150/220/120 · `recebimentos` 180/120 | Consistente internamente (recebimento casa com conta); sem evidência em contrário |
| Jornada saúde (09-29 18:14) | `contas_receber` 180/120/260 (+pendentes/pago) · `recebimentos` 180 · `caixa` 380 | Idem — coerente |
| Tenant sintético `11111111-…` (09-30 10:48) | `v116_faturamento_lotes` 360 · `v116_caixa_movimentos` 120 | Artefato de teste |

## 5. Limitações (declaradas, sem prometer ausência absoluta de bugs)

- A heurística não prova ×100 em valores inteiros legítimos; a confirmação exige fonte do que foi digitado
  (capturas/logs de jornada), disponível neste ambiente apenas para os clusters citados.
- Fase 2: tabelas de itens sem coluna `created_at` (`adm360_lotes.custo_unitario`, `adm360_orcamento_itens.*`,
  demais `*_itens`) **não completaram o varrimento** — pendência aberta (§6).
- Banco varrido: apenas `plantaopro_test` local. Outros ambientes não varridos nesta rodada.
- Flakes conhecidos sob paralelismo (A3/G5 e `Aceite12_XmlRepetido_NaoDuplica`) não invalidam os contratos
  (isolados: verdes) mas impedem um único run canônico 100% — causa raiz no WS-A4.
- IA sem chave real: inferência real não homologada externamente (não declarado em nenhum momento).

## 6. Pendências que alimentam os próximos workstreams

1. **WS-A3** — completar o varrimento das colunas `*_itens` (nomenclatura de coluna inconsistente) e as
   colunas monetárias restantes fora das famílias escaneadas.
2. **WS-A3** — se o aceite funcional exigir, conciliar os clusters demo (§4.3) com as capturas originais
   e registrar decisão (manter/corrigir) com a mesma auditoria aplicada à §4.1.
3. **WS-A4** — `TestSigninController` exclusiva de ambiente de teste e desabilitada por padrão; causa raiz
   dos flakes de paralelismo.
4. **C11** — `Properties\PublishProfiles\` dos projetos Web/API aparecem untracked (artefato de IDE):
   decidir versionar ou `.gitignore`.

## 7. Status do GATE (item 2)

| Critério do item 2 | Estado |
|---|---|
| Entrada humana conforme cultura da tela | ✅ binder pt-BR global + parser com regras explícitas |
| JSON sem ambiguidade | ✅ payload com número (`"precoCusto":12.5`); contrato BFF→API invariant preservado |
| Cálculos decimais / arredondamento por regra | ✅ serviços sem alteração (já `numeric(18,4)`); nenhum ÷/×100 localizado |
| Apresentação sem alterar valor | ✅ `Format` N2 apenas em exibição (testado no CSV) |
| Inclusão/edição/releitura testadas (12,50 · 1.234,56 · zero · limites · casas · inválidos · ambíguos) | ✅ matriz unitária 25+25+4+2 e gate 7/7 |
| Custo/preço/quantidade/desconto/comissão/título/recebimento sob o mesmo contrato | ✅ binder+filtro globais cobrem qualquer campo decimal Web; revisão de serviços concluída na auditoria |
| Reconciliação auditável de histórico com evidência de origem | ✅ §4 (uma linha corrigida com auditoria; classificações declaradas) |

**Prontidão técnica: APROVADO.** Aceite funcional e liberação de produção seguem o item 11 (roteiro
por perfil + classificação final); nenhuma publicação automática.
