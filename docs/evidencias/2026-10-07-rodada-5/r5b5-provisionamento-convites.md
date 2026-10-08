# R5-B5 — Provisionar cliente novo de verdade + convites de equipe

Data: 2026-10-08. Escopo: self-service e admin sobre o MESMO núcleo
(idempotente, tudo-ou-nada); admin do cliente sem privilégio global; sem
acesso por conhecer ID; convites com expiração e uso único.

## Provisionamento (self-service + manual B2B, mesmo núcleo)

- `TentarProvisionarAsync` (núcleo único): pré-checagens em-tx (CNPJ/e-mail/
  plano), 10 escritas em UMA transação, retry limitado de slug (ux_tenants_slug)
  e 23505→409 honesto (CNPJ/e-mail/outros via índice único).
- `FinalizarCadastroAsync` (self-service, ATIVA) e `ProvisionarManualAsync`
  (admin MNSOFT, `POST api/admin-saas/provisionar-cliente`, Global):
  TRIAL exige DiasTrial 1..90 explícito; data_fim = trial+30d (janela de
  conversão — o gate de trial dispara antes do de vigência); trial NÃO gera
  cobrança inicial (billing do trial = pendência B6); LGPD manual registra
  aceito=false/base 'contrato' (consentimento a coletar no 1º acesso — C7).
- `Cadastro.Confirmar` (Web) chama o finalizar real com a mensagem real da API.

## Reconciliações de schema (todas provadas com 500/42703 no banco real)

Colisão "IF NOT EXISTS congela o shape de quem rodou primeiro":

- **v2328**: `clientes` += email/telefone/cidade/estado/plano_id (finalizar
  500); NOVA `usuario_convites` (hash SHA-256 único + único parcial de
  pendente por tenant+e-mail).
- **v2330**: `tenant_white_label` (15 cols), `tenant_onboarding`
  (progresso/proxima_acao/...) e checklist (onboarding_id/titulo/...).
- **v2331**: `lgpd_consentimentos` += titular_email/politica/aceito/origem/
  ip_origem; código grava também consentido/ip do shape vigente.
- Código: insert usuarios += email_normalizado/senha_alteracao/title…
  (23502), base_legal consentimento|contrato, `status_novo` já coberto (B4).
- Grants **v2329**: USUARIOS.VER/CRIAR/EDITAR/CONVIDAR → ADMINISTRADOR_CLIENTE.

## Convites de equipe (expiração + uso único)

- `ConvitesEquipeService`: token 32 bytes (hex), banco guarda SÓ o hash;
  validade padrão 7d/máx 30d (explícito); aceite com UPDATE condicional
  atômico (pendente+válido) — reuso/corrida/expirado/revogado = 409/410;
  capacidade do plano e perfis (nunca global) revalidados no aceite; usuário
  criado com os perfis do convite; auditoria.
- Rotas: `POST/GET /api/equipe/convites`, `POST .../{id}/revogar`
  (admins do cliente); `GET/POST /api/public/convites/{token}[/aceitar]`
  (anônimo). Web: `/ConvitesEquipe` (lista+criar com token uma vez+revogar)
  e `/convite/aceitar/{token}` (público, layout auth). Guard por ação
  (CONVIDAR/VER; AUDITOR barrado nos POSTs).
- Sem privilégio global: perfis globais rejeitados no convite (teste);
  `SalvarUsuario` já excluía (SqlInList); TestarAsync nega cross-tenant
  (CROSS_TENANT_DENIED) — coberto por teste de revogação fora do tenant.

## Envelope 200-falso (achado no smoke, corrigido)

Tenant resolvido sem cliente vinculado devolvia `Fail(ctx.Message=200/
"Sucesso")`. Agora: 404 honesto em MinhaAssinatura/Uso/MinhasSolicitacoes.

## Testes novos (17)

- `ProvisionamentoB5Tests` (11, API+DB): mundo todo + hash de senha + grants;
  CNPJ/e-mail 400 sem resíduo; corrida mesmo CNPJ (1 vence/1 409 + 1 cliente);
  plano inativo atômico; manual TRIAL (trial_fim, sem cobrança) + validações;
  convites (criar/hash/listar/aceitar/uso-único/duplicado/expirado/revogado/
  perfil-global/403/404-cross).
- `ProvisionamentoB5WebTests` (6, BFF/stub): Confirmar→finalizar (200/400);
  convites listar+criar (token uma vez); AUDITOR barrado; aceite público
  GET/POST→login.
- Seeds B5 com valor 0 (neutros em receita) p/ não mover deltas globais R4B9.

## Smoke real (API 51976 + Web 52977, plantaopro_test)

- `POST finalizar` (antes 500) agora provisiona; `solicitar-*` continuam
  honestos; `/planos` e `/MinhaAssinatura/*` 200 honestos.
- Convite fim-a-fim como gestor Santa Casa: criar → validar → aceitar →
  login do convidado (prova acesso real) → limpeza do usuário/convite.

## Decisões pendentes (NÃO inventadas)

- Scheduler p/ ativar-agendados (E13, B4); TRIAL padrão de novos cadastros e
  billing do trial (B6); coleta de consentimento LGPD no 1º acesso (C7);
  etapas intermediárias `api/public/cadastro/*` seguem validação rated
  (jornada multistep = C7); Billing admin e provider de cobrança (B6);
  tenant demo Santa Casa sem assinatura (cobrir no provisionamento demo B5/C7).
