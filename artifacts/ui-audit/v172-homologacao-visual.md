# Homologação visual v1.72.0

## Escopo executável

O runner `scripts/ui/visual-smoke.mjs` cobre as 22 rotas públicas, SaaS, clínicas e operacionais definidas para a versão, em oito viewports de 360×800 a 1920×1080. Além dos contratos de shell da v1.71, verifica jornada clínica, ações operacionais desabilitadas com motivo, overflow, cards, tabelas, formulários, dialogs, Command Palette e Notification Drawer.

## Como executar

```bash
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 \
PLANTAOPRO_STORAGE_STATE=playwright/.auth/user.json \
scripts/ui/run-visual-smoke.sh
```

Para conferir somente as rotas públicas, use `PLANTAOPRO_PUBLIC_ONLY=1`. A execução autenticada exige estado real de sessão; o runner não cria usuário nem dados clínicos.

## Evidência

As imagens serão gravadas em `screenshots/v172/` e o resultado estruturado em `v172-visual-smoke-results.json`. Nenhuma captura ou aprovação de runtime é declarada neste artefato antes da execução.
