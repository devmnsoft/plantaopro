# Faturamento SaaS — Release Candidate

## Fluxo básico homologável

1. Admin global cria cliente.
2. Admin global cria plano ativo com limites de médicos, hospitais, plantões/mês, mobile e BI.
3. Admin global cria assinatura ativa para o cliente.
4. Sistema impede mais de uma assinatura ativa por cliente.
5. Sistema gera fatura SaaS.
6. Financeiro/Admin global marca fatura como paga informando valor, data e forma.
7. Sistema permite simular vencimento/inadimplência.
8. Cliente inadimplente pode ser suspenso com justificativa.
9. Cliente suspenso não publica plantão.
10. Reativação libera operação e registra auditoria.

## Critérios de aceite

- Plano inativo não gera nova assinatura.
- Cliente cancelado não opera.
- Limites do plano bloqueiam excesso de médicos, hospitais e plantões publicados no mês.
- Plano sem mobile bloqueia API Mobile com 403 amigável.
- Plano sem BI bloqueia BI.
- Pagamento de fatura exige valor maior que zero, data e forma.
- Cancelamento de fatura e suspensão de cliente exigem justificativa.

## Demonstração comercial

Mostrar a relação entre plano contratado, uso operacional, faturas, inadimplência e bloqueio controlado sem expor dados técnicos ou stack trace.
