# Build e runtime — v1.81.0

## Resultado no ambiente Codex
- `dotnet --info`: **BLOQUEADO** — executável `dotnet` não instalado.
- Restore, build Release e testes .NET: **NÃO EXECUTADOS**, pois dependem do SDK ausente. Não há declaração de aprovação.
- Validações estáticas Python/Node: executadas separadamente.
- Smoke visual: **NÃO EXECUTADO**; runtime autenticado e storage state não foram fornecidos. Nenhum screenshot ou resultado aprovado foi fabricado.

## Validação exigida no Visual Studio / Windows
1. Abrir e restaurar `backend/PlantaoPro.sln` com SDK compatível.
2. Compilar a solução em Release.
3. Executar `PlantaoPro.Tests`.
4. Iniciar Web/API com configuração e tenant reais.
5. Autenticar no Playwright e salvar o storage state.
6. Definir `PLANTAOPRO_BASE_URL` e `PLANTAOPRO_STORAGE_STATE`; executar `scripts/ui/run-visual-smoke.ps1`.
