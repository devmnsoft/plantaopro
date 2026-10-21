# R82a0 - Folha de pagamento PF em linguagem leiga (mes, 13o, ferias + 1/3, rescisao)

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R82 do bloco B4.
Escopo desta rodada: folha de pagamento funcional de pessoa fisica montada sobre o nucleo
da rodada 81: servico `FolhaPfService` com os quatro simuladores (mes com horas extras e
deducoes, 13o proporcional, ferias com terço constitucional, rescisao sem justa causa com
verbas) + tela simples na Web que explica cada desconto em linguagem leiga. Nenhum numero
legal vive no codigo: tudo sai da fonte unica `plantaopro.parametros_legais`.

## Fontes oficiais consultadas

- Hora extra: CLT art. 7 inciso XIII (EC 72/2013) - acrescimo minimo de 50% sobre a hora
  normal; jornada mensal canonica de 220 horas (CLT art. 58; INSS instrucao geral 120.301).
- Deducoes do contribuinte: tabela mensal IRRF da Receita Federal vigente ancorada ao
  minimo 1.518,00 (Lei 14.848/2024) - dedução por dependente 189,59 e limite da dedução
  simplificada 564,80 (gov.br/receitafederal, tabelas 2025).
- 13o salario: RIR art. 788 (Decreto 9.580/2018) - isento ate 2x salario minimo; o imposto
  incide apenas sobre o excedente.
- Rescisao: multa de 40% do FGTS (CLT art. 18 paragrafo unico, Lei 5.889/1973); FGTS 8%
  mensal (ja semeado pela rodada 80 em FGTS_ALIQUOTA_MENSAL).
- Proporcionais: regra canonica de contar o mes como cheio a partir de 15 dias trabalhados
  (INSS instrucao geral 120.318).

### Ancoragem do mundo do plano

Os valores derivados do minimo (dependente 189,59, simplificada 564,80, piso do 13o
3.036,00 = 2 x 1.518,00) ganham vigencia anual 2026. As regras estaveis da CLT (50% da hora
extra, 220 horas, 15 dias, multa 40%) ficam fixas com vigencia nula - padrao estabelecido
na v2363. Novo minimo futuro = novas linhas por ano (indice `(codigo, ano)` ja permite).

## Regras de calculo implementadas (FolhaPfService)

- Mes: valor hora = base / 220 (divisao exata, sem arredondamento intermediario); horas
  extras pagas com 50%; bruto = base + extras; INSS/IRRF pelo nucleo da rodada 81 agora
  aceitando deducoes (dependentes x 189,59 OU dedução simplificada 564,80 - nunca as duas,
  validacao explicita); FGTS 8% sobre o bruto como carga patronal informativa.
- 13o: meses creditados no ano com a regra dos 15 dias; proporcional = salario x meses/12;
  1a parcela = metade, 2a = saldo; cada parcela paga INSS progressivo por si; IRRF so sobre
  o excedente de 2x minimo (faixas mensais aplicadas ao excedente).
- Ferias: periodo proporcional em dias (1 a 30) + terço constitucional (1/3 do valor do
  periodo); tributa como evento unico (INSS progressivo limitado ao teto; IRRF mensal com
  piso) - previa isolada documentada.
- Rescisao sem justa causa: saldo do mes (dias/30), 13o proporcional do ano (15+ dias),
  ferias vencidas (períodos de 12 meses completos, teto 2) e ferias proporcionais (dias
  decorridos, teto 30), cada uma com o terço, aviso previo indenizado quando nao dado;
  INSS e IRRF consolidados em evento unico sobre o total tributavel; FGTS mensal x meses
  creditados (+1 do aviso) e multa de 40% sobre o acumulado; "total a perceber" soma liquido
  + FGTS + multa.

### Simplificacoes de previa registradas (fora do escopo desta rodada)

Sem media de variaveis/produtividade nas ferias e na rescisao (usa o ultimo salario fixo);
sem abonos e sem acrescimo do dobro do terceiro período de ferias; saldo do mes usa a
convencao de 30 dias; rescisao tributa o total consolidado em evento unico (pratica comum
na retencao de fontes em previa). Cada uma dessas linhas pode virar contrato futuro.

