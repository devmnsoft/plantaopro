# R47 · r47z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R46 (`39070b5`, suíte 1587). Plano: `r47a0`.

## Escopo da rodada

Roadmap R47 (epic E9): fluxo completo de faturamento/cobrança do lado do cliente — o cliente vê as
faturas da própria assinatura (unindo as duas fontes do sistema), paga a fatura mensal pela tela
self-service e cai no checkout público do sandbox, sem depender de ninguém.

## Commits

| Commit | Conteúdo |
|---|---|
| `60b6a0c` | docs: r41z/r43z retroativos + r46z consolidado + r47a0 (plano E9 faturamento/cobrança) |
| `ed595c6` | r47b1: faturamento/cobrança self-service (D-R47-1..4) + contrato permanente (4 fatos) |
| `b1a9792` | r47fix: cast `::timestamptz` em colunas `date` das consultas de minha assinatura + asserts permanentes no contrato |
| (docs) | este r47z |

## O que foi entregue (conforme r47a0)

- **Listagem unificada (D-R47-1)**: `GET api/minha-assinatura/faturas` junta
  `faturas_saas` (motor de cobrança) e `cadastro_cliente_pagamentos_iniciais` (pagamentos de cadastro),
  escopado por `cliente_id` do contexto; cada linha marcada com origem (`SAAS`/`CADASTRO`) no DTO
  existente (+campo `Origem`). Mesma rota, sem novo endpoint de listagem.
- **Ação "Pagar agora" (D-R47-2)**: existe só para fatura `SAAS` ABERTA/VENCIDA.
  `POST api/minha-assinatura/faturas/{faturaId}/cobrar` checa o dono na própria query
  (`where id=@faturaId and cliente_id=@clienteId`) e delega ao `CobrancaService.CobrarAsync`
  idempotente — zero cópia da regra de criação/provedor/eventos. Estados PAGA/CANCELADA/EM_CONTESTACAO
  devolvem resposta humana honesta (409 no conflito).
- **Autorização (D-R47-3)**: padrão da família MinhaAssinatura — `[Authorize]` de controlador na API
  + guard de rota na Web (`MinhaAssinatura/PagarFatura` = verbo `CRIAR` do módulo ASSINATURAS)
  + `[ValidateAntiForgeryToken]`.
- **Linguagem simples (D-R47-4)**: status vira rótulo humano na tela (Em aberto / Vencida / Paga /
  Cancelada / Em análise); o sucesso do POST devolve a URL do checkout via TempData e a tela oferece
  "Abrir página de pagamento" em nova aba (mesmo padrão do lado MNSOFT). Sem código cru nem a palavra
  "sandbox" na tela do cliente.

## Ajuste pós-verde (após `ed595c6`, exposto pela smoke viva)

A primeira smoke viva derrubou 500 no `GET /api/minha-assinatura/faturas`:
`System.Data.DataException: Error parsing column 6 (DataInicio=…) → System.InvalidCastException:
Object must implement IConvertible`.

O stack completo apontou o culpado: não era o novo UNION, era a **`MinhaAssinaturaAsync()`**
(endpoint B4) — `assinaturas.data_inicio`/`data_fim` são colunas `date` cruas mapeadas para
propriedades `DateTime?`. O Npgsql 10 devolve colunas `date` em tipo que o
`Convert.ChangeType` do Dapper não converte para `DateTime`. Era um **bug latente do B4**: aquele
endpoint nunca tinha sido exercitado com dados reais (os testes B4 usam HTTP fake sobre a API). O MESMO
risco existia na query nova da R47 (`vencimento date` → `Vencimento DateTime` nos dois ramos do UNION).

Correção: cast `::timestamptz` explícito nas 4 colunas dos SELECTs (`data_inicio`/`data_fim` da
assinatura + `vencimento` nos dois ramos do UNION) — zero mudança de DTO/contrato BFF. Padrão de
referência no próprio codebase: `CobrancaPublicaDto` já mapeia essas colunas `date` como `DateOnly`.
Fato permanente acrescido ao contrato R47 (asserts travando os 4 casts, com o contexto do incidente no
comentário).

## Gates e suíte

- r47b1: rebuild real exit 0 · gate de casa multiset **160/160** limpo · suíte integral **1591/1591,
  exit 0** (run realizado antes do ajuste — base válida para a comparação).
- pós-r47fix: rebuild real exit 0 · gate **160/160** limpo · classes dirigidas **29/29**
  (R47 + B4 MinhaAssinatura + CobrancaB6 + higiene de repositório + R39).
- Suíte integral pós-r47fix: base inalterada **1591** (nenhum fato novo — extensão de fato
  existente): **1591/1591, exit 0, 1m25s, zero falhas**.

## Smoke viva — fluxo E9 de ponta a ponta

Ambiente: tenant demo, provider SANDBOX ativo, credencial de provedor apenas via variável de ambiente
(não existe em appsettings versionado). Fatura demo `ABERTA` R$ 399,00 (vencimento 31/10/2026)
inserida direto no banco para o cliente demo.

| Etapa | Resultado |
|---|---|
| Login do gestor → `Minha Assinatura/Faturas` | ✅ linha com vencimento, badge **"Em aberto"** (rótulo humano; nenhum status cru no HTML) e botão **"Pagar agora"** |
| POST "Pagar agora" | ✅ tela com **"Abrir página de pagamento"** (nova aba) + URL do checkout `/api/cobranca/sandbox/SBX-a524…/pagina`; a cobrança `INICIADA` foi criada pelo motor (referência idempotente reutilizada entre tentativas — mesma `SBX-a524…` no banco) |
| GET do checkout (público) | ✅ nome do cliente "Santa Casa", valor "R$ 399,00", status INICIADA — leitura pública, sem autenticação |
| POST simular aprovado (sandbox) | ✅ motor confirmou: **cobranca=PAGA** e **fatura=PAGA** (`data_pagamento` registrada); a tela passou a exibir badge **"Paga"** e nenhum form de pagamento naquela linha |
| Segundo "cobrar" (API direta com JWT do cliente) | ✅ **409** com a mensagem honesta "Esta fatura já está paga." — nada duplicado |

Observações de harness (não são defeitos do produto): o `HttpClient` cru do PS 5.1 perde o
antiforgery em logins consecutivos (400 sem chegar no controller) — a smoke foi estabilizada com
`Invoke-WebRequest -WebSession`, padrão das rodadas anteriores. Documentado também: múltiplas
instruções num único `-c` do psql formam uma transação implícita (um erro FK durante a limpeza
desfez os `DELETE` anteriores do mesmo lote — resolvido executando por tabela).

## Limpeza

Dados da smoke removidos e zerados em 5 tabelas: `faturas_saas`(1) · `pagamentos_saas`(1) ·
`cobranca_cobrancas`(1) · `cobranca_eventos`(2) · `cobranca_webhook_eventos`(1).

## Estado de casa

- Apps PARADOS, portas 51976/52976 livres.
- Nova base de suíte para as próximas rodadas: **1591**.
- Fila: **R48 alertas de inadimplência** — fatura VENCIDA → notificar o admin do cliente (recon:
  módulo `Notificacoes` existente).
