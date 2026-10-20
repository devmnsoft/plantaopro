# R75 - Inventario maquina paginas x artigos do manual (bloco B3)

## Objetivo

Antes dos lotes de artigos (R76-R78), travar o universo de paginas do manual
(rotas que renderizam views) de forma verificavel por maquina e medir o gap real
de cobertura contra os artigos declarados em `AjudaController`. O plano de
referencia (R66a0) usava como indicador aproximado "~319 acoes de view"; este
inventario substitui essa aproximacao por uma enumeracao reprodutivel e
versionada.

## Artefatos (esta pasta)

| Arquivo | Funcao |
|---|---|
| `r75_gap.ps1` | Script gerador. Uso: `powershell -NoProfile -ExecutionPolicy Bypass -File r75_gap.ps1 [caminho-saida]`. Saida padrao: `r75_inventario.txt` ao lado do script. Ao final emite uma linha unica `INVENTARIO_OK ...` com os totais. |
| `r75_inventario.txt` | Inventario gerado e travado como baseline da R75 (secao Resumo abaixo). |
| `r75a0-inventario-paginas-artigos.md` | Este documento. |

Contrato permanente: `backend/PlantaoPro.Tests/R75InventarioManualContratoTests.cs`
(3 fatos):

- todo artigo da fonte aponta para pagina presente no inventario (zero artigo
  orfao) e a quantidade de artigos do inventario acompanha a fonte (frescor);
- `GAP_SEM_ARTIGO` nao cresce acima do valor travado nesta rodada (573): nova
  pagina sem artigo obriga rerun do script e revisao da evidencia;
- o grupo de quebras potenciais (29 rotas) fica travado literalmente; corrigir
  qualquer rota exige atualizar o contrato com evidencia.

## Como o universo e definido (regras do script)

1. **Pagina** = acao publica de classe concreta cujo nome termina em
   `Controller`, com retorno de tipo action, sem atributo `[NonAction]`;
   somam-se acoes publicas declaradas em bases abstratas (herdadas por todas as
   subclasses; ex.: o `Salvar` da base de modulo vale para cada subclasse).
   Classes parciais contribuem com todas as partes (incluindo as declaradas sem
   base). Metodos de extension (ex.: `RenderPaged<T>`) sao mapeados para
   delegacao, mas nao viram acao.
2. **Resolucao da view** segue cadeias de delegacao ate profundidade 6, na
   ordem: (a) literal explicito em `View("...")` (absoluto `~/Views/...` ou
   relativo); (b) nomeacao padrao pelo par controller/acao (semantica de tempo
   de execucao quando `View(model)` e chamada ate dentro de helper); (c)
   chamada a helper renderizador. A delegacao resolve primeiro o metodo da
   propria classe e depois sobe a cadeia de bases. Chamadas genericas
   (`this.RenderPaged<T>(...)`) sao reconhecidas.
3. **Existencia** = arquivo `.cshtml` em `Views/<dir>/<nome>` ou
   `Views/Shared/<nome>` (resolucao padrao MVC; o projeto nao possui location
   expander customizado).
4. **Classificacao** de cada acao efetiva no inventario:
   - arquivo existente: pagina real, entao `COBERTA` (tem artigo),
     `SEM_ARTIGO` (gap) ou `FORA_ESCOPO` (controllers `Ajuda` e `Manual`,
     superficie do proprio manual);
   - `ACO_AUX`: submissao via POST, resolucao dinamica por interpolacao ou
     helper herdado sem arquivo proprio (nao entra no gap);
   - `POTE_QUEBRA`: pagina GET cuja view resolvida nao existe em disco
     (candidata a falha de renderizacao - ver Achados).
5. **Artigos**: os 25 registros de `AjudaController.CriarArtigos()`, cada um
   com ponteiro `(Controller, Action)` para a tela; o casamento de cobertura e
   por par `controller/acao` sem distinguir maiusculas.

## Resumo (baseline R75)

```
RESUMO|RETURNS_VIEW_BRUTOS=321|ACOES_EFETIVAS_RESOLVIDAS=674|FILE_OK=616|ARTIGOS=25|COBERTAS=24|FORA_ESCOPO=19|GAP_SEM_ARTIGO=573|ACO_AUX=29|POTE_QUEBRA=29
```

- 321 ocorrencias brutas de retorno de `View(` nos controllers. O indicativo do
  plano (319) usava padrao mais restrito (`return View(` sem qualificador);
  mesmo universo, a diferenca vem das chamadas qualificadas
  (`controller.View(...)`) agora contadas.
- Das 616 paginas com arquivo existente, 19 sao superficie do proprio manual
  (fora de escopo). Das 597 paginas efetivas: **24 cobertas por artigos** e
  **573 sem artigo** (gap de partida para R76-R78).
- Dois artigos (`financeiro-pagamento` e `medico-pagamentos`) apontam para a
  mesma pagina `Financeiro/Index`. Situação registrada, nao erro; a
  diferenciacao dos dois textos pode acontecer em rodada futura.

## Achados (quebras potenciais - 29 rotas, travadas no contrato)

| Grupo | Rotas | Leitura estatica |
|---|---|---|
| `Agenda` (7) | calendario, conflitos, dia, hospitais, medicos, mes, semana | Helper `Carregar` renderiza `View(vm)` por nomeacao padrao; somente `Views/Agenda/Index.cshtml` existe na pasta. |
| `DemoAdmin` (4) | gerardados, index, roteiros, status | Renderiza `View("Dashboard")`, mas a pasta em disco e `Views/Demo`, nao `Views/DemoAdmin`. |
| `FaturamentoClinico` (2) | contasreceber, recebimentos | Delegam para `Index`, que renderiza `View(model)` por nomeacao padrao; a pasta tem um unico arquivo. |
| `GestorDashboard` (1) | index | `ModuloAsync` resolve dinamicamente `~/Views/{Controller}/{Action}.cshtml`; a pasta nao existe. |
| `V112Web` (13) | activities, billing, customers, dashboard, demo, homologation, inventory, journey, orders, outbox, products, tasks, templates | Renderiza `View("OperationalPage")`, mas o arquivo esta em `Views/V112/OperationalPage.cshtml`, nao em `Views/V112Web/`. |
| `WorkflowSaude360` (2) | index, proximaacao | Mesmo caso de `GestorDashboard`. |

A analise e estatica (nao ha location expander que resgate essas resolucoes).
A confirmacao em tempo de execucao e a correcao de cada grupo ficam para rodadas
posteriores; cada correcao exige atualizar a lista travada no contrato da R75.

## Uso em R76-R79

1. Lotes de artigos: escolher as paginas-alvo nas linhas `SEM_ARTIGO` de
   `r75_inventario.txt` (ordem por dominio do produto).
2. Após cada mudança em paginas ou artigos: rerun de `r75_gap.ps1` para
   regenerar `r75_inventario.txt` e commitar o diff (o contrato detecta
   desatualizacao).
3. R79 (contrato de zero gap): quando a cobertura atingir todas as paginas
   efetivas, virar `GapMaximoTravado` para 0 no contrato.

## Execucao desta rodada

- Recon: mapeio das fontes (`AjudaController`, `ManualController`, bases
  abstratas, parciais, extension methods) e contagens de referencia.
- Script iterado ate convergir com as regras acima; saida final idem ao
  `RESUMO` acima.
- Contrato criado com 3 fatos; suíte alvo ampliada de 1688 para 1691 casos.
- Gates padrao da casa: build A/B sem novos avisos, suíte completa, docscan e
  higiene apos este documento.
