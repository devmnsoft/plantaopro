# Análise — Admin SaaS e Governança v1.81.0

| Tela | Rota | Controller/action | Regra esperada | Dado real existente | Ação real | Sem backend | Correção | Pendência |
|---|---|---|---|---|---|---|---|---|
| Central Admin | `/AdminSaas/Index` | `AdminSaas.Index` | não presumir tenant, plano ou uso | não há agregado | navegar às fontes | diagnóstico consolidado | central por fonte e CTA desabilitado | DTO agregado auditável |
| Planos | `/Planos` | `Planos.Index` | catálogo somente da API | catálogo paginado | detalhes, edição e status | plano atual/contratação | preservado catálogo real | contratação e plano atual |
| Assinatura | `/MinhaAssinatura` | `MinhaAssinatura.Index` | contrato somente da API | DTO `api/minha-assinatura` | consulta | upgrade, downgrade, cancelamento | empty state e campos condicionais | endpoints de mutação |
| Configurações | `/Configuracoes` | `Configuracoes.Index` | status somente com fonte | conta autenticada | dez rotas de domínio | prontidão por domínio | agrupamento e status não informado | DTO de configuração |
| Usuários | `/Usuarios` | `Usuarios.Index` | usuários/perfis reais | API existente, sujeita a policy | consulta | matriz efetiva agregada | aviso de autorização | endpoint de permissões |
| Auditoria | `/Auditoria` | `Auditoria.Index` | eventos persistidos | rota existente | consulta | exportação consolidada | acesso por fonte | contrato de exportação |
| LGPD | `/Lgpd` | `Lgpd.Index` | evidências persistidas | rota existente | consulta | relatório agregado | acesso por fonte | endpoint de evidências |
| Relatórios | `/Relatorios` | `Relatorios.Index` | gerar/exportar com endpoint | SaaS possui fonte própria | abrir visões reais | governança agregada | ações desabilitadas com motivo | endpoints administrativos |
