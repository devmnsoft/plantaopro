# Build e runtime — v1.86.0

## Estado do ambiente Codex

- **BLOQUEADO — .NET:** `dotnet --info` retornou `command not found`; restore, build e testes .NET não podem ser declarados aprovados neste contêiner.
- **BLOQUEADO — runtime:** sem SDK .NET e PostgreSQL configurado, a API/Web não foi iniciada.
- **BLOQUEADO — smoke autenticado:** depende de runtime e storage-state real.

## Homologação local assistida

No Windows/Visual Studio, configure `ConnectionStrings__Default` via User Secrets, abra `backend/PlantaoPro.sln` com SDK .NET 10 e execute `scripts/local/run-homologacao-windows.ps1`. Depois inicie a Web/API, gere o storage-state conforme `docs/GERAR_STORAGE_STATE.md` e rode `scripts/ui/run-visual-smoke.ps1`.
