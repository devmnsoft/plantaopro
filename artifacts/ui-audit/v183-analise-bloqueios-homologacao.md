# Análise dos bloqueios de homologação — v1.83.0

| Bloqueio | Causa provável/evidência | Afetado | Correção aplicada | Validar | Status final |
|---|---|---|---|---|---|
| Playwright ausente | v182: `ERR_MODULE_NOT_FOUND`; instalação v183 recebeu HTTP 403 do registry | tooling raiz/smoke | `package.json` raiz declara `playwright`; scripts npm criados | `npm install && npx playwright --version` | CONFIGURADO; instalação local BLOQUEADA pelo registry deste ambiente |
| Storage state ausente | arquivo real nunca gerado | rotas privadas | gerador manual/credenciais explícitas e proteção Git | `npm run smoke:auth` | FLUXO CORRIGIDO; sessão real depende do usuário/runtime |
| SDK .NET ausente | `dotnet: command not found` | restore/build/test/startup | runners Windows/Linux com logs e propagação de falha | `scripts/local/run-build-backend.ps1` | BLOQUEADO neste ambiente |
| Smoke público indisponível | aplicação não pode iniciar | quatro rotas públicas | modo público automático sem sessão | `PLANTAOPRO_BASE_URL=http://localhost:5000 npm run smoke:ui` | PRONTO; execução BLOQUEADA pelo runtime |
| Smoke autenticado/screenshots | runtime e sessão ausentes | rotas privadas/evidência | rotas privadas marcadas bloqueadas; runner autenticado criado | `scripts/local/run-smoke-authenticated.ps1` | BLOQUEADO até login real |
