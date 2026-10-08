# R5-B6 — Cobrança SaaS operacional (sandbox ponta a ponta, webhook assinado, sem sucesso fictício)

Data: 2026-10-08. Escopo: transformar telas comerciais de cobrança em operação real —
gerar cobrança de fatura, checkout, pagamento aplicado por evento assinado, dedupe e
fora-de-ordem honestos. Provedor externo real não existe na máquina ⇒ a entrega é o
pipeline completo com provedor **SANDBOX** (mesma máquina de estados que um conector
PRODUCAO usará), sem dados de cartão em lugar algum.

## Migration v2332 (`2026_10_v2332_b6_cobranca_saas.sql`, dependsOn v2331)

- Reconcilia colunas congeladas pelo `IF NOT EXISTS` em `faturas_saas`/`pagamentos_saas`
  (padrão recorrente da rodada — ADD COLUMN IF NOT EXISTS, jamais alterar migration aplicada).
- Novas tabelas: `cobranca_providers` (SANDBOX|PRODUCAO, ATIVO|INATIVO; banco guarda só o
  CÓDIGO, nunca o segredo — regra A33), `cobranca_cobrancas` (referência única; único
  parcial: 1 cobrança ativa por fatura) e `cobranca_webhook_eventos`
  (UNIQUE (provider_codigo, evento_id) = árbitro do dedupe; resultado
  APLICADO/IGNORADO/RECUSADO/DUPLICADO).
- Seed do provider `SANDBOX` ATIVO; purge de teste NUNCA apaga SANDBOX.

## Pipeline do webhook (o coração da honestidade)

- A máquina de estados roda **primeiro** (locks `for update`); o INSERT do evento é o
  ÚLTIMO passo — violação 23505 desfaz tudo e devolve `DUPLICADO` 200 (repostagem não
  duplica pagamento nem efeitos).
- Validações ANTES de qualquer persistência: provider ausente→404, inativo→409, sem
  implementação→409, segredo ausente→**503 honesto**, HMAC inválido/ausente→401 (um
  replay não-assinado NÃO consome o dedupe). JSON inválido→200 `RECUSADO` sem gravar.
- Estados: terminais nunca são rebaixados; valor divergente (>0,005) → `RECUSADO`;
  FALHA só vale de PENDENTE/INICIADA; ESTORNO só de PAGA. Pagamento aprovado aplica os
  efeitos canônicos espelhando `SaasCommercialController` (fatura PAGA + `pagamentos_saas`).
- Assinatura: header `X-Cobranca-Signature: sha256=<hex>`, HMAC-SHA256 do corpo RAW,
  comparação em tempo constante. O simulador sandbox assina server-side e chama o MESMO
  pipeline do webhook (não atalha).

## Superfície

- API: admin (GLOBAL/ADMIN) `providers`/`cobrar`/`eventos`; anônimos checkout sandbox
  (`GET pagina` = **somente leitura**; `POST simular`) e `webhooks/{provedor}`.
  Cobrar é idempotente (ativa existente retorna `JaExistia=true` com a mesma referência).
  Redirecionamento de navegador no checkout JAMAIS ativa a assinatura — só o evento.
- Web: `FaturamentoSaasController.Cobrar` (POST antiforgery) + card "Cobrança online" e
  modal em Details (gate GLOBAL/ADMIN); `BillingController` virou redirects canônicos.
- Segredo `Cobranca__Credenciais__{CODIGO}` só no pool da API (dev: `sb_dev_secret_b6_2026`).

## Defeitos reais achados durante B6 (corrigidos pela causa)

1. **Dapper × records posicionais**: `ObterPublicaAsync` selecionava `FaturaStatus` antes
   de `ClienteNome`; materialização inválida era engolida pelo catch (500). SELECT
   reordenado para casar com a ordem dos parâmetros do construtor do DTO.
2. **Rota `/Billing` dava 404 no servidor vivo** (a rota default do app é
   `{controller=Account}/{action=Login}/{id?}`, então `/Billing` resolvia a ação
   INEXISTENTE `Login` do `BillingController`): adicionados templates explícitos
   `[Route("Billing")] + [HttpGet(...)]` — link legado sem ação agora redireciona certo
   (`/Billing`→`/Assinaturas/Index`, `/Billing/Faturas`→`/FaturamentoSaas/Index`).

