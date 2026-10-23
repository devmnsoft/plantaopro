-- R97: planos/preços configuráveis + entitlements.
--
-- 1) codigo é o identificador comercial canônico do plano (ex.: HOMOLOGACAO_360,
--    DEMO_LOCAL). Vira único entre planos ativos e recebe backfill a partir do
--    slug dos planos existentes (slug único global -> codigo único derivado).
create unique index if not exists ux_planos_codigo
    on plantaopro.planos (upper(codigo))
    where reg_status='A' and codigo is not null;

update plantaopro.planos
   set codigo = upper(replace(slug, '-', '_')), reg_update = now()
 where codigo is null and slug is not null and reg_status='A';

-- 2) Entitlement de trial por plano: dias_trial = 0 significa SEM TETO (o teto
--    de 90 dias por requisição permanece no núcleo). O padrão legado das linhas
--    existentes (14) passa a 0 para preservar o comportamento das rodadas
--    anteriores; o teto explícito passa a ser configurado via api/planos.
update plantaopro.planos
   set dias_trial = 0, reg_update = now()
 where coalesce(dias_trial, 0) = 14;
