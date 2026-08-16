# Smoke visual v1.75.0 — preparação e bloqueio

## Status

**NÃO EXECUTADO contra runtime real.** O ambiente não possui `dotnet`, logo não foi possível iniciar `PlantaoPro.Web`. Não foram gerados screenshots nem `v175-visual-smoke-results.json`; criar esses artefatos sem execução real seria incorreto.

O runner foi atualizado para `screenshots/v175/`, `v175-visual-smoke-results.json` e este resumo. Ele cobre as 23 rotas obrigatórias, oito viewports, overflow, cards, tabelas, formulários, dialogs, shell, Command Palette, Notification Drawer e estados financeiros honestos.

## Execução real

```bash
PLANTAOPRO_BASE_URL=https://localhost:<porta> \
PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json \
scripts/ui/run-visual-smoke.sh
```

Para apenas as quatro rotas públicas, use `PLANTAOPRO_PUBLIC_ONLY=1`. Isso não homologa as rotas operacionais autenticadas.
