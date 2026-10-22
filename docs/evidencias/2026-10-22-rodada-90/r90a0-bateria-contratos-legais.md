# R90a0 - Bateria de contratos legais: parâmetros oficiais 2026 travados com fonte citada e exemplos reais de cálculo

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R90 do bloco B4
(fechar o bloco). Escopo desta rodada: a "bateria de contratos legais" prometida desde a R81 -
travar em contrato o conjunto completo de parâmetros oficiais 2026 que vive na fonte única
`plantaopro.parametros_legais` (116 parâmetros ativos, semeados nas rodadas 80 a 89) em três
camadas: valores exatos com fonte oficial citável e vigência linha a linha, derivações cruzadas
matemáticas entre parâmetros, e exemplos reais de cálculo passando pelos serviços oficiais de
tributação PF e folha. Mais uma superfície web somente leitura de revisão da bateria agrupada por
tema, com os exemplos oficiais calculados ao vivo pelos serviços. Nenhum número legal entra no
código nesta rodada e nenhum parâmetro novo é criado: a bateria fecha o que já existe.

## Interpretação registrada: ancoragem do mundo do plano

No mundo do plano r66a0 o salário mínimo 2026 permanece R$ 1.518,00 (travado na R80 como
`SALARIO_MINIMO` com fonte citável) e as tabelas de tributação PF estão ancoradas nesse mínimo:
faixas INSS da Portaria Interministerial MPS/MF n. 6/2025 (Anexo II) e tabela mensal de IRRF da
Receita Federal vigente com esse mínimo (Lei 14.848/2024) - decisão registrada na R81 e mantida
aqui. No recon desta rodada (2026-10-22) o mundo externo confirma valores distintos para o ciclo
corrente (Portaria Interministerial MPS/MF n. 13/2026, publicada em 12/01/2026 no gov.br:
mínimo 1.621,00, teto de contribuição 8.475,55 com reajuste de 3,90%). O produto mantém a
ancoragem do plano: o contrato trava a consistência interna do mundo ancorado (derivativas
3x/2x mínimo, teto = última faixa, continuidade das parcelas), não o valor externo do dia.
Atualizar a âncora no futuro exige apenas um parâmetro novo com fonte citável por ano/migração -
a fonte única segue sendo a única dona dos números.

## Fontes oficiais consultadas

Recon desta rodada (websearch, 2026-10-22): Portaria Interministerial MPS/MF n. 13/2026 (gov.br/
previdencia, Anexo II/III) como referência externa do ciclo corrente; tabelas RFB de IRRF mensal.
As fontes travadas em contrato são as que cada linha carrega em `fonte_oficial` nas migrações
v2363 a v2372: MTE/MF diário oficial (mínimo), RIR Decreto-Lei 3.240/1941 art. 8 (piso 3x) e art.
788 (13o 2x), Portaria MPS/MF n. 6/2025 (INSS), Lei 14.848/2024 (IRRF e deduções), CLT art. 59
parágrafo único c/c EC 72/2013 e art. 7 inciso XIII (hora extra), CLT art. 487, II c/c Lei
12.506/2011 (aviso prévio), CF/88 art. 7, XIII (jornada 44h), CLT art. 74 parágrafo único e art.
58 (divisor/jornada 220h), Lei 5.107/1966 (FGTS 8%) e Lei 5.889/1973 art. 18 (multa 40%), LC
123/2006 anexos I a V (redação LC 155/2016) com limites ME/EPP (art. 3), IN RFB 2.110/2022
(DCTFWeb), Manual EFD ICms/IPI, MOS eSocial S-1.3 (catálogo de eventos e dia 15), CTN art. 194,
Lei 13.709/2018 (LGPD) e Lei 13.787/2018 art. 6 c/c Resolução CFM n. 1.821/2007 (retenções de
prontuário), BACEN Carta-Circular 2926 (fator/moeda do bloqueto), Especificação FEBEN ISO 20022
(camt.239.00/camt.240.00), Banco Central GUI Pix br.gov.bcb.pix e BR Code MCC 0000, Tabela de
códigos de UF do DF-e (Ajuste SINIEF 07/05) com ambiente SAT por Portaria SVS/ME n 159/2011, ISO
4217 (moedas) e dispensas de exportação Lei 8.080/1990 art. 39, II (ICMS) c/c Lei 4.502/1964 art.
15, III (IPI).

