# Ordem de migrations e seeds

1. Criar estrutura base/tenants/usuários/perfis.
2. Criar tabelas assistenciais: pacientes, agendamentos, painel_chamada, triagens, consultas, cid_tabela e prescricoes.
3. Criar financeiro clínica: contas a receber, recebimentos, caixa, repasses e glosas.
4. Criar convênios e planos de saúde antes de seeds que referenciam planos.
5. Criar plantões, escalas, médicos, hospitais e especialidades.
6. Executar seeds idempotentes demo por último.

Regras: usar `CREATE TABLE IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`, constraints em bloco `DO $$`, índices de busca e nunca inserir seed antes da tabela existir.

## Ordem validada para RC

1. Estrutura base (`database/PlantaoPro_PostgreSQL_Completo.sql`).
2. SaaS, tenants, planos, assinaturas e white label.
3. Plantões, médicos, hospitais, especialidades, escalas e financeiro médico.
4. Saúde 360: pacientes, agendamentos, painel, triagem, consultas, CID e prescrições.
5. Financeiro clínica.
6. Convênios e planos de saúde.
7. Seeds demo (`database/seeds.sql` e `database/seeds/*.sql`).

Use somente migrations incrementais com `IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`, `CREATE INDEX IF NOT EXISTS` e constraints protegidas por consulta a `pg_constraint`.
