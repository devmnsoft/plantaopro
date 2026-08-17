# Smoke visual v1.77.0

**Status: NÃO EXECUTADO / NÃO APROVADO.** O ambiente não possui SDK .NET, portanto a aplicação não foi iniciada e screenshots/resultados não foram inventados.

O runner foi atualizado para 23 rotas, oito viewports, saída em `screenshots/v177/` e checks de acessibilidade, layout, jornadas honestas, ações sem backend, valores falsos, links e dashboard por perfil.

## Execução

- Público: `PLANTAOPRO_BASE_URL=<url> PLANTAOPRO_PUBLIC_ONLY=1 scripts/ui/run-visual-smoke.sh`
- Completo: `PLANTAOPRO_BASE_URL=<url> PLANTAOPRO_STORAGE_STATE=<arquivo-real> scripts/ui/run-visual-smoke.sh`
- Windows: definir as mesmas variáveis e executar `scripts/ui/run-visual-smoke.ps1`.

A execução real produzirá `v177-visual-smoke-results.json` e screenshots; ausência desses arquivos indica homologação pendente.
