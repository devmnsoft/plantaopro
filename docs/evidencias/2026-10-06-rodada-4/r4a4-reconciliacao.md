# R4-A4 — Reconciliação monetária: inventário completo, revalidação do v2311 (limpa + upgrade) e rerun T1–T8

Data: 2026-10-06 · Base: `c875362` (R4-A3) · Ambiente: PostgreSQL 18 local (`plantaopro_test` + bancos efêmeros) · Runner: `Tools.Database` (manifesto de instalação v2.21.8, manifesto de migrações com 84 migrações; executor v1.91.0). Produção **não acessível** nesta máquina — auditoria de dados executada sobre o ambiente de teste canônico, conforme escopo da D1 (§3.3).

## 1. Inventário monetário completo (fora de `adm360_*`)

**Método.** `information_schema.columns` do schema `plantaopro`: 416 colunas `numeric` no total; 250 com nome monetário (valor/total/preço/saldo/multa/desconto/comissão/custo/cobrança/recebimento/pagamento/estorno/baixa/repasse/glosa); **189 verificadas coluna a coluna** por heurística executada (count/min/max/frações reais) via bloco PL/pgSQL dinâmico em `plantaopro_test`. As ~74 colunas `adm360_*` já estão normalizadas para `numeric(18,4)` e testadas em WS-A2/WS-A3 (`d1b3736`/`622cbfa`) — não repetidas aqui.

### 1.1 Descobertas

1. **Nenhuma escala-centavo fora de adm360**: nenhuma coluna tem `max` ≥ 1e5 nem padrão de inteiro suspeito de centavos. Maior valor observado: `planos.valor_mensal = 3990.00` (preço de plano SaaS — realista em reais).
2. **Únicos valores fracionários legítimos**: `v113_*` (99.90 / 199.80 — reais·centavos em colunas scale-2, uso correto) e `ai_precos_modelos` (preço unitário por milhão de tokens, parâmetro de tabela de preço, não saldo; colunas 6 casas por projeto).
3. **Fluxos legados ativos com dados reais** — todos em **reais integrais** coerentes com a escala (12,2)/(14,2): `clinica_caixa`/`clinica_contas_receber`/`clinica_recebimentos`/`clinica_estornos` (≤ 480.00), `convenio_autorizacoes` (90–500), `fechamento_plantao_escalas` (1500), `pagamentos` (apurado 1500; aprovado/pago NULL = aguardando fluxo), `plantoes.valor` (1500–1650), `modulos_sistema.preco_base` (1131 linhas = 250.00, catálogo), `saas_planos.valor_mensal` (299–2990), `v115_regras_*` (parâmetros de regra), `v116_*` (parcialmente NULL).
4. **Módulos demo/métricos 100% vazios** (rows=0 em todas as colunas monetárias): `churn_*`, `cs_*`, `contas_b2b_*`, `executivo_*`, `comercial_*`, `piloto_*`, `suporte_*`, `operacao_assistida_*`, `adocao_*`, `faturas_saas`/`fatura_itens`, `cadastro_cliente_pagamentos_iniciais`, `contas_b2b_*`. Sem dados = sem autoridade monetária no ambiente auditado; **excluídos do contrato (18,4)** (são parâmetros/métricas, não saldos) e não exigem correção.
5. `ai_usos.custo_confirmado`/`custo_estimado`: 31 linhas, custo todo NULL — coerente com o estado IA P2 (custo incerto até confirmação do provedor; homologação externa bloqueada por credencial).

### 1.2 Decisão de correção

**Correções aplicadas no A4: zero.** Nenhuma coluna fora de `adm360_*` apresenta evidência de contaminação em centavos ou divergência de escala que justifique alteração de dado ou de schema — e a regra da rodada proíbe correção sem evidência (sem ÷100 automático). As escalas mistas entre gerações ((12,2) vs (14,2) vs (18,2)) são **contratos históricos documentados**; unificação só por justificativa de negócio por tabela → backlog (§5). Anexo A traz as 65 colunas com dados reais (min/max observados) para auditoria reprodutível.

## 2. Revalidação da migration v2311 (`2026_10_v2311_adm360_documentos_identidade_eventos`)

Objetos do v2311: índice único parcial `ux_adm360_docrec_tenant_fiscal` (identidade fiscal tenant+emitente+modelo+numero+serie, `WHERE quarentena=false`); constraint `ux_adm360_doc_eventos_tenant_doc_seq` (sequência de eventos 1..N); dedup de cópias **byte-identicas** sem vínculos; diagnóstico `RAISE EXCEPTION` para duplicatas divergentes fora de quarentena; renumeração + normalização de `sha256_hash` pela fórmula canônica do trigger v2301; pré/pos-condições abortam a transação em falha. Checksum canônico: `5075bfe673fcf1d093b36eda354eb5f93287e2675af402da23599c2c4b85b477`.

