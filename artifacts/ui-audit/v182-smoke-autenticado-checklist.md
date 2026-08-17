# Checklist do smoke autenticado — v1.82.0

- [x] 24 rotas críticas declaradas.
- [x] Viewports mínimos 360x800, 390x844, 768x1024, 1366x768, 1440x900 e 1920x1080 declarados (viewports históricos adicionais preservados).
- [x] Saídas `screenshots/v182`, JSON e Markdown configuradas.
- [x] Checks `runtimeResponds`, sessão, páginas 500/Razor e erros client-side implementados.
- [x] Checks de layout, formulários, drawers, jornadas e honestidade de dados preservados.
- [ ] **BLOQUEADO:** smoke público; aplicação não iniciou sem SDK .NET.
- [ ] **BLOQUEADO:** smoke autenticado; falta runtime, dependência Playwright (`ERR_MODULE_NOT_FOUND`) e storage-state real.
- [ ] **BLOQUEADO:** screenshots reais; nenhum arquivo foi alegado ou criado.

Para criar o estado, autentique uma conta/tenant reais via Playwright e execute `await page.context().storageState({ path: "artifacts/auth/storage-state.json" })`. Depois use os comandos de `v182-rotas-homologadas.md`. Para somente as públicas, use `PLANTAOPRO_PUBLIC_ONLY=1`.
