-- Massa demo comercial premium Saúde 360.
-- Idempotente por tenant demo; não contém dados reais.
-- Bloco E: realinhado ao schema atual (email/telefone/cidade/estado passaram para o jsonb "dados").
BEGIN;

INSERT INTO clientes (id, razao_social, nome_fantasia, cnpj, status, dados, reg_status)
SELECT '00000000-0000-0000-0000-000000360001', 'Clinica Demo Premium Ltda', 'PlantãoPro Saúde 360 Demo', '00000000000191', 'ATIVO',
       '{"email":"demo+saude360@example.com","telefone":"(11) 3000-0000","cidade":"São Paulo","estado":"SP"}'::jsonb, 'A'
WHERE NOT EXISTS (SELECT 1 FROM clientes WHERE id = '00000000-0000-0000-0000-000000360001');

-- As tabelas clínicas variam por instalação. Execute após migrations Saúde 360 e complemente com pacientes, agendamentos, triagens, consultas, financeiro, convênios e plantões demo usando WHERE NOT EXISTS.
COMMIT;
