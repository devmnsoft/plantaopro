# Checklist v1.70 — Admin, billing e notificações

| Área | Evidência | Estado |
|---|---|---|
| Notificações | API canônica `/api/notificacoes`, destinatário e tenant no repositório | Integrada pelo BFF autenticado |
| Contador | Derivado da lista real de não lidas | Sem fallback fictício |
| Leitura | POST individual e em lote | Ação real |
| Segurança | Bearer da sessão/claims, credenciais same-origin, URL de destino same-origin | Validada estaticamente |
| Falhas | Mensagens próprias para 401, 403, 404 e erro de rede | Implementado |
| Minha assinatura | GET `/api/minha-assinatura` | Integrado |
| Billing ausente | Empty state sem plano, preço ou data presumidos | Implementado |
| Admin SaaS | Atalhos apontam para controllers existentes | Mantido |
| Build/runtime | SDK .NET ausente | Bloqueado pelo ambiente |
