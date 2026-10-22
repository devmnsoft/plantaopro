# R85a0 - DF-e nacional: configuração SEFAZ por UF, layouts oficiais e chave de acesso módulo 11

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R85 do bloco B4.
Escopo desta rodada: configuração SEFAZ por UF, layouts (NF-e 4.0, NFC-e, CF-e-SAT, CF-e-Serviços
3.0) e chave de acesso mod-11 com dígito verificador por UF. O catálogo institucional (os 27
códigos oficiais cUF com os documentos apoiados em cada estado e as versões vigentes dos layouts)
é semeado na fonte única `plantaopro.parametros_legais` (v2368); o serviço `DfeNacionalService`
gera e valida a chave de 44 dígitos com a aritmética oficial do módulo 11; a API
`api/dfe-nacional` e a tela web expõem tudo em linguagem leiga. Nenhum valor da chave vive fora
da fonte única ou da composição oficial.

O mapa da R80 já havia registrado o que existia (parser de XML de recebimento e validação só de
formato/modelo 55/65) e o que faltava exatamente este escopo: configuração SEFAZ por UF, layouts
e cálculo/geração do DV. As regras de recebimento da R4/R24 (`XmlDocumentoRegras`,
`FiscalPreEmissoes`) permanecem intatas; esta rodada adiciona a camada de configuração e do DV
sem alterar contrato existente.

## Fontes oficiais consultadas

- Códigos cUF: tabela de códigos de UF do DF-e (Ajuste SINIEF 07/05; Manual de Orientação do
  Contribuinte da NF-e - gov.br/nfe). Estáveis desde o lançamento da NF-e: RO 11, AC 12, AM 13,
  RR 14, PA 15, AP 16, TO 17, MA 21, PI 22, CE 23, RN 24, PB 25, PE 26, AL 27, SE 28, BA 29, MG 31,
  ES 32, RJ 33, SP 35, PR 41, SC 42, RS 43, MS 50, MT 51, GO 52, DF 53.
- Dígito verificador: Manual de Orientação do Contribuinte da NF-e v4.00 - composição da chave de
  acesso (gov.br/nfe): corpo de 43 dígitos; pesos 2 a 9 aplicados da direita para a esquerda,
  reiniciando em 2 quando o peso 9 é atingido; se o resto da divisão por 11 for 0 ou 1 o DV vale 0;
  nos demais casos DV = 11 - resto.
- Layouts vigentes: Manual NF-e (layout XML v4.00 - gov.br/nfe); Manual NFC-e (layout XML v4.00 -
  gov.br/nfce); Manual CF-e SAT / Portaria SVS ME nº 159/2011 (gov.br/sat); Manual CF-e Serviços
  3.0 - DREX (gov.br/notasfiscais).
- Ambientes SAT: Portaria SVS/ME nº 159/2011 e atos de extensão - na data da rodada os ambientes
  ativos são AC, AP, CE, PA, PB, PE e RR (consulta pública SEFAZ Nacional - gov.br/solucoes).
  Estado pode migrar de volta para a NFC-e nacional por ato próprio: o conjunto de documentos
  apoiados por UF é dado operacional datado (out/2026), não constante legal imutável.
- CPF de exemplo 111.444.777-35: exemplo oficial utilizado pela RFB em seus manuais (serviço de
  conferência de inscrição).

## Regras implementadas (DfeNacionalService)

- Composição oficial da chave (44 dígitos): cUF(2) + AAMM(4) + inscrição do emitente(14, CPF
  zerado à esquerda) + modelo(2) + série(3) + número da nota(9) + tipo de emissão(1) + código
  numérico aleatório(8) + DV(1).
- "DV por UF": o cUF faz parte do corpo sobre o qual o módulo 11 é calculado, então trocar o
  estado muda o dígito verificador; a validação também confere se o cUF está no catálogo oficial
  (27 UFs) e se aquela UF apoia o modelo presente na chave. É isso que personaliza o DV por UF,
  sem que o algoritmo mude.
- Modelos geridos: 55 (NF-e, todas as UFs), 65 (NFC-e, todas exceto ambientes SAT), 59 (CF-e SAT,
  somente UFs SAT) e 67 (CF-e Serviços, federal - todas as UFs). Qualquer outro modelo é rejeitado
  com mensagem citando os quatro geridos.
- Faixas oficiais: ano 2000..2099 (AAMM), série 1..999, número da nota 1..999999999, tipo de
  emissão 1..9, código numérico 0..99999999 (oito dígitos), inscrição 11 dígitos (CPF) ou
  14 (CNPJ) com ou sem pontuação.
