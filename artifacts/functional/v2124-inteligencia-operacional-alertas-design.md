# PlantãoPro v2.12.4 — inteligência operacional, alertas e design

## Diagnóstico e modo de execução

- **Modo usado:** MODO DESIGN ESTÁTICO.
- **SDK:** `dotnet` não está instalado. `which dotnet` não encontrou executável e `dotnet --info` / `dotnet --list-sdks` retornaram `command not found`.
- **Git:** a branch `codex/v2124-inteligencia-operacional-alertas-design` foi criada a partir do estado local. `git remote -v` não apresentou remoto configurado no diagnóstico inicial.
- **Limite respeitado:** nenhum arquivo C#, contrato, `.csproj`, `.props`, `.sln`, banco, migration ou SQL foi alterado. A entrega limita-se a Razor de apresentação, CSS estático e esta documentação; não cria comportamento aparente sem servidor.

## Inteligência encontrada e preservada

O repositório já possui `OperacaoRecomendacaoService`, dashboard de inteligência e modelos de operação inteligente. O serviço existente consulta plantões e escalas persistidos, e a interface já recebe pendências, severidade, módulo, perfil, motivo, ação e URL. Como não foi possível compilar ou testar os contratos `net10.0`, essa implementação foi preservada sem afirmar cobertura além das regras já existentes.

### Fontes de dados identificadas

- plantões, hospitais e escalas para pendências de plantão sem profissional confirmado;
- histórico de escala e carga para o ranking e as sugestões já exibidos;
- dados financeiros persistidos para alertas financeiros já entregues ao dashboard;
- tenant/cliente presentes nos DTOs e consultas existentes.

Nenhum valor, percentual, score, SLA ou previsão foi criado nesta rodada. A apresentação continua exibindo somente o conteúdo recebido dos serviços reais.

## Regras, alertas e recomendações

Não foram adicionadas regras server-side por ausência do SDK. A tela de Operação Inteligente agora explicita que as recomendações existentes são **determinísticas**, orienta o usuário a verificar o motivo e separa visualmente pendências e próximos passos. O estado vazio esclarece que um risco só aparece quando dados reais atendem às regras operacionais.

As recomendações existentes mantêm severidade, módulo, perfil-alvo, mensagem, ação sugerida e link real. Sugestões de profissionais continuam mostrando a justificativa devolvida pelo serviço; não foram adicionados disponibilidade, conflito, especialidade, vínculo, descanso, localidade ou confirmação quando a fonte não estava validável.

## Dashboards e telas alteradas

### Operação Inteligente

- região principal e títulos com semântica acessível;
- bloco **Como usar esta tela** com explicação sobre regras e dados;
- estado vazio orientando acompanhamento e próximo passo;
- cards de pendência com hierarquia, foco e ação mais claros;
- seção de recomendações com contexto sobre motivo, módulo e ação.

### Dashboard Executivo Inteligente

- orientação de uso para período, priorização e comparação;
- título executivo e linguagem visual decisiva;
- placeholders genéricos de filtro, sem hospital ou especialidade fictícios;
- acabamento de filtros, KPIs, tabelas e superfícies que consomem somente valores do modelo.

## Design premium e acessibilidade

A folha `v2124-operational-intelligence.css`, carregada pelo layout autenticado, adiciona superfícies médicas sóbrias, destaque teal, bordas discretas, elevação controlada e hierarquia editorial. Inclui:

- cards e recomendações responsivos;
- foco visível em links de recomendação;
- estado vazio claro;
- filtros empilhados e ações em largura total no celular;
- tabelas com cabeçalhos legíveis e realce de linha;
- suporte a movimento reduzido e cores forçadas.

Não foram adicionados `href="#"`, diálogos `alert()` / `confirm()`, campos de ID manual, mocks, dados fixos, senha ou segredo. Estado de erro e carregamento não foram simulados: devem ser ligados ao ciclo real de requisição em modo completo.

## Métricas e recursos não implementados por falta de dados validados

Permanecem para uma execução com .NET 10 e PostgreSQL disponíveis:

- central persistida com novo/visto/resolvido/ignorado, motivo e timeline;
- feedback de utilidade, ação tomada, responsável e trilha de auditoria;
- regras completas de sobreposição, baixa cobertura configurável, no-show, implantação, cobrança SaaS e chamados críticos;
- recomendação que prove disponibilidade, ausência de conflito, especialidade, vínculo e descanso;
- visões globais do Super Admin e escopo tenant/perfil com testes de autorização;
- dashboards específicos para cliente, escalas e financeiro;
- materialização Dapper, scripts idempotentes e validação server-side.

Não foram implementadas previsões de pendências, adoção, proximidade, taxa de confirmação ou ocorrência recente porque não foi possível confirmar e testar fontes suficientes. Não foi criado machine learning nem qualquer “IA” simulada.

## Testes, verificações e limitações

Foram executados diagnóstico Git/SDK, buscas por padrões frágeis e proibidos, scripts estáticos existentes e `git diff --check`. Restore, builds e testes não puderam ser executados porque o SDK está ausente. Também não foi possível iniciar a aplicação ASP.NET para captura de screenshot; a alteração visual foi validada apenas estaticamente.

A revisão encontrou débito técnico preexistente fora do escopo seguro: `InteligenciaNegocioService` e `InteligenciaController` contêm GUIDs e nomes fixos; formulários de assinatura ainda possuem placeholders de ID; há consultas interpoladas/`SELECT *` e usos de `Guid.Parse` em áreas antigas. Esses itens não foram corrigidos porque exigiriam alterar e validar C#, consultas, contratos e fluxos server-side. Em especial, os registros fixos não foram reutilizados nem apresentados como fonte das melhorias desta versão.

Uma próxima rodada em **MODO COMPLETO** deve validar isolamento global/tenant, permissões por perfil, auditoria, materialização dos DTOs e todas as transições de alerta antes de habilitar novos recursos persistidos.
