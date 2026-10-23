# R102 — Regulação da triagem, experiência PWA de triagem/recepção, cobrança e integridade adm360

## Objetivo

Linha 97 do plano (r66a0): "experiência PWA para triagem/recepção". O escopo da rodada foi ampliado a
pedido do usuário e fechou, num único pacote de mudanças, quatro frentes:

1. **(C) Regulação** — área nova de regulação/orientação do fluxo assistencial integrada ao PWA: o
   paciente chega, é triado e **regularizado** para o profissional/vaga certa, com a decisão registrada
   no encaminhamento (médico de destino opcional + orientação obrigatória);
2. **(A) Cobrança** — regras pendentes herdadas: ciclo anual no gerar-mensal (R97), expiração de trial
   vencido e mudança de plano pela fonte única de preços;
3. **(B) PWA** — service worker v2 network-first com fallback offline determinístico, página offline em
   PT e botão de instalação discreto para o celular (antes do paciente chegar ao balcão);
4. **(D) Integridade adm360** — pendência herdada da R95: duas FKs NOT VALID com órfãs reais.

Lacunas tratadas:
- a triagem saía do balcão sem registro de decisão: o upsert do encaminhamento `CONSULTA` só existia no
  `finalizar`, sem médico de destino nem orientação;
- o `gerar-mensal` faturava assinaturas ANUAL todos os meses (fora do ciclo) e não respeitava `data_fim`;
- trial vencido sem conversão ocupava o slot ATIVA/TRIAL do cliente para sempre (sem caminho de saída);
- `alterar-plano` trocava o plano sem recalcular `valor_contratado`;
- o site tinha service worker v1 sem fallback de navegação offline nem affordance de instalação;
- `adm360_vale_itens_vale_id_fkey` e `adm360_cotacao_envios_resposta_id_fkey` viviam NOT VALID com 580 e
  224 linhas órfãs herdadas.

Entregas: migracao v2379 idempotente/transacional; kernel `regularizar` na API clínica + rota + BFF +
botão na tela da triagem + 2 novas chaves i18n nos quatro resx; três regras de cobrança em
`SaasCommercialController`; pacote estático PWA (SW v2, offline.html, pwa-install.js, wiring do _Layout);
drill ponta a ponta API+Web sobre o banco vivo e contrato de 8 fatos.

## Recon

- Ponta do manifest = v2378 (R100); a v2379 entra como `2026_10_v2379_r102_regulacao_triagem_integridade_adm360`
  (checksum LF `df3dd62b4561e3359e3902609a33cefaf001d2bae01e0b2277fa07f44b611e53`, dependsOn v2378,
  transacional).
- `triagem_encaminhamentos` tem 12 colunas (id, cliente_id, triagem_id, destino, status, medico_id,
  observacao + auditoria) e o unique parcial `ux_v2160_encaminhamento_triagem_consulta` sobre
  `(cliente_id, triagem_id, destino) WHERE destino='CONSULTA' AND reg_status='A'` — ele é o alvo do
  `ON CONFLICT` do upsert da regulação (Achado 1 corrige o insert que citava colunas inexistentes).
- O check `ck_assinaturas_status_saas` só permite `TRIAL/ATIVA/SUSPENSA/CANCELADA/VENCIDA` → o trial
  vencido vira **VENCIDA** (o estado do domínio para "expirada"), sem DDL novo (Achado 2).
- `assinaturas.data_trial_fim` é DATE e `valor_contratado` numeric; `PrecosPlano(valor_mensal,
  valor_anual, periodicidade)` já é a fonte única testada (R97).
- Guard web (`SaasRouteGuardFilter`) exige `HasPermission(TRIAGEM, EDITAR)` **e** `IsModuleEnabled(TRIAGEM)`:
  o módulo TRIAGEM já está efetivo no tenant via dependências do SAUDE360, mas o perfil
  **ADMINISTRADOR_CLIENTE tem zero grants TRIAGEM.\*** (catálogo global tem 25 permissões TRIAGEM.\*)
  → o drill concede por `usuario_permissoes_especiais`, padrão R101 (Achado 3).
