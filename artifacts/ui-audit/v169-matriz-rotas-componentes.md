# Matriz de rotas e componentes — v1.69.0

| Rota(s) | Tipo | Componentes esperados | Visual | Mobile | Correção v1.69 | Pendência real |
|---|---|---|---|---|---|---|
| `/`, `/Planos` | Pública | `pp-public-hero`, cards públicos | Automatizado | Automatizado | Smoke versionado | Executar contra runtime |
| `/Account/Login` | Pública | `pp-auth-page`, `pp-auth-shell`, `pp-auth-card` | Contrato | Contrato | Auditoria específica | Executar contra runtime |
| `/cadastro/empresa` | Pública | `pp-selfservice-page`, `pp-onboarding-form` | Contrato | Contrato | Auditoria específica | Executar contra runtime |
| `/AdminSaas/Index` | Autenticada | `pp-admin-layout`, KPI, checklist | Contrato | Automatizado | Mantida hierarquia operacional | Requer storage state |
| `/Home/Dashboard`, `/MinhaCentral`, `/MeuDia` | Autenticada | `pp-page`, cards, drawers | Automatizado | Automatizado | Palette e overlays auditados | Requer storage state |
| `/Agenda`, `/Plantoes`, `/Escalas` | Autenticada | tabelas responsivas/cards | Automatizado | Automatizado | Contrato de tabela | Requer storage state |
| `/Saude360`, `/Pacientes`, `/Agendamentos`, `/Triagem`, `/Consultas` | Autenticada | jornada clínica, forms | Automatizado | Automatizado | Contratos de forms/dialogs | Revisão clínica manual |
| `/Pagamentos`, `/Financeiro` | Autenticada | KPIs, tabelas, timeline | Automatizado | Automatizado | Contrato responsivo | Requer dados autorizados |
| `/Relatorios`, `/Configuracoes` | Autenticada | biblioteca/central, action cards | Automatizado | Automatizado | Rotas reais preservadas | Requer storage state |
