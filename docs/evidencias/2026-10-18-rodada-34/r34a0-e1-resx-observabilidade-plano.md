# R34 · r34a0 — E1 varredura final: valores dos resx (4 culturas) + alertas de Observabilidade

Data: 2026-10-18 · Sessão: `opencode` · Base: R33 (`9d1132c`, suíte 1554).

## Contexto

As rodadas R25–R29 limparam o jargão em **views literais** (zonas portal, Adm360 e demais módulos). O scan
dessa rodada (`%TEMP%\opencode\r34_resx_jargao.txt`) encontrou o último resíduo: **valores dos
`UiTexts.*.resx`** — 33 chaves nas zonas Clinical (Saúde 360), Onboarding e Adm360 — além de dois
alertas crujes na zona Observabilidade ("Use o endpoint `/api/observabilidade/...`").

## Segurança dos contratos i18n (verificado antes de tocar)

- `R7DF6` trava o **total** de chaves Adm360 (653) — só mudamos VALORES, nenhuma chave é criada/removida.
- `R7DF1`–`R7DF6`/`R7E1` leem os resx mas fixam apenas presença de CHAVES, estrutura XML/keyset e
  literais-crujos-ausentes nas VIEWS. Nenhum teste fixa os VALORES dos 27 alvos (busca por trecho antigo
  nos testes retornou apenas listas `DoesNotContain` em views — que continuam passando).

## Correções (r34b1) — 27 chaves × 4 culturas + 3 pontos em 2 views

Nova redação por chave (pt-BR / en / es / fr):

| Chave | pt-BR (antigo → novo, resumo do alvo) |
|---|---|
| Clinical.PhWhenToUse | "dados reais da API e contexto por tenant" → "dados reais do sistema e contexto da sua instituição" |
| Clinical.PhTipTenant | "Respeitam o tenant atual" → "respeitam a instituição atual" |
| Clinical.NextBody | "registro retornado pela API" → "registro" (sem atribuição técnica) |
| Clinical.PatientBlockBody | "paciente retornado pela API" → "paciente" |
| Clinical.EmptyBody | "A API não retornou registros reais" → "O sistema ainda não tem registros reais" |
| Clinical.KpiUnavailableHint | "a API não confirmou o total" → "o total ainda não foi confirmado" |
| Clinical.KpiProtectedHint | "tenant, perfil e auditoria" → "instituição, perfil e auditoria" |
| Clinical.LgpdBody | "endpoints autenticados, escopo por tenant" → "conexão autenticada, escopo da sua instituição" |
| Clinical.FormHelpDescription | "validação server-side e envio para a API configurada" → "validação no servidor e envio seguro dos dados" |
| Clinical.FormHelpStep2 | "sem expor GUID ao usuário" → "sem expor códigos técnicos ao usuário" |
| Clinical.FormHelpTip2 | "Todo POST usa antiforgery e endpoint API real" → "Todo envio usa verificação antifalsificação e comunicação real com o sistema" |
| Clinical.FormSubline | "escopo por tenant … em logs" → "escopo da sua instituição … nos registros de eventos" |
| Clinical.TriageReferDisabledTitle | "quando a API retornar o identificador" → "quando o sistema registrar a consulta vinculada" |
| Onboarding.NoClientBody | "pertence a um cliente (tenant)" → "pertence a um cliente" |
| Onboarding.CompanySectionBody | "contato do tenant" → "contato do cliente" |
| Onboarding.HelpCnpj | "para criar o tenant" → "para criar o cliente" |
| Onboarding.HelpInitialStatus | "disponibilidade inicial do tenant" → "do cliente" |
| Onboarding.AdminSectionBody | "acesso administrativo ao novo tenant" → "ao novo cliente" |
| Adm360.Cotacoes.GuideStep1Body | "proteção de payload" → "proteção da integridade dos dados" |
| Adm360.Cotacoes.GuideStep4Title | "Fila Outbox Segura" → "Fila de Envios Segura" |
| Adm360.Dash.CapabilitiesTitle | "Capacidades Contratadas no Tenant" → "por Instituição" |
| Adm360.Dash.CapabilitiesIsolationBadge | "Isolamento Multi-tenant Ativo" → "Isolamento entre Instituições Ativo" |
| Adm360.Dash.CapQuotePortalsDesc | "transmissão outbox" → "envios automáticos" |
| Adm360.Dash.KpiFailuresLast24hDesc | "Tentativas de outbox e comunicação" → "Tentativas de envio e comunicação" |
| Adm360.Org.Subheading | "restritos ao tenant autenticado" → "restritos à instituição autenticada" |
| Adm360.CotDet.LblIdentificadorManual | "ou identificador manual (UUID)" → "ou identificador manual" (placeholder mantém o formato) |
| Adm360.CotDet.OutboxTitulo | "Histórico e Fila Outbox de Transmissão" → "Histórico e Fila de Envios" |

