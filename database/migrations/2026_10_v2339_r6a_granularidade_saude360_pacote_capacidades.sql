-- ============================================================================
-- PlantaoPro | Migration: 2026_10_v2339_r6a_granularidade_saude360_pacote_capacidades
-- R6-BlocoA item 2: granularidade real do Saude 360. O catalogo canônico só
-- registrava PACIENTES e CONSULTAS como modulos finos; os demais codigos que o
-- guard Web cobra por controller (AGENDAMENTOS, TRIAGEM, UNIDADES,
-- CLINICA_DASHBOARD, PAINEL_CHAMADA, CID, PRESCRICOES, CLINICA_FINANCEIRO,
-- CONVENIOS, PLANOS_SAUDE, PENDENCIAS_CLINICAS) nao existiam em
-- modulos_sistema, entao um contrato SAUDE360 valido ainda derrubava paginas
-- clinicas legitimas em MODULO_NAO_CONTRATADO (defeito medido no inventario
-- R6 secao D.2/D.3).
--
-- Modelo decidido (sem preco, sem cobranca extra): SAUDE360 permanece o
-- pacote comercial de topo; as capacidades viram linhas proprias no catalogo
-- com disponivel_comercialmente=false e pacote_codigo='SAUDE360' (relacao
-- pacote->capacidade no proprio catalogo canunico). Quem contrata o pacote
-- passa a ter as capacidades efetivas automaticamente - nenhum insert novo em
-- tenant_modulos, nenhuma machine de estado paralela. A vigencia continua
-- avaliada 1:1 pelo predicado canonical de B6 (ModuleContractVigencia), agora
-- materializado na funcao plantaopro.modulos_efetivos()/modulo_efetivo(), que
-- todo o runtime passa a consultar (claims de login, politica por request,
-- gate clinico da API, autosservico).
--
-- Permissoes por acao preservadas: cada capacidade recebe a MESMA suíte de
-- 31 permissoes X.ACAO do modulo PACIENTES (v2322/v2335), e os grants
-- operacionais translating os conjuntos hardcoded do guard legado
-- (AccessServices pre-v2149) para dados de catalogo: administradores recebem a
-- suíte completa das capacidades (mesmo contrato de v2334/v2335), perfis
-- operacional recebem apenas as acoes que o guard sabe cobrar (VER/LISTAR/
-- VISUALIZAR/CRIAR/EDITAR/EXCLUIR/EXPORTAR/CONFIRMAR) e auditoria recebe
-- somente leitura. Aditiva e idempotente: nao remove nem reduz nada.
-- ============================================================================

DO $migration$
DECLARE
    v_acoes uuid[];
