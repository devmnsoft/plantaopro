# R5 — Entrega final: matriz de aceitação (12 itens), roteiro de aceite e backlog priorizado

Data: 2026-10-09. `HEAD = main = 2160dd8` (não pushado — push só por decisão explícita).
Ambiente de prova: hosts locais (API 51976 / Web 52976) → banco `plantaopro_test`; probes
PowerShell contra HTTP real (sem browser automation) + verificação persistida via psql.
Suíte no fechamento: **1171/1171** com hosts derrubados.
Regra de honestidade aplicada em todos os itens: nenhum sucesso fictício; conclusões derivam de
dados persistidos; estados terminais nunca regridem; áreas sem tenant dão leitura honesta.

## 1. Matriz de aceitação dos 12 itens da rodada

| # | Item | Status | Commit | Evidência |
|---|---|---|---|---|
| A1 | Inventário funcional (~825 ações classificadas) | Entregue | `c38acdc` | `d5-baseline-diagnostico.md`, `d5a1-inventario-funcionalidades.md` |
| A2 | Fiscal honesto: `ENVIANDO` reservado a conector real; API por ação + BFF fino; catálogo `IFiscalTransmissor` vazio até P1 | Entregue | `7a47900` | `r5a2-fiscal-honesto.md` (v2322, smoke real) |
| A3 | Defeitos visuais pela causa (badge global sem tenant 500→leitura honesta; checkout terminal sem ações fictícias) | Entregue | `a8d4455`+`b1d92c9` | `r5a3-defeitos-visuais.md` (triagem DOM + 4 viewports) |
| B4 | Matriz comercial canônica com estados explícitos (matriz/decisão/agendados/minha-assinatura reais; SOLICITADO→APROVADA/RECUSADA idempotente) | Entregue | `49b9ede` | `r5b4-matriz-comercial.md` (v2323–v2327) |
| B5 | Provisionamento real self-service+admin pelo mesmo núcleo (`TentarProvisionarAsync`; TRIAL 1–90 dias; corrida 409; convites com expiração/uso único, hash SHA-256 no banco) | Entregue | `f4eecf2` | `r5b5-provisionamento-convites.md` (v2328–v2331) |
| B6 | Cobrança SaaS operacional **em sandbox** (o sandbox É a entrega — provedor externo não existe): webhook HMAC tempo-constante, dedupe `(provider_codigo,evento_id)`, máquina de estados antes do insert, checkout somente-leitura, cobrar idempotente | Entregue | `4fa9b99` | `r5b6-cobranca-sandbox.md` (v2332; fatura f2 PAGA PIX_SANDBOX 222,00 ao vivo) |
| C7 | Onboarding adaptado ao contrato: materialização só com contrato efetivo (`ModuleContractVigencia.EffectivePredicate`) | Entregue | `0c6595b` | `r5c7-onboarding-contrato.md` (v2333 catálogo canônico) |
| C8 | Conclusão derivada de dados: avaliador lê dados persistidos; transições com origem AUTOMATICO/MANUAL + evidencia; skip só opcional; concluir-no-clique só com critério atendido (senão 409 honesto) | Entregue | `0c6595b` | idem + matriz da seção 3 de `r5d11-d12-jornadas-saude-identidade.md` |
| D9 | Homologação viva da jornada + acesso a módulos contratados (5 defeitos corrigidos na origem, incl. rota Onboarding e seed v2334) | Entregue | `c6022d9` | `r5d9-onboarding-jornada-homologacao.md` |
| D10 | Loop convite→aceite→reavaliação fechado ao vivo por rotas reais (3 passos do núcleo viraram CONCLUIDO/AUTOMATICO só com dados) | Entregue | `2160dd8` | `r5d10-onboarding-loop-ao-vivo.md` |
| D11 | Jornada Saúde 360 real: habilitar PACIENTES, paciente via BFF (validação real de CPF provada), triagem via API; defeito do avaliador (tenant_id×cliente_id) corrigido na causa; v2335 | Entregue | `2160dd8` | `r5d11-d12-jornadas-saude-identidade.md` |
| D12 | Identidade (white-label) via rota self-service com validador WCAG/HEX e tenancy provada (403 cross-tenant); matriz final 10/12 com as 2 pendências honestas documentadas | Entregue | `2160dd8` | idem, seção 3 |

Estado medido do checklist demo: **12/12 CONCLUIDO, todos os concluídos com origem AUTOMATICO**
(atualizado na sequência E13 — rota de escrita de unidades de atendimento entregue e provada ao
vivo em `r5-e13-unidades-atendimento.md`). Na abertura da rodada o estado era 10/12: `ONB_SD_UNIDADE`
pendia por falta de rota de escrita no produto (não por falha de execução) e `ONB_REVISAO` era o
gate honesto derivado exatamente dessa pendência; ambas fecharam por dado real, sem clique falso.

## 2. Roteiro de aceite (usuário final, ~30 min)

Pré-requisito: hosts up apontando para o mesmo banco; contas: superadmin
`superadmin@plantaopro.local`, gestor demo `gestor@santacasa-demo.example`.

