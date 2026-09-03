# PlantãoPro v2.13.7 — refino de design, UX global e Super Admin

## Escopo e pré-validação

- Branch de trabalho: `codex/v2137-refino-design-ux-global-superadmin`.
- O repositório não possui remote configurado neste ambiente.
- O SDK .NET não está instalado (`dotnet: command not found`). Em respeito ao escopo, esta rodada alterou somente Views, CSS, JavaScript de apresentação e este documento; controllers, services, DTOs, autenticação server-side, banco e migrations não foram modificados.
- Sem runtime .NET, não foi possível validar login, autorização ou consultas ponta a ponta. A inspeção do login confirmou POST MVC real, antiforgery, proteção contra duplo envio, restauração após `pageshow` e timeout que devolve controle sem ocultar falhas.

## Matriz de revisão visual

O inventário abrangeu as Views reais em `PlantaoPro.Web/Views`, seus componentes compartilhados e assets. A matriz agrupa telas com a mesma jornada; estados marcados como **compartilhados** são fornecidos pelo AppShell/design system, e não representam dados artificiais.

| Tela / conjunto real | Módulo | Perfil / escopo | Problema visual | Problema de uso | Formulário | Mensagens | Vazio / loading / erro | Ajuda | Responsividade | Ação v2.13.7 |
|---|---|---|---|---|---|---|---|---|---|---|
| Account/Login, ForgotPassword, ResetPassword | Acesso | Público | versão desatualizada; hierarquia acumulada | risco de botão permanecer ocupado | Login e recuperação | resumo seguro e live region | loading e erro presentes | ajuda de segurança presente | layout dedicado | versão atualizada; fluxo POST preservado |
| MinhaCentral, MeuDia, Home/Dashboard | Visão geral | Tenant / perfil | contexto pouco explícito | global e instituição podiam parecer equivalentes | filtros pontuais | toasts compartilhados | estados por tela | guia compartilhado | desktop/mobile | badge agora declara “Área da instituição” |
| AdminSaas/Dashboard, AdminSaas/Index | SaaS global | Super Admin | nomenclatura genérica | alcance global pouco evidente | ações por rotas reais | aviso de governança | indisponível é declarado, sem mock | ajuda global reforçada | grids responsivos | criada identidade “Central Global MNSOFT” e aviso auditável |
| Clientes, Onboarding, Assinaturas, Planos | Clientes / comercial | Super Admin | ações sensíveis densas | confirmar tenant e impacto | cadastro e plano | confirmação própria existente | vazio real e erros do servidor | presente | tabela responsiva | acesso global destacado no shell |
| Usuarios, Perfis, Permissoes | Acessos | Admin do tenant / Super Admin | ícones repetitivos no menu | delimitação do tenant dependia do texto | cadastros e matriz | feedback compartilhado | usuário sem fonte real explicitado | guia contextual | estados responsivos | grupo Gestão mantido isolado por role; Central Global oferece entrada clara |
| Medicos, Hospitais, Especialidades | Cadastros | Gestor autorizado | CRUD heterogêneo | vínculos precisam de seleção, não ID | forms parciais | validação compartilhada | empty/error parciais | guia compartilhado | formulários em grid | tokens de labels, inputs e foco uniformizados |
| Escalas, Plantoes, Cobertura, Convites | Operação | Operação / coordenação | muitos controles em telas densas | prioridade e contexto precisam permanecer visíveis | filtros, criação, substituição | erro e status próprios | todos os estados reais | guia compartilhado | lista/cards/kanban | hierarquia e contraste globais reforçados |
| Financeiro, Pagamentos, Caixa, FaturamentoSaas | Financeiro | Financeiro / admin | tabelas e KPIs variados | SaaS e tenant devem ser distinguíveis | filtros e lançamentos | feedback compartilhado | empty/error por tela | guia compartilhado | tabela/card | Cobranças SaaS passa a existir somente no menu global |
| Relatorios, Bi | Inteligência | Gestor autorizado | densidade de filtros | exportação exige cuidado LGPD | filtros | avisos da fonte | vazio/erro reais | guia compartilhado | painéis responsivos | contexto e contraste consistentes |
| Auditoria, Observabilidade, Lgpd | Governança | Perfis expressamente autorizados | acessos globais dispersos | trilha e logs difíceis de descobrir | filtros | callouts/estados | estados reais | guia contextual | tabelas responsivas | Auditoria e Logs e saúde reunidos no grupo global |
| Configuracoes, Parametrizacoes, WhiteLabel | Configuração | Admin autorizado | previews usam estilos dinâmicos necessários | alteração pode impactar instituição | seções configuráveis | validação | estado conforme fonte | guia compartilhado | grids responsivos | mantém escopo existente; tokens base sem sobrescrever marca do cliente |
| Pendencias | Central de Ações | Usuário / equipe autorizada | prompt nativo incoerente | adiamento sem contexto e validação inline | filtros e adiar | toast e erro inline | vazio/error existentes | guia compartilhado | lista responsiva | `prompt()` substituído por dialog acessível com `datetime-local` |
| Notificacoes, Comunicacao | Comunicação | Destinatário no tenant | densidade variável | ações precisam de resultado humano | preferências/conversa | toast compartilhado | estados por tela | guia compartilhado | layouts adaptáveis | tokens globais aplicados sem mudar fonte de dados |
| Saúde 360: Pacientes, Agendamentos, Triagem, Consultas | Assistencial | Perfis clínicos | workspace extenso | cuidado com dado sensível | cadastros clínicos | validação e feedback | estados reais | guia padrão | desktop/mobile | não alterado funcionalmente sem SDK; acabamento global herdado |