## Parametros semeados (migration v2365)

| codigo | valor | fonte |
| --- | --- | --- |
| HORA_EXTRA_ACRESCIMO_PCT | 50.00 (fixo) | CLT art. 7 XIII (EC 72/2013) |
| DIAS_MES_TRABALHADO_HORAS | 220 (fixo) | CLT art. 58 / IG 120.301 |
| DEPENDENTE_DEDUCAO_MENSAL | 189.59 (2026) | RFB tabela IRRF, minimo 1.518,00 |
| DEDUCAO_SIMPLIFICADA_MENSAL | 564.80 (2026) | RFB tabela IRRF, minimo 1.518,00 |
| PISO_ISENCAO_IRRF_DECIMO_TERCEIRO | 3036.00 (2026) | RIR art. 788 (2 x minimo) |
| FGTS_MULTA_RESCISAO_PCT | 40.00 (fixo) | CLT art. 18 par. unico |
| DIAS_CONTAGEM_MES_CREDITADO | 15 (fixo) | IG 120.318 (proporcionais) |

Idempotente (`on conflict (codigo, ano) ... do nothing`), manifesto v2365 com checksum real
e dependencia em v2364.

## Exemplos travados em contrato (R82FolhaContratoTests)

### Mes (salario base + horas extras + deducao)

| entrada | bruto | INSS | base IRRF | IRRF | liquido | FGTS 8% |
| --- | --- | --- | --- | --- | --- | --- |
| 5.000 + 10h extras + 1 dependente | 5.340,91 | 557,33 | 4.593,99 | 370,88 | 4.412,70 | 427,27 |
| 6.000 + dedução simplificada | 6.000,00 | 649,60 | 4.785,60 | 420,04 | 4.930,36 | 480,00 |

O primeiro caso mostra o ponto sensível: sem o dependente a base seria 4.783,58 (acima do
piso, imposto maior); com a dedução de 189,59 a base cai para 4.593,99 - o teste trava os
dois lados da conta.

### 13o salario (proporcional por meses trabalhados)

| admissao / salario | meses | proporcional | 1a parcela | INSS | IRRF (excedente 2x min) | liquido da parcela |
| --- | --- | --- | --- | --- | --- | --- |
| 2025-06-10 / 5.000 | 12 | 5.000,00 | 2.500,00 | 202,23 | 0,00 (abaixo de 3.036) | 2.297,77 |
| 2026-03-15 / 4.500 | 10 | 3.750,00 | 1.875,00 | 145,98 | 0,00 | 1.729,02 |
| 2024-01-05 / 8.000 | 12 | 8.000,00 | 4.000,00 | 373,41 | 0,00 (excedente 964,00 zerado pela parcela a deduzir) | 3.626,59 |

### Ferias + terço constitucional (evento isolado)

| salario / dias | ferias | terço | total | INSS | IRRF | liquido |
| --- | --- | --- | --- | --- | --- | --- |
| 3.000 / 30 | 3.000,00 | 1.000,00 | 4.000,00 | 373,41 | 0,00 | 3.626,59 |
| 8.000 / 30 | 8.000,00 | 2.666,67 | 10.666,67 | 951,64 (teto) | 1.775,63 | 7.939,40 |

### Rescisao sem justa causa (verba por verba)

Cenário: salario 4.000,00, admitido 2025-02-10, demitido 2026-10-15, sem aviso prévio dado.

| verba | valor |
| --- | --- |
| Saldo do mês (15 de 30 dias) | 2.000,00 |
| 13o proporcional (10 de 12 meses) | 3.333,33 |
| Ferias vencidas (1 período) | 4.000,00 |
| Terço das ferias vencidas | 1.333,33 |
| Ferias proporcionais (248 dias > 30, cheia) | 4.000,00 |
| Terço das ferias proporcionais | 1.333,33 |
| Aviso previo indenizado | 4.000,00 |
| Total tributavel | 19.999,99 |
| INSS (evento unico, teto) | -951,64 |
| IRRF (base 19.048,35) | -4.342,30 |
| Liquido das verbas (TRCT) | 14.706,05 |
| FGTS depositado (22 meses x 320,00) | 7.040,00 |
| Multa de 40% | 2.816,00 |
| Total a perceber | 24.562,05 |