## Regras implementadas

- `IBateriaLegalService` / `BateriaLegalService` (Application/Legislacao +
  Infrastructure/Legislacao): partição dos parâmetros ativos de um ano em 10 temas fixos (tributa-
  ção PF, folha e rescisão, FGTS, Simples Nacional, calendário fiscal, eSocial e trabalhista, DF-e
  e SEFAZ, trilhos de pagamento, LGPD e venda internacional) com rótulo de apresentação por tema.
  O catálogo fixa apenas a classificação código -> tema: nenhum número legal vive nele. Parâmetro
  ativo futuro fora do catálogo cai no grupo de segurança "DIVERSOS" em vez de sumir da bateria;
  para 2026 o grupo é vazio (os 116 códigos estão todos classificados).
- API: `GET api/legislacao/parametros/bateria?ano=` no `LegislacaoController` existente da R80
  (rota literal precede a paramétrica `{codigo}`); demais rotas intocadas.
- Superfície web somente leitura: `LegislacaoWebController` (proxy padrão da casa
  `{ ok, data/erro }`) com ações `Bateria`, `TributacaoTabela` e `TributacaoCalcular` sobre
  `api/legislacao/parametros`; página em `Views/LegislacaoWeb/Index.cshtml` com a bateria agrupada
  por tema (código em mono, valor canônico, vigência, fonte completa no balão) e painel de
  exemplos reais (salário mínimo, teto e 5.000,00) calculados ao vivo pelo serviço oficial de
  tributação PF. Sem inputs, sem escrita: revisão e auditoria. Sidebar na seção de administração
  (ADM360) após Folha e Rescisões com ícone `Edit` - único no menu inteiro: o `Information` já
  pertence à entrada Observabilidade da administração global e o `Lock` ao rodapé LGPD, e o
  contrato R46 limita cada ícone a 1 uso por link fora dos 5 pares declarados. Rótulo
  `Nav.BateriaLegal` nos 4 locales ("Parâmetros Legais"/"Legal Parameters"/"Parámetros Legales"/
  "Paramètres légaux"). Artigo de ajuda `adm-parametros-legais` (ordem 450) aponta para a página.

## Bateria travada: 116 parâmetros ativos em 2026, 10 temas

| tema | parâmetros |
| --- | --- |
| Tributação PF (INSS, IRRF, tetos e piso) | 11 |
| Folha, horas extras e rescisão | 7 |
| FGTS | 3 |
| Simples Nacional (limites, DAS e 30 faixas de 5 anexos) | 34 |
| Calendário fiscal e obrigações | 8 |
| eSocial, jornada e aviso prévio | 7 |
| DF-e e SEFAZ por UF (4 layouts + 27 unidades) | 31 |
| Trilhos de pagamento | 6 |
| LGPD e prontuário | 6 |
| Venda internacional | 3 |
| total | 116 |

O primeiro fato da suíte exige o total exato (116), a partição sem perdas nem duplicatas, os 10
temas na ordem do catálogo, a ausência de grupo "DIVERSOS" em 2026, ano correto e fonte não vazia
em cada linha, e a igualdade com a contagem ativa direta no banco.

## Derivações cruzadas conferidas por contrato

- Piso mensal de isenção = 3 x salário mínimo (4.554,00 = 3 x 1.518,00); piso do 13o = 2 x mínimo
  (3.036,00); teto de contribuição = limite da INSS_FAIXA_4 (8.157,41); a primeira faixa INSS
  parte do próprio mínimo (1.518,00).
- Continuidade das "parcelas a deduzir" nas fronteiras de faixa (a fórmula da faixa N avaliada no
  limite da faixa N-1 bate com a fórmula da faixa N-1 no mesmo ponto, tolerância de 0,01 que é o
  arredondamento da parcela): INSS nas 3 fronteiras e IRRF nas 3 fronteiras. Discriminadores reais
  do arredondamento no conjunto: fronteira da 3a faixa INSS (4.190,83) dá 396,32 pela fórmula nova
  contra 396,31 pela anterior (diferença 0,01), e a da 4a faixa IRRF (4.664,68) dá 386,79 contra
  386,78 (diferença 0,01).
