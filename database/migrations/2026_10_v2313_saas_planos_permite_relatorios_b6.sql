-- R4-B6 (rodada 4, item B6 governanca SaaS): coluna permite_relatorios ausente em plantaopro.planos.
-- Os endpoints comerciais (SaasCommercialController: listar/criar/editar plano) e o
-- SELECT de ObterUsoPlano (TenantServices) leem/escrevem permite_relatorios; nenhuma
-- migracao da linhagem de upgrade a criou, o que causava erro 42703 em tempo de
-- execucao em plantaopro_test e no banco local plantaopro (verificado em 2026-10-06).
-- A linhagem nova (2026_plantao_pro_saas_jornada_lgpd_inteligencia.sql) ja nasce com
-- a coluna; bancos criados pela linhagem antiga precisam desta adicao.
-- Adicao aditiva e idempotente; default false mantem o feature flag fechado em bancos
-- existentes, no mesmo padrao das colunas vizinhas (permite_api, permite_integracoes).

alter table plantaopro.planos add column if not exists permite_relatorios boolean not null default false;
