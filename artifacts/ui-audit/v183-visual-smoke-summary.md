# Smoke visual — v1.83.0

- Playwright: CONFIGURADO; instalação local BLOQUEADA por HTTP 403 do registry.
- Runtime e smoke público: BLOQUEADOS pela ausência do SDK .NET; nenhum screenshot foi gerado.
- Smoke autenticado: BLOQUEADO adicionalmente pela ausência de storage state real.
- Comando público: `PLANTAOPRO_BASE_URL=http://localhost:5000 npm run smoke:ui`.
- Comando autenticado: `PLANTAOPRO_BASE_URL=http://localhost:5000 PLANTAOPRO_STORAGE_STATE=artifacts/auth/storage-state.json npm run smoke:ui`.
