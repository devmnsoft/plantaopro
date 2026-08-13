# Smoke visual v1.72.0

## Estado

Contrato executável atualizado; runtime e screenshots ainda não declarados como aprovados. O runner grava `v172-visual-smoke-results.json`, atualiza este resumo e cria imagens em `screenshots/v172/` somente quando executado contra uma instância real.

## Cobertura

São 22 rotas em oito viewports (176 combinações), com validações de overflow, cards, tabelas responsivas, labels, dialogs inicialmente ocultos, drawers acessíveis, shell, jornada clínica, indisponibilidades operacionais explícitas, Command Palette e Notification Drawer.

## Execução

```bash
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json scripts/ui/run-visual-smoke.sh
```
