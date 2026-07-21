# Inventário de SQLs v1.18.4

Este inventário foi gerado por varredura de `database/`, `database/migrations/`, `database/seeds/`, `backend/sql/` e `backend/PlantaoPro.Web/Database/`.

## Classificação consolidada

- `database/scrpt_completo.sql`: `BASE_ATIVA`; fonte oficial para instalações novas; incorpora estrutura mínima e objetos operacionais usados pela aplicação; não contém `\i`, credenciais nem dados demo.
- `database/schema-manifest.json`: manifesto determinístico de objetos esperados.
- `database/seeds.sql` e `database/seeds/*.sql`: `SEED_DEMO` ou `SEED_REFERENCIAL` conforme conteúdo; mantidos fora do script oficial quando contêm demonstração.
- `database/migrations/*.sql`: `MIGRATION_ATIVA`; continuam como cadeia incremental para upgrade de bancos existentes.
- `backend/sql/*.sql`: `LEGADO_NECESSARIO` quando alimenta migrations já incorporadas; `FORA_DA_CADEIA` quando é script auxiliar histórico.
- `backend/PlantaoPro.Web/Database/*.sql`: `LEGADO_NECESSARIO`; scripts históricos de módulos Web/SaaS considerados na consolidação.

## Objetos incorporados

Todos os objetos referenciados por `plantaopro.<objeto>` no código C# foram incluídos no manifesto e no script completo, exceto falsos positivos de texto (`com`, `local`). A validação automatizada em `scripts/validate-scrpt-completo.py` falha quando novo objeto usado no código não existir no script completo.

## Conflitos estruturais observados

- `planos` era usado/alterado por migrations SaaS antes de existir na instalação limpa.
- Há coexistência histórica de `relatorios_exportacoes` e `relatorio_exportacoes`; a v1.18.4 mantém ambas para compatibilidade e define `relatorio_exportacoes` como tabela oficial de auditoria da Central de Relatórios.
- Seeds demo permanecem separados para evitar dados fictícios no script oficial.
