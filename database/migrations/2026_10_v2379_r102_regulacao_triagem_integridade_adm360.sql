-- PlantaoPro v2.36.9 (R102 - regulação de triagem e integridade adm360): dois pacotes da
-- rodada em um unico arquivo idempotente, ambos com guarda de estado.
--
-- Parte 1 (R102, linha do plano r66a0): regulação da triagem — o encaminhamento de
-- triagem agora carrega a decisao de regulação/orientacao do fluxo assistencial: medico
-- de destino (opcional) e orientacao registrada (texto). A regulação e gravada pela
-- acao "regularizar" da triagem (finaliza a triagem e registra a regulacao).
-- Idempotente: add column if not exists.
alter table plantaopro.triagem_encaminhamentos add column if not exists medico_id uuid null;
alter table plantaopro.triagem_encaminhamentos add column if not exists observacao text not null default '';

-- Parte 2 (pendencia herdada da R95): os FKs de vales/cotacoes existiam NOT VALID por
-- causa de linhas orfaas herdadas (itens de vale sem vale pai e envios de cotacao sem
-- resposta). Limpeza deterministica das orfaas e validacao das constraints para fechar
-- a integridade referencial real dos modulos adm360. Idempotente: deletes por ausencia
-- de pai (reexecucao apaga zero linhas) e validate com guarda de existencia.
delete from plantaopro.adm360_vale_itens where not exists (select 1 from plantaopro.adm360_vales v where v.id = vale_id);
delete from plantaopro.adm360_cotacao_envios where resposta_id is not null and not exists (select 1 from plantaopro.adm360_cotacao_respostas r where r.id = resposta_id);

do $$
begin
    if exists (select 1 from pg_constraint c where c.conname = 'adm360_vale_itens_vale_id_fkey') then
        alter table plantaopro.adm360_vale_itens validate constraint adm360_vale_itens_vale_id_fkey;
    end if;
    if exists (select 1 from pg_constraint c where c.conname = 'adm360_cotacao_envios_resposta_id_fkey') then
        alter table plantaopro.adm360_cotacao_envios validate constraint adm360_cotacao_envios_resposta_id_fkey;
    end if;
end $$;
