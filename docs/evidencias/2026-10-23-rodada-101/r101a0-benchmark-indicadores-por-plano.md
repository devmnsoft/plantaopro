# R101 — Relatórios com benchmark ("seus indicadores vs o segmento") por plano

## Objetivo

Linha 96 do plano (r66a0): "relatórios com benchmark ('seus indicadores vs o segmento') por plano" — um
novo cartão no hub de relatórios compara os indicadores mensais da instituição autenticada com os pares
contratados no **mesmo plano**: para cada indicador, o valor próprio, a mediana do plano, a média, a
posição na amostra e quantos pares compõem a comparação.

Lacunas tratadas nesta rodada:
1. **sem referência de segmento**: os relatórios do hub mostravam os valores absolutos da própria
   instituição, mas nenhuma referência contra outras organizações contratadas no mesmo plano — não era
   possível saber se a cobertura, o cancelamento ou o tempo de preenchimento eram bons ou ruins para o
   segmento da instituição;
2. **posição sem vazar vizinho**: o benchmark expõe apenas estatísticas agregadas do plano (mediana,
   média, percentil) e nunca o valor individual de nenhum vizinho; com pares abaixo da amostra mínima a
   comparação fica suspensa (valores nulos) em vez de exibir uma amostra frágil.

Entregas: serviço `RelatorioBenchmarkService` (API) + rota `GET api/relatorios/benchmark?mes`, ação BFF
web + página server-side `/Relatorios/Benchmark` com cartão "Benchmark do plano" no hub, drill ponta a
ponta API+Web sobre o banco vivo e contrato de 5 fatos.

## Recon

- O hub `/Relatorios/Index` é server-side PT hardcoded (sem resx nesta área — paridade i18n dos quatro
  resx intocada); o cartão é uma entrada `{Title, Description, Icon, Action, Category, Scope, Formats}`
  e o contrato R24 mantém o literal "Plantões por período" intacto.
- Guard web (`SaasRouteGuardFilter`): exige `HasPermission(módulo, VER)` (claims do login — construídos
  dos grants do banco: perfis + exceções por usuário) **e** `IsModuleEnabled` (claim do módulo + módulo
  efetivo ao vivo). Sem módulo contratado → `MODULO_NAO_CONTRATADO`; sem grant → `PERMISSAO_NEGADA`.
- Catálogo: módulo RELATORIOS com 31 permissões (código `RELATORIOS.VER`, forma com ponto), porém o
  perfil **ADMINISTRADOR_CLIENTE tem zero grants nesse módulo** (ADMINISTRADOR=31,
  ADMINISTRADOR_GLOBAL=2, ADMINISTRADOR_CLIENTE=0) → toda a área `/Relatorios/*` é negada ao tenant admin
  na web hoje (Achado 1); a API central só passava via FallbackPermissoes.
- Fontes do benchmark = mesmas tabelas que o hub já lê: `plantoes` (publicados no mês, situação
  diferente de rascunho/cancelado), `escalas` (confirmadas/canceladas; `reg_date` alimenta o tempo de
  preenchimento), `pagamentos` (`status='pago'`, mês pela data de pagamento), `medicos` (ativos). Escopo
  canônico por `cliente_id`. **MUDANÇA ZERO de DDL** nesta rodada (nem manifest, nem migração).
- A assinatura define o plano: a mais recente `ATIVA` (fallback `TRIAL`), desempate
  `data_inicio desc nulls last`; sem ativa/trial → 404 determinístico.
- Dapper materializa record pelo tipo EXATO das colunas do reader: `avg()`/`percentile_cont()` sobre
  numeric devolve `Decimal`, enquanto o DTO usa `double?` (`InvalidOperationException`) → o select final
  casta a saída para `::float8` (Achado 2).

## Decisões

Semântica travada (fechada pelos asserts do drill + dump t1 + contrato):
- **Segmento** = pares do MESMO plano: assinaturas ATIVA/TRIAL do plano **excluindo o próprio
  `cliente_id`**;
