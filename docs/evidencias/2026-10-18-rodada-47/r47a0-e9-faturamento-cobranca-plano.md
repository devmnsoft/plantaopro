# R47 · r47a0 — E9 SaaS: faturamento/cobrança (fluxo completo do lado do cliente)

Data: 2026-10-18 · Sessão: `opencode` · Base: R46 (`39070b5`, suíte 1587).

## Recon — o que existe hoje

| Camada | Estado |
|---|---|
| Motor de cobrança (API) | **Completo desde R5-B6**: `faturas_saas` (ABERTA/VENCIDA/PAGA/CANCELADA/EM_CONTESTACAO), `CobrancaService.CobrarAsync` idempotente (cobrança ativa existente é devolvida, índice único parcial arbitra concorrência), sandbox checkout PÚBLICO (`/api/cobranca/sandbox/{ref}/pagina`, GET só lê; POST simula), webhook assinado confirma e atualiza a fatura |
| Lado MNSOFT (Web) | **Completo**: `FaturamentoSaas` lista faturas, gera cobrança ("Cobrar"), abre `{apiBaseUrl}{CheckoutPath}` em nova aba, mostra eventos + trilha |
| Lado do cliente (`Minha Assinatura → Faturas`) | **Gap**: tabela estática (Vencimento/Status bruto/Valor), sem linguagem simples e **sem nenhuma ação de pagamento** |
| Divergência de fonte | A listagem do cliente lê `cadastro_cliente_pagamentos_iniciais` (pagamentos iniciais do cadastro), enquanto o motor de cobrança opera sobre `faturas_saas` — os IDs que o cliente vê nem sempre existem no motor |

## Decisões

- **D-R47-1**: a listagem do cliente unifica as duas fontes (`faturas_saas` +
  `cadastro_cliente_pagamentos_iniciais`) escopadas por `cliente_id` do contexto do usuário — o cliente vê
  tudo o que deve da assinatura, com origem marcada (`SAAS`/`CADASTRO`). Mesma rota, mesmo DTO (+`Origem`),
  sem novo endpoint de listagem (contrato BFF preservado).
- **D-R47-2**: ação "Pagar agora" existe só para fatura `SAAS` com status ABERTA/VENCIDA. Novo endpoint
  `POST api/minha-assinatura/faturas/{faturaId}/cobrar`: verifica **dono** na própria query
  (`where id=@faturaId and cliente_id=@clienteId`) e depois delega ao `CobrarAsync` existente — zero cópia
  da regra de criação/idempotência/provedor. `EM_CONTESTACAO` segue intocável (mesma regra do admin).
- **D-R47-3**: autorização segue o padrão da família MinhaAssinatura — `[Authorize]` de controlador na API
  + guard de rota na Web (`MinhaAssinatura/PagarFatura` = verbo `CRIAR` do módulo ASSINATURAS; perfis de
  leitura como AUDITOR seguem barrados nos POSTs).
- **D-R47-4**: idioma simples na tela do cliente: status vira rótulo humano (Em aberto / Vencida / Paga /
  Cancelada / Em análise); sucesso do POST devolve a URL do checkout via TempData e a tela oferece "Abrir
  página de pagamento" (nova aba, mesmo padrão MNSOFT) — honesto sobre expiração: fatura continua pagável
  gerando nova cobrança se necessário.

## Plano de implementação (b1)

1. **API**: `SelfServiceSaasService` ganha `CobrancaService` no construtor; `FaturasMinhaAssinaturaAsync`
   unifica as fontes (UNION ALL, `order by vencimento desc limit 24`); novo
   `CobrarFaturaMinhaAssinaturaAsync(faturaId, ip)` (escopo de dono + delegação).
   Controller `api/minha-assinatura`: `POST faturas/{faturaId:guid}/cobrar`. DTO +`Origem`.
2. **Web**: `AssinaturaCobrancaViewModel` +`FaturaId`/`Origem`/`StatusExibicao`/`PagarDisponivel`;
   `FaturasAssinaturaWebViewModel` +`PagamentoUrl`/`PagamentoAviso`; `MinhaAssinaturaController.Faturas`
   mapeia rótulos + lê TempData; novo `POST PagarFatura/{faturaId}` (AntiForgeryToken, `SendApiAsync`);
   guard +`["MinhaAssinatura/PagarFatura"] = "CRIAR"`; `Faturas.cshtml`: coluna Ação com form POST,
   badges de status humano, aviso explicativo simples.
3. **Contrato permanente** (`R47FaturamentoCobrancaContratoTests`): endpoint com escopo de dono no SQL +
   delegação (sem cópia de regra); listagem unificada das duas fontes; guard cobre a rota nova; view tem
   ação de pagamento + linguagem simples (sem status cru no HTML).

## Gates e suíte

- Rebuild real → multiset 160/160 idêntico ao pristino, 0 erros.
- Classes: R47 + B4 MinhaAssinatura + hygiene + R39.
- Suíte integral base **1592** = 1587 + ~5 fatos R47.
- Smoke viva: gerar fatura `ABERTA` p/ o tenant demo, pagar pela UI, confirmar PAGA.
