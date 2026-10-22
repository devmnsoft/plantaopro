# R88a0 - Trabalhista: eventos eSocial por colaborador (S-1000 a S-1299) em prévia, jornada semanal e aviso prévio proporcional

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R88 do bloco B4.
Escopo desta rodada: tornar o ciclo trabalhista mensurável dentro do produto: o catálogo oficial dos
eventos eSocial da série S-1000 a S-1299 (leiaute S-1.3) com vencimento de cada evento, o registro em
prévia (`forma_envio` fixa `PREVIEW`) de eventos institucionais e por colaborador, a análise de horas
semanais contra a carga horária contratada (contrato vigente de `adm_contratos_trabalho`) e o teto
legal de 44h/semana, com estimativa de hora extra sobre salário/220h com acréscimo mínimo de 50%, e o
simulador de aviso prévio proporcional com fontes citáveis. Sem extensão de `adm_colaboradores`:
jornada contratual e salário vêm do contrato vigente já existente; sem envio real ao eSocial (prévia é
a entrega da rodada) e sem folha de pagamento.

## Interpretação registrada: aviso prévio na fronteira de 1 ano

Lei 12.506/2011 (art. 1) dá 30 dias de aviso prévio + 3 dias por ano completo de trabalho; seu parágrafo
único limita o total a 90 dias. A Nota Técnica MTE 184/2012 vincula o acréscimo ao tempo de serviço
superior a 1 ano. Vetores travados desta rodada: até 364 dias de casa = somente a base de 30 dias;
completo 1 ano de serviço (admissão 2024-01-15, demissão 2025-01-15) = 33 dias. Ou seja, o produto
adota a leitura de que o acréscimo passa a incidir a partir da conclusão do primeiro ano completo
(cada ano inteiro adicional soma 3, estourando o teto de 90 a partir de 20 anos). A redução da jornada
durante o aviso (2h/dia ou 7 dias corridos, CLT art. 488) é exibida como referência, não desconta dias.

Não-goal documentado: não há cálculo proporcional abaixo de 1 ano. No recon, a MP 1.120/2022 foi
confirmada como tratando cargos da ANTAQ (e não aviso prévio), a Lei 14.310/2022 trata da Lei Maria da
Penha e a Lei 14.704/2023 trata de Libras; nenhum veículo normativo ativo foi encontrado para
proporcionalizar a base abaixo do primeiro ano, então a base permanece em 30 dias nesse intervalo.

## Fontes oficiais consultadas

- Lei 12.506/2011, art. 1 e parágrafo único: aviso prévio de 30 dias + 3 dias por ano completo de
  trabalho, total máximo de 90 dias - conferida em planalto.gov.br durante o recon.
- Nota Técnica MTE 184/2012: acréscimo de 3 dias por ano de serviço, vinculado ao tempo superior a
  1 ano (base da interpretação registrada acima).
- CLT (Decreto-Lei 5.452/1943): art. 487, II (aviso prévio escrito de 30 dias para contrato
  indeterminado); art. 488 (redução de 2h/dia ou de 7 dias corridos durante o aviso, como referência
  exibida); art. 59, §1º (jornada extraordinária com acréscimo mínimo de 50%); art. 74, §2º (mensalidade
  dividida por 220 horas, base do valor-hora).
- CF/88, art. 7: XIII (duração máxima de 44 horas semanais) e XVI (acréscimo de no mínimo 50% sobre a
  hora normal para trabalho extraordinário).
- eSocial: Manual de Orientação S-1.3 (MOS) e leiaute S-1.3 - Nota Técnica 06/2026, rev. 09/04/2026,
  publicados em gov.br/esocial; os vencimentos e a classificação TABELA_EMPREGADOR x PERIODICO de cada
  evento do catálogo foram conferidos contra a reprodução pública do manual do leiaute S-1.3 em
  garoasp.com.br.

## Regras implementadas

- `EsocialTrabalhoRegras` (aritmética pura, sem IO): `AnosCompletos` e `DiasAvisoPrevio` (base 30,
  +3 por ano completo a partir do primeiro ano, teto 90); `SemanaInicio` (qualquer data normaliza para a
  segunda-feira da semana que a contém, por dia da semana - nunca avança); `ResumoSemanaJornada.Calcular`
  (total = normais + extras; excedentes medidos contra a carga contratada quando houver contrato, senão
  contra o teto de 44h; situações estáveis NAO_DECLARADA, CONFORME, EXCEDE_CONTRATO e
  EXCEDE_LIMITE_LEGAL, priorizando a mais grave); `ValorHoraExtra` = round(salário/220 x 1,5; 4 casas)
  e `ValorHorasExtras` = round(valor-hora x excedentes; 2 casas), nulos quando não há base.
