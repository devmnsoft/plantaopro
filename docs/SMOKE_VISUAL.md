# Smoke visual v1.83

## Instalação

```bash
npm install
npm run playwright:install
npx playwright --version
```

## Público

Com a aplicação ativa:

```bash
export PLANTAOPRO_BASE_URL=http://localhost:5000
unset PLANTAOPRO_STORAGE_STATE
npm run smoke:ui
```

Sem storage state, somente `/`, `/Account/Login`, `/cadastro/empresa` e `/Planos` são navegadas; as privadas aparecem como **BLOQUEADAS**, não como falhas falsas.

## Autenticado

Gere a sessão conforme `docs/GERAR_STORAGE_STATE.md`, depois:

```bash
export PLANTAOPRO_STORAGE_STATE=artifacts/auth/storage-state.json
npm run smoke:ui
```

No PowerShell, use `$env:PLANTAOPRO_BASE_URL` e `$env:PLANTAOPRO_STORAGE_STATE`. Também existem `scripts/ui/run-visual-smoke.ps1` e `.sh`.

## Resultados

- screenshots reais: `artifacts/ui-audit/screenshots/v183/`;
- dados: `artifacts/ui-audit/v183-visual-smoke-results.json`;
- resumo: `artifacts/ui-audit/v183-visual-smoke-summary.md`.

`APROVADA` significa que todos os checks daquela execução passaram; `FALHA` exige diagnóstico; `BLOQUEADA` significa que a rota privada não foi aberta por falta da sessão. Arquivos só são produzidos quando o runner realmente inicia.
