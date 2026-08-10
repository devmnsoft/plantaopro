# Diagnóstico visual v1.51 — telas reais

## Método e runtime
Auditoria feita diretamente sobre Razor, CSS e JavaScript reais. Em 10/08/2026, `dotnet --info` retornou `dotnet: command not found`; portanto restore, build, testes e screenshots autenticados não podem ser declarados validados. Não foram criados dados demonstrativos ou contadores artificiais.

## Diagnóstico por superfície
| Superfície | Problema visual | Problema funcional aparente | Bootstrap cru / CSS duplicado | Ícones | Hero / KPI / ação / vazio | Responsividade | Correção v1.51 |
|---|---|---|---|---|---|---|---|
| `_Layout` | Fundo plano e composição fragmentada | Nenhum aparente | Imports históricos sobrepostos | Sistema `app-icon` preservado | Cabeçalho contextual já existe | Conteúdo podia encostar na navegação móvel | Nova camada canônica de acabamento e respiro mobile |
| `_AppSidebar` | Boa base, porém versão antiga e suporte sem grupo | Dashboard e LGPD pouco descobríveis | Dependência visual residual de utilitários | `app-icon` consistente | Busca e tenant existentes | Drawer existente preservado | Gradiente navy/teal, estados ativos, Dashboard, suporte/LGPD e versão 1.51 |
| `_AppTopbar` | Hierarquia correta, mas pouco contraste do shell | Ações variam por permissão | Utilitários Bootstrap ainda pontuais | Consistente | Busca, ajuda, plano e tenant reais | Controles são progressivamente ocultos | Vidro elevado, borda e título executivo |
| `_MobileNavigation` | Apenas três destinos, mas adequado a polegar | Menu completo acessível pelo botão central | Sem duplicação crítica | Consistente | Ação central clara | Alvos de toque mantidos | Espaço inferior do conteúdo e shell mais robusto |
| `_WorkspaceHeader` | Superfície clara pouco diferenciada | Nenhum aparente | Regras anteriores duplicam header | Consistente | Contexto e CTA já suportados | Empilhamento já existente | Passa a compartilhar elevação e tokens v1.51 |
| Login | Narrativa em lista e fundo pouco memorável | Toggle acessível já existente | `alert` e `btn-outline-secondary` crus | Consistente | Sem KPIs (não aplicável), ação forte | Benefícios desapareciam abruptamente | Fundo ambiente, narrativa SaaS, benefícios em cards, aviso e segurança próprios |
| Minha Central | Estrutura kanban e dados reais já madura | Drawer depende da API real | Classes de página coexistem com sistema | Bootstrap Icons legado | Hero, resumo, prioridade e vazio presentes | Cards móveis já previstos | Harmonizada pela nova identidade de shell e componentes |
| Meu Dia | Boa hierarquia v1.49, excesso de superfícies semelhantes | Drawer real preservado | CSS de página necessário | Bootstrap Icons legado | Spotlight, KPIs, timeline e vazios presentes | Layout dedicado existente | Contraste global, elevação e alvos refinados |
| Dashboard | Página contextual ainda dispersa | Sem falha evidente | Alguns cards Bootstrap | Misto | Resumos reais; vazio depende da fonte | Grid responsivo | Navegação explícita e superfície global consolidada |
| Agenda | Cards com `shadow-sm`; drawer mostrava só especialidade/período | Contexto insuficiente | `card border-0 shadow-sm`, outline secundário | Bootstrap Icons legado | Hero e ação existentes; KPI limitado ao total real | Drawer e cards responsivos | `pp-page`, hero, filtro/data-card, hospital, valor e cobertura reais no drawer |
| Plantões | Já evoluído, ainda cercado pelo shell antigo | Ações dependem de permissão/detalhe | Tabela Bootstrap sob wrapper próprio | Misto | Hero introdutório, KPIs e vazio reais | Tabela + cards móveis | Componentes e shell v1.51 aplicados sem inventar indicadores |
| Escalas | Tabela operacional densa | Timeline fica no detalhe | Bootstrap residual | Misto | Contexto real, sem KPI inventado | Overflow tratado | Tokens, tabela, foco e superfícies unificados |
| Pendências / Minha Central | Entrada é a central baseada em `work_items`, não há view `Pendencias/Index` local | Rota precisa ser confirmada em runtime | Kanban possui CSS dedicado | Bootstrap Icons | Prioridades, responsáveis, drawer e vazio presentes | Kanban responsivo existente | Documentada a implementação real e preservado acesso na sidebar |
| Saúde 360 | Jornada já existe mas perde força em telas estreitas | Agregação depende do view model real | CSS dedicado justificável | Ícones de jornada | Etapas e vazios reais | Scroll/empilhamento dedicado | Shell, contraste, espaçamento e touch target v1.51 |
| Pacientes | Listagem ainda alterna padrões antigos | Dados e LGPD dependem da API | Cards/tabela legados | Misto | Busca e vazio presentes | Tabela com overflow | Identidade global e foco consistente |
| Agendamentos | Muitas visões com estilos diferentes | Ações dependem do estado real | Bootstrap residual | Misto | CTAs/empty states por view | Varia por visão | Superfícies, forms e navegação consolidados |
| Triagem | Alta densidade clínica | Tempos dependem do backend | Bootstrap residual | Misto | Fila/risco reais | Overflow tratado | Semântica de status, contraste e superfícies globais |
| Consultas | Formulário longo | Jornada depende do paciente selecionado | Forms Bootstrap | Misto | Contexto real, sem número fake | Ações sticky existentes | Inputs, foco e elevação unificados |
| Convites | Redesign anterior ainda usa ícones legados | Seleção depende de plantões reais | CSS de página | Bootstrap Icons | KPIs/empty state reais | Cards móveis | Absorvido pelo novo shell/tokens sem alterar dados |
| Pagamentos / Financeiro | Informação monetária compete com metadados | Timeline depende do detalhe real | Tabela residual | Misto | KPIs e vazio reais | Lista móvel existente | Gradiente financeiro, cards e tipografia monetária consistentes |
| Relatórios | Biblioteca fixa parece promessa de funcionalidade | Exportações bloqueadas | `card shadow-sm`, alert cru | Ausentes | Sem dados agregados; não se devem inventar KPIs | Duas colunas | Identidade global aplicada; evolução funcional fica pendente de endpoints auditados |
| Configurações | Apenas conta/sistema, não uma landing completa | Grupos administrativos dependem de permissões e rotas | Grid/card Bootstrap | Bootstrap Icons | Sem hero/KPI; vazio real | Grid existente | Superfícies globais melhoradas; expansão funcional pendente para evitar links falsos |

## Decisões
- A nova camada `v151-product-experience.css` é importada por último e usa tokens existentes, evitando editar dezenas de regras minificadas de legado.
- Classes reutilizáveis solicitadas (`pp-page`, `pp-shell`, `pp-hero`, grids, cards, filtros, badges, drawer, timeline, stepper, section header e lista móvel) são consolidadas; as aplicadas nesta rodada estão presentes em Agenda e workspaces existentes.
- Nenhum número, paciente, médico, status ou valor foi criado. O drawer da Agenda só lê atributos produzidos pelo view model atual.
