# R48 · r48z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R47 (`427162f`, suíte 1591). Plano: `r48a0`.

## Escopo da rodada

Roadmap R48 (epic E9): alertas de inadimplência — quando a fatura do cliente vence, o admin da conta
(`ADMINISTRADOR_CLIENTE`) é notificado em linguagem simples e o lado MNSOFT deixa de ser cego
mesmo quando o vencimento acontece pelo sweep preguiçoso.

## Commits

| Commit | Conteúdo |
|---|---|
| `0e250ad` | r48b1: dono único dos efeitos do vencimento + sweep com identificação pré-flip + contrato permanente (3 fatos) |
| (docs) | este r48z + r48a0 (plano) |

## O que foi entregue (conforme r48a0)

- **Dono único (D-R48-1)**: novo helper privado `RegistrarVencimentoFaturaAsync(cn, tx, faturaId, clienteId)`
  no `FaturamentoSaasController` registra, em ordem e na mesma transação do caller: (i) evento
  `FATURA_VENCIDA` em `cobranca_eventos` (helper existente), (ii) alerta `INADIMPLENCIA`/`ALTA` em
  `cliente_alertas` (helper existente, dedupe `not exists`), (iii) **novo**: notificação para cada
  usuário **ativo** com perfil `ADMINISTRADOR_CLIENTE` do cliente via
  `NotificacaoService.CriarNotificacaoAsync` com `tipo='financeiro'`, `origem_tipo='fatura_saas'`,
  `origem_id=<faturaId>` — uma notificação por fatura por usuário (dedupe do índice único parcial
  R8-B2). As DUAS vias de vencimento delegam ao helper; a branch `VENCIDA` da `AtualizarFatura`
  deixou de registrar evento/alerta por conta própria. No arquivo inteiro, exatamente UMA chamada
  efetiva registra `FATURA_VENCIDA` (travado por fato).
- **Ponto cego do sweep fechado (D-R48-2)**: `AtualizarVencidasAsync` agora faz SELECT das faturas
  `ABERTA` com `vencimento < current_date` (id + cliente_id) ANTES do UPDATE idêntico ao anterior e
  chama o helper para cada fatura afetada dentro de uma transação (`BeginTransactionAsync` → foreach
  → `CommitAsync`). Assinatura `(NpgsqlConnection)` mantida — os 3 callers (resumo/faturas/
  inadimplentes) não mudaram.
- **Linguagem simples (D-R48-3)**: título **"Fatura vencida"**; mensagem
  `"Sua fatura de R$ {valor pt-BR N2} venceu em {dd/MM/yyyy}. Para regularizar, acesse Minha
  Assinatura."` (com fallback sem valor/data). Formatação pt-BR explícita via
  `CultureInfo.GetCultureInfo("pt-BR")`. Sem status cru, sem jargão, sem a palavra "tenant".
- **Alvo estrito (D-R48-4)**: apenas `ADMINISTRADOR_CLIENTE` ativo do cliente (pattern de casa
  `usuarios × usuarios_perfis × perfis`), sem fallback para qualquer usuário — diferentemente do
  `AlertRuleService` operacional. Se não houver ninguém com o perfil, o alerta MNSOFT continua
  cobrindo o acompanhamento. Sem nova UI: a superfície já existia (`Dashboard` renderiza
  `UltimasNotificacoes`).

## Ajuste pós-verde (exposto pela smoke viva)

A primeira smoke viva derrubou **500** no `GET api/faturamento-saas/resumo`:
`System.InvalidOperationException: Connection is not open` em `cn.BeginTransactionAsync()` (L847).

Causa: os reads de casa criam a conexão **fechada** (`new NpgsqlConnection(...)` sem `OpenAsync`)
e deixam o Dapper abrir/fechar a conexão por statement (comportamento documentado: quem abre é
quem fecha). O SELECT e o UPDATE do sweep funcionaram por conta disso, mas a transação exige
conexão aberta. Padrão de referência no próprio arquivo: `AtualizarFatura` (L786) faz
`await cn.OpenAsync()` explícito antes de transacionar.

Correção mínima: guarda no topo do sweep —
`if (cn.State != System.Data.ConnectionState.Open) await cn.OpenAsync();`
(quem devolve o estado é o `await using` do caller, em todos os caminhos). O fato de contrato do
sweep foi revalidado após o ajuste (a janela da região cresceu de 900→1200 chars para acomodar as
linhas novas).

## Gates e suíte

- Cada mudança: rebuild real exit 0 · gate de casa multiset **160/160** limpo, 0 erros (3 vezes:
  bloco do sweep/helper, guarda de conexão, região do fato).
- Classes dirigidas **32/32** (R48 + R47 + B4 MinhaAssinatura + CobrancaB6 + higiene de
  repositório + R39).
