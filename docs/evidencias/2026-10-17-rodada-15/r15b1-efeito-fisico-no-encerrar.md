# R15b1 — Efeito físico dirigido no ENCERRAR (A12 baixa × devolução)

**Rodada 15 · fatia r15b1 · base `aa8b5c5` (r15a0) · contrato: P1b herdada R11→R14**

Objetivo: o efeito de saldo do ciclo de ocorrência sai do RESOLVER e passa a ser
**dirigido pelo destino no ENCERRAR**, canônico `EfeitoFisico ∈ {DEVOLUCAO, BAIXA}`
(decisões D1–D8 do r15a0). RESOLVIDA segue mantendo BLOQUEADO até encerrar; DEVOLUCAO
espelha a liberação histórica; BAIXA consome fisicamente por perna única
`BAIXA_OCORRENCIA` — a única exceção autorizada à soma física nula do ciclo.
Sem DDL. Campo opcional no fim do command (nulo/vazio ⇒ DEVOLUCAO; estranho ⇒ 409).

## 1. Alterações por camada (diff: 11 arquivos, +337/−45, + 1 arquivo novo)

| Camada | Arquivo | O que mudou |
|---|---|---|
| Domain | `Administrativo360/OcorrenciasA12.cs` | Constantes de efeito, `ValidarEncerramento(destino, justificativa, efeitoFisico=null)`, `NormalizarEfeitoFisico` (case-insensitive via `ToUpperInvariant`; nulo/vazio ⇒ DEVOLUCAO; estranho ⇒ inválido) |
| Application | `Administrativo360/Contracts.cs` | Command de encerrar ganha `string? EfeitoFisico = null` (fim da assinatura = compat) |
| Infrastructure | `Administrativo360/OcorrenciasRepository.cs` | `TransicionarAsync` +parâmetro; liberação saiu do ramo RESOLVIDA e foi para o ENCERRADA, ramificado por efeito; espelho extraído em `LerBlocosRegistradosAsync` (compartilhado liberação/baixa); novos `BaixarSaldoBloqueadoAsync` + `EscreverPernaBaixaAsync` (perna única BLOQUEADO −q, chave idempotência `{ocorrenciaId}:BXA:{localId}:BAIXA_BLOQUEADO`, motivo canônico `"Baixa fisica do saldo bloqueada da ocorrencia (ocorrencia {id})"`) |
| Web | `Controllers/Administrativo360Controller.ComprasRecebimentos.cs` | Ação `EncerrarOcorrencia` recebe `efeitoFisico` e programa `EfeitoFisico` no payload (trimado; vazio segue vazio ⇒ normalização do domínio) |
| Web | `Views/Administrativo360/Ocorrencias.cshtml` | `<select name="efeitoFisico">` com DEVOLUCAO (selected/default) e BAIXA dentro do colapso do form de encerrar |
| Web | `Localization/UiTexts{,.en,.es,.fr}.resx` | 3 chaves novas × 4 idiomas (650→653), inseridas após `Adm360.Occ.ThDestino`: `Adm360.Occ.LabelEfeitoFisico`, `.EfeitoDevolucao`, `.EfeitoBaixa` |
| Tests | `Administrativo360OcorrenciasA12Tests.cs` | Header atualizado; adaptações dos fatos existentes ao novo contrato (RESOLVIDA não libera mais; encerra dirige); +3 fatos integrados: BAIXA exata (perna única, saldo físico −q), baixa sobre bloqueio parcial (seed qty 10, ocorrencia 3), baixa multi-local (2 locais, 2 pernas independentes) |
| Tests | `R15A12EfeitoFisicoContratoTests.cs` (novo) | 4 fatos: 2 source-scan (writer com perna única + espelho compartilhado; contrato/view/controller programam o efeito) + 2 web-bff stub (página renderiza o seletor nas duas opções; POST programa `"EfeitoFisico":"BAIXA"` no payload à API) |
| Tests | `R7DF6Admin360I18nContratoTests.cs` | Contrato i18n de contagem fixa atualizado 650→653 (renomeio do método + assert comentado) |

## 2. Fatos e evidência (logs em `%TEMP%\opencode\`)

