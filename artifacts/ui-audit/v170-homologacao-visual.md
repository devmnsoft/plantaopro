# Homologação visual v1.70.0

## Escopo preparado

O smoke navega pelas 4 rotas públicas e pelas 18 rotas autenticadas obrigatórias, incluindo `/MinhaAssinatura`, nos viewports 360×800, 390×844, 430×932, 768×1024, 1024×768, 1366×768, 1440×900 e 1920×1080.

Além dos contratos de overflow, cards, shell, formulários, tabelas e dialogs, a rota `/Home/Dashboard` valida abertura e fechamento por Escape da Command Palette e do drawer de notificações.

## Execução

```bash
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 \
PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json \
scripts/ui/run-visual-smoke.sh
```

Para páginas públicas: `PLANTAOPRO_PUBLIC_ONLY=1 PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 scripts/ui/run-visual-smoke.sh`.

## Estado desta homologação

Runtime e screenshots não foram declarados aprovados: o SDK .NET não está instalado neste ambiente. O runner está preparado para gravar em `screenshots/v170/`, `v170-visual-smoke-results.json` e `v170-visual-smoke-summary.md` quando executado contra uma aplicação real.
