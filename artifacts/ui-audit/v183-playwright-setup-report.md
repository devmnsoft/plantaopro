# Setup Playwright — v1.83.0

- Tooling web isolado na raiz; o pacote mobile não foi alterado.
- Scripts: `smoke:ui`, `smoke:auth`, `playwright:install`.
- `npm install` foi tentado em 17/08/2026 e ficou **BLOQUEADO** por `403 Forbidden` do registry; não há alegação de pacote instalado.
- Validação local: `npm install && npm run playwright:install && npx playwright --version`.
