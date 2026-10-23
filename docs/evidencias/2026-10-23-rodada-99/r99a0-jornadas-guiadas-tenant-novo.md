# R99 — Jornadas guiadas para tenants novos: os módulos incluídos no plano viram contrato efetivo

## Objetivo

Linha 94 do plano (r66a0): "jornadas guiadas para tenants novos (checklist por módulo)" — o tenant que
entra pelo funil de autoatendimento nasce com a jornada de implantação materializada para os módulos
que ele de fato contratou e encontra orientação para o próximo passo em todas as telas que abre
(onboarding, minha assinatura, primeiros passos, sucesso do cadastro).

Lacunas tratadas nesta rodada:
1. **módulo incluído no plano não gerava contrato**: todos os planos vivos (ESSENCIAL, PROFISSIONAL,
   ENTERPRISE e os dos drills R97/R98) tinham ZERO linhas em `plano_modulos` — e mesmo com o vínculo
   criado, o provisionamento não materializava `tenant_modulos`: o tenant novo nascia com
   `modulos_efetivos()` vazio e a jornada por módulo nunca aparecia (sempre as 6 etapas gerais);
2. **nenhuma superfície guiava para o próximo passo**: `/Onboarding/Index` listava etapas sem ação
   hero apontando para a tela pendente; `MinhaAssinatura` não mostrava o andamento da implantação;
   `/PrimeirosPassos` caía no dicionário órfão do guard (catálogo não configurado); `/Cadastro/Sucesso`
   deixava o usuário sem saber o que acontece depois do cadastro.

Entregas: hook de kernel `ContratarModulosIncluidosDoPlanoAsync` (mesma transação do cadastro, antes da
materialização da jornada), reparo da rota de governança `vincular-plano` (latente quebrada desde R6a),
as quatro superfícies web, chaves de idioma nos 4 resx, drill E2E API+Web sobre o banco vivo e contrato
de 4 fatos.

## Recon

- Catálogo da jornada: 12 etapas com `link_acao` NUNCA nula (o CTA sempre tem alvo real), pré-requisitos
  apenas por código (`ONB_EQUIPE_ACESSO` ← convite, `ONB_PL_PUBLICAR` ← criar, `ONB_PL_ESCALA` ← publicar)
  e critérios avaliados contra dados persistidos (`EMPRESA_DADOS_COMPLETOS` exige e-mail + telefone +
  cidade + estado do cliente).
- `OnboardingJornadaService.StatusAsync` é LEITURA PURA da linha mestre; a reavaliação é lazy, só em
  `GET checklist` / `GET proxima-acao` / `POST reavaliar`. Sequência determinística: seed
  (`progresso=0`, `proxima_acao='Completar dados da empresa'`) → primeira leitura da jornada reavalia.