### Cenário L — instalação limpa (banco efêmero `plantaopro_a4_limpa`)

| Passo | Comando | Resultado |
|---|---|---|
| 1 | `create-database` | banco criado UTF-8 (template0) |
| 2 | `install` | **84 fontes canônicas do manifesto v2.21.8, success=true** (última seção = [80] v2311); `IDENTITY_SCHEMA_READY` (Verify) |
| 3 | assert objetos | índice = 1, constraint = 1 |

### Cenário U — upgrade representativo (banco efêmero `plantaopro_a4_upgrade`)

| Passo | Ação | Resultado |
|---|---|---|
| 1 | `install` completo + **simulação do estado pré-v2311** (DROP do índice e da constraint; dados: nenhum — DB vazia) | estado "instalação existente anterior ao release" |
| 2 | `upgrade` #1 | **84/84 migrações aplicadas sem erro** — a cadeia inteira é idempotente sobre instalação completa; objetos do v2311 recriados |
| 3 | re-simulação pré-v2311 (DROP objetos + DELETE da linha do runner) | estado limpo para o upgrade real |
| 4 | `upgrade` #2 | **exatamente 1 migração aplicada: v2311** (demais 83 puladas por checksum) · linha registrada com checksum idêntico ao manifesto, 20 ms, success=true · objetos recriados (índice=1, constraint=1) · `IDENTITY_SCHEMA_READY` |
| 5 | `upgrade` #3 | **0 aplicações** (no-op estável) · `IDENTITY_SCHEMA_READY` |

`schema_migrations` final: 87 linhas = 84 do manifesto + **3 backfills de versões legadas** (`v1.27.0`, `v1.31.0`, `v1.95.1`) inseridos pelas próprias migrações correspondentes — comportamento esperado, estável entre os upgrades #2/#3.

**Conclusão:** v2311 validada por execução em instalação limpa **e** em upgrade representativo; idempotência dupla confirmada (executada 2× no Cenário U sem divergência) e checksum travado pelo runner (diferença de 1 byte na migration seria abortada como "Checksum alterado").

## 3. Rerun T1–T8 (WS-A3) — cenários exigidos

`dotnet test --filter "FullyQualifiedName~Administrativo360DocumentosWsA3Tests"` → **Aprovado: 8/8, Falha: 0** (2 s, contra `plantaopro_test` com v2311 ativo).

| Cenário exigido | Teste | O que prova |
|---|---|---|
| Concorrente | T1 `ConcorrenciaMesmaChave_IdempotenciaExata`; T2 `...Divergente_UnicoVencedor_ErroExplicito` | importações paralelas da mesma chave fiscal convergem (idempotência exata) ou deixam um único vencedor com erro explícito |
| Divergência de bytes | T2; T7 `ColisaoIdentidadeFiscal_BytesDistintos` | XMLs diferentes com mesma identidade fiscal não são mesclados silenciosamente — erro explícito com diagnóstico |
| Quarentena | T8 `QuarentenaMalformados_CoexistemSemColisao` | documentos MALFORMADO em quarentena coexistem fora do índice único parcial e não bloqueiam novas importações |
| Recebimento parcial | T4 `RecebimentoFisico_WriteBackSemMudarStatus`; T5 `RecebimentoParcialSobreoRecebimentoCanonica`; T6 `OutroPedido_ErroExplicito` | write-back sem mudança de status; parcial sob o recebimento canônico; outro pedido → erro explícito |
| Título único | T5 (exerce `ux_adm360_titulos_pagar_origem`) | origem gera no máximo um título por vínculo |

Cadeia completa (status vinculado + sequências contínuas 1..N) coberta por T3.

## 4. Findings

- **F1 — v2311 fora do registro do runner em `plantaopro_test`:** o commit WS-A3 (`622cbfa`) aplicou a migration via psql manual (2×) — os objetos existem, mas **não há linha em `schema_migrations`**. Consequência benigna provada hoje: um `Tools.Database upgrade` real re-executa o v2311 idempotentemente (Cenário U, executado 2×). **Recomendação para homologação:** rodar `Tools.Database upgrade` contra o banco de homologação para registrar o v2311 no runner.
- **F2 — Escalas mistas entre gerações:** (12,2)/(14,2) nos fluxos legados × (18,4) em ADM360; valores armazenados coerentes em ambos os mundos (reais integrais ou reais·centavos), mas não existe contrato único de precisão fora de ADM360. Unificação só com justificativa de negócio por tabela (backlog).
- **F3 — Produção inacessível:** a auditoria de dados de §1 vale para o ambiente de teste canônico; divergências de dados em produção só seriam detectáveis com acesso ao banco produtivo (pendência de homologação, não bloqueio).
- **F4 (carregado do A3):** `LimparJornadaAsync` engole erros de cleanup em `catch {}` silencioso — alvo do A5.

