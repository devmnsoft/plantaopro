# Rodada 22 — Auditoria inicial e plano (r22a0)

- **Data:** 2026-10-18 · **Base:** `8100e45` (r21z, ponta de `origin/main`) · **Escopo:** fila r21z — **fantasmas P3**: (A) os 4 ações Adm360 classificadas como "zero chamadores" em R21 com decisão ligar × remover (insumo D2/r21a0 favorecia ligar) e (B) o fantasma legado `Ocorrencias → Pendencias?module=OCORRENCIAS` herdado da R19. Fora de escopo e seguidos na fila R23: FecharCaixa F14 + F-R15-C, quirk de sessão portal gestor (observação aberta 1 da R19).

## Achados

### F-R22-A — Os 4 "fantasmas" Adm360 são gaps funcionais com máquina de estados completa (ligar é viável)

Guardas lidas na fonte da Infrastructure (transições reais que a UI deve espelhar):

| Ação | Situações de origem (guarda do repositório) | Transição | Efeito colateral |
|---|---|---|---|
| `OrcamentoRejeitar` | `RASCUNHO` ou `ENVIADO` (`OrcamentoCirurgicoRepository.RejeitarAsync` L448-453: `WHERE ... situacao IN ('RASCUNHO','ENVIADO')`; motivo obrigatório L445) | → `REJEITADO` | appenda `Rejeição: <motivo>` nas observações, `versao+1` |
| `OrcamentoCancelar` | qualquer situação exceto `CANCELADO` (`CancelarAsync` L466-471: `WHERE ... situacao <> 'CANCELADO'`; motivo obrigatório L460) | → `CANCELADO` | mesma transação: cancela reservas `ATIVA` do orçamento (L476-480) |
| `ValeCancelar` | exceto `CANCELADO` (idempotente, L935) e bloqueado em `EXPEDIDO`/`RETORNO_PARCIAL`/`RECONCILIADO` (`ValeConsignacaoRepository.CancelarAsync` L937-940 — "utilize consumo, devolução e reconciliação"; motivo obrigatório L922) | → `CANCELADO` | cancela tarefas móveis pendentes (`adm360_tarefas_coleta` L953-956) |
| `OrcamentoCancelarReserva` | somente reserva `ATIVA` (`OrcamentoCirurgicoRepository.CancelarReservaAsync` L703-711: `WHERE ... situacao = 'ATIVA'`) | reserva → `CANCELADA` | liberação de saldo (via UPDATE da reserva) |

UX derivada das guardas: botão **Rejeitar** visível em `RASCUNHO|ENVIADO`; **Cancelar Orçamento** em `RASCUNHO|ENVIADO|APROVADO` (estados terminais `REJEITADO/CANCELADO` sem ação); **Cancelar Vale** em `RASCUNHO|EM_SEPARACAO|PRONTO_PARA_EXPEDICAO`. As 3 primeiras mapeiam diretamente para as views de detalhe existentes (`OrcamentoDetalhes.cshtml`, `ValeDetalhes.cshtml`) seguindo o padrão de casa já consolidado no próprio módulo: botão no cabeçalho + modal com efeito e `<textarea name="motivo" required>` (modelo: `CirurgiaDetalhes.cshtml` L138-165, `CirurgiaCancelar`).

i18n: mensagens BFF já usam `UiText.L` c/ keys resx presentes desde R8 (`Adm360.Orcamentos.OkRejected/OkCancelled/FallbackReject/FallbackCancel/FallbackCancelReservation`, `Adm360.Vales.OkCancelled/FallbackCancel`) — **nenhuma key nova**. Os rótulos dos botões seguem o estilo local das telas de Suprimentos (literais pt-BR na view, como `Aprovar Orçamento`/`Confirmar Cancelamento` vizinhos — área pré-conversão i18n; converte-la é item de outra rodada).

### F-R22-B — `OrcamentoCancelarReserva` não dá para ligar com a superfície atual: precisa de extensão de dados

