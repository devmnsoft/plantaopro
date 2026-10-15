# r9g — DRE gerencial por CLIENTE em competência: reconstrução, reconciliação, origem e limitações (§11)

**Data:** 2026-10-15 · **Base:** commit local r9f (`9c347ff`) · **Banco:** `plantaopro` local (PostgreSQL 18) · **Tenant de prova:** Sistema Vertical (`8b0c8e74-a81b-4ea2-b499-94755a1ca301`) · **Gate:** build 0 erros + suite **1396** (1394 baseline roda 9 + r9f + teste novo; única falha da execução foi o flake B14 já registrado — ver §7)

## 1. Escopo do corte

O DRE gerencial era **AUSENTE** na auditoria (0 ocorrências de "DRE" em `.cs`/`.sql`). Foi construído como a 5ª aba dos Relatórios Financeiros, com as propriedades exigidas pelo §11:

| Exigência §11 | Como ficou |
|---|---|
| **Por cliente, em competência** | Uma linha por cliente comercial resolvido no tenant; período sobre `competencia` do documento (vendas e despesas) — **explicitamente não fluxo de caixa** (a tela e os indicadores declaram a diferença e apontam para o Fluxo de Caixa). |
| **Receita, deduções identificadas, líquido, custo realizado, bruto** | Receita Bruta = Σ`total_bruto`; Deduções = Σ`desconto` (identificados na venda); Líquida = Σ`total_liquido`; Custo Realizado = Σ`total_custo` gravado na venda; Resultado Bruto = Líquida − Custo. Só vendas `CONFIRMADA` (RASCUNHO/CANCELADA fora — declarado). |
| **Despesas atribuídas + "Não atribuída"** | Bucket **"Não atribuída"** recebe (a) vendas cujo `cliente_id` não resolve em parceiro/hospital e (b) **todas** as despesas do período (`adm360_titulos_pagar`, `valor_principal`, CANCELADO fora) — porque nesta base a despesa **não tem vínculo com cliente comercial** (`origem_tipo` ∈ RECEBIMENTO_COMPRA/DESPESA_MANUAL/COMISSAO_VENDEDOR; verificado no esquema). Nenhum regra de atribuição foi inventada: com filtro por cliente, a linha leva despesas = 0 (declaração explícita na tela). |
| **Resultado gerencial do período** | Resultado Bruto − Comissões Previstas (gravadas na venda) − Despesas Atribuídas, por linha e total no rodapé. |
| **Consolidado reconciliado = clientes + não atribuída** | Na geração, o repositório **re-agrega as mesmas fontes** (mesmo WHERE) e compara com as linhas somadas; deltas diferentes de zero viram badge vermelho com o valor — em vez de somas que "casam por construção". Badge verde "Consolidado reconciliado" quando tudo bate. Conciliação de despesas sinalizada como estrutural (sem divisão por cliente nesta base). |
| **Acesso aos registros de origem** | Cada linha tem atalho (ícone) que abre a aba **Vendas** já filtrada por período e pelo cliente da linha (deep-link `?aba=vendas&clienteId=…`); a linha "Não atribuída" aponta para a aba Vendas com instrução de revisar vendas sem atribuição (o bucket é derivado — não há filtro único para ele). Contadores no rodapé: N vendas confirmadas + N títulos de despesa no período. |
| **Fontes incompletas sinalizadas como limitação** | Bloco de indicadores declara, sem esconder: despesas concentradas em "Não atribuída" (sem vínculo na base); comissões previstas ≠ caixa; base gerencial (sem critério contábil/ISC, sem conversão cambial); valores gravados no documento (sem recálculo retroativo). |

## 2. Migração

**Nenhuma nova DDL.** Nada de manifest/consolidado alterado (cadeia segue terminando em v2353). Corte 100% código.

## 3. Alterações por arquivo

