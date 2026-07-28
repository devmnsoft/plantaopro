# Matriz de migração de contratos — v1.24.0

Esta matriz impede que contratos históricos sejam recriados apenas para satisfazer testes de texto. A validação atual deve privilegiar assembly, OpenAPI, catálogo canônico de rotas e comportamento observável.

| Contrato antigo | Origem | Ainda vigente? | Substituto atual | Compatibilidade | Decisão |
| --- | --- | --- | --- | --- | --- |
| `api/customers` | API comercial anterior | Não | Jornada canônica de clientes exposta pelo catálogo atual | Sem alias novo | Atualizar consumidores e testes; não recriar endpoint obsoleto. |
| `runtime-e2e-v113` | Script de homologação v1.13 | Não | jobs `runtime-from-complete-script`, `auth-e2e` e `security-access-e2e` | Evidências históricas permanecem somente para rastreabilidade | Validar o runtime vigente no workflow canônico. |
| `runtime-e2e-v116` | Script de homologação v1.16 | Não | jobs E2E canônicos | Sem execução paralela | Remover a expectativa nominal dos testes, preservando o histórico. |
| `smoke-test-v114.sh` | Smoke test versionado | Não | `scripts/smoke/smoke-api.sh` | Scripts históricos não são fonte canônica | Direcionar automação ao smoke test sem versão. |
| nomes antigos de migrations | Evoluções anteriores do banco | Não como contrato nominal | manifesto `database/source-checksums.json` e fontes canônicas | migrations continuam imutáveis no histórico | Comparar schema semanticamente, não pelo nome do arquivo. |
| frases exatas em documentação | testes de contrato estático | Não | validação de estrutura e comportamento | Conteúdo continua documentado sem redação congelada | Não inserir frases artificiais para satisfazer teste. |
| menus removidos | navegação anterior ao catálogo | Não | catálogo canônico de funcionalidades | alias somente quando houver consumidor ativo comprovado | Testar visibilidade por perfil/permissão, não texto legado. |
| um arquivo por controller | testes de arquivos físicos | Não | reflexão, assembly, OpenAPI e busca da classe nos fontes | controllers consolidados são suportados | Não exigir `AgendamentosController.cs`, `DashboardsController.cs`, `OperacaoInteligenteController.cs` ou `PrescricoesController.cs`. |
| `/api/medicos/me/disponibilidade` | autosserviço médico | Sim, sujeito à autorização | rota atual no catálogo/API | Manter enquanto houver jornada ativa | Validar tenant, vínculo médico e dados persistidos. |
| `/api/medicos/me/escalas` | autosserviço médico | Sim, sujeito à autorização | rota atual no catálogo/API | Manter enquanto houver jornada ativa | Validar apenas escalas acessíveis ao médico autenticado. |
| `/api/medicos/me/pagamentos` | autosserviço médico | Sim, sujeito à autorização | rota atual no catálogo/API | Manter enquanto houver jornada ativa | Não expor dados financeiros de outro vínculo ou tenant. |
| Central de Cobertura | jornada operacional | Sim | catálogo e serviços canônicos de cobertura | Convites existentes devem ser preservados | Evoluir por comportamento e persistência, sem segunda arquitetura. |
| favoritos, recentes e timelines | contexto de navegação | Sim | serviços e catálogo canônicos | Persistência deve ser por usuário e tenant | Manter e cobrir com testes comportamentais. |
| menus clínicos | jornada assistencial | Sim | catálogo canônico com RBAC e entitlement | Não oferecer atalhos sem permissão | Validar perfil, unidade, vínculo e módulo contratado. |

## Evidência de falhas

A matriz de falhas deve ser gerada exclusivamente de TRX real por `scripts/generate-test-failure-matrix.py`. O gerador rejeita arquivos vazios e execuções com zero testes, reconcilia cada falha do baseline com o resultado final e permite anexar decisões de triagem sem inventar casos de teste.

```bash
python3 scripts/generate-test-failure-matrix.py \
  artifacts/TestResults/tests-v1240-baseline.trx \
  --final-trx artifacts/TestResults/tests-v1240-final.trx \
  --decisions artifacts/v1240/test-failure-decisions.json \
  --output artifacts/v1240/test-failure-matrix.json
```
