# v1.64 — diagnóstico visual por rota

## Limite desta auditoria

O contêiner não disponibiliza o SDK/runtime .NET (`dotnet: command not found`). Portanto, nenhuma rota foi apresentada em runtime e nenhuma screenshot foi fabricada. A revisão abaixo é **estática**, baseada nas views, no design system e nos contratos automatizados. A homologação visual permanece pendente até a execução do smoke conforme `v164-smoke-visual-instrucoes.md`.

## Matriz de rotas

| Rota | Runtime | Screenshot | Diagnóstico estático (layout / formulário / card-tabela / overlay) | Correção aplicada | Pendência real |
|---|---|---|---|---|---|
| `/` | indisponível | não | Hero e grid comercial já estruturados; escala do título e altura dos cards podiam competir com o conteúdo. | Hero mais proporcional, promessa comercial, cards semânticos e alturas coerentes. | Conferir copy dinâmica e dobra nos 7 viewports. |
| `/Account/Login` | indisponível | não | Contrato `pp-auth-*`, labels, erros e banner presentes; headline e shell altos para telas compactas. | Headline controlada, shell menor, texto com contraste e card limitado a 30rem. | Validar teclado móvel, autofill e mensagens server-side. |
| `/AdminSaas/Index` | indisponível | não | `pp-admin-layout`, KPIs e checklist presentes; risco de compressão entre 992–1199px. | KPIs com altura/ritmo previsíveis; layout lateral já colapsa em 1199px. | Validar conteúdo real e sticky review. |
| `/Home/Dashboard` | indisponível | não | Raiz premium, KPIs e alternativa mobile protegidos pelos gates. | Escala compartilhada de page hero, KPI e overflow. | Conferir gráficos e estados reais. |
| `/MinhaCentral` | indisponível | não | Composição operacional e drawer acessível existentes. | Proteções globais de largura e hero. | Exercitar foco, loading, erro e ações autenticadas. |
| `/MeuDia` | indisponível | não | Estrutura de página crítica verificada estaticamente. | Ritmo de página e hero unificados. | Conferir densidade da agenda real. |
| `/Agenda` | indisponível | não | Tabelas exigem wrapper/card mobile pelo gate. | Wrapper recebe scroll contido e raio consistente. | Exercitar intervalos e calendário. |
| `/Plantoes` | indisponível | não | Introdução, cards mobile e drawer real presentes. | Tabelas, botões de hero e drawer cobertos pelo smoke. | Abrir detalhes em todos os viewports. |
| `/Escalas` | indisponível | não | Introdução, tabela responsiva e drawer real presentes. | Escala compartilhada e tabela contida. | Validar colunas com dados extensos. |
| `/Saude360` | indisponível | não | Jornada assistencial e composição clínica verificadas. | Larguras mínimas seguras e tipografia compartilhada. | Conferir timeline e formulário real. |
| `/Pacientes` | indisponível | não | Página crítica e responsividade de tabela verificadas. | Contrato de tabela incluído no smoke v1.64. | Validar nomes longos e empty state. |
| `/Agendamentos` | indisponível | não | Ações reais e modal acessível preservados. | Smoke passa a verificar tabelas e altura de cards. | Exercitar confirmação, erro e loading. |
| `/Triagem` | indisponível | não | Formulário tem limites clínicos server/client; layout clínico existente. | Campos herdam largura e grid responsivo. | Validar erros clínicos e teclado móvel. |
| `/Consultas` | indisponível | não | Página crítica sem placeholders/API nativa pelo gate. | Page rhythm e tabela responsiva compartilhados. | Conferir atendimento com dados reais. |
| `/Pagamentos` | indisponível | não | Página crítica protegida pelo gate operacional. | Cards e conteúdo recebem mínimos seguros. | Validar valores reais sem quebra. |
| `/Financeiro` | indisponível | não | Composição de página e tabela mobile verificadas. | Hierarquia de hero/KPI e overflow unificada. | Conferir moeda, filtros e ações. |
| `/Relatorios` | indisponível | não | Página crítica padronizada estaticamente. | Hero e grids fluidos compartilhados. | Validar gráficos e exportação reais. |
| `/Configuracoes` | indisponível | não | `pp-page` e action cards presentes. | Cards e ações mobile padronizados. | Conferir formulários por permissão. |

## Resultado honesto

A auditoria não atribui “aprovado visual” a nenhuma rota sem pixels reais. Os gates estáticos aprovam a composição e o smoke está pronto para produzir 133 capturas (19 rotas × 7 viewports) quando houver runtime e sessão autenticada.
