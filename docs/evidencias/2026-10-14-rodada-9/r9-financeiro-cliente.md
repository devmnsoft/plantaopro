# r9 — Relatórios financeiros por CLIENTE + vencimentos por PARCELA (§9/§10)

**Data:** 2026-10-15 · **Base:** commit local r9e (`684ab79`) · **Banco:** `plantaopro` local (PostgreSQL 18) · **Tenant de prova:** Sistema Vertical (`8b0c8e74-a81b-4ea2-b499-94755a1ca301`) · **Gate:** build 0 erros + suite **1395/1395** (baseline 1394 + 1 teste novo)

## 1. Escopo do corte

Dois problemas reais do módulo (auditoria §9, classificados QUEBRADO/AUSENTE) foram corrigidos sem tocar em DDL:

| Ponto | Antes | Depois |
|---|---|---|
| **Dimensão comercial** | Coluna "Hospital" (`COALESCE(c.nome, hosp.nome, 'Hospital')`) — rótulo errado para a dimensão comercial e fallback genérico | Coluna **"Cliente"** (`COALESCE(c.nome, hosp.nome, 'Cliente não identificado')`). A tabela clínica `hospitais` continua como **fallback de nome** para registros históricos em que `v.cliente_id` aponta para lá (compatibilidade preservada; nenhuma tabela clínica/FK removida). |
| **Números/datas vazios na tela** | O espelho Web usava `Numero`/`Data`, que **nunca casavam** com o JSON da API (`vendaNumero`/`competencia`) — as colunas renderizavam vazias e o CSV saía sem esses campos | Espelho renomeado **por nome** (`VendaNumero`/`Competencia`) — colunas e CSV agora realmente preenchidos (verificado na saída real). |
| **Bruto/desconto perdidos** | O item do relatório só levava líquido/custo/comissão | `TotalBruto`/`Desconto` agora são transportados (parâmetros finais opcionais no record) e aparecem na tela e no CSV. |
| **Filtro por cliente** | `hospitalId` (sem validação — id de outro tenant devolvia relatório vazio "silencioso") | `clienteId` com **rejeição cross-tenant explícita**: id que existe em outro tenant → 400 `"O cliente informado pertence a outro tenant."`; id inexistente → 400 `"Cliente não encontrado."`. Na Web o erro vira toast e a tabela explica ("Nenhuma venda encontrada"). |
| **Vencimentos** | AUSENTE (0 telas/consultas de vencimento no relatório financeiro) | Nova aba **"Vencimentos por Parcela"** + endpoint `GET …/relatorios-financeiros/vencimentos` + CSV, **uma linha por título/parcela**. |

### Semântica dos vencimentos (definida e verificada)

- **Sem multiplicação:** cada `adm360_titulos_receber` (parcela) = exatamente uma linha; os JOINs de nome (cliente/pagador) são 1:1. Os totais do rodapé somam cada parcela **uma única vez**.
- **Período** sobre `data_vencimento` (inclusivo) — o eixo natural de um relatório de vencimentos.
- **Situações:** `ABERTO`/`PARCIAL`/`QUITADO` aparecem (QUITADO como histórico de liquidação, saldo 0); `CANCELADO` fica de fora (parcela que não será cobrada) — declarado nos indicadores da tela.
- **"Vencido"** = `data_vencimento < data do relatório AND saldo_aberto > 0`, com referência = **momento da geração do relatório**, mesma regra das Contas a Receber (`ContasReceberRepository`). Parcela quitada com vencimento passado **não** é vencida (verificado).
- Mesmos filtros do resto da página (período + `clienteId` c/ rejeição cross-tenant + tenant sempre isolado).

## 2. Migração

**Nenhuma nova DDL.** Nada de manifest/consolidado alterado (cadeia segue terminando em v2353). Corte 100% código.

## 3. Alterações por arquivo

