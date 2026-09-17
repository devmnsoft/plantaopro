# Central Operacional e jornadas — relatório funcional

## Base auditada

- Commit inicial preservado: `f133cd0f67f18b36b080bb4a0f7486013d26ff9f`.
- Branch de trabalho: `codex/central-operacional-jornadas-design`.
- Configuração efetiva encontrada: todos os projetos em `net10.0` e `Directory.Build.props` fixa `LangVersion` em `10.0`; nenhum desses valores foi alterado.
- O `OperationalReportsController` já usa construtor convencional e campo `readonly`; as strings SQL auditadas em `SecurityAdministrationServices` e `OcorrenciaService` usam delimitadores válidos; o gate de compatibilidade C# 10 passou. Não foram reaplicadas correções anteriores.
- `DevelopmentSeed` e constantes de autorização serão também cobertos pelo build quando houver SDK. A inspeção não encontrou uma nova referência de constante ausente.
- A tela de login já valida antes de entrar em loading, impede duplo envio, recupera o botão em `pageshow`, usa antiforgery e POST nativo. O controller valida `returnUrl` com `Url.IsLocalUrl`, chama a API persistida e não contém fallback de senha. A execução completa permaneceu bloqueada pela ausência do SDK e de navegador no ambiente.

## Matriz da jornada e fontes

| Funcionalidade / rota | Fonte canônica | Perfis / escopo | Módulo | Ação existente reutilizada | Lacuna / regra de entrada e saída | Evidência e teste necessário |
|---|---|---|---|---|---|---|
| Minha Central `/MinhaCentral` / `GET api/minha-central` | `plantaopro.work_items`, isolada por `tenant_id` e unidade | Usuário autenticado; contexto do token | Comum + módulo da origem | Abre detalhe e ação do módulo autorizado | Entra enquanto status não é `CONCLUIDO`/`CANCELADO`; sai somente após mudança persistida da origem. Registros legados ainda dependem do produtor de `work_items`. | Testar lista vazia, deduplicação por `tipo:id`, falha da API e escopo de unidade. |
| Pendências `/Pendencias` | `work_items` e serviços específicos | Perfis com vínculo no tenant | Conforme tipo | Consulta do registro | Notificação lida não altera a seleção da Central. | Testar leitura de notificação sem remoção da pendência. |
| Plantões `/Plantoes`, detalhe `/Plantoes/Details/{id}` | `plantoes` | Gestão e profissionais conforme vínculo | `PLANTOES` | Publicar, cancelar, abrir cobertura nos controllers canônicos | Tipos `PLANTAO`/`COBERTURA` recebem link somente quando o módulo é acessível. | Testar plantão sem cobertura e resolução real. |
| Escalas `/Escalas` | `escalas` e vínculos de escala | Gestão / profissional vinculado | `ESCALAS` | Confirmações existentes no módulo | Tipos `ESCALA`/`CONFIRMACAO` direcionam ao módulo; sem mutação genérica. | Testar confirmação repetida e concorrente. |
| Execução `/ConferenciaExecucao` | presença/execução persistida | Conferentes autorizados | `EXECUCAO` | Conferência canônica | A Central não altera o estado diretamente. | Testar negação sem permissão e saída após conferência. |
| Ocorrências `/Ocorrencias?id={id}` | `ocorrencias_operacionais` + eventos | Gestor ou solicitante/responsável | `OCORRENCIAS` | Atribuir/transicionar pelo serviço de ocorrência | Aberta/em atendimento permanece; resolvida/cancelada deve deixar de ser produzida como pendência. | Testar tenant, versão concorrente e resolução com providência. |
| Apuração/financeiro `/Financeiro` | tabelas e serviços financeiros | Financeiro / administração autorizada | `FINANCEIRO` | Consulta de apuração/demonstrativo | Atalho e ação são omitidos sem módulo e permissão. | Testar módulo não contratado e perfil profissional. |
| Relatórios `/Relatorios/Operacional` | consultas parametrizadas de `OperationalReportService` | Perfis do relatório | Relatórios | Consulta/exportação | Já evoluído no commit anterior; não duplicado na Central. | Executar contratos de relatório e escopo. |
| Equipe/perfis `/Seguranca/Usuarios` | usuários, perfis, permissões e sessões | Administrador do cliente / global em contexto explícito | `SEGURANCA` | Gestão canônica de acessos | Atalho apenas para admin e módulo autorizado; revogação existente invalida sessões. | Testar revogação com aba aberta e permissões delegáveis. |
| Administração global `/SaasDashboard` e `/Clientes` | clientes, contratos e módulos SaaS | Administrador global | `ADMIN_SAAS` | Seleção explícita de cliente | Sem tenant selecionado, a Central não fabrica zeros operacionais: apresenta visão global e exige seleção de cliente. | Testar retorno à visão global e auditoria da troca de contexto. |

## Implementação

A Central existente foi mantida e convertida de um quadro que permitia mover/concluir tarefas genericamente para uma lista operacional orientada à origem. Cada item agora expõe chave estável, tipo, situação textual com ícone, origem, unidade, prazo real ou “Sem prazo definido”, responsável, detalhe e próxima ação autorizada. A consulta deduplica por tipo/origem, aplica o escopo de tenant/unidade do token, limita a cem itens com ordenação estável e nunca trata falha de carregamento como lista vazia.

Os atalhos são produzidos no servidor depois de verificar módulo e permissão. Administradores globais sem tenant recebem uma visão global explícita e devem selecionar cliente antes de acessar a operação; os totais operacionais não são apresentados nesse estado.

Os filtros são recolhíveis, persistem na URL ao navegar e voltar, e distinguem lista vazia de filtro sem resultado. A apresentação adapta-se a 360, 768 e 1440 px por breakpoints, mantém foco visível herdado do design system, usa labels persistentes, `aria-live` no resultado e respeita redução de movimento.

## Validações

### Aprovadas

- `python3 scripts/check-csharp10-compatibility.py` — compatibilidade C# 10 e CSS Razor.
- `git diff --check` — sem erros de whitespace.

### Não executadas por limitação ambiental

- Restore, builds Debug/Release e testes .NET: o executável `dotnet` não está instalado.
- PostgreSQL isolado e cenários transacionais: não executados sem build/runtime da aplicação.
- E2E de autenticação e jornada persistida: não executado porque não há runtime .NET nem navegador disponível.
- Screenshots em 360/768/1440: não produzidos; a aplicação real não pôde ser iniciada.

## Limitações e próximos passos

1. Executar CI com .NET 10 e PostgreSQL 16, incluindo os cenários transacionais listados na matriz.
2. Validar no banco real quais produtores sincronizam cada estado canônico para `work_items`; criar sincronização oficial somente para tipos ainda ausentes, sem duplicar regras.
3. Executar o E2E completo com usuários persistidos de gestor, profissional, administrador do cliente e global, incluindo revogação durante sessão.
4. Capturar screenshots da aplicação real em 360, 768 e 1440 px depois do E2E aprovado.
5. Evoluir carregamento independente por fonte quando a Central passar a consultar múltiplos serviços diretamente; hoje a fonte consolidada falha como uma única seção.
