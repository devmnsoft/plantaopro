# R84a0 - Calendário fiscal e motor de obrigações/reminders por tenant

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R84 do bloco B4.
Escopo desta rodada: calendário fiscal/obrigações legais (DAS dia 20, FGTS dia 20, DCTFWeb dia
15, eSocial, EFD, livros contábeis) virando um motor de obrigações/reminders por tenant sobre a
fonte única das rodadas 80 a 83: catálogo oficial semeado em `plantaopro.parametros_legais`
(v2367), serviço `CalendarioFiscalService` que calcula competência, vencimento, dias restantes e
o estado de aviso de cada obrigação, estado por tenant persistido em
`plantaopro.adm_obrigacoes_fiscais`, API `api/calendario-fiscal` e tela web explicada em
linguagem leiga. Nenhum dia legal vive no código.

## Fontes oficiais consultadas

- DAS: LC 123/2006 art. 71 (regulamento do Simples Nacional) - competência do mês anterior,
  vencimento no dia 20 do mês subsequente.
- FGTS: Lei 5.107/1966 (CLT) art. 15 - depósito até o dia 20 do mês subsequente ao da
  remuneração.
- DCTFWeb: IN RFB 2.110/2022 - transmissão no mês subsequente à competência; o dia fixo de 15 é
  simplificação operacional registrada no plano r66a0 (R84).
- eSocial: Manual de Orientação do eSocial, eventos de folha S-1200 (mes subsequente à
  competência); o dia fixo de 7 é simplificação operacional do plano (na prática o prazo corre em
  dias úteis conforme o órgão).
- EFD ICms/IPI: layout oficial (IN Sefaz) - DEF mensal no mês subsequente; dia fixo de 15 como
  simplificação do plano (o prazo real varia por estado).
- Livros contábeis: LC 640/1994 art. 32 (normas de contabilidade aplicáveis às pequenas
  empresas) - encerramento do exercício; convenção de exibição em dezembro com vencimento dia 31.
- Janela de alerta antecipado (7 dias): parâmetro operacional do plano, não valor legal.

## Regras implementadas (CalendarioFiscalService)

- Formato canônico do catálogo na fonte única:
  `periodicidade|offset_periodos|dia_vencimento|regime_aplicavel`.
- MENSAL: o vencimento cai no dia fixo do MÊS EXIBIDO e a competência (mês de referência) é o
  mês exibido menos o offset (ex.: DAS de setembro vence em 20/outubro, exibido em outubro).
- ANUAL: o item só aparece na exibição de dezembro e encerra o exercício do ano exibido
  (livros: referência 12/ano, vencimento 31/12 do mesmo ano).
- Dia maior que o tamanho do mês de vencimento é aparado para o último dia (proteção aritmética;
  nenhum item semeado depende dela).
- Estados de reminder, nessa ordem: NAO_SE_APLICA (tenant desmarcou a obrigação para o período) >
  CONCLUIDA (marca registrada por usuário) > ATRASADA (dias restantes negativos) > VENCE_HOJE >
  VENCE_EM_CURTO_PRAZO (dentro da janela oficial, inclusiva nos dois bordos) > AGENDADA.
- DiasRestantes = vencimento - data atual; negativo quando passou do prazo.
- Estado por tenant isolado: sem linha persistida a obrigação nasce ativa; ativar/desativar é
  upsert na chave (tenant, obrigação, mês de referência, ano de referência); concluir/desconcluir
  é toggle idempotente que grava quem registrou (`concluida_por`) e quando (`concluida_em`).
- Validações explícitas (mensagem direta): mês 1..12, ano 2000..2100, tenant da sessão definido,
  obrigação existente no catálogo oficial, obrigação anual gerenciada somente na exibição de
  dezembro; linha malformada ou fora de faixa na fonte única = erro de operação citando o código.

## Parâmetros semeados (migration v2367)

