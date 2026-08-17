# Correções por evidência — v1.82.0

| Rota/área | Viewport | Problema/evidência | Correção | Arquivo | Status |
|---|---:|---|---|---|---|
| Smoke global | todos | gate inicial falhou por saída v181 e ausência dos viewports legados exigidos | saídas migradas para v182 e matriz compatível preservada | `scripts/ui/visual-smoke.mjs`, `scripts/check-layout-regression.py` | gate repetido: aprovado |
| Runtime global | n/a | `dotnet: command not found` | nenhuma correção de produto seria justificável sem compilação | relatório v182 | BLOQUEADO |
| Páginas HTTP | seis viewports obrigatórios | aplicação não pôde iniciar; não há evidência visual | nenhuma alteração CSS/tela | — | BLOQUEADO |

Não foi feito redesign nem criada camada CSS v182: não houve screenshot/runtime que justificasse mudança visual.
