# Relatório incremental — CRUD/AJAX seguro e ações críticas Web

Data: 2026-06-05

## Escopo consolidado

Este incremento reforça a camada Web MVC da Beta Homologável com foco nas ações críticas que alteram status ou fluxo financeiro/operacional:

- Escalas: confirmação, recusa, conclusão e substituição passam a exigir antiforgery, modal de confirmação, AJAX seguro e toast de sucesso/erro.
- Plantões: publicação e cancelamento passam a usar o fluxo uniforme de confirmação e envio AJAX com fallback de redirect.
- Financeiro: confirmação e cancelamento de pagamentos passam a exigir antiforgery, modal de confirmação, AJAX seguro e feedback visual padronizado.
- Área médica: solicitação de plantão passa a usar confirmação explícita, AJAX e feedback visual, preservando validações de conflito e vagas no backend.

## Padrão técnico aplicado

1. Todas as ações sensíveis adicionadas usam `@Html.AntiForgeryToken()` no formulário.
2. Controllers Web que recebiam POST financeiro/escala agora exigem `[ValidateAntiForgeryToken]`.
3. `plantaopro-ui.js` centraliza envio AJAX com `X-Requested-With` e propaga o token antiforgery no header `RequestVerificationToken` quando disponível.
4. `site.js` deixou de registrar loading genérico para todos os forms, evitando conflito entre submit comum, modal de confirmação e AJAX centralizado.
5. As telas alteradas usam partials padronizadas (`_PageHeader`, `_EmptyState`, `_StatusBadge`) e mensagens de confirmação com contexto de auditoria/histórico.

## Evidências para homologação manual

- Em Escalas, validar como Coordenação:
  - solicitar tela de detalhes de uma escala `solicitado`;
  - clicar em **Confirmar** e verificar modal, toast e redirecionamento;
  - repetir recusa exigindo justificativa.
- Em Escalas confirmadas, validar **Marcar realizado** e **Substituir**.
- Em Plantões, validar publicação de rascunho e cancelamento com justificativa.
- Em Financeiro, validar confirmação de pagamento pendente e cancelamento com justificativa.
- Como Médico, validar solicitação de plantão disponível com modal e toast.

## Itens ainda dependentes de ambiente

- Build/test automatizado dependem do SDK .NET disponível no ambiente de execução.
- Teste visual final deve ser feito em homologação com usuários reais de Admin Global, Coordenação, Médico, Financeiro e Hospital.