- **amostra mínima** `AmostraMinimaPeers = 3` (constante no serviço): máximo de pares < 3 →
  `amostraInsuficiente=true` e todos os indicadores ficam nulos (comparação suspensa, nunca exibida);
- **mês** = calendário UTC semiaberto `[primeiro dia, primeiro dia + 1 mês)`; parâmetro `mes`
  `yyyy-MM` opcional (padrão: mês atual UTC); `TentarParsearMes` é parse manual e determinístico —
  `DateOnly.TryParseExact("yyyy-MM", AssumeUniversal)` falha no net10.0 (funciona com `None`; Achado 3);
- **agregados** (mediana `percentile_cont(0.5) within group (order by v)`, média, posição) incluem o
  próprio dono quando ele tem dado — a amostra é o conjunto de TODOS os donos com dado na KPI no mês;
- **`peersNaAmostra`** por indicador = contagem de **pares** (excluindo-se) com dado naquela KPI naquele
  mês — informa quantos vizinhos compõem a comparação;
- **posição** = `100·count(v < meu)/(n−1)` onde n conta o próprio dono — com n=1 a posição é indefinida
  → nula;
- **mês vazio** (ninguém com dado): `long_form` vazio → DTOs todos nulos, `PeersNaAmostra=0`,
  `temDadosProprios=false` (guard do máximo de pares: `rows.Length == 0 ? 0 : rows.Max(...)`).

Cinco indicadores (código e rótulo estáveis na resposta e na página): plantões publicados por médico
(`BENCH_PLANTOES_POR_MEDICO`), cobertura média de vagas (`BENCH_COBERTURA_VAGAS_PCT`), cancelamento de
escalas (`BENCH_CANCELAMENTO_ESCALAS_PCT`), tempo médio de preenchimento (`BENCH_TEMPO_MEDIO_PREENCHIMENTO_H`)
e valor médio pago por plantão (`BENCH_VALOR_MEDIO_PLANTAO_PAGO`).

- **API**: `RelatorioBenchmarkService` (DI scoped) + `GET api/relatorios/benchmark?mes` no
  `RelatoriosCentralController`; porta protegida por `PermissionConstants.RelatoriosVer` via
  `PermissionGuardService.ValidarPermissaoAsync` (permissão `RELATORIOS.VER` do catálogo).
- **BFF web**: ação `Benchmark(string? mes)` posicionada no **final** do `RelatoriosController` (após
  `ExportarOperacional`) com argumentos `IDictionary<string,string?>` — posicionamento que evita
  deslocar os dois CS8620 pré-existentes e não soma aviso nenhum; `ReadApiResponse` em melhor esforço →
  a página renderiza mesmo se a API degradar (banner em vez de crash).
