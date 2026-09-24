# Administrativo 360 — execução do bloco 2

Data: 24/09/2026. Este relatório não declara homologação do módulo inteiro.

## Entregue

- Domínio puro para disponibilidade e decisão de inspeção.
- Contratos de aplicação e repositórios PostgreSQL separados para compras/recebimento, qualidade, estoque/reserva/transferência, inventário e coleta.
- API com autorização específica no servidor. `ADM360:LIBERAR_QUALIDADE` não é concedida pelo simples acesso do estoquista.
- Migration incremental v2191, incluída no DAG e no instalador consolidado, com FKs compostas por tenant nos fluxos críticos, checks, índices, versão e chaves idempotentes.
- Oito páginas separadas e responsivas, com orientação operacional e alternativa manual na coleta.
- Seed opt-in idempotente com pedido/recebimento parcial, lotes livre/quarentena/vencido, ocorrência, inventário e tarefas.

## Parcial

- As páginas operacionais expõem a jornada e a API funcional, mas os formulários ricos de todos os comandos ainda não estão conectados em todas as telas.
- Ocorrências possuem persistência e massa demonstrativa, porém os comandos completos de tratamento/liberação/encerramento não entraram neste incremento.
- A migration contempla inventário e ajuste idempotente; não há relatório CSV dedicado neste incremento.
- O Prompt 1 encontrado na branch implementou departamentos/cargos/colaboradores, não os cadastros A02–A05 previstos no documento canônico. A v2191 adiciona parceiro/produto/local mínimos necessários ao fluxo, sem substituir a futura conclusão desses CRUDs.

## Bloqueado / não executado neste ambiente

- `dotnet`, `psql` e Docker não estão instalados. Por isso build, testes xUnit, instalação/upgrade real, login HTTP, concorrência PostgreSQL, reinício e screenshot navegada não foram executados e não são declarados aprovados.
- Os aceites A–K precisam ser executados em ambiente com .NET 10 e PostgreSQL 16 antes de qualquer GO.

## Execução recomendada

```bash
python3 scripts/database_manifest_validation.py
python3 scripts/generate-scrpt-completo.py
psql -v ON_ERROR_STOP=1 -f database/migrations/2026_09_v2191_administrativo360_suprimentos.sql
psql -v ON_ERROR_STOP=1 -f database/seeds/development/141_administrativo360_suprimentos_demo.sql
dotnet build backend/PlantaoPro.sln
dotnet test backend/PlantaoPro.sln
```

A massa de demonstração depende do tenant e usuário do seed 121 e jamais altera a senha existente.
