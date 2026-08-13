# Matriz de funcionalidades administrativas — v1.69.0

| Área | Fonte real | Estado entregue | Ação real | Pendência |
|---|---|---|---|---|
| Notificações | BFF `/bff/operacao/notificacoes` | Drawer acessível, filtros, empty/error state e contador sob demanda | Ler, ler todas, abrir origem mesma origem e histórico | Depende da disponibilidade do BFF |
| Assinatura | Rotas `MinhaAssinatura` | Valores fictícios removidos; ausência de vínculo explicitada | Uso, limites, módulos, faturas e opções | Integrar resumo contratual ao backend |
| Admin SaaS | `CommercialDemoPageViewModel` | Cockpit, áreas, checklist e atalhos de governança | Planos, onboarding, implantação, billing, LGPD, auditoria | Status detalhado depende da fonte administrativa |
| Relatórios | Rotas `Relatorios` | Catálogo executivo com categorias e indisponibilidades honestas | Abrir relatórios implementados | Persistência de favoritos/agendamentos |
| Configurações | `UserSettingsSummaryViewModel` e rotas administrativas | Central agrupada por responsabilidade | Perfil, usuários, assinatura, marca, notificações, LGPD, integrações e parâmetros | Datas de atualização dependem do backend |
