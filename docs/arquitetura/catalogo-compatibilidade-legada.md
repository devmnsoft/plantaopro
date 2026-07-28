# Catálogo de compatibilidade legada

Este catálogo impede que contratos genéricos da homologação v1.12 voltem a orientar a experiência do produto. Os aliases preservados são internos, autenticados e isolados por tenant; não devem aparecer em menus ou textos de interface.

| Contrato v1.12 | Classificação | Contrato atual | Diretriz |
|---|---|---|---|
| `customers` | SUBSTITUÍDO | Clientes e tenants | Usar o domínio SaaS canônico; manter alias apenas enquanto houver consumidor identificado. |
| `products` | OBSOLETO | Planos, módulos e itens faturáveis | Não recriar catálogo genérico. |
| `inventory` | REMOVÍVEL | Não pertence às jornadas essenciais | Remover o alias após confirmar ausência de tráfego e integrações. |
| `orders` | SUBSTITUÍDO | Assinaturas, faturas e cobranças | Usar os fluxos comerciais e financeiros tipados. |
| `fake boleto` | OBSOLETO | Simulação de cobrança de homologação | Nunca disponibilizar como operação financeira real. |
| `outbox` | COMPATIBILIDADE | Processamento assíncrono interno | Preservar somente para diagnóstico autorizado; não expor na navegação. |
| `templates` | COMPATIBILIDADE | Modelos de implantação | Preservar instalações existentes sem promover um módulo genérico. |
| `tasks` | SUBSTITUÍDO | Pendências e Meu Dia | Usar prioridades contextualizadas por perfil. |
| `homologation/status` | CANÔNICO | Diagnóstico operacional | Rota técnica restrita a observabilidade. |
| `validation/worker/status` | CANÔNICO | Saúde de workers | Rota técnica restrita a observabilidade. |

## Política de retirada

Um alias só pode ser removido depois de inventário de consumidores, análise de telemetria, comunicação aos responsáveis e janela de depreciação. Testes devem validar metadados de rotas ou OpenAPI, em vez de procurar texto em controllers.
