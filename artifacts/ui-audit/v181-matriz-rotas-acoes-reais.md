# Matriz de rotas e ações reais — v1.81.0

| Rota | Ação real | Condição | Ação indisponível |
|---|---|---|---|
| `/AdminSaas/Index` | navegar às fontes | role autorizada | diagnóstico consolidado |
| `/Planos` | listar/detalhar/administrar catálogo | API e autorização | contratação/plano atual |
| `/MinhaAssinatura` | consultar contrato | token e API | upgrade, downgrade, cancelamento |
| `/Configuracoes` | abrir dez domínios | rota e autorização | status agregado |
| `/Usuarios` | consultar usuários | API/policy | matriz agregada nesta central |
| `/Auditoria` e `/Lgpd` | consultar fontes | policy | exportação consolidada |
| `/Relatorios` | abrir relatórios implementados | endpoint específico | relatórios administrativos agregados |
