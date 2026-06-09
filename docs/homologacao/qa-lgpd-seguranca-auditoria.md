# QA LGPD, Segurança e Auditoria

## Resultado desta rodada

- Varredura não encontrou `alert()`, `confirm()`, `href="#"`, `@page`, `asp-page`, `@model dynamic` ou `NotImplementedException` em Web/API ativos.
- Guard de rotas revisado para cobrir assinatura e treinamento.
- Acesso suporte/auditoria alinhado com observabilidade e auditoria.

## Checklist LGPD

- [ ] Política atual e termos versionados.
- [ ] Consentimentos rastreáveis.
- [ ] Solicitação do titular registrada.
- [ ] Exportação dos próprios dados.
- [ ] Anonimização controlada.
- [ ] Eventos de privacidade auditados.

## Checklist Segurança

- [ ] CSRF em POST Web.
- [ ] CORS por ambiente na API.
- [ ] Rate limit quando habilitado.
- [ ] Máscara de dados sensíveis.
- [ ] Senha/token/API key fora de logs.
- [ ] Acesso negado amigável.
- [ ] Bloqueio de acesso cruzado tenant.

## Checklist Auditoria

- [ ] Login/logout.
- [ ] Acesso negado.
- [ ] Troca de tenant.
- [ ] Cliente, usuário, perfil e permissão.
- [ ] Plano, assinatura, billing e fatura.
- [ ] White label.
- [ ] Plantão, convite, escala e pagamento.
- [ ] Exportação e LGPD.
