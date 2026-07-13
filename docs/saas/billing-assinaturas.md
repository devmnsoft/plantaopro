# PlantãoPro Saúde 360 — SaaS comercial operacional

## Escopo entregue

Esta documentação descreve a evolução SaaS comercial do PlantãoPro: clientes, tenants, planos, limites, billing interno, onboarding, white label, marketplace, suporte, observabilidade, Customer Success e QA de homologação.

## Operação principal

1. Admin Global cadastra o cliente SaaS.
2. Admin Global cria o tenant com subdomínio e plano.
3. O plano libera módulos e limites.
4. Billing registra assinatura, faturas e eventos sem armazenar cartão ou dados bancários sensíveis.
5. Onboarding orienta implantação por etapas.
6. White label aplica marca, cores, domínio e comunicação do cliente quando contratado.
7. Marketplace permite solicitação e aprovação de módulos.
8. Suporte registra chamados por tenant com SLA.
9. Operação monitora health, erros, uso e auditoria.
10. Customer Success acompanha health score, riscos e oportunidades.

## Pendências reais

- Aplicar migrations no banco de homologação antes de validar endpoints conectados.
- Conectar telas a formulários ricos para todas as entidades quando houver design final aprovado.
- Integrar billing a provedor externo apenas via tokenização/checkout hospedado, sem persistir cartão.
- Executar QA manual em ambiente com .NET SDK disponível.