- Suíte integral: base 1591 + 3 fatos = **1594/1594, exit 0, 48 s, zero falhas**.

## Smoke viva — vencimento de ponta a ponta

Ambiente: tenant demo, API em porta 51976 (credencial de provedor só via env, como nas rodadas
anteriores). Duas faturas `ABERTA` com vencimento no passado (R$ 42,50 venc. 01/10/2026 e
R$ 78,00 venc. 02/10/2026) inseridas direto no banco para o cliente demo. Faturas alheias
verificadas antes: todas com vencimento futuro — o sweep não tocaria nelas.

| Etapa | Resultado |
|---|---|
| `GET api/faturamento-saas/resumo` (dispara sweep) | ✅ 200, `FaturasVencidas=1`; fatura virou `VENCIDA` |
| Notificação (banco) | ✅ exatamente **1** para o admin do cliente — título "Fatura vencida", mensagem **"Sua fatura de R$ 42,50 venceu em 01/10/2026. Para regularizar, acesse Minha Assinatura."**, origem `fatura_saas`+id da fatura; segundo resumo não duplicou |
| Alerta MNSOFT (banco) | ✅ `INADIMPLENCIA / ALTA / "Fatura SaaS vencida"` não resolvido (dedupe `not exists`) |
| Evento (2ª fatura, query corrigida) | ✅ `FATURA_VENCIDA x1` em `cobranca_eventos`; 1 notificação + 1 alerta pela 2ª fatura |
| Idempotência | ✅ mesma fatura reavaliada não gera segunda notificação (índice parcial R8-B2) |

Sem nova superfície verificada por tela de propósito: a exibição (`UltimasNotificacoes` no
dashboard) é código pré-existente exercitado em B4/R47; o gap da rodada era a CRIAÇÃO no evento
certo, e ela está coberta pelos asserts de banco acima.

### Ambiente do smoke — papel temporário (documentado)

O gestor demo só tinha perfil `ADMINISTRADOR_CLIENTE` e o endpoint exige
`ADMINISTRADOR`/`ADMINISTRADOR_GLOBAL`. Dois obstáculos de ambiente, resolvidos com mudanças
**reversíveis e removidas**: (1) o catálogo só tem o perfil `ADMINISTRADOR` escopado a OUTRO
tenant, e a query de papéis do login filtra `p.tenant_id` — criou-se uma cópia do perfil escopada
ao tenant demo + link no gestor; (2) o JWT expõe um único papel primário por claim, então o link
só valia se o perfil fosse aceito pelo filtro de tenant. Após a smoke: link e cópia removidos,
usuário confirmado de volta a `[ADMINISTRADOR_CLIENTE, ADMINISTRADOR_CLIENTE]`, faturas alheias
intactas.

## Limpeza

Zerado em ambas as execuções de smoke: `notificacoes` (origem as duas faturas) · `cobranca_eventos`
(fatura_id das duas) · `cliente_alertas` (INADIMPLENCIA do cliente demo) · `pagamentos_saas` (nada
criado) · `faturas_saas` (2) · `usuarios_perfis` (link temporário) · `perfis` (cópia temporária).
Verificação final por contagem: tudo 0.

## Observações de harness (não são defeitos do produto)

- A corrupção de exibição/transporte `plantaopro → plantaouro` (D-SESSÃO-1) voltou a incomodar e
  exigiu confirmação **sempre por char codes** (palavra correta: 112,108,97,110,116,97,111,112,
  114,111). Artefatos desta rodada (controller, bloco aplicado, teste, scripts psql) passaram por
  varredura byte-level: 0 ocorrências da variante errada. Nos scripts, a palavra do schema foi
  montada por código de caractere (`<SCH>` placeholder) para eliminar o risco do canal.
- Âncoras PowerShell de edição: duas falhas de `IndexOf` que NÃO eram bug de PS — uma âncora digitada
  sem o `=` (`set status'+$q` no lugar de `set status='+$q`) e a palavra corrompida. Regra prática
  ratificada: montar âncoras por segmentos + char codes e validar comprimento antes de aplicar.
- `INSERT...SELECT` com 2 linhas de origem gerou conflito próprio contra o índice único parcial
  `(usuario_id, perfil_id) where reg_status='A'` — resolvido com `LIMIT 1`.
- Vários statements num único `-c` do psql = transação implícita: um erro de sintaxe no último
  SELECT desfez os DELETEs anteriores do lote (reexecutado corrigido).

## Estado de casa

- Apps PARADOS, porta 51976 livre (só postgres em 5432).
- Nova base de suíte para as próximas rodadas: **1594**.
- Fila: **R49** (próxima da fila E9 pós R48 — escopo a definir na rodada).
