# R48 · r48a0 — E9 SaaS: alertas de inadimplência (fatura VENCIDA → admin do cliente)

Data: 2026-10-18 · Sessão: `opencode` · Base: R47 (`427162f`, suíte 1591).

## Recon — o que existe hoje

| Camada | Estado |
|---|---|
| Notificações in-app (API) | **Infra completa**: tabela `plantaopro.notificacoes` + `NotificacaoService.CriarNotificacaoAsync(usuarioId, titulo, mensagem, tipo, tx, origemTipo, origemId)` com dedupe por índice único parcial `(usuario_id, origem_tipo, origem_id, titulo) where reg_status='A' and origem_id is not null` (convenção R8-B2/v2346). DI registrada (`Program.cs` L159). Convenção de `tipo`: palavra única minúscula ('convite', 'escala', 'financeiro') |
| Superfície de exibição (Web) | **Existe**: `Home/Dashboard.cshtml` renderiza `UltimasNotificacoes` (via `api/dashboard`) e há página de detalhes genérica (`Views/Notificacoes/Details.cshtml`). O que falta é a CRIAÇÃO da notificação no evento certo — não a tela |
| Alertas do lado MNSOFT | `plantaopro.cliente_alertas` + `RegistrarAlertaFinanceiroAsync` (dedupe por `not exists` mesmo cliente/tipo/título não resolvido) + `ResolverAlertasFinanceirosAsync` (resolve quando sobra fatura nenhuma VENCIDA/EM_CONTESTACAO). Dispara **só na transição explícita** para VENCIDA (`INADIMPLENCIA`/`ALTA`) |
| Flip ABERTA→VENCIDA | Duas vias: **(a)** sweep preguiçoso `AtualizarVencidasAsync` rodando nas leituras de resumo/faturas/inadimplências — **ponto cego**: não registra evento, nem alerta MNSOFT, nem notificação de cliente; **(b)** transição explícita `AtualizarFatura` — registra `FATURA_VENCIDA` em `cobranca_eventos` + alerta INADIMPLENCIA, mas **zero notificação ao cliente** |
| Alvo da notificação | Pattern de casa (`AlertRuleService`): `usuarios u join usuarios_perfis up join perfis pf where u.cliente_id=@clienteId and pf.codigo in (...)` — usuários ativos do cliente com perfil |

## Gap

Quando a fatura do cliente vence, **ninguém avisa o próprio cliente**: o admin do
(`ADMINISTRADOR_CLIENTE`) só descobre pela tabela "Minha Assinatura → Faturas". O lado MNSOFT
também fica cego quando o vencimento acontece pelo sweep (via mais comum: qualquer leitura do
faturamento passa a fatura pra VENCIDA em silêncio).

## Decisões

- **D-R48-1**: dono único dos efeitos colaterais do vencimento — novo helper privado
  `RegistrarVencimentoFaturaAsync(cn, tx, faturaId, clienteId)` no `FaturamentoSaasController`:
  (i) evento `FATURA_VENCIDA` em `cobranca_eventos` (helper existente), (ii) alerta
  `INADIMPLENCIA`/`ALTA` em `cliente_alertas` (helper existente, com dedupe), (iii) **novo**:
  notificação para os usuários ativos `ADMINISTRADOR_CLIENTE` do cliente via
  `NotificacaoService.CriarNotificacaoAsync` com `tipo='financeiro'`,
  `origem_tipo='fatura_saas'`, `origem_id=<faturaId>` → uma notificação por fatura por usuário
  (o dedupe do índice garante idempotência entre sweep e transição repetida). As DUAS vias de
  vencimento (sweep + transição explícita) passam a delegar ao helper — zero cópia de regra.
- **D-R48-2**: fechar o ponto cego do sweep — `AtualizarVencidasAsync` vira: SELECT das faturas
  `ABERTA` com `vencimento < current_date` (id + cliente_id) ANTES do UPDATE, UPDATE idêntico ao
  atual, e chamada ao helper para cada fatura afetada dentro de uma transação. Assinatura
  `(NpgsqlConnection)` mantida (os 3 callers não mudam).
- **D-R48-3**: linguagem simples na notificação do cliente — título **"Fatura vencida"**;
  mensagem: `"Sua fatura de R$ {valor:pt-BR N2} venceu em {dd/MM/yyyy}. Para regularizar, acesse
  Minha Assinatura."` — nenhum código de status, sem a palavra "tenant", sem jargão de cobrança;
  formatação pt-BR explícita (`CultureInfo("pt-BR")`).
- **D-R48-4**: alvo estrito — apenas usuários ativos com perfil `ADMINISTRADOR_CLIENTE` do
  cliente (sem fallback para "qualquer usuário" como o `AlertRuleService` operacional faz:
  inadimplência é assunto do admin da conta, e se não existe ninguém com o perfil, o alerta MNSOFT
  continua cobrindo o acompanhamento). Sem nova UI: a superfície já existe.

## Plano de implementação (b1)

1. **API** (`SaasCommercialController.FaturamentoSaasController`): construtor +`NotificacaoService`;
   classe de linha `FaturaVencidaRow` (id/cliente_id); helper `RegistrarVencimentoFaturaAsync`
   (busca vencimento+valor da fatura na própria query; lista admins; evento+alerta+notificações);
   `AtualizarVencidasAsync` reescrito (select→update→helper p/ cada, em tx); branch `VENCIDA` da
   `AtualizarFatura` substituído pela delegação ao helper (evento+alerta saem de lá).
2. **Contrato permanente** (`R48AlertasInadimplenciaContratoTests`): sweep notifica (tokens do
   select pré-flip + delegação + perfil alvo); dono único (`FATURA_VENCIDA` uma única vez no
   arquivo + branch delega); linguagem da notificação (título/mensagem/origem travados).

## Gates e suíte

- Rebuild real → multiset 160/160 idêntico ao pristino, 0 erros.
- Classes: R48 + R47 + B4 MinhaAssinatura + CobrancaB6 + higiene + R39.
- Suíte integral base **~1594** = 1591 + ~3 fatos R48.
- Smoke viva: fatura `ABERTA` com vencimento no passado p/ o cliente demo → leitura de resumo
  (dispara sweep) → notificação criada para o admin do cliente visível no dashboard → limpeza.
