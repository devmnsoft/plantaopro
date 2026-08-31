# PlantãoPro v2.12.0 — design premium, acabamento final e acessibilidade

## Modo de execução

- **Modo usado:** DESIGN ESTÁTICO.
- **Status do SDK:** o executável `dotnet` não está disponível no ambiente (`dotnet: command not found`).
- **Escopo de segurança adotado:** somente Razor visual (`.cshtml`), CSS e esta documentação. Nenhum controller, modelo, contrato, projeto, solução, migration ou arquivo de banco foi alterado.
- **Branch:** `codex/v2120-design-premium-final-polish-a11y`.

## Inventário e telas revisadas

O inventário cobriu as views existentes de autenticação, shell compartilhado e módulos principais. Foram revisados diretamente o login, o layout autenticado, o layout de autenticação, sidebar, topbar, navegação mobile e estados vazios. Também foram inspecionadas, pelo inventário de Razor e pelos seletores compartilhados, as telas de dashboard, Minha Central/Meu Dia, escalas, plantões, profissionais, unidades, especialidades, solicitações, agenda e disponibilidade, confirmações e presença, financeiro, pagamentos, relatórios, notificações, ocorrências, administração, usuários, perfis e preferências.

Como o acabamento foi implementado na folha transversal carregada pelos dois layouts, cards, botões, formulários, badges, alertas, tabelas, menus, modais, toasts e estados reutilizados nessas páginas recebem o mesmo padrão sem mudanças nos contratos das telas.

## Problemas visuais encontrados

- A evolução visual estava distribuída em várias folhas históricas e ainda não havia uma camada final da v2.12.0 para harmonizar densidade, bordas, elevação, contraste e foco.
- O shell mobile exibia dois acionadores equivalentes do menu: um na topbar e outro na navegação inferior.
- Cards e tabelas podiam variar em borda, raio, sombra, densidade de células e tratamento de cabeçalho conforme a idade da página.
- Inputs e placeholders não possuíam contraste e altura mínima uniformes em todos os módulos.
- O estado vazio compartilhado usava um `id` fixo no título, que poderia se repetir quando mais de um estado fosse renderizado na mesma página.
- O login já possuía uma boa composição, mas faltava a camada final de superfície, profundidade, adaptação para telas estreitas e movimento respeitando redução de animação.
- A busca de segurança visual identificou campos técnicos de cliente/plano em uma tela legada de assinatura. Eles não foram convertidos nesta rodada porque isso exigiria fonte de opções e mudança de contrato/backend, proibida no modo sem SDK.

## Melhorias aplicadas

### Design system transversal

- Criada a folha `v2120-premium-final-polish.css`, carregada por `_Layout` e `_AuthLayout`.
- Consolidada uma paleta clínica sóbria em azul-marinho, azul e teal, com canvas frio, superfícies brancas, texto de alto contraste e bordas claras.
- Padronizados raio, sombra leve, densidade e hierarquia de cards, painéis, botões, inputs, selects, badges, alertas, dropdowns, modais, toasts e tabelas.
- Botões e campos receberam alvos de interação confortáveis; placeholders e mensagens auxiliares receberam contraste reforçado.
- Tabelas mantêm rolagem horizontal segura em telas estreitas, com cabeçalho legível e linhas mais bem alinhadas.

### Shell e navegação

- Sidebar ganhou separação, refinamento de agrupamentos, indicador ativo e scrollbar discreta sem introduzir um segundo logo.
- Topbar passou a usar superfície translúcida clara, borda e elevação contidas.
- Em resoluções abaixo de `lg`, o acionador duplicado da topbar é ocultado e o botão da navegação mobile permanece como ponto único de abertura do menu.
- Conteúdo recebeu largura máxima e gutters fluidos para 360 px, tablet, 1024 px e desktop.

### Login premium

- Reforçada a composição de produto com canvas clínico, shell elevado, painel institucional sóbrio e formulário com largura de leitura controlada.
- Adicionado detalhe geométrico discreto, sem imagem binária ou dado fictício.
- Em mobile, benefícios secundários são recolhidos, ações ocupam a largura disponível e os painéis usam espaçamento compacto.
- A animação é decorativa, lenta, e é efetivamente desativada quando `prefers-reduced-motion` está ativo.

### Acessibilidade e estados

- Foco visível de alto contraste cobre links, botões, campos, selects, textareas e elementos com `tabindex`.
- Incluído suporte a `forced-colors`, redução de movimento e impressão limpa.
- Estado vazio compartilhado deixou de gerar `id` duplicado, passou a nomear a região diretamente e marcou o ícone como decorativo.
- Estados com `aria-busy="true"` recebem feedback visual consistente sem interferir em regiões de status.
- Textos longos podem quebrar com segurança; títulos usam balanceamento quando suportado.

## Componentes criados ou alterados

- **Criado:** `wwwroot/css/design-system/v2120-premium-final-polish.css`.
- **Alterado:** `Views/Shared/_Layout.cshtml` para carregar o acabamento v2.12.0 no produto autenticado.
- **Alterado:** `Views/Shared/_AuthLayout.cshtml` para carregar o acabamento na autenticação.
- **Alterado:** `Views/Shared/_EmptyState.cshtml` para semântica reutilizável sem colisão de identificador.

## Decisões de design

1. **Evolução, não reescrita:** a camada v2.12.0 complementa os componentes existentes e preserva Razor, rotas, permissões e JavaScript.
2. **Sem excesso de gradiente:** apenas o canvas do login usa uma luz radial muito sutil; superfícies operacionais permanecem sólidas.
3. **Densidade vendável:** sombras contidas, raios médios e espaçamento fluido evitam cards gigantes e aparência de template cru.
4. **Mobile com uma única navegação:** o menu inferior já existente é a fonte única do acionador em telas pequenas.
5. **Progressive enhancement:** `:has`, `text-wrap`, `backdrop-filter` e `color` modernos são melhorias; o layout continua funcional sem eles.

## Validações executadas

- Diagnóstico obrigatório de caminho, branch, remotos e SDK.
- Inventário de `.cshtml`, `.css`, `.scss` e `.js` em `backend/PlantaoPro.Web`.
- Buscas por padrões inseguros/temporários e por componentes visuais.
- `git diff --check`.
- Verificação estrutural básica da nova folha CSS (balanceamento de chaves).
- Scripts de segurança e compatibilidade existentes, quando presentes.
- Varredura final solicitada de `href="#"`, diálogos nativos, IDs manuais, conteúdo fictício e segredos conhecidos.

Build e testes .NET não puderam ser executados porque o SDK não está instalado. Nenhuma tentativa de contornar essa limitação alterando backend ou configuração foi feita.

## Limitações reais restantes

- Não foi possível compilar Razor, executar testes automatizados .NET nem iniciar a aplicação para captura visual, pois `dotnet` está ausente.
- A conversão dos campos técnicos legados de assinatura em selects exige opções reais providas pelo backend; fazer isso apenas no HTML criaria uma interface falsa ou quebraria o contrato.
- Páginas muito antigas com estilos inline de progresso e preview de white-label permanecem inalteradas quando o valor visual é calculado dinamicamente; removê-los com segurança exigiria uma estratégia de classes/variáveis validada em runtime.
- O remoto `origin` foi configurado com a URL informada, mas o acesso de rede retornou HTTP 403 e o GitHub CLI não possui autenticação; fetch, push e abertura do PR não puderam ser concluídos neste ambiente.