- **F1 — Build limpo**: `r15b1_build6.log` → `BUILD_EXIT:0`, 0 erros (34 avisos pré-existentes, nenhum nos arquivos tocados — verificado por grep).
- **F2 — Suíte filtrada das classes tocadas**: `r15b1_filtered.log` → **31/31 aprovados** (27 A12: 18 fatos + 9 casos de theory; 4 R15).
- **F3 — Gate full**: `r15b1_gate.log` → **1463/1463 aprovados, 0 falhas**. Delta do baseline r15a0 (1456): **+3** fatos A12 novos + **4** fatos R15 novos = **+7** → 1463 exato. A família ① (40P01 em `PurgarAsync`) não apareceu nesta rodada.
- **F4 — Contrato i18n**: os 4 resx passam no teste de 653 chaves em todos os idiomas (dentro do gate); fonte das chaves nova no parágrafo 1.

## 3. Achados e gotchas desta fatia (com prova)

- **G1 — typo do nome do campo + required implícito do NRT (o bug que quase escapou)**: a view chegou com `<select name="efetivoFisico">` ("ti" transposto) enquanto o parâmetro do controller é `string efeitoFisico`. Com NRT ativo, parâmetro reference-type não-nulável ganha `[Required]` implícito; o `ModelStateInvalidoFiltro` global intercepta o POST com ModelState inválido e redireciona **antes da ação e antes de qualquer chamada à API** (302 para o Referer local). Prova obtida por sonda temporária no filtro (`X-R15Diag-MS`):
  `loc=/Administrativo360/Ocorrencias | status=302 | ACAO: <sem-header> | MODELSTATE: efeitoFisico: The efeitoFisico field is required.`
  Correção: `name="efeitoFisico"` na view + campo `["efeitoFisico"]` no web-bff + source-scan atualizado. Impacto: sem a correção, **toda submissão do seletor falharia** como campo obrigatório — o defeito foi pegado pelo contrato web-bff novo antes de chegar à produção.
- **G2 — lambda ternário quebrou o csc 34× em região ASCII perfeita**: o arquivo novo falhou com 34 erros de sintaxe começando em (100,74) apesar de os bytes da região serem verificadamente ASCII limpos (dump char-codes `r15_line100.ps1`). Reescrever o ternário multi-linha em if-chain (estilo da casa, padrão R14/R5A2) resolveu; builds seguintes determinísticos. Causa-raiz ambiental (suspeita: final de linha/race na escrita inicial do arquivo) — registrado para a z-report.
- **G3 — contagem fixa de i18n**: o contrato R7DF6 asserciona contagem exata de chaves (650). Qualquer resx nova exige o bump — feito para 653 com comentário apontando as 3 chaves.
- **G4 — o seletor vive dentro do colapso RESOLVIDA**: lista vazia nunca renderiza o form de encerrar; o fato de renderização exige uma ocorrência RESOLVIDA no stub (shape exata do `Ocorrencia360ViewModel`, camelCase), não `data:[]`.

## 4. Sondas temporárias — instaladas e removidas (grep `X-R15Diag` no backend = 0 ocorrências)

| Sonda | Onde | Propósito |
|---|---|---|
| header `X-R15Diag` | top da ação `EncerrarOcorrencia` | provar se a ação executou e o que bindou |
| header `X-R15Diag-MS` | `ModelStateInvalidoFiltro.OnActionExecuting` | dumpar chaves+mensagens do ModelState inválido (→ G1) |
| `File.WriteAllText(r15b1_post_diag.txt)` | fato web-bff POST | carregar as sondas no resultado sem depender de assert que passa |

## 5. Estado e próximos passos

- Árvore: apenas os arquivos da tabela 1 (+ o teste novo); sem artefatos de debug no repo.
- Credenciais/banco inalterados nesta fatia (sem ciclo de grants — o SV admin já possui ADM360 ativo); net-zero do banco é trivialmente mantido porque nenhum write fora do Postgres de teste/suite ocorreu.
- **Próximos**: commit desta fatia; opcional r15b2 smoke vivo no browser (sem grants — login SV existente); depois r15z consolidado + fila R16 (P2 F-R14-C parsers Web → …). Push só com autorização explícita ao fim da rodada.