- O piso de isenção (4.554,00) fica acima da primeira faixa IRRF (2.826,65): a isenção prevalece
  sobre a tabela (regra da R81 re-travada). Deduções por dependente (189,59) e simplificada
  (564,80) positivas e abaixo do piso.
- Limites crescentes e alíquotas crescentes nas faixas INSS, IRRF e dos 5 anexos do Simples;
  classe ME (360.000,00) = fim da 2a faixa e EPP (4.800.000,00) = fim da 6a faixa em todos os
  anexos; faixas 2 a 5 do Simples contínuas entre si (a 6a, trecho exclusivo de EPP acima dos
  3,6M, é intencionalmente descontinua nesse mundo do plano - faixa punitiva - e fica fora da
  conferência, com o motivo registrado nos testes).
- Consistência entre duplicatas registradas: hora extra 50 (HORA_EXTRA_ACRESCIMO_PCT =
  HORA_EXTRA_ACRESIMO_MIN_PCT), divisor/jornada 220 (SALARIO_DIVISOR_MENSAL_HORAS =
  DIAS_MES_TRABALHADO_HORAS) e dias do DAS 20 (SIMPLES_DAS_DIA_VENCIMENTO = DAS_SIMPLES_VENCIMENTO_
  DIA = dia dentro de OBRIGACAO_FISCAL_DAS_SIMPLES); DCTFWeb 15 e FGTS 20 iguais entre parâmetro
  isolado (R80) e obrigação (R84).
- Aviso prévio: teto de 90 atingível pelos acréscimos de 3 dias/ano a partir da base de 30 em
  exatamente 20 anos completos (90-30)/3 = 20.
- Retenções LGPD em meses cheias de anos: fiscal 60 = 5 anos (CTN art. 194) e prontuário 240 = 20
  anos (Lei 13.787/2018 art. 6).

## Exemplos reais de cálculo travados (pelos serviços oficiais)

| cenário | bruto | INSS | base IRRF | IRRF | líquido |
| --- | --- | --- | --- | --- | --- |
| salário mínimo (trava R81 re-travada) | 1.518,00 | 113,85 | 1.404,15 | 0,00 | 1.404,15 |
| salário 5.000,00 (base abaixo do piso) | 5.000,00 | 509,60 | 4.490,40 | 0,00 | 4.490,40 |
| teto de contribuição (trava R81 re-travada) | 8.157,41 | 951,64 | 7.205,77 | 1.085,59 | 6.120,18 |

Folha real (trava R82 re-travada pela bateria): 5.000,00 base + 10 horas extras (50% sobre hora
normal de 220h = 340,91) + 1 dependente = bruto 5.340,91, INSS 557,33, base IRRF 4.593,99 (acima
do piso pela primeira vez), IRRF 370,88, líquido 4.412,70 e FGTS depositado 427,27 (8%).

## Achados registrados (observados e travados, sem mudança nesta rodada)

- Colisão de ícone capturada pela própria suíte na primeira execução completa: o recon havia
  marcado o `Information` como livre sem ver a entrada Observabilidade da seção de administração
  global (adicionada por rodada anterior). Troca para `Edit` (zero usos em todo o arquivo do
  menu, resolve no registry do design system) sem tocar nos 5 pares declarados do contrato R46
  nem no item de outra rodada.
- Duplicidade de hora extra: `HORA_EXTRA_ACRESCIMO_PCT` (R82, fonte CLT art. 7 XIII) e
  `HORA_EXTRA_ACRESIMO_MIN_PCT` (R88, fonte CLT art. 59 parágrafo único) convivem com
  consumidores diferentes; a bateria trava ambos e exige equivalência numérica.
- Duplicidade do dia do DAS: `DAS_SIMPLES_VENCIMENTO_DIA` (R80) e `SIMPLES_DAS_DIA_VENCIMENTO`
  (R83) - mesmo dia 20, fontes distintas; a bateria exige igualdade.
- eSocial S-1200 no dia 7 (calendário fiscal R84, "fixado no plano") convive com eventos
  periódicos genéricos no dia 15 (MOS R88): datas diferentes para visões diferentes, ambas citáveis;
  a bateria trava os dois valores sem exigir igualdade.
