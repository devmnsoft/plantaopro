# Matriz de rotas e ações reais v1.70.2

| Área | Origem real | Ação disponível | Comportamento sem backend/dado |
|---|---|---|---|
| Notificações | `bff/operacao/notificacoes/nao-lidas` | abrir, filtrar, marcar uma/todas como lidas | mensagem por status; contador oculto |
| Minha Assinatura | `GET api/minha-assinatura` | consulta somente leitura | empty state sem CTA contratual fictício |
| Admin SaaS | modelo entregue pelo controller | abrir Planos, Onboarding, Implantação e governança | cards ausentes geram empty state |
| Relatórios | actions de `RelatoriosController` | abrir relatório implementado | automação e favoritos indisponíveis com motivo |
| Configurações | perfil autenticado e rotas MVC existentes | abrir área configurável | erro/empty state com nova tentativa real |

Destinos recebidos em notificações só são ativados quando `safeDestination` confirma a mesma origem.
