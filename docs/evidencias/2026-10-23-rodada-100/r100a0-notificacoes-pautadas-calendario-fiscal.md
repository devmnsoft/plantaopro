# R100 — Notificações pautadas no calendário fiscal: SLAs e escalonamento para os gestores

## Objetivo

Linha 95 do plano (r66a0): "notificações pautadas no calendário fiscal (R84) + SLAs + escalonamento" — o
calendário fiscal R84 deixa de ser apenas uma leitura: vencimentos dentro da janela oficial, que vencem
hoje e atrasados viram avisos na caixa de notificações dos gestores, com nível de SLA derivado do estado
e escalonamento direto ao dono da organização quando o atraso atinge o limiar operacional.

Lacunas tratadas nesta rodada:
1. **ninguém era avisado**: o calendário R84 mostrava os estados (ATRASADA, VENCE_HOJE…), mas o gestor
   só descobria a pendência se abrisse a tela — não havia gatilho de caixa de notificação;
2. **sem nível de prioridade**: um atraso de 2 dias e um atraso de semanas apareciam com o mesmo peso;
   não havia SLA (CRITICO/ALTA/MEDIA) derivado do estado;
3. **sem escalonamento**: obrigação fiscal parada há muito tempo não subia para o dono da organização —
   faltava o limiar operacional e o canal próprio para ele.

Entregas: serviço `CalendarioFiscalAlertasService` (API) que pauta a caixa in-app canônica, rotas
`GET alertas` / `POST notificacoes/gerar` + hook lazy na leitura do calendário, parâmetro de fonte única
`OBRIGACOES_FISCAIS_ESCALONAMENTO_ATRASO_DIAS` (migração v2378), proxy web melhor-esforço + cartão
server-side "SLA e escalonamento" em `/CalendarioFiscalWeb`, drill E2E API+Web sobre o banco vivo e
contrato de 4 fatos.

## Recon

- Motor R84 (`CalendarioFiscalService`): precedência CONCLUIDA > ATRASADA (`diasRestantes < 0`) >
  VENCE_HOJE > VENCE_EM_CURTO_PRAZO (≤ janela) > AGENDADA; janela oficial de 7 dias da fonte única;
  catálogo v2367 com 6 obrigações (mensais DAS/FGTS/DCTFWeb/eSocial/EFD + livros anuais, exibição 12).
  **Zero mudança no contrato R84** — a pauta só consome `ObterCalendarioAsync`; o motor segue sem ler o
  parâmetro novo (o serviço R100 lê, e o teste R84 continua aplicando só v2363/v2366/v2367).
- O repo tem **dois sistemas distintos** de notificação:
  - **caixa in-app** `plantaopro.notificacoes` — o caminho canônico dos avisos de domínio (34 chamadas
    de `NotificacaoService.CriarNotificacaoAsync`: escala, fatura, fechamento, contestação…), dedupe
    idempotente `(usuario_id, origem_tipo, origem_id, titulo)` parcial (v2346); exposta pelo sino do
    dashboard (contagem + últimos itens), pela rota mobile `/api/mobile/notificacoes` (gate de plano
    `permite_mobile`) e pelo portal profissional;
  - **central operacional** `plantaopro.notifications` + destinatários/categorias/prioridades — é o que
    `/Notificacoes/Index` ("Central de Notificações") e `/api/notificacoes` leem; categories fixas
    (OPERACAO/ESCALA/CLINICA/FINANCEIRO/SISTEMA), sem passagem por `CriarNotificacaoAsync`.
  A notificação fiscal usa a **caixa in-app** (coerência com todos os demais avisos de domínio); a
  ponte para a central operacional fica documentada como pendência.
- `notificacoes.tenant_id` **não é preenchido** por `CriarNotificacaoAsync` (linha escopada por
  `usuario_id`; o filtro operacional trata `tenant_id null` como visível na sessão daquele usuário) —
  escopo aceitável, documentado aqui.
- `AlternarConclusaoAsync` recebe o período **exibido** e resolve o mês de referência pelo offset do
  catálogo (exibido 10/2026 = referência 09/2026 para as mensais de competência anterior).
- Governança: `POST /api/modulos/{id}/habilitar-tenant` grava `tenant_modulos` com origem `CONTRATO` e
  histórico `HABILITAR`; `modulos_efetivos()` é o gate canônico dos módulos.
- O dono criado pelo self-service sai com os dois perfis gestores (ADMINISTRADOR_CLIENTE +
  ADMINISTRADOR) — 1 usuário distinto no mundo do drill.
- Na data do drill (23/10/2026), o período exibido 10/2026 tem as 5 mensais ATRASADA (vencimentos
  07/15/15/20/20) e 3 atrasos ≥ limiar 7 (16/8/8) → bases=5, escal=3, total=8, estável até 29/10.

## Decisões

