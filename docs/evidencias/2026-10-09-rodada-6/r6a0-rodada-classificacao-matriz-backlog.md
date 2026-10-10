# R6 Rodada 0 — Evolução integrada: classificação canônica, matriz de estado e backlog por dependência

Data: 2026-10-09. `HEAD = 4e0869a` no momento desta escrita (entrega do item A1 ainda não commitada).
Escopo da evolução: blocos **A–E** (acesso canônico, granularidade clínica, jornada/comercial, fiscal,
identidade/operacional) + blocos complementares **i18n** e **design global**. Este documento fixa, antes
de qualquer mudança de comportamento: (1) a classificação de cada regra exigida; (2) a matriz de estado
× efeito consolidada a partir da prova real das rodadas 4–5 e desta rodada 6; (3) o roteiro de aceite;
(4) o backlog em ordem de dependência; (5) as decisões comerciais/clínicas que bloqueiam itens.

Legenda de classificação (exigida):
- **EX+C** = existente + comprovada (há código e prova executada persistida);
- **EX?** = existente + sem validação viva (código real, homologação pendente);
- **PROP** = proposta nesta evolução (ainda sem código);
- **DEC** = decisão pendente do usuário (comercial ou clínica) que impede avanço honesto.

## 1. Classificação das regras exigidas

| # | Regra da evolução | Classificação | Base da classificação |
|---|---|---|---|
| 1 | Onboarding materializa etapas **só com contrato efetivo** (predicado B6), conclusão derivada de dados persistidos, reavaliação sem clique falso | **EX+C** | R5 C7/C8/D9–D12 (`r5c7`, `r5d9`, `r5d10`, `r5d11`); matriz demo 12/12 CONCLUIDO/AUTOMATICO sobre banco real |
| 2 | Provisionamento compartilhado self-service+admin pelo mesmo núcleo (TRIAL 1–90 dias, corrida CNPJ → 409 idempotente) | **EX+C** | R5 B5 (`r5b5-provisionamento-convites.md`; v2328–v2331) |
| 3 | Convites com expiração e uso único (hash SHA-256 no banco, token exibido 1x) | **EX+C** | R5 B5/D10 (loop ao vivo por rotas reais) |
| 4 | Cobrança SaaS **em sandbox** como entrega (webhook HMAC tempo-constante, dedupe por evento, checkout somente-leitura, cobrança idempotente); provedor externo inexistente por definição | **EX+C** (o sandbox É a entrega) + **DEC** para produção | R5 B6 (`r5b6-cobranca-sandbox.md`; fatura f2 PAGA PIX_SANDBOX ao vivo) |
| 5 | Fiscal: API operacional + BFF fino; `ENVIANDO` reservado a conector real; catálogo `IFiscalTransmissor` vazio até credencial | **EX+C** (interno/honesto) + **DEC/BLOQ** externo (credenciais P1/P2) | R5 A2 (`r5a2-fiscal-honesto.md`; v2322, smoke real sem conector) |
| 6 | Escopo clínico canonizado em `tenant_id` (kernel resolve origem pelo mapeamento persistido; backfill idempotente) | **EX+C** | R5 E13-P2 (`r5-e13-p2-tenant-id-canonical.md`; v2336, 18/18 inserts canonizados) |
| 7 | Scheduler de contratos AGENDADO pelo mesmo kernel comercial (ator do sistema na trilha) | **EX+C** | R5 E13 (`r5-e13-scheduler-agendados.md`) |
| 8 | Funções canônicas `modulos_efetivos(uuid)`/`modulo_efetivo(uuid,text)` como fonte única do kernel de acesso (B6 + herança de pacote); login, política por request, portal e onboarding pela função | **EX+C** | R6-A2 `4e0869a` (v2339) + teste de sincronia C#↔SQL |
| 9 | Conflito pacote×capacidade resolvido explicitamente (linha própria prevalece sobre herança) | **EX+C** (nova) | Rodada atual `v2340` + S2/S3 em banco real — ver `r6a1-acesso-canonico.md` |
| 10 | Verificação live da contratação no Web (não depender somente de claims antigos para comandos críticos; suspensão/expiração/reflexos na hora) | **EX+C** em host Testing (end-to-end com stub) + **homologação viva pendente** | Rodada atual: endpoint `api/auth/effective-modules` + resolver + guard IAsyncActionFilter + `web-bff-live` |
| 11 | Nada liberado pela simples presença de menu; guard permanece autoridade | **EX+C** (contratos) — menu alinhado nesta rodada; fontes residuais listadas em `d5a1` adendo D.3 | `r6a1-acesso-canonico.md` seção 2/5 |
| 12 | i18n: pt-BR default + en/es, glossário canônico, precedência usuário→organização→navegador→pt-BR, sem chaves expostas, sem tradução automática de dados do usuário/XML fiscal | **PROP** (sem código nesta rodada) | bloco complementar não iniciado — nada inventado; nenhum percentual/prazo prometido |
| 13 | Design global: auditoria de layouts, consolidação de tokens, contraste 4.5:1 (texto) / 3:1 (UI), viewports 1440/1024/768/390 + zoom 200%, formulários/tabelas/dashboards por jornada, ajuda "Como usar", evidência antes/depois | **PROP** (sem código nesta rodada) | idem; a R5 A3 já trouxe triagem DOM de defeitos visuais (base da auditoria) |

