# R89a0 - Venda para fora do país: moeda base BRL + cotações USD/EUR/GBP e faturamento internacional com nota não-tradicional e VAT por destino

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R89 do bloco B4.
Escopo desta rodada: tornar a venda para fora do país mensurável dentro do produto: as faturas saem
sempre em real brasileiro (moeda base), o produto mantém uma tabela institucional de cotações de
câmbio (BRL por unidade de USD/EUR/GBP) com upsert por (moeda, data) preservando histórico, um
catálogo de alíquotas padrão de IVA/VAT por país de destino com fonte citável por linha, e o
faturamento internacional simplificado como nota fiscal não-tradicional amarrada à fatura SaaS:
conversão do valor pela cotação registrada mais recente até o dia de emissão e VAT do destino
acrescido sobre a base convertida, com snapshot imutável para auditoria. Sem integração externa nesta
etapa (sem OSS/IOSS, sem sales tax estadual dos EUA, sem atualização automática de câmbio): tudo
permanece em registro local. i18n mantido nos 4 locales (pt/en/es/fr).

## Interpretação registrada: VAT acrescido e dispensas brasileiras na exportação

A leitura adotada é a do faturamento internacional simplificado: o VAT do país de destino é cobrado
**acrescido** sobre a base convertida (subtotal + imposto), exibido em duas linhas independentes, e a
nota registra como snapshot a cotação aplicada (data + taxa) e a alíquota aplicada, permitindo
reauditoria posterior independente do estado atual das tabelas. No lado brasileiro, a venda ao
exterior enquadra-se nas dispensas clássicas de exportação - ICMS (Lei 8.080/1990, art. 39, II) e IPI
(Lei 4.502/1964, art. 15, III, c/c Decreto 4.404/2002, arts. 19-23) - citadas no parâmetro de forma
de documento. A nota usa a cotação mais recente em data **anterior ou igual** ao dia de emissão
(registros futuros não existem na prática e são bloqueados no registro, mas a consulta respeita a
fronteira mesmo assim). O arredondamento monetário é sempre metade-afasta-de-zero em 2 casas
(discriminadores travados nos testes: `503,3984625 / 5,0277 = 100,125 -> 100,13`, onde arredondamento
banca daria 100,12; e `2,50 x 1% = 0,025 -> 0,03`, onde banca daria 0,02).

Não-goal documentado: alíquotas reduzidas/especiais por item (somente a alíquota padrão do destino),
OSS/IOSS europeu, sales tax estadual dos EUA (o catálogo carrega US com alíquota 0 e a observação da
fonte), conversão com câmbio automático e múltiplas moedas além de USD/EUR/GBP (catálogo
parametrizado - estender exige apenas parâmetro + cotação).

## Fontes oficiais consultadas

- Cotação do dólar (seed 5.027700): taxa comercial do Banco Central do Brasil -
  bcb.gov.br/en/currencyconversion, confirmado em 2026-10-22 durante o recon.
- Cotação do euro (seed 5.635300): euro comercial em BRL - Valor Data, fechamento de 21/10/2026,
  confirmado em 2026-10-22.
- Cotação da libra (seed 6.730000): libra esterlina comercial em BRL - Investidor10, cotação de
  22/10/2026.
- Alíquotas padrão de VAT (catálogo de 9 destinos): HMRC gov.uk/vat-rates (GB 20%); BOFiP DGFiP
  BOI-TVA-LIQ-10 (FR 20%); Agencia Tributaria Espanola AEAT (ES 21%, vigente desde 01/09/2012);
  Belastingdienst (NL 21%); Autoridade Tributária de Portugal (PT 23%); Agenzia delle Entrate (IT
  22%); BMF/Finanzverwaltung Alemanha (DE 19%); National Tax Agency do Japão JCT (JP 10%); EUA sem
  imposto federal sobre valor agregado (US 0,00, sales tax estadual fora do escopo).
- Dispensas brasileiras na exportação: Lei 8.080/1990, art. 39, II (dispensa de ICMS) e Lei
  4.502/1964, art. 15, III, c/c Decreto 4.404/2002, arts. 19-23 (dispensa de IPI).
- ISO 4217 para os códigos de moeda e ISO 3166-1 alpha-2 para os países de destino (chaves das
  tabelas, com check constraints de formato).

## Regras implementadas