- `EsocialTrabalhoService` (Dapper/Postgres, tudo escopado por tenant): catálogo oficial de eventos;
  listagem de colaboradores com contexto trabalhista (contrato vigente por row_number de inicio desc:
  tipo, salário, carga horária; primeira admissão = menor inicio); resumo de jornada mensal (semanas do
  mês, declarações, flags contratual/legal, totais e estimativa do mês); declaração semanal com
  normalização à segunda, upsert pela única (tenant, colaborador, semana) e validações (horas nao
  negativas, total semanal <= 120 por sanidade, observação <= 400, colaborador do tenant senao 404);
  simulador de aviso prévio devolvendo base, acréscimo, teto, redução de referência e as fontes
  aplicadas citáveis; declaração de evento eSocial (código deve existir no catálogo senao 400; evento
  PERIODICO exige periodo_apuracao AAAA-MM; colaborador informado precisa existir no tenant senao 404;
  `forma_envio` fixa `PREVIEW`); listagem de eventos por colaborador/período; exclusões com 404 quando
  ausente. Parâmetros legais lidos de `parametros_legais` (ano 2026) com fallback para as constantes
  puras - fonte única preservada.
- API `api/esocial-trabalhista` com sessão autenticada exigida (`[Authorize]` simples, padrão da casa):
  9 rotas - `GET catalogo`, `GET colaboradores`, `GET jornada?colaboradorId&mes`,
  `POST jornada/declarar`, `POST jornada/excluir`, `POST aviso-previo`, `GET eventos`,
  `POST eventos/declarar`, `POST eventos/excluir`; falha de contrato = 400 com pendências diretas.
- Superfície web: 9 ações proxy `{ ok, data/erro }` em `EsocialTrabalhoWebController` (padrão das
  rodadas R84-R86) + página com quatro blocos: catálogo oficial, horas/jornada semanal por colaborador
  (com situações coloridas E textualizadas - cor nunca é o único canal), simulador de aviso prévio e
  eventos do mês em prévia; confirmação de exclusão via modal Bootstrap (sem alert/confirm nativos).
  Sidebar sob `Can("ADM360")` com ícone único `Attachment` (clip de documento oficial - contrato R46
  de pares conceituais impede reaproveitar `Schedule`, usado pela Agenda); guard
  `EsocialTrabalhoWeb -> ADM360`; rótulo `Nav.EsocialTrabalhista` nos 4 locales
  (pt/en/es/fr). Artigo de ajuda `adm-esocial-trabalho` (ordem 430) aponta para a página.

## Migração v2371 e seeds

- `esocial_eventos_catalogo`: 14 sementes (S-1000, S-1005, S-1010, S-1020 e S-1070 como
  TABELA_EMPREGADOR; S-1200, S-1202, S-1207, S-1210, S-1260, S-1270, S-1280, S-1298 e S-1299 como
  PERIODICO), cada linha com nome, descrição, vencimento citável e `fonte_oficial` apontando para o
  leiaute S-1.3/MOS. Vencimentos travados: S-1200 em 20/12; S-1210 no mês seguinte ao pagamento;
  S-1280 com ou antes do S-1299; S-1299 no dia 15 do mês seguinte ao de referência (o fechamento é o
  que dispara S-5011/S-5012/S-5013 para DCTFWeb/FGTS Digital).
- `colaborador_eventos_esocial`: registro em prévia com check constraint `forma_envio = 'PREVIEW'`,
  período `character(7)` para os periódicos e índices por (tenant, colaborador) e (tenant, data).
- `colaborador_jornadas_semanais`: declaração semanal única por (tenant, colaborador, semana_inicio) -
  índice que torna o upsert possível - com horas normais/extras, observação e auditoria.
