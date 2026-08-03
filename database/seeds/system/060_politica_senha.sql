INSERT INTO plantaopro.politicas_senha(id,tenant_id,tamanho_minimo,exige_maiuscula,exige_minuscula,exige_numero,exige_especial,expiracao_dias,tentativas_permitidas,bloqueio_minutos,reg_status)
SELECT md5('password-policy:global')::uuid,NULL,12,true,true,true,true,90,5,15,'A'
WHERE NOT EXISTS (SELECT 1 FROM plantaopro.politicas_senha WHERE tenant_id IS NULL AND reg_status='A');