## 5. Status e backlog

- **Status A4: implementado e testado por execução** (inventário heurístico executado, v2311 limpa + upgrade + idempotência, T1–T8 verdes). Correções de dados/schema: **0** (sem evidência que as justifique).
- Backlog: (1) unificação de escala por tabela com justificativa de negócio; (2) `Tools.Database upgrade` no banco de homologação para registrar o v2311 (F1); (3) auditoria equivalente sobre banco produtivo quando houver acesso (F3); (4) F4 no A5.
- Bancos efêmeros `plantaopro_a4_limpa`/`plantaopro_a4_upgrade` criados, usados e removidos após a coleta; nenhum dado sensível neles.

## Anexo A — Colunas monetárias fora de adm360_* com dados reais em `plantaopro_test` (65)

`min`/`max` observados (fração real = nº de valores com parte decimal):

```
agendamentos.valor                      rows=12   min=0.00    max=0.00
ai_precos_modelos.preco_entrada_milhao  rows=8    min=0.075   max=1.32     (tabela de preço/tokens)
ai_precos_modelos.preco_saida_milhao    rows=8    min=0.30    max=10.00    (tabela de preço/tokens)
ai_usos.custo_confirmado                rows=31   NULL        NULL         (custo incerto até confirmação)
ai_usos.custo_estimado                  rows=31   NULL        NULL
assinaturas.valor_contratado            rows=1    min=0.00    max=0.00
assinaturas.valor_mensal                rows=1    min=0.00    max=0.00
clinica_caixa.saldo_final               rows=2    min=0.00    max=480.00
clinica_caixa.saldo_inicial             rows=2    min=100.00  max=100.00
clinica_caixa.total_entradas            rows=2    min=0.00    max=380.00
clinica_caixa.total_saidas              rows=2    min=0.00    max=0.00
clinica_contas_receber.* (8 cols)       rows=8    0.00 .. 260.00  (desconto, bruto, copart., liq., pago, pend., total)
clinica_estornos.valor                  rows=1    min=10.00   max=10.00
clinica_recebimentos.valor              rows=3    min=120.00  max=180.00
convenio_autorizacoes.valor_autorizado  rows=6    min=90.00   max=500.00
fechamento_plantao.valor_apurado/_previsto rows=1 min=0.00    max=0.00
fechamento_plantao_escalas.valor_calculado/_previsto rows=1   min=1500.00 max=1500.00
modulos_sistema.preco_base              rows=1131 min=250.00  max=250.00   (catálogo)
pagamentos.valor_apurado                rows=13   min=1500.00 max=1500.00
pagamentos.valor_previsto               rows=13   min=500.00  max=1500.00
pagamentos.valor_hora                   rows=13   min=0.00    max=100.00
pagamentos.valor_aprovado / valor_pago  rows=13   NULL        NULL         (aguardando fluxo)
planos.valor_anual / valor_mensal       rows=8    0.00 .. 3990.00
plantoes.valor                          rows=4    min=1500.00 max=1650.00
saas_planos.valor_mensal                rows=4    min=299.00  max=2990.00
tenant_modulos.preco_contratado         rows=18   NULL        NULL
v113_faturas.valor/discontos/glosa/base rows=1    0.00 .. 199.80   (reais·centavos)
v113_pedido_itens.valor_unitario        rows=1    99.90
v113_pedidos.total                      rows=1    199.80
v113_produtos.preco                     rows=1    99.90
v113_titulos.valor                      rows=1    199.80
v115_regras_faturamento.* (2 cols)      rows=3    0.00 .. 1200.00  (+ percentuais 0–5)
v115_regras_glosa.* (3 cols)            rows=1    0.00 .. 45.00    (+ percentual 10)
v115_regras_repasse.* (3 cols)          rows=2    0.00 .. 900.00
v116_caixa_movimentos.valor             rows=1    120.00
v116_faturamento_lotes.valor            rows=1    360.00
v116_recebimentos_parciais.valor        rows=1    80.00
v116_caixas / v116_convenio_* / v116_integracao / v116_notificacoes / v116_relatorios / v116_timelines — rows≤3, valores NULL ou ausentes
white_label_templates.valor             rows=7    min=0.00    max=0.00     (parâmetro de template, não saldo)
```

Todas as demais 124 colunas monetárias fora de adm360_* (módulos demo/métricos listados em §1.1-4) têm **0 linhas** neste ambiente.
