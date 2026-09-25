-- OPERAÇÃO EXPLÍCITA: somente Development/Homologação e somente gestor Santa Casa.
-- Execute depois do seed 121 apenas quando for necessário restaurar a senha
-- documentada SantaCasa!Demo2026#Gestor. Revoga as sessões anteriores.
do $reset$
declare v_id uuid := 'd3f6584c-2c64-4e5a-9ea9-4e1428647511';
begin
 if current_database() in ('template0','template1') then raise exception 'Banco não elegível'; end if;
 if not exists(select 1 from plantaopro.usuarios where id=v_id and lower(email)='gestor@santacasa-demo.example'
               and tenant_id='d3f6584c-2c64-4e5a-9ea9-4e1428647502') then
   raise exception 'Identidade exata do gestor demo não encontrada; nenhuma senha foi alterada';
 end if;
 update plantaopro.usuarios set senha_hash='$2a$11$mNmgw83PBauw.5XsFVB5TuMB1.8OgFZ0SpfujQSuGzUgzwvs7d8S.',
   senha_alteracao_obrigatoria=false,bloqueado_ate=null,reg_update=now() where id=v_id;
 update plantaopro.auth_sessoes set revogada_em=coalesce(revogada_em,now()),motivo_revogacao=coalesce(motivo_revogacao,'DEMO_PASSWORD_RESET'),reg_update=now()
 where usuario_id=v_id and revogada_em is null;
end $reset$;
