# Rodada 23 — Relatório final consolidado (r23z)

- **Data:** 2026-10-18 · **Base de entrada:** `ec3d424` (r22z, ponta de `origin/main`) · **Diretriz:** "continuar pelas próximas 10 sprints (R19→R28) e avançar também o design do sistema" — push ao fim da rodada segue autorização genérica registrada em r19z.

## O que a rodada fechou

| Item | Severidade | Estado | Prova |
|---|---|---|---|
| **F14** — "modal de FecharCaixa sem justificativa" (achado r16z, herdado R17–R22) | P3 → **falso positivo** | Cadeia verificada de ponta a ponta: o campo `textarea name="justificativa"` ("Justificativa de Divergência") já existia no form em `8669951` (r16b2 — contemporâneo do próprio achado, ou seja, a observação estava defasada da superfície); BFF `FecharCaixa` binda `string? justificativa` e repassa ao payload (`Financeiro.cs` L517/L531); `CaixaRegras.ValidarFechamento` exige motivo apenas quando \|conferido − calculado\| > 0 (`FinanceiroVendas.cs` L192-197). O 400 da smoke R16 era o comportamento correto da regra com campo vazio (o smoke homologou fechar com diferença zero). Fechado sem código de produto | 2 fatos permanentes (`R23CaixaJustificativaEItemPedidoTests`): unit puro da regra + fato de fonte form→assinatura→payload |
| **F-R15-C** — form de Recebimentos não preenchia `pedidoItemId` (achado R15) | P3 → resolvido | Causa raiz: hidden input com `Guid.Empty` (sem seletor de item; os resumos de pedido não expunham itens; a API faz lookup `i.id = @id AND i.pedido_id = @PedidoId` → GUID vazio = 400 "Quantidade excede o saldo autorizado"). Extensão de superfície no padrão r22b2/ADR-0003: `ListarAsync` retorna `Itens` por pedido (query em lote `ANY(ids)`) + `PedidoItemResumo`/espelho Web grafia idêntica + view com `<select name="pedidoItemId">` real (optgroups por pedido `data-pedido`, saldo por item visível, JS filtra no change e auto-seleciona item único; botões da tabela chamam o filtro) + guarda BFF contra GUID vazio (key resx existente — zero novas keys) | fato reflexivo ADR-0003 nos dois records + fato de fonte (select presente / hidden ausente / guarda BFF) + fato de banco (seed R8B4 → lista expõe item c/ saldo → receber 2 de 5 atualiza `QuantidadeRecebida`) + **smoke vivo** abaixo |

**Design (diretriz):** o padrão de extensão de superfície agora tem 3 aplicações documentadas no ciclo (r22b2 reservas ativas, r23b2 itens de pedido): (1) estender o DTO Application no record de resumo com lista aninhada; (2) query de repositório em lote sobre os ids já filtrados; (3) espelho Web com grafia idêntica — o contrato BFF (`R20BffMappingContratoTests`) cobre os props novos recursivamente **sem nenhuma mudança de teste** (comportamento previsto pelo ADR-0003); (4) ligar a view + 3 fatos (reflexivo/fonte/banco). Superfície do scanner estável em 83 pareamentos/212 parâmetros/76 ações (o mesmo campo `name="pedidoItemId"` mudou de hidden para select — conjunto inalterado).

## Smoke vivo (observação aberta R19 — desbloqueada)

A sessão portal ADMINISTRADOR_CLIENTE que rebolava na R19 **não reproduziu nesta execução** (mesma técnica: curl-equivalente com jar de cookies):

| Passo | Resultado |
|---|---|
| POST `/Account/Login` (gestor@santacasa-demo.example) | 200 → cookie + token em sessão (log Web: `UsuarioId:…7511 Perfil:ADMINISTRADOR_CLIENTE Escopo:TENANT`; log Api: `Login bem-sucedido … Modulos:18`) → redirect `ClientePortal/Index` |
| GET `/ClientePortal/Index` | **permanece na página** (na R19 rebolava para login imediatamente) |
| GET `/Administrativo360/Recebimentos` (página alvo desta rodada) | 200 com a nova superfície renderizada: **16 optgroups** (pedidos APROVADO+PARCIAL do banco dev `plantaopro_test`), **16 rótulos de saldo**, primeiro grupo `PC-70 - Fornecedor WsA3 T6 LTDA`, legado `hiddenPedidoItemId` ausente |
| Chamadas BFF por trás da página | 4× GET 200 (`compras?APROVADO`, `compras?PARCIAL`, `lookups`, `xml?quarentena=false`) |

Apps parados ao final (portas 51976/52976 livres, estado de casa). Nota de honestidade: o quirk R19 era comportamental de sessão — uma execução verde não "prova fix" (nada foi mudado no fluxo de sessão), mas remove o bloqueio da fila: as próximas rodadas podem fazer smoke de UI Adm360 normalmente.

## Gates finais

- Build real `--no-incremental` antes de cada compare (protocolo corrigido na R22): **0 erros**; multiset de avisos **idêntico** ao pristino r17 (160 entradas, chave `arquivo|mensagem`). Executado nas fatias r23b1 e r23b2.
- Suíte final: **1529/1529** (base 1524 + 5 fatos novos desta rodada) — verde sem flakes nesta rodada.

## Commits da rodada

| Commit | Conteúdo |
|---|---|
| `df02f9d` | r23a0 auditoria/plano + r23b1 F14 falso positivo (2 fatos permanentes) |
| `270cf29` | r23b2 F-R15-C: extensão itens de pedido + seletor na view + guarda BFF + 3 fatos |
| (este) | r23z consolidado + smoke vivo |

## Observações abertas / fila R24 (plano das 10 sprints)

1. **R24 = F-R14-D + F-R14-B** (itens herdados — recon no r24a0).
2. Smoke de UI continua habilitada (sessão desbloqueada nesta rodada); manter apps parados entre fatias.
3. Padrões mantidos: commits `<seções> (rodada N rNx)` sem acentos; gate com rebuild real; evidência versionada; C# 10; zero novas keys resx Adm360 (contador R7DF6 653 intacto).