## Mudancas desta rodada

- `database/migrations/2026_10_v2365_r82_parametros_folha_2026.sql`: os 7 parametros acima
  idempotentes com fonte citavel em cada linha.
- `database/migration-manifest.json`: entrada v2365 (checksum SHA256 real, dependencia
  v2364).
- `backend/PlantaoPro.Application/Legislacao/ITributacaoPfService.cs` +
  `backend/PlantaoPro.Infrastructure/Legislacao/TributacaoPfService.cs`: `CalcularAsync`
  ganha deducao por dependentes e dedução simplificada (opcionais, padrao zero - vetores da
  R81 intocados); `AplicarFaixa` vira interno para reuso no servico de folha.
- `backend/PlantaoPro.Application/Folha/IFolhaPfService.cs`: contrato com comandos e DTOs
  dos quatro calculos (inclui `VerbaRescisaoDetalhe` com nome em linguagem leiga).
- `backend/PlantaoPro.Infrastructure/Folha/FolhaPfService.cs`: motor das regras acima
  (meses creditados 15+ dias, HE sobre 220h, isenção do 13o ate 2x minimo, evento unico na
  rescisão, FGTS + multa).
- `backend/PlantaoPro.Api/Controllers/FolhaController.cs`: `POST api/folha/mes`,
  `decimo-terceiro`, `ferias`, `rescisao`; 400 com mensagem direta em validação falha.
- `backend/PlantaoPro.Api/Program.cs`: DI no padrao da casa.
- `backend/PlantaoPro.Web/Controllers/FolhaWebController.cs`: proxy BFF `{ ok, data/erro }`
  (padrao anexos R72) apontando para a API.
- `backend/PlantaoPro.Web/Views/FolhaWeb/Index.cshtml`: quatro simuladores com campos
  rotulados, status inline (sem alert/confirm) e resultado em tabela; cada card termina com
  o bloco "Em linguagem simples" explicando a regra aplicada.
- `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml`: entrada "Folha e Rescisões"
  (modulo ADM360, ícone Reception - livre no registro, sem reutilizar par conceitual).
- `backend/PlantaoPro.Web/Localization/UiTexts*.resx` (4 locales): chave Nav.FolhaCalculos.
- `backend/PlantaoPro.Web/Controllers/AjudaController.cs`: artigo adm-folha-calculos
  (ordem 380) apontando para FolhaWeb/Index - a nova pagina entra ja coberta.
- `docs/evidencias/2026-10-20-rodada-75/r75_inventario.txt`: rerun do inventário apos a
  mudanca: paginas 675 (+1), artigos 83 (+1), cobertas 81 (+1), gap mantido em 517.
- `backend/PlantaoPro.Tests/R82FolhaContratoTests.cs`: 7 fatos - manifesto v2365 com hash
  real; parametros semeados linha a linha (fixos vs vigentes); mes com HE/dependente e com
  simplificada; 13o nos tres cenários; ferias nos dois salários; rescisao verba por verba;
  validacoes explicitas (dependente+simplificada juntos, ferias fora de 1-30 dias, parcela
  invalida, demissao antes da admissao).

## Suíte e gates

- Suíte cresce de 1702 para 1709 aprovados (apps parados nas portas 51976/51977/52976/52977).
- Build A/B Release `--no-incremental`: base 81 avisos, nova 81 avisos, 0 erros nos dois,
  DIFF=NENHUMA.
- Gap de manuais mantido travado em 517 (nova pagina + novo artigo no mesmo commit;
  inventário versionado atualizado).
- Docscan `docs/evidencias/2026-10-19-rodada-62/r62_docscan.ps1` reexecutado apos o
  documento final, delta zero.
- Higiene `RepositoryHygieneContractTests` 8/8 apos o documento.

## Proxima rodada

R83 - Simples Nacional: classes de receita, faixas cumulativas, preview de DAS com exemplo
real e explicacao campo a campo (mesma filosofia de fonte unica: parametros semeados com
fonte citavel e contrato de vetores).