- **Serviço** (`CalendarioFiscalAlertasService`, namespace `PlantaoPro.Api.Data`): SLA derivado do
  estado R84 (`ATRASADA→CRITICO`, `VENCE_HOJE→ALTA`, `VENCE_EM_CURTO_PRAZO→MEDIA`, resto NENHUM);
  alvos = os três primeiros estados; escalonada = `ATRASADA` com `-diasRestantes >= limiar`.
  Destinatários = usuários `reg_status='A'`, `status='ATIVO'`, perfis ADMINISTRADOR_CLIENTE ou
  ADMINISTRADOR, somente com **ADM360 efetivo** no tenant. Tudo numa transação única por período.
- **Limiar sem número no código**: lido de `parametros_legais.OBRIGACOES_FISCAIS_ESCALONAMENTO_ATRASO_DIAS`
  (migração v2378, valor 7) — o motor R84 não o lê; quem lê é só este serviço.
- **Origem determinística** (sem estado novo persistido): `origem_id` = MD5 de
  `fiscal|r100|{tenant:N}|{codigo}|{mes:D2}/{ano:D4}|{base|esc}` — reexecução cria zero pela dedupe
  canônica; a variante `esc` convive com a `base` na mesma caixa.
- **Escalonada vai só aos donos** (ADMINISTRADOR_CLIENTE), título `ESCALONADO: Obrigação fiscal {nome}`
  e origem própria `OBRIGACAO_FISCAL_ESCALONADA`; as outras vão aos dois perfis com origem
  `OBRIGACAO_FISCAL`.
- **Gatilhos**: hook melhor-esforço no `GET api/calendario-fiscal/calendario` (pauta antes de responder)
  + `POST .../notificacoes/gerar?mes&ano` (gatilho explícito idempotente) + `GET .../alertas?mes&ano`
  (visão de SLA sem efeito colateral). Sem background job: quem abre a tela pautou a caixa.
- **Web**: `Index()` do `CalendarioFiscalWebController` (BFF) busca `alertas` do mês atual em melhor
  esforço → cartão server-side **"SLA e escalonamento"** na view (badges text CRITICO/ALTA/MEDIA + chip
  ESCALONADO; cor nunca é o único canal — texto + `title`). Tabela JS R84 intocada. Sem resx nesta
  rodada (a view R84 hardcoda PT, padrão da vizinhança — paridade i18n não afetada).
- **Banco**: sem DDL estrutural — v2378 só insere a linha do parâmetro (shape v2363, `on conflict
  (codigo, ano) where reg_status='A' do nothing`); manifest +1 entrada (v2378, dependsOn v2377,
  checksum LF `251866a1d02fe88f90b5aa0a80d010589f66b2288a145688a35043e4e9ff9f38`).

## Achados

1. **Dois stores de "notificações"**: `/api/notificacoes` e `/Notificacoes/Index` leem a central
   OPERACIONAL (`plantaopro.notifications` + recipients, categorias fixas) — **não** a caixa in-app
   `plantaopro.notificacoes`. O primeiro CAIXA do drill falhou asserindo no store errado; a lista
   canônica da caixa in-app é `/api/mobile/notificacoes` (gate de plano `permite_mobile`), adotada no
   drill com o plano fixture liberando mobile.
2. **Dapper × CancellationToken**: argumento posicional após os parâmetros liga em `IDbTransaction?`
   (CS1503 no `QueryAsync`) → `new CommandDefinition(sql, param, cancellationToken: ct)` em todo call do
   serviço. Outros dois erros do build #1: CS8852 (propriedade init-only fora de initializer) e CS0019
   (`??` entre `List<T>` e `T[]`).
3. **PS 5.1: helper que devolve objeto sem `.Content`** — `$rCai.Content` era `$null` e
   `$null -like '*ESCALONADO*'` era false silencioso (a contagem `fiscais=8` passava enquanto o "corpo"
   estava ausente); o `HttpRequest` do drill agora devolve o corpo nos dois caminhos (sucesso/erro).
4. **Regra R60 pega formatação de data**: `Substring(0, 4)` em Razor viola
   `Views_SemFragmentosTruncadosDeIdentificador` (fatia de identificador sem valor inteiro na linha) —
   o cartão usa `{DataVencimento:dd/MM/yyyy}` (formato sem truncamento).
5. **Parâmetro novo mexe nos contratos anteriores** (fonte única é o contrato): contagem fixa do R84
   (7→8 regras de vigência nula) e bateria do R90 (116→117 parâmetros ativos de 2026 — nome do fato e
   asserções) atualizados; e o código precisou entrar na lista do tema CALENDARIO_FISCAL em
   `BateriaLegalService` — sem isso cairia em "DIVERSOS", tema que o contrato do R90 veta.

## Evidência (execução canônica)

