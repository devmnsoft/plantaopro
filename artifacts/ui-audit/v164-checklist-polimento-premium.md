# v1.64 — checklist de polimento

## Evidência

- [x] Smoke configurado para 19 rotas, 7 viewports e diretório `screenshots/v164`.
- [x] Smoke verifica overflow, shell, topbar, sidebar, footer, cards, tabelas, drawers, toasts e ação principal.
- [ ] Runtime real executado (bloqueado: SDK .NET ausente).
- [ ] Screenshots públicas e autenticadas produzidas (não foram fabricadas).

## Superfícies

- [x] Login mantém `pp-auth-page`, `pp-auth-shell`, `pp-auth-card`, labels, erros e banner de segurança.
- [x] Headline e shell do login tiveram escala/altura reduzidas.
- [x] Landing mantém `pp-public-hero` e `pp-public-card-grid`, com copy comercial explícita.
- [x] Landing diferencia action cards e data cards.
- [x] Admin SaaS mantém `pp-admin-layout`, KPIs, áreas reais, checklist e empty states.
- [x] Páginas críticas são protegidas por gates de `pp-page` ou composição equivalente.

## Componentes e mobile

- [x] KPI tem altura, padding, valor e texto auxiliar coerentes.
- [x] Tabela tem contrato `.pp-data-table`/`.table-responsive` e teste visual.
- [x] Hero autenticado possui escala moderada e ações empilhadas no mobile.
- [x] Forms colapsam em uma coluna e ações ocupam a largura no mobile.
- [x] Drawers exigem diálogo acessível, loading, erro, timeline, ações e tela cheia mobile.
- [x] Scripts impedem `href="#"`, APIs nativas de feedback e buttons sem `type` nas superfícies auditadas.
- [x] CSS alterado não contém `!important`.

## Homologação pendente

- [ ] Conferir pixels, foco, zoom 200%, teclado virtual e conteúdo real.
- [ ] Validar contraste AA com ferramenta de browser.
- [ ] Exercitar sessão autenticada e permissões por perfil.
- [ ] Anexar as 133 capturas ao review apenas após execução real.