- `VendasInternacionaisRegras` (aritmética pura, sem IO): `ConvertParaMoeda` = round(valor_brl /
  taxa; 2 casas, metade afasta de zero); `ValorVat` = round(valor_moeda x aliquota / 100; 2 casas,
  metade afasta de zero); `CalcularNota` compõe base convertida + VAT acrescido e devolve o total
  somado com arredondamento próprio; validações defensivas (valor não negativo, taxa positiva,
  alíquota entre 0 e 100); `MoedaAceita` (código ISO de 3 letras contra o catálogo parametrizado,
  sem distinção de caixa) e `FormatoIso` (alpha-2/alpha-3). Constantes de fallback espelham a fonte
  única `parametros_legais`.
- `VendasInternacionaisService` (Dapper/Postgres, superfície global de administração - mesmo plano
  dos trilhos R86 e da cobrança, sem filtro de tenant): listagem de cotações com filtro opcional por
  moeda/intervalo (intervalo inverso 400); registro/atualização de cotação com upsert por
  (moeda, data) preservando o histórico e validações (moeda fora do catálogo 400, data antes de
  2020-01-01 ou futura 400, taxa <= 0 ou acima do limite de sanidade 400, fonte até 300 caracteres
  400 com default institucional); catálogo de destinos com alíquota e fonte; faturas candidatas
  (ABERTA/VENCIDA com valor positivo, join com o nome do cliente); simulação da nota (sem
  persistir) devolvendo a decomposição completa + fontes aplicadas; emissão transacional com
  snapshot imutável (valor_brl, data_cotacao, taxa_aplicada, valor_moeda, aliquota_aplicada,
  valor_vat, total_moeda, forma NAO_TRADICIONAL) e exatamente uma EMITIDA por fatura (checagem prévia
  + catch do 23505 do índice parcial único -> 409; fatura ausente 404; fatura fechada, país fora do
  catálogo ou moeda fora do escopo 400); anulação com motivo obrigatório (até 400 caracteres) que
  libera a reemissão (segunda anulação 404); listagem de notas com filtro opcional por fatura.
  Parâmetros lidos de `parametros_legais` (COTACAO_MOEDAS, COTACAO_MOEDA_BASE,
  NOTA_INTERNACIONAL_FORMA) com fallback para as constantes puras - fonte única preservada.
- API `api/vendas-internacionais` com sessão autenticada exigida (`[Authorize]` simples, padrão dos
  trilhos R86): 8 rotas - `GET cotacoes?moeda&de&ate`, `POST cotacoes`, `GET paises-vat`,
  `GET faturas?limite`, `POST notas/simular`, `POST notas`, `GET notas?faturaId` e
  `POST notas/anular`; falha de contrato = 400/404/409 com mensagem direta.
