# R81a0 - Nucleo de tributacao pessoa fisica 2026 (vetores oficiais na fonte unica)

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R81 do bloco B4.
Escopo desta rodada: nucleo de tributacao PF 2026 (INSS progressivo, IRRF mensal, teto,
salario minimo) com contrato travado por vetores oficiais, tudo lido da fonte unica
`plantaopro.parametros_legais` criada na rodada 80. Nenhum numero legal vive no codigo.

## Fontes oficiais consultadas

- INSS 2025/2026: Portaria Interministerial MPS/MF n. 6/2025 (tabela progressiva por
  "parcela a deduzir", Anexo II; teto do salario-de-contribuicao).
- IRRF mensal: tabela da Receita Federal vigente ancorada ao salario minimo de
  1.518,00 (Lei 14.848/2024), publicada em gov.br/receitafederal (tabelas 2025).
- Piso de isencao mensal: PISO_ISENCAO_IRRF_MENSAL = 4.554,00 (3 x minimo, RIR art. 8),
  semeado pela rodada 80 e mantido como unica fonte da isencao.

### Ancoragem do mundo do plano

No mundo do plano r66a0 o salario minimo 2026 permanece 1.518,00 (travado em
SALARIO_MINIMO pela rodada 80). A tabela oficial da previdencia com esse minimo e a da
Portaria MPS/MF n. 6/2025, e a tabela mensal de IRRF com esse minimo e a vigente da Lei
14.848/2024. A cada mudanca de minimo futuro, uma nova migration insere as linhas do novo
ano (o indice unico parcial `(codigo, ano)` da tabela ja permite varios anos ativos ao
mesmo tempo). A R90 fecha a bateria de contratos legais sobre o conjunto.

## Regras de calculo implementadas (TributacaoPfService)

- Validacao: salario bruto deve ser maior que zero (senao erro explicito 400).
- ANO: se nao informado, o servico usa o maior ano presente na fonte unica (hoje 2026).
- INSS: base = min(bruto, teto 8.157,41); primeira faixa cujo limite cubra a base;
  desconto = aliquota x base - parcela a deduzir; arredondamento comercial (2 casas).
- IRRF: base = bruto - INSS; isento enquanto a base for menor ou igual ao piso da fonte
  unica (4.554,00); acima, mesma formula de parcela a deduzir (faixa final ilimitada).
- Dependentes, deducao simplificada e eventos de 13o/ferias ficam na R82 (folha).

### Vetores semeados (migration v2364, ano 2026)

| codigo | valor (limite|aliquota_pct|parcela) |
| --- | --- |
| INSS_TETO_CONTRIBUICAO | 8157.41 |
| INSS_FAIXA_1 | 1518.00\|7.5\|0.00 |
| INSS_FAIXA_2 | 2793.88\|9.0\|22.77 |
| INSS_FAIXA_3 | 4190.83\|12.0\|106.59 |
| INSS_FAIXA_4 | 8157.41\|14.0\|190.40 |
| IRRF_FAIXA_1 | 2826.65\|7.5\|169.44 |
| IRRF_FAIXA_2 | 3751.05\|15.0\|381.44 |
| IRRF_FAIXA_3 | 4664.68\|22.5\|662.77 |
| IRRF_FAIXA_4 | (sem teto)\|27.5\|896.00 |

Limite vazio = faixa final sem teto. Formato documentado no comment da coluna valor.

## Exemplos oficiais travados em contrato

| bruto | base INSS | INSS | base IRRF | IRRF | total | liquido |
| --- | --- | --- | --- | --- | --- | --- |
| 1.518,00 (minimo) | 1.518,00 | 113,85 | 1.404,15 | 0,00 | 113,85 | 1.404,15 |
| 2.793,88 (limite faixa 2) | 2.793,88 | 228,68 | 2.565,20 | 0,00 | 228,68 | 2.565,20 |
| 5.000,00 | 5.000,00 | 509,60 | 4.490,40 | 0,00 | 509,60 | 4.490,40 |
| 8.157,41 (teto) | 8.157,41 | 951,64 | 7.205,77 | 1.085,59 | 2.037,23 | 6.120,18 |
| 12.000,00 (acima do teto) | 8.157,41 | 951,64 | 11.048,36 | 2.142,30 | 3.093,94 | 8.906,06 |

Nota: com piso de isencao em 4.554,00 aplicado sobre (bruto - INSS), um salario de
5.000,00 tem IRRF zerado - consequencia direta da fonte unica, registrada aqui e
travada no teste.

## Mudancas desta rodada

- `database/migrations/2026_10_v2364_r81_tabelas_pf_2026.sql`: insere os 9 vetores
  (INSS teto + 4 faixas, IRRF 4 faixas) idempotente (on conflict do nothing); fonte
  oficial citavel em toda linha; atualiza o comment do formato da coluna valor.
- `database/migration-manifest.json`: entrada v2364 com checksum SHA256 real
  (normalizado CRLF->LF, hex minusculo) e dependencia em v2363.
- `backend/PlantaoPro.Application/Legislacao/ITributacaoPfService.cs`: contrato
  (ObterTabelaAsync / CalcularAsync) + DTOs (FaixaTributoLegalDto, TabelaTributacaoPfDto,
  CalculoTributacaoPfDto, CalcularTributacaoPfCommand).
- `backend/PlantaoPro.Infrastructure/Legislacao/TributacaoPfService.cs`: servico que
  carrega exclusivamente da fonte unica e aplica as regras acima; ano sem vetores ou
  codigo ausente viram erro explicito citando a fonte unica.
- `backend/PlantaoPro.Api/Controllers/LegislacaoController.cs`: dois endpoints novos
  sob `api/legislacao/parametros`:
  - `GET tributacao-pf/tabela?ano=` -> tabela vigente (minimo, piso, teto, faixas);
  - `POST tributacao-pf/calcular` `{salarioBruto, ano?}` -> quebra liquida completa;
    erros 400 com mensagem em linguagem direta.
- `backend/PlantaoPro.Api/Program.cs`: registro do servico (DI no padrao da casa).
- `backend/PlantaoPro.Tests/R81TributacaoPfContratoTests.cs`: 3 fatos -
  (1) vetores 2026 semeados com fonte citavel, conferidos linha a linha;
  (2) contrato de vetores oficiais pelos 5 exemplos travados acima + tabela exposta;
  (3) validacao de salario (zero/negativo) e ano sem vetores (1999).

## Suíte e gates

- Suíte cresce de 1699 para 1702 aprovados (apps parados nas portas 51976/51977/52976/52977).
- Build A/B Release `--no-incremental`: base 81 avisos, nova 81 avisos, DIFF=NENHUMA.
- Docscan `docs/evidencias/2026-10-19-rodada-62/r62_docscan.ps1` reexecutado apos o
  documento final, delta zero.
- Higiene `RepositoryHygieneContractTests` 8/8 apos o documento.

## Proxima rodada

R82 - folha de pagamento funcional (13o, ferias + 1/3, horas extras, rescisao com
verbas): servico de calculo em cima deste nucleo + tela simples explicada em linguagem
leiga (reaproveita GET tabela e POST calcular via UI).
