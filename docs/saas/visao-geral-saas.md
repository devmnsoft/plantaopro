# PlantãoPro SaaS Inteligente

O módulo SaaS centraliza gestão comercial multiempresa do PlantãoPro: clientes, planos, recursos, assinaturas, limites, faturamento, saúde do cliente, alertas e recomendações de upgrade.

## Fluxo principal

1. Cadastre o cliente em **Clientes**.
2. Cadastre ou revise o plano em **Planos**.
3. Crie uma assinatura **TRIAL** ou **ATIVA**.
4. Acompanhe uso do plano em **Uso do Plano**.
5. Gere faturas mensais em **Faturamento SaaS**.
6. Recalcule saúde em **Inteligência SaaS**.
7. Trate alertas e registre interações de Customer Success.

## Regras inteligentes

- Cliente **SUSPENSO** ou **CANCELADO** é bloqueado para operação.
- Assinatura sem status operacional bloqueia publicação e uso de recursos contratados.
- Uso acima de 80% gera alerta comercial.
- Uso acima de 100% bloqueia ação e registra bloqueio.
- Faturas vencidas reduzem score de saúde e geram risco.
