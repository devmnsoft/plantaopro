# Rotas do smoke de layout v1.63.0

O script `scripts/ui/visual-smoke.mjs` cobre 360×800, 390×844, 430×932, 768×1024, 1024×768, 1366×768 e 1920×1080.

Rotas: `/`, `/Account/Login`, `/AdminSaas/Index`, `/Home/Dashboard`, `/MinhaCentral`, `/MeuDia`, `/Agenda`, `/Plantoes`, `/Escalas`, `/Saude360`, `/Pacientes`, `/Agendamentos`, `/Triagem`, `/Consultas`, `/Pagamentos`, `/Financeiro`, `/Relatorios` e `/Configuracoes`.

Contratos verificados: overflow horizontal, presença do shell/container, topbar sem sobreposição, sidebar sem cobrir conteúdo, dimensões dos cards, ação primária visível, login sem corte, drawers acima do shell e toasts livres da navegação móvel. Screenshots são gravados em `artifacts/ui-audit/screenshots/v163/` quando há runtime e sessão autenticada disponíveis.
