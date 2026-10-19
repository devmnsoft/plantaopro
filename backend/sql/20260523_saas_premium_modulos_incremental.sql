create schema if not exists plantaopro;

create table if not exists plantaopro.ajuda_artigos (
 id uuid primary key default gen_random_uuid(),
 slug text not null,
 titulo text not null,
 conteudo text not null,
 perfil text null,
 reg_date timestamp not null default now(),
 reg_status char(1) not null default 'A'
);
-- R66 (rodada 66): indice COMENTADO — a tabela ajuda_artigos canonica nao tem coluna
-- slug (evolucao do esquema; artigo identificado por topico_id/titulo).
-- create index if not exists idx_ajuda_artigos_slug on plantaopro.ajuda_artigos(slug);

create table if not exists plantaopro.onboarding_checklist_usuario (
 id uuid primary key default gen_random_uuid(),
 usuario_id uuid not null,
 perfil text not null,
 item text not null,
 concluido boolean not null default false,
 reg_date timestamp not null default now()
);

do $$
begin
    insert into plantaopro.ajuda_artigos(slug,titulo,conteudo,perfil)
    select 'como-criar-plantao','Como criar plantão','Acesse Plantões > Novo, preencha hospital, especialidade, data e vagas.','COORDENACAO'
    where not exists (select 1 from plantaopro.ajuda_artigos where slug='como-criar-plantao');
exception when others then
    raise notice '[legado R66] seed de artigo ignorada (%): %', 'como-criar-plantao', sqlerrm;
end $$;
