# R86a0 - Trilhos de pagamento: PIX EMV, boleto e remessa CNAB 400 amarrados ao motor de cobrança

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R86 do bloco B4.
Escopo desta rodada: transformar as faturas abertas do motor de cobrança SaaS nos formatos que os
bancos aceitam, sem nenhuma integração bancária real ainda: PIX copia-e-cola EMV (geração e
validação com CRC16), boleto (código de barras 44, linha digitável 47 e fator de vencimento com
rollover) e arquivo CNAB 400 de remessa, além do catálogo FEBEN ISO 20022 institucionalizado na
fonte única. A configuração dos trilhos (banco, conta cedente, nosso número base, chave PIX e
beneficiário) vive em tabela global - a conta que recebe é da operação que emite as faturas, não
por cliente. A emissão XML FEBEN, o CNAB 240 e o SPI/DCI do Pix entram no marco de homologação
externa com credenciais de banco (mesma regra do produto desde o B6: nenhum banco real conectado).

O motor de cobrança (faturas_saas, gerado pela R4/R82 e cobrado pela R85 de DF-e em outra camada)
já existia; esta rodada adiciona a camada de trilhos por cima sem alterar contrato existente.

## Fontes oficiais consultadas

- BACEN Carta-Circular 2926 (25/07/2000) - boleto: código de barras de 44 posições = banco(3) +
  moeda(1) + DV módulo 11(1) + fator de vencimento(4) + valor(10 centavos) + campo livre(25 =
  agência 4 + carteira 2 + nosso número 11 + conta do cedente 7 + zero 1); linha digitável de 47
  = três blocos com seus próprios DV módulo 10 + DV geral módulo 11 + fator + valor; módulo 11
  com pesos 2 a 9 da direita para a esquerda reiniciando em 2 (resto 0, 1 ou 10 resulta em DV 1);
  módulo 10 com pesos 2 e 1 somando os dígitos do produto (DV = (10 - resto) mod 10); fator de
  vencimento contado a partir de 07/10/1997, com a tabela oficial de correlação encerrada em
  21/02/2025 (fator 9999) e, depois disso, rollover: fator exibido = dias totais mod 10000.
  Reprodução citável dos exemplos oficiais da CC 2926: MACORATTI, "O Boleto à vista" (net_bol.htm).
- QR Code BR / BR Code (BACEN com EMVCo, Especificação Pix): payload TLV em LVL; GUI
  br.gov.bcb.pix no campo 26>00; MCC 0000 (campo 52) para Pix sem categoria de comércio; moeda
  986 (campo 53); país BR (campo 58); nome do beneficiário com até 25 caracteres e cidade com
  até 15, ambos sem acento (campos 59 e 60); valor com ponto decimal (campo 54); identificador
  da transação de até 35 caracteres alfanuméricos maiúsculos (62>05); CRC16-CCITT (início 0xFFFF,
  polinômio 0x1021) calculado sobre todos os dados precedentes incluindo o literal 6304 -
  conferência cruzada pública de um payload real termina em `63041D3D` com o mesmo CRC sobre o
  prefixo.
- Manual Febraban CNAB 400 - remessa de cobrança: arquivo ASCII com linhas de exatamente 400
  posições separadas por CRLF; cabeçalho 01RECERMISSAO com banco @14(3), agência @17(4), conta do
  cedente alinhada à esquerda @22(11) e data ddMMyyyy @49(8); registro de título com nosso número
  D17 @52(17), linha número (o código de barras) @75(44), vencimento ddMMyyyy @124(8) e valor
  D15 em centavos @132(15); trailer com contagem D3 @77(3), soma D15 @132(15) e preenchimento de
  253 espaços até fechar as 400 posições.
- FEBEN (ISO 20022, Banco Central): mensagens de gestão de lote de cobrança camt.239.00
  (ManagementAdvice, remessa) e camt.240.00 (Management Response Advice, retorno). Nesta rodada o
  catálogo entra como parâmetro institucional citável; a emissão XML e o SPI/DCI seguem no marco
  de homologação externa.