- Superfície web: 8 ações proxy `{ ok, data/erro }` em `VendasInternacionaisWebController` + página
  com três painéis: cotações de câmbio com registro inline, destinos com alíquotas padrão de VAT e
  notas internacionais (simulação com decomposição linha a linha, emissão e anulação via modal
  Bootstrap - sem alert/confirm nativos). Sidebar na seção de administração global após
  Faturamento SaaS com ícone único `Move` (livre no registry, contrato R46); guard
  `VendasInternacionaisWeb -> FATURAMENTO_SAAS` (permissão existente); rótulo
  `Nav.VendasInternacionais` nos 4 locales ("Vendas Internacionais"/"International Sales"/"Ventas
  Internacionales"/"Ventes Internationales"). Artigo de ajuda `adm-vendas-internacionais` (ordem 440)
  aponta para a página.

## Migração v2372 e seeds

- `cotacoes_moedas`: moeda char(3) com check ISO alpha-3, data, taxa numeric(18,6) > 0 (formato BC:
  quantos BRL compram 1 unidade), fonte 5..300, PK (moeda, data); 3 seeds de 2026-10-22 - USD
  5.027700 (BCB), EUR 5.635300 (Valor Data, fech. 21/10), GBP 6.730000 (Investidor10, 22/10) - cada
  um com a fonte citável.
- `paises_vat`: pais char(2) PK com check ISO alpha-2, nome 5..120, aliquota numeric(5,2) 0..100,
  fonte 5..300; 9 linhas citáveis (DE 19, ES 21, FR 20, GB 20, IT 22, JP 10, NL 21, PT 23, US 0.00).
- `notas_internacionais`: amarrada a `faturas_saas.id` (uuid sem FK - validação no serviço, padrão da
  casa), snapshot imutável completo, `forma_documento` fixa `NAO_TRADICIONAL`, status
  `EMITIDA|ANULADA`, índice parcial único `ux_v2372_nota_emitida_por_fatura` (fatura_id where status
  'EMITIDA' and reg_status 'A') que torna o 409 estrutural; anular libera a reemissão.
- 3 linhas em `parametros_legais` (ano 2026) com valor e fonte: `COTACAO_MOEDAS` = USD|EUR|GBP;
  `COTACAO_MOEDA_BASE` = BRL; `NOTA_INTERNACIONAL_FORMA` = NAO_TRADICIONAL (com as dispensas
  ICMS/IPI citadas na fonte). O comentário canônico da coluna `parametros_legais.valor` foi estendido
  com a cláusula R89 (ISO de moeda "|" para cotações, ISO puro para moeda base, identificador puro
  para forma de documento). Idempotente (create if not exists + on conflict / where not exists);
  manifesto v2372 com checksum SHA256 real (LF
  `f507cae52f2ab0d8d63859429f73f9466b2d204359d1cae8b37bfbf02abe94ed`) e dependência em v2371
  (manifesto agora com 143 entradas).
- Nota técnica: em Npgsql/Dapper desta base, parâmetro `DateOnly?` nulo enviado sem tipo faz o
  Postgres responder 42P08 ("não foi possível determinar o tipo de dados do parâmetro") quando usado
  em `@x is null or coluna >= @x`; a consulta de intervalo da cotação aplica cast explícito
  `@de::date`/`@ate::date`.

## Exemplos travados em contrato (R89VendasInternacionaisContratoTests, 9 fatos)

Notas simuladas/emitidas com os seeds institucionais (2026-10-22):

| fatura (BRL) | destino / moeda | cotação aplicada | convertido | VAT (alíq.) | total |
| --- | --- | --- | --- | --- | --- |
| 5.027,70 ABERTA | DE / USD | 5,0277 (22/10) | 1.000,00 | 190,00 (19%) | 1.190,00 |
| 100,50 ABERTA | DE / USD | 5,0277 (22/10) | 19,99 | 3,80 (19%) | 23,79 |
| 563,53 VENCIDA | GB / EUR | 5,6353 (22/10) | 100,00 | 20,00 (20%) | 120,00 |
| 5.027,70 ABERTA | GB / GBP | 6,73 (22/10) | 747,06 | 149,41 (20%) | 896,47 |
| 100,50 ABERTA | US / GBP | 6,73 (22/10) | 14,93 | 0,00 (0%) | 14,93 |

Arredondamento metade-afasta-de-zero (discriminadores): `ConvertParaMoeda(503,3984625, 5,0277) =
100,13` (ponto médio exato 100,125 - banker's daria 100,12) e `ValorVat(2,50, 1%) = 0,03` (ponto
médio exato 0,025 - banker's daria 0,02).

Registro de cotação: upsert por (moeda, data) - registrar USD 2026-10-20 a 4,90 cria a linha e
registrar novamente a 4,95 atualiza em vez de duplicar; histórico intacto (o seed USD 22/10 segue
presente e mais recente). Rejeições 400: moeda fora do catálogo (JPY), moeda base (BRL), taxa 0,
taxa negativa, taxa acima da sanidade (2.000.000), data futura (2027-01-01), data anterior a
2020-01-01, e intervalo de listagem invertido.

Faturas candidatas: entram ABERTA/VENCIDA com valor positivo (com o nome do cliente via join) e
saem PAGA, CANCELADA e ABERTA zerada; o teste exige competências distintas por cliente/mês por causa
do índice parcial único `ux_faturas_saas_cliente_competencia_ativa` existente em `faturas_saas`.

Emissão: persiste o snapshot completo (status EMITIDA, forma NAO_TRADICIONAL, data_cotacao 2026-10-22
e taxa aplicada) e devolve o nome do cliente; segunda emissão da mesma fatura -> 409; fatura
CANCELADA -> 400; fatura ausente -> 404; simular não persiste nada (mesma fatura emite logo em
seguida). Anulação: motivo ausente -> 400, motivo de 401 caracteres -> 400, anular grava status e
motivo, segunda anulação do mesmo id -> 404, e a reemissão da fatura anulada volta a funcionar
(prova do ciclo completo de liberação). Cotação mais recente vence: com o seed GBP deslocado para
2026-10-15 e uma cotação nova de 2026-10-20 a 6,55, a simulação usa 6,55 (563,53 / 6,55 = 86,04;
VAT 20% = 17,21; total 103,25) e o histórico listado sai ordenado pela mais recente primeiro.

Schema e manifesto: 3 cotações/9 destinos/3 parâmetros com valores exatos e fonte >= 5 caracteres
em toda linha, reexecução da migração não duplica nada, índice parcial único presente, manifesto com
hash real + dependência v2371. Padrão B6: advisory locks r89_vendas_internacionais_seed/_schema,
seed/purge transacional com GUIDs fixos hex.

## Mudanças desta rodada

- `database/migrations/2026_10_v2372_r89_vendas_internacionais.sql`: 3 tabelas novas (cotações
  institucionais, catálogo de destinos/VAT, notas internacionais) + índices + 3 parâmetros legais
  com fonte citável + comentário canônico da coluna `parametros_legais.valor` estendido com a
  cláusula R89; idempotente.
- `database/migration-manifest.json`: entrada v2372 (checksum SHA256 real, dependência v2371;
  manifesto passa de 142 para 143 entradas).
- `backend/PlantaoPro.Api/VendasInternacionaisRegras.cs`: aritmética pura (conversão BRL, VAT
  acrescido, cálculo composto da nota, validações de código ISO).
- `backend/PlantaoPro.Api/VendasInternacionaisService.cs`: 8 operações Dapper de superfície global
  (cotações, destinos, faturas, simular/emitir/anular/listar notas) com snapshot imutável,
  parâmetros legais e fallback, DTOs records com colunas aliasadas.
- `backend/PlantaoPro.Api/Controllers/VendasInternacionaisController.cs`: `api/vendas-internacionais`
  com as 8 rotas; sessão autenticada exigida.
- `backend/PlantaoPro.Api/Program.cs`: DI do serviço no padrão da casa.
- `backend/PlantaoPro.Web/Controllers/VendasInternacionaisWebController.cs`: 8 ações proxy
  `{ ok, data/erro }` (padrão das rodadas R84-R88).
- `backend/PlantaoPro.Web/Views/VendasInternacionaisWeb/Index.cshtml`: painéis de cotações,
  destinos/VAT e notas com simulação detalhada e modal de anulação; tabelas em `.table-responsive`
  com `th scope="col"` (contratos R45/R59).
- `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml`: item Vendas Internacionais na seção de
  administração global com ícone Move único no menu (contrato R46).
- `backend/PlantaoPro.Web/Localization/UiTexts{,.en,.es,.fr}.resx`: chave `Nav.VendasInternacionais`
  nos 4 locales.
- `backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs`: rota
  `VendasInternacionaisWeb -> FATURAMENTO_SAAS`.
- `backend/PlantaoPro.Web/Controllers/AjudaController.cs`: artigo `adm-vendas-internacionais`
  (ordem 440) apontando para VendasInternacionaisWeb/Index - a nova página entra já coberta.
- `docs/evidencias/2026-10-20-rodada-75/r75_inventario.txt`: rerun do inventário após a mudança:
  páginas 682 (+1), artigos 90 (+1), cobertas 88 (+1), gap mantido em 517, nenhum artigo órfão,
  aux 29/pote quebra 29 estáveis.
- `backend/PlantaoPro.Tests/R89VendasInternacionaisContratoTests.cs`: 9 fatos - manifesto v2372 com
  hash real e dependência; schema/seeds idempotentes com valores exatos e fonte citável; vetores
  puros de conversão e VAT com discriminadores de arredondamento; registro/upsert de cotação com
  validações 400; faturas candidatas (só ABERTA/VENCIDA com valor positivo, nome do cliente);
  simulação com rejeições 400/404 sem persistir; emissão com snapshot, 409 por duplicidade e
  listagem com filtros; anulação com motivo obrigatório, 404 repetida e reemissão liberada;
  cotação mais recente vencendo na emissão.

## Suíte e gates

- Suíte completa cresce de 1755 para 1764 aprovados / 0 falhas (apps parados nas portas
  51976/51977/52976/52977 antes da execução).
- Build A/B Release `--no-incremental`: base 81 avisos, nova 81 avisos, 0 erros nos dois,
  DIFF=NENHUMA (dedupe: 76 únicos de cada lado; logs `%TEMP%\opencode\r89_build_base.txt` /
  `r89_build_nova.txt`).
- Gap de manuais mantido travado em 517 (nova página + novo artigo no mesmo ciclo; inventário
  versionado atualizado, nenhum artigo órfão).
- Docscan `docs/evidencias/2026-10-19-rodada-62/r62_docscan.ps1` reexecutado após o documento
  final: 0 defeitos.
- Higiene `RepositoryHygieneContractTests` 8/8 após o documento.

## Próxima rodada

R90 - bateria de contratos legais: parâmetros oficiais 2026 (mínimo R$ 1.518, tetos e tabelas
INSS/IRRF) travados com fonte citada; exemplos reais de cálculo.
