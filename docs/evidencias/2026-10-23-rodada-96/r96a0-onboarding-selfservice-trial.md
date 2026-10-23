# R96 (B6) — Onboarding self-service: o funil público ganha o teste grátis

## Objetivo

Linha 90 do plano (r66a0): "onboarding self-service (assinatura → tenant pelo motor existente → trial)".

O funil público (`POST /api/public/cadastro/finalizar`) existia desde a B5, mas sempre
provisionava **ATIVA com cobrança inicial** (`pagamentoInicial=true` hardcoded no caminho
público). O TRIAL só existia no provisionamento manual B2B. Ou seja: o cliente que chegava
pela web não tinha como testar antes de pagar — e a tela de sucesso prometia datas que nem
sempre batiam com o banco.

Entregas da rodada:
1. Núcleo do self-service (API) aceita `statusAssinatura`/`diasTrial` no caminho público;
2. Wizard web (`cadastro`) oferece as duas opções de contrato, com teste grátis como padrão
   de produto; tela de sucesso mostra as datas reais;
3. `minha-assinatura` expõe o fim do teste (KPI + DTO);
4. Migração v2375 semeia as 3 FAQ públicas do teste grátis;
5. Drill E2E `r96a_onboarding_selfservice_trial.ps1` contra o banco vivo;
6. Contrato `R96OnboardingSelfServiceContratoTests` (9 fatos; suíte 1793 → 1802);
7. Dois defeitos latentes descobertos pelo drill e **corrigidos nesta rodada** (login 401 do
   admin provisionado; off-by-one de fuso nas datas do trial).

## Recon

- **Motor existente**: `SelfServiceSaasService.TentarProvisionarAsync` é o núcleo compartilhado
  pelos dois caminhos (público e manual B2B): solicitação, tenant, cliente, assinatura, admin,
  perfil+grants, white label, onboarding+checklist, LGPD e pagamento inicial em uma transação.
  A R96 reutilizou o motor sem antecipar o esquema de planos configuráveis da R97.
- **Superfícies**: API pública anônima (`api/public/*`); wizard web com POST interno para o
  mesmo endpoint; `api/onboarding/status` + KPI de `minha-assinatura`; catálogo público atual
  = Essencial R$ 399 / Profissional R$ 899 (destaque) / Enterprise R$ 1.999 / Custom R$ 0.
- **Banco vivo**: sessão `America/Sao_Paulo`; colunas de data da assinatura são `date`;
  `plano_faq` sem coluna `codigo` (guarda de idempotência por `pergunta`).
- **Compat B5**: `ProvisionamentoB5Tests.P1–P4` cobrem o status ausente (= ATIVA com cobrança)
  e não podem mudar de comportamento; o P5 (TRIAL manual) usa tolerância de 14–15 dias nas
  datas — desenhada para absorver exatamente o ±1 dia que virou defeito no caminho público.

## Decisões

- **Default de produto no wizard: TRIAL** (radio "Começar com teste grátis" como primeira
  opção), dias 1..90 com padrão 30 e campo show/hide por JS. Assinar já continua disponível.
- **Contrato do wire (compat)**: `StatusAssinatura` ausente ⇒ ATIVA com cobrança inicial
  (comportamento B5 intacto, P1–P4 sem mudança). `TRIAL` exige `DiasTrial` explícito 1..90
  (400 "Informe os dias de teste (entre 1 e 90) para começar o teste grátis."); status
  inválido ⇒ 400 "Status da assinatura deve ser TRIAL ou ATIVA."
- **Semântica do trial**: sem cobrança inicial; obs "Criada via self-service (teste gratuito)";
  `data_fim = trial_fim + 30 dias` (janela de conversão, mesma regra do núcleo B2B); LGPD com
  base legal de consentimento; conversão trial→pago segue pelas solicitações existentes
  (R97/R101, junto com planos configuráveis).
- **Calendário local** (`DateTime.Today`) nas três posições que calculam data de contrato
  (núcleo público, núcleo manual B2B, resumo web da tela de sucesso) — ver Achado 2.
- **FAQ pública** (v2375): 3 perguntas sobre o teste grátis em `plano_faq` com `plano_id NULL`
  (global, aparece na FAQ de qualquer plano), ordem 40/50/60, idempotente por `not exists`
  na pergunta; entrada ativa no manifest dependendo de v2374.
- **Drill**: API standalone na porta 51977 contra o banco vivo; limpeza por CNPJ idempotente
  no início (rerun previsível, padrão B5); os tenants TRIAL e ATIVA permanecem no banco ao fim
  (cadastros reais, volume honesto).

## Achados