- Geração: UF fora do catálogo = erro de contrato (400); demais falhas de faixa/modelo voltam no
  resultado com a lista de erros individuais e sem chave. Sucesso devolve a chave completa, a
  decomposição em nove segmentos nomeados e o detalhe do cálculo (soma ponderada, resto e regra
  aplicada).
- Validação: formato (exatamente 44 dígitos numéricos) → cUF contra o catálogo → DV recalculado x
  informado (mensagem traz os dois valores) → mês do AAMM → modelo gerido e apoio na UF da chave
  → faixas de série/número/tipo. Cada falha aparece individualmente.

## Parâmetros semeados (migration v2368)

- 27 linhas `SEFAZ_UF_<UF>` com valor canônico `cUF|DOC,DOC,...` (DOC em {NFE, NFC-E, CFE_SAT,
  CFE_SERVICOS}) e fonte citável por linha. Regra fixada em out/2026: NFE e CFE_SERVICOS são
  nacionais; UFs comuns apoiam NFE, NFC-E e CFE_SERVICOS; UFs SAT (AC, AP, CE, PA, PB, PE, RR)
  apoiam NFE, CFE_SAT e CFE_SERVICOS (o CF-e SAT substitui a NFC-e nesses estados).
- 4 linhas de layout vigente no ano 2026: `DFE_LAYOUT_NFE` = 4.0, `DFE_LAYOUT_NFCE` = 4.0,
  `DFE_LAYOUT_CFE_SAT` = 1.00, `DFE_LAYOUT_CFE_SERVICOS` = 3.00, cada uma com a fonte do manual.
- Idempotente (`on conflict (codigo, ano) ... do nothing`), manifesto v2368 com checksum SHA256
  real e dependência em v2367; comentário de coluna amplia a convenção do campo `valor` com os
  dois formatos novos.

## Exemplos travados em contrato (R85DfeContratoTests)

Vetores aritméticos calculados da especificação oficial (soma ponderada e resto conferidos em
duas implementações independentes dentro do contrato):

| vetor | entrada | corpo (43) | resto | DV | chave (44) |
| --- | --- | --- | --- | --- | --- |
| V1 (rest 0) | SP, 10/2026, CNPJ 11.222.333/0001-81, mod 55, s. 1, n. 1, te 1, CNF 12345678 | 3526101122233300018155001000000001112345678 | 0 | 0 | 35261011222333000181550010000000011123456780 |
| V2 (rest 1) | SP, 10/2026, CNPJ 11.222.333/0001-81, mod 55, s. 1, n. 2, te 1, CNF 14 | 3526101122233300018155001000000002100000014 | 1 | 0 | 35261011222333000181550010000000021000000140 |
| V3 (rest 6) | MG, 01/2026, CPF 111.444.777-35, mod 55, s. 7, n. 999, te 2, CNF 12345 | 3126010001114447773555007000000999200012345 | 6 | 5 | 31260100011144477735550070000009992000123455 |

- V1 trava a regra "resto 0 resulta em DV 0" e a decomposição em segmentos (cUF 35/SP, AAMM 2610,
  registro 11222333000181, NNF 000000001, CNF 12345678).
- V2 trava a segunda face da regra especial ("resto 1 resulta em DV 0") no mesmo prefixo.
- V3 trava o CPF de 11 dígitos entrando zerado à esquerda no corpo (`00011144477735`) e o caso
  regular `DV = 11 - resto` (11-6=5).
- Validação travada: chaves válidas por UF (SP e MG), DV errado reportando "calculado 0,
  informado 7", 43 dígitos, caractere não numérico, cUF 99 (fora do catálogo, com DV corretamente
  recalculado para isolar o erro de UF) e modelo 57 (CT-e) fora dos layouts geridos.
- Faixas e apoio por UF travadas: série 0/1000, número 0/10^9, tipo 0/10, CNF -1/10^8, inscrição
  10/15 dígitos, UF ZZ (erro de contrato), NFC-e em CE (ambiente SAT) rejeitada, CF-e SAT em RJ
  rejeitado, CF-e SAT em CE aceito (chave nascendo com cUF 23) e CF-e Serviços em SP aceito.

## Mudanças desta rodada

- `database/migrations/2026_10_v2368_r85_dfe_nacional.sql`: 27 linhas `SEFAZ_UF_<UF>` + 4 linhas
  `DFE_LAYOUT_*` idempotentes; comentários ampliam a convenção do campo `valor`.
