# R97 (B6) — Planos/preços configuráveis + entitlements por plano

## Objetivo

Linha 91 do plano (r66a0): "planos/preços configuráveis + entitlements (estilo
HOMOLOGACAO_360)" — o catálogo comercial deixa de ser fixo no código e passa a ser
configurado pela administração, com o funil self-service respeitando o que cada plano
oferece.

Lacunas tratadas nesta rodada:
1. `planos.valor_anual` era **coluna morta** (NOT NULL DEFAULT 0): o self-service ANUAL
   contratava sempre `valor_mensal`;
2. o CRUD `api/planos` ignorava `codigo/slug/publico/destaque/ordem/valor_anual/
   permite_white_label/permite_trial/dias_trial` (criar/editar só tocavam nome, preço
   mensal e limites);
3. `permite_trial`/`dias_trial` não tinham efeito nenhum: qualquer plano dava teste grátis
   de até 90 dias;
4. conversão trial→pago **inexistente** (pendência herdada da R96): alterar-plano/editar
   nunca viravam o status nem limpavam `data_trial_fim`, e o cliente convertido seguia
   barrado pelo gate do trial vencido.

Entregas: resolução única de preço (`PrecosPlano`), CRUD administrativo estendido com
validações e 409 honesto, catálogo público com preços efetivos e entitlements, núcleo
self-service travando o trial por plano, nova rota `converter-para-paga` com gate
aberto, migração v2376 (código canônico único + backfill), drill E2E e contrato de 10 fatos.

## Recon

- **Coluna morta**: `valor_anual numeric NOT NULL DEFAULT 0`; nenhum SELECT do funil ou do
  admin a usava; o insert do núcleo gravava `valor_mensal` como `valor_contratado` em toda
  periodicidade.
- **CRUD**: `PlanosController` (criar/editar/listar/detalhar/matriz/recursos) mapeava um
  subconjunto dos campos; request/DTO sem código/slug/visibilidade/preço anual/trial.
- **Entitlements**: `Coalesce(permite_trial,true)` e `dias_trial` existiam no schema desde
  o legado (seed padrão 14 dias), sem consumo algum.