## 2. Matriz de estado × efeito (consolidada, com fonte de prova)

| Área | Estado medido | Efeito comprovado | Fonte |
|---|---|---|---|
| Kernel de acesso (função canônica) | v2339+v2340 aplicadas no `plantaopro_test`; sincronia C#↔SQL testada | pacote→capacidades; conflito por linha própria; revogação live | `r6a1-acesso-canonico.md` §2–4 |
| Web guard/live-check | guard `IAsyncActionFilter`; check ON em produção, OFF na fábrica genérica, ON na `web-bff-live` | revogado-depois-do-login nega sem re-login; motivos preservados | idem |
| Menu/sidebar | códigos finos canônicos; guard cobre `/Caixa` | menu ⊆ páginas liberadas pelo guard (sem item fantasma nem item legítimo oculto em v2149) | idem §3 |
| Onboarding/jornadas | 12/12 CONCLUIDO (AUTOMATICO) no demo | conclusões sempre com dado persistido | R5 `r5d*` |
| Comercial/provisionamento/convites/billing sandbox | estáveis, idempotentes, 409 de corrida | mesma matriz R5 (nenhum regresso: suíte 1198/1198) | R5 `r5b*` + suíte |
| Fiscal | interno honesto; transmissão BLOQUEADA por credencial externa | `ENVIANDO` só com conector real | R5 A2 |
| tenant_id clínico | canonizado (v2336) | cross-tenant 403 provado | R5 E13-P2 |
| i18n / design global | **não iniciado** | — | este documento |

Suíte global de fechamento da etapa atual: **1198/1198** (2 execuções), hosts derrubados — a suíte é
autocontida (banco `plantaopro_test` + stubs determinísticos).

## 3. Roteiro de aceite (por bloco; detalhamentos nos docs citados)

1. **Acesso (Bloco A)**: roteiro completo em `r6a1-acesso-canonico.md` §6 (live revocation sem re-login,
   conflito per-capacidade, core sempre aberto, degradação honesta com API fora).
2. **Onboarding/jornada (já aceito em R5, re-verificar por regressão)**: gestor demo → checklist 12 passos
   com evidências; Reavaliar não muda nada sem dados novos; cadastrar paciente real fecha `ONB_SD_PACIENTE` sozinho.
3. **Comercial/provisionamento (R5)**: upgrade SOLICITADO→APROVADA idempotente; TRIAL com CNPJ repetido → 409;
   convite aceite em janela incógnita.
4. **Fiscal (R5)**: sem credencial nenhuma nota vai a `ENVIANDO`; motivo real exibido.
5. **i18n e design**: entram no roteiro quando existirem (nenhum passo fictício agora).