## Instabilidade cross-suite (diagnóstico + mitigação)

Vítimas variáveis sob paralelismo eram corridas LATENTES entre classes, apenas expostas
pela mudança de timing dos novos testes (B6 inocente: suíte controle verde, e depois a
suíte cheia verde 2×). Causas:
- `AtivarAgendadosAsync` varre `tenant_modulos` GLOBALmente e `SaasGovernancaB6Rodada4Tests`
  semeia AGENDADO vencido → contagem de idempotência do B4 mudava (Expected 0/Actual 1);
- `Aceite12` × `WS-A3` compartilham `TenantSantaCasa`/documentos adm360;
- `DashboardPremium_KpisReaisPorContextoEDeltaGlobal` mede delta GLOBAL e
  `ProductivityActionScopingTests` escreve `escalas`/`agendamentos` dentro da janela.
Mitigação (consistente com coleções existentes `A360Transmissao`/`web-bff`/`ia-camada`):
coleção xUnit **`saas-operacao-serial`** agregando CobrancaB6 + ProvisionamentoB5 +
SaasComercialB4Matriz + SaasGovernancaB6Rodada4 + ProductivityActionScoping +
Saude360R4B9; `Administrativo360DocumentosWsA3Tests` movido para `A360Transmissao`.
Nota: assembly novo não permite dois `[Collection]` na mesma classe (CS0579).

## Testes

- `CobrancaB6Tests` (7): cobrar/idempotência/estados iniciais; simular aprovado→PAGA com
  efeitos canônicos; recusado; FALHA; ESTORNO; validações do webhook (401/404/409/503/
  RECUSADO/DUPLICADO/IGNORADO); gates. Verde 7/7.
- Suíte CHEIA verde 2× consecutivas (1159 testes) antes do smoke; nova rodada completa
  será re-executada após o smoke para cobrir a correção de rota do Billing.

## Smoke real (API 51976 + Web 52976 http, plantaopro_test) — FALHAS=0

- Login superadmin na API; `providers` lista SANDBOX ATIVO **sem** o segredo no corpo.
- Cobrar F1 → REF `SBX-a8ae2b5d…` INICIADA; repetir → mesma ref `jaExistia=true`.
- Página de checkout 200; GETs de status/página **não mutam** (fatura segue ABERTA);
  `simular aprovado` → `APLICADO` → cobrança PAGA **e** fatura PAGA (`valor_pago=111`,
  `pagamentos_saas` PIX_SANDBOX); timeline FATURA_PAGA visível no admin.
- Webhook bruto na F2: sem assinatura → **401** (sem consumir dedupe); assinado HMAC →
  `APLICADO` (fatura 222 PAGA, PIX); replay idêntico → **DUPLICADO 200** (sem linha nova
  no log — por design, o índice único desfaz o insert); evento novo em cobrança terminal
  → **IGNORADO** ("fora de ordem… estado PAGA").
- Web com login REAL (form + antiforgery): `/Billing`→302 `/Assinaturas/Index`,
  `/Billing/Faturas`→302 `/FaturamentoSaas/Index`; gerar cobrança da F3 pelo formulário
  cai na página real de checkout (`SBX-97ff8663…`) e a fatura **continua ABERTA**
  (nenhum sucesso fictício); abrir a página 2× não muda nada.
- Persistência conferida por psql: faturas PAGA/PAGA/ABERTA com os valores certos.

## Decisões pendentes (NÃO inventadas)

- Conector do provedor externo real (modo PRODUCAO) — sandbox é o estado atual.
- Suspensão automática por inadimplência (política comercial pendente).
- Billing do TRIAL (trial não gera cobrança inicial — B5) e carência (B4).
- Tabelas-sombra `saas_billing_*` e rotas `api/billing/*` (leitura legada) — remover no A1-backlog.
- Scheduler `ativar-agendados` (E13) — fora do escopo B6.
