-- PlantãoPro v2.17.0: concorrência e rastreabilidade da cobertura/substituição.
alter table plantaopro.substituicoes_plantao
    add column if not exists versao bigint not null default 1,
    add column if not exists nova_escala_id uuid,
    add column if not exists cancelada_em timestamptz;

-- Um pedido ativo por atribuição. Estados finais permanecem consultáveis e auditáveis.
create unique index if not exists ux_v2170_substituicao_ativa_por_escala
    on plantaopro.substituicoes_plantao(escala_id)
    where reg_status='A' and status in ('SOLICITADA','APROVADA','SUBSTITUTO_CONVIDADO','AGUARDANDO_APROVACAO');

-- A nova atribuição é exclusiva da efetivação e relaciona as duas pontas sem mover presença/financeiro.
create unique index if not exists ux_v2170_substituicao_nova_escala
    on plantaopro.substituicoes_plantao(nova_escala_id)
    where nova_escala_id is not null;

alter table plantaopro.substituicao_candidatos
    add column if not exists expira_em timestamptz,
    add column if not exists respondido_em timestamptz,
    add column if not exists notificacao_status varchar(24) not null default 'PENDENTE';

create unique index if not exists ux_v2170_candidato_convite_ativo
    on plantaopro.substituicao_candidatos(substituicao_id,medico_id)
    where reg_status='A' and status in ('CONVIDADO','ACEITO');