- `database/migration-manifest.json`: entrada v2368 (checksum SHA256 real, dependência v2367).
- `backend/PlantaoPro.Application/Legislacao/IDfeNacionalService.cs`: contrato com os registros
  `DfeUfInfo`, `DfeLayoutInfo`, `GerarChaveCommand`, `ValidarChaveCommand`, `ChaveSegmento`,
  `ChaveAcessoDfe` e `DfeChaveAcessoResultado`.
- `backend/PlantaoPro.Infrastructure/Legislacao/DfeNacionalService.cs`: catálogo lido da fonte
  única (27 UFs ordenadas por cUF, layouts em ordem fixa) e o módulo 11 oficial (pesos 2..9 da
  direita para a esquerda, resto 0 ou 1 => DV 0) com geração, validação e detalhe do cálculo em
  texto legível.
- `backend/PlantaoPro.Api/Controllers/DfeNacionalController.cs`: `GET api/dfe-nacional/ufs`,
  `GET layouts`, `POST chave-acesso/gerar`, `POST chave-acesso/validar`; sessão autenticada
  exigida (configuração global, sem tenant); falha de contrato = 400 com mensagem direta.
- `backend/PlantaoPro.Api/Program.cs`: DI no padrão da casa.
- `backend/PlantaoPro.Web/Controllers/DfeNacionalWebController.cs`: proxy BFF `{ ok, data/erro }`
  (mesmo padrão da Folha R82, Simples R83 e Calendário Fiscal R84).
- `backend/PlantaoPro.Web/Views/DfeNacionalWeb/Index.cshtml`: formulário de geração rotulado
  campo a campo com decomposição da chave em segmentos e aritmética exibida; painel de validação
  de chave recebida com cada erro individual listado; tabela das 27 UFs com selos de documentos
  apoiados (sempre com texto - cor nunca é o único canal) e tabela dos layouts vigentes com
  fonte oficial; bloco "Em linguagem simples" explicando a estrutura da chave, o módulo 11, o DV
  por UF e os ambientes SAT, com aviso de que a ferramenta não substitui a autorização junto à
  SEFAZ.
- `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml`: entrada "DF-e Nacional" (módulo ADM360,
  ícone Upload - envio do documento ao autorizador: entre os ícones livres do registro nenhum dos
  com semântica de documento fiscal (Document, Print, Audit, Report, Attachment) estava disponível
  como par da R46 sem reuso, e Upload é o mais próximo do ato de transmitir ao fisco).
- `backend/PlantaoPro.Web/Localization/UiTexts*.resx` (4 locales): chave Nav.DfeNacional.
- `backend/PlantaoPro.Web/Controllers/AjudaController.cs`: artigo adm-dfe-nacional (ordem 410)
  apontando para DfeNacionalWeb/Index - a nova página entra já coberta.
- `docs/evidencias/2026-10-20-rodada-75/r75_inventario.txt`: rerun do inventário após a mudança:
  páginas 678 (+1), artigos 86 (+1), cobertas 84 (+1), gap mantido em 517, nenhum artigo órfão.
- `backend/PlantaoPro.Tests/R85DfeContratoTests.cs`: 7 fatos - manifesto v2368 com hash real e
  dependência; catálogo das 27 UFs linha a linha (cUF oficial, regra SAT/NFC-E, fonte citável) e
  layouts com versão/fonte; geração reproduzindo o vetor travado com decomposição; regras
  especiais do DV (restos 0, 1 e 6) com confiança cruzada de implementação; validação de DV/UF/
  formato/modelo; faixas oficiais e apoio de modelo por UF; exposição do catálogo ordenado pelo
  serviço.

## Suíte e gates

- Suíte cresce de 1723 para 1730 aprovados / 0 falhas (apps parados nas portas
  51976/51977/52976/52977 antes da execução).
- Build A/B Release `--no-incremental`: base 81 avisos, nova 81 avisos, 0 erros nos dois,
  DIFF=NENHUMA.
- Gap de manuais mantido travado em 517 (nova página + novo artigo no mesmo ciclo; inventário
  versionado atualizado).
- Docscan `docs/evidencias/2026-10-19-rodada-62/r62_docscan.ps1` reexecutado após o documento
  final: 0 defeitos.
- Higiene `RepositoryHygieneContractTests` 8/8 após o documento.

## Próxima rodada

R86 - trilhos de pagamento: remessa CNAB 400/240, FEBEN e PIX EMV (validação + geração) amarrados
ao motor de cobrança.