| Arquivo | Alteração |
|---|---|
| `backend/PlantaoPro.Application/Administrativo360/Contracts.cs` | Novos records `DreGerencialLinha` (10 colunas de resultado + `ClienteId` nulo só no bucket órfão), `DreGerencialConciliacao` (contadores + 3 flags + 3 deltas), `DreGerencialDto` (período + linhas + conciliação); método `RelatorioDreAsync(tenantId, inicio, fim, clienteId?, ct)` na interface de relatórios |
| `backend/PlantaoPro.Infrastructure/Administrativo360/Adm360FinanceiroRelatoriosRepository.cs` | `RelatorioDreAsync`: validação cross-tenant reutilizada (`ValidarClienteFiltroAsync` → 400); agregação GROUP BY sobre `adm360_vendas` CONFIRMADA (LEFT JOINs de nome 1:1); coalescimento C# dos ids órfãos em um único bucket; despesas em `ExecuteScalarAsync` separados (padrão do módulo); conciliação por re-agregação das mesmas fontes (comparação exata — PG numeric ↔ decimal .NET); ordenação com "Não atribuída" sempre por último |
| `backend/PlantaoPro.Api/Controllers/Adm360FinanceiroRelatoriosController.cs` | Novo `GET api/administrativo360/relatorios-financeiros/dre?inicio&fim&clienteId` (mesma policy `Adm360.Financeiro`) |
| `backend/PlantaoPro.Web/Models/Administrativo360Models.cs` | Espelhos por nome (binding JSON): `DreGerencialLinhaViewModel`, `DreGerencialConciliacaoViewModel`, `DreGerencialDtoWeb`; PageVM `+Dre` e `+DreConciliacao` |
| `backend/PlantaoPro.Web/Controllers/Administrativo360Controller.RelatoriosFinanceiros.cs` | Página busca o 5º endpoint e encadeia o erro ao toast existente; novo `ExportarDreCsv` (cabeçalho ASCII, valores pt-BR via `ValorHumano.Format`, BOM UTF-8) |
| `backend/PlantaoPro.Web/Views/Administrativo360/RelatoriosFinanceiros.cshtml` | 5ª aba "DRE Gerencial" (links preservam período+vendedor+cliente); tabela de resultado com cores por sinal, célula órfã em itálico, atalhos de origem; tfoot com consolidado; rodapé de conciliação (badges + deltas + contadores + diferença de caixa); botão Exportar CSV no ramo `aba=dre`; bloco de indicadores novo (col-lg-12) com fonte/fórmula/filtros/conciliação/limitações |
| `backend/PlantaoPro.Tests/Administrativo360RelatoriosFinanceiroClienteTests.cs` | Novo Fact `DreGerencial_PorCliente_CompentenciaConciliacaoEOrfa`: linha nomeada com todos os 10 valores; bucket órfão (venda sem nome + despesas APROVADA na janela, CANCELADA e futura fora); conciliação contadores/deltas/flags; filtro por cliente (despesas zeradas + conciliação); cross-tenant 400; janela secundária sem vendas (só despesa entra → resultado −55); isolamento por tenant |

## 4. Evidência de execução (tenant SV, saída real de 2026-10-15)

Seed idempotente de homologação (`r9-seed-dre-demo.sql`, ids fixos hexadecimais): venda órfã `VEN-SV-R9G-ORFA` (`cliente_id` sem parceiro/hospital; bruto 300,00 / desconto 50,00 / líquido 250,00 / custo 100,00 / comissão 2,50) + 2 títulos pagar na mesma competência (`TP-SV-R9G-01` APROVADO 40,00; `TP-SV-R9G-02` CANCELADO 99,00). Soma-se à venda do r9f (`VEN-SV-R9F-01` / Clinica Horizonte Azul / líquido 1.000,00 / custo 600,00 / comissão 50,00).

