# Matriz de rotas e ações reais v1.71.0

| Superfície | Ação | Destino real | Fonte |
|---|---|---|---|
| Admin SaaS | Planos e limites | `/Planos` | Controller MVC |
| Minha Assinatura | Consultar planos | `/Planos` | Controller MVC |
| Minha Assinatura | Central administrativa | `/Configuracoes` | Controller MVC |
| Configurações | Usuários e permissões | `/Usuarios` | Controller MVC |
| Configurações | LGPD | `/Lgpd` | Controller + API |
| Configurações | Auditoria | `/Auditoria` | Controller + API |
| Configurações | Notificações | `/Notificacoes/Preferencias` | Controller MVC |
| Relatórios | Catálogo executivo | `/Relatorios/*` | Actions MVC existentes |

Recursos sem endpoint não recebem CTA habilitado. A autorização final permanece a cargo do backend e do perfil autenticado.
