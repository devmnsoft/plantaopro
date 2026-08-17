# Homologação local — v1.83

## Pré-requisitos

- .NET SDK **10** (`net10.0`), Visual Studio compatível e Git.
- Node.js/npm atuais para o tooling Playwright.
- PostgreSQL acessível; crie configuração local a partir de `backend/PlantaoPro.Web/appsettings.Development.example.json` ou variáveis de `.env.example`. Nunca copie uma senha real para o Git.

## Windows / Visual Studio

1. Abra `backend/PlantaoPro.sln`, selecione `PlantaoPro.Web` como projeto de inicialização e configure secrets/`ConnectionStrings__Default` localmente.
2. Execute `powershell -ExecutionPolicy Bypass -File scripts/local/run-homologacao-windows.ps1`.
3. Inicie pelo perfil HTTP do Visual Studio (`http://localhost:52976`) ou execute:
   ```powershell
   $env:ASPNETCORE_URLS="http://localhost:5000"
   dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj
   ```
4. Em outro terminal, instale o tooling e execute o smoke público conforme `docs/SMOKE_VISUAL.md`.

## Linux/macOS

```bash
./scripts/local/run-homologacao-linux.sh
ASPNETCORE_URLS=http://localhost:5000 dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj
```

O build isolado é `scripts/local/run-build-backend.sh` (ou `.ps1`) e executa `restore`, `build -c Release` e os testes. Logs ficam em `artifacts/ui-audit/runtime-logs/v183/`.

## URLs

Com `ASPNETCORE_URLS=http://localhost:5000`: `/`, `/Account/Login`, `/cadastro/empresa` e `/Planos`. O perfil HTTP do Visual Studio usa a porta 52976 definida em `launchSettings.json`.

## Diagnóstico

- **SDK:** `dotnet --info`; confirme o SDK 10.
- **DI/configuração:** leia a primeira exceção do log de startup e confira chaves dos arquivos `*.example.json`.
- **Banco:** valide host/porta/database do PostgreSQL e aplique o roteiro de `docs/database-scriptcompleto.md`.
- **Porta:** use a URL impressa por `dotnet run`, ou defina `ASPNETCORE_URLS` explicitamente.
- **Segredos:** use User Secrets/variáveis de ambiente; não edite nem versione configuração real.
