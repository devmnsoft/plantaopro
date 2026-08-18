# Build e runtime — v1.85.0

| Validação | Resultado | Evidência |
|---|---|---|
| `dotnet --info` | **BLOQUEADO** | SDK .NET não está instalado (`dotnet: command not found`). |
| restore/build/test .NET | **BLOQUEADO** | Dependem do SDK ausente; nenhum PASS foi alegado. |
| Runtime PostgreSQL | **BLOQUEADO** | Aplicação não pôde ser compilada/iniciada neste ambiente. |
| Gates estáticos | Executáveis | Python, Node e validação de banco permanecem disponíveis. |

## Homologação local
No Windows/Visual Studio, restaurar `backend/PlantaoPro.sln`, configurar `ConnectionStrings:Default`, compilar em Release, executar `PlantaoPro.Api` e `PlantaoPro.Web` e então rodar o smoke com storage-state autenticado.