| codigo | valor | fonte citável |
| --- | --- | --- |
| OBRIGACAO_FISCAL_DAS_SIMPLES | MENSAL\|1\|20\|SIMPLES_NACIONAL | LC 123/2006 art. 71 |
| OBRIGACAO_FISCAL_FGTS_MENSAL | MENSAL\|1\|20\|CLT | Lei 5.107/1966 art. 15 |
| OBRIGACAO_FISCAL_DCTFWEB_MENSAL | MENSAL\|1\|15\|GERAL | IN RFB 2.110/2022 + plano r66a0 |
| OBRIGACAO_FISCAL_ESOCIAL_S1200 | MENSAL\|1\|7\|CLT | eSocial S-1200 + plano r66a0 |
| OBRIGACAO_FISCAL_EFD_ICMS_MENSAL | MENSAL\|1\|15\|GERAL | Layout EFD ICms/IPI + plano r66a0 |
| OBRIGACAO_FISCAL_LIVROS_CONTABEIS_ANUAIS | ANUAL\|0\|31\|GERAL | LC 640/1994 art. 32 + plano r66a0 |
| OBRIGACOES_FISCAIS_JANELA_ALERTA_DIAS | 7 | parâmetro operacional do plano |

Todos com vigência nula (regra fixa) no ano 2026, idempotentes (`on conflict (codigo, ano) ...
do nothing`), manifesto v2367 com checksum SHA256 real e dependência em v2366. Coerência cruzada
travada em teste: o dia do catálogo tem que bater com os parâmetros avulsos já semeados
(SIMPLES_DAS_DIA_VENCIMENTO e DAS_SIMPLES_VENCIMENTO_DIA = 20; FGTS_VENCIMENTO_DIA = 20;
DCTFWEB_VENCIMENTO_DIA = 15) - a fonte única só pode ter uma verdade por valor.

## Exemplos travados em contrato (R84CalendarioContratoTests)

### Calendário de outubro/2026 (competência setembro/2026, hoje travado em 21/10/2026)

| obrigacao | competencia | vencimento | dias | estado |
| --- | --- | --- | --- | --- |
| eSocial (S-1200) | set/2026 | 07/10/2026 | -14 | ATRASADA |
| DCTFWeb | set/2026 | 15/10/2026 | -6 | ATRASADA |
| EFD ICms/IPI | set/2026 | 15/10/2026 | -6 | ATRASADA |
| DAS Simples | set/2026 | 20/10/2026 | -1 | ATRASADA |
| FGTS mensal | set/2026 | 20/10/2026 | -1 | ATRASADA |

Ordem por vencimento (empate quebra por código); janela oficial = 7 dias. Janeiro/2027:
mensais com competência dezembro/2026 e vencimento em janeiro de 2027 (ano vira na data).

### Estados de reminder contra data travada

| cenario | obrigacao | dias restantes | estado |
| --- | --- | --- | --- |
| hoje 01/10/2026 | eSocial (vence 07) | 6 | VENCE_EM_CURTO_PRAZO |
| hoje 01/10/2026 | DCTFWeb (vence 15) | 14 | AGENDADA |
| hoje 07/10/2026 | eSocial (vence 07) | 0 | VENCE_HOJE |
| hoje 13/10/2026 (borda da janela, 7 dias) | DAS (vence 20) | 7 | VENCE_EM_CURTO_PRAZO |
| hoje 12/10/2026 (fora da janela, 8 dias) | DAS (vence 20) | 8 | AGENDADA |

### Item anual e isolacao por tenant

- Dezembro/2026: seis itens - cinco mensais (competência novembro/2026) + encerramento anual dos
  livros (referência 12/2026, vencimento 31/12/2026). Em junho o item anual não aparece.
- Tenant A conclui o DAS de março/2027 (CONCLUIDA + registro de usuário/hora), alterna de novo e
  volta a AGENDADA sem resíduo; desativa o eSocial do mesmo período e só o próprio tenant fica
  NAO_SE_APLICA (tenant B continua AGENDADA/ativa); reativar não duplica linha (contagem
  direta no banco = 1 por chave).
- Validações: mês 0/13, ano 1999/2101, tenant vazio, código desconhecido e obrigação anual
  fora de dezembro = erro direto.

## Mudanças desta rodada

- `database/migrations/2026_10_v2367_r84_calendario_fiscal_2026.sql`: tabela
  `adm_obrigacoes_fiscais` (estado por tenant, sem FK de referência de tenant) com índice único
  parcial (tenant, obrigação, referência) e os 7 parâmetros do catálogo idempotentes; comentário
  de coluna amplia a convenção do campo `valor` com o novo formato.
