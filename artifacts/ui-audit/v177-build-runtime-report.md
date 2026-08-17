# Build e runtime — v1.77.0

## Resultado neste ambiente

| Verificação | Resultado | Evidência |
|---|---|---|
| `dotnet --info` | **BLOQUEADA** | `dotnet: command not found` em 17/08/2026 |
| Restore, build e testes .NET | **NÃO EXECUTADOS** | O SDK .NET não está instalado; nenhum PASS foi declarado |
| Runtime e smoke autenticado | **NÃO HOMOLOGADOS** | Dependem do build e de uma sessão real por perfil |

## Homologação obrigatória no Windows/Visual Studio

1. Instalar o SDK indicado pelos projetos e abrir `backend/PlantaoPro.sln` no Visual Studio.
2. Restaurar NuGet e compilar a solução em **Release**.
3. Executar `PlantaoPro.Tests` pelo Test Explorer e iniciar API e Web com configuração real, sem seed sintético.
4. Autenticar usuários reais dos perfis Admin, Coordenação, Médico, Hospital, Financeiro e Operador.
5. Salvar o storage state do Playwright e executar `PLANTAOPRO_BASE_URL=<url> PLANTAOPRO_STORAGE_STATE=<arquivo> scripts/ui/run-visual-smoke.ps1`.

Comandos equivalentes: `dotnet restore backend/PlantaoPro.sln`, `dotnet build backend/PlantaoPro.sln -c Release` e `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release`.
