# PlantãoPro — fase 2 de fluxos funcionais SaaS

## Implementado nesta rodada

- Inclusão do painel operacional compartilhado da fase 2 para Portal Cliente, Central de Escala, Área do Médico, Financeiro e Portal Parceiro.
- Inclusão de KPIs por perfil, fila de trabalho, etapas do fluxo, botões com navegação real e modal para ações críticas.
- Inclusão dos componentes reutilizáveis de bloqueio por plano, CTA de upgrade e limite atingido.
- Inclusão da API `api/fase2/fluxos/{area}` para expor resumo funcional por área em envelope `ApiResponse<T>`.
- Inclusão da API `api/fase2/fluxos/acao` para validar e auditar ações críticas com justificativa.
- Ajustes de rotas Web para tornar funcionais páginas antes concentradas em dashboards demonstrativos.

## Pendências reais

- Persistir todos os eventos de fila em tabelas operacionais específicas, em vez de dados demonstrativos de resumo.
- Ligar cada ação crítica do painel compartilhado aos services transacionais já existentes de plantões, convites, escalas, pagamentos e billing.
- Executar build em ambiente com SDK .NET disponível.
- Executar QA ponta a ponta com banco PostgreSQL e usuários reais de cada perfil.
