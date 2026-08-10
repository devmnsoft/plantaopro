# Checklist de entrega v1.51

## Identidade e componentes
- [x] Navy profundo, azul, ciano e teal aplicados ao shell e autenticação.
- [x] Fundo off-white, superfícies elevadas, bordas e sombras normalizados por tokens.
- [x] Componentes `pp-page`, `pp-shell`, `pp-hero`, `pp-hero-actions`, `pp-kpi-grid`, `pp-command-card`, `pp-action-card`, `pp-filter-panel`, `pp-data-table`, `pp-data-card`, `pp-status-badge`, `pp-priority-badge`, `pp-empty-state`, `pp-detail-drawer`, `pp-timeline`, `pp-stepper`, `pp-section-header` e `pp-mobile-card-list` consolidados.
- [x] Agenda usa componentes do produto no hero, filtro e agrupamentos.
- [x] Nenhum mock operacional adicionado.

## Telas e navegação
- [x] Login com narrativa comercial, benefícios em cards, segurança e aviso de demo próprio.
- [x] Sidebar com Dashboard, grupo de suporte, LGPD e identificação v1.51.
- [x] Topbar com acabamento translúcido e hierarquia reforçada.
- [x] Agenda com drawer de hospital, especialidade, período, valor, cobertura, status e CTA para o plantão.
- [x] Plantões, Minha Central, Meu Dia e Saúde 360 preservam dados reais e empty states existentes.
- [ ] Expansão funcional de Configurações e Relatórios aguarda rotas/endpoints reais; links ou métricas não foram simulados.

## Acessibilidade e mobile
- [x] Foco visível global e contraste semântico preservados.
- [x] Drawer da Agenda mantém `role=dialog`, `aria-modal`, Escape, foco inicial e retorno ao gatilho.
- [x] Touch targets mínimos e conteúdo com espaço para a navegação inferior.
- [x] Breakpoints revisados para 360, 390, 430, 768 e 1024 px por análise CSS.
- [x] Movimento desativável por `prefers-reduced-motion`.

## Validação
- [x] JavaScript validado sintaticamente com `node --check`.
- [x] Scripts estáticos do repositório executados (resultado detalhado no PR).
- [ ] Runtime/screenshots: pendentes porque o SDK `dotnet` não existe no ambiente (`dotnet: command not found`).
