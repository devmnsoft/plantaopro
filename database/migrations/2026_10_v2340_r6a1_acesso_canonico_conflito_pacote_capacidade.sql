-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2340_r6a1_acesso_canonico_conflito_pacote_capacidade
-- R6-BlocoA item 1 (acesso canônico): trata EXPLICITAMENTE o conflito entre o
-- contrato de um pacote e a restrição de uma de suas capacidades-filhas.
--
-- O que mudou em relação à v2339:
--   Na v2339, um pacote contratado (ex.: SAUDE360) expandia TODAS as capacidades
--   cujo pacote_codigo = código do pacote, independentemente de o tenant ter uma
--   linha própria de contrato para aquela capacidade. Consequência: se um
--   ADMINISTRADOR desabilitava uma capacidade individualmente (tenant_modulos
--   com habilitado=false / status inativo para PACIENTES, por exemplo), a função
--   ainda devolvia PACIENTES como efetivo via herança do pacote -- a restrição
--   era ignorada e a página continuava acessível com contrato "contraditório".
--
-- Regra canônica agora (documentada no kernel):
--   Um módulo M é efetivo para o tenant T quando
--     (a) T possui linha direta efetiva para M (predicado B6), OU
--     (b) M é capacidade de um pacote P contratado por T (linha efetiva para P)
--         E T NÃO possui linha própria de contrato para M (nenhum override).
--   Ou seja: abrir uma linha própria de contrato para uma capacidade é uma
--   DECISÃO PER-CAPACIDADE que prevalece sobre a herança do pacote. Capacidade
--   sem linha própria herda o estado do pacote; capacidade com linha própria
--   segue o estado daquela linha. Aditivo e idempotente; reusa o predicado B6
--   idêntico (mesmo texto verificado pelo teste de sincronia contra o C#).
-- ============================================================================

DO $migration$
BEGIN
    create or replace function plantaopro.modulos_efetivos(p_tenant_id uuid)
    returns table (codigo text, modulo_id uuid, pacote_codigo text)
    language sql stable
    as $fn$
        -- (a) Contratação direta por modulo_id, avaliada 1:1 pelo predicado B6.
        select distinct upper(ms.codigo), ms.id, nullif(ms.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema ms on ms.id = tm.modulo_id and ms.reg_status = 'A' and upper(ms.status) = 'ATIVO'
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
        union
        -- (b1) Herança de capacidades do pacote contratado por modulo_id,
        --      SEMPRE QUE o tenant NÃO tenha aberto linha própria para aquela
        --      capacidade (override per-capacidade prevalece sobre o pacote).
        select distinct upper(ch.codigo), ch.id, nullif(ch.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema pkg on pkg.id = tm.modulo_id and pkg.reg_status = 'A' and upper(pkg.status) = 'ATIVO'
        join plantaopro.modulos_sistema ch on ch.reg_status = 'A' and upper(ch.status) = 'ATIVO'
            and lower(ch.pacote_codigo) = lower(pkg.codigo)
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
          and not exists (
              select 1 from plantaopro.tenant_modulos ovr
              where ovr.tenant_id = p_tenant_id and ovr.reg_status = 'A'
                and (ovr.modulo_id = ch.id
                     or (ovr.modulo_id is null and lower(nullif(ovr.codigo_modulo,'')) = lower(ch.codigo)))
          )
        union
        -- (a') Contratação direta por codigo_modulo (linhas sem modulo_id).
        select distinct upper(ms.codigo), ms.id, nullif(ms.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema ms on lower(ms.codigo) = lower(nullif(tm.codigo_modulo, '')) and ms.reg_status = 'A' and upper(ms.status) = 'ATIVO'
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
        union
        -- (b2) Herança de capacidades por codigo_modulo do pacote contratado,
        --      com a mesma regra de override per-capacidade.
        select distinct upper(ch.codigo), ch.id, nullif(ch.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema pkg on lower(pkg.codigo) = lower(nullif(tm.codigo_modulo, '')) and pkg.reg_status = 'A' and upper(pkg.status) = 'ATIVO'
        join plantaopro.modulos_sistema ch on ch.reg_status = 'A' and upper(ch.status) = 'ATIVO'
            and lower(ch.pacote_codigo) = lower(pkg.codigo)
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
          and not exists (
              select 1 from plantaopro.tenant_modulos ovr
              where ovr.tenant_id = p_tenant_id and ovr.reg_status = 'A'
                and (ovr.modulo_id = ch.id
                     or (ovr.modulo_id is null and lower(nullif(ovr.codigo_modulo,'')) = lower(ch.codigo)))
          );
    $fn$;

    comment on function plantaopro.modulos_efetivos(uuid) is
      'R6: modulos efetivamente contratados do tenant (predicado B6 + capacidades herdadas do pacote). Override per-capacidade (linha propria em tenant_modulos) prevalece sobre a heranca do pacote. Fonte unica do kernel de acesso.';
END
$migration$;
