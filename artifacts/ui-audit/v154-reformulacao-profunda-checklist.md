# Checklist de entrega visual v1.54

| Área | Estado | Evidência / decisão |
|---|---|---|
| Login | Concluído | Shell 55/45, controles full-width, botão estável, Caps Lock e erros acessíveis. |
| Topbar | Concluído | Breadcrumb compacto, prioridades responsivas e título truncado com segurança. |
| Sidebar | Concluído | Dimensão canônica de 280px, shell responsivo e navegação normalizada. |
| AdminSaaS | Concluído no design system | Shell, cards, grids e hierarquia deixam de depender de listas cruas. |
| Planos | Concluído no design system | Superfícies, grids responsivos, contraste e CTAs usam tokens canônicos. |
| Cadastro em etapas | Concluído no design system | Cards de seção, etapa, grid e ações aderentes disponíveis para todos os fluxos. |
| Formulários | Concluído | `pp-form-*` define página, card, seção, grid, campo, controle, ajuda e ações. |
| Validações | Concluído | Erro textual, ícone, borda, summary e `aria-describedby` no login. |
| Toasts | Concluído | Região viva e variantes success/error/warning/info. |
| Modais | Concluído | Diálogo semântico, loading e variante destrutiva `pp-delete-dialog`. |
| Tabelas | Concluído no design system | Tokens, superfícies e responsividade preservam o componente de tabela existente. |
| Mobile | Concluído | Breakpoints do shell e formulários cobrem 360–1024px sem larguras fixas. |

## Pendência de ambiente

O SDK .NET não está instalado no container; restore, build, testes e captura autenticada em runtime não podem ser produzidos localmente. As verificações estáticas, JavaScript e mobile são executadas separadamente.
