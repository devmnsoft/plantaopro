WITH catalog(codigo,nome,ordem,sensivel) AS (VALUES
 ('VER','Ver',10,false),('LISTAR','Listar',20,false),('CRIAR','Criar',30,false),('EDITAR','Editar',40,false),
 ('GERENCIAR','Gerenciar',50,true),('SUSPENDER','Suspender',60,true),('IMPERSONAR','Impersonar',70,true),
 ('PUBLICAR','Publicar',80,true),('CANCELAR','Cancelar',90,true),('CONFIRMAR','Confirmar',100,false),
 ('RECUSAR','Recusar',110,false),('SUBSTITUIR','Substituir',120,true),('INICIAR','Iniciar',130,false),
 ('FINALIZAR','Finalizar',140,true),('EXPORTAR','Exportar',150,true)
)
INSERT INTO plantaopro.acoes_sistema(id,codigo,nome,descricao,ordem,sensivel,status,reg_status)
SELECT md5('action:'||codigo)::uuid,codigo,nome,'Ação canônica PlantãoPro',ordem,sensivel,'ATIVO','A' FROM catalog
ON CONFLICT DO NOTHING;