- As páginas de autenticação usam `_AuthLayout` (minimal, sem wiring PWA por design); todas as telas de
  triagem/recepção usam `_Layout` → o wiring PWA vive no `_Layout` e a asserção de layout do drill é feita
  na tela autenticada `/Triagem/Edit` (Achado 6).
- `auditoria_acoes_criticas` é a tabela de auditoria real (`auditoria` antiga tem outra estrutura e o
  insert legado dela falha em silêncio no banco vivo); o kernel grava `"SAUDE360_" + acao`.
- Mundo do drill: funil público com cinco CNPJs determinísticos — reg `99009009000257` (usuário
  `r102-drill-reg@plantaopro-local.test`), M mensal `99011011000303`, A anual `99012012000326`,
  F fim-período 2026-09 `99014014000372`, T trial vencido `99013013000349` — e dois planos
  (`r102_plano_drill` 100/1188, `r102_plano_b` 250/2880) limpados por código a cada rerun.

## Decisões

Semântica travada (fechada pelos asserts do drill + dump t1 + contrato):

- **Regularizar** = finalizar tipado + decisão de regulação:
  - `POST api/triagens/{id}/regularizar` (body `TriagemUpdateRequest` + `MedicoId`): validação clínica
    completa (`ClinicalMeasurements.Validar(request, true)`) + `PacienteId` obrigatório (400) e
    **orientação obrigatória** (`Observacoes` vazia → 400 "Regulação exige orientação registrada.");
  - kernel força status `FINALIZADA`; pré-check do agendamento em
    `{CHECKIN_REALIZADO, EM_TRIAGEM, AGUARDANDO_CONSULTA}` — fora disso, 409 "Regulação exige o paciente
    no fluxo de atendimento (após o check-in)" (sem check-in não há paciente em triagem para regular);
  - retry seguro: triagem já `FINALIZADA` devolve o registro (idempotente);
  - efeitos numa única transação: agendamento → `AGUARDANDO_CONSULTA` + `medico_id` (case-when, preserva
    o anterior se a regulação não informar médico); upsert do `triagem_encaminhamentos` destino `CONSULTA`
    status `ENCAMINHADA` com `on conflict` no unique parcial existente (`coalesce(excluded.medico_id, ...)`
    preserva médico já registrado); histórico enriquecido **só para regularizar**
    `{acao, medicoDestino, orientacao, motivo, justificativa}`; auditoria `SAUDE360_regularizar` em
    `auditoria_acoes_criticas` (delta +1 assertado pelo drill).
- **Web/BFF**: o botão Regularizar ganha vida quando `vm.Id.HasValue` (antes, só o Encaminhar desabilitado)
  → `POST /Triagem/Regularizar` (BFF valida Id/Paciente/orientação no client e chama
  `Saude360WebService.RegularizarTriagemAsync`); chaves novas `Clinical.ButtonRegulateTriage` e
  `Clinical.TriageReferTitle` nos quatro resx (paridade de presença coberta pelo contrato).
- **Gerar-mensal**: literal `upper(a.status)='ATIVA'` preservado (contrato CobrancaB6 C7); ciclo anual
  travado por dois gates aditivos: ANUAL fatura somente quando `mês(data_inicio) = mês(competência)` e
  nenhuma fatura nasce quando `data_fim < competência`, comparando ordinal `ano*12+mês` (imune a fuso).
