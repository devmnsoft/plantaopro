# Correção C# 10 e relatórios operacionais

## Baseline e configuração efetiva

- Commit inicial desta rodada: `9fdbd7e` (`Merge pull request #487 from devmnsoft/codex/corrigir-erros-de-compilacao-e-evoluir-funcionalidade`). A árvore estava limpa.
- `backend/Directory.Build.props` define `LangVersion` como `10.0`, além de nullable e implicit usings. Não há `Directory.Build.targets` no repositório.
- `backend/PlantaoPro.Api/PlantaoPro.Api.csproj` e os demais projetos relevantes definem `TargetFramework` como `net10.0`. A linguagem e o framework foram preservados, sem mudanças no pipeline.

## CS8936

A declaração de `OperationalReportsController` usava construtor primário de classe, recurso posterior ao C# 10. Ela foi convertida para classe comum com campo privado `readonly`, construtor público e atribuição explícita de `OperationalReportService`. Rotas, `[ApiController]`, `[Authorize]`, classe base, endpoints, tipos de resposta e `CancellationToken` foram preservados. O serviço continua registrado como scoped em `Program.cs`; não foi criado manualmente e não foi introduzido service locator.

A inspeção direcionada das alterações recentes e dos arquivos relacionados não encontrou outro construtor primário de classe. Records posicionais, válidos em C# 10, foram preservados. As strings SQL de `SecurityAdministrationServices.cs` e `OcorrenciaService.cs` estão delimitadas corretamente; não foi feita remoção global de barras invertidas.

## Matriz da implementação existente

| relatório | fonte | regra | filtros | permissão | detalhamento | impressão/exportação | teste |
|---|---|---|---|---|---|---|---|
| Cobertura | `plantoes`, `hospitais`, `escalas`, `substituicoes_plantao` | vagas planejadas; atribuições ativas; confirmação/realização; descobertas por `greatest(vagas-atribuições,0)`; substituição confirmada com nova escala | início do plantão, unidade, especialidade, profissional, situação; máximo 366 dias | `RELATORIOS_VER`, tenant/cliente e escopo de unidade/profissional | origem em Plantões; filtro do indicador preserva o contexto | impressão do navegador e CSV da seleção, até 5.000 linhas | contratos do controller e segurança do CSV; consulta real depende de PostgreSQL |
| Execução/conferência | `medico_checkins`, `escalas`, `plantoes`, `hospitais`, `medicos` | check-in registrado; incompleto; pendente; correção pendente; aprovado | início do plantão associado, unidade, especialidade, profissional, situação | `RELATORIOS_VER`, tenant/cliente e escopo | origem em Conferência de Execução | impressão e CSV da seleção | contratos e CSV; consulta real depende de PostgreSQL |
| Apuração | `fechamento_plantao`, `plantoes`, `pagamentos`, `pagamento_contestacoes`, `fechamento_divergencias` | apurado e fechado não são somados entre si; pago usa pagamentos; ajuste usa diferença resolvida; pendência usa divergência aberta | competência pelo início do plantão; pagamento pela data de pagamento; unidade, especialidade, profissional e situação | `FINANCEIRO_VER`, tenant/cliente e escopo | origem em Fechamento | impressão e CSV da seleção | preservação de status na falha de exportação e CSV; consulta real depende de PostgreSQL |

As CTEs agregam atribuições por plantão antes de combiná-las e usam paginação estável por data e identificador. A data final informada na interface é transformada em limite superior exclusivo. Assim, um plantão que começa no último dia é incluído mesmo se terminar depois da meia-noite. O relatório declara que apresenta estado atual, não fotografia histórica.

## Evolução funcional e visual

- A exportação agora preserva o status HTTP produzido pela validação/autorização em vez de transformar todo erro em 403. Continua revalidando consulta, tenant, escopo e permissão antes de gerar o arquivo.
- CSV UTF-8 com BOM, separador `;`, neutralização de fórmulas, remoção de quebras de linha e escape de aspas. A ação declara que exporta a seleção (limitada a 5.000 registros), e não apenas a página.
- Cabeçalho identifica PlantãoPro/MNSOFT e o contexto autenticado. A impressão contém nome, filtros ativos, período, critério temporal, geração, dados e totais; navegação, filtros e ações são ocultados no papel.
- Foram adicionados “Limpar filtros”, resumo de filtros ativos e paginação com todos os filtros preservados.
- O layout mantém azul-marinho, gelo e bordas discretas. Em mobile, filtros e ações ficam em coluna, indicadores ficam legíveis e somente o contêiner da tabela rola horizontalmente. Há foco visível e instrução acessível para a tabela.
- Ajuda contextual diferencia vaga, atribuição, confirmação, execução, aprovação, apuração e pagamento; também distingue resultado vazio de falha e registra a limitação histórica.

## Testes e evidências

Executados no ambiente desta rodada:

- `git diff --check`: aprovado.
- Inspeções com `rg` dos arquivos relacionados, configurações herdadas, registro DI e strings SQL: sem nova incompatibilidade comprovada.

Não executados por limitação do ambiente:

- Todos os comandos `dotnet` falharam imediatamente porque o executável/SDK não está instalado (`dotnet: command not found`). Portanto, não se declara build verde.
- PostgreSQL e navegador da aplicação não estão disponíveis/configurados; instalação/upgrade de banco, consultas de integração e E2E autenticado desktop/mobile (incluindo salvar impressão em PDF) não puderam ser executados. Nenhum SQL ou migration foi alterado nesta rodada.

Foram substituídos os testes que apenas procuravam texto em arquivos por testes executáveis de metadados reais do controller (construtor DI, autorização, rotas e `CancellationToken`), comportamento do escape CSV e preservação do status de falha de exportação.

## Limitações restantes

A seleção de unidade, especialidade e profissional ainda usa os identificadores aceitos pela tela existente; uma futura evolução pode conectá-los aos componentes de autocomplete autorizados já usados em outros módulos. O limite explícito da exportação é de 5.000 linhas. A fotografia histórica não pode ser reconstruída sem uma fonte histórica completa. Validação em PostgreSQL e homologação visual permanecem pendentes pelas limitações acima.
