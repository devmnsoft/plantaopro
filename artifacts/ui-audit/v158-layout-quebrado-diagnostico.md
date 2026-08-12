# Diagnóstico emergencial do layout v1.58.0

## Rota afetada

`/AdminSaas/Index` (e, pelo shell compartilhado, todas as rotas autenticadas).

## Problema observado no print

A sidebar ocupava a coluna visual, mas o conteúdo podia iniciar atrás dela. A topbar e o cockpit SaaS ficavam comprimidos ou cortados, sem um container interno estável; cards e ações perdiam alinhamento e sobrava uma grande área sem função.

## Arquivos analisados

- `Views/Shared/_Layout.cshtml`, `_AppSidebar.cshtml`, `_AppTopbar.cshtml` e `_AppFooter.cshtml`;
- `Views/AdminSaas/Index.cshtml` (ausente antes desta correção) e `Views/AdminSaas/Dashboard.cshtml`;
- `Views/B2BLaunch/Index.cshtml`, `Views/Planos/Index.cshtml` e `Views/Onboarding/NovoCliente.cshtml`;
- `wwwroot/css/plantaopro.css`, `design-system/layout.css`, `navigation.css`, `responsive.css`, `v154-clinical-pages.css` e `v155-medical-experience.css`.

## Causa provável confirmada por análise estática

O HTML usava classes `pp-*`, mas `layout.css` ainda aplicava `position: fixed` à mesma sidebar pela classe legada `app-sidebar`. A correção v1.55 definia largura, porém não redefinia `position`, `height`, limites ou overflow; além disso, removia a coluna do grid já em 1199px, enquanto a regra de drawer só se completava em outro breakpoint. O conteúdo tinha limite diretamente no `<main>`, sem o `pp-content-container` solicitado. A action `AdminSaas.Index` renderizava `Dashboard.cshtml`; portanto, não existia uma view `Index` explícita para o cockpit da rota do incidente.

No login, várias gerações de seletores (`pp-auth`, `auth-experience`, `login-card-modern`) se acumulavam sem o contrato semântico `pp-auth-page`/`pp-auth-form-panel`, o que dificultava garantir largura dos campos e comportamento mobile.

## Correção aplicada

- O shell agora é um grid desktop de duas colunas; a sidebar `pp-sidebar` é sticky, tem dimensões controladas e scroll interno.
- `pp-main-shell` permanece flexível, o conteúdo usa `pp-content-container` de até 1440px e o footer fica após o conteúdo.
- Em até 991px, o shell vira bloco e a sidebar passa a drawer fixa, inicialmente fora da tela.
- A topbar sticky possui camada menor que a sidebar e continua dentro do shell principal.
- `/AdminSaas/Index` ganhou view própria com hero, resumo baseado no modelo, áreas reais, revisões, empty states e atalhos para planos, billing, white label, LGPD e auditoria.
- O login adotou os contratos `pp-auth-page`, `pp-auth-brand-panel`, `pp-auth-benefit-grid`, `pp-auth-form-panel` e `pp-login-form`, mantendo validação inline, Caps Lock, toggle e loading.
- Drawers passam a ocupar toda a viewport no mobile.

## Como validar

1. Execute restore/build/test conforme `v158-rotas-validadas.md`.
2. Inicie o web project e autentique com uma conta válida (nenhuma credencial foi incluída).
3. Exporte um storage state Playwright e execute `PLANTAOPRO_STORAGE_STATE=/caminho/state.json node scripts/ui/visual-smoke.mjs`.
4. Confira as imagens em `artifacts/ui-audit/screenshots/v158/` nas larguras 360, 390, 430, 768, 1024, 1366 e 1920px.
5. No desktop, confirme que `content.left >= sidebar.right`; no mobile, abra/feche o menu e confirme que foco, overlay e rolagem continuam utilizáveis.

## Limitação do ambiente

O runtime .NET não pôde ser reproduzido neste container: `dotnet --info` retornou `dotnet: command not found`. A correção e os gates foram executados por análise estática; runtime e screenshots não são declarados como aprovados.