BEGIN
    -- 1) Relacao pacote->capacidade no catalogo canônico -----------------------
    alter table plantaopro.modulos_sistema add column if not exists pacote_codigo text;
    comment on column plantaopro.modulos_sistema.pacote_codigo is
      'R6: codigo do modulo-pacote que inclui esta capacidade (ex.: SAUDE360). Null = modulo de topo, contratado e cobrado individualmente.';
    create index if not exists ix_modulos_sistema_pacote_codigo
        on plantaopro.modulos_sistema (lower(pacote_codigo)) where reg_status = 'A';

    if not exists (select 1 from pg_constraint where conname = 'ck_modulos_sistema_pacote_distinto') then
        alter table plantaopro.modulos_sistema
            add constraint ck_modulos_sistema_pacote_distinto
            check (pacote_codigo is null or lower(pacote_codigo) <> lower(codigo));
    end if;

    if not exists (select 1 from plantaopro.modulos_sistema
                   where lower(codigo) = 'saude360' and reg_status = 'A') then
        raise exception 'v2339: modulo-pacote SAUDE360 ausente do catalogo canônico';
    end if;

    -- 2) Capacidades-filhas faltantes (somente as que o guard/regras ja cobram)
    insert into plantaopro.modulos_sistema
        (id, codigo, nome, descricao, ordem, status, reg_status, reg_date,
         categoria, essencial, disponivel_comercialmente, funcionalidades, metadata)
    select md5('module:' || f.codigo)::uuid, f.codigo, f.nome, f.descricao,
           111 + row_number() over () * 10, 'ATIVO', 'A', now(),
           'SAUDE360', false, false, '[]'::jsonb, '{}'::jsonb
    from (values
        ('AGENDAMENTOS',      'Agendamentos',       'Capacidade inclusa no pacote SAUDE360: agenda, check-in e chegada de pacientes.'),
        ('TRIAGEM',           'Triagem',            'Capacidade inclusa no pacote SAUDE360: priorizacao e encaminhamento de pacientes.'),
        ('UNIDADES',          'Unidades de Atendimento', 'Capacidade inclusa no pacote SAUDE360: unidades clinicas do tenant (R5-E13).'),
        ('CLINICA_DASHBOARD', 'Dashboard Clinico',  'Capacidade inclusa no pacote SAUDE360: visao operacional da clinica.'),
        ('PAINEL_CHAMADA',    'Painel de Chamada',  'Capacidade inclusa no pacote SAUDE360: chamada de pacientes sem expor dados sensiveis.'),
        ('CID',               'CID',                'Capacidade inclusa no pacote SAUDE360: catalogo e uso de cid na consulta.'),
        ('PRESCRICOES',       'Prescricoes',        'Capacidade inclusa no pacote SAUDE360: prescricoes e receitarios.'),
        ('CLINICA_FINANCEIRO','Financeiro Clinico', 'Capacidade inclusa no pacote SAUDE360: contas a receber e faturamento da clinica.'),
        ('CONVENIOS',         'Convenios',          'Capacidade inclusa no pacote SAUDE360: credenciamento e guias de convenios.'),
        ('PLANOS_SAUDE',      'Planos de Saude',    'Capacidade inclusa no pacote SAUDE360: planos vinculados aos convenios.'),
        ('PENDENCIAS_CLINICAS','Pendencias Clinicas','Capacidade inclusa no pacote SAUDE360: pendencias assistenciais por paciente.')
    ) as f(codigo, nome, descricao)
    on conflict (lower(codigo)) where reg_status = 'A' do nothing;

    -- 3) Mapeamento de pacote em todas as capacidades da familia ---------------
    update plantaopro.modulos_sistema
       set pacote_codigo = 'SAUDE360', reg_update = now()
     where reg_status = 'A'
       and lower(codigo) in ('pacientes', 'consultas', 'agendamentos', 'triagem', 'unidades',
                             'clinica_dashboard', 'painel_chamada', 'cid', 'prescricoes',
                             'clinica_financeiro', 'convenios', 'planos_saude', 'pendencias_clinicas')
       and pacote_codigo is distinct from 'SAUDE360';

    -- 4) Suíte de permissoes por capacidade ====================================
    -- Espelha exatamente o conjunto de acoes das permissoes existentes do modulo
    -- PACIENTES (mesma suíte generica canônica); em bancos onde ela ainda não
    -- existe cai no conjunto completo de acoes_sistema ativas.
    select array_agg(distinct px.acao_id) into v_acoes
    from plantaopro.permissoes px
    join plantaopro.modulos_sistema mp on mp.id = px.modulo_id and lower(mp.codigo) = 'pacientes'
    where px.reg_status = 'A' and px.acao_id is not null;

    if v_acoes is null or cardinality(v_acoes) = 0 then
        select array_agg(id) into v_acoes from plantaopro.acoes_sistema where reg_status = 'A';
    end if;

    insert into plantaopro.permissoes (id, codigo, nome, status, reg_status, reg_date, modulo_id, acao_id)
    select gen_random_uuid(), upper(m.codigo || '.' || ac.codigo), m.nome || ' - ' || lower(ac.codigo),
           'ATIVO', 'A', now(), m.id, ac.id
    from plantaopro.modulos_sistema m
    join plantaopro.acoes_sistema ac on ac.id = any (v_acoes) and ac.reg_status = 'A'
    where m.reg_status = 'A'
      and lower(m.codigo) in ('pacientes', 'consultas', 'agendamentos', 'triagem', 'unidades',
                              'clinica_dashboard', 'painel_chamada', 'cid', 'prescricoes',
                              'clinica_financeiro', 'convenios', 'planos_saude', 'pendencias_clinicas')
    on conflict (lower(codigo)) where reg_status = 'A' do nothing;

    -- 5) Grants - administrados/operacao/auditoria =============================
    -- 5a) Administradores do tenant e da clinica administram cada capacidade
    --     contratada via pacote (mesmo contrato de v2334/v2335).
    insert into plantaopro.perfil_permissoes (
        id, perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, status, metadata
    )
    select gen_random_uuid(), pf.id, p.id, true, false, 'A', now(), 'ATIVO', '{}'::jsonb
    from plantaopro.perfis pf
    join plantaopro.permissoes p on p.reg_status = 'A'
    join plantaopro.modulos_sistema ms on ms.id = p.modulo_id
        and upper(ms.codigo) in ('PACIENTES', 'CONSULTAS', 'AGENDAMENTOS', 'TRIAGEM', 'UNIDADES',
                                 'CLINICA_DASHBOARD', 'PAINEL_CHAMADA', 'CID', 'PRESCRICOES',
                                 'CLINICA_FINANCEIRO', 'CONVENIOS', 'PLANOS_SAUDE', 'PENDENCIAS_CLINICAS')
    where upper(pf.codigo) in ('ADMINISTRADOR_CLIENTE', 'ADMINISTRADOR_CLINICA')
      and pf.reg_status = 'A'
      and not exists (
          select 1 from plantaopro.perfil_permissoes pp
          where pp.perfil_id = pf.id and pp.permissao_id = p.id
      );

    -- 5b) Subconjuntos operacionais: traducao dos conjuntos hardcoded do guard
    --     legado (saude360Recepcao/Triagem/Medico/Financeiro/Convenios em
    --     AccessServices) para grants de catalogo. So usa acoes que o resolver
    --     generico do guard sabe exigir e o tipo de leitura definida por perfil.
    insert into plantaopro.perfil_permissoes (
        id, perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, status, metadata
    )
    select gen_random_uuid(), pf.id, p.id, true, false, 'A', now(), 'ATIVO', '{}'::jsonb
    from plantaopro.perfis pf
    join (values
        ('RECEPCAO',             'PACIENTES',          'op'),
        ('RECEPCAO',             'AGENDAMENTOS',       'op'),
        ('RECEPCAO',             'PAINEL_CHAMADA',     'op'),
        ('RECEPCAO',             'CLINICA_DASHBOARD',  'leitura'),
        ('TRIAGEM',              'TRIAGEM',            'op'),
        ('TRIAGEM',              'CLINICA_DASHBOARD',  'leitura'),
        ('ENFERMAGEM',           'TRIAGEM',            'op'),
        ('ENFERMAGEM',           'CLINICA_DASHBOARD',  'leitura'),
        ('COORDENADOR_CLINICO',  'TRIAGEM',            'op'),
        ('COORDENADOR_CLINICO',  'CLINICA_DASHBOARD',  'leitura'),
        ('MEDICO',               'CONSULTAS',          'op'),
        ('MEDICO',               'PRESCRICOES',        'op'),
        ('MEDICO',               'CID',                'op'),
        ('MEDICO',               'CLINICA_DASHBOARD',  'leitura'),
        ('MEDICO',               'AGENDAMENTOS',       'leitura'),
        ('MEDICO',               'TRIAGEM',            'leitura'),
        ('FINANCEIRO_CLINICA',   'CLINICA_FINANCEIRO', 'op'),
        ('FINANCEIRO',           'CLINICA_FINANCEIRO', 'relatorio'),
        ('FATURAMENTO_CONVENIO', 'CONVENIOS',          'op'),
        ('FATURAMENTO_CONVENIO', 'PLANOS_SAUDE',       'op')
    ) as r(perfil, modulo, tipo) on upper(pf.codigo) = r.perfil
    join plantaopro.permissoes p on p.reg_status = 'A'
    join plantaopro.modulos_sistema ms on ms.id = p.modulo_id and upper(ms.codigo) = r.modulo
    join plantaopro.acoes_sistema ac on ac.id = p.acao_id
        and ac.codigo = any (case r.tipo
                                when 'op'       then array['VER', 'LISTAR', 'VISUALIZAR', 'CRIAR', 'EDITAR', 'EXCLUIR', 'EXPORTAR', 'CONFIRMAR']
                                when 'relatorio' then array['VER', 'LISTAR', 'VISUALIZAR', 'EXPORTAR', 'CONFIRMAR']
                                else array['VER', 'LISTAR', 'VISUALIZAR']
                             end)
    where pf.reg_status = 'A'
      and not exists (
          select 1 from plantaopro.perfil_permissoes pp
          where pp.perfil_id = pf.id and pp.permissao_id = p.id
      );

    -- 5c) Auditor clinico: somente leitura em toda a familia (guard legado
    --     liberava qualquer SAUDE360_* para leitura).
    insert into plantaopro.perfil_permissoes (
        id, perfil_id, permissao_id, permitido, bloqueado_por_plano, reg_status, reg_date, status, metadata
    )
    select gen_random_uuid(), pf.id, p.id, true, false, 'A', now(), 'ATIVO', '{}'::jsonb
    from plantaopro.perfis pf
    join plantaopro.permissoes p on p.reg_status = 'A'
    join plantaopro.modulos_sistema ms on ms.id = p.modulo_id
        and upper(ms.codigo) in ('PACIENTES', 'CONSULTAS', 'AGENDAMENTOS', 'TRIAGEM', 'UNIDADES',
                                 'CLINICA_DASHBOARD', 'PAINEL_CHAMADA', 'CID', 'PRESCRICOES',
                                 'CLINICA_FINANCEIRO', 'CONVENIOS', 'PLANOS_SAUDE', 'PENDENCIAS_CLINICAS')
    join plantaopro.acoes_sistema ac on ac.id = p.acao_id
        and ac.codigo in ('VER', 'LISTAR', 'VISUALIZAR')
    where upper(pf.codigo) = 'AUDITOR_CLINICO'
      and pf.reg_status = 'A'
      and not exists (
          select 1 from plantaopro.perfil_permissoes pp
          where pp.perfil_id = pf.id and pp.permissao_id = p.id
      );

    -- 6) Kernel canonico de contratacao efetiva =================================
    -- MESMA semantica de ModuleContractVigencia.EffectivePredicate (B6) +
    -- heranca de capacidades pelo pacote (pacote_codigo). O texto do predicado e
    -- verificado por teste de sincronia contra o C# (R6GranularidadeSaude360Tests).
    create or replace function plantaopro.modulos_efetivos(p_tenant_id uuid)
    returns table (codigo text, modulo_id uuid, pacote_codigo text)
    language sql stable
    as $fn$
        select distinct upper(ms.codigo), ms.id, nullif(ms.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema ms on ms.id = tm.modulo_id and ms.reg_status = 'A' and upper(ms.status) = 'ATIVO'
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
        union
        select distinct upper(ch.codigo), ch.id, nullif(ch.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema pkg on pkg.id = tm.modulo_id and pkg.reg_status = 'A' and upper(pkg.status) = 'ATIVO'
        join plantaopro.modulos_sistema ch on ch.reg_status = 'A' and upper(ch.status) = 'ATIVO'
            and lower(ch.pacote_codigo) = lower(pkg.codigo)
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
        union
        select distinct upper(ms.codigo), ms.id, nullif(ms.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema ms on lower(ms.codigo) = lower(nullif(tm.codigo_modulo, '')) and ms.reg_status = 'A' and upper(ms.status) = 'ATIVO'
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()))
        union
        select distinct upper(ch.codigo), ch.id, nullif(ch.pacote_codigo, '')
        from plantaopro.tenant_modulos tm
        join plantaopro.modulos_sistema pkg on lower(pkg.codigo) = lower(nullif(tm.codigo_modulo, '')) and pkg.reg_status = 'A' and upper(pkg.status) = 'ATIVO'
        join plantaopro.modulos_sistema ch on ch.reg_status = 'A' and upper(ch.status) = 'ATIVO'
            and lower(ch.pacote_codigo) = lower(pkg.codigo)
        where tm.tenant_id = p_tenant_id
          and tm.reg_status='A' and tm.desativado_em is null
          and ((upper(coalesce(tm.status,'ATIVO'))='ATIVO' and tm.habilitado=true and (tm.ativado_em is null or tm.ativado_em<=now()))
            or (upper(coalesce(tm.status,''))='AGENDADO' and coalesce(tm.ativado_em,tm.reg_date)<=now()));
    $fn$;

    create or replace function plantaopro.modulo_efetivo(p_tenant_id uuid, p_codigo text)
    returns boolean
    language sql stable
    as $fn$
        select exists(
            select 1 from plantaopro.modulos_efetivos(p_tenant_id) e
            where e.codigo = upper(btrim(p_codigo)));
    $fn$;

    comment on function plantaopro.modulos_efetivos(uuid) is
      'R6: unicos modulos efetivamente contratados do tenant (predicado B6 + capacidades herdadas do pacote). Fonte unica do kernel de acesso.';
    comment on function plantaopro.modulo_efetivo(uuid, text) is
      'R6: consulta pontual de contratacao efetiva (inclui heranca de pacote). Usado por politicas por request e gates de modulo.';
END
$migration$;
