# Rodada 20 — Relatório final consolidado (r20z)

- **Data:** 2026-10-18 · **Base de entrada:** `a78921c` (r20a0, ponta da R19) · **Ponta local desta rodada:** commit deste arquivo · **Diretriz:** "continuar pelas próximas 10 sprints (R19→R28) e avançar também o design do sistema" — push ao fim de cada rodada segue autorização genérica registrada em r19z.

## O que a rodada fechou

| Defeito | Severidade | Estado | Prova |
|---|---|---|---|
| **F-R20-A** — `RelatorioComissaoItemViewModel.DataBaixa` × wire `dataRecebimento` (relatório de comissões): toda a coluna Data (e o CSV) em `0001-01-01` | P2 visível | Corrigido (rename p/ a grafia do wire, precedente F9d) | fix + ripple (view L228 + export CSV); fato de mecanismo offline (wire camelCase real → espelho Web, valor chega) + contrato versionado |
| **F-R20-B** — `FluxoCaixaItemViewModel.Tipo` nunca enviado pela Api (`FluxoCaixaItemDto` sem o campo): todas as linhas classificadas como badge "Prev. Pagar" | P2 visível | Corrigido (remoção do `Tipo`; classificação derivada dos valores transportados na view — ADR-0003 regra 3) | fix na view de fluxo; contrato versionado trava a forma do espelho |
| **F-R20-C** — `TituloBaixaViewModel.MotivoEstorno` × wire `recebidoPor` (estorno no detalhe do título): motivo sempre em branco | P3 | Corrigido (rename p/ `RecebidoPor`) | fix + ripple view L270; fato de mecanismo offline |
| **F-R20-D/E** — campos mortos: `TituloReceberDetalhesViewModel.Observacoes+CriadoEm`, `ContaFinanceiraViewModel.CriadoEm` (ausentes nos DTOs; `CriadoEm` = mina `0001-01-01` latente) | P3 | Corrigido (remoção; zero consumo, zero construções manuais) | contrato versionado |
| **F-R20-G** — tipos fantasmas `ExtratoLancamentoViewModel` + `ExtratoContaViewModel`: nenhum controller desserializa e divergem do DTO (`SaldoInicial`×`SaldoAbertura`, `Lancamentos`×`Movimentos`, conta achatada × aninhada) | P3 | Corrigido (remoção) | contrato versionado |
| **Parte A — extensão do scanner F-R15-A**: os 4 `Adm360*WebController*` estavam fora do scan de form-binding | — | Instaurado | glob estendido c/ pareamento por URL estática (segmento a segmento, `{param}` = wild-card) + whitelist `AnaliseIa` (JS-driven via `data-ai-url`); superfície medida: **79 pareamentos / 203 parâmetros / 76 ações**, pisos travados; zero ação sem form fora da whitelist |
| **Parte B — camada DTO-API × ViewModel-Web** (nova família de risco: binding STJ por nome case-insensitive deixa campo no default silencioso) | — | Instaurado | `R20BffMappingContratoTests` (4 fatos): dicionário versionado 53 endpoints com antivacuidade nos dois sentidos sobre 90 chamadas; tipos Api existentes c/ uso visível; props Web ⊆ props Api case-insensitive **recursivo em toda a árvore** (809 props / 21 aninhadas); fato de mecanismo STJ offline nos fixes |
| Design (diretriz de avançar o design) — **ADR-0003** (contrato BFF: espelho c/ grafia idêntica à chave do wire; tipo Application compartilhado; derivação de apresentação só sobre valores transportados; allowlist "espelho aguardando a Api" versionada e auto-obsoletável) | — | Instaurado | `docs/arquitetura/adr/adr-0003-*` + mapa de contratos e índice em `docs/arquitetura.md` |

Matriz completa de achados e baseline limpo (~47 pares 1:1, incl. aninhados) na seção F3 de `r20a0-auditoria-inicial-plano-sprint.md` (preenchida nesta rodada). Desvendamento estrutural registrado lá: 4 respostas da família vêm de tipos **locais do projeto Api** (`Administrativo360ResumoDto`, `ContratoTrabalhoDetalheDto`, `DashboardAdm360Resposta`, `UserListVM`) — razão pela qual o scanner permanente lê também `PlantaoPro.Api`.

## Gates finais

- Build `dotnet build --no-incremental -c Release`: **0 erros**; multiset de avisos **idêntico** ao pristino (160 entradas, chave `arquivo|mensagem`, `Compare-Object` sem divergência).
- Suíte final: **1522/1522** (base 1518 + 4 fatos `R20BffMappingContratoTests`; scanner da parte A estendido no fato existente). Um flake em `R19DapperBindingGuardTests.Db_CentralClientes_SqlCompletaDaFonte` (e2e de banco sob carga paralela — passa na corrida seguinte) tratado pelo protocolo de casa: isolate 5/5.
- Smoke vivo de UI **não executado** nesta rodada: as três páginas tocadas (comissões/relatórios financeiros, fluxo de caixa, detalhe do título) só são alcançáveis por usuário do tenant Santa Casa, e o quirk de sessão portal gestor (observação aberta 1 da R19) ainda rebola GET pós-login. A prova dos fixes P2 repousa no fato de mecanismo STJ offline (wire camelCase real deserializado no espelho Web com as opções do BFF) + no contrato versionado que torna a casada de nomes a condição do binding. Confirmação visual segue pendente do quirk de sessão.

## Commits da rodada

| Commit | Conteúdo |
|---|---|
| `a78921c` | r20a0 auditoria inicial + plano de slices (superfícies medidas, decisões D1–D4) |
| `143c479` | r20b1 contrato BFF versionado (scanner parte A estendido + `R20BffMappingContratoTests` + fixes F-R20-A..G + ADR-0003 + seção F3 preenchida) |
| (este) | r20z consolidado + fato de mecanismo STJ offline dos fixes P2 |

## Observações abertas (não bloqueiam; seguem na fila)

1. **Sessão portal ADMINISTRADOR_CLIENTE (gestor)** — herdada da R19, continua reproduzível; bloqueia smoke de UI das páginas Adm360 para o tenant Santa Casa. Próxima rodada que tocar sessão/portal deve levá-la.
2. **Espelho aguardando a Api (R8-B27)**: `FluxoCaixaViewModel.Definicoes/TimeCriterion` consumidos pela view com degradação suave mas ainda não emitidos por `FluxoCaixaDto` — allowlist versionada no contrato (obsoletos quando a Api emitir). Mesma situação, coberta, nos pares resumo/dashboard que a Api já atende.
3. **Fantasma legado** `Ocorrencias → Pendencias?module=OCORRENCIAS` (P3, item 9 → R22).

## Fila R21 (plano das 10 sprints)

1. **Whitelist JS-driven das 6 ações** sem form estático identificadas na auditoria (ex.: `AbrirTriagem` via rota crua já coberta pelo pareamento de URL; restantes a classificar: JS-driven com justificativa vs orfanato de fantasia).
2. Padrões de casa mantidos: commits `<secoes> (rodada 21 r21xx)` sem acentos, gate de casa antes de cada commit, evidência versionada em `docs/evidencias/<data>-rodada-21/`, push ao fim da rodada.
