-- ============================================================================
-- Migration: 2026_09_v2198_reconciliar_duplicidades_contratos_modulos.sql
-- Objetivo: Reconciliação incremental segura de duplicidades em contratos de
--           módulos (tenant_modulos), preservando histórico, contratos inativos,
--           garantindo catálogo completo de permissões operacionais do ADM360,
--           papéis explícitos de parceiros (hospital, pagador, fornecedor, cliente)
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
-- (b) Se houver conflito de múltiplos contratos ATIVOS:
--     - Se termos forem idênticos (mesmo preço e limite), preserva um e inativa o duplicado com DUPLICIDADE_IDENTICA_RESOLVIDA.
--     - Se houver divergência comercial/ambiguidade, marca o conflitante como INATIVO_AMBIGUIDADE ('I'),
--       mantém o provisional como PENDENTE_RECONCILIACAO ('A'), e registra em tenant_modulos_reconciliacao
--       com motivo 'CONTRATO_DUPLICADO_AMBIGUO' e resolvido_em = NULL para reconciliação administrativa explícita.
do $reconciliar$
declare
    v_rec record;
    v_ativo_id uuid;
    v_outro_id uuid;
    v_qtd_ativos integer;
    v_termos_iguais boolean;
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
            -- Múltiplos ativos ou nenhum habilitado: verificar se termos são idênticos
            select (count(distinct coalesce(preco_contratado, 0)) <= 1 and count(distinct coalesce(limite_contratado, 0)) <= 1)
            into v_termos_iguais
            from plantaopro.tenant_modulos
            where tenant_id = v_rec.tenant_id
              and modulo_id = v_rec.modulo_id
              and reg_status = 'A';

            -- Selecionar o primeiro registro canônico
            select id into v_ativo_id
            from plantaopro.tenant_modulos
            where tenant_id = v_rec.tenant_id
              and modulo_id = v_rec.modulo_id
              and reg_status = 'A'
            order by (case when habilitado = true and upper(status) = 'ATIVO' then 0 else 1 end),
                     reg_date asc, id asc
            limit 1;

            if v_termos_iguais then
                for v_outro_id in
                    select id from plantaopro.tenant_modulos
                    where tenant_id = v_rec.tenant_id
                      and modulo_id = v_rec.modulo_id
                      and reg_status = 'A'
                      and id <> v_ativo_id
                loop
                    update plantaopro.tenant_modulos
                    set reg_status = 'I', habilitado = false, status = 'INATIVO_DUPLICADO', reg_update = now()
                    where id = v_outro_id;

                    insert into plantaopro.tenant_modulos_reconciliacao
                        (tenant_modulo_id, tenant_id, codigo_legado, motivo, candidatos, detectado_em, resolvido_em)
                    values
                        (v_outro_id, v_rec.tenant_id, 'ADM360', 'DUPLICIDADE_IDENTICA_RESOLVIDA', array[v_ativo_id], now(), now())
                    on conflict (tenant_modulo_id) do update
                    set motivo = excluded.motivo, resolvido_em = now();
                end loop;
            else
                -- Divergência comercial real: manter o contrato primário como PENDENTE_RECONCILIACAO para preservar continuidade
                -- e marcar ambiguidade explícita na reconciliação para auditoria humana sem exclusão de histórico
                update plantaopro.tenant_modulos
                set status = 'PENDENTE_RECONCILIACAO', reg_update = now()
                where id = v_ativo_id;

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
                    set motivo = excluded.motivo, resolvido_em = null;
                end loop;
            end if;
        end if;
    end loop;
end $reconciliar$;

-- 3. Garantir índice único parcial de contrato ativo
create unique index if not exists ux_tenant_modulos_contrato_ativo
    on plantaopro.tenant_modulos(tenant_id, modulo_id)
    where reg_status = 'A' and modulo_id is not null;

-- 4. Papéis explícitos de parceiros (Hospital, Pagador/Convênio, Fornecedor, Cliente Comercial)
alter table plantaopro.adm360_parceiros add column if not exists eh_hospital boolean not null default false;
alter table plantaopro.adm360_parceiros add column if not exists eh_pagador boolean not null default false;
alter table plantaopro.adm360_parceiros add column if not exists eh_cliente boolean not null default false;

-- Backfill coerente: parceiros que não eram fornecedores assumem papéis operacionais
update plantaopro.adm360_parceiros
set eh_hospital = true, eh_pagador = true, eh_cliente = true
where fornecedor = false and eh_hospital = false and eh_pagador = false and eh_cliente = false;