Linha a linha de `r100a_drill_saida.txt` (API porta 52084, Web porta 52083, banco vivo
`plantaopro_test`, execução de 23/10/2026, DLLs Release do gate r100):

```
R100A_DRILL data=20261023-121642 db=plantaopro_test api=52084 web=52083
PORTAS_LIVRES_INICIO=SIM
ESTAGEM_1_API_NO_AR ok=SIM pid=22956
ESTAGEM_1_WEB_NO_AR ok=SIM pid=8460
PRE_LIMPEZA_RERUN ok=SIM
PA_PLANO_OK plano=581d498b-b5ef-4149-a8eb-d7c733809ee0 vinculos=0
PA_MUNDOS_OK adm_tenant=38c68d01-59b5-436f-af4a-7c95de3faef5 adm_status=TRIAL ctrl_tenant=63abb3d5-74b0-446d-a89b-9ee3688012ed ctrl_status=TRIAL
PA_ADM360_OK http=200 origem=CONTRATO historico=1 efetivos=[ADM360]
CALENDARIO_PAUTA_OK http=200 itens=5 atrasadas=5
T1_DUMP_OK arquivo=r100a_t1_alertas_banco.txt linhas=2
GERAR_IDEMPOTENTE_OK http=200 criadas=0 alertas=5 total_mantido=8
CAIXA_OK http=200 fiscais=8 escalonado=SIM base=SIM
NOVEMBRO_AGENDADO_OK http=200 agendadas=5 criadas=0 total_mantido=8
CONCLUIDA_OK http=200 alertas_apos=4 total_mantido=8
CONTROLE_OK http=200 criadas=0 alertas=5 total=0
WEB_LOGIN_OK http=200
WEB_CALENDARIO_OK http=200 cartao=SIM escalonado=SIM critico=SIM
WEB_NOTIFICACOES_OK http=200
PORTAS_LIVRES=SIM
RESULTADO=PASSEI
```

Dump t1 do banco (`r100a_t1_alertas_banco.txt`, capturado **antes** do gatilho explícito — a leitura
lazy do calendário já pautou a caixa):

```
T1_MUNDO_A tenant=38c68d01-59b5-436f-af4a-7c95de3faef5 efetivos=[ADM360] bases=5 escal=3 total=8 lida_falsa=8 dist_id=8
T1_MUNDO_B tenant=63abb3d5-74b0-446d-a89b-9ee3688012ed efetivos=[vazio] bases=0 escal=0 total=0 lida_falsa=0 dist_id=0
```

Fluxo: o superadmin cria o plano de drill (sem vínculos) e o funil self-service abre dois mundos TRIAL
de 5 dias (mundo A / mundo controle). Só o mundo A recebe ADM360 pela rota real de governança
`habilitar-tenant` (contrato `CONTRATO`, histórico `HABILITAR`, `modulos_efetivos=[ADM360]`). Com
23/10/2026, o período exibido 10/2026 tem as 5 mensais ATRASADA (janela oficial 7 confirmada na
resposta) → o hook lazy do `GET calendario` cria **antes de responder** 5 avisos base + 3 escalonados
(atrasos 16/8/8 ≥ limiar 7) — o t1 prova o estado no banco. O gatilho explícito `gerar` cria
**zero** (dedupe canônica), a caixa in-app lista 8 itens de tipo `fiscal` com títulos base e
ESCALONADO, o próximo período exibido (11/2026) está todo AGENDADA fora da janela e pauta nada,
marcar DAS como concluída (período exibido 10 → referência 09/2026) a tira dos alertas (4 após) sem
duplicar a caixa (8 mantidos, avisos nunca são recolhidos), e o mundo controle vê o SLA (5 alertas,
3 escalonáveis) mas sua caixa recebe zero (sem ADM360 não há destinatários). Na Web, com sessão do
dono: `/CalendarioFiscalWeb/Index` renderiza o cartão server-side "SLA e escalonamento" com badges
CRITICO/ESCALONADO reais e `/Notificacoes/Index` abre 200.

Banco: migração `2026_10_v2378_r100_notificacoes_calendario_fiscal` aplicada no banco vivo via runner
(`upgrade` — "aplicada."), parâmetro `OBRIGACOES_FISCAIS_ESCALONAMENTO_ATRASO_DIAS=7` ativo; manifest
com 147 entradas ativas, v2378 última (dependsOn v2377, checksum acima). O runner reporta o resíduo
conhecido `IDENTITY_SCHEMA_INCOMPLETE MISSING_ADMIN_ROLE_LINK count=2` (WP-A1, herdado e documentado).

## Contrato