- **Conversão**: pendência registrada na R96 ("corrida trial→cobrança no gate de expiração
  e a fatura do trial em si: R97/entitlements").
- **Fatos do domínio**: TRIAL nunca gera fatura (`gerar-mensal` só cobra ATIVA);
  `limite<=0` já significava ilimitado (HOMOLOGACAO_360 vive com zeros = ambiente
  ilimitado custo zero); `HOMOLOGACAO_360` é seed de desenvolvimento (não vem de
  migração); datas de contrato em calendário local (`DateTime.Today`, regra da R96).

## Decisões

- **Fonte única de preço** `PrecosPlano.ValorContratado(mensal, anual, periodicidade)`:
  ANUAL ⇒ `anual > 0 ? anual : round(12×mensal,2)` (nunca preço inventado); MENSAL/ausente
  ⇒ mensal. Usada no núcleo self-service (assinatura + cobrança inicial) e na conversão.
- **CRUD administrativo** estendido (request/DTO/insert/update/listar/detalhar/matriz):
  código canônico `^[A-Z][A-Z0-9_]{0,39}$` normalizado para maiúsculas (aceita minúsculas
  na entrada); slug `^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$` normalizado para minúsculas;
  `valor_anual ≥ 0`; `dias_trial 0..90` (0 = sem teto); `ordem 1..9999`. Duplicata de
  código/slug entre ativos ⇒ **409** ("Código de plano já está em uso por outro plano." /
  "Slug de plano já está em uso por outro plano.") capturando 23505.
- **Migração v2376**: índice único parcial `ux_planos_codigo` sobre `upper(codigo)`
  (ativos), backfill `codigo` a partir do slug (8 planos legados ganham código estilo
  HOMOLOGACAO_360) e `dias_trial 14 → 0` (sem teto) nos legados — o teto genérico de 90
  por requisição segue valendo; entrada ativa no manifest dependendo de v2375.
- **Trial por plano**: o núcleo (`TentarProvisionarAsync`) trava dentro da transação,
  antes de gravar mundo: `permite_trial=false` ⇒ 400 "O plano {nome} não oferece teste
  gratuito."; `dias_trial>0` e pedido acima ⇒ 400 "O plano {nome} oferece teste gratuito
  de até {N} dias." Coalesce `permite_trial,true` (coluna nula = oferece). Sem resíduo de
  cliente/tenant em caso de bloqueio (tudo-ou-nada da transação).
- **Catálogo público** expõe por plano `valorAnualEfetivo` (CASE SQL: anual quando > 0,
  senão 12× mensal), `valorAnual`, `permiteTrial`, `diasTrial`; planos internos
  (`publico=false`) seguem ocultos (HOMOLOGACAO_360 etc.).
- **Wizard web** (`cadastro`): opções do plano carregam `data-mensal/data-anual/
  data-permite-trial/data-dias-trial`; o preço exibido segue a periodicidade escolhida
  (valor anual efetivo vem do servidor — nada de precificar no cliente); radio TRIAL
  desabilita quando o plano selecionado não oferece (e realoca para assinar agora se
  estava ativo); teto do plano limita o campo de dias (0 mantém os 90 da requisição).
- **Conversão trial→pago** `POST /api/assinaturas/{id}/converter-para-paga`
  (`ConversaoTrialService.ConverterAsync`, global/admin): transação com advisory lock por
  cliente (mesma chave `'assinaturas:'+clienteId` do padrão B6) + `for update`; apenas
  TRIAL converte (400 "Apenas assinaturas em teste (TRIAL) podem ser convertidas para
  paga."); justificativa obrigatória (400 "Justificativa obrigatória para converter o
  teste em plano pago."); destino precisa estar ativo (400 "Plano escolhido não está
  ativo."); periodicidade MENSAL/ANUAL (400 "Periodicidade deve ser MENSAL ou ANUAL.").
  Efeito: status ATIVA, `data_trial_fim=null`, `data_inicio=hoje`, `data_fim` pelo ciclo
  final (1 mês/1 ano), `valor_contratado` por `PrecosPlano`, histórico
  `acao='CONVERSAO_TRIAL_PAGO'`, auditoria (ação Editar com detalhe) e 23505→409.
  O gate (`ObterUsoPlano`) abre imediatamente na chamada seguinte.
- **Ciclo do ANUAL**: `data_fim = hoje + 1 ano` tanto no provisionamento ATIVA ANUAL
  quanto na conversão (antes era sempre +1 mês — coerente quando o preço cobrado era o
  mensal). Ver Achado 2.
- **Drill**: API standalone na porta 52077 contra o banco vivo; admin global
  (`docs/usuarios-teste.md`) cria/edita o plano `R97_PLANO_DRILL` (mensal 100 / anual
  1150→1250 / teto 5→7→5 / trial on→off→on); limpeza idempotente por CNPJ/código no
  início; mundos TRIAL (convertido) e ATIVA ANUAL permanecem no banco ao fim.

## Achados

1. **ANUAL contratava o preço mensal — corrigido**. `valor_anual` nunca saiu do insert:
   o núcleo usava `valor_mensal` como contratado e como cobrança inicial em qualquer
   periodicidade, e `data_fim` sempre corria 1 mês. Agora ANUAL cobra o preço anual
   configurado (fallback honesto 12× mensal quando a coluna está zerada) e corre 1 ano —
   travado pelo drill (`PA_ANUAL_OK valor=1250.00 cob=1250.00 fim_1ano=SIM`) e pelos
   fatos 3/7 do contrato (resolver puro + banco vivo).
2. **Comportamento mudado (combinado)**: `data_fim` de ATIVA ANUAL passou de +1 mês para
   +1 ano, alinhado ao preço agora anual. Nenhum teste anterior cobria ATIVA ANUAL
   (novidade desta rodada); o MENSAL segue +1 mês intacto (P1–P4 do B5 e drill verde).
3. **Fixture do B5 dependia do default do schema**: o insert de teste do plano "B5 Plano"
   não especificava `dias_trial` (default legado 14) e provisionava TRIAL de 15 dias —
   passou a explicitar `dias_trial=0` (sem teto), preservando o foco do teste (teto
   genérico 1..90 do núcleo, não teto por plano).
4. **Gate fechado × conversão (pendência R96 resolvida)**: com `data_trial_fim`
   retrocedida, `/api/minha-assinatura/uso` responde 403 "Período experimental encerrado"
   mesmo assim a conversão é permitida (é o caminho de saída do trial) e o gate abre na
   leitura seguinte — corrida herdada da R96 coberta ponto a ponto no drill
   (`PA_GATE_VENCIDO_OK` → `PA_CONVERSAO_OK` → `PA_GATE_ABERTO_OK`) e no fato 8.

## Evidência (execução canônica)

Linha a linha de `r97a_drill_saida.txt` (API porta 52077, banco vivo `plantaopro_test`):

```
R97A_DRILL data=20261023-082250 db=plantaopro_test porta=52077
PORTAS_LIVRES_INICIO=SIM
ESTAGEM_1_API_NO_AR ok=SIM pid=45012
PRE_LIMPEZA_RERUN ok=SIM
PA_PLANO_ADMIN_OK http=200 plano=cf1252ae-72a9-4090-95bb-e9b26b6908c9
PA_DUPLICA_CODIGO_OK http=409 msg_em_uso=SIM
PA_PLANO_EDIT_OK http=200 valor_anual=1250.00 dias_trial=7
PA_PLANO_EDIT2_OK http=200 dias_trial=5
PA_CATALOGO_OK total=5 tem_r97=SIM anual_efetivo=1250.00 interno=NAO
PA_TRIAL_TETO_OK http=400 msg_teto=SIM residuo=0
PA_TRIAL_SEM_PERMISSAO_OK http=400 msg_sem_trial=SIM residuo=0
PA_TRIAL_OK tenant=09128386-93b3-410d-bfc0-98c836d572cd assinatura=eb98d599-cefe-4503-ab10-26591a7739f4 status=TRIAL trial_fim_ok=SIM cob_inicial=0
PA_ANUAL_OK tenant=0254c3a8-3731-4e6b-a90c-d897556816a4 assinatura=539b4fea-dafb-4f54-ad2f-c57a142f8851 status=ATIVA periodicidade=ANUAL valor=1250.00 cob=1250.00 fim_1ano=SIM
PA_GATE_ATIVO_OK minha_http=200 minha_status=TRIAL uso_http=200
PA_GATE_VENCIDO_OK uso_http=403 msg_encerrado=SIM
PA_CONVERSAO_OK http=200 status=ATIVA trial_fim=nulo valor=100.00 fim_1mes=SIM historico=1
PA_CONVERSAO_DUPLO_OK http=400 msg_apenas_trial=SIM
PA_GATE_ABERTO_OK minha_http=200 minha_status=ATIVA uso_http=200
PORTAS_LIVRES=SIM
RESULTADO=PASSEI
```

Banco vivo pós-upgrade: `aplicadas=150 esperadas=146 falhas=0 pendentes=0` (150 = 146
ativas + 4 linhas de linhagem do instalador, classificação da R95). Manifest:
**146 migrações ativas** (v2376 nova; sha_lf
`775a8835202f3a49ef397a7a7c7b9aaff00e2093cb58d562c67ec058e29694b4`). Backfill verificado:
8 códigos legados preenchidos a partir do slug, `dias_trial=0` nos planos semeados e
índice `ux_planos_codigo` presente no vivo.

## Contrato

`R97PlanosConfiguraveisEntitlementsContratoTests` — 10 fatos:
1. Saída do drill: todos os marcadores máquina-legíveis (admin CRUD, 409, catálogo,
   tetos, TRIAL OK, ANUAL 1250, gate vencido→conversão→gate aberto), `RESULTADO=PASSEI` e
   nenhuma linha `FALHA `;
2. v2376 ativa no manifest, `dependsOn` v2375, checksum coerente (lógica CRLF→LF do
   runner), fragmentos do SQL, índice no vivo, backfill completo e `dias_trial=0` em
   ESSENCIAL/HOMOLOGACAO_360;
3. `PrecosPlano.ValorContratado` puro (anual > 0, fallback 12×, MENSAL, tolerância de
   caixa/espaços) + uso confirmado no núcleo e na conversão;
4. **Controller (banco vivo)**: criar/edita com código/slug/preço anual/teto/visibilidade;
   duplicata de código 409 "em uso"; código fora do formato 400; `dias_trial=91` 400
   "entre 0 (sem teto) e 90";
5. **Catálogo público**: plano próprio com `valorAnualEfetivo` configurado, ESSENCIAL com
   fallback 12× (4788.00), entitlements expostos e HOMOLOGACAO_360 ausente;
6. **Núcleo (banco vivo)**: plano sem trial ⇒ 400 sem resíduo; teto 5 com pedido 6 ⇒ 400
   sem resíduo; pedido 5 ⇒ mundo TRIAL com `trial_fim = hoje+5` exato;
7. **Núcleo (banco vivo)**: ATIVA ANUAL contrata 1188 (configurado) com ciclo de 1 ano;
   MENSAL contrata 100 com ciclo de 1 mês;
8. **Conversão (banco vivo)**: trial vencido fecha o gate (403 "experimental encerrado");
   converter ⇒ ATIVA + `data_trial_fim` limpa + preço mensal + `data_fim hoje+1m` +
   histórico `CONVERSAO_TRIAL_PAGO` + gate reaberto; segunda conversão 400 "apenas
   assinaturas em teste";
9. **Conversão p/ outro plano ANUAL**: destino troca `plano_id`, preço anual do destino,
   periodicidade ANUAL e ciclo de 1 ano;
10. Wire pinado no source: rota `converter-para-paga`, mensagens do núcleo, coalesce do
    entitlement, lock advisory, ação de histórico e os `data-*` do wizard.

Suíte: 1802 → 1812.

## Gates

- Build A/B Release `--no-incremental` (tag r97): `base-exit=0 nova-exit=0`; avisos únicos
  **76 por lado**, `DIFF=NENHUMA`.
- Suíte completa pré-doc (`--no-build -c Release`): **1812/1812 aprovados** (~40 s) —
  inclui higiene `RepositoryHygieneContractTests` 8/8. (Na 1ª passagem o flake conhecido
  `Administrativo360CotacoesXmlDashboardTests.Aceite17_PersistenciaDuravel` falhou 1× e
  passou na repetição; protocolo de flake.)
- Docscan pós-doc: 0 defeitos.
- Suíte completa pós-doc (`--no-build -c Release`): **1812/1812 aprovados**.
- Sem `r75_gap` nesta rodada: nenhuma página/artigo novo (só a view existente
  `Cadastro.cshtml` editada).

## Pendências

- **Renovação ANUAL no `gerar-mensal`**: o gerador mensal cobre `valor_contratado`
  independentemente da periodicidade — um ANUAL renovaria cobrando o preço ANUAL todo
  mês. Corrigir o ciclo de renovação em rodadas futuras (fora do escopo R97; hoje nenhum
  ANUAL existe em produção, apenas no drill/bancos de teste).
- Fatura pós-trial (expiração sem conversão): TRIAL vencido segue travado pelo gate 403
  até a conversão manual/por solicitação (fluxo de solicitações de mudança de plano
  segue para a R101, junto com a máquina de conversões).
- Limpeza do resíduo WP-A1 (2 usuários sem link `ADMINISTRADOR_GLOBAL`, `tenant_id null`).
- (Herdadas) Órfãs adm360 vales/cotações (FKs NOT VALID); endurecer o wrapper B1 contra
  stderr benigno; conciliar `scrpt_completo.sql` × manifest (32 legados + seeds demo);
  política de retenção de backups.
