-- ============================================================================
-- Migration: 2026_09_v2198_reconciliar_duplicidades_contratos_modulos.sql
-- Objetivo: Reconciliação incremental segura de duplicidades em contratos de
--           módulos (tenant_modulos), preservando histórico, contratos inativos
--           e garantindo idempotência em bases novas ou migradas.
-- ============================================================================

-- 1. Garantir tabela de reconciliação para auditoria e histórico
create table if not exists plantaopro.tenant_modulos_reconciliacao (
    tenant_modulo_id uuid primary key,
    tenant_id uuid null,
    codigo_legado text null,
    motivo text not null,
    candidatos uuid[] not null default '{}',
    detectado_em timestamptz not null default now(),
    resolvido_em timestamptz null
);

-- 2. Identificar e reconciliar contratos ativos duplicados para o mesmo (tenant_id, modulo_id)
-- Regra de negócio:
-- (a) Se houver apenas um contrato efetivamente ATIVO (habilitado = true e status = 'ATIVO')
--     e contratos legados desabilitados com reg_status = 'A', preserva o ativo e inativa os legados.
-- (b) Se houver conflito de múltiplos contratos ATIVOS, mantém o canônico mais antigo como 'A'
--     para respeitar o índice único, move o conflitante para 'I' (INATIVO_AMBIGUIDADE) e registra
--     em tenant_modulos_reconciliacao com motivo 'CONTRATO_DUPLICADO_AMBIGUO' para auditoria.
do $reconciliar$
declare
    v_rec record;
    v_ativo_id uuid;
    v_outro_id uuid;
    v_qtd_ativos integer;
begin
    for v_rec in
        select tenant_id, modulo_id, count(*) as total
        from plantaopro.tenant_modulos
        where reg_status = 'A' and modulo_id is not null
        group by tenant_id, modulo_id
        having count(*) > 1
    loop
        -- Contar quantos estão efetivamente habilitados e ativos
        select count(*) into v_qtd_ativos
        from plantaopro.tenant_modulos
        where tenant_id = v_rec.tenant_id
          and modulo_id = v_rec.modulo_id
          and reg_status = 'A'
          and habilitado = true
          and upper(status) = 'ATIVO';

        if v_qtd_ativos = 1 then
            -- Exatamente um ativo: escolher este como o canônico
            select id into v_ativo_id
            from plantaopro.tenant_modulos
            where tenant_id = v_rec.tenant_id
              and modulo_id = v_rec.modulo_id
              and reg_status = 'A'
              and habilitado = true
              and upper(status) = 'ATIVO'
            limit 1;

            -- Inativar os outros registros não habilitados que estavam como reg_status = 'A'
            for v_outro_id in
                select id from plantaopro.tenant_modulos
                where tenant_id = v_rec.tenant_id
                  and modulo_id = v_rec.modulo_id
                  and reg_status = 'A'
                  and id <> v_ativo_id
            loop
                update plantaopro.tenant_modulos
                set reg_status = 'I', habilitado = false, status = 'INATIVO', reg_update = now()
                where id = v_outro_id;

                insert into plantaopro.tenant_modulos_reconciliacao
                    (tenant_modulo_id, tenant_id, codigo_legado, motivo, candidatos, detectado_em, resolvido_em)
                values
                    (v_outro_id, v_rec.tenant_id, 'ADM360', 'DUPLICIDADE_RESOLVIDA_PRESERVADO_ATIVO', array[v_ativo_id], now(), now())
                on conflict (tenant_modulo_id) do update
                set motivo = excluded.motivo, resolvido_em = now();
            end loop;

        else
            -- Múltiplos ativos ou nenhum ativo: selecionar o primeiro por data de criação como canônico
            select id into v_ativo_id
            from plantaopro.tenant_modulos
            where tenant_id = v_rec.tenant_id
              and modulo_id = v_rec.modulo_id
              and reg_status = 'A'
            order by (case when habilitado = true and upper(status) = 'ATIVO' then 0 else 1 end),
                     reg_date asc, id asc
            limit 1;

            for v_outro_id in
                select id from plantaopro.tenant_modulos
                where tenant_id = v_rec.tenant_id
                  and modulo_id = v_rec.modulo_id
                  and reg_status = 'A'
                  and id <> v_ativo_id
            loop
                update plantaopro.tenant_modulos
                set reg_status = 'I', habilitado = false, status = 'INATIVO_AMBIGUIDADE', reg_update = now()
                where id = v_outro_id;

                insert into plantaopro.tenant_modulos_reconciliacao
                    (tenant_modulo_id, tenant_id, codigo_legado, motivo, candidatos, detectado_em, resolvido_em)
                values
                    (v_outro_id, v_rec.tenant_id, 'ADM360', 'CONTRATO_DUPLICADO_AMBIGUO', array[v_ativo_id], now(), null)
                on conflict (tenant_modulo_id) do update
                set motivo = excluded.motivo;
            end loop;
        end if;
    end loop;
