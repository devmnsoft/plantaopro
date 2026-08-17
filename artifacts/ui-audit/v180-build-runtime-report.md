# Build e runtime — v1.80.0

## Resultado neste ambiente

- `dotnet --info`: **BLOQUEADO** (`dotnet: command not found`).
- Restore, build Release e testes .NET: **NÃO APROVADOS / NÃO EXECUTÁVEIS**, porque o SDK não está instalado.
- Validações estáticas Python, sintaxe JavaScript/shell e suíte mobile: executadas separadamente.
- Smoke visual autenticado e screenshots: **não executados**, pois exigem aplicação/API em execução, tenant e estado Playwright autenticado. Nenhum resultado foi presumido.

## Validação Windows / Visual Studio

Abrir `backend/PlantaoPro.sln`, restaurar os pacotes, selecionar Release, compilar a solução e executar `PlantaoPro.Tests`. Iniciar API e Web com tenant válido, capturar o storage state autenticado e executar `PLANTAOPRO_BASE_URL=<url> PLANTAOPRO_STORAGE_STATE=<arquivo> scripts/ui/run-visual-smoke.sh`.