1. **Comercial (B4)** — logar superadmin → abrir matriz comercial → aprovar um upgrade
   SOLICITADO e ver histórico com estado destino; tentar reprovar o mesmo pedido (idempotência).
2. **Provisionamento (B5)** — no portal público, iniciar TRIAL com plano e duração válidos;
   conferir e-mail/tokens novos; repetir o mesmo CNPJ/CPF e observar recusa 409 honesta.
3. **Convite (B5/D10)** — logar gestor demo → Equipe → novo convite (token exibido 1x); abrir o
   link anônimo em janela incógnita, aceitar com senha forte e logar com o convidado.
4. **Onboarding (C7/C8/D9-D12)** — logar gestor demo → checklist deve mostrar os 12 passos do
   contrato com evidências textuais; clicar em Reavaliar e conferir que nada muda sem dados novos;
   cadastrar um paciente real (aceite o erro de CPF inválido como prova de validação) e Reavaliar:
   `ONB_SD_PACIENTE` deve virar CONCLUIDO sozinho (origem AUTOMATICO).
5. **Módulos (D9/D11)** — com contrato ativo: `/Plantoes` abre; desabilitar um módulo fino e
   verificar `AccessDenied` com motivo; reabilitar e re-logar (claims no login).
6. **Cobrança sandbox (B6)** — fatura ABERTA → "Cobrar agora" → simular PIX pago → status PAGA com
   cobrança `SBX-…`; tentar pagar de novo: checkout somente-leitura e recusa do simulador.
7. **Fiscal (A2)** — área fiscal ADM360 sem credencial de transmissão: nenhuma nota entra em
   `ENVIANDO`; a ação de transmission exibe o motivo real (catálogo de conectores vazio até P1).

Aceite = todos os passos acima reproduzíveis sem banner de erro falso e sem estado concluído sem dado.

## 3. Backlog priorizado (E13+ — nada disso foi maquiado na rodada)

| Prioridade | Item | Motivo |
|---|---|---|
| P0 | **Upgrade formal do banco principal `plantaopro`** pela sequência completa v2323→v2335 e registro em `schema_migrations` (aplicações manuais em `plantaopro_test` não registradas) | sem isso o ambiente de produção não tem os módulos da rodada |
| P0 | **Deploy live IIS** da build homologada (hosts locais provam o código; IIS é o alvo) | entrega real ao usuário |
| ~~P1~~ ✅ | **Rota de escrita para `clinica_unidades_atendimento`** — ENTREGUE na sequência E13 (`api/unidades-atendimento` + página BFF `ClinicaUnidades`; matriz 12/12 ao vivo): ver `r5-e13-unidades-atendimento.md` | defeito de produto medido em D11 |
| P1 | **Granularidade do catálogo Saúde 360**: linhas `TRIAGEM`, `AGENDAMENTOS`, `CLINICA_DASHBOARD` em `modulos_sistema` + expansão por contrato, OU mapeamento desses controllers para `SAUDE360` no guard | páginas legítimas caem em MODULO_NAO_CONTRATADO com contrato ativo (decisão de produto) |
| P1 | Conector fiscal real P1/P2 (credenciais produção) — `ENVIANDO` continua reservado | único caminho honesto para fiscal de verdade |
| ~~P2~~ ✅ | Scheduler de `POST api/modulos/ativar-agendados` — ENTREGUE na sequência E13 (`SaasModulosAgendadosHostedService`, mesmo kernel B4, ator do sistema na trilha): ver `r5-e13-scheduler-agendados.md` | AGENDADO só opera se alguém chamar |
| ~~P2~~ ✅ | Padronizar escopo clínico em `tenant_id` — ENTREGUE na sequência E13 (kernel resolve origem pelo mapeamento persistido, 18/18 inserts canonizados, writers contaminados corrigidos, backfill v2336 idempotente, bug pre-existente `created_by` em `convenios`/`planos_saude` corrigido): ver `r5-e13-p2-tenant-id-canonical.md` | dívida de canonicidade medida em D11 |
| P2 | Decisão sobre customização de permissões por perfil `base_sistema` (runtime bloqueia edição; grants finos só por migração v2334/v2335) | mecanismo atual é migration-only |
| P3 | Passo de IA do onboarding sem código de módulo canônico (herança C7); tabela morta `assinatura_modulos`; rotas sombra `api/billing/*`/`saas_billing_*`; resumos fixos do portal (`CommercialDemoService`); política de suspensão por inadimplência; billing de TRIAL; parâmetro de motivo no AccessDenied do guard; legacy `OnboardingService.CriarClienteCompletoAsync`; linhas ADM360/AUDITORIA origem LEGADO/SEED_DEMO | inventário A1 + observações D9-D12 |

## 4. Decisões pendentes do usuário

1. **Push** do `main` local (`2160dd8`, 10 commits R5 sobre `0ad2617`) — só com ordem explícita.
2. Agenda do upgrade formal do banco principal + deploy IIS (P0 acima).
3. Direção das duas decisões de produto E13 (unidade: criar rota vs remover passo; granularidade
   SAUDE360: expandir catálogo vs colapsar no guard).
