# Build e runtime — v1.75.0

Data da tentativa: 2026-08-16 (UTC).

## Resultado real no ambiente Codex

| Etapa | Comando | Resultado |
|---|---|---|
| SDK | `dotnet --info` | **BLOQUEADO** — `dotnet: command not found` (exit 127). |
| Restore | `dotnet restore backend/PlantaoPro.sln` | **NÃO APROVADO** — CLI indisponível (exit 127). |
| Build | `dotnet build backend/PlantaoPro.sln -c Release` | **NÃO APROVADO** — CLI indisponível (exit 127). |
| Testes | `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release` | **NÃO APROVADO** — CLI indisponível (exit 127). |

Não houve runtime autenticado nem screenshots reais: sem o SDK não foi possível iniciar a aplicação. Nenhum resultado foi fabricado.

## Diagnóstico e arquivos corrigidos

O bloqueio é ambiental e anterior ao MSBuild; portanto não há erro C#/MSBuild novo diagnosticável. Nesta rodada foram refinados o cadastro self-service, o feedback de envio, a camada visual v175 e o contrato executável do smoke.

## Validação no Windows / Visual Studio

1. Instalar o SDK compatível com a solução e abrir `backend/PlantaoPro.sln`.
2. Selecionar **Release**, executar **Build > Rebuild Solution** e rodar `PlantaoPro.Tests` no Test Explorer.
3. No Developer PowerShell, executar:

```powershell
dotnet --info
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release --no-restore
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build
```

## Smoke autenticado

Após iniciar `PlantaoPro.Web` e gerar um storage state com uma conta real autorizada:

```bash
PLANTAOPRO_BASE_URL=https://localhost:<porta> \
PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json \
scripts/ui/run-visual-smoke.sh
```

Para verificar somente as quatro páginas públicas, adicionar `PLANTAOPRO_PUBLIC_ONLY=1`; isso não substitui a homologação autenticada.