- `database/migration-manifest.json`: entrada v2367 (checksum SHA256 real, dependência v2366).
- `backend/PlantaoPro.Application/Legislacao/ICalendarioFiscalService.cs`: contrato com o DTO do
  item (código, textos leigos, periodicidade, regime, competência, vencimento, ativa/concluída,
  estado e dias restantes), do calendário do período e dos dois comandos.
- `backend/PlantaoPro.Infrastructure/Legislacao/ObrigacoesFiscaisRepository.cs`: leitura do estado
  por período e os dois upserts (ativa/desativa e toggle de conclusão com trilha de usuário).
- `backend/PlantaoPro.Infrastructure/Legislacao/CalendarioFiscalService.cs`: motor das regras
  acima; reusa `ValorObrigatorio` da tributação PF (R81) para a janela oficial.
- `backend/PlantaoPro.Api/Controllers/CalendarioFiscalController.cs`: `GET
  api/calendario-fiscal/calendario?mes=&ano=`, `POST alterar-ativa` e `POST alternar-conclusao`;
  sem organização na sessão = 401, validação/parâmetro fora da faixa = 400 com mensagem direta.
- `backend/PlantaoPro.Api/Program.cs`: DI no padrão da casa.
- `backend/PlantaoPro.Web/Controllers/CalendarioFiscalWebController.cs`: proxy BFF
  `{ ok, data/erro }` (mesmo padrão da Folha R82 e do Simples R83).
- `backend/PlantaoPro.Web/Views/CalendarioFiscalWeb/Index.cshtml`: tabela do período com
  competência, vencimento, dias, selo de estado SEMPRE com texto (cor nunca é o único canal),
  interruptor de ativação por período e botão de concluir/desconcluir; seleção de mês/ano
  rotulada, status inline (sem alert/confirm) e bloco "Em linguagem simples" explicando cada
  conceito e cada estado + aviso de que prazos oficiais podem variar por estado/orgão.
- `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml`: entrada "Calendário Fiscal" (módulo
  ADM360, ícone Favorite - estrela: todos os ícones de calendário/agenda do registro
  (Calendar, Schedule, Shift, Recent, History, Audit, Document, Report) já estavam empregados uma
  vez e não constam da allowlist de pares da R46; entre os ícones livres o marcador de
  pendência/prioridade é o mais próximo semanticamente).
- `backend/PlantaoPro.Web/Localization/UiTexts*.resx` (4 locales): chave Nav.ObrigacoesFiscais.
- `backend/PlantaoPro.Web/Controllers/AjudaController.cs`: artigo adm-calendario-fiscal
  (ordem 400) apontando para CalendarioFiscalWeb/Index - a nova página entra já coberta.
- `docs/evidencias/2026-10-20-rodada-75/r75_inventario.txt`: rerun do inventário após a mudança:
  páginas 677 (+1), artigos 85 (+1), cobertas 83 (+1), gap mantido em 517.
- `backend/PlantaoPro.Tests/R84CalendarioContratoTests.cs`: 7 fatos - manifesto v2367 com hash
  real; catálogo semeado linha a linha + coerência cruzada de dias com as rodadas anteriores;
  calendário mensal com competência/vencimento oficiais e ano que vira; estados de reminder com
  bordas inclusivas da janela; item anual só em dezembro; estado por tenant isolado com conclusão
  alternada e upsert sem duplicidade; validações explícitas.

## Suíte e gates

- Suíte cresce de 1716 para 1723 aprovados (apps parados nas portas 51976/51977/52976/52977).
- Build A/B Release `--no-incremental`: base 81 avisos, nova 81 avisos, 0 erros nos dois,
  DIFF=NENHUMA.
- Gap de manuais mantido travado em 517 (nova página + novo artigo no mesmo commit; inventário
  versionado atualizado).
- Docscan `docs/evidencias/2026-10-19-rodada-62/r62_docscan.ps1` reexecutado após o documento
  final, delta zero.
- Higiene `RepositoryHygieneContractTests` 8/8 após o documento.

## Próxima rodada

R85 - DF-e nacional: configuração SEFAZ por UF, layouts (NF-e 4.0, NFC-e, CF-e-SAT, CF-e-Serviços
3.0) e chave de acesso mod-11 com dígito verificador por UF.
