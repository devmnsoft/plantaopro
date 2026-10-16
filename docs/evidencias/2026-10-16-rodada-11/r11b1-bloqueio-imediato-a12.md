# Rodada 11 — r11b1: Bloqueio imediato de saldo ao abrir ocorrência (A12, P1 da fila R10)

- **Data:** 2026-10-16 · **Base:** `0c97b95` (r11a0) · **Slice:** `r11b1` · Banco de teste `plantaopro_test` (PG18).
- **Regra:** sem push nesta fatia (autorização só ao final da rodada).

## 1. Mandato (spec com evidência)

- `PlantaoPro_Administrativo360_MVP_Requisitos_Prompts.md` **A12 L112**: "Aberta → em tratamento → resolvida → encerrada. **Bloqueio imediato autorizado pode abranger saldo interno e externo.** Encerrar exige destino dos itens e justificativa. Registro de ocorrência não apaga estoque."
- **Regra global L296**: "**Lote bloqueado/vencido não pode ser reservado ou expedido.** Mostre FEFO, quantidade disponível, validade e **motivo do bloqueio**. Não edite saldo diretamente."

Estado pré-fatia (auditoria r11a0 §1): o mecanismo de bloqueio existia no DDL (`condicao='BLOQUEADO'` válido) e na view `adm360_saldos` (saldo por condição), mas **nenhum writer em produção** gravava a condição BLOQUEADO — o lote nunca era efetivamente bloqueado ao abrir uma ocorrência.

## 2. Decisões implementadas (plano r11a0 §2 + ajustes de implementação documentados)

| # | Decisão | Observação |
|---|---------|-----------|
| 1 | **Momento:** bloqueio no ABRIR, na MESMA transação do INSERT da ocorrência (serializável + retry 40001/40P01 da base). O bloqueio persiste durante EM_TRATAMENTO. | "bloqueio imediato". |
| 2 | **Alcance:** cobre até `quantidade` a partir do `disponivel` (condição LIBERADO, sem vencer) do lote em **todos os locais ativos** (INTERNO e EXTERNO) — o que "pode abranger saldo interno e externo" autoriza. Ordem determinística: local da ocorrência primeiro, demais por `(codigo, id)`. Saldo total < quantidade ⇒ **bloqueio parcial** (a abertura não é rejeitada). | Ajuste documentado vs r11a0 §2.2: ordenação por `(codigo, id)` (id como desempate, pois unicidade de `codigo` por tenant não está garantida no DDL). |
| 3 | **Liberação no RESOLVER:** espelha exatamente as pernas registradas ao abrir (lidas dos movimentos por `origem_tipo='OCORRENCIA'`, `tipo='BLOQUEIO_OCORRENCIA'`, `condicao='BLOQUEADO'` — nada recalculado). TRATAR e ENCERRAR não geram pernas. Consequência: todo ciclo ABERTA→…→ENCERRADA termina net-zero ⇒ "não apaga estoque" vale para o caminho COM bloqueio. | Caveat: se inventário ajustar a quantidade bloqueada entre abrir/resolver, a liberação espelha o registrado (fluxo simétrico; divergência física é papel do inventário). |
| 4 | **Modelagem (descoberta-chave da fatia):** a view `adm360_saldos` agrupa por **(lote, local, CONDIÇÃO)** — cada mudança de condição é escrita como **duas pernas que somam zero na soma física** (débito na condição de saída + crédito na de entrada), idioma já usado em `QualidadeRepository` (inspeção) e `EstoqueRepository` (transferência). Bloquear = `LIBERADO -q` / `BLOQUEADO +q`; liberar = inverso. | Sem DDL novo — colunas existentes cobrem tudo. |
| 5 | **Identidade/auditoria:** pernas com `origem_tipo='OCORRENCIA'`, `origem_id=<ocorrencia_id>`, `motivo` citando tipo/descrição/id da ocorrência (L296 — motivo consultável); tipos `BLOQUEIO_OCORRENCIA`/`LIBERACAO_OCORRENCIA`; chaves idempotência determinísticas `{ocorrencia_id}:{BLQ\|DEL}:{local_id}:{PERNA}` (unique tenant+key protege contra reenvio). | Ajuste documentado vs r11a0 §2.4: chave por `local_id` (não código) + sufixo de perna (dois INSERTs por local). |
| 6 | **Trancas:** advisory locks determinísticos `{tenant}:{produto}:{lote}:{local}` (mesma forma das chaves de reserva/transferência ⇒ abre/trava/resolve serializam corretamente) + `ValidarBloqueioInventarioAsync` por local tocado + releitura do saldo sob lock (padrão `ReservarAsync`). | Inventário ativo num local candidato ⇒ abertura falha fail-closed (mesmo contrato da transferência). |
| 7 | **API/Web:** rotas e contratos invariantes (as 4 endpoints do r10b1; `IOcorrenciasRepository` sem mudança de assinatura). O saldo passa a refletir o bloqueio por derivação da view — sem novo endpoint, sem nova tela nesta fatia. | |

