# R83a0 - Simples Nacional: classes de receita, faixas cumulativas e previa do DAS

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R83 do bloco B4.
Escopo desta rodada: Simples Nacional funcional montado sobre a fonte unica das rodadas 80 a 82:
servico `SimplesNacionalService` com leitura da tabela oficial vigente (cinco anexos, faixas
cumulativas, tetos ME/EPP, dia de vencimento) e previa do DAS campo a campo com exemplo real
na Web, explicada em linguagem leiga. Nenhum numero legal vive no codigo: tudo sai da fonte
unica `plantaopro.parametros_legais`.

## Fontes oficiais consultadas

- Tabela de faixas: anexos I a V da LC 123/2006 na redacao da LC 155/2016 (vigencia
  01/01/2018), texto consolidado em gov.br/planalto - seis faixas por anexo, limites
  cumulativos 180 mil / 360 mil / 720 mil / 1,8 m / 3,6 m / 4,8 m.
- Limites do regime: LC 123/2006 art. 3 incisos I e II (redacao LC 155/2016) - microempresa
  ate 360.000,00 e empresa de pequeno porte ate 4.800.000,00 de receita bruta anual.
- Enquadramento por atividade: LC 123/2006 art. 18 paragrafos 4o e 5o-B a 5o-I (revenda,
  fabricacao, servicos em geral com saude e educacao, construcao/limpeza/vigilancia/advocacia,
  consultoria/auditoria/veterinaria/engenharia/publicidade/traducao/agenciamento).
- Apuracao: LC 123/2006 art. 18 capitulo e paragrafos 1o, 1o-A e 3o - alíquota efetiva =
  (RBT12 x alíquota - parcela a deduzir) / RBT12, aplicada sobre a receita bruta do mes.
- Recolhimento: regulamento do Simples Nacional (CGSN) - o DAS emitido pelo PGDAS-D vence no
  dia 20 do mes subsequente a competencia.
- Fora do escopo registrado na tela: transicao CBS/IBS no regime (LC 214/2025 e resolucoes
  CGSN de 2026), que mantem por ora as faixas consolidadas aqui travadas.

### Ancoragem do mundo do plano

As tabelas sao valores fixos em lei (reais, nao indexados ao salario minimo): todas as 33
linhas ganham vigencia nula (regra fixa) no ano bucket 2026 - mesmo padrao da v2363. Novas
tabelas futuras = novas linhas por ano; o indice `(codigo, ano)` da fonte unica ja permite.
A descontinuidade oficial de entrada na faixa 6 do Anexo I (alíquota efetiva cai de 11,875%
para 8,5% ao cruzar 3,6 m) e reproduzida naturalmente pela formula - e mantida como esta, pois
e o texto oficial consolidado.

## Regras de calculo implementadas (SimplesNacionalService)

- Faixa cumulativa: a faixa do anexo e escolhida pela receita bruta acumulada nos 12 meses
  ANTERIORES a competencia (teto da faixa inclusivo; limite exato fica na faixa de baixo).
- Alíquota efetiva = (RBT12 x alíquota da faixa - parcela a deduzir) / RBT12, porcentagem
  com 4 casas (arredondamento comercial, meio para fora).
- Valor do DAS = alíquota efetiva x receita bruta do mes, arredondado em 2 casas.
- Classe de receita: ME ate o teto menor da fonte unica, EPP acima dele ate o teto maior;
  acumulado acima de 4.800.000,00 = fora do regime, erro explicito com a mensagem direta.
- Empresa nova (sem acumulado informado): estima RBT12 = 12 x receita do mes e sinaliza
  `ReceitaAcumuladaEstimada` no resultado; acumulado zerado aplica a aliquota plana da faixa 1
  (parcela zero, sem divisao por zero).
- Vencimento = dia oficial da fonte unica (20) do mes seguinte a competencia; dezembro vira
  janeiro do ano seguinte.
- Validações explicitas (ArgumentException com mensagem direta): anexo 1..5, mes 1..12,
  competencia 2000..2100, mensal > 0, acumulado >= 0; ano sem vetores = erro de operacao.

### Simplificacoes de previa registradas (fora do escopo desta rodada)

