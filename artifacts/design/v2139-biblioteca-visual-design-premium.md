# PlantãoPro v2.13.9 — biblioteca visual e design premium

## Escopo e pré-validação

A rodada foi limitada ao frontend, views e documentação porque o ambiente não contém o SDK .NET. Nenhum arquivo de backend funcional, banco, migration, projeto ou solução foi alterado. A autenticação existente foi auditada visualmente: o formulário mantém antiforgery, validação, recuperação de senha, feedback de demora e submissão protegida, sem bypass ou credenciais de demonstração.

## Matriz de auditoria

| Tela | Módulo | Perfil permitido | Problema / evolução | Formulário e mensagens | Loading / vazio / erro | Ajuda | Responsividade | Prioridade |
|---|---|---|---|---|---|---|---|---|
| Login | Acesso | Público autorizado | Hierarquia e confiança consolidadas; versão atualizada | Labels, validação inline, resumo e estado de envio | Atraso e erro acessíveis | Ajuda de acesso seguro | Painéis adaptativos | Crítica |
| Dashboard / Meu Dia | Operação | Conforme perfil | Cabeçalhos e cards unificados | Toasts próprios | Skeleton/empty/error compartilhados | Guia global contextual | Grids fluidos | Alta |
| Central Global MNSOFT | SaaS global | Super Admin MNSOFT | Atalhos globais e contexto auditado explícitos | Callout de proteção | Estados compartilhados | Guia específico | 3/2/1 colunas | Crítica |
| Clientes / Tenants | SaaS global | Super Admin MNSOFT | Contexto global inequívoco | Filtros e mensagens do design system | Vazio/erro sem dados fake | Guia Clientes/AdminSaas | Tabelas roláveis | Crítica |
| Usuários | Acessos | Admin tenant / global | Estado real sem usuários fictícios | Validação e perfis relacionados | Empty state explícito | Guia Usuários | Ação empilhável | Alta |
| Perfis / Permissões | Segurança | Admin autorizado | Hierarquia e cuidado LGPD | Formulários aprimorados globalmente | Estados compartilhados | Guia Perfis | Matriz rolável | Alta |
| Médicos | Cadastros | Gestor autorizado | Form e tabela consistentes | Máscaras e validação existentes | Estados compartilhados | Guia Médicos | Form grid fluido | Alta |
| Hospitais / Unidades | Cadastros | Gestor autorizado | Progresso sem CSS inline | Labels e feedback próprios | Loading real | Guia Hospitais | Grid adaptativo | Alta |
| Escalas | Operação | Coordenação | Cobertura e filtros priorizados | Confirmação acessível | Empty/error compartilhados | Guia Escalas | Calendário rolável | Alta |
| Plantões | Operação | Conforme permissão | Barra de cobertura declarativa | Form seccionado e validado | Empty/error reais | Guia Plantões | Cards/tabela mobile | Crítica |
| Financeiro | Financeiro | Financeiro / admin | KPIs e progresso consistentes | Filtros organizados | Vazio/erro próprios | Guia Financeiro | Resumo 3/1 colunas | Alta |
| Relatórios | Analytics | Perfil autorizado | Catálogo visual premium | Filtros e exportação consciente | Empty state | Guia Relatórios | Cards adaptativos | Média |
| Auditoria | Governança | Auditor / admin | Ênfase em rastreabilidade | Filtros seguros | Empty/error | Guia Auditoria | Tabela rolável | Crítica |
| Configurações | Plataforma | Admin / usuário | Escopo institucional explícito | Feedback de salvamento | Erro padronizado | Guia Configurações | Seções empilháveis | Alta |

## Componentes criados e evoluídos

- Camada `v2139-premium-library.css` com tokens preservando a paleta, acabamento de page hero, guia contextual, navegação global, utilitários sem estilo inline e breakpoints mobile.
- Inicializador `v2139-premium-library.js` para progresso declarativo e preview white-label com validação de cores.
- Renderizações dinâmicas de erros, loading, toasts, Meu Dia, ações rápidas, busca global e operações migradas de HTML textual para APIs DOM seguras.
- Componentes existentes consolidados no shell: cabeçalho, KPIs, cards, botões, forms, filtros, tabelas, badges, toast, modal, banners, empty/error states, skeleton, ajuda e tenant context.

## Super Admin e isolamento tenant

A Central Global ganhou acesso visual direto a clientes/tenants, usuários, perfis/permissões, cobranças, auditoria/logs e configurações globais. O badge informa **Modo Global MNSOFT**, **Acesso global auditado** e, havendo contexto selecionado, **Visualizando cliente: [nome]**. A mudança é estritamente de apresentação e não amplia autorização no cliente; rotas continuam protegidas pelas policies/roles existentes e o tenant comum mantém a descrição de escopo vinculada à sessão.

## Formulários, mensagens e ajuda

A experiência transversal preserva labels visíveis, marcadores obrigatórios, `aria-describedby`, validação inline, resumo focável, bloqueio durante envio, máscaras cadastradas e seletores reais. Feedback usa toast, painel, callout, modal/drawer existentes. O `_ScreenGuide` injeta “Como usar esta tela” em todas as páginas autenticadas, com finalidade, público, ação, cuidado e contexto global/tenant.

## QA e comandos executados

- `git status --short --branch`, `git remote -v`, `dotnet --info`, `dotnet --list-sdks` — branch inicial limpa; sem remote configurado e sem SDK .NET.
- Mapeamento com `find` e `rg` das views, layouts, páginas e assets.
- Busca obrigatória de antipadrões executada e ocorrências no escopo corrigidas.
- `git diff --check` executado.
- Screenshots não foram geradas: a aplicação não pôde ser compilada/iniciada sem SDK .NET e não há sessão real autorizada disponível. Nenhum dado fake ou bypass foi introduzido.

## Limitações restantes

Build, testes .NET e QA navegável desktop/mobile precisam ser executados em CI ou estação com SDK .NET 10 e fontes reais acessíveis. A ausência de remote neste checkout também impede rebase e publicação direta daqui; a alteração permanece pronta para o fluxo Git assim que `origin` estiver configurado.
