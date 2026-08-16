# v1.75.0 — relatório de build real e Visual Studio

## Ambiente de homologação Codex

Data: 2026-08-15 (UTC).

| Comando | Resultado |
|---|---|
| `dotnet --info` | **BLOQUEADO** — `dotnet: command not found` (exit 127). |
| `dotnet restore backend/PlantaoPro.sln` | **NÃO EXECUTADO** — SDK/CLI indisponível (exit 127). |
| `dotnet build backend/PlantaoPro.sln -c Release` | **NÃO EXECUTADO** — SDK/CLI indisponível (exit 127). |
| `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release` | **NÃO EXECUTADO** — SDK/CLI indisponível (exit 127). |

Build, testes .NET e runtime **não estão aprovados** neste ambiente. O bloqueio ocorreu antes de restore/compilação; portanto não houve diagnóstico de erro MSBuild/CS nem smoke contra a aplicação real.

## Arquivos corrigidos

- `FaturamentoClinicoController.cs` e `FaturamentoClinicoViewModel.cs`: filtros somente sobre dados retornados e campos financeiros/status opcionais.
- `Views/FaturamentoClinico/Index.cshtml`: filtros e apresentação honesta de campos ausentes.
- `Views/Financeiro/Index.cshtml` e `Views/Pagamentos/Index.cshtml`: valor pago ausente deixa de ser apresentado como zero.
- scripts de smoke/regressão: contrato v1.75.0.

## Validação obrigatória no Windows/Visual Studio

No **Developer PowerShell for Visual Studio**, na raiz do repositório e com o SDK indicado pelo projeto instalado, execute exatamente:

```powershell
dotnet --info
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release --no-restore
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build
$env:PLANTAOPRO_BASE_URL="https://localhost:<porta>"
$env:PLANTAOPRO_STORAGE_STATE="playwright/.auth/user.json"
node scripts/ui/visual-smoke.mjs
```

Alternativamente, abra `backend/PlantaoPro.sln`, selecione `Release`, use **Build > Rebuild Solution**, execute os testes no Test Explorer e inicie `PlantaoPro.Web`. Registrar versão do SDK, saída completa e URL/porta usada. O smoke autenticado exige storage state real e não cria credenciais ou dados fictícios.