Sem decomposicao do valor por tributo (IRPJ/CSLL/PIS/COFINS/CPP/ICMS/ISS): a previa mostra o
total unico, como o documento oficial arrecada; o PGDAS-D emite a decomposicao. Sem creditos
de ICMS e sem regras especificas de municipios (Sao Paulo, Distrito Federal). O DAS definitivo
com codigo de barras sai do PGDAS-D apos a declaracao mensal - esta tela e planejamento.

## Parametros semeados (migration v2366)

Formato canonico das faixas (v2364): `limite|aliquota_pct|parcela_a_deduzir`, com limite =
receita bruta acumulada em 12 meses. Matriz oficial travada (aliquota / parcela, por faixa):

| anexo | f1 | f2 | f3 | f4 | f5 | f6 |
| --- | --- | --- | --- | --- | --- | --- |
| I - comercio por revenda/distribuicao | 4,00/0 | 7,30/5.940 | 9,50/13.860 | 10,70/22.500 | 14,30/87.300 | 19,00/378.000 |
| II - industria/fabricacao | 4,50/0 | 7,80/5.940 | 10,00/13.860 | 11,20/22.500 | 14,70/85.500 | 30,00/720.000 |
| III - servicos em geral (saude, educacao) | 6,00/0 | 11,20/9.360 | 13,50/17.640 | 16,00/35.640 | 21,00/125.640 | 33,00/648.000 |
| IV - construcao, limpeza, vigilancia, advocacia | 4,50/0 | 9,00/8.100 | 10,20/12.420 | 14,00/39.780 | 22,00/183.780 | 33,00/828.000 |
| V - consultoria, auditoria, veterinaria, engenharia, publicidade, traducao | 15,50/0 | 18,00/4.500 | 19,50/9.900 | 20,50/17.100 | 23,00/62.100 | 30,50/540.000 |

Limites cumulativos comuns as seis faixas (reais): 180.000,00 / 360.000,00 / 720.000,00 /
1.800.000,00 / 3.600.000,00 / 4.800.000,00. Mais tres parametros:

| codigo | valor | fonte |
| --- | --- | --- |
| SIMPLES_LIMITE_ME_RECEITA_ANUAL | 360000.00 (fixo) | LC 123 art. 3 I |
| SIMPLES_LIMITE_EPP_RECEITA_ANUAL | 4800000.00 (fixo) | LC 123 art. 3 II (LC 155/2016) |
| SIMPLES_DAS_DIA_VENCIMENTO | 20 (fixo) | Regulamento CGSN (PGDAS-D) |

Idempotente (`on conflict (codigo, ano) ... do nothing`), manifesto v2366 com checksum SHA256
real e dependencia em v2365.

## Exemplos travados em contrato (R83SimplesContratoTests)

### Faixa cumulativa, classe e aliquota efetiva (exemplo real de clinica de servicos)

| cenario | RBT12 | mes | classe | faixa | efetiva | DAS |
| --- | --- | --- | --- | --- | --- | --- |
| Servicos de saude (exemplo real da tela) | 720.000,00 | 60.000,00 | EPP | 3 (teto 720 mil) | 11,0500% | 6.630,00 |
| Comercio por revenda | 200.000,00 | 18.750,00 | ME | 2 (teto 360 mil) | 4,3300% | 811,88 |
| Revenda no limite exato (inclusivo) | 180.000,00 | 15.000,00 | ME | 1 | 4,0000% | 600,00 |
| Consultoria na faixa 1 | 90.000,00 | 7.500,00 | ME | 1 (Anexo V) | 15,5000% | 1.162,50 |

### Empresa nova e faixa alta

| cenario | RBT12 | mes | faixa | efetiva | DAS |
| --- | --- | --- | --- | --- | --- |
| Servicos, empresa nova (acumulado estimado) | nula -> 120.000,00 | 10.000,00 | 1 | 6,0000% | 600,00 |
| Industria na faixa 5 | 2.400.000,00 | 200.000,00 | 5 (teto 3,6 m) | 11,1375% | 22.275,00 |

O primeiro caso trava a estimativa de empresa nova (flag `ReceitaAcumuladaEstimada`); o
segundo trava a formula efetiva em faixa alta: (2.400.000 x 14,7% - 85.500) / 2.400.000.

### Vencimento do DAS (regulamento CGSN)

Competencia dezembro/2026 -> vencimento 20/janeiro/2027 (ano vira). Competencia junho/2026 ->
vencimento 20/julho/2026. O dia 20 vem da fonte unica, nao do codigo.

