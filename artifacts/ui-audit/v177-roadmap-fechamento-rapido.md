# Roadmap de fechamento rápido — v1.77.0

| Módulo | Status atual | Pendência crítica | Regra de negócio faltante | Ação de fechamento rápido | Arquivos envolvidos | Prioridade | Status após esta PR |
|---|---|---|---|---|---|---|---|
| Build/runtime/controllers | Estático consistente | SDK e runtime indisponíveis | Transições devem ser homologadas no servidor | Validar no Windows/VS | solução, controllers, testes | P0 | Bloqueado pelo ambiente; roteiro criado |
| Shell e rotas | Rotas críticas catalogadas | Homologação autenticada | Perfil não substitui autorização | Smoke v177 e gates | Shared, JS, smoke | P0 | Contrato atualizado |
| Dashboard por perfil | Dados reais, mas médico era redirecionado | Todos os perfis precisam de foco próprio | Ausência não pode virar KPI zero | Manter dashboard e apresentar prioridade do perfil | HomeController, Dashboard, CSS | P1 | Fechado estaticamente |
| Login/cadastro | Forms reais e acessíveis | Runtime comercial | Submit só em endpoint real | Preservar gate de forms | Account, Cadastro | P1 | Sem regressão estática |
| Jornada clínica | Rotas parciais e ações condicionadas | Homologar status/vínculos | Conduta, risco e IDs reais | Manter indisponível sem vínculo | Agendamentos, Triagem, Consultas | P1 | Regras registradas; runtime pendente |
| Jornada operacional | Parcial | Transições e motivos | Cancelar/substituir/devolver exigem motivo | Homologar endpoints existentes | Plantões, Escalas, Fechamentos | P1 | Pendente de runtime/backend |
| Jornada financeira | Origem real preservada | Homologar geração e pagamento | Ausente não vira zero/pago | Validar encadeamento real | Faturamento, Financeiro, Pagamentos | P1 | Estado honesto preservado |
| Admin SaaS/assinatura | Rotas reais | Policies por perfil | Uso/plano só da API | Homologar por tenant | AdminSaas, Planos, MinhaAssinatura | P2 | Próxima rodada |
| Relatórios/notificações | Parcial | Exportações não contratadas | Sem endpoint, ação indisponível | Fechar endpoints antes dos CTAs | Relatórios, drawer, palette | P2 | Registrado |
| Mobile/polimento | Contrato em 8 viewports | Smoke visual não executado | Tabelas e overlays responsivos | Executar screenshots em runtime | CSS, views, smoke | P3 | CSS aditivo; homologação pendente |
| Artefatos | Inventário v176 | Consolidar decisão | Rastreabilidade | Publicar matrizes v177 | artifacts/ui-audit | P4 | Fechado |
