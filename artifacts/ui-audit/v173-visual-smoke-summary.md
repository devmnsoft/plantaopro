# Smoke visual v1.73.0

Contrato executável atualizado para 23 rotas, incluindo `/FaturamentoClinico`, oito viewports e saídas em `screenshots/v173/` e `v173-visual-smoke-results.json`.

## Estado desta execução

Runtime e screenshots não foram executados: o ambiente não contém o SDK `dotnet`, portanto a aplicação não pôde ser iniciada. Nenhuma aprovação visual foi declarada.

## Execução em homologação

1. Inicie a solução com API e Web configuradas.
2. Capture um storage state Playwright de usuário autorizado.
3. Execute `PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json scripts/ui/run-visual-smoke.sh`.
4. Revise o JSON, este resumo regenerado e as imagens em `artifacts/ui-audit/screenshots/v173/`.
