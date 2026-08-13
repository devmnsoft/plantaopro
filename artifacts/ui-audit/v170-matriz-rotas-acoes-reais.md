# Matriz v1.70 — rotas e ações reais

| Superfície | Rota/endpoint | Ação |
|---|---|---|
| Drawer | `/bff/operacao/notificacoes/nao-lidas` | Lista e conta registros persistidos |
| Drawer | `/bff/operacao/notificacoes/{id}/lida` | Marca o registro do destinatário como lido |
| Drawer | `/bff/operacao/notificacoes/marcar-todas-lidas` | Marca os registros do usuário como lidos |
| Histórico | `/Notificacoes` | Abre a central existente |
| Assinatura | `/MinhaAssinatura` → `/api/minha-assinatura` | Consulta contrato do tenant autenticado |
| Uso | `/MinhaAssinatura/Uso` | Abre jornada existente; não presume medição |
| Faturas | `/MinhaAssinatura/Faturas` | Abre jornada existente; não presume cobrança |
| Relatórios | `/Relatorios` | Catálogo limitado às actions implementadas |
| Configurações | `/Configuracoes` | Links para controllers existentes |
