# Build e runtime — v1.83.0

- Projeto: `backend/PlantaoPro.Web/PlantaoPro.Web.csproj`, `net10.0`.
- Comando recomendado: `ASPNETCORE_URLS=http://localhost:5000 dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj`.
- Perfil Visual Studio: HTTP `http://localhost:52976`.
- Banco: PostgreSQL e `ConnectionStrings__Default` configurada localmente a partir dos exemplos; nenhum segredo foi alterado.

| Validação | Status | Evidência/continuação |
|---|---|---|
| `dotnet --info` | BLOQUEADO | executável ausente no ambiente em 17/08/2026 |
| restore/build/test | BLOQUEADO | dependem do SDK; execute `scripts/local/run-build-backend.ps1` |
| startup/runtime | BLOQUEADO | sem SDK e configuração local de banco; use o comando acima |

Erros de DI/configuração aparecem no log de startup; erros de banco devem ser confirmados pela exceção e pela conectividade PostgreSQL. Não há aprovação de build ou runtime neste relatório.
