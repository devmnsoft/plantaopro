# Smoke visual — v1.82.0

## Resultado

- Runtime: **BLOQUEADO** (`dotnet` não instalado).
- Smoke público: **BLOQUEADO** (nenhuma aplicação disponível em `http://localhost:5000`).
- Smoke autenticado: **BLOQUEADO** (runtime, pacote Playwright e `artifacts/auth/storage-state.json` ausentes).
- Screenshots: **BLOQUEADO**; `artifacts/ui-audit/screenshots/v182/` e o JSON de resultados não foram fabricados.

## Comandos exatos

```bash
# após instalar .NET 10 e configurar a aplicação
dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj
export PLANTAOPRO_BASE_URL="http://localhost:5000"
PLANTAOPRO_PUBLIC_ONLY=1 node scripts/ui/visual-smoke.mjs
export PLANTAOPRO_STORAGE_STATE="artifacts/auth/storage-state.json"
node scripts/ui/visual-smoke.mjs
```

```powershell
$env:PLANTAOPRO_BASE_URL="http://localhost:5000"
$env:PLANTAOPRO_STORAGE_STATE="artifacts/auth/storage-state.json"
node scripts/ui/visual-smoke.mjs
```
