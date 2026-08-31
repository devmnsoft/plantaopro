# PlantãoPro v2.11.8 — retomada funcional e design E2E

## Contexto da execução

- **Modo:** MODO ESTÁTICO SEGURO.
- **SDK .NET:** indisponível (`dotnet: command not found` em `dotnet --info` e `dotnet --list-sdks`).
- **Git:** branch `codex/v2118-retomada-funcionalidades-antigas-design-e2e` criada a partir de `work`, com árvore inicialmente limpa.
- **Remoto:** nenhum remoto configurado em `git remote -v`; portanto não foi possível executar `fetch`, `pull`, `push` ou abrir PR remoto.
- **Limite aplicado:** nenhum arquivo C#, projeto, solução, contrato, banco ou migration foi alterado. A rodada ficou restrita a Razor, CSS e documentação.

## Inventário revisado

O inventário de views, controllers e referências de rota confirmou as seguintes áreas reais:

- autenticação, recuperação de senha e acesso negado;
- dashboards, Minha Central, Meu Dia, busca global, favoritos/recentes e preferências;
- administração SaaS, usuários, perfis, permissões, segurança, auditoria e LGPD;
- médicos/profissionais, hospitais/unidades e especialidades;
- escalas, plantões, agenda, convites, cobertura, fechamentos, substituição e check-in;
- financeiro, pagamentos, caixa, faturamento clínico e SaaS;
- notificações, comunicação, pendências e ocorrências de operação assistida;
- relatórios, BI e produtividade;
- Saúde360, pacientes, agendamentos, triagem, consultas, prescrições e CID;
- portais de hospital, cliente e parceiro, além de implantação e atendimento.

## Funcionalidades retomadas e bugs corrigidos

1. **Command Center invisível para perfis administrativos reais.** O menu usava nomes legados e com capitalização incompatível (`Admin`, `Gestor`, `Administrador`), enquanto a aplicação define roles normalizadas em caixa alta. A condição visual agora reutiliza `canManage` e as constantes de coordenação já existentes, sem ampliar autorização do servidor.
2. **Gestão de perfis sem entrada na navegação administrativa.** O fluxo existente de Perfis foi conectado entre Usuários e Permissões, tornando a jornada administrativa explícita.
3. **Notificações sem acesso persistente no menu principal.** Foi incluída uma seção de acompanhamento disponível a usuários autenticados; o controller continua responsável pela autorização e pelo escopo real.
4. **Acabamento global inconsistente.** Uma camada CSS versionada passou a uniformizar bordas, sombras sutis, labels, foco visível, badges, tabelas responsivas, ações no mobile e preferência por movimento reduzido.

## Telas e componentes alterados

- Shell autenticado: carregamento da folha de acabamento v2.11.8.
- Sidebar: regras coerentes de perfil e atalhos de Perfis e Notificações.
- Todas as telas autenticadas que usam o layout: foco, cards, formulários, tabelas e responsividade recebem o refinamento global sem duplicar markup.

## Achados não alterados por falta do SDK

Os achados abaixo exigem validação compilada e testes; foram deliberadamente apenas documentados:

- usos de `Guid.Parse` em claims em controllers de médicos, hospitais, escalas, financeiro, plantões, operação e outros;
- SQL interpolado e ocorrências de `SELECT *`, incluindo caminhos clínicos e comerciais;
- GUIDs fixos em serviços de inteligência e operação comercial que precisam ser auditados quanto a dado demonstrativo;
- formulário de Assinaturas ainda solicita `ClienteId` e `PlanoId` manualmente; removê-los com segurança requer carregar lookups reais no controller/API e validar o binding;
- compatibilidade entre actions, DTOs Dapper, aliases SQL, nullability e isolamento por tenant não pôde ser comprovada por build/testes.

Nenhum desses pontos foi mascarado, removido ou modificado sem compilação.

## Comandos e resultados

| Comando | Resultado |
| --- | --- |
| `pwd` | `/workspace/plantaopro` |
| `git status --short --branch` | árvore inicialmente limpa na branch `work`; depois branch v2.11.8 com mudanças esperadas |
| `git branch --show-current` | `work` no diagnóstico; branch solicitada após criação |
| `git remote -v` | sem saída; remoto ausente |
| `which dotnet` | sem saída |
| `dotnet --info` | falhou: comando não encontrado |
| `dotnet --list-sdks` | falhou: comando não encontrado |
| `find` de soluções/projetos | confirmou `backend/PlantaoPro.sln` e projetos `net10.0` |
| `rg` de framework/LangVersion | confirmou `net10.0` e `LangVersion 10.0` |
| inventários `find` de Web/API | executados; módulos listados acima |
| buscas de rotas, forms e ações | executadas sobre Web e API |
| busca de `href="#"`, diálogos nativos, IDs manuais e TODOs | encontrou IDs manuais em Assinaturas; nenhum diálogo nativo em view/JS alvo |
| busca técnica de parses/SQL/async | executada e documentada, sem alteração C# |

## Limitações reais restantes

- Build, restore e testes .NET não podem ser executados sem SDK.
- Não há servidor executável no ambiente; a alteração visual não pôde ser validada por screenshot nesta execução.
- Não há remoto configurado; publicação e abertura do PR dependem de configurar `origin` em ambiente com credenciais.
- A revisão estática reduz inconsistências objetivas, mas não substitui teste E2E autenticado com tenants e perfis representativos.
- Os IDs manuais de Assinaturas e os achados de backend permanecem como dívida explícita para uma rodada em MODO COMPLETO.