```text
SEED_R9F_OK=True SEED_R9G_VENDA_OK=True SEED_R9G_TITULOS_OK=True
API_DRE_STATUS=200
DRE_LINHAS_2_OK=True                    # Clinica Horizonte Azul + Nao atribuida
DRE_NOM_VALORES_OK=True                 # 1200/200/1000/600/400/50/0 -> resultado 350,00
DRE_ORFA_VALORES_OK=True                # 250 liq - 100 custo - 2,50 com - 40 desp = 107,50
DRE_ORFA_ID_NULO=True
DRE_CONC_CONTS_OK=True                  # 2 vendas + 1 titulo de despesa no periodo
DRE_CONC_BATE_TUDO=True                 # deltas zero nas tres dimensoes
DRE_FILTRO_CLIENTE_OK=True DRE_FILTRO_DESPESAS_ZERO=True DRE_FILTRO_CONC_OK=True
DRE_CROSSTENANT_STATUS=400 MSG_OK=True  # "O cliente informado pertence a outro tenant."
DRE_INEXISTENTE_STATUS=400 MSG_OK=True  # "Cliente não encontrado."
WEB_DRE_STATUS=200 WEB_DRE_TABELA_TITULO=True WEB_DRE_CONC_BADGE=True
WEB_DRE_COL_RESULTADO=True WEB_DRE_NOME_CLIENTE=True WEB_DRE_ORFA_CELULA=True
WEB_DRE_PTBR_VALORES=True               # R$ 1.200,00 / R$ 350,00 / R$ 107,50 na tela
WEB_DRE_LINK_ORIGEM_CLIENTE=True        # deep-link ?aba=vendas&clienteId=...
WEB_DRE_CONCILIAFOOTER=True             # badges "bate — delta zero" + Contadores
WEB_DRE_DIF_CAIXA=True                  # diferenca de caixa declarada na tela
WEB_DRE_CROSSTENANT_STATUS=200 WEB_DRE_CROSSTENANT_TOAST=True
CSV_DRE_STATUS=200 CSV_DRE_HEADER_OK=True CSV_DRE_LINHAS_OK=True CSV_DRE_PTBR_VALORES=True
```

Trecho real do CSV exportado pelo botão da tela (`r9-sv-dre.csv`):

```csv
Cliente;QtdVendas;ReceitaBruta;Deducoes;ReceitaLiquida;CustoRealizado;ResultadoBruto;ComissoesPrevistas;DespesasAtribuidas;ResultadoGerencial
"Clinica Horizonte Azul";1;1.200,00;200,00;1.000,00;600,00;400,00;50,00;0,00;350,00
"Não atribuída";1;300,00;50,00;250,00;100,00;150,00;2,50;40,00;107,50
```

## 5. Artefatos

- `r9-sv-relatorios-dre.html` — tela real logada como SV, aba DRE Gerencial (linhas, badge "Consolidado reconciliado", rodapé de conciliação, atalhos de origem).
- `r9-sv-dre.csv` — arquivo exportado pelo botão real da tela.
- `r9-seed-dre-demo.sql` — seed idempotente dos dados de homologação acima.

## 6. Observações e pendências registradas

1. **Dados demo permanecem no tenant SV** (ids fixos `e9a0…`) — reaproveitam e completam o cenário financeiro do tenant para as próximas fatias; reexecução é idempotente (`ON CONFLICT (id) DO NOTHING`).
2. **Suite 1396 — uma falha ambiental re-executada verde:** na execução completa, `ProvisionamentoB5Tests.P1_Finalizar_…` morreu por deadlock 40P01 (B14, RAM baixa) **durante a limpeza**, deixando resíduo; a reexecução isolada falhou então por "CNPJ já cadastrado" (o resíduo). Depois de limpeza pontual do mundo daquele CNPJ (espelhando `LimparMundoAsync`), toda a classe B5 passou 11/11 isolada. Nenhum teste do DRE falhou em nenhuma execução.
3. **Razor codifica texto dinâmico em entidades HTML** (`Não` → `N&#xE3;o`) — texto estático nos `.cshtml` não é afetado. Detalhe relevante apenas para asserções de evidência; sem efeito funcional (navegador decodifica).
4. Sem alterações em resx (contrato i18n de 246 chaves intacto); rótulos novos em pt-BR estático conforme convenção do módulo (refino de UX/i18n é escopo de r9h). Sem novas permissões/policies (`Adm360.Financeiro`, já concedida ao usuário SV — jornada inteira autenticada).
5. **Despesas por centro de custo:** `adm360_titulos_pagar.centro_custo` existe como texto livre, mas **não foi** usado como regra de atribuição (sem dicionário/controle na base — seria atribuição inventada). Registrado como evolução futura com fonte completa.
