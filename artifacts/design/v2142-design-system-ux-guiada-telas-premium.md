# PlantãoPro v2.14.2 — design system e UX guiada

## Escopo e diagnóstico

Rodada de acabamento visual exclusivamente no frontend. O ambiente não possui o comando `dotnet`; por isso nenhum arquivo C#, controller, service, DTO, autenticação, banco ou migration foi alterado. A branch de trabalho é `codex/v2142-design-system-ux-guiada-telas-premium`.

O inventário estático encontrou **398 views Razor**. A aplicação já possuía uma biblioteca extensa de partials (cabeçalho, KPI, filtros, tabela, paginação, empty/error state, modal, drawer, timeline, toast, contexto de tenant e jornada guiada) e folhas incrementais até v2.14.1. A decisão desta rodada foi consolidar o acabamento, sem duplicar componentes nem simular dados.

## Telas auditadas

Foram mapeadas as views de:

- acesso: login, recuperação/redefinição de senha, acesso negado, erro e não encontrado;
- operação: Dashboard, Meu Dia, Minha Central, Command Center, escalas, plantões, agenda, convites e substituições;
- cadastros: médicos, hospitais, especialidades, pacientes, usuários, perfis e permissões;
- clínica: agendamentos, atendimento, prontuário, convênios e financeiro clínico;
- gestão: financeiro, pagamentos, relatórios, BI, configurações, integrações, auditoria, LGPD e observabilidade;
- SaaS: Administração Global, clientes, módulos, planos, assinaturas, faturamento SaaS, onboarding, Customer Success, implantação e operação assistida;
- portais por perfil: cliente, hospital, médico/profissional e parceiro;
- estados transversais: carregamento, vazio, erro, bloqueio de módulo/plano, confirmação, notificações e drawers.

A auditoria considerou título e explicação, hierarquia de ações, labels, mensagens, estados assíncronos, tabela/filtros, contraste, foco, adaptação mobile, ícones, contexto de tenant e ocorrências de padrões proibidos. A cobertura detalhada por view continuará em evolução: nem todas as 398 telas foram alteradas nesta rodada.

## Telas e arquivos alterados

- **Login:** indicação persistente de falta de conexão, bloqueio do envio enquanto offline, retorno correto do botão ao histórico de navegação e versão visual v2.14.2.
- **Cockpit Super Administrador:** comando de contexto explícito, acesso ao seletor real de clientes e ajuda compacta “Como usar esta tela”.
- **Shell autenticado e shell de acesso:** inclusão da camada v2.14.2 para consistência transversal.
- **Design system:** nova camada de acabamento para cabeçalhos, cards, estados, formulários, tabelas, foco, ajuda contextual, contexto global e redução de movimento.

## Melhorias de design aplicadas

O arquivo `v2142-guided-premium.css` preserva os contratos atuais e adiciona:

- ritmo tipográfico e largura de leitura controlada em PageHeader/SectionHeader;
- superfície institucional única para KPI Card, Action Card, Empty/Error State e Form Section;
- cabeçalho, contorno e foco contextual mais claros em Data Table;
- bloco compacto reutilizável para “Como usar esta tela”;
- banner de contexto que combina texto, borda e cor (sem depender apenas da cor);
- comportamento mobile para comando de contexto, tabelas e ajuda;
- respeito a `prefers-reduced-motion` e foco visível preservado.

Os componentes existentes continuam sendo a fonte para Loading State, Status Badge, Info Banner, toast, Filter Bar, confirmação, drawers e timeline. Não foram criados equivalentes concorrentes.

## Login premium e resiliente

O login mantém identidade PlantãoPro/MNSOFT, identificador “E-mail, CPF ou CNPJ”, senha, recuperação, Caps Lock, mensagens por motivo, validação, acessibilidade e POST real. O controle de submissão existente já restaura o botão após 15 segundos, ao voltar pelo histórico, em erro local e em perda de rede. A v2.14.2 acrescenta um status offline humano anunciado por `aria-live` e impede novo envio até a conexão voltar.

As mensagens não confirmam a existência de conta, não exibem credenciais e não adicionam bypass. Usuário bloqueado, cliente bloqueado, sessão expirada, acesso negado e funcionalidade indisponível continuam descritos sem expor detalhes sensíveis.

## Formulários e mensagens

Os formulários tocados mantêm labels visíveis, obrigatoriedade, mensagens por campo, validação summary, ação primária e estado de processamento. Máscaras e seletores de relacionamento permanecem sob os componentes reais já existentes; não foram adicionados campos de ID manual.

Tom recomendado e adotado:

- sucesso: “Registro salvo. As informações já estão disponíveis.”;
- erro: “Não foi possível concluir agora. Revise os dados e tente novamente.”;
- atenção: “Há informações pendentes antes de continuar.”;
- sessão: “Sua sessão expirou por segurança. Entre novamente para continuar.”;
- acesso: “Você não tem permissão para acessar esta área.”;
- comunicação: “A conexão foi interrompida. Verifique sua internet e tente novamente.”.

## Experiência por perfil e tenant

- **Super Administrador:** o cockpit identifica a visão global, concentra atalhos globais e agora deixa explícito quando nenhum cliente está em assistência. A seleção encaminha ao cadastro real de clientes; nenhuma opção fake foi criada.
- **Cliente/tenant:** o frontend apenas apresenta contexto. Isolamento e ações continuam condicionados às autorizações retornadas pelo servidor.
- **Usuário comum:** menus e módulos continuam derivados do perfil/permissões existentes; a camada visual não amplia acesso.
- **Acesso assistido:** deve sempre exibir o tenant e oferecer saída fácil pelo banner existente; ações permanecem auditáveis no backend existente.

## Padrão “Como usar esta tela”

O padrão recomendado é um `details` compacto, navegável por teclado, com título literal **Como usar esta tela**. O conteúdo deve responder, em até dois parágrafos curtos: finalidade, ações, obrigatoriedade, significado dos status e caminho em erro/bloqueio. O login e o cockpit tocados atendem ao padrão. O shell também mantém o guia contextual global para telas autenticadas.

## Comandos e resultados

- diagnóstico: `pwd`, `git status --short --branch`, `git remote -v`, `dotnet --info`, `dotnet --list-sdks`;
- inventário: `find backend/PlantaoPro.Web/Views -type f -name '*.cshtml'` e buscas `rg` solicitadas;
- validações finais: `git diff --check`, varredura de padrões proibidos, varredura de segredos e scripts Python disponíveis;
- resultado do SDK: `dotnet: command not found`;
- QA visual: aplicação não pôde ser iniciada sem runtime .NET; portanto não foram produzidas capturas confiáveis. O QA foi estático sobre Razor, CSS e JavaScript.

## Limitações reais e próxima rodada

Sem .NET não foi possível executar clean, restore, builds, testes, servidor local ou screenshots das rotas autenticadas. Também não seria seguro inventar sessão, dados ou permissões para capturas.

Próximos passos:

1. executar a suíte .NET em ambiente com SDK compatível;
2. validar visualmente login desktop/mobile e os principais perfis com contas de teste autorizadas;
3. capturar dashboard, formulário, tabela, cockpit, empty state e error state;
4. migrar gradualmente telas legadas para os partials atuais, adicionando ajuda específica somente quando forem tocadas;
5. testar leitores de tela, zoom de 200%, teclado e navegadores suportados com dados reais controlados.