- O literal do seed difere do título do catálogo ('Completar dados da empresa' vs 'Completar **os**
  dados da empresa') — asserções usam fragmentos que distinguem os dois estados.
- PLANTOES é standalone (`pacote='-'`, status ATIVO) → a herança de pacotes não arrasta filhos;
  `modulos_efetivos(uuid)` devolve `(codigo, modulo_id, pacote_codigo)` = contrato direto ∪ pacotes.
- O dono (ADMINISTRADOR_CLIENTE) resolve todas as rotas-alvo pelo serviço canônico de acesso:
  USUARIOS/ONBOARDING/ASSINATURAS são módulos de administração do tenant (bypass de tenant-admin) e
  AJUDA é comum.
- `LinkPlanAsync` (`POST /api/modulos/{id}/vincular-plano`) gravava no shape ANTERIOR de
  `plano_modulos`: faltavam colunas NOT NULL (`codigo_modulo`, `habilitado`, `status`, `metadata`) e o
  insert citava colunas inexistentes (`created_by`, `updated_by`) — 500 em qualquer tentativa
  (reproduzido ao vivo no banco atual).
- A purge do drill precisou ganhar `tenant_modulos(_historico)`: nenhuma das duas tabelas tem FK para
  `tenants` (a limpeza é por `tenant_id`, na ordem históricas → contrato → tenant).

## Decisões

- **Kernel** (`SelfServiceServices.TentarProvisionarAsync`): ANTES de `CriarOnboardingAsync` (mesma
  transação), o novo `ContratarModulosIncluidosDoPlanoAsync` percorre `plano_modulos` com
  `incluido=true` do plano contratado e faz upsert em `tenant_modulos` com o conflito canônico
  `(tenant_id,modulo_id) where reg_status='A' and modulo_id is not null`: origem `PLANO`, preço 0
  (já está incluído no preço do plano), limite contratado do vínculo, ATIVO/habilitado/efetivo agora —
  idempotente (reativa como ATIVO no conflito) e rastreado com `CONTRATAR_PLANO` em
  `tenant_modulos_historico`. É o que faz a `MaterializarAsync` enxergar o módulo em
  `modulos_efetivos()` e materializar as etapas do módulo (9 etapas no mundo do drill, sem as de
  módulos não contratados).
- **Governança** (`SaasCoreServices.LinkPlanAsync`): insert alinhado ao shape vigente
  (`codigo_modulo` vem do catálogo; NOT NULLs preenchidos; sem `created_by`/`updated_by`) e alvo de
  conflito completo casando `ux_plano_modulos_ativo` — a rota pela qual a operação vincula módulos aos
  planos volta a funcionar (antes era 500 latente).
- **Web**:
  - `/Onboarding/Index`: lê o status (gate nulo), depois `GET checklist` (reavalia) e RE-LÊ o status —
    a página nunca mistura o seed do provisionamento com o estado reavaliado. O hero ganha CTA
    "Iniciar agora" → `proxima.LinkAcao` (título da etapa como `title`) com nota de cadeado quando o
    pré-requisito ainda não atende ("A etapa depende de um pré-requisito anterior da jornada"), e cada
    cabeçalho de grupo ganha o progresso "{0} de {1} concluídas".
  - `MinhaAssinatura/Index`: faixa de implantação em melhor esforço ("Sua implantação está N% pronta"
    + botão "Continuar implantação" → `/Onboarding/Index`); oculta quando a jornada está FINALIZADO ou
    a leitura falha (a tela do contrato segue normal).
  - Guard: `["PrimeirosPassos"]="AJUDA"` (módulo comum sai da lista de órfãos) + faixa de handoff na
    própria página ("Ver minha implantação").
  - `/Cadastro/Sucesso`: orientação do que acontece depois do cadastro (jornada guiada de implantação
    + menu Implantação).
  - Resx em 4 idiomas: `Onboarding.NextStepCta`, `Onboarding.NextStepBlockedNote`,
    `Onboarding.GroupProgress` (pt/en/es/fr — o contrato de paridade i18n exigiu os três também nos
    demais idiomas).
- **Drill** (`r99a_drill_jornada_tenant_novo.ps1`): API porta 52082 + Web porta 52081 contra o banco
  vivo, sem stub; o superadmin cria DOIS planos pelas rotas de admin — `r99_plano_drill` (com PLANTOES
  vinculado `incluido=true` pela rota de governança, prova real do reparo) e `r99_plano_sem_mod`
  (sem vínculo = controle); dois mundos TRIAL de 5 dias pelo funil: mundo 1 usa o plano com o módulo e
  traz telefone de contato no payload (a jornada avança por dados persistidos), mundo 2 fica geral.
  Limpeza idempotente por CNPJ (incluindo `tenant_modulos(_historico)` e `ai_*`) + por código de plano
  no início (rerun previsível); os mundos permanecem no banco ao fim.

## Achados

1. **`vincular-plano` quebrada desde R6a** (500 latente): o insert citava `created_by`/`updated_by`
   (colunas que não existem em `plano_modulos`) e omitia NOT NULLs adicionados pela granularidade de
   pacotes — nenhum plano conseguia receber módulo pela governança; coerente com o catálogo vivo zerado
   em `plano_modulos`. Corrigida e coberta pelo drill (o vínculo do mundo 1 sai por ela).
2. **Três defeitos do upsert novo, todos pegues fora do drill**: em `ON CONFLICT DO UPDATE`, coluna
   pura `ativado_em` é ambígua (tabela × `excluded`) → `excluded.ativado_em`; em CTE de modificação de
   dados, `select id ... from contratadas` colidia com a coluna `id` da tabela-alvo do segundo insert
   → qualificou `contratadas.id`; e o jsonb do histórico tinha virgula faltando entre os campos
   (reproduzido isoladamente no psql antes de publicar).
3. **PowerShell 5.1: `-like '*14%'` ancora no fim da string** — sem o `*` de fechamento só casava se o
   HTML terminasse em "14%" (reproduzido: `'x 14% y' -like '*14%'` = False). A página renderizava 14%
   desde a primeira execução; o padrão estava errado. Documentado no próprio drill.
4. **PostgreSQL não aceita `string_agg(distinct x order by y)`** na mesma chamada — o dump do drill
   lê a origem por consulta de coluna única (determinístico: o mundo tem exatamente 1 contrato).
5. **Seed ≠ catálogo**: 'Completar dados da empresa' (literal do seed do kernel) vs 'Completar os
   dados da empresa' (título do catálogo) — confundir os dois quebra a asserção do estado inicial;
   drill e contrato distinguem por fragmento.

## Evidência (execução canônica)

Linha a linha de `r99a_drill_saida.txt` (API porta 52082, Web porta 52081, banco vivo
`plantaopro_test`):

```
R99A_DRILL data=20261023-110926 db=plantaopro_test api=52082 web=52081
PORTAS_LIVRES_INICIO=SIM
ESTAGEM_1_API_NO_AR ok=SIM pid=29748
ESTAGEM_1_WEB_NO_AR ok=SIM pid=29260
PRE_LIMPEZA_RERUN ok=SIM
PA_PLANO_OK plano1=33a9deb9-6076-4702-b59f-64f31fc4a107 vinculos_plano1=1 plano2=1f32e44c-6aba-4086-a1c4-888f4e53523c vinculos_plano2=0
PA_MUNDOS_OK trial_tenant=27aa834d-3ae0-4aff-b0e5-1357a6137c0b trial_status=TRIAL semmod_tenant=35f6f6ba-f0b0-428e-bd88-ab510cb1feb8 semmod_status=TRIAL
FUNIL_STATUS_SEED http=200 status=EM_ANDAMENTO progresso=0 proxima_seed=SIM
FUNIL_CONTRATO_OK mundo1=9_moderulos_mundo2=6_geral onb=EM_ANDAMENTO_0 seed=SIM
T1_DUMP_OK arquivo=r99a_t1_contrato_banco.txt
FUNIL_REAVALIACAO_OK http=200 etapas=9 progresso=14 proxima_convite=SIM
FUNIL_CONTROLE_OK etapas=6 progresso=0 proxima_empresa=SIM
PROXIMA_ACAO_OK http=200 titulo_convite=SIM link=/ConvitesEquipe/Index status=PENDENTE
WEB_LOGIN_OK http=200
WEB_ONBOARDING_OK http=200 itens=9 cta=SIM cta_link=SIM proxima_convite=SIM progresso=14 grupo_geral=1de4 grupo_plantoes=0de3 sem_saudes360=SIM sem_adm360=SIM
WEB_LINK_PROXIMA_OK http=200
WEB_ASSINATURA_OK http=200 faixa=SIM percentual=14 botao=SIM
WEB_PRIMEIROS_PASSOS_OK http=200 handoff=SIM
WEB_SUCESSO_OK http=200 orientacao=SIM
PORTAS_LIVRES=SIM
RESULTADO=PASSEI
```

Dump t1 do banco (`r99a_t1_contrato_banco.txt`, capturado ANTES de qualquer reavaliação):

```
T1_MUNDO1 tenant=27aa834d-3ae0-4aff-b0e5-1357a6137c0b modulos=[PLANTOES] origens=[PLANO] efetivos=[PLANTOES] historico=1 checklist_total=9 codigos=ONB_EMPRESA_DADOS,ONB_EQUIPE_CONVIDAR,ONB_EQUIPE_ACESSO,ONB_IDENTIDADE,ONB_PREFERENCIAS,ONB_PL_CRIAR,ONB_PL_PUBLICAR,ONB_PL_ESCALA,ONB_REVISAO onb=EM_ANDAMENTO_0 seed_empresa=SIM
T1_MUNDO2 tenant=35f6f6ba-f0b0-428e-bd88-ab510cb1feb8 modulos=[] origens=[] efetivos=[vazio] historico=0 checklist_total=6 codigos=ONB_EMPRESA_DADOS,ONB_EQUIPE_CONVIDAR,ONB_EQUIPE_ACESSO,ONB_IDENTIDADE,ONB_PREFERENCIAS,ONB_REVISAO onb=EM_ANDAMENTO_0 seed_empresa=SIM
```

Fluxo: o superadmin cria os dois planos (o primeiro recebe PLANTOES pela governança reparada —
vínculo `incluido=true` confirmado no banco) e o funil self-service abre dois TRIAL de 5 dias. Logo
após o provisionamento, o status é o SEED puro (EM_ANDAMENTO, progresso 0, próxima "…empresa"): a
reavaliação ainda não ocorreu. O dump t1 prova o contrato já materializado na mesma transação —
mundo 1: 1× `tenant_modulos` PLANTOES de origem PLANO, efetivo em `modulos_efetivos()`, histórico
`CONTRATAR_PLANO` e checklist de 9 etapas (6 gerais + 3 de plantões, sem SAUDE360/ADM360); mundo 2
(controle): contrato vazio e checklist geral de 6. O `GET checklist` reavalia: mundo 1 com dados da
empresa completos (telefone no payload) salta para **progresso 14** (1 de 7 obrigatórias) com próxima
"Convidar o primeiro integrante da equipe"; mundo 2 permanece 0 com próxima "Completar os dados da
empresa". `GET proxima-acao` devolve o item com link não vazio, título coerente e status PENDENTE. Na
Web, com sessão do dono: `/Onboarding/Index` renderiza 9 itens, anel 14%, CTA "Iniciar agora"
apontando para `/ConvitesEquipe/Index` (alvo abre 200), grupos "1 de 4 concluídas"/"0 de 3
concluídas" e zero etapas de módulos não contratados; `/MinhaAssinatura` exibe a faixa "Sua
implantação está 14% pronta" + "Continuar implantação"; `/PrimeirosPassos` abre (guard corrigido)
com handoff; `/Cadastro/Sucesso` orienta a jornada guiada.

Banco/migrações: SEM DDL nesta rodada — `database/migration-manifest.json` byte-idêntico ao publicado
em R98 (git limpo), sem `v2378`, última versão ativa v2377 (146 entradas ativas contadas no arquivo).
Sem `r75_gap`.

## Contrato

`R99JornadaGuiadaTenantNovoContratoTests` — 4 fatos:
1. Saída do drill E2E: todos os marcadores máquina-legíveis (apps, limpeza, planos com vínculos
   1×/0× via governança, dois mundos TRIAL, seed puro EM_ANDAMENTO/0, contrato 9×/6×, dump t1,
   reavaliação 14/convite, controle 0/empresa, proxima-acao com link `/ConvitesEquipe/Index` e
   PENDENTE, as cinco telas web com CTA/anel/grupos/faixa/handoff/orientação), `RESULTADO=PASSEI`
   e zero `FALHA `;
2. Dump t1 do banco pré-reavaliação: mundo 1 `modulos=[PLANTOES]`, `origens=[PLANO]`,
   `efetivos=[PLANTOES]`, `historico=1`, `checklist_total=9` com os códigos exatos em ordem
   (sem `ONB_SD_*`/`ONB_AD_*`), `EM_ANDAMENTO_0`; mundo 2 `[]`/`[vazio]`/6 gerais;
3. Fragmentos de código: o hook do kernel roda ANTES da materialização (ordem no source), origem
   `PLANO` + `CONTRATAR_PLANO` + jsonb do histórico com a vírgula, guard `PrimeirosPassos→AJEDA`,
   controller web com re-leitura do status pós-checklist + contagem por grupo, views/resx com as
   chaves do CTA e do progresso por grupo (4 idiomas cobertos à parte pelo contrato de paridade
   i18n existente);
4. Kernel em nível de serviço no banco vivo (crash-idempotente com CNPJs/planos próprios, fixture de
   plano criada por SQL no mesmo shape da governança): plano com PLANTOES `incluido=true` →
   1× `tenant_modulos` (`PLANTOES|PLANO`), efetivo em `modulos_efetivos()`, histórico 1, checklist 9
   (0 `ONB_SD_*`/`ONB_AD_*`), seed `EM_ANDAMENTO/0`; `ReavaliarAsync` com telefone persistido →
   progresso 14 + próxima convite + etapa da empresa `CONCLUIDO/AUTOMATICO`; plano de controle sem
   vínculo → contrato vazio, 6 etapas gerais, progresso 0, próxima "Completar os dados da empresa".

Suíte: 1821 → 1825.

## Gates

- Build A/B Release `--no-incremental` (tag r99): `base-exit=0 nova-exit=0`; avisos únicos **76 por
  lado**, `DIFF=NENHUMA` (executado duas vezes; números finais da segunda, com o código final).
- Suíte completa pré-doc (`--no-build -c Release`): **1825/1825 aprovados** (~42 s) — inclui higiene
  `RepositoryHygieneContractTests`.
- Docscan pós-doc: 0 defeitos.
- Suíte completa pós-doc (`--no-build -c Release`): **1825/1825 aprovados**.
- Sem DDL: manifest byte-idêntico a R98 (git limpo), sem `v2378`, última ativa v2377; sem `r75_gap`.

## Pendências

- **Ativar módulos nos planos existentes é ato operacional**: o mecanismo (governança + kernel) agora
  funciona e está coberto, mas ESSENCIAL/PROFISSIONAL/ENTERPRISE seguem sem linhas em
  `planos`×`plano_modulos` no banco vivo — ligar módulos `incluido=true` nos planos reais fica a
  critério comercial da operação (documentado aqui como configuração, não débito de código).
- Tenants antigos sem módulos contratados continuam com a jornada geral; a retroatividade segue via
  `POST /api/onboarding/sincronizar` (mesma governança de sempre).
- **Renovação ANUAL no `gerar-mensal`** (herdada R97): um ANUAL renovaria cobrando o preço anual todo
  mês — corrigir o ciclo em rodada futura.
- Fatura pós-trial (expiração sem conversão): TRIAL vencido segue 403 até conversão; o fluxo formal de
  mudança/conversão de plano segue para a R101.
- Limpeza do resíduo WP-A1 (2 usuários sem link `ADMINISTRADOR_GLOBAL`, `tenant_id null`).
- ClientPortal continua descartado (shell demo Fase 2) e `/OperacaoInteligente` segue órfã, ambas
  pendências futuras documentadas nas rodadas anteriores.
- (Herdadas) Órfãs adm360 vales/cotações (FKs NOT VALID); endurecer o wrapper B1 contra stderr
  benigno; conciliar `scrpt_completo.sql`/`instalar_no_banco_atual.sql` × manifest (inclui v2377);
  política de retenção de backups.
