# Consolidação Saúde 360 — convênios, CID e financeiro

## Resumo

Esta rodada estabiliza a base do Saúde 360 para evitar que a API quebre por tabelas ausentes, especialmente `plantaopro.convenios`.

## Entregas

- Migration de convênios e planos de saúde.
- Migration do financeiro clínica.
- Migration CID oficial com capítulos, grupos, importações e fontes.
- Seed demo idempotente com convênios, planos, autorizações, caixa, contas, recebimentos e CIDs mínimos.
- Documentação de ordem segura para migrations e seeds.

## Validações planejadas

- Build API e Web.
- Swagger, login e endpoints Saúde 360.
- Execução idempotente das migrations/seeds.
