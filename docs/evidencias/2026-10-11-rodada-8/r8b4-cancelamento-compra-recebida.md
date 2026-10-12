# R8-B4 — Cancelamento de pedido de compra com compensação de efeitos

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia de bug/policy #4
- **Data:** 2026-10-12
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (roteiro `r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica)

Varredura `r8a0-varredura-aprovacoes-precos-cancelamento.md`, Área C (cancelamento, §21):

> C3 — Operações compensatórias ao cancelar: **parcial**. "Compra não tem edição de itens nem cancelamento (C3)".

Matriz de estado `r8a0-auditoria-inicial-matriz-estado.md`, linha "Compensação ao cancelar":

> "**compra recebida sem compensação automática nem endpoint de cancelamento de pedido** | parcial | cancelamento de compra recebida inexistente (matriz §30) | fatia §21 | novo teste".

Fatos do domínio que sustentam o diagnóstico:

- `ComprasRepository` só expunha Listar/Criar/Aprovar/Receber — nenhum caminho produzia o estado `CANCELADO`, ainda que ele já fosse válido no check de `adm360_pedidos.situacao` (v2191).
- O recebimento produz efeitos concretos: lote (upsert SEM-LOTE quando o produto não controla lote), movimentos `ENTRADA` (`origem RECEBIMENTO`), `adm360_recebimento_itens` e título em `adm360_titulos_pagar` (`origem RECEBIMENTO_COMPRA`).
- Consequência: um pedido PARCIAL/RECEBIDO não tinha como ser desfeito — o estoque e o título ficavam pendurados sem operação que os revertesse (só ajuste manual).

## 2. Decisões (sem regras inventadas)

1. **Política de cancelamento do próprio módulo** (precedentes vale consignação / inventário / venda): motivo obrigatório; transação SERIALIZABLE via `ExecutarComRetrySerializableAsync` (retry 40001/40P01); `FOR UPDATE` no pedido; advisory locks deterministas por `(tenant, produto, lote, local)`; idempotência por estado — já `CANCELADO` vira no-op (precedente `ValeConsignacaoRepository.CancelarAsync`).
2. **Escopo por estado, sem regra nova:** RASCUNHO/APROVADO cancelam direto (nenhum efeito produzido ainda); PARCIAL/RECEBIDO só cancelam quando seguro (gates abaixo).
3. **Gate de estoque numérico, por grupo `(produto, lote, local, condicao)`**: exigir saldo físico integral do grupo da linha recebida (≥ quantidade recebida). Uma única regra cobre consumo (`SAIDA`), transferência (`TRANSFERENCIA_*`) e rota de qualidade (`INSPECAO`), porque toda saída do grupo reduz a soma dos movimentos. Se algo saiu do grupo → 409 fundamentado exigindo ajuste manual de estoque — sem estorno/SAIDA fictícia.
4. **Gate de reserva apenas nas linhas LIBERADO** (é o único grupo onde `disponível = físico − reservado` se aplica): após a saída compensatória a reserva deve continuar coberta (`saldo − qtd ≥ reservado`); senão 409 "libere as reservas". Invariante vinda de `Estoque.Disponivel` ("Reservas não podem superar o saldo físico liberado").
5. **Títulos com pagamento exigem estorno prévio** (padrão do módulo: venda paga exige estorno antes de cancelar): qualquer título do pedido com `valor_pago > 0` ou situação `PARCIAL`/`PAGO` → 409 "estorne os pagamentos antes de cancelar". Sem pagamento → título `CANCELADO`, `saldo_aberto = 0`, trilha em `observacoes`.
6. **Compensação de estoque**: uma `SAIDA` por linha recebida, quantidade negativa, **mesma condição de origem** (a view `adm360_saldos` agrupa por condição — espelhar mantém o grupo exato), `origem_tipo='CANCELAMENTO_COMPRA'` (nova, dentro do varchar(30)), chave de idempotência determinística `CANC_PEDIDO:{pedido:N}:{linha:N}`.
7. **Auditoria**: migration **v2347** adiciona `motivo_cancelamento`, `cancelado_em`, `cancelado_por` em `adm360_pedidos`; evento `CANCELAMENTO_RECEBIMENTO` nos documentos fiscais vinculados aos recebimentos (quando existirem); `pedido_itens.quantidade_recebida` preservada como histórico.
8. **Contrato público mínimo**: novo endpoint `POST api/administrativo360/compras/{id}/cancelar` com `[FromBody] string motivo` (mesma binding do cancelamento de vendas, consumido pela Web via `SendApiAsync<string>`); 409 `{message}` para exceção de negócio (padrão C1 "server-side bloqueia, não apresenta"), 400 para motivo ausente. Ação Web `CancelarPedidoCompra` + botão/modal por linha na view `PedidosCompra` (motivo obrigatório, efeitos explicados antes de confirmar). i18n: **+3 chaves** `Adm360.Compras.*` nos 4 idiomas — contrato F6 atualizado de **241 → 244** chaves fixas.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `database/migrations/2026_10_v2347_r8b4_cancelamento_compras.sql` | Nova: colunas de auditoria do cancelamento em `adm360_pedidos` (`add column if not exists`). |
| `backend/PlantaoPro.Application/Administrativo360/Contracts.cs` | `IComprasRepository`: novo `Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid pedidoId, string motivo, CancellationToken ct)`. |
| `backend/PlantaoPro.Infrastructure/Administrativo360/ComprasRepository.cs` | `CancelarAsync`: locks, gates (estoque/reserva/título) e compensação (movimento, título, evento de documento, atualização do pedido) numa única transação serializável com retry. Consulta de linhas ancorada em subconsulta por pedido — nunca `ANY(@array)` vazio (Dapper geraria cláusula inválida para RASCUNHO/APROVADO). |
| `backend/PlantaoPro.Api/Controllers/Adm360SuprimentosControllers.cs` | `Compras360Controller`: endpoint `POST {id:guid}/cancelar` (`ArgumentException`→400, `Administrativo360BusinessException`→409). |
| `backend/PlantaoPro.Web/Controllers/Administrativo360Controller.ComprasRecebimentos.cs` | Ação `CancelarPedidoCompra(Guid id, string motivo)`: validação de motivo, chamada à API, `TempData` sucesso/erro com as novas chaves. |
| `backend/PlantaoPro.Web/Views/Administrativo360/PedidosCompra.cshtml` | Botão "Cancelar" por linha (exceto CANCELADO) + modal com motivo obrigatório e explicação dos efeitos/bloqueios. |
| `backend/PlantaoPro.Web/Localization/UiTexts.resx` + `.en` + `.es` + `.fr` | +3 chaves: `Adm360.Compras.ErrCancelMotivo`, `Adm360.Compras.OkOrderCancelled`, `Adm360.Compras.FallbackCancelOrder`. |
| `backend/PlantaoPro.Tests/R7DF6Admin360I18nContratoTests.cs` | Contagem fixa Adm360 atualizada 241→244 (fact renomeado em consequência). |
| `backend/PlantaoPro.Tests/R8B4CancelamentoCompraRecebidaTests.cs` | Nova: 8 fatos (recebido+compensação+idempotência; consumo bloqueia; título pago bloqueia; parcial compensa só o recebido; rascunho sem efeitos trava aprovar; reserva ativa bloqueia; motivo vazio; contrato de fonte/endpoint/web/i18n). Aplica a migration v2347 sozinha (padrão R8-B2: advisory lock + probe por coluna). |

Detalhe implementativo relevante: os reads dinâmicos do repositório usam **nomes crus snake_case** das colunas — apelidos camelCase são dobrados para minúsculo pelo PostgreSQL e o acesso dinâmico do Dapper é case-sensitive (nulo silencioso caso contrário); mesmo padrão dos demais reads do módulo.

## 4. Evidência (executada)

- **Alvo:** `R8B4*` + `R7DF6*` = **20/20 aprovados** (8 novos + 12 do contrato i18n atualizado) — build27: `BUILDTESTS27_EXIT:0`, `RUNR8B4_EXIT:0`.
- **Higiene do repositório:** contrato `Repositorio_NaoDeveConterTermosDoProdutoIncorretoEmArquivosVersionaveis` → EXIT=0 na árvore final (código + docs + migration).
- **Gate phaseA v2** (contratos R7E1/R7A1/R7OB + login/refresh, 52): **EXIT=0**.
- **Full ×3 (pipeline v2):** total agora **1298** (1290 + 8 novos).
  - `full2`: **1298/1298 verde**, EXIT=0.
  - `full1`: 1 falha — `SaasComercialB4MatrizTests.B3_DecidirDowngrade_EmSolicitacaoDeCancelamento_409` → `Npgsql.PostgresException 40P01: impasse detectado` (classe não relacionada à fatia, já catalogada em ①).
  - `full3`: 1 falha — `R7OBOnboardingPreferenciasIdiomaTests.O1_Iniciar_MaterializaEtapaDeIdioma_OpcionalNoNucleo_Idempotente` → mesmo `40P01` (também catalogada em ①).
  - Nota: um primeiro pipeline (cancelado por restart do servidor de execução) exibiu a mesma assinatura em outras duas classes catalogadas (`SaasGovernancaB6Rodada4Tests.G1b_…`, `ProvisionamentoB5Tests.P4_…`) — reforço do padrão, não contado como registro.
- **Prova de flake (categoria ①):** assinatura exata do catálogo — `40P01: impasse detectado` sob carga paralela da suíte inteira, testes diferentes a cada run, sempre em classes não relacionadas à fatia; RAM livre ~0,9 GB no início do pipeline. Isolate-rerun das duas classes do registro limpo: **17/17 aprovados** (isolate-r8b4.log). Nenhuma classe nova entra no catálogo nesta rodada (as 4 catalogadas foram as observadas em ambas as execuções).

## 5. Dívida registrada (fora do escopo desta fatia)

1. Linha com inspeção decidida cai no mesmo gate numérico do consumo — a mensagem é genérica ("consumida, transferida ou com destino de qualidade definido"); refinamento de mensagem/rotulagem específica pode vir depois.
2. Não há estorno automático de pagamento de título — por decisão (exige estorno prévio); o fluxo de estorno é o do módulo de contas a pagar (`ContasPagarRepository`).
3. `adm360_ocorrencias` não tem writer ativo em produção (só DDL) — nada a gatear por enquanto.
4. `pedido_itens.quantidade_recebida` permanece preenchido após o cancelamento (histórico); qualquer agregado que some quantidades por situação precisa excluir `CANCELADO` (nenhum impacto localizado neste corte — conferir em homologação se houver relatório de compras).
5. Base principal em v2339: o lote de deploy passa a incluir **v2340–v2347**.
6. Categoria ① de flake (40P01 sob carga da suíte inteira) continua aberta como item de infraestrutura — observada novamente no pipeline desta fatia nas classes catalogadas (provada transiente por isolate 17/17); nenhuma classe nova entrou no catálogo.

## 6. Fila seguinte (R8)

Depois do cancelamento (próxima fatia), pela ordem quebrado → alto impacto de `r8a0` §4: `medico_especialidades` sem write path em produção; ausência de mapeamento usuário×unidade; granularidade de glosa por conta/título; merge de paciente ausente; importações sem prévia; 5 calculadores de KPI sobrepostos; `cid_tabela` duplicada em 6 lugares. (A duplicação Usuarios/Perfis na sidebar é intencional, documentada.)