- 8 linhas em `parametros_legais` (ano 2026), cada uma com valor e fonte citável:
  `ESOCIAL_EVENTOS_CATALOGO` = S-1000|S-1005|S-1010|S-1020|S-1070|S-1200|S-1202|S-1207|S-1210|S-1260|S-1270|S-1280|S-1298|S-1299;
  `ESOCIAL_VENCIMENTO_PERIODICO_DIA` = 15; `AVISO_PREVIO_BASE_DIAS` = 30;
  `AVISO_PREVIO_ACRESCIMO_POR_ANO_DIAS` = 3; `AVISO_PREVIO_TETO_TOTAL_DIAS` = 90;
  `JORNADA_SEMANAL_MAX_HORAS` = 44; `HORA_EXTRA_ACRESIMO_MIN_PCT` = 50;
  `SALARIO_DIVISOR_MENSAL_HORAS` = 220. Idempotente (create if not exists + on conflict/where not
  exists); manifesto v2371 com checksum SHA256 real (LF
  `5f7c5591d39d40041416e27a9263638e615b31fff38da9c7b574ff30a06a2e24`) e dependência em v2370.
- Nota técnica: em Postgres, coluna não qualificada no `ON CONFLICT ... DO UPDATE SET` (ex.:
  `criado_por=coalesce(excluded.criado_por, criado_por)`) produz erro 42702 "referência ambígua"; o
  upsert qualifica pelo nome da tabela.

## Exemplos travados em contrato (R88EsocialTrabalhoContratoTests, 10 fatos)

Aviso prévio proporcional (Lei 12.506/2011):

| vetor | admissao -> demissao | dias | leitura |
| --- | --- | --- | --- |
| 1 ano completo | 2024-01-15 -> 2025-01-15 | 33 | +3 do primeiro ano (NT MTE 184/2012) |
| 364 dias | 2023-06-30 -> 2024-06-29 | 30 | só a base, sem acréscimo |
| 10 anos | 2015-06-30 -> 2025-06-30 | 60 | 30 + 30 |
| 36 anos | 1990-05-20 -> 2026-10-01 | 90 | teto da lei estoura |
| mesmo ano | 2026-01-05 -> 2026-10-22 | 30 | base pura |

Semana de referência sempre volta à segunda-feira: 2026-10-22 (quinta) -> 2026-10-19;
2026-01-01 (quinta) -> 2025-12-29; segunda permanece; domingo 2026-10-25 cai em 2026-10-19 (não
avança).

Jornada: contrato 44h/salário 2200 -> semana 40+5 = 45h = EXCEDE_LIMITE_LEGAL (1 hora excedente;
valor-hora R$ 15,00; total estimado R$ 15,00); 40+0 = CONFORME; contrato 40h/salário 4400 -> 41+2 =
43h = EXCEDE_CONTRATO sem estourar o teto (3 horas excedentes; valor-hora R$ 30,00; total R$ 90,00);
sem contrato 44+1 = 45h = EXCEDE_LIMITE_LEGAL com `excedeContrato` nulo; 0+0 = NAO_DECLARADA.
Outubro/2026 tem exatamente 5 semanas (inícios 28/09, 05, 12, 19 e 26/10), todas carregando o
contexto contratual do colaborador (CLT, 44h, salário 2200, primeira admissão 2020-03-02).

Declaração semanal: declarar quinta 2026-10-21 grava a segunda 2026-10-19 (normalização); redeclarar a
mesma semana atualiza em vez de duplicar (upsert) e reclassifica para CONFORME; horas negativas ->
400; 100+21 -> 400 (total acima de 120h/sanidade); observação de 401 caracteres -> 400; colaborador
inexistente -> 404; excluir responde ok e a segunda exclusão responde 404.

Eventos em prévia: S-1200 com período 2026-10 sai com forma PREVIEW, nome contendo "Remuneração" e
categoria PERIODICO; S-1000 institucional (sem colaborador) é aceito; código fora do catálogo (S-9999)
-> 400; periódico sem período -> 400; colaborador de outro tenant -> 404; filtros por colaborador e
por período funcionam; evento do tenant vizinho é invisível (isolamento); exclusão remove e repete 404.

Schema e manifesto: catálogo com exatamente os 14 códigos nas duas categorias, fonte/nome/vencimento
presentes em toda linha, reexecução da migração não duplica nada, os 8 parâmetros saem com os valores
esperados, e o manifesto traz hash real + dependência v2370. Padrão B6: advisory locks
r88_esocial_trabalho_seed/_schema, seed/purge transacional com GUIDs fixos hex.

