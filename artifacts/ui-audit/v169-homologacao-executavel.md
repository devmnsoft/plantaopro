# Homologação visual executável — v1.69.0

## Execução

1. Inicie a aplicação e defina `PLANTAOPRO_BASE_URL`.
2. Para a matriz completa, autentique com uma conta autorizada e salve o estado do contexto Playwright com `await page.context().storageState({ path: "playwright/.auth/user.json" })`.
3. Defina `PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json` e execute `scripts/ui/run-visual-smoke.sh` (Linux/macOS) ou `scripts/ui/run-visual-smoke.ps1` (PowerShell).
4. Para somente as quatro rotas públicas, defina `PLANTAOPRO_PUBLIC_ONLY=1`.

O runner executa 20 rotas em oito viewports, grava capturas em `screenshots/v169/` e sempre consolida resultados em JSON e Markdown. Nenhum resultado runtime foi pré-preenchido: os arquivos de resultado nascem somente de uma execução real.

## Contratos

São auditados HTTP, redirecionamento indevido ao login, overflow, cards, tabelas, dialogs, overlays, formulários, contratos específicos de login/cadastro/Admin SaaS, shell desktop e Command Palette.