## Regras implementadas

- `TrilhosPagamentoRegras` (aritmética pura, sem banco): CRC16 com valor público conferido
  (`Crc16Ccitt("123456789")` = `29B1`); geração do payload EMV estático e dinâmico fechando com
  `6304` + CRC de todo o dado precedente (conferência cruzada acima); validação do payload
  recebido (estrutura TLV, campos obrigatórios, nome/cidade sem acento, valor com ponto decimal
  e os quatro dígitos finais iguais ao CRC recalculado sobre o prefixo); dígitos verificadores
  módulo 11 e módulo 10 da CC 2926; fator de vencimento desde 1997-10-07 com rollover após
  2025-02-21; composição do código de barras 44 e da linha digitável 47; validação da linha
  digitável recebida (remove espaços e pontos, confere os três DVs módulo 10, o DV geral módulo
  11, exige moeda 9 e extrai fator e valor, estimando o vencimento pela janela
  base + (fator + k*10000) dias).
- `TrilhosPagamentoService`: configuração global de linha única (upsert) com validações explícitas
  de contrato (código do banco em 3 dígitos, agência 4, carteira 2, conta do cedente em 7, nosso
  número dentro do campo de 11, nome/cidade sem acento e nos limites do BR Code) que respondem
  400 sem persistir nada; PIX estático e dinâmico por fatura com txid derivado do id da fatura
  (forma N maiúscula, máx. 25) e idempotente por construção; remessa CNAB 400 somente das faturas
  candidatas (`reg_status='A'` e status ABERTA/VENCIDA e valor maior que zero), com nosso número
  sequenciado a partir da base pela posição da fatura no arquivo (ordem por vencimento e id),
  um registro por título e trailer fechando com contagem e soma dos registros. Faturas já pagas
  recusam novo payload com 409; inexistentes com 404; ausência de configuração responde 404 com
  mensagem direta em toda a superfície de geração.
- API `api/trilhos-pagamento` exigindo sessão autenticada; BFF web `{ ok, data/erro }` no padrão
  das rodadas anteriores; tudo em linguagem leiga na tela.

## Parâmetros semeados (migration v2369)

- Tabela global `cobranca_trilhos_config` no padrão de `cobranca_providers` (sem dimensão de
  tenant), com restrições amarradas às especificações citáveis: conta cedente em exatamente 7
  dígitos (posições 37-43 do campo livre, CC 2926; sai alinhada à esquerda em campo de 11 no CNAB
  400), nome do beneficiário em até 25 e cidade em até 15 (limites oficiais dos campos 59 e 60
  do BR Code), nosso número base cabendo nos campos de 11 e 17 dígitos.
- 6 linhas `TRILHO_*` em `parametros_legais`, cada uma com fonte citável: `TRILHO_PIX_GUI` =
  br.gov.bcb.pix, `TRILHO_PIX_MCC_PADRAO` = 0000, `TRILHO_BOLETO_MOEDA` = 9,
  `TRILHO_BOLETO_BASE_FATOR_DIA` = 1997-10-07 (com a regra de rollover documentada na própria
  descrição), `TRILHO_CNAB_REMESSA_LAYOUT` = 400 e `TRILHO_FEBEN_DOCUMENTOS` =
  camt.239.00|camt.240.00. Idempotente; manifesto v2369 com checksum SHA256 real e dependência
  em v2368; comentário da coluna `valor` amplia a convenção com o formato "AAAA-MM-DD" para dia
  base e documento ISO separado por "|".

## Exemplos travados em contrato (R86TrilhosContratoTests, 7 fatos)

Vetores de dígito verificador da CC 2926 (os mesmos do exemplo oficial, aritmética conferida):

| vetor | corpo | soma ponderada | resto | DV |
| --- | --- | --- | --- | --- |
| mod 11 oficial | 7999123200000066007878130875503042541200000 | 720 | 5 | 6 |
| mod 10 oficial | 999977721 | 57 | 7 | 3 |