- **Expirar-vencidos**: `POST api/assinaturas/trials/expirar-vencidos` (admin global) seleciona TRIAL com
  `data_trial_fim::date < hoje` → marca `VENCIDA`, grava histórico `TRIAL_EXPIRADO` ("Trial vencido sem
  conversao para o plano pago."), audita e responde `Ok(contagem)`; idempotente (segunda chamada = 0).
  Liberta o slot único ATIVA/TRIAL para nova conversão/reativação; trial vencido nunca gera fatura
  (gerar-mensal só cobra ATIVA).
- **Alterar-plano**: aceita apenas `ATIVA`/`TRIAL` (demais → 400 "Apenas assinaturas ativas ou em trial
  aceitam mudança de plano."); recalcula `valor_contratado = PrecosPlano(...)` do plano de destino ×
  periodicidade da assinatura (fonte única R97); `ATIVA` reinicia o ciclo (`data_inicio=hoje`,
  `data_fim=+1 mês/+1 ano`); `TRIAL` mantém datas e status (o fim do teste segue valendo até a conversão).
- **PWA**: `service-worker.js` v2 — `CACHE_NAME 'plantaopro-static-v2'` (activate remove caches antigos),
  precache de 5 rotas, navegações `network-first` com fallback cache → `/offline.html`, estáticos
  `cache-first` (estabilidade sem surpresa), `skipWaiting`/`clients.claim()`/mensagem `SKIP_WAITING`;
  `wwwroot/offline.html` em PT (lang pt-BR, "Tentar novamente"); `wwwroot/js/pwa-install.js` ES5 puro —
  captura `beforeinstallprompt`, botão fixo discreto "Instalar no celular", some em `appinstalled`;
  `_Layout` ganha `<link rel="manifest">`, meta iOS standalone (`apple-mobile-web-app-capable`/`-title`)
  e os scripts `service-worker-registration.js` + `pwa-install.js` (asp-append-version). Manifest.json
  inalterado.
- **Integridade adm360 (v2379)**: `add column if not exists medico_id uuid null` e `observacao text not
  null default ''` em `triagem_encaminhamentos`; limpeza **determinística** das órfãs (580 itens de vale
  sem vale pai + 224 envios de cotação sem resposta — deletes por ausência de pai, reexecução apaga zero);
  `VALIDATE CONSTRAINT` das duas FKs com guarda de existência (`do $$ if exists`). Decisão
  limpeza-vs-documentação = **limpeza**.

## Achados

1. **Colunas inexistentes no insert da regulação**: o primeiro upsert citava `tenant_id/paciente_id/
   agendamento_id/created_by` — a tabela real tem 12 colunas (500 `column "tenant_id" does not exist`);
   diagnosticado com log capturado da API e corrigido espelhando o padrão de casa do `finalizar`
   (mesmo arquivo, insert de 5 colunas).
2. **"EXPIRADA" não existe no domínio**: o check `ck_assinaturas_status_saas` rejeita o valor (`23514`);
   `VENCIDA` é o estado permitido e semanticamente exato para trial vencido — adotado sem DDL.
3. **Guard web nega TRIAGEM.EDITAR ao perfil do tenant admin**: o ADMINISTRADOR_CLIENTE não tem grants no
   módulo TRIAGEM (lacuna de catálogo conhecida; a correção via migração ficou como pendência). O drill
   concede `TRIAGEM.EDITAR` por `usuario_permissoes_especiais` (idempotente `not exists`, escopo tenant,
   justificativa), inserida **antes** dos logins (claims construídos no login) e pré-limpada **antes** da
   remoção dos mundos (ordem de FK) — mesmo padrão da RELATORIOS.VER na R101.
4. **Build do site não builda a API**: `PlantaoPro.Web` não referencia `PlantaoPro.Api` por projeto (BFF
   sobre HTTP) — a DLL da API ficou parada (19:19) executando SQL antigo durante as primeiras rodadas do
   drill; rebuild explícito do projeto da API + o drill agora captura stdout/stderr dos dois apps e anexa
   os tails à saída em qualquer FALHA.
5. **Boleanos e ERRO no psql**: `-t -A` imprime booleans como `true`/`false` (não `t`/`f`); com
   `ON_ERROR_STOP=1` o texto ecoa o argumento e falseia match ingênuo em `'ERRO'` → exit code é o árbitro
   (`EAP=Continue` + `2>&1 | Out-String`), padrão acumulado da maratona.
6. **Primeiro fetch estático após idle do Kestrel**: um reset de conexão isolado derrubou o fetch do
   service worker no meio do drill (app vivo, rota 200 no probe); os ativos PWA passam a ser buscados via
   helper `WebObter` (6 tentativas), padrão de casa das rotas autenticadas.
7. **Zero aviso novo (76/76)**: gate A/B r102 base=nova=76 unicos, DIFF NENHUMA. O CS8629 do `.Value` na
   BFF foi eliminado com `form.Id.GetValueOrDefault()` sob o guard de ModelState (Id garantido).
8. **Codificação legada em comentários (surfada pela mudança própria no arquivo)**: o docscan sinalizou
   `Saude360ClinicalService.cs` — parte da mudança da rodada — por conter `U+00C3` em dois comentários
   pré-existentes de eras anteriores (R8): um mojibake de "não" (tilde deslocado para a letra A) e uma
   "ação" totalmente maiúscula. O scan de casa não distingue os dois — a presença do byte em arquivo da
   rodada é defeito. Corrigido in-place com minúsculas mínimas (`não são`, `por ação`): só comentário,
   zero impacto em lógica, SQL ou contagem de avisos.

## Evidência (execução canônica)

Linha a linha de `r102a_drill_saida.txt` (API porta 52086, Web porta 52085, banco vivo
`plantaopro_test`, execução de 23/10/2026 20:13, DLLs Release do gate r102):

```
R102A_DRILL data=20261023-201325 db=plantaopro_test api=52086 web=52085
PORTAS_LIVRES_INICIO=SIM
ESTAGEM_1_API_NO_AR ok=SIM pid=31512
ESTAGEM_1_WEB_NO_AR ok=SIM pid=19060
PRE_LIMPEZA_RERUN ok=SIM
MIGRACAO_OK v2379 colunas_regulacao=2 fk_validadas=t orfaas=0|0
PLANO_OK drill=7ee25518-3c78-4940-bab3-15194ad5a78a b=cf2fa584-d858-4ff2-945e-83efcaa58dd2 mensal=100/250 anual=1188/2880
REG_MODULO_OK saude360 tenant=ae07a736-2014-4def-b595-088e41992fb2 origem=CONTRATO
REG_PERMISSAO_OK TRIAGEM.EDITAR via usuario_permissoes_especiais
REG_FIXTURE_OK medico=90cf4bf0-61b9-4226-a1b9-1ad715ef7b3d paciente=e12db3fa-f49e-498a-9f14-f372210bfe57 agendamento=0eacbbdc-7805-46fc-90ed-ab27135fb205 triagem=be40571e-730e-46d9-b037-9e3c36916c6f
REG_409_OK http=409 motivo=sem_check-in
REG_400_OK http=400 motivo=sem_orientacao
REG_SUCESSO_OK http=200 agendamento=AGUARDANDO_CONSULTA medico_destino=SIM encaminhamento=1 historico=1 auditoria=+1
T1_DUMP_OK arquivo=r102a_t1_regularizar_api.json acao=regularizar triagem=FINALIZADA
BIL_CFG_OK M=ATIVA_MENSAL A=ATIVA_ANUAL F=ATIVA_FIM_2026_09 T=TRIAL_VENCIDO
GERAR_SET_OK 2026-09 M=1x100 A=1x1188 F=1x100 T=0
GERAR_OUT_CYCLE_OK 2026-10 M=1x100 A=0 (ciclo anual) F=0 (gate data_fim)
TRIAL_EXPIRAR_OK primeira>=1 status=VENCIDA historico=1 segunda=0 expiradas_primeira=1
ALTERAR_PLANO_OK M=250+ciclo_1m A=2880+ciclo_1y TRIAL=datas_preservadas VENCIDA=400
PWA_ASSETS_OK sw=v2_network_first offline.html=PT pwa-install.js=beforeinstallprompt manifest=200
WEB_REG_EDIT_OK http=200 botao=Regularizar acao=/Triagem/Regularizar
PWA_LAYOUT_OK _Layout da triagem referencia pwa-install.js + manifest.json + service-worker-registration
PORTAS_LIVRES=SIM
RESULTADO=PASSEI
```

O dump `r102a_t1_regularizar_api.json` traz a resposta canônica da API do `POST
api/triagens/{id}/regularizar`: sucesso 200, triagem `FINALIZADA` com `finalizada_por` gravado e a
orientação registrada ("Regulado para clinico geral: reavaliar PA em 1h.").

Gates da rodada:
- **Gate A/B Release (tag r102)**: base-exit=0, nova-exit=0, base-unicos=76, nova-unicos=76, DIFF NENHUMA.
- **Suíte completa pré-doc** (`--no-build -c Release`): **1842/1842 aprovados** em execução única
  (baseline 1834 + 8 fatos novos do contrato R102).
- **Suíte completa pós-doc** (`--no-build -c Release`): **1842/1842 aprovados** em execução única.
- **Docscan** (`r62_docscan.ps1`): 0 defeitos (sem mojibake nos arquivos modificados/novos).
- **Higiene**: `RepositoryHygieneContractTests` cobre os termos proibidos sobre o repo inteiro (suíte).
- **EOL**: arquivos da rodada em CRLF sem BOM (sweep dinâmico sobre os commitados).

## Contrato

`backend/PlantaoPro.Tests/R102RegulacaoPwaAdm360ContratoTests.cs` (coleção serial), 8 fatos:
1. drill canônico executa a rodada inteira (todas as linhas estáveis do `r102a_drill_saida.txt`);
2. dump t1 do regularizar: FINALIZADA + finalizada_por + orientação nas observações;
3. fragmentos da API da regulação (rota, validação orientação 400, pré-check 409 pós-check-in, FINALIZADA
   forçado, efeito no agendamento, upsert no unique parcial, histórico enriquecido);
4. fragmentos do gerar-mensal (literal ATIVA pós-rota mantido, gate do mês de renovação ANUAL, gate de
   `data_fim` por ordinal);
5. fragmentos de expirar-vencidos (VENCIDA + TRIAL_EXPIRADO + seleção por `data_trial_fim < hoje`) e de
   alterar-plano (gate ATIVA/TRIAL, `PrecosPlano`, reinício de ciclo só para ATIVA);
6. experiência PWA no site (SW v2 com navegação network-first/offline, offline.html PT, pwa-install.js,
   wiring completo do _Layout);
7. botão Regularizar na tela da triagem + BFF + presença das chaves i18n nos quatro resx;
8. migracao v2379 + manifest (colunas, deletes das órfãs, validates guardados, checksum LF,
   dependsOn v2378, transacional).

## Pendências mantidas para rodadas futuras

- Grants TRIAGEM.\* no perfil ADMINISTRADOR_CLIENTE (correção de catálogo via migração — a rodada usou
  exceção por usuário no drill, como a R101 fez com RELATORIOS.VER);
- as 7 constraints NOT VALID fora do escopo adm360 seguem como estado conhecido
  (`ck_v2159_agendamento_periodo`, `ck_consultas_*` ×2, `ck_plantoes_vagas_consistentes`,
  `fk_resultados_*` ×2, `fk_exame_itens_solicitacao`);
- demais pendências herdas da maratona (grants RELATORIOS.\* no perfil, ponte caixa in-app ↔ central
  operacional, avisos pautados não recolhidos ao concluir, pauta só leitura/gatilho,
  `notificacoes.tenant_id` nulo, ativar módulos em planos reais, WP-A1, ClientPortal descartado +
  `/OperacaoInteligente` órfã, conciliar scrpt_completo/instalar_no_banco_atual × manifest, retenção de
  backups, retroatividade `POST /api/onboarding/sincronizar`, endurecer wrapper B1 contra stderr).
