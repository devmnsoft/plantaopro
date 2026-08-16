# Funcionalidades abertas e fechadas — v1.76.0

| Tela/funcionalidade | Rota/controller | Backend/view/JS | Estado nesta PR | Motivo/prioridade |
|---|---|---|---|---|
| Login | `/Account/Login` / Account | Sim/sim/auth-login.js | Ativa e refinada | Alta; submit e acessibilidade reais |
| Cadastro | `/cadastro/empresa` / Cadastro | Sim/sim/form-experience.js | Ativa | Alta |
| Dashboard por perfil | `/Home/Dashboard` / Home | Sim/sim/não exige JS | Fechada | CTAs reais condicionados ao perfil |
| Admin SaaS | `/AdminSaas/Index` | Sim/sim | Ativa para Admin | Permissão do usuário |
| Minha Central/Meu Dia/Agenda | rotas homônimas | Sim/sim | Ativas | Dados do backend |
| Agendamentos/Pacientes/Triagem/Consultas | rotas homônimas | Sim/sim | Ativas conforme vínculo | Sem inventar vínculo clínico |
| Faturamento/Financeiro/Pagamentos | rotas homônimas | Sim/sim | Ativas conforme dado | Ausência permanece explícita |
| Plantões/Escalas | rotas homônimas | Sim/sim | Ativas conforme status | Ações dependem do registro |
| Relatórios | `/Relatorios` | View/controller existente | Limitada pelo endpoint disponível | Alta; não oferecer exportação fictícia |
| Notificações | drawer + API existente | Sim/sim/notification-drawer.js | Ativa | Contador e itens vêm da API |
| Command Palette | layout + catálogo | Rotas reais/JS | Ativa | Catálogo same-origin |
| Planos/Assinatura/Configurações | rotas homônimas | Sim/sim | Ativas conforme permissão | Dados do backend |
