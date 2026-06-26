# Ordem de migrations e seeds

1. Criar estrutura base/tenants/usuários/perfis.
2. Criar tabelas assistenciais: pacientes, agendamentos, painel_chamada, triagens, consultas, cid_tabela e prescricoes.
3. Criar financeiro clínica: contas a receber, recebimentos, caixa, repasses e glosas.
4. Criar convênios e planos de saúde antes de seeds que referenciam planos.
5. Criar plantões, escalas, médicos, hospitais e especialidades.
6. Executar seeds idempotentes demo por último.

Regras: usar `CREATE TABLE IF NOT EXISTS`, `ADD COLUMN IF NOT EXISTS`, constraints em bloco `DO $$`, índices de busca e nunca inserir seed antes da tabela existir.
