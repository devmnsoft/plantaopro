# Bugs corrigidos durante preparação de runtime real

Data: 2026-07-09.

## Bugs corrigidos

| Bug | Correção | Status |
| --- | --- | --- |
| Defaults com senha demo/local fixa em scripts | Substituídos por placeholders `CHANGE_ME_*` e variáveis de ambiente. | Corrigido estaticamente. |
| Risco de documentação sugerir validação runtime concluída | Evidências passam a declarar bloqueio de ambiente e pendência real. | Corrigido. |
| Testes não exigiam todos os novos documentos de runtime | Adicionado contrato específico para evidências de runtime. | Corrigido estaticamente. |

## Não corrigidos por bloqueio ambiental

Nenhum 404/500/SQL/binding real pôde ser reproduzido porque API, Web e PostgreSQL não puderam ser executados neste executor.