Linha digitável oficial da CC 2926 rodada de volta contra o gerador: `99997.77213 30530.150082
18975.000003 1 10010000035000` reconstrói o código de barras `9999110010000035000777213053015008
1897500000` (DV geral 1) e o valor lido do final da linha dá R$ 350,00.

Fator de vencimento - pontos da tabela oficial mais o rollover: 2000-07-03→1000, 2000-07-05→1002,
2002-12-01→1881, 2007-03-29→3460, 2011-11-11→5148, 2025-02-21→9999 (fim da tabela) e, depois,
2026-10-30→dias totais 10615 exibido `0615` e 2026-11-15→10631 exibido `0631`.

Boleto gerado pelo produto (banco 237, moeda 9, agência 0001, carteira 01, conta 1234567):

| vetor | nosso | vencimento | valor | linha digitável exibida |
| --- | --- | --- | --- | --- |
| A | 7001 | 2026-10-30 | R$ 150,00 | 23790.00108 10000.000702 01123.456707 7 06150000015000 |
| B | 7002 | 2026-11-15 | R$ 234,56 | 23790.00108 10000.000702 02123.456705 4 06310000023456 |

Validação de linha digitável trava o caminho feliz (hoje 2026-10-22, vetor A devolve vencimento
provável 2026-10-30 e 15000 centavos) e dois tamperes isolados: corromper o último dígito do
bloco falha no módulo 11 do bloco; trocar a moeda da posição 3 falha na moeda.

PIX travado nas duas formas geradas pelo produto (configuração de exemplo: chave
plantaopro@exemplo.com.br, beneficiário PLANTAOPRO LTDA em CURITIBA):

- Estático (117 caracteres): `00022626470014br.gov.bcb.pix0125plantaopro@exemplo.com.br52040000
  53039865802BR5915PLANTAOPRO LTDA6008CURITIBA6304FCB5`.
- Dinâmico (160 caracteres) da fatura fixa `1c8b7a20-0001-4a56-b7c8-000000000001` (R$ 150,00):
  `00022626470014br.gov.bcb.pix0125plantaopro@exemplo.com.br5204000053039865406150.005802BR5915P
  LANTAOPRO LTDA6008CURITIBA622905251C8B7A2000014A56B7C800000630401D0`, com txid
  `1C8B7A2000014A56B7C800000` derivado do id.
- Validação do payload: caminho feliz extrai GUI, chave, MCC, moeda, país, nome, cidade, valor e
  txid; rejeita payload vazio, truncado, com tag trocada, sem país, com acento no nome do
  beneficiário e com vírgula no valor - cada pendência listada individualmente.

Remessa CNAB 400: contagem e total pré-calculados das faturas candidatas da base (o fato é
hermético contra resíduos globais); todas as linhas com exatamente 400 posições; cabeçalho com
banco/agência/conta/data nas posições do manual; trailer com contagem e soma batendo e 253
espaços de preenchimento; registros das faturas da rodada com linha número, vencimento e valor
independentes e nosso número por posição; separador CRLF conferido no split do conteúdo.

Configuração: 404 honesto sem config em obter/gerar/remessa; validações 400 sem persistir
(mensagem cita "código do banco", "conta do cedente" ou "sem acento"); upsert mantém sempre uma
única linha; PIX estático e dinâmico saem idênticos aos vetores acima; fatura paga responde 409
e inexistente 404.

## Mudanças desta rodada

- `database/migrations/2026_10_v2369_r86_trilhos_pagamento.sql`: tabela global de configuração
  com restrições das especificações + 6 parâmetros `TRILHO_*` idempotentes; comentários ampliam a
  convenção da coluna `valor` de `parametros_legais`.
- `database/migration-manifest.json`: entrada v2369 (checksum SHA256 real, dependência v2368).
- `backend/PlantaoPro.Api/TrilhosPagamentoRegras.cs`: aritmética pura (CRC16, módulos 10/11,
  fator com rollover, composição/validação de boleto e payload EMV).
- `backend/PlantaoPro.Api/TrilhosPagamentoService.cs`: serviço com configuração global, geração
  PIX estático/dinâmico, validações e remessa CNAB 400 das faturas candidatas.
