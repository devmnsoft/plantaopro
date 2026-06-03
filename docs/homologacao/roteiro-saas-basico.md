# Roteiro de Homologação — SaaS Básico

## Objetivo
Validar criação comercial, assinatura, limites, faturamento, inadimplência e bloqueio/liberação operacional do cliente.

## Pré-condições
- Usuário `ADMINISTRADOR_GLOBAL` ativo.
- Banco atualizado com scripts incrementais no schema `plantaopro`.
- Cliente piloto identificado para homologação.

## Passo a passo
1. Entrar como `ADMINISTRADOR_GLOBAL`.
2. Criar cliente piloto com status `ATIVO` ou `TESTE`.
3. Criar plano ativo contendo limites de médicos, hospitais, plantões/mês, mobile e BI.
4. Criar assinatura ativa para o cliente e validar que não há segunda assinatura ativa simultânea.
5. Abrir uso do plano e validar consumo de médicos, hospitais, plantões, mobile e BI.
6. Criar hospital vinculado ao cliente e validar bloqueio ao atingir limite.
7. Criar usuários do cliente e confirmar que só acessam o próprio `cliente_id`.
8. Gerar fatura SaaS mensal.
9. Marcar fatura como paga informando valor pago, data de pagamento e forma.
10. Gerar ou simular fatura vencida e validar lista de inadimplência.
11. Suspender cliente informando justificativa.
12. Tentar publicar plantão e confirmar bloqueio amigável.
13. Reativar cliente e confirmar liberação da operação.
14. Validar auditoria e notificação para administrador do cliente.

## Resultado esperado
- Plano inativo não permite nova assinatura.
- Cliente suspenso/cancelado não opera.
- Limites comerciais impedem expansão fora do contrato.
- Faturas exigem dados obrigatórios e justificativas em cancelamento/contestação.
- Todos os bloqueios exibem mensagem amigável e são auditados.

## Critérios de aprovação
- Nenhuma etapa exibe exception técnica.
- Fluxo pode ser demonstrado em até 15 minutos.
- Faturamento SaaS básico fica rastreável em auditoria e relatórios.