## 3. Mudanças

| Arquivo | Mudança |
|---|---|
| `backend/PlantaoPro.Infrastructure/Administrativo360/OcorrenciasRepository.cs` | `AbrirAsync` ganha `BloquearSaldoImediatoAsync` (candidatos → locks → inventário → pernas, na mesma transação); `TransicionarAsync` ganha `usuarioId` e, no RESOLVIDA, chama `LiberarSaldoBloqueadoAsync`; novo helper compartilhado `EscreverPernasMudancaCondicaoAsync` (duas pernas, chave por perna). Comentários de classe atualizados. |
| `backend/PlantaoPro.Tests/Administrativo360OcorrenciasA12Tests.cs` | Fato antigo renomeado para o caso que ele agora cobre (`A12_SemSaldoLiberado_CicloCompleto_NaoGeraPernasDeBloqueio` — saldo só em QUARENTENA ⇒ zero pernas); +7 fatos novos: (1) pernas com origem/motivo e queda de disponível; (2) reserva acima do disponível falha com ABERTA e EM_TRATAMENTO; (3) multi-local INTERNO+EXTERNO em ordem determinística; (4) bloqueio parcial sem rejeitar; (5) RESOLVER inverte + ciclo net-zero + reserva volta a ser possível; (6) TRATAR/ENCERRAR sem pernas; (7) escopo tenant. Helpers de saldo/pernas adicionados. |

Sem DDL, sem resx/i18n (nenhuma literal nova de UI — mensagens/`motivo` seguem o padrão textual já existente dos writers Adm360), sem API, sem Web.

## 4. Evidência executada

- **Build Release sln completa:** EXIT 0, `0 Erro(s)` (`r11b1_build.log`). Nota de processo: primeira tentativa falhou com CS0246 (`NpgsqlConnection`/`NpgsqlTransaction` sem `using Npgsql;` nos novos helpers do repository) — corrigido e rebuild limpo antes de qualquer execução.
- **Classe isolada `Administrativo360OcorrenciasA12Tests`:** **24/24 aprovados, TEST_EXIT 0** (`r11b1_a12_iso.log`) — 17 fatos do r10b1 (sem regressão; o invariante de estoque foi renomeado para o caso que cobre) + 7 fatos novos do bloqueio imediato.
- **Suíte full Release:** `Total de testes: 1428`, `Aprovados: 1428`, **TEST_EXIT 0 — limpo** (`r11_baseline_suite.log`) — 1421 da base R10 + 7 fatos novos desta fatia; nenhum flake do catálogo ① apareceu neste run; a linha `Falha ao registrar auditoria legada` no log é o registro não-fatal conhecido dos runs verdes.

## 5. Riscos/caveats registrados

- **Perfis de falha conhecidos (catálogo ①):** `ProvisionamentoB5Tests` e `R7OBOnboardingPreferenciasIdiomaTests` podem flakear 40P01 em teardown sob RAM baixa; receita = rerun isolado 5×.
- **Exposição visual:** o motivo do bloqueio está nos movimentos (consultável via saldo/movimentações existentes). Se a homologação apontar falta de visibilidade na tela de ocorrências, registrar como pendência visual (não inventar tela nesta fatia).
- **P1b (fila):** baixa/devolução dirigida por `destino` no encerramento (destino = descarte, por exemplo, ainda não consome fisicamente o saldo — permanece em BLOQUEADO até ajuste de inventário ou comando futuro). Fora desta máquina estrita.
