# Rodada 22 — Relatório final consolidado (r22z)

- **Data:** 2026-10-18 · **Base de entrada:** `8100e45` (r21z, ponta de `origin/main`) · **Ponta local desta rodada:** commit deste arquivo · **Diretriz:** "continuar pelas próximas 10 sprints (R19→R28) e avançar também o design do sistema" — push ao fim de cada rodada segue autorização genérica registrada em r19z.

## O que a rodada fechou

| Item | Severidade | Estado | Prova |
|---|---|---|---|
| **D-R22-1** — 3 ações fantasma sem call-site (gap funcional P3 da R21) | P3 → resolvido | `OrcamentoRejeitar` e `OrcamentoCancelar` ligadas em `OrcamentoDetalhes.cshtml` (botões + modais motivo, visibilidade conforme guardas: RASCUNHO\|ENVIADO p/ rejeitar; RASCUNHO\|ENVIADO\|APROVADO p/ cancelar) e `ValeCancelar` em `ValeDetalhes.cshtml` (RASCUNHO\|EM_SEPARACAO\|PRONTO_PARA_EXPEDICAO), padrão botao+modal-motivo do próprio módulo (`CirurgiaCancelar`) | scan principal pareou os 3 novos forms (82/209/76); set de fantasmas enxuto no mesmo commit (exigência do fato auto-verificador R21) |
| **D-R22-2** — `OrcamentoCancelarReserva` inalcançável (plano de reserva não expunha reservas individuais) | P3 → resolvido | Extensão de superfície: `ObterPlanejamentoReservaAsync` agora retorna `ReservasAtivas` (id/produto/lote/quantidade/criado_em, ATIVA); DTO Application + espelho Web grafia idêntica (ADR-0003); view ganhou seção Reservas Ativas + modal cancelar por-reserva; fato de banco novo (seed→lista→cancelar→remove da lista e zera soma agregada) | contrato BFF `R20BffMappingContratoTests` verde c/ props novos comparados recursivamente; fato `Integracao_PlanejamentoReserva_ListaReservasAtivasE_CancelarRemoveDaLista` verde; superfície final 83/212/76 |
| **D-R22-3** — "fantasma legado" `Pendencias?module=OCORRENCIAS` (herdado da R19 como parâmetro inexistente) | P3 → **falso positivo** | Trace completo executado na auditoria: `ProductivityQueryViewModel.Module` (ProductivityViewModels.cs L89) → `BuildQuery["module"]` (L112) → Api `ProductivityQuery.Module` → SQL `(@module is null or Module=@module)` → CTE `'OCORRENCIAS' as Module` — o redirect entrega a Central filtrada por OCORRENCIAS. Fechado sem mudança de código; registro atualizado | `r22a0` F-R22-C (file:line na fonte em ambos os projetos) |
| **D-R22-4** — `AcoesFantasmaSemChamador` vazio por design | — | As 4 entradas da R21 foram ligadas; o set segue versionado (qualquer fantasma novo entra classificado; o fato auto-verificador exige zero ocorrências enquanto cadastrado) | `r21b1`+`r22b1`+`r22b2` no scanner |

**Design (diretriz):** ADR-0003 re-aplicado na extensão de superfície (espelho c/ grafia idêntica; o contrato BFF já cobre os props novos sem mudança de teste — comportamento previsto pelo ADR). Padrão reutilizável consolidado nesta rodada: **ação BFF com rota Api viva mas sem form = gap funcional, não código morto** — a decisão ligar×remover é sempre informada pelas guardas de estado do repositório (a UI espelha exatamente as transições que a API aceita), com efeito colateral descrito fielmente no modal (nada inventado).

## Gates finais

- Build `dotnet build backend\PlantaoPro.sln --no-incremental -c Release`: **0 erros**; multiset de avisos **idêntico** ao pristino r17 (160 entradas, chave `arquivo|mensagem`, `DIFF=NENHUMA`). **Nota de integridade:** descoberto no r22b2 que o script `r20b1_gate.ps1` apenas compara logs — nas fatias r21/r22b1 ele rodou sobre log estaleiro (comparação vacua). Corrigido desta rodada em diante com rebuild real antes do compare; o build real do r22b2 (que compila todas as mudanças r21+r22) validou retroativamente a superfície de avisos. Bônus: o rebuild real revelou um xUnit2004 latente em `R20BffMappingContratoTests` (`Assert.Equal(true, bool)` — corrigido para `Assert.True`, semanticamente equivalente).
- Suíte final: **1524/1524** (base 1523 + 1 fato de banco novo). Recorrentes sob carga paralela: flakes de banco e2e girando entre `Db_CentralClientes_SqlCompletaDaFonte`, `Aceite17_PersistenciaDuravel` e família 40P01 — cada um isolate 5/5 verde (protocolo de casa). Padrão sugerido p/ R23+: serializar via xunit collection as e2e que partilham o Postgres local (candidato de design, fora do escopo desta rodada).

## Commits da rodada

| Commit | Conteúdo |
|---|---|
| `ecd685a` | r22a0 auditoria (F-R22-A/B/C) + decisões D-R22-1..4 |
| `71439e5` | r22b1 três botões + modais nos detalhes (orcamento ×2, vale ×1) |
| `e2cb7e2` | r22b2 extensão reserva-planejamento + botão cancelar-reserva + fato de banco + fix xUnit2004 |
| (este) | r22z consolidado |

## Observações abertas (não bloqueiam; seguem na fila)

1. **Smoke de UI das novas telas**: sessão portal ADMINISTRADOR_CLIENTE (gestor) — quirk herdado da R19 — ainda bloqueia o clique real nos botões novos; a prova funcional por enquanto é fato de banco + pareamento estático + guardas espelhadas. A fila R23 inclui a retomada da FecharCaixa F14, onde a smoke Adm360 voltará a ser tentativa.
2. **Flakes de banco sob carga**: 3+ corridas completas com falha única e isolate 5/5 — o contéiner de teste é o gargalo (concorrência no Postgres local), não a lógica. Ver item de gates acima.

## Fila R23 (plano das 10 sprints)

1. **FecharCaixa F14 + F-R15-C** (itens herdados das filas R17–R21).
2. Tentativa de desbloqueio da sessão portal gestor p/ smoke das páginas Adm360 (validar visualmente os 4 botões novos).
3. Padrões de casa mantidos: commits `<seções> (rodada 23 r23xx)` sem acentos, gate de casa **com rebuild real** antes de cada commit, evidência versionada em `docs/evidencias/<data>-rodada-23/`, push ao fim da rodada (credencial local wincred fixada na R20).
