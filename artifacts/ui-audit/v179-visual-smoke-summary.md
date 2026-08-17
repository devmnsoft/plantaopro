# Smoke visual v1.79.0

## Estado
Contrato atualizado, execução de runtime ainda não registrada. Não há screenshots inventadas.

## Como executar
```bash
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 \
PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json \
bash scripts/ui/run-visual-smoke.sh
```

A saída real será gravada em `screenshots/v179/`, `v179-visual-smoke-results.json` e neste resumo. Rotas incluem Plantões, Escalas e Fechamentos; os checks verificam cobertura, risco, próxima ação, convites, substituição, fechamento, ligação financeira e ações indisponíveis.
