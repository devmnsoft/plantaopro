-- R96 (B6 onboarding self-service): teste grátis no funil público.
-- O cadastro self-service (POST /api/public/cadastro/finalizar) agora aceita
-- statusAssinatura=TRIAL com diasTrial explícito (1..90): a assinatura nasce
-- TRIAL, sem cobrança inicial, e com data_fim = teste + 30 dias de janela de
-- conversão (MESMO núcleo do provisionamento manual B2B). Este arquivo só
-- alimenta o catálogo público de dúvidas (plano_faq), porque o FAQ da página
-- pública é lido do banco e o funil não deve inventar respostas fora dele.
-- Idempotente: not-exists por pergunta (reaplicação do runner não duplica).
insert into plantaopro.plano_faq (plano_id, pergunta, resposta, ordem, status, reg_date, reg_status)
select null, 'Como funciona o teste grátis?', 'Escolha "Começar com teste grátis" na etapa de plano e informe os dias (de 1 a 90). A assinatura nasce em teste, sem cobrança inicial, e você já usa a plataforma normalmente.', 40, 'ATIVO', now(), 'A'
where not exists (select 1 from plantaopro.plano_faq where reg_status='A' and pergunta='Como funciona o teste grátis?');

insert into plantaopro.plano_faq (plano_id, pergunta, resposta, ordem, status, reg_date, reg_status)
select null, 'Quando começo a pagar no teste grátis?', 'Somente depois do fim do período testado, se você decidir assinar. Após o teste ainda há uma janela de 30 dias para decidir antes de qualquer cobrança.', 50, 'ATIVO', now(), 'A'
where not exists (select 1 from plantaopro.plano_faq where reg_status='A' and pergunta='Quando começo a pagar no teste grátis?');

insert into plantaopro.plano_faq (plano_id, pergunta, resposta, ordem, status, reg_date, reg_status)
select null, 'O que acontece quando o teste termina?', 'O acesso continua durante a janela de decisão (30 dias após o fim do teste). Para converter, siga a orientação na página Minha assinatura; sem decisão registrada, nada é cobrado automaticamente.', 60, 'ATIVO', now(), 'A'
where not exists (select 1 from plantaopro.plano_faq where reg_status='A' and pergunta='O que acontece quando o teste termina?');