-- 5. Catálogo Canônico Completo de Permissões ADM360 (32 Permissões)
do $permissoes$
declare
    v_acao_acessar_id uuid;
    v_acao_editar_id uuid;
    v_modulo_adm_id uuid;
    v_perm_code text;
    v_perm_id uuid;
    v_perfil_rec record;
    v_all_perms text[] := array[
        'ADM360:VER',
        'ADM360:COMPRAS',
        'ADM360:ESTOQUE',
        'ADM360:LIBERAR_QUALIDADE',
        'ADM360:INVENTARIO_APROVAR',
        'ADM360:COMERCIAL',
        'ADM360:CIRURGIAS',
        'ADM360:SEPARAR',
        'ADM360:EXPEDIR',
        'ADM360:RECONCILIAR',
        'ADM360:VALORIZAR',
        'ADM360:VENDAS',
        'ADM360:RECEBER',
        'ADM360:ESTORNAR',
        'ADM360:FINANCEIRO',
        'ADM360:PAGAR',
        'ADM360:APROVAR_DESPESA',
        'ADM360:CRIAR_DESPESA',
        'ADM360:FECHAR_CAIXA',
        'ADM360:CONFIGURAR_INTEGRACAO',
        'ADM360:COTACAO_CONSULTAR',
        'ADM360:MAPEAR_CADASTROS',
        'ADM360:MAPEAR_PRODUTOS',
        'ADM360:ELABORAR_ORCAMENTO',
        'ADM360:APROVAR_RESPOSTA',
        'ADM360:TRANSMITIR_RESPOSTA',
        'ADM360:CONSULTAR_ANEXOS',
        'ADM360:IMPORTAR_XML',
        'ADM360:MANIFESTAR_DFE',
        'ADM360:VINCULAR_DOCUMENTOS',
        'ADM360:EXPORTAR',
        'ADM360:AUDITAR'
    ];
    v_readonly_perms text[] := array[
        'ADM360:VER',
        'ADM360:COTACAO_CONSULTAR',
        'ADM360:CONSULTAR_ANEXOS',
        'ADM360:AUDITAR'
    ];
begin
    -- Garantir ações canônicas
    select id into v_acao_acessar_id from plantaopro.acoes_sistema where codigo in ('ACESSAR', 'LISTAR') order by id limit 1;
    if v_acao_acessar_id is null then
        v_acao_acessar_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_acessar_id, 'ACESSAR', 'Acessar', 'ATIVO', 'A', now());
    end if;

    select id into v_acao_editar_id from plantaopro.acoes_sistema where codigo in ('EDITAR', 'SALVAR') order by id limit 1;
    if v_acao_editar_id is null then
        v_acao_editar_id := gen_random_uuid();
        insert into plantaopro.acoes_sistema(id, codigo, nome, status, reg_status, reg_date)
        values (v_acao_editar_id, 'EDITAR', 'Editar', 'ATIVO', 'A', now());
    end if;

    -- Obter módulo ADM360
    select id into v_modulo_adm_id from plantaopro.modulos_sistema where upper(btrim(codigo)) = 'ADM360' and reg_status = 'A' limit 1;

    -- Cadastrar todas as permissões do Administrativo 360
    foreach v_perm_code in array v_all_perms loop
        select id into v_perm_id from plantaopro.permissoes where upper(btrim(codigo)) = upper(btrim(v_perm_code)) and reg_status = 'A' limit 1;
        if v_perm_id is null then
            v_perm_id := gen_random_uuid();
            insert into plantaopro.permissoes (id, acao_id, modulo_id, codigo, nome, status, reg_status, reg_date)
            values (v_perm_id, case when v_perm_code in ('ADM360:VER', 'ADM360:COTACAO_CONSULTAR', 'ADM360:CONSULTAR_ANEXOS', 'ADM360:AUDITAR') then v_acao_acessar_id else v_acao_editar_id end,
                    v_modulo_adm_id, v_perm_code, replace(v_perm_code, ':', ' - '), 'ATIVO', 'A', now());
        end if;

        -- Atribuir a perfis de gestão (ADMINISTRADOR_CLIENTE, ADMIN_CLIENTE, GESTOR_OPERACIONAL)
        for v_perfil_rec in
            select id from plantaopro.perfis
            where upper(btrim(codigo)) in ('ADMINISTRADOR_CLIENTE', 'ADMIN_CLIENTE', 'GESTOR_OPERACIONAL')
              and reg_status = 'A'
        loop
            insert into plantaopro.perfil_permissoes (id, perfil_id, permissao_id, permitido, reg_status, reg_date)
            values (gen_random_uuid(), v_perfil_rec.id, v_perm_id, true, 'A', now())
            on conflict (perfil_id, permissao_id) where reg_status = 'A' do update set permitido = true;
        end loop;

        -- Atribuir apenas permissões de leitura a perfis de auditoria/consulta
        if v_perm_code = any(v_readonly_perms) then
            for v_perfil_rec in
                select id from plantaopro.perfis
                where upper(btrim(codigo)) in ('CONSULTA_CLIENTE', 'AUDITOR')
                  and reg_status = 'A'
            loop
                insert into plantaopro.perfil_permissoes (id, perfil_id, permissao_id, permitido, reg_status, reg_date)
                values (gen_random_uuid(), v_perfil_rec.id, v_perm_id, true, 'A', now())
                on conflict (perfil_id, permissao_id) where reg_status = 'A' do update set permitido = true;
            end loop;
        end if;
    end loop;
end $permissoes$;

-- 6. Constraints de integridade e consistência dos cadastros
do $constraints$
begin
    if not exists (select 1 from pg_constraint where conname = 'ck_adm360_produtos_preco_custo_nao_negativo') then
        alter table plantaopro.adm360_produtos add constraint ck_adm360_produtos_preco_custo_nao_negativo check (coalesce(preco_custo, 0) >= 0);
    end if;
end $constraints$;

create unique index if not exists ux_adm360_locais_tenant_codigo
    on plantaopro.adm360_locais(tenant_id, upper(codigo)) where ativo = true;
