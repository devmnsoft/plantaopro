# Smoke visual v1.74.0 — estado da homologação

**Status: BLOQUEADO (não aprovado).** O SDK/runtime .NET não existe no ambiente, portanto não foi possível iniciar a aplicação nem produzir screenshots/resultados JSON reais.

O runner foi atualizado para `screenshots/v174`, `v174-visual-smoke-results.json` e todas as 22 rotas obrigatórias. Ele também verifica estado honesto de Faturamento Clínico e renderização financeira sem placeholders fictícios.

## Como executar

```bash
export PLANTAOPRO_BASE_URL=http://127.0.0.1:5000
export PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json
scripts/ui/run-visual-smoke.sh
```

Para uma auditoria pública limitada: `PLANTAOPRO_PUBLIC_ONLY=1 PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 scripts/ui/run-visual-smoke.sh`.