## Mudanças desta rodada

- `database/migrations/2026_10_v2371_r88_esocial_trabalhista.sql`: 3 tabelas novas (catálogo de
  eventos, eventos em prévia, jornadas semanais) + índices + 8 parâmetros legais com fonte citável;
  idempotente.
- `database/migration-manifest.json`: entrada v2371 (checksum SHA256 real, dependência v2370).
- `backend/PlantaoPro.Api/EsocialTrabalhoRegras.cs`: aritmética pura (aviso prévio, semana, resumo de
  jornada, valor-hora e estimativa de extras).
- `backend/PlantaoPro.Api/EsocialTrabalhoService.cs`: 9 métodos Dapper escopados por tenant com
  parâmetros legais e fallback, DTOs records com colunas aliasadas (Dapper sem ValueTuple) e sentinela
  para datas nulas.
- `backend/PlantaoPro.Api/Controllers/EsocialTrabalhoController.cs`: `api/esocial-trabalhista` com as
  9 rotas; sessão autenticada exigida.
- `backend/PlantaoPro.Api/Program.cs`: DI do serviço no padrão da casa.
- `backend/PlantaoPro.Web/Controllers/EsocialTrabalhoWebController.cs`: 9 ações proxy `{ ok, data/erro }`.
- `backend/PlantaoPro.Web/Views/EsocialTrabalhoWeb/Index.cshtml`: catálogo, jornada semanal com
  situações, aviso prévio, eventos em prévia com modal de exclusão; tabelas em `.table-responsive`
  com `th scope="col"` (contratos R45/R59).
- `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml`: item eSocial Trabalhista sob ADM360 com
  ícone Attachment único no menu (contrato R46).
- `backend/PlantaoPro.Web/Localization/UiTexts{.pt,.en,.es,.fr}.resx`: chave `Nav.EsocialTrabalhista`
  nos 4 locales.
- `backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs`: rota `EsocialTrabalhoWeb -> ADM360`.
- `backend/PlantaoPro.Web/Controllers/AjudaController.cs`: artigo `adm-esocial-trabalho` (ordem 430)
  apontando para EsocialTrabalhoWeb/Index - a nova página entra já coberta.
- `docs/evidencias/2026-10-20-rodada-75/r75_inventario.txt`: rerun do inventário após a mudança:
  páginas 681 (+1), artigos 89 (+1), cobertas 87 (+1), gap mantido em 517, nenhum artigo órfão,
  aux 29/pote quebra 29 estáveis.
- `backend/PlantaoPro.Tests/R88EsocialTrabalhoContratoTests.cs`: 10 fatos - manifesto v2371 com hash
  real e dependência; schema/catálogo/parâmetros idempotentes com fonte citável por linha; vetores de
  aviso prévio travados; normalização de semana à segunda; situações de jornada com e sem contrato e
  estimativa de hora extra; mês de outubro/2026 com 5 semanas e contexto contratual; upsert de
  declaração com validações e exclusão; aviso prévio via serviço com fontes e validação de datas;
  round-trip de eventos com rejeições 400/404 e isolamento entre tenants; listagem de colaboradores
  com contrato vigente/carga/salário/admissão.

## Suíte e gates

- Suíte cresce de 1745 para 1755 aprovados / 0 falhas (apps parados nas portas
  51976/51977/52976/52977 antes da execução).
- Build A/B Release `--no-incremental`: base 81 avisos, nova 81 avisos, 0 erros nos dois,
  DIFF=NENHUMA (dedupe: 76 únicos de cada lado; logs `%TEMP%\opencode\r88_build_base.txt` /
  `r88_build_nova.txt`).
- Gap de manuais mantido travado em 517 (nova página + novo artigo no mesmo ciclo; inventário
  versionado atualizado, nenhum artigo órfão).
- Docscan `docs/evidencias/2026-10-19-rodada-62/r62_docscan.ps1` reexecutado após o documento
  final: 0 defeitos.
- Higiene `RepositoryHygieneContractTests` 8/8 após o documento.

## Próxima rodada

R89 - venda para fora do país: moeda base BRL + cotações (USD/EUR/GBP), faturamento internacional
simplificado (nota fiscal não-tradicional/VAT por destino) mantendo i18n dos 4 locales.
