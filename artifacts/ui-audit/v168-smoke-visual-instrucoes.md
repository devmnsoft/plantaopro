# Execução do smoke visual v1.68.0

## Pré-requisitos

1. Instale o SDK .NET compatível com a solução e Node.js.
2. Execute `npm ci` na raiz para instalar o Playwright e `npx playwright install chromium` se necessário.
3. Inicie a aplicação em uma URL acessível e prepare um storage state de uma conta de homologação sem dados fictícios.

## Execução

```bash
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 \
PLANTAOPRO_STORAGE_STATE=/caminho/seguro/auth-state.json \
node scripts/ui/visual-smoke.mjs
```

Para conferir somente viewports específicos, use `PLANTAOPRO_VIEWPORTS=390x844,1440x900`. A execução completa usa `360x800`, `390x844`, `430x932`, `768x1024`, `1024x768`, `1366x768`, `1440x900` e `1920x1080`.

## Evidências e segurança

As imagens são gravadas em `artifacts/ui-audit/screenshots/v168/`. Não versione o storage state: ele pode conter cookies. Falhas de HTTP, redirecionamento ao login, overflow, sobreposição, contrato estrutural ou acessibilidade encerram o smoke com código diferente de zero. Screenshots só devem ser declaradas quando os arquivos forem realmente produzidos.