Aceite geral = reproduzível sem sucesso falso e sem estado concluído sem dado.

## 4. Backlog em ordem de dependência

Ordem respeita: primeiro o que destrava prova viva; depois canonicidade residual; depois blocos novos.

| Ordem | Item | Dependências | Motivo |
|---|---|---|---|
| 1 | **Homologação viva do item A1** (roteiro §6 de `r6a1`) contra hosts reais no banco homologado | hosts up + `v2340` aplicada | converte TESTADO → HOMOLOGADO; única etapa que falta para liberar o kernel |
| 2 | **Upgrade formal do banco principal `plantaopro`** (sequência completa até v2340) + registro em `schema_migrations` + deploy IIS | janela do usuário | o `plantaopro_test` provou as migrations; produção precisa da sequência oficial (carrega desde R5, ainda P0) |
| 3 | ~~**Unificação das fontes residuais de acesso**~~ — **CONCLUÍDO em R6-A1b** (`r6a1b-completamento-autorizacao-canonica.md`): #2/#3/#10 já eram canônicos (reclassificados); #8 base única `AppRoles`; #9 fallbacks pré-v2149 removidos; `CobrancaSaasServices` reclassificado (já canônico); motivos de denegação separados incl. `VERIFICACAO_INDISPONIVEL` | — | restam apenas DEC/P3 registrados no r6a1b §8 |
| 4 | **i18n** (glossário canônico first; precedência user→org→browser→pt-BR; **pt-BR/en/es/fr** conforme a spec atualizada do ciclo) | decisões abaixo (escopo: data da entrega) | exige catálogo de strings antes de layout novo; **próximo corte de código** |
| 5 | **Design global** (tokens, contraste, viewports+zoom, "Como usar", antes/depois) | i18n (textos definitivos afetam medidas de contraste/overflow) | auditoria de layouts usa o inventário R5 A3 como base |
| 6 | Código fino `UNIDADES` no guard (`ClinicaUnidades` hoje grossa em SAUDE360 — decisão E13) | decisão de expansão de contrato | granularidade clínica residual |
| 7 | Conector fiscal P1/P2 (credenciais reais) — `ENVIANDO` continua reservado até lá | credenciais externas (DEC) | único caminho honesto para transmissão |
| 8 | Dívidas P3 do inventário (linhas ADM360/AUDITORIA origem LEGADO/SEED_DEMO, `assinatura_modulos` morta, rotas sombra billing, resumos fixos do portal, política de suspensão por inadimplência, billing de TRIAL) | 2–3 | higiene pós-canonicidade |

## 5. Decisões pendentes (comerciais/clínicas) — nada foi inventado para contorná-las

1. **Agenda**: upgrade formal do banco principal + deploy IIS (ordem 2 do backlog).
2. **Credenciais fiscais** P1/P2 (transmissão NF-e/NFS-e) — sem elas, fiscal segue "interno honesto".
3. **Expansão do contrato clínico**: granularidade fina de `UNIDADES` (e eventuais capacidades novas do
   SAUDE360) — hoje a rota de unidades usa o módulo grosso por decisão E13 explícita.
4. **i18n**: quais idiomas na entrega inicial (pt-BR é default; en/es são alvo da spec — data/ordem a definir)
   e quem revisa o glossário canônico (dicionário clínico×fiscal).
5. **Design**: prioridade dos layouts na auditoria (cofre de evidência antes/depois por viewport já previsto
   na spec; nada medido até aqui — sem número, sem promessa).
6. ~~**Motivo de AccessDenied**: separar "não contratado" de "verificação indisponível"~~ — **implementado em R6-A1B** (motivo `VERIFICACAO_INDISPONIVEL` + `retorno` p/ retry); permanece DEC a decisão de relatórios p/ linhas sem tenant (r6a1b §6/§8).
7. **Relatórios × linhas sem tenant**: filtro estrito vs compatibilidade legada (`is null or`) — r6a1b §6 opões a/b/c.
