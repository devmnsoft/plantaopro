# PlantãoPro v1.66 — auditoria visual por rota

## Escopo e método

Auditoria estática realizada em 13/08/2026 sobre as views Razor, contratos `pp-*`, CSS responsivo e smoke Playwright. O runtime não pôde ser iniciado porque o SDK .NET não está instalado no ambiente; portanto, nenhuma captura foi simulada e a validação navegada permanece como pendência real. “Aceitável” indica contrato completo após o polimento, sujeito à conferência visual local.

| Rota | Status | Layout | Hierarquia | Espaçamento | Formulário | Cards/KPIs | Tabela | Drawer/modal | Mobile | Correção aplicada | Pendência real |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `/` | Aceitável | Hero e grid públicos | Título limitado | Ritmo vertical reduzido | N/A | Cards equalizados | N/A | N/A | Uma coluna | Bordas, sombra e tipografia refinadas; contrato da landing no smoke | Captura/runtime |
| `/Account/Login` | Aceitável | Painéis proporcionais | Headline moderada | Ritmo do form regular | `pp-form`, ajuda e erros | Benefícios compactos | N/A | N/A | Coluna, ações sem colisão | Cards de benefício, largura e breakpoint 575 px | Captura/runtime |
| `/cadastro/empresa` | Aceitável | Wizard e resumo | Stepper explícito | Seções em cards | Grid, labels, ajuda e validação | Resumo real do plano | N/A | Feedback final | Campos e ações empilham | Contrato self-service preservado e smoke dedicado | Captura/runtime |
| `/AdminSaas/Index` | Aceitável | Cockpit em 2 colunas | Hero executivo compacto | Grid uniforme | N/A | KPIs e ações fluidos | N/A | N/A | Painel lateral empilha | Cards, hero e atalhos refinados | Captura autenticada |
| `/Home/Dashboard` | Aceitável | Container e hero | KPIs antes das ações | Gaps do sistema | N/A | Altura coerente | Alternativa mobile | Drawer global | Grid colapsa | Superfícies globais refinadas | Captura autenticada |
| `/MinhaCentral` | Aceitável | Workspace operacional | Prioridade explícita | Colunas controladas | Filtros acessíveis | Work items reais | N/A | Dialog acessível | Drawer full-screen | Gate reforça semântica dos drawers | Captura autenticada |
| `/MeuDia` | Aceitável | Página operacional | Resumo antes da agenda | Ritmo compacto | Filtros existentes | Cards responsivos | N/A | Overlay global | Uma coluna | Contrato `pp-page`/container verificado | Captura autenticada |
| `/Agenda` | Aceitável | Agenda no container | Ações contextuais | Contenção horizontal | Filtros existentes | Cards móveis | Wrapper responsivo | Confirmação acessível | Alternativa mobile | Smoke testa overflow e ações visíveis | Captura autenticada |
| `/Plantoes` | Aceitável | Introdução e conteúdo | CTA contextual | Seções consistentes | Form padronizado | Cards móveis | Wrapper responsivo | Detail drawer | Full-screen | Gate de formulário/tabela/drawer | Captura autenticada |
| `/Escalas` | Aceitável | Introdução e conteúdo | Filtros legíveis | Grid consistente | Form padronizado | Ações agrupadas | Wrapper responsivo | Detail drawer | Full-screen | Gate de tabela/drawer | Captura autenticada |
| `/Saude360` | Aceitável | Jornada clínica | Etapas ordenadas | Fluxo contínuo | Limites e erros | Módulos coerentes | Quando aplicável | Overlay global | Etapas empilham | Jornada real preservada | Captura autenticada |
| `/Pacientes` | Aceitável | Página clínica | Busca e CTA claros | Card por seção | `pp-form` no CRUD | Empty state real | Responsiva/mobile | Detail drawer global | Sem overflow | Gate operacional e de form | Captura autenticada |
| `/Agendamentos` | Aceitável | Página clínica | Contexto de recepção | Filtros agrupados | `pp-form` no CRUD | Alternativa mobile | Responsiva/mobile | Confirmação dialog | Sem overflow | Smoke e gate operacional | Captura autenticada |
| `/Triagem` | Aceitável | Jornada Saúde 360 | Risco e dados clínicos | Campos agrupados | Limites, ajuda e erros | Cards clínicos | N/A | Feedback acessível | Grid empilha | Validação estática clínica preservada | Captura autenticada |
| `/Consultas` | Aceitável | Página clínica | Ação de atendimento | Seções claras | Form padronizado | Empty state real | Responsiva/mobile | Drawer global | Sem overflow | Gate operacional | Captura autenticada |
| `/Pagamentos` | Aceitável | Página financeira | Status antes das ações | Ritmo compacto | Form padronizado | Dados reais | Responsiva/mobile | Overlay global | Sem overflow | Gate operacional | Captura autenticada |
| `/Financeiro` | Aceitável | Página financeira | Resumo e detalhe | Grid consistente | Filtros existentes | KPIs fluidos | Responsiva/mobile | Detail drawer | Uma coluna | Estilos de KPI/tabela refinados | Captura autenticada |
| `/Relatorios` | Aceitável | Página de consulta | Filtros antes do resultado | Seções controladas | Grid responsivo | Empty state real | Responsiva/mobile | N/A | Ações empilham | Gate operacional | Captura autenticada |
| `/Configuracoes` | Aceitável | Hub de ações | Categorias explícitas | Cards equilibrados | Forms internos padronizados | Action cards | N/A | Overlay global | Uma coluna | Action cards e ações refinados | Captura autenticada |

## Resultado

Nenhuma rota foi mantida como **Quebrada** ou **Precisa ajuste** na análise estática. As 19 rotas continuam sujeitas à homologação visual real nas sete dimensões; o comando e os requisitos estão em `v166-smoke-visual-instrucoes.md`.
