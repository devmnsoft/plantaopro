# Checklist de polimento UI v1.66

## Concluído por análise estática

- [x] 19 rotas catalogadas com diagnóstico e correção.
- [x] Login mantém `pp-auth-page`, `pp-auth-shell`, `pp-auth-card` e `pp-form`.
- [x] Login mobile empilha CTA e recuperação sem colisão.
- [x] Landing mantém `pp-public-hero` e `pp-public-card-grid`.
- [x] Cadastro mantém `pp-selfservice-page` e `pp-onboarding-form`.
- [x] Admin SaaS mantém `pp-admin-layout`, KPIs e painel lateral.
- [x] Cards, KPIs, action cards e tabelas receberam acabamento coerente no CSS existente.
- [x] Drawers são validados como dialogs acessíveis.
- [x] Modal começa hidden e usa portal de overlay.
- [x] Smoke cobre 19 rotas e sete viewports, com destino v166.
- [x] Smoke distingue páginas públicas de shell autenticado e valida contrato da landing.
- [x] Nenhum `!important`, mock, CTA falso ou alteração de banco foi introduzido.

## Bloqueios do ambiente

- [ ] Restore/build/test .NET: SDK ausente.
- [ ] Runtime e screenshots: aplicação não pode ser iniciada sem .NET.
- [ ] Rotas autenticadas: exigem storage state real, sem usuário mock.

## Homologação manual pendente

- [ ] Confirmar contraste AA com conteúdo e white label reais.
- [ ] Conferir quebra de textos retornados pelo ambiente em 360 e 390 px.
- [ ] Conferir foco, Escape, loading e erros dos overlays durante interação real.
