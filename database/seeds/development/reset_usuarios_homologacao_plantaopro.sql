-- Reset EXPLÍCITO das cinco credenciais locais. Execute somente via psql:
-- psql "$ConnectionStrings__Default" -v demo_environment=Development \
--   -v confirm_demo_password_reset=RESET_DEMO_PASSWORDS -f <este-arquivo>
-- Os hashes foram produzidos pelo PasswordHashService canônico e são
-- verificados pelos testes HomologationUsersSeedContractTests.
\if :{?demo_environment}
\else
\echo 'Informe -v demo_environment=Development ou Test'
\quit 3
\endif
\if :{?confirm_demo_password_reset}
\else
\echo 'Informe -v confirm_demo_password_reset=RESET_DEMO_PASSWORDS'
\quit 3
\endif

SELECT :'demo_environment' IN ('Development','Test') AS ambiente_demo_valido \gset
SELECT :'confirm_demo_password_reset' = 'RESET_DEMO_PASSWORDS' AS reset_confirmado \gset
\if :ambiente_demo_valido
\else
\echo 'Reset recusado: ambiente deve ser Development ou Test'
\quit 3
\endif
\if :reset_confirmado
\else
\echo 'Reset recusado: confirmação inválida'
\quit 3
\endif

BEGIN;
SET LOCAL search_path TO plantaopro, public;
SELECT pg_advisory_xact_lock(12220260923);

WITH credencial(email, senha_hash) AS (VALUES
  ('superadmin@plantaopro.local','$2a$11$KZ80jdGp.ymLQ/E6zk8vluf6o4/.Ur2cEKxjD4Hp4jx5YETf7OaUG'),
  ('admin.clinica@plantaopro.local','$2a$11$4jafymzm6xqC48JdaVE3GuH0Dy2evtr/dqT7sKDqUe92OwpbaLbX2'),
  ('medico@plantaopro.local','$2a$11$EIMmoQs8gPeaCFShI4.ACeL2WdJFeFulTrXL4JjBKFgJLCmqc11q2'),
  ('recepcao@plantaopro.local','$2a$11$1biWFM2YemJyh9DaoRRjXe3K5UpzJdN.GYJMeglzoODIBi9BTW5yK'),
  ('financeiro@plantaopro.local','$2a$11$9URjd.sZ/id/DeASc.y4a.s/fX3GbcINasNKsgRtMwi10u1/b7Ss2')
), atualizadas AS (
  UPDATE plantaopro.usuarios u
     SET senha_hash=c.senha_hash, senha_alteracao_obrigatoria=false,
         bloqueado_ate=NULL, reg_update=now()
    FROM credencial c
   WHERE lower(coalesce(u.email_normalizado,u.email))=c.email
     AND u.reg_status='A'
  RETURNING u.id
)
SELECT count(*) AS contas_redefinidas FROM atualizadas \gset

\if :{?contas_redefinidas}
\else
\echo 'Reset recusado: não foi possível contabilizar as contas'
ROLLBACK;
\quit 4
\endif
SELECT :contas_redefinidas::integer = 5 AS quantidade_valida \gset
\if :quantidade_valida
\else
\echo 'Reset recusado: as cinco contas devem existir; aplique antes o seed de demonstração'
ROLLBACK;
\quit 4
\endif

INSERT INTO plantaopro.auditoria(id,codigo,nome,status,dados,criado_em)
VALUES(gen_random_uuid(),'RESET_CREDENCIAIS_DEMO','Credenciais demonstrativas redefinidas explicitamente','ATIVO',
  jsonb_build_object('quantidade',5,'ambiente',:'demo_environment','senhas_registradas',false),now());
COMMIT;
\echo 'Cinco credenciais demonstrativas redefinidas e evento auditado (sem senhas).'
