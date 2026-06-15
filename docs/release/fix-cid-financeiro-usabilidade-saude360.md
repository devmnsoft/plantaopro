# Correção CID, financeiro e usabilidade Saúde 360

## Problema e causa
- `GET /api/cid` podia consultar a coluna genérica `nome` em `cid_tabela`, embora o catálogo CID use `codigo` e `descricao`.
- Endpoints financeiros clínicos dependiam de tabelas mínimas que poderiam não existir no banco de demonstração.
- Lookups precisavam propagar falhas de schema sem transformar erro em sucesso HTTP 200.

## Correções
- A listagem de CID agora pesquisa somente `codigo` e `descricao`, ordenando por `codigo`.
- A DTO visual monta o nome do CID como `codigo - descricao` sem exigir coluna física `nome`.
- Foram adicionadas migrations idempotentes para padronizar `cid_tabela` e criar a base financeira clínica mínima.
- Lookups retornam falha amigável quando o serviço base retorna erro.

## Como aplicar
1. Aplicar `database/migrations/2026_fix_cid_tabela_schema.sql`.
2. Aplicar `database/migrations/2026_fix_clinica_financeiro_minimo.sql`.
3. Aplicar opcionalmente `database/seeds/2026_demo_saude360_complementar.sql` para massa demo complementar.

## Validação esperada
- `GET /api/cid` e `GET /api/cid?termo=I10` sem erro 42703.
- `GET /api/clinica-financeiro/contas-receber` e `GET /api/clinica-financeiro/caixa` sem erro 42P01 após migrations.
- Login, Swagger e endpoints Saúde 360 existentes permanecem funcionando.

## Pendências reais
- O ambiente desta execução não possui SDK `dotnet`, então build e testes precisam ser executados em ambiente com .NET instalado.