- Ancoragem do mínimo (1.518 do plano vs 1.621 da Portaria 13/2026 externa): interpretada acima.

## Mudanças desta rodada

- `backend/PlantaoPro.Application/Legislacao/IBateriaLegalService.cs`: DTOs da bateria (grupo por
  tema, total, partição) + serviço de leitura.
- `backend/PlantaoPro.Infrastructure/Legislacao/BateriaLegalService.cs`: catálogo de 10 temas com
  os 116 códigos de 2026 classificados + rede de segurança "DIVERSOS".
- `backend/PlantaoPro.Api/Program.cs`: DI do serviço no padrão da casa (fonte única injetada).
- `backend/PlantaoPro.Api/Controllers/LegislacaoController.cs`: rota `GET bateria?ano=` (R80/R81
  intocadas).
- `backend/PlantaoPro.Web/Controllers/LegislacaoWebController.cs`: proxy somente leitura
  (bateria, tabela PF, cálculo PF) com contrato `{ ok, data/erro }`.
- `backend/PlantaoPro.Web/Views/LegislacaoWeb/Index.cshtml`: bateria agrupada por tema + exemplos
  reais ao vivo; tabelas em `.table-responsive` com `th scope="col"` (contratos R45/R59), sem
  inputs/escrita.
- `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml`: item Parâmetros Legais (ADM360) após
  Folha e Rescisões, ícone Edit único no menu (contrato R46).
- `backend/PlantaoPro.Web/Localization/UiTexts{,.en,.es,.fr}.resx`: chave `Nav.BateriaLegal` nos
  4 locales.
- `backend/PlantaoPro.Web/Controllers/AjudaController.cs`: artigo `adm-parametros-legais`
  (ordem 450) apontando para LegislacaoWeb/Index - a nova página entra já coberta.
- `backend/PlantaoPro.Tests/R90ContratosLegaisContratoTests.cs`: 11 fatos - partição completa dos
  116 parâmetros sem perda/duplicata; bateria PF linha a linha com fonte citável e vigência 2026;
  derivações cruzadas matematicamente consistentes; folha/FGTS/rescisão com duplicatas registradas
  e distinção fixos x anuais; Simples Nacional com 30 faixas exatas, limites ME/EPP e continuidade
  cumulativa; calendário fiscal no formato periodicidade|offset|dia|regime com dias cruzados;
  eSocial/trabalho com teto de aviso atingível e catálogo de 14 eventos; DF-e/SEFAZ com 27
  unidades, códigos SINIEF distintos e ambientes SAT exatos; trilhos e LGPD com fontes citáveis;
  venda internacional com dispensas de exportação; exemplos reais de cálculo pelos serviços.
- Sem migração nesta rodada: os 116 parâmetros já existem nas migrações v2363 a v2372 (rodadas
  80-89) com fonte citável e a bateria apenas os trava - manifesto permanece com 143 entradas.

## Suíte e gates

- Suíte completa cresce de 1764 para 1775 aprovados / 0 falhas (apps parados nas portas
  51976/51977/52976/52977 antes da execução; a primeira execução pegou o helper de parsing da
  própria bateria com `NumberStyles.None`, que não admite ponto decimal - corrigido para
  `AllowDecimalPoint` antes do gate final, suíte final limpa).
- Build A/B Release `--no-incremental`: base 81 avisos, nova 81 avisos, 0 erros nos dois,
  DIFF=NENHUMA (dedupe: 76 únicos de cada lado; logs `%TEMP%\opencode\r90_build_base.txt` /
  `r90_build_nova.txt`).
- Inventário rerun no mesmo ciclo que adiciona página + artigo: páginas 683 (+1), artigos 91
  (+1), cobertas 89 (+1), gap 517 (imutável - a nova página entra já coberta, trava R75 mantida),
  aux 29.
- Docscan `docs/evidencias/2026-10-19-rodada-62/r62_docscan.ps1` reexecutado após o documento
  final: 0 defeitos.
- Higiene `RepositoryHygieneContractTests` 8/8 após o documento.

## Próxima rodada

R91 (abre o bloco B5 - escala, performance e confiabilidade): paginação/virtualização das listas
densas (Orçamentos 17,2 MB, Estoque 8,75 MB - pendência 1 do §7 ainda aberta).