| Arquivo | Alteração |
|---|---|
| `backend/PlantaoPro.Application/Administrativo360/Contracts.cs` | `RelatorioVendasItem`: 3º parâmetro `Hospital`→`Cliente` + finais opcionais `TotalBruto`/`Desconto`; `RelatorioMargemItem`: `Hospital`→`Cliente`; novo `RelatorioVencimentoItem` (título/venda/competência/cliente/pagador/parcela/totaisParcelas/emissão/vencimento/principal/recebido/saldo/situação/vencido); interface: parâmetro renomeado em `RelatorioVendasAsync`, `RelatorioMargemAsync` ganha `Guid? clienteId = null` **final** (chamadas posicionais existentes seguem compilando), novo `RelatorioVencimentosAsync` |
| `backend/PlantaoPro.Infrastructure/Administrativo360/Adm360FinanceiroRelatoriosRepository.cs` | Helper `ValidarClienteFiltroAsync` (dois `SELECT EXISTS` no padrão do módulo — parceiro∪hospitais escopado ao tenant; existe em outro tenant → `Administrativo360BusinessException` "pertence a outro tenant"; inexistente → "não encontrado"; o filtro do módulo já mapeia para **400**); `RelatorioVendasAsync` reescrita (fallback `'Cliente não identificado'`, `+v.total_bruto/v.desconto`, filtro `@clienteId::uuid`); `RelatorioMargemAsync` (+fallback +filtro opcional); nova `RelatorioVencimentosAsync` (JOINs 1:1 no padrão de `ContasReceberRepository`, `CANCELADO` fora, `Vencido` calculado em C# na data de geração) |
| `backend/PlantaoPro.Api/Controllers/Adm360FinanceiroRelatoriosController.cs` | `GET vendas`: `hospitalId`→`clienteId`; `GET margem`: `+[FromQuery] Guid? clienteId`; novo `GET vencimentos` (mesma policy `Adm360.Financeiro`) |
| `backend/PlantaoPro.Web/Models/Administrativo360Models.cs` | `RelatorioVendasItemViewModel`: `Numero`→`VendaNumero`, `Data`→`Competencia`, `Hospital`→`Cliente` (espelho **por nome** = corrige o binding); `RelatorioMargemItemViewModel`: `Hospital`→`Cliente`; novo `RelatorioVencimentoItemViewModel`; PageVM `+Vencimentos`, `+ClienteId`, `+Clientes` |
| `backend/PlantaoPro.Web/Controllers/Administrativo360Controller.RelatoriosFinanceiros.cs` | Assinatura `+clienteId`; busca dos 4 endpoints; opções do filtro = lookups (parceiros ativos + hospitais clínicos rotulados "(clínico)", dedup por id, ordenados; falha degrada para lista vazia); **primeiro erro da API vira `TempData["Error"]`** (toast — é assim que a rejeição 400 chega à tela); CSVs: cabeçalho "Hospital"→"Cliente", campos renomeados, `&clienteId=` em todos os exports; nova ação `ExportarVencimentosCsv` (coluna `Vencido` Sim/Nao; numeração pt-BR via `ValorHumano.Format`, BOM UTF-8) |
| `backend/PlantaoPro.Web/Views/Administrativo360/RelatoriosFinanceiros.cshtml` | Filtros: `<select>` de cliente (+ `aba`/`vendedorId` preservados em reenvio); abas: 4ª "Vencimentos por Parcela" e **todos os links carregam período+vendedor+cliente** (o que se imprime/visualiza é o relatório filtrado); tabelas: coluna "Cliente", propriedades renomeadas; nova aba: badges (VENCIDO vermelho; QUITADO verde; PARCIAL âmbar; ABERTO cinza), vencimento vencido em vermelho, saldo aberto em destaque, tfoot com Σ principal/recebido/saldo + contador de vencidas "cada parcela contada uma única vez"; indicadores declarados: bloco Vendas atualizado (fallbacks, `clienteId` c/ 400) e bloco novo de Vencimentos (fonte, fórmula "vencido", filtros, tratamento de estorno/baixa) |
| `backend/PlantaoPro.Tests/Administrativo360RelatoriosFinanceiroClienteTests.cs` (novo) | Teste integrado: nome via parceiro + bruto/desconto transportados; filtro próprio tenant funciona; **cross-tenant rejeitado**; inexistente rejeitado; isolamento por tenant; **3 parcelas → 3 linhas (CANCELADA fora)**; exatamente 1 vencida (ABERTA no passado); QUITADA passada não é vencida; ABERTA futura não é vencida; período sobre `data_vencimento`; margem com dimensão Cliente + filtro opcional + cross-tenant |

## 4. Evidência de execução (tenant SV, saída real de 2026-10-15)

Seed idempotente de homologação (`r9-seed-demo.sql`, ids fixos): parceiro-cliente `Clinica Horizonte Azul` (eh_cliente), pagador `Cofre Clinica Azul` (eh_pagador), venda `VEN-SV-R9F-01` CONFIRMADA a prazo (bruto 1200,00 / desconto 200,00 / líquido 1000,00 / custo 600,00 / comissão 5%) e 3 títulos: parcela 1 **ABERTA vencida há 15 dias**, parcela 2 **QUITADA vencida há 7 dias**, parcela 3 **ABERTA futura (20 dias)**; + parceiro de outro tenant para a prova cross-tenant.

```text
SEED_VENDA_OK=True SEED_TITULOS_OK=True SEED_OUTRO_TENANT_OK=True
API_VENDAS_STATUS=200
VENDAS_CLIENTE_OK=True VENDAS_PAGADOR_OK=True
VENDAS_BRUTO=1200.0000 DESC=200.0000 LIQ=1000.0000 CUSTO=600.0000
VENDAS_COMPETENCIA_PREENCHIDA=True VENDAS_SHAPE_ANTIGO_HOSPITAL_AUSENTE=True VENDAS_KEYS_NOVAS_OK=True
VENDAS_FILTRO_CLIENTE_OK=True
VENDAS_CROSSTENANT_STATUS=400 MSG_OK=True        # "O cliente informado pertence a outro tenant."
VENDAS_INEXISTENTE_STATUS=400 MSG_OK=True        # "Cliente não encontrado."
API_VENCIMENTOS_STATUS=200
VENC_PARCELAS_LINHAS_OK=True                     # 3 títulos -> 3 linhas (sem multiplicação)
VENC_SALDO_TOTAL_OK=True VALOR=[666.67]          # 333.33 + 0 + 333.34
VENC_VENCIDAS_COUNT_OK=True                      # exatamente 1 (parcela 1, ABERTA vencida)
VENC_CLIENTE_EM_TODAS_OK=True VENC_QUITADA_NAO_E_VENCIDA_OK=True
VENC_CROSSTENANT_STATUS=400 MSG_OK=True
MARGEM_CLIENTE_OK=True MARGEM_CONTRIB_OK=True MARGEM_PCT_OK=True    # 350.00 (35.00%) = 1000-600-50
MARGEM_FILTRO_CLIENTE_OK=True MARGEM_CROSSTENANT_STATUS=400 MSG_OK=True
WEB_VENDAS_STATUS=200 WEB_COL_CLIENTE=True WEB_COL_HOSPITAL_AUSENTE=True
WEB_NOME_CLIENTE=True WEB_NUMERO_VENDA_PREENCHIDO=True WEB_SELECT_CLIENTE=True
WEB_TAB_VENCIMENTOS=True WEB_INDICADOR_CLIENTEID=True
WEB_VENCIMENTOS_STATUS=200 WEB_VENC_TITULOS=True WEB_VENC_BADGE_VENCIDO=True
WEB_VENC_PTBR_VALORES=True                       # R$ 333,33 / R$ 666,67 na tela
WEB_VENC_TOTAIS_ROTULO=True WEB_VENC_CONTADOR=True WEB_VENC_NOME_CLIENTE=True
WEB_VENC_FILTRO_CLIENTE_OK=True WEB_SELECT_MARCADO=True
WEB_CROSSTENANT_STATUS=200 WEB_CROSSTENANT_TOAST=True WEB_CROSSTENANT_TABELA_VAZIA=True
CSV_VENC_STATUS=200 CSV_VENC_HEADER_OK=True CSV_VENC_LINHA_OK=True
CSV_VENC_SIM_EXATAMENTE_1_OK=True CSV_VENC_PTBR=True
CSV_VENDAS_STATUS=200 CSV_VENDAS_HEADER_CLIENTE=True CSV_VENDAS_LINHA_OK=True CSV_VENDAS_PTBR_BRUTO=True
```

Trecho real do CSV de vencimentos baixado (`r9-sv-vencimentos.csv`):

```csv
TituloNumero;VendaNumero;Competencia;Cliente;Pagador;Parcela;TotalParcelas;DataEmissao;DataVencimento;ValorPrincipal;ValorRecebido;SaldoAberto;Situacao;Vencido
"TIT-SV-R9F-01";"VEN-SV-R9F-01";2026-10-09;"Clinica Horizonte Azul";"Cofre Clinica Azul";1;3;2026-10-09;2026-09-29;333,33;0,00;333,33;"ABERTO";Sim
"TIT-SV-R9F-02";"VEN-SV-R9F-01";2026-10-09;"Clinica Horizonte Azul";"Cofre Clinica Azul";2;3;2026-10-09;2026-10-07;333,33;333,33;0,00;"QUITADO";Nao
"TIT-SV-R9F-03";"VEN-SV-R9F-01";2026-10-09;"Clinica Horizonte Azul";"Cofre Clinica Azul";3;3;2026-10-09;2026-11-03;333,34;0,00;333,34;"ABERTO";Nao
```

## 5. Artefatos

- `r9-sv-relatorios-vendas.html` — tela real logada como SV, aba Vendas (coluna Cliente, select de clientes, valores preenchidos).
- `r9-sv-relatorios-vencimentos.html` — tela real, aba Vencimentos por Parcela (badges, VENCIDO, totais, indicadores).
- `r9-sv-vencimentos.csv` / `r9-sv-vendas.csv` — arquivos exportados pelos botões reais da tela.
- `r9-seed-demo.sql` — seed idempotente dos dados de homologação acima.

## 6. Observações e pendências registradas

1. **Dados demo permanecem no tenant SV** (ids fixos `e9f000…`) — deliberadamente: a próxima fatia (r9g, DRE gerencial por cliente) reaproveita a mesma venda/títulos. Reexecução do seed é idempotente (`ON CONFLICT (id) DO NOTHING`).
2. **Flake ambiental de teste** — `ProvisionamentoB5Tests.P6_Convite_…` falhou uma vez na suite completa (deadlock 40P01 no dispose, RAM baixa com hosts concorrentes) e passou isolado e nas execuções finais; não há relação com este corte (tabelas/jornada distintas). Gate final: suite 1395/1395.
3. **Dapper 2.1.35 × `DateOnly`** — o handler é registrado no construtor **estático** de `Adm360Repository`; o teste novo constrói o repositório antes do INSERT com parâmetros `DateOnly` (senão `NotSupportedException` no bind). Registrado para futuros testes que façam SQL bruto com datas.
4. **Quirk PS 5.1 do script de evidência (não da aplicação):** `@(corpo | ConvertFrom-Json)` não desembrulha o array (chega como 1 item `Object[]`); o padrão que funciona é atribuir primeiro e depois embrulhar (`$x = $body | ConvertFrom-Json; @($x)`). Afetou só o script de prova.
5. Sem alterações em resx (contrato i18n de 246 chaves intacto); rótulos novos em pt-BR estático conforme convenção do módulo (refino de UX/i18n é o escopo de r9h). Sem novas permissões/policies (rota nova usa `Adm360.Financeiro`, já concedida ao usuário SV — toda a jornada executou autenticada).
6. **Compatibilidade preservada:** `hospitais` continua servindo como fonte de nome histórico (sem drop de tabela clínica/FK); `comissões` não mudou (dimensão vendedor, escopo fora do §9/§10); chamadas posicionais existentes de `RelatorioMargemAsync` seguem compilando (parâmetro final opcional).