end $reconciliar$;

-- 3. Garantir índice único parcial de contrato ativo
create unique index if not exists ux_tenant_modulos_contrato_ativo
    on plantaopro.tenant_modulos(tenant_id, modulo_id)
    where reg_status = 'A' and modulo_id is not null;

-- 4. Garantir permissões de escrita para cadastros do Administrativo 360
do $permissoes$
declare
    v_acao_id uuid;
    v_perm_cadastros_id uuid;
    v_perm_produtos_id uuid;
    v_perfil_adm_id uuid;
    v_rec_perfil record;
begin
    select id into v_acao_id from plantaopro.acoes_sistema where codigo in ('EDITAR', 'SALVAR', 'ACESSAR') order by id limit 1;
    if v_acao_id is null then
        v_acao_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_id, 'EDITAR', 'Editar', 'ATIVO', 'A', now());
    end if;

    -- ADM360:MAPEAR_CADASTROS
    select id into v_perm_cadastros_id from plantaopro.permissoes where codigo = 'ADM360:MAPEAR_CADASTROS' and reg_status = 'A' limit 1;
    if v_perm_cadastros_id is null then
        v_perm_cadastros_id := gen_random_uuid();
        insert into plantaopro.permissoes (id, acao_id, codigo, nome, status, reg_status, reg_date)
        values (v_perm_cadastros_id, v_acao_id, 'ADM360:MAPEAR_CADASTROS', 'Mapear e Editar Cadastros', 'ATIVO', 'A', now());
    end if;

    -- ADM360:MAPEAR_PRODUTOS
    select id into v_perm_produtos_id from plantaopro.permissoes where codigo = 'ADM360:MAPEAR_PRODUTOS' and reg_status = 'A' limit 1;
    if v_perm_produtos_id is null then
        v_perm_produtos_id := gen_random_uuid();
        insert into plantaopro.permissoes (id, acao_id, codigo, nome, status, reg_status, reg_date)
        values (v_perm_produtos_id, v_acao_id, 'ADM360:MAPEAR_PRODUTOS', 'Mapear e Editar Produtos', 'ATIVO', 'A', now());
    end if;

    -- Conceder aos perfis de administrador do cliente existentes
    for v_rec_perfil in
        select id from plantaopro.perfis
        where codigo in ('ADMINISTRADOR_CLIENTE', 'ADMIN_CLIENTE', 'GESTOR_OPERACIONAL')
          and reg_status = 'A'
    loop
        insert into plantaopro.perfil_permissoes (id, perfil_id, permissao_id, reg_status, reg_date)
        values (gen_random_uuid(), v_rec_perfil.id, v_perm_cadastros_id, 'A', now())
        on conflict (perfil_id, permissao_id) where reg_status = 'A' do nothing;

        insert into plantaopro.perfil_permissoes (id, perfil_id, permissao_id, reg_status, reg_date)
        values (gen_random_uuid(), v_rec_perfil.id, v_perm_produtos_id, 'A', now())
        on conflict (perfil_id, permissao_id) where reg_status = 'A' do nothing;
    end loop;
end $permissoes$;