1. **O admin provisionado pelo self-service não conseguia logar (401) — corrigido**. O insert
   do núcleo não gravava `usuarios.tenant_id`, mas o `LoginAsync` resolve as roles pela chave
   `user.TenantId ?? ClienteId`; o link `usuarios_perfis`/`perfis` nasce escopado ao TENANT,
   então o filtro de roles nunca casava ⇒ zero roles ⇒ 401 "Identificador ou senha inválidos"
   para TODO admin criado pelo self-service (inclusive o mundo B5). O usuário demo funcionava
   porque carrega `tenant_id` da seed; o `ConvitesEquipeService` também grava `tenant_id` (o
   contraste confirmou o invariant). Nenhum teste anterior pegou: o B5 P1 só verificava o hash
   diretamente (`PasswordHashService.Verify`) e nunca exercia o login real. Correção no núcleo
   `TentarProvisionarAsync` (afeta público e manual B2B); travada pelo drill
   (`FUNCIONAL_API_OK login_dono=SIM` nos dois mundos), pelo fato 5 do contrato (insert com a
   coluna `tenant_id`) e pelo invariant do fato 8 (usuario.tenant_id = link = perfil).
2. **Off-by-one de fuso nas datas do trial — corrigido**. O núcleo gravava
   `UtcNow.Date.AddDays(dias)` (meia-noite UTC) em coluna `date` sob sessão
   `America/Sao_Paulo`: o Npgsql converte pelo fuso da sessão e o valor chegava **um dia a
   menos** (trial de 14 dias nascia em 05/11 em vez de 06/11 entre 0h e 3h locais, e a tela de
   sucesso — calculando à parte — mostrava a outra data). Troca para `DateTime.Today` (fuso do
   servidor = fuso operacional do tenant e do banco) nas três posições; o drill agora garante
   `trial_fim = hoje+14` e `data_fim = hoje+44` exatos, e o P5 do B5 (tolerância 14–15 d) segue
   verde.
3. **Convergência histórica do banco vivo**: ~26 migrações pendentes (v2346–v2374, nunca
   aplicadas pelo runner) foram convergidas neste upgrade, e o runner ganhou **duas
   recuperações controladas de checksum** (padrão existente v1186/clinica_financeiro_minimo)
   para os estados históricos de `2026_v1186_schema_permissoes_compatibilidade` (hash antigo
   `7a64ca1e…` no banco) e
   `2026_10_v2305_administrativo360_imutabilidade_por_papel` (hash antigo `4b4e31a9…`): ambas
   idempotentes (delete da linha antiga → reaplica → re-registra). Fecha a parte restante do
   drift de checksum herdado da R95 (que tinha alinhado o manifest, mas o estado das linhas no
   banco vivo seguia pendente de convergência).
4. **Resíduo WP-A1 pré-existente (não fatal)**: `IDENTITY_SCHEMA_INCOMPLETE
   MISSING_ADMIN_ROLE_LINK count=2` — dois usuários "Teste WP-A1" (`wpa1-…@exemplo.test`,
   2026-10-06 e 2026-10-14) com `tenant_id null` e sem link `ADMINISTRADOR_GLOBAL`: resíduo de
   testes antigos. Pendência de limpeza (não bloqueia nada hoje).
5. **Gotas operacionais do harness (PS 5.1)**: chamada de função de zero argumentos com
   parênteses (`EscreverSaida()`) é erro de parse neste ambiente (chamar sem parênteses);
   `(if …)` em posição de expressão compila mas falha em runtime (usar `if` de declaração);
   stderr nativo do psql com `EAP=Stop` vira erro terminante (o `Psq` escopa `Continue` e trata
   o erro como texto para as asserções decidirem).

## Evidência (execução canônica)

Linha a linha de `r96a_drill_saida.txt`:

```
R96A_DRILL data=20261023-062342 db=plantaopro_test porta=51977
PORTAS_LIVRES_INICIO=SIM
ESTAGEM_1_API_NO_AR ok=SIM pid=21036
ESTAGEM_2_CATALOGO_OK planos=4 plano=Profissional faq_trial=SIM
PRE_LIMPEZA_RERUN ok=SIM
PA_TRIAL_CRIADO solicitacao=7ecedc09-215b-460e-9c67-d8bcc5325237 tenant=00730695-3512-475d-9e4d-120a54f7529b cliente=3b816a7f-7da5-4809-a289-fbf8c24bfca1 assinatura=591ec7e9-4641-4c9b-ad05-cfad1f3690dd
PA_TRIAL_OK status=TRIAL trial_fim_ok=SIM fim_conv_ok=SIM cobranca_inicial=0 lgpd=consentimento onboarding=EM_ANDAMENTO checklist=6
FUNCIONAL_API_OK login_dono=SIM onboarding_http=200 minha_assinatura_http=200 minha_status=TRIAL dataTrialFim=2026-11-06 dataFim=2026-12-06
PA_DUPLICATA_OK http=400 msg_cnpj=SIM residuo=0
PA_VALIDACAO_OK casos=4 http_todos=400 residuos=0
PA_ATIVA_OK status=ATIVA trial_fim_nulo=SIM fim_1mes=SIM cobranca=ABERTO faturas_api=1 login_dono=SIM
PORTAS_LIVRES=SIM
RESULTADO=PASSEI
```