## Telas refinadas e sistema visual

- O novo stylesheet `v2137-global-ux.css` consolida a paleta solicitada (`#071F3A`, `#2563EB`, `#16A34A`, `#F59E0B`, `#F8FAFC`, `#111827`, `#E5E7EB`) com superfície clara, sombras leves, bordas suaves, labels mais fortes e controles de 44 px.
- O stylesheet foi incluído por último nos layouts autenticado e público para atuar como acabamento incremental, preservando white-label e componentes anteriores.
- Estados de foco permanecem visíveis; foi adicionado reforço para preferência de alto contraste e layouts menores.

## Super Admin e isolamento visual por tenant

- O menu global continua condicionado exclusivamente a `ADMINISTRADOR_GLOBAL`; nele foram agrupados Central Global, Clientes, Cobranças SaaS, Módulos e recursos, Logs e saúde e Auditoria. Nenhum item global foi movido para fora da condição de role.
- O contexto global mostra “Modo Global MNSOFT”, cliente/contexto atual e “Acesso global auditado”. A Central Global explica exclusividade, necessidade de confirmar cliente e auditoria de ações sensíveis.
- Usuário comum recebe “Área da instituição” no badge. Ele continua vendo somente grupos autorizados pelas roles e não recebe seletor, cobrança, observabilidade ou Central Global no bloco global.
- Esta rodada não ampliou autorização, não modificou claims e não criou bypass de tenant. A interface apenas torna visível o escopo que o servidor já autorizou.

## AppShell, login, formulários, mensagens e ajuda

- **AppShell:** menu global comercialmente claro, contexto com hierarquia em duas linhas, estado ativo reforçado, conteúdo limitado para leitura e breakpoints para ações.
- **Login:** POST real, antiforgery, Caps Lock, mostrar/ocultar senha, recuperação, resumo de erro, loading e recuperação de 15 segundos foram preservados; o rótulo explicita e-mail institucional e a versão visual foi atualizada. CPF/CNPJ não foram anunciados porque o view model inspecionado suporta `Email`.
- **Formulários:** labels e bordas ganharam contraste, controles preservam altura mínima de toque e validações existentes continuam inline. Nenhum ID manual foi introduzido; IDs ocultos de registros existentes são chaves técnicas de POST, não campos solicitados ao usuário.
- **Mensagens:** o adiamento da Central de Ações deixou de usar `prompt()` e agora usa dialog nativo acessível, texto de impacto, data/hora, erro inline e toast do resultado real da API.
- **Ajuda:** o AppShell já injeta “Como usar esta tela” conforme controller. A Central Global recebeu ainda orientação específica para o Super Admin.

## QA, comandos e resultados

| Comando | Resultado |
|---|---|
| `git status --short --branch` | executado; árvore inicialmente limpa na branch `work` |
| `git remote -v` | executado; nenhum remote configurado |
| `dotnet --info` / `dotnet --list-sdks` | indisponível: `dotnet: command not found` |
| `find backend/PlantaoPro.Web backend/PlantaoPro.Api ...` | inventário concluído |
| `rg -n "Login|Dashboard|..." backend/PlantaoPro.Web backend/PlantaoPro.Api` | 1.638 correspondências inspecionáveis |
| scans de `href="#"`, dialogs nativos, ID manual, inline style e `innerHTML` | executados; `prompt()` encontrado no escopo foi removido; estilos dinâmicos de progresso/white-label e manipulações preexistentes foram registrados, não alterados sem runtime |
| scan de TODO, parses e SQL interpolado | executado; ocorrências server-side registradas e não modificadas devido à ausência do SDK |

## Screenshots e limitações restantes

- Playwright está instalado, porém a aplicação ASP.NET não pode ser iniciada sem o SDK/runtime .NET. Portanto, não foram produzidos screenshots novos nesta rodada; reutilizar imagens antigas como evidência seria enganoso.
- Builds Debug/Release, testes e QA autenticado não puderam ser executados. Login, policies e isolamento precisam ser novamente validados em CI com o SDK definido pelo projeto.
- A Central Global usa somente links e coleções já entregues pelo backend. KPIs consolidados de clientes, plantões, cobrança, recursos bloqueados, eventos e auditoria não foram inventados; para exibi-los em um único agregado será necessário endpoint real, autorizado e auditável em rodada com SDK disponível.
- Ocorrências antigas de dados demonstrativos em serviços/controllers, SQL dinâmico e estilos inline estão fora do frontend seguro desta rodada e exigem revisão backend com build e testes.
