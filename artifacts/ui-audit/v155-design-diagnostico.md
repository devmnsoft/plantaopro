# Diagnóstico visual PlantãoPro v1.55

## Escopo e método

Auditoria estática realizada no shell compartilhado, autenticação e grupos `AdminSaas`, `B2BLaunch`, `MinhaAssinatura`, `Planos`, `Onboarding`, `Plantoes`, `Pacientes`, `Agendamentos`, `Triagem`, `Consultas`, `Configuracoes` e `Relatorios`. A análise considerou estrutura Razor, cascata CSS, semântica, feedback, contraste e comportamento nos breakpoints de 360, 390, 430, 768, 1024, 1366 e 1920 px. Dados exibidos continuam vindo dos modelos existentes; nenhum mock foi introduzido.

## Shell compartilhado

| Grupo | Problema visual | Problema estrutural / CSS | Responsividade e contraste | Correção aplicada |
|---|---|---|---|---|
| Layout e footer | Nomes legados concorrentes tornavam a hierarquia ambígua e o rodapé dependia de seletores indiretos. | Shell misturava `pp-app`, `app-shell`, `pp-main` e `app-main`. | Conteúdo podia perder largura mínima e o footer não tinha contrato explícito. | Estrutura `pp-app-shell > pp-sidebar + pp-main-shell > topbar + content + pp-footer`, coluna flexível e conteúdo com padding fluido. |
| Topbar e breadcrumb | Contexto, busca, plano e ações comprimiam o título. Breadcrumb podia herdar numeração. | Lista ordenada não zerava marcadores no contrato final. | Ações secundárias são progressivamente ocultadas, preservando título, notificação e usuário. | Reset local de lista, título truncável, grupos flexíveis e topbar sticky com superfície de alto contraste. |
| User menu | Dependência visual de dropdown Bootstrap podia exibir bullets ou links soltos. | `ul/li`, `data-bs-toggle` e estilos externos controlavam abertura. | Menu podia ultrapassar 430 px e não havia navegação por setas no componente. | Dropdown próprio sem lista, contexto real, Escape, clique externo, retorno de foco, setas e largura limitada ao viewport. |
| Sidebar | Versão desatualizada e largura distribuída entre aliases. | O shell não declarava a sidebar pelo nome canônico. | Drawer desktop/mobile precisava manter largura estável. | Classe `pp-sidebar`, largura única de 17 rem, shell colapsável e versão discreta v1.55. |
| Mobile/footer | Navegação fixa podia disputar espaço com o conteúdo. | Espaço inferior não estava no contrato final. | Risco maior em 360–430 px. | `pp-content` reserva 6,5 rem no mobile; footer usa `margin-top:auto`. |

## Autenticação e formulários

| Grupo | Problema visual/estrutural | Classes cruas ou conflito | Formulário/feedback | Correção aplicada |
|---|---|---|---|---|
| Login | Story podia dominar a tela e logo tinha dimensão nominal excessiva. | Regras duplicadas no agregador `plantaopro.css` repetiam imports e uma camada antiga de autenticação. | Botão podia quebrar; aviso de Caps Lock dependia somente de classe utilitária. | Imports consolidados, shell equilibrado, logo limitado, benefícios legíveis, botão sem quebra, warning âmbar textual e loading já preservado. |
| Recuperação e reset | Precisavam herdar a mesma superfície e ritmo vertical. | Cascata anterior podia sobrescrever painel e campos. | Labels/ajuda/erro necessitam distância constante. | Contrato global `pp-form-field`, label forte, controle com 46 px, ajuda e erro separados. |
| Plantões/Pacientes/Agendamentos | Form partials já usam componentes, mas gaps variavam por página. | Utilitários Bootstrap ainda aparecem em telas não críticas; não foram adicionados novos. | Risco de duas colunas apertadas no celular. | Grid definitivo de duas colunas e uma coluna abaixo de 768 px; ações empilham no mobile. |
| Triagem/Consultas/Configurações | Alta densidade clínica exige alinhamento previsível. | Seções antigas coexistem com o design system. | Validação não pode depender apenas de vermelho. | Seção com padding fluido, divisória sem `hr`, área reservada para erro textual e foco mantido pelo sistema existente. |

## SaaS, planos e onboarding

| Grupo | Problema visual | Problema estrutural | Responsividade/feedback | Correção aplicada |
|---|---|---|---|---|
| Admin SaaS | Checklist precisava se comportar como painel, não lista operacional crua. | A tela já contém `pp-checklist-grid/card`, mas dependia do contrato v1.54. | Cards devem refluír sem sobreposição. | Gate v1.55 preserva hero e cards reais e impede regressão estrutural. |
| B2B Launch | Hero e ações poderiam perder ritmo em larguras intermediárias. | Componentes existentes foram auditados (`pp-page-hero`, grid e action card). | CTAs continuam reais e refluem pelo sistema responsivo. | Contrato SaaS verificado pelo script atualizado. |
| Planos/Minha assinatura | Cards poderiam esticar de modo desigual, preço perder contraste e ações flutuar. | Grid e card já estavam sem mock, alimentados por `Model.Planos.Items`. | Sobreposição em telas estreitas. | `auto-fit/minmax`, cards flexíveis, ações no rodapé, preço navy e coluna mínima limitada a 100%. |
| Onboarding | Formulário é extenso; stepper e resumo precisam permanecer legíveis. | Wizard existente possui cinco etapas, grid e resumo real. | Labels colados e ações apertadas no mobile. | Espaçamento unificado, grid responsivo, cards/seções e gate dos campos/erros associados. |

## Feedback, tabelas e demais jornadas

- `_ConfirmModal` mantém diálogo acessível, sem `confirm()` nativo; `_ToastRegion` mantém `aria-live`.
- Os grupos MinhaAssinatura, Plantoes, Pacientes, Agendamentos, Triagem, Consultas, Configuracoes e Relatorios foram auditados quanto a placeholders, botões sem tipo e APIs nativas pelos gates do repositório.
- Tabelas e cards continuam usando os componentes do design system; a nova camada não cria seletores genéricos nem `!important`.
- Cores finais usam navy, azul clínico, teal, off-white e vermelho textual, mantendo distinção além da cor por ícones, títulos e mensagens.