`R100NotificacoesCalendarioFiscalContratoTests` — 4 fatos:
1. Saída do drill E2E: todos os marcadores máquina-legíveis (apps, limpeza, plano sem vínculos, dois
   mundos TRIAL, ADM360 via governança real CONTRATO/HABILITAR efetivo, calendário 5×ATRASADA com
   pauta lazy, t1, gerar idempotente criadas=0/total 8, caixa fiscais=8 com ESCALONADO+base, novembro
   agendado criadas=0, concluída alertas 4/total 8, controle criadas=0/alertas 5/total 0, cartao web
   com escalonado+critico, portas livres), `RESULTADO=PASSEI` e zero `FALHA `;
2. Dump t1 pré-gatilho: mundo A `efetivos=[ADM360]`, `bases=5`, `escal=3`, `total=8`, `lida_falsa=8`,
   `dist_id=8` (origens determinísticas distintas); mundo B `[vazio]` tudo zero;
3. Fragmentos de código + funções puras: mapa completo de SLA pelo estado, fronteira exata do
   escalonamento (−7 passa, −6 não; só ATRASADA escala), constantes de origem, MD5 determinístico,
   gate `ADM360` efetivo, lista de perfis gestores, dedupe canônica, rotas `alertas`/`notificacoes/gerar`
   + hook no `calendario`, DI em Program, proxy web + view com o cartão, migração v2378 e entrada do
   manifest (source, checksum, status active, dependsOn v2377);
4. Serviço em nível de banco com hoje fixado em 2026-10-23 (crash-idempotente, CNPJs/planos próprios,
   ADM360 concedido no shape da governança): 5 alertas todos ATRASADA/CRITICO, 3 escalonadas, 8
   criadas (5×base + 3×escal) todas `tipo='fiscal'` não lidas; reexecução cria zero mantendo 8 origens
   distintas; período 11/2026 tudo AGENDADA e pauta zero; mundo sem ADM360 enxerga 5 alertas/3
   escalonáveis com caixa zerada.

Suíte: 1825 → 1829.

## Gates

- Build A/B Release `--no-incremental` (tag r100): `base-exit=0 nova-exit=0`; avisos únicos **76 por
  lado**, `DIFF=NENHUMA` (executado duas vezes; números finais do segundo, com o código final — a view
  e a lista da bateria mudaram entre uma e outra).
- Suíte completa pré-doc (`--no-build -c Release`): **1829** fatos — 1828 aprovados na integral +
  reexecuções isoladas aprovadas (`R86Trilhos…Remessa_cnab400…`, flake documentado no checkpoint;
  contenção infra passageira em fixtures de `Administrativo360OcorrenciasA12Tests` e em
  `R84…GarantirSchemaAsync` sob paralelismo, ambas passando isoladas). Inclui higiene
  `RepositoryHygieneContractTests`.
- Docscan pós-doc: 0 defeitos.
- Suíte completa pós-doc (`--no-build -c Release`): **1829/1829 aprovados**.
- EOL: CRLF sem BOM em todos os arquivos commitados (sweep script-local, padrão R99).
- Manifest: 147 entradas ativas, v2378 última da cadeia (dependsOn v2377); sem `r75_gap`.

## Pendências

- **Central operacional ainda não recebe os avisos fiscais**: `/Notificacoes/Index` lê
  `plantaopro.notifications` (recipients/categorias próprias) — a ponte caixa in-app ↔ central
  operacional (ou mover o alvo) é rodada futura; por ora o sino do dashboard, o portal profissional e
  a rota mobile exibem os avisos pautados.
- `tenant_id` segue nulo nas linhas criadas por `CriarNotificacaoAsync` (escopo por `usuario_id` é
  suficiente hoje; se um usuário ganhar multi-tenancy, revisar).
- Pautas dependem de leitura do calendário ou gatilho explícito (sem background job) — tenant que nunca
  abre a tela pauta na primeira abertura (comportamento documentado, não débito).
- Avisos já criados não são recolhidos quando a obrigação é concluída (o alerta permanece como registro
  histórico na caixa; só o conjunto futuro de alertas muda) — comportamento canônico, documentado aqui.
- (Herdada R99) Ativar módulos nos planos reais é ato operacional (ESSENCIAL/PROFISSIONAL/ENTERPRISE
  seguem sem linhas em `planos`×`plano_modulos`).
- (Herdada R97) Renovação ANUAL no gerar-mensal cobra o preço anual todo mês — corrigir ciclo.
- (Herdada R99) Fatura pós-trial expirado + mudança/conversão de plano → R101.
- (Herdada) Limpeza do resíduo WP-A1 (2 usuários sem link ADMINISTRADOR_GLOBAL); ClientPortal
  descartado + `/OperacaoInteligente` órfã; órfãs adm360 vales/cotações (FKs NOT VALID); endurecer o
  wrapper B1 contra stderr benigno; conciliar `scrpt_completo.sql`/`instalar_no_banco_atual.sql` ×
  manifest (agora incluindo v2378); política de retenção de backups; retroatividade de tenants antigos
  via `POST /api/onboarding/sincronizar`.
