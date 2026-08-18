# Build e runtime — v1.84.0

## Ambiente desta execução
- `dotnet --info`: **BLOQUEADO** — o executável `dotnet` não está instalado no container.
- Restore, build, testes .NET e runtime: **BLOQUEADOS** pela mesma dependência; não são declarados aprovados.
- As validações estáticas Python, sintaxe JavaScript/shell e mobile são registradas no checklist após execução.
- O kit v1.83 permanece compatível; o smoke passa a gravar evidências em `screenshots/v184`.

## Homologação local
No Windows/Visual Studio, restaurar `backend/PlantaoPro.sln`, aplicar o `script_completo.sql`, iniciar API/Web, criar o storage-state e executar `npm run smoke:ui` com `PLANTAOPRO_BASE_URL` e `PLANTAOPRO_STORAGE_STATE`.
