-- PlantãoPro v2.35.2 (R9-C / §6): cadastro regulatório ANVISA para produtos do Administrativo 360.
-- Requisitos da rodada: número como TEXTO (preserva zeros à esquerda — nunca numérico),
-- aplicabilidade tri-estável (INFORMADO / NAO_APLICAVEL / PENDENTE), flag de entrada manual
-- (não há busca simulada: toda entrada atual é manual), vigência REGULATÓRIA independente da
-- validade do lote, e justificação obrigatória quando o produto é marcado como não aplicável.
-- Rerun idempotente: add column if not exists / drop-add constraint.

alter table plantaopro.adm360_produtos
    add column if not exists anvisa_numero varchar(60),
    add column if not exists anvisa_aplicabilidade varchar(15) not null default 'PENDENTE',
    add column if not exists anvisa_entrada_manual boolean not null default true,
    add column if not exists anvisa_vigencia date,
    add column if not exists anvisa_justificacao text;

alter table plantaopro.adm360_produtos
    drop constraint if exists ck_adm360_produtos_anvisa_aplicabilidade,
    add constraint ck_adm360_produtos_anvisa_aplicabilidade
        check (anvisa_aplicabilidade in ('INFORMADO', 'NAO_APLICAVEL', 'PENDENTE'));
