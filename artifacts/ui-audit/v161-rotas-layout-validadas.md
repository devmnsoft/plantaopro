# Rotas e layout validados — v1.61

O smoke em `scripts/ui/visual-smoke.mjs` navega nas rotas abaixo e registra screenshots em `artifacts/ui-audit/screenshots/v161/` quando um runtime autenticado está disponível.

| Rota | Raiz esperada | Verificações |
|---|---|---|
| `/Account/Login` | `.pp-auth-page` | raiz auth, botão visível, overflow e dimensões |
| `/AdminSaas/Index` | `.pp-page.pp-admin-saas-page` | shell, container, cards, footer e conteúdo livre da sidebar |
| `/Home/Dashboard` | shell autenticado | sidebar, content, container, cards, botões e footer |
| `/MinhaCentral` | shell autenticado | idem |
| `/MeuDia` | shell autenticado | idem |
| `/Agenda` | shell autenticado | idem |
| `/Plantoes` | shell autenticado | idem |
| `/Escalas` | shell autenticado | idem |
| `/Saude360` | shell autenticado | idem |
| `/Pacientes` | shell autenticado | idem |
| `/Agendamentos` | shell autenticado | idem |
| `/Triagem` | shell autenticado | idem |
| `/Consultas` | shell autenticado | idem |
| `/Pagamentos` | shell autenticado | idem |
| `/Configuracoes` | shell autenticado | idem |

Viewports: **360, 390, 430, 768, 1024, 1366 e 1920 px**. Rotas autenticadas exigem `PLANTAOPRO_STORAGE_STATE`; sem essa evidência, o script falha em vez de produzir falso positivo.