- `backend/PlantaoPro.Api/Controllers/TrilhosPagamentoController.cs`: `GET/POST config`,
  `GET pix/estatico`, `POST pix/faturas/{id}`, `POST pix/validar`,
  `POST boletos/validar-linha`, `POST remessas/cnab400`, `GET faturas-abertas`; sessão
  autenticada exigida; falha de contrato = 400 com mensagem direta.
- `backend/PlantaoPro.Api/Program.cs`: DI no padrão da casa.
- `backend/PlantaoPro.Web/Controllers/TrilhosPagamentoWebController.cs`: proxy BFF `{ ok,
  data/erro }` (mesmo padrão da Folha R82, Simples R83, Calendário Fiscal R84 e DF-e R85).
- `backend/PlantaoPro.Web/Views/TrilhosPagamentoWeb/Index.cshtml`: formulário de configuração
  rotulado campo a campo com os limites oficiais; geração do payload PIX estático e por fatura
  com o copia-e-cola visível e botão de copiar; geração da remessa CNAB 400 com a tabela de
  títulos (nosso número, vencimento, valor, linha digitável) e o conteúdo do arquivo em linhas
  de 400 posições; painel de conferência de payload PIX ou linha digitável recebida com cada
  pendência individual; bloco "Em linguagem simples" explicando o BR Code com CRC16, o boleto com
  os módulos 10/11 e o rollover do fator, a remessa CNAB 400 e o lugar do FEBEN/CNAB 240 no
  marco de homologação externa, com aviso de que a ferramenta não substitui a homologação junto
  ao banco.
- `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml`: entrada "Trilhos de Pagamento"
  (módulo ADM360, ícone Download - download do arquivo de remessa/copia-e-cola).
- `backend/PlantaoPro.Web/Localization/UiTexts*.resx` (4 locales): chave Nav.TrilhosPagamento.
- `backend/PlantaoPro.Web/Controllers/AjudaController.cs`: artigo adm-trilhos-pagamento (ordem
  420, categoria instituicao) apontando para TrilhosPagamentoWeb/Index - a nova página entra já
  coberta.
- `docs/evidencias/2026-10-20-rodada-75/r75_inventario.txt`: rerun do inventário após a mudança:
  páginas 679 (+1), artigos 87 (+1), cobertas 85 (+1), gap mantido em 517, nenhum artigo órfão.
- `backend/PlantaoPro.Tests/R86TrilhosContratoTests.cs`: 7 fatos - manifesto v2369 com hash real
  e dependência; schema e seeds idempotentes na fonte única com fonte citável por linha; PIX
  estático/dinâmico travados com o CRC16 conferido contra valor público e self-CRC; validação do
  payload com caminho feliz e seis rejeições individuais; boleto com os vetores oficiais dos
  módulos, a linha digitável oficial de volta ao código de barras, a tabela do fator com o
  rollover e os vetores gerados; remessa CNAB 400 com posições do manual e trailer fechado;
  configuração com 404 honesto, 400 sem persistir, upsert de linha única e os vetores PIX
  saindo pela API.

## Suíte e gates

- Suíte cresce de 1730 para 1737 aprovados / 0 falhas (apps parados nas portas
  51976/51977/52976/52977 antes da execução).
- Build A/B Release `--no-incremental`: base 81 avisos, nova 81 avisos, 0 erros nos dois,
  DIFF=NENHUMA (logs `%TEMP%\opencode\r86_build_base.txt` / `r86_build_nova.txt`).
- Gap de manuais mantido travado em 517 (nova página + novo artigo no mesmo ciclo; inventário
  versionado atualizado, nenhum artigo órfão).
- Docscan `docs/evidencias/2026-10-19-rodada-62/r62_docscan.ps1` reexecutado após o documento
  final: 0 defeitos.
- Higiene `RepositoryHygieneContractTests` 8/8 após o documento.

## Próxima rodada

R87 - LGPD + saúde: consentimento (COAM/prontuário), direitos do titular (listar/exportar/apagar
com trilha) e retenção mínima legal.