- **View**: `Benchmark.cshtml` server-side PT hardcoded: um bloco por indicador com marcador `data-kpi`,
  posição exibida como "N% da amostra" e banner de amostra insuficiente com a nota de privacidade ("A
  contagem de pares exclui a sua própria organização e nenhum valor individual de vizinho é exposto.");
  o hub ganha o cartão "Benchmark do plano" (categoria Executivo, escopo "Pares do mesmo plano").
- **Banco**: sem DDL, sem migração, manifest intacto — somente leitura das tabelas existentes; o drill e
  o teste gravam apenas dados fixtures, limpos no início e no fim.

## Achados

1. **ADMINISTRADOR_CLIENTE sem grants RELATORIOS.\***: toda a área `/Relatorios/*` é negada ao tenant
   admin na web hoje (o guard decide pelos claims do login; o perfil não tem grant no módulo) — a API
   central só passava via permissões de fallback. O drill contorna com exceção por usuário
   (`usuario_permissoes_especiais`, idempotente `not exists`, escopo tenant, justificativa), inserida
   **antes** dos logins (os claims são construídos no login) e pré-limpada **antes** da remoção dos
   mundos (ordem de FK). A correção do catálogo (grants no perfil, via migração) ficou como pendência.
2. **Dapper materializa pelo tipo exato**: `avg()`/`percentile_cont(0.5)` sobre numeric → `Decimal` vs
   `double?` do DTO → `InvalidOperationException` na materialização; resolvido castando a projeção final
   para `::float8` (`pf.v::float8 MeuValor`).
3. **`DateOnly.TryParseExact` com `AssumeUniversal`** falha para `"yyyy-MM"` no net10.0 (com `None`
   funciona — provado em scratch isolado antes de decidir): parse manual com validação de dígitos
   (`ApenasDigitos`), mês 1..12 e `null`/vazio/whitespace → mês atual UTC.
4. **Zero aviso novo (76/76)**: a ação BFF entrou no fim do controller para manter estáveis as linhas
   dos dois CS8620 pré-existentes, e os argumentos de consulta como `IDictionary<string,string?>` para
   não gerar CS8620/CS8625 novo na web.
5. **PS 5.1 no drill**: stderr nativo do `psql.exe` com `EAP=Stop` prefixa `psql.exe : ` — sucesso
   checado por `-match 'ERRO'`; nome do schema digitado por bytes (o troço "plantaouro" no byte 8)
   recontado após cada edição por contagem ok/ruim.

## Evidência (execução canônica)

Linha a linha de `r101a_drill_saida.txt` (API porta 52086, Web porta 52085, banco vivo
`plantaopro_test`, execução de 23/10/2026, DLLs Release do gate r101):

```
R101A_DRILL data=20261023-140827 db=plantaopro_test api=52086 web=52085
PORTAS_LIVRES_INICIO=SIM
ESTAGEM_1_API_NO_AR ok=SIM pid=41204
ESTAGEM_1_WEB_NO_AR ok=SIM pid=42484
PRE_LIMPEZA_RERUN ok=SIM
PA_PLANO_OK plano=e3dda2c5-5831-4d2c-a32c-9b0fcad5a53c vinculos=0
PA_MUNDOS_OK adm_tenant=5efbb727-bb19-4513-81f7-2357d6ea3119 adm_status=TRIAL ctrl_tenant=f6427ae1-f2d4-40fb-ac80-253a6e673a19 ctrl_status=TRIAL
PA_RELATORIOS_OK http=200 origem=CONTRATO historico=1 efetivos=[RELATORIOS]
PA_PERMISSAO_OK usuario=donoA permissao=RELATORIOS.VER escopo=tenant
PEERS_FIXTURE_OK adm_eventos=22 ctrl_eventos=6 p1_eventos=11 p2_eventos=30 total=69
T1_DUMP_OK arquivo=r101a_t1_benchmark_api.json plano=R101_PLANO_DRILL mes=2026-09 kpis=5
BENCHMARK_VAZIO_OK http=200 mes=2025-01 insuficiente=SIM valores_nulos=5
WEB_LOGIN_OK http=200
WEB_HUB_OK http=200 cartao=SIM
WEB_BENCHMARK_OK http=200 kpi=SIM plano=SIM valor=SIM percentil=SIM
CONTROLE_WEB_OK http=200 motivo=MODULO_NAO_CONTRATADO
PORTAS_LIVRES=SIM
RESULTADO=PASSEI
```

Dump t1 (`r101a_t1_benchmark_api.json`) — chamada canônica como dono A de
`GET api/relatorios/benchmark?mes=2026-09`, plano `R101_PLANO_DRILL`, `amostraMinimaPeers=3`,
`amostraInsuficiente=false`, `temDadosProprios=true`. Números fechados (meu valor / mediana / média /
posição / pares):

| Indicador | Meu valor | Mediana | Média | Posição | Pares |
| --- | --- | --- | --- | --- | --- |
| Plantões publicados por médico | 4 | 3.5 | 3.25 | 67 | 3 |
| Cobertura média de vagas (%) | 75 | 81.25 | 78.125 | 33 | 3 |
| Cancelamento de escalas (%) | 20 | 10 | 10 | 67 | 3 |
| Tempo médio de preenchimento (h) | 10 | 9 | 9.5 | 67 | 3 |
| Valor médio pago por plantão (R$) | 200 | 250 | 250 | 33 | 3 |

Fluxo: o superadmin cria o plano de drill `R101_PLANO_DRILL` (sem vínculos) e o funil self-service abre
dois mundos TRIAL (mundo A ator / mundo C controle). A governança real habilita RELATORIOS no A
(`habilitar-tenant`: origem `CONTRATO`, histórico `HABILITAR`, `modulos_efetivos=[RELATORIOS]`). Como o
perfil ADMINISTRADOR_CLIENTE não tem grants RELATORIOS.* (Achado 1), o dono A recebe `RELATORIOS.VER`
por exceção por usuário antes do login. Fixture determinística de setembro de 2026 no MESMO plano: A
(2 médicos, 8 plantões com cobertura 75%, 8 confirmadas + 2 canceladas, preenchimento +10h, 2×R$ 200),
C (1 médico, 2 plantões, 2 confirmadas, +8h, 1×R$ 300), P1 (1 médico, 4 plantões, 4 confirmadas +
1 cancelada, +5h, 1×R$ 100) e P2 (4 médicos, 12 plantões, 12 confirmadas, +15h, 2×R$ 400) — 69 eventos
no total. A amostra fecha em 3 pares com dado nas cinco KPIs: os agregados incluem o A (mediana/média
sobre os 4 donos com dado) e a contagem de pares exclui o A (sempre 3). Mês vazio 2025-01: HTTP 200,
`amostraInsuficiente=true`, cinco indicadores nulos. Na Web com sessão do dono A: o hub renderiza o
cartão "Benchmark do plano" e `/Relatorios/Benchmark?mes=2026-09` renderiza os valores reais do t1
(cobertura própria `75,00` e posição `67% da amostra`); o mundo controle (sem o módulo contratado)
recebe HTTP 200 com motivo `MODULO_NAO_CONTRATADO`.

Banco: MUDANÇA ZERO de DDL/migração (manifest intacto — v2378, da R100, segue última da cadeia). O
resíduo do mundo do drill (plano `e3dda2c5…`, mundos TRIAL e a linha de permissão por usuário) fica no
banco vivo e é limpo pela pré-limpeza (`PRE_LIMPEZA_RERUN`) de qualquer rerun.

## Contrato

`R101BenchmarkPlanoContratoTests` — 5 fatos:
1. **saída do drill**: todos os marcadores máquina-legíveis (portas livres, apps no ar, pré-limpeza,
   plano com `vinculos=0`, dois mundos TRIAL, RELATORIOS via governança real `origem=CONTRATO` efetivo,
   permissão por usuário `escopo=tenant`, fixture 22/6/11/30 eventos com `total=69`, t1 `kpis=5`, mês
   vazio `http=200 insuficiente=SIM valores_nulos=5`, login web, hub com cartão, página com
   kpi/plano/valor/percentil, controle `MODULO_NAO_CONTRATADO`, portas livres), `RESULTADO=PASSEI` e
   zero `FALHA `;
2. **dump t1**: cabeçalho (`2026-09`, plano, amostra mínima 3, suficiente, dados próprios) e os cinco
   indicadores com os valores exatos fechados da tabela acima;
3. **fragmentos de código + função pura**: constante `AmostraMinimaPeers = 3`,
   `percentile_cont(0.5) within group (order by v)`, filtro de pares `('ATIVA','TRIAL')` +
   `pa.cliente_id<>@cid`, `count(*) filter (where dono<>@cid)`, escolha do plano ATIVA>TRIAL por
   `data_inicio desc nulls last`, saída `::float8 MeuValor`, guard do mês vazio, os cinco códigos de
   KPI, `PermissionConstants.RelatoriosVer`, mensagem do 404, rota `[HttpGet("benchmark")]`, DI
   `AddScoped`, BFF `api/relatorios/benchmark` + view, marcadores da view (`data-kpi`, "Amostra
   insuficiente neste mês.", "% da amostra", "Pares na amostra"), cartão do hub + literal "Plantões por
   período" preservado; `TentarParsearMes`: casos válidos (`2026-09`, trim, ano de borda `0026`/`9999`),
   `null`/`""`/whitespace → mês atual UTC, e rejeição de mês 13/00, barra invertida, tamanho errado,
   caractere não dígito, ano zero e entrada com dia;
4. **serviço em nível de banco** (crash-idempotente, mundos próprios com CNPJs `999981…`, dois planos):
   a fixture do drill replicada no plano de teste → as cinco KPIs com a tabela exata acima; mundo X com
   extremos (30 plantões de 40 vagas, 10 pagamentos de R$ 1.000.000) em PLANO diferente, presente durante
   o cálculo → números de A intactos (isolamento por plano), e X no próprio plano tem dados próprios
   (6 plantões por médico) com zero pares (agregados degeneram no próprio valor, posição nula); mês
   vazio 2025-01 → tudo nulo, zero pares, `temDadosProprios=false`;
5. **sem assinatura ativa** (única assinatura `CANCELADA`, com dados de negócio no mês) →
   `Success=false`, HTTP 404 e mensagem "Assinatura ativa não encontrada para calcular o benchmark."

Suíte: 1829 → 1834.

## Gates

- Build A/B Release `--no-incremental` (tag r101): `base-exit=0 nova-exit=0`; avisos únicos **76 por
  lado**, `DIFF=NENHUMA` (reexecutado após o teste de contrato entrar no solution — o arquivo novo não
  soma aviso nenhum).
- Suíte completa pré-doc (`--no-build -c Release`): **1834/1834 aprovados** em execução única (inclui os
  5 fatos da R101 e a higiene `RepositoryHygieneContractTests`).
- Docscan pós-doc: 0 defeitos (12 arquivos verificados).
- Suíte completa pós-doc (`--no-build -c Release`): **1834/1834 aprovados** (a primeira tentativa teve um
  transiente de resíduo de `cotacoes_moedas` da própria R89 — linha `GBP|2026-10-15` criada por um fato
  interrompido; limpeza do resíduo + classe isolada 9/9 e integral completa verde logo após).
- EOL: CRLF sem BOM em todos os arquivos commitados (sweep script-local, padrão R100).
- Manifest: sem mudança nesta rodada (MUDANÇA ZERO de DDL): 147 entradas ativas, v2378 (R100) segue
  última da cadeia.

## Pendências

- **Catálogo sem grants RELATORIOS.* no perfil ADMINISTRADOR_CLIENTE** (31 permissões no módulo; o
  perfil tem zero — ADMINISTRADOR tem 31): toda a área `/Relatorios/*` é negada ao tenant admin na web
  hoje; donos de organização só chegam lá por exceção por usuário até que o catálogo ganhe os grants do
  perfil (migração futura).
- (Herdada R100) Central operacional ainda não recebe os avisos da caixa in-app (ponte caixa in-app ↔
  central operacional).
- (Herdada R100) Avisos pautados não são recolhidos ao concluir; pauta só por leitura/gatilho (sem
  background job); `notificacoes.tenant_id` nulo.
- (Herdada R99) Ativar módulos nos planos reais é ato operacional (ESSENCIAL/PROFISSIONAL/ENTERPRISE
  seguem sem linhas de vínculo).
- (Herdada R97) Renovação ANUAL no gerar-mensal cobra o preço anual todo mês — corrigir ciclo.
- (Herdada R99) Fatura pós-trial expirado + mudança/conversão de plano (sem rodada atribuída).
- (Herdada) Resíduo WP-A1 (2 usuários sem link ADMINISTRADOR_GLOBAL); ClientPortal descartado +
  `/OperacaoInteligente` órfã; órfãs adm360 vales/cotações (FKs NOT VALID); endurecer o wrapper B1
  contra stderr benigno; conciliar `scrpt_completo.sql`/`instalar_no_banco_atual.sql` × manifest;
  política de retenção de backups; retroatividade de tenants antigos via `POST /api/onboarding/sincronizar`.
