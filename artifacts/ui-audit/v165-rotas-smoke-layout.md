# Rotas do smoke de layout v1.65

O script `scripts/ui/visual-smoke.mjs` valida 360×800, 390×844, 430×932, 768×1024, 1024×768, 1366×768 e 1920×1080.

Rotas: `/`, `/Account/Login`, `/cadastro/empresa`, `/AdminSaas/Index`, `/Home/Dashboard`, `/MinhaCentral`, `/MeuDia`, `/Agenda`, `/Plantoes`, `/Escalas`, `/Saude360`, `/Pacientes`, `/Agendamentos`, `/Triagem`, `/Consultas`, `/Pagamentos`, `/Financeiro`, `/Relatorios` e `/Configuracoes`.

Contratos: ausência de overflow horizontal crítico; shell/content/topbar presentes; topbar e sidebar sem cobrir conteúdo; footer depois do conteúdo; cards dimensionados; tabelas responsivas; hero proporcional; ação primária visível; drawers e modal acima da sidebar; confirmação oculta por padrão; formulário self-service presente.

Screenshots, quando o runtime e uma sessão autenticada estiverem disponíveis, são gravados em `artifacts/ui-audit/screenshots/v165/`.