## Mudancas desta rodada

- `database/migrations/2026_10_v2366_r83_simples_nacional_2026.sql`: os 33 parametros acima
  idempotentes com fonte citavel em cada linha (gov.br/planalto por linha).
- `database/migration-manifest.json`: entrada v2366 (checksum SHA256 real, dependencia
  v2365).
- `backend/PlantaoPro.Application/Legislacao/ISimplesNacionalService.cs`: contrato com os DTOs
  da tabela (anexo + faixas reutilizando `FaixaTributoLegalDto`), do comando e da previa do
  DAS campo a campo.
- `backend/PlantaoPro.Infrastructure/Legislacao/TributacaoPfService.cs`: helpers `ResolveAno`,
  `ValorObrigatorio` e `Faixas` viram internal para reuso pelo servico do Simples (padrao do
  `AplicarFaixa` expandido em R82); vetores PF intocados.
- `backend/PlantaoPro.Infrastructure/Legislacao/SimplesNacionalService.cs`: motor das regras
  acima (faixa pelo acumulado, formula efetiva, classe ME/EPP, estimativa de empresa nova,
  teto do regime, vencimento).
- `backend/PlantaoPro.Api/Controllers/SimplesController.cs`: `GET api/simples/tabela` e
  `POST api/simples/calcular-das`; 400 com mensagem direta em validação falha.
- `backend/PlantaoPro.Api/Program.cs`: DI no padrao da casa.
- `backend/PlantaoPro.Web/Controllers/SimplesWebController.cs`: proxy BFF `{ ok, data/erro }`
  apontando para a API (mesmo padrao da Folha, R82).
- `backend/PlantaoPro.Web/Views/SimplesWeb/Index.cshtml`: dois cartoes - tabelas oficiais por
  atividade (faixas cumulativas + tetos + dia de vencimento) e previa do DAS com o botao
  "Carregar exemplo real"; campos rotulados, status inline (sem alert/confirm) e o bloco
  "Em linguagem simples" explicando cada campo do resultado campo a campo.
- `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml`: entrada "Simples Nacional"
  (modulo ADM360, icone Print - livre no registro, sem reutilizar par conceitual).
- `backend/PlantaoPro.Web/Localization/UiTexts*.resx` (4 locales): chave Nav.SimplesNacional.
- `backend/PlantaoPro.Web/Controllers/AjudaController.cs`: artigo adm-simples-nacional
  (ordem 390) apontando para SimplesWeb/Index - a nova pagina entra ja coberta.
- `docs/evidencias/2026-10-20-rodada-75/r75_inventario.txt`: rerun do inventário apos a
  mudanca: paginas 676 (+1), artigos 84 (+1), cobertas 82 (+1), gap mantido em 517.
- `backend/PlantaoPro.Tests/R83SimplesContratoTests.cs`: 7 fatos - manifesto v2366 com hash
  real; parametros semeados linha a linha (matriz completa + 3 fixos, todos com fonte); tabela
  do servico com os cinco anexos em ordem e tetos crescentes; vetores reais de faixa/efetiva
  (com limite inclusivo); empresa nova + industria em faixa alta; vencimento dezembro ->
  janeiro e Anexo V; validações explicitas (anexo/mes/competencia/receitas/teto/ano).

## Suíte e gates

- Suíte cresce de 1709 para 1716 aprovados (apps parados nas portas 51976/51977/52976/52977).
- Build A/B Release `--no-incremental`: base 81 avisos, nova 81 avisos, 0 erros nos dois,
  DIFF=NENHUMA.
- Gap de manuais mantido travado em 517 (nova pagina + novo artigo no mesmo commit;
  inventário versionado atualizado).
- Docscan `docs/evidencias/2026-10-19-rodada-62/r62_docscan.ps1` reexecutado apos o
  documento final, delta zero.
- Higiene `RepositoryHygieneContractTests` 8/8 apos o documento.

## Proxima rodada

R84 - calendario fiscal e obrigacoes legais (DAS dia 20, FGTS dia 20, DCTFWeb dia 15, eSocial,
EFD, livros contabeis) virando motor de obrigacoes e reminders por tenant, consumindo os
mesmos parametros da fonte unica que esta rodada consolidou.
