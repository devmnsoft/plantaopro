-- v1.37.0 bootstrap condicional: o instalador define somente o hash BCrypt.
\if :{?bootstrap_admin_password_hash}
\if :{?bootstrap_environment}
\else
  \echo 'ERRO: bootstrap_environment deve ser informado explicitamente.'
  \quit 3
\endif
\if :{?bootstrap_admin_email}
\else
  \set bootstrap_admin_email 'admin.global@plantaopro.local'
\endif
\if :{?bootstrap_admin_name}
\else
  \set bootstrap_admin_name 'Super Administrador PlantãoPro'
\endif
\if :{?bootstrap_force_rotation}
\else
  \set bootstrap_force_rotation 'true'
\endif

SELECT set_config('plantaopro.bootstrap_environment', :'bootstrap_environment', false),
       set_config('plantaopro.bootstrap_admin_email', :'bootstrap_admin_email', false),
       set_config('plantaopro.bootstrap_password_hash', :'bootstrap_admin_password_hash', false),
       set_config('plantaopro.bootstrap_force_rotation', :'bootstrap_force_rotation', false);
DO $bootstrap$
DECLARE
    environment_name text := current_setting('plantaopro.bootstrap_environment');
    admin_email text := lower(btrim(current_setting('plantaopro.bootstrap_admin_email')));
    supplied_hash text := current_setting('plantaopro.bootstrap_password_hash');
    force_rotation boolean := current_setting('plantaopro.bootstrap_force_rotation')::boolean;
BEGIN
    -- Character classes avoid PostgreSQL string/backslash escaping changing
    -- the meaning of the BCrypt delimiter.
    IF supplied_hash !~ '^[$]2[aby][$][0-9]{2}[$].{53}$' THEN
        RAISE EXCEPTION 'Bootstrap recusado: hash BCrypt inválido.';
    END IF;
    IF lower(environment_name) = 'production' AND
       (admin_email LIKE '%@plantaopro.local' OR NOT force_rotation OR
        crypt(concat('PlantaoPro.Admin@','2026!','Trocar'), supplied_hash) = supplied_hash) THEN
        RAISE EXCEPTION 'Bootstrap recusado: Production exige credencial própria e rotação obrigatória.';
    END IF;
END $bootstrap$;

INSERT INTO plantaopro.perfis(id,tenant_id,cliente_id,codigo,nome,descricao,base_sistema,customizado,status,reg_status,reg_date)
SELECT md5('profile:ADMINISTRADOR_GLOBAL')::uuid,NULL,NULL,'ADMINISTRADOR_GLOBAL','Super Administrador',
       'Administração global da plataforma',true,false,'ATIVO','A',now()
WHERE NOT EXISTS (
    SELECT 1 FROM plantaopro.perfis
    WHERE tenant_id IS NULL AND cliente_id IS NULL AND lower(btrim(codigo))='administrador_global' AND reg_status='A');
\endif
