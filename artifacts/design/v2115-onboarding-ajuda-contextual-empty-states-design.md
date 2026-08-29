# PlantãoPro v2.11.5 — onboarding, ajuda contextual e estados visuais

## Escopo e modo de execução

Rodada executada em **Design Estático Seguro**. O SDK .NET 10 não está disponível no `PATH`; por isso, nenhuma lógica Razor C# nova, contrato, dependência ou funcionalidade de backend foi criada.

## Telas auditadas

Foram revisados login, painel operacional, agenda, plantões, escalas, cobertura, Portal do Profissional, documentos/credenciamento, disponibilidade, Portal da Unidade, solicitações, ocorrências, Saúde360, checklists, indicadores, relatórios e administração/configurações. A auditoria também considerou os componentes compartilhados de empty state, erro, confirmação, toast, ajuda, filtros e formulários.

## Telas e componentes alterados

- **Shell autenticado:** recebeu acesso discreto à ajuda contextual, drawer não invasivo, fechamento por botão, backdrop ou `Escape` e devolução de foco ao acionador.
- **Portal da Unidade:** ganhou orientação de primeiro passo, CTA conectado à rota existente e estado vazio mais informativo para notificações.
- **Checklist do Piloto:** ganhou guia curto, rota real para relatar impedimento e empty state com próximo passo.
- **Ocorrências do Piloto:** ganhou bloco “como funciona”, labels visíveis, obrigatoriedade, hints, mensagens por campo, conteúdo de prioridade, submit com estrutura de loading e empty state orientado.
- **Empty states compartilhados:** receberam acabamento visual mais calmo, institucional, responsivo e consistente.

## Melhorias de onboarding

O painel de orientação usa título curto, explicação direta e, quando aplicável, uma ação real. A comunicação conduz o usuário a conferir cobertura, avançar o checklist e registrar somente ocorrências reais, sem presumir dados ou configuração concluída.

## Ajuda contextual

O drawer compartilhado oferece três passos: conferir contexto, revisar status e ajustar a busca. Inclui orientação curta sobre permissões, não abre automaticamente e não interrompe a tarefa. Em telas pequenas, o acionador é reduzido a ícone com nome acessível.

## Empty states criados ou evoluídos

- notificações da unidade sem atualizações;
- checklist ainda não disponibilizado;
- unidade sem ocorrências registradas;
- padrão compartilhado com ícone, título, texto útil, ação opcional e responsividade.

Na auditoria, plantões, escalas, agenda, dashboard, pagamentos e outros módulos já utilizavam o partial compartilhado de estado vazio. Esses usos passam a herdar o refinamento visual sem alteração de seus dados.

## Mensagens e feedback

A microcopy revisada informa o que está vazio, quando o conteúdo aparecerá e qual ação pode ser tomada. Não foram introduzidos `alert()` ou `confirm()` nativos, links `href="#"`, stack traces ou mensagens com dados sensíveis.

## Formulários ajustados

O formulário de ocorrência agora apresenta labels persistentes, marcação visual de campos obrigatórios, ajuda para prioridade e descrição, validação por campo e botão primário com estrutura de loading já suportada pela experiência visual existente. Nenhum campo de ID foi criado ou solicitado.

## Padrões proibidos removidos

Nenhum novo ID manual, mock, dado fake fixo, dependência, binário, popup invasivo ou ação sem backend real foi adicionado. Os achados residuais da busca global pertencem a áreas preexistentes e ficam como inventário para rodadas específicas; removê-los em massa extrapolaria o escopo estático seguro.

## Arquivos alterados

- `backend/PlantaoPro.Web/Views/Shared/_Layout.cshtml`
- `backend/PlantaoPro.Web/Views/Shared/_ContextHelpDrawer.cshtml`
- `backend/PlantaoPro.Web/wwwroot/js/context-help.js`
- `backend/PlantaoPro.Web/wwwroot/css/design-system/v2010-screen-polish.css`
- `backend/PlantaoPro.Web/Views/HospitalArea/Index.cshtml`
- `backend/PlantaoPro.Web/Views/Piloto/Checklist.cshtml`
- `backend/PlantaoPro.Web/Views/Piloto/Ocorrencias.cshtml`
- `artifacts/design/v2115-onboarding-ajuda-contextual-empty-states-design.md`

## Validações estáticas

Executadas as verificações de whitespace do Git, padrões problemáticos de navegação/diálogos/IDs, segredos conhecidos e status da árvore. A busca de padrões é tratada como auditoria e pode listar ocorrências preexistentes fora dos arquivos alterados.

## Status do build

**Build não executado porque SDK .NET 10 não está disponível no PATH. Alterações limitadas a design estático seguro.**

## Limitações reais

Sem o SDK não foi possível compilar ou executar a aplicação para captura de tela. A validação foi estática. Não foram alterados backend, banco, migrations, autenticação, autorização, `.csproj`, propriedades de build, framework ou TargetFramework. Recomenda-se executar restore/build e inspeção visual navegável em ambiente com .NET 10 e dados reais.