Traduções en/es/fr seguem a mesma redação simples (no script de aplicação; ex.: en "Safe Outbound
Queue" / es "Cola Segura de Envíos" / fr "File d'Envoi Sécurisée"; "tenant" → "institution/institución/
institution"). O nome das CHAVES não muda (incluindo `CotDet.OutboxTitulo`) — chaves são identificador
de código, whitelisted E1.

Views Observabilidade:

- `Erros.cshtml` L5: "Use o endpoint `<code>/api/observabilidade/erros</code>` para análise detalhada
  sem stack trace completa." → "Para análise automatizada, as integrações podem consumir a mesma lista
  de falhas em formato estruturado, sem exibir os registros completos de cada erro."
- `Performance.cshtml` L4 (subtítulo do cabeçalho): "Endpoints com maior tempo médio de resposta." →
  "Páginas e operações com o maior tempo médio de resposta."
- `Performance.cshtml` L5: "Use o endpoint `<code>/api/observabilidade/performance</code>` para dados
  agrupados de latência." → "Para análise automatizada, as integrações podem consumir os dados de
  latência já agrupados em formato estruturado."

## Decisões KEEP (documentadas)

- **`Adm360.RelFin.Ind*` (6 chaves)** — definições de indicadores financeiros FIÉIS AO SQL (família
  R8-B27): nomes de tabela, `competencia`, "rejeição cross-tenant (400)" são a especificação técnica do
  cálculo para analistas. Manter.
- **AdminSaas "tenant" (5 ocorrências renderizadas)** — cockpit global exclusivo da MNSOFT
  (D-R24-3: backoffice SaaS é global por design; audiência = operador da plataforma, não o tenant).
  Vocabulário de plataforma correto para o público; será revisitado no epic E2 (R38–R39) junto da regra
  de visibilidade de tenant.

## Fato permanente (r34b1)

`R34LinguagemSimplesResxContratoTests` (2 fatos):
1. **anti-regressão por TOKEN nas 4 culturas**: para cada uma das 27 chaves, extrai o valor do resx e
   proíbe a volta de qualquer token da lista (API/BFF/endpoint/UUID/GUID/tenant/outbox/payload/JSON/
   HTTP/POST/antiforgery/server/log(s)/log(s)/inquilino/"points d'accès") — cobre regressão sem travar
   redação exata.
2. **Observabilidade**: as duas views não podem voltar a renderizar "Use o endpoint" nem o caminho
   cru `/api/observabilidade/`.

## Plano de fatias R34

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r34b1 | 108 valores resx (script aplicado com assert de ocorrência única) + 3 pontos Observabilidade | 1 fato (2 asserts) |
| r34z | Consolidado + suíte (base 1554) + push | — |

Com R34 fecha o **E1** (linguagem simples em TODO texto renderizado: views + resx). Roadmap:
R35–R37 E3 (instruções de uso por página), R38–R39 E2 (acesso/tenant), R40+ E9.