Dumps de banco (`r96a_t1_trial_banco.txt` / `r96a_t3_ativa_banco.txt`):

```
assinatura_id=591ec7e9-… status=TRIAL data_inicio=2026-10-23 data_trial_fim=2026-11-06 data_fim=2026-12-06 cobranca_inicial_linhas=0 lgpd_base_legal=consentimento aceito=true linhas=1 onboarding_status=EM_ANDAMENTO checklist_linhas=6 tenant_nome=R96 Drill Trial
assinatura_id=be2cd5f9-… status=ATIVA data_trial_fim=null data_fim=2026-11-23 cobranca_inicial_status=ABERTO faturas_api_total=1 faturas_api_cadastro=1
```

Convergência do runner após o upgrade do banco vivo:
`Migrations: aplicadas=149 esperadas=145 falhas=0 pendentes=0` (149 = 145 ativas + 4 linhas de
linhagem do instalador, conforme classificado na R95). Manifest: **145 migrações ativas**
(v2375 nova; sha_lf `e02809a9d67457fc774b4cb80d6ab44fdff9305a10f98bf8d4c879854ffc04ec`).

## Contrato

`R96OnboardingSelfServiceContratoTests` — 9 fatos:
1. Saída do drill: todos os marcadores máquina-legíveis, pid numérico, `faq_trial=SIM`,
   `RESULTADO=PASSEI` e nenhuma linha `FALHA `;
2. Dump TRIAL: uuid válido, `trial_fim = inicio + 14d`, `data_fim = trial_fim + 30d`,
   `cobranca_inicial_linhas=0`, LGPD consentimento, onboarding EM_ANDAMENTO com checklist ≥ 1;
3. Dump ATIVA legado: `data_trial_fim=null`, data_fim bem-formado, cobrança ABERTA e fatura de
   cadastro visível (compat B5);
4. O DTO expõe `StatusAssinatura`/`DiasTrial` e `MinhaAssinaturaDto.DataTrialFim`;
5. Rota `api/public/cadastro/finalizar` anônima; mensagens e alcance exatos do trial no
   serviço; insert do admin com `tenant_id` (Achado 1); `DateTime.Today` (Achado 2);
6. v2375 ativa no manifest, `dependsOn` v2374, checksum coerente com o arquivo (mesma lógica
   CRLF→LF do runner) e seed idempotente com as 3 FAQ;
7. Wizard com as duas opções de contrato e dias 1..90; Sucesso lê `ContratoResumo` e renderiza
   as datas reais; controlador web monta o resumo com calendário local; minha-assinatura expõe
   `DataTrialFim`;
8. **Serviço (banco vivo)**: world TRIAL 14d completo — `trial_fim = hoje+14` e
   `data_fim = hoje+44` exatos, zero cobrança, onboarding/LGPD, hash verificável e o invariant
   de login (`usuarios.tenant_id` alinhado a `usuarios_perfis`/`perfis`);
9. **Serviço (banco vivo)**: validações honestas — dias ausente/0/91 pedem os dias, status
   inválido pede TRIAL ou ATIVA, CNPJ duplicado 400 com um único cliente no banco.

Suíte: 1793 → 1802.

## Gates

- Build A/B Release `--no-incremental` (tag r96): `base-exit=0 nova-exit=0`; avisos únicos
  **76 por lado**, `DIFF=NENHUMA`.
- Suíte completa pré-doc (`--no-build -c Release`): **1802/1802 aprovados** (30 s) — inclui
  higiene `RepositoryHygieneContractTests` 8/8. (Na 1ª passagem o flake conhecido
  `R86TrilhosContratoTests.Remessa_cnab400_…` falhou 1× e passou isolado; protocolo de flake.)
- Docscan pós-doc: 0 defeitos.
- Suíte completa pós-doc (`--no-build -c Release`): **1802/1802 aprovados**.
- Sem `r75_gap` nesta rodada: nenhuma página/artigo novo (só views existentes editadas e FAQ
  em banco).

## Pendências

- Corrida trial→cobrança no gate de expiração e a fatura do trial em si: **R97/entitlements**
  (planos/preços configuráveis, estilo HOMOLOGACAO_360; conversão trial→pago pelas
  solicitações existentes, R97/R101).
- Limpeza do resíduo WP-A1 (2 usuários sem link `ADMINISTRADOR_GLOBAL`, `tenant_id null`).
- (Herdadas da R95) Órfãs adm360 vales/cotações (FKs NOT VALID); endurecer o wrapper B1 contra
  stderr benigno; conciliar `scrpt_completo.sql` × manifest (32 legados + seeds demo); política
  de retenção de backups.