A view `OrcamentoReserva.cshtml` (model `PlanejamentoReservaOrcamentoViewModel`) lista **lotes elegíveis** por produto, mas a consulta de planejamento (`OrcamentoCirurgicoRepository.ObterPlanejamentoReservaAsync` L489-528) só agrega `SUM(quantidade)` das reservas ativas por item — **não expõe reservas individuais** (id, lote, quantidade, data). Sem `reservaId` visível, o botão por-reserva não existe onde assentar. Extensão mínima necessária: repositório (+SELECT das reservas ativas), DTO Application `PlanejamentoReservaOrcamento` (+lista aninhada), espelho Web c/ grafia idêntica (ADR-0003; `R20BffMappingContratoTests` cobre recursivamente os novos props) e seção de tabela na view c/ modal de motivo.

### F-R22-C — O "fantasma legado" `Pendencias?module=OCORRENCIAS` da R19 é **falso positivo** (parâmetro vivo de ponta a ponta)

Trace completo executado nesta auditoria — o parâmetro existe e filtra:

1. `OcorrenciasController.Index` sem `id` → `RedirectToAction("Index","Pendencias",{module="OCORRENCIAS"})` (`Controllers/OcorrenciasController.cs` L19);
2. `PendenciasController.Index([FromQuery] ProductivityQueryViewModel query)` — `ProductivityQueryViewModel.Module` **existe** (`Models/ProductivityViewModels.cs` L89);
3. `ProductivityWebService.BuildQuery` envia `["module"] = query.Module` (`Services/ProductivityWebService.cs` L112) p/ `api/produtividade`;
4. Api: `ProductivityActionController.List([FromQuery] ProductivityQuery query)` — `ProductivityQuery.Module` existe (`ProductivityActionModels.cs` L18-22);
5. SQL filtra: `... where (@priority is null or Priority=@priority) and (@module is null or Module=@module)` (`ProductivityActionServices.cs` L143) com `Normalize()` (trim+upper L298);
6. CTE de ocorrências emite `'OCORRENCIAS' as Module` (ibid. L116).

Conclusão: o redirect (e o "voltar" de `Views/Ocorrencias/Index.cshtml` L4, mesmo alvo) entrega a Central de Ações **filtrada por OCORRENCIAS**, como prometido. O diagnóstico r19z ("parâmetro inexistente na rota de destino") ficou curto — fechando como falso positivo, **sem mudança de código**, e atualizando o registro.

## Decisões

- **D-R22-1 (r22b1)** — ligar os 3 botões diretos: `OrcamentoRejeitar` e `OrcamentoCancelar` em `OrcamentoDetalhes.cshtml` (cabeçalho + modais com motivo, guardas de situação como F-R22-A) e `ValeCancelar` em `ValeDetalhes.cshtml`. Sair do set `AcoesFantasmaSemChamador` no mesmo commit (o fato auto-verificador R21 exige); o scan principal assume o pareamento (pares 79→82, parâmetros 203→207, ações 76 — pisos intactos).
- **D-R22-2 (r22b2)** — extensão de superfície p/ `OrcamentoCancelarReserva`: reservar ativa listada no planejamento (repositório + DTO Application + espelho Web grafia idêntica + fato de regressão de banco seedando reserva) e botão cancelar por-reserva com modal de motivo em `OrcamentoReserva.cshtml`. Sai do set de fantasmas; piso do contrato BFF segue verde (props novos nos dois lados).
- **D-R22-3** — fechar `Pendencias?module=OCORRENCIAS` como falso positivo com o trace de F-R22-C (sem código).
- **D-R22-4** — após r22b1+r22b2, `AcoesFantasmaSemChamador` fica **vazio por design** (set mantido c/ piso ≥0 e comentário; qualquer fantasma novo entra com classificação r21a0). Antivacuidade inversa: o scan principal + o fato auto-verificador continuam cobrindo entradas novas.

## Plano de slices

| Slice | Conteúdo | Gate / evidência |
|---|---|---|
| r22a0 (este doc) | auditoria (F-R22-A/B/C) + decisões D-R22-1..4 | este arquivo |
| r22b1 | 3 botões + modais (Rejeitar/Cancelar orçamento, Cancelar vale) + sets do scanner atualizados | gate build `--no-incremental` 0E/multiset; suíte verde |
| r22b2 | extensão `reserva-planejamento` + botão por-reserva + fato de banco | gate + suíte + contrato BFF verde |
| r22z | relatório consolidado + push (credencial wincred) | gates finais |
