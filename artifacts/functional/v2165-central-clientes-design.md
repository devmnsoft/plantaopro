# PlantãoPro v2.16.5 — correção de testes e Central de Clientes

## Baseline e CS0122

- Baseline: commit `8620a05`, branch local `work`, árvore inicialmente limpa; não foi encontrado `AGENTS.md` no repositório nem nos diretórios superiores pesquisados.
- A única referência externa indevida encontrada por `rg -n "RepositoryPathResolver.Root\b" backend` estava em `V2160ClinicalJourneyContractTests.cs`. `Root` é implementação privada (`Lazy<string>`); o teste agora usa exclusivamente `RepositoryPathResolver.RepoRoot`, preservando o encapsulamento, as assertions e a resolução portátil do repositório.

## Matriz curta verificada

| Capacidade | Implementação encontrada | Lacuna / decisão desta rodada | Regra preservada | Teste/evidência |
|---|---|---|---|---|
| Contratação modular | `ModuleContractingService`, solicitação versionada/idempotente e aprovação global | Reconciliador de início futuro segue pendente de homologação | Solicitação não ativa; snapshot guarda preço acordado; perfil continua necessário | contratos v2.16.3/v2.16.4 + gate PostgreSQL pendente |
| Login e revogação | JWT, `auth_sessoes`, middleware e serviço de administração de segurança | E2E com sessão aberta requer runtime/banco | Revogação é verificada pelo servidor, não apenas menu | testes existentes + E2E pendente |
| Onboarding | Criação transacional, assinatura, unidade, administrador e checklist persistido | Endpoint legado ainda permite conclusão manual; convite individual canônico completo não foi encontrado | Não foi criado segundo sistema de identidades/convites | matriz v2.16.4; jornada completa pendente |
| Equipe/perfis | `SecurityAdministrationServices`, perfis e permissões por tenant | Concorrência do último admin requer PostgreSQL | Admin local não atribui papel global; módulo não concede permissão | testes de segurança existentes + integração pendente |
| Central global | Controller API global; consulta única paginada com busca/status e agregados; MVC somente global | Detalhes operacionais usam rotas canônicas existentes | Totais usam conjunto filtrado; estado administrativo não é contrato | `V2165CentralClientesContractTests` |
| Ação de situação | Transação, `FOR UPDATE`, motivo obrigatório, estado idempotente e auditoria | Endpoints legados permanecem por compatibilidade, não são usados pela tela nova | Audita ator, cliente, antes/depois, motivo e resultado; não apaga histórico | contrato v2.16.5 + integração pendente |

## Central e experiência

A listagem agora pesquisa nome/razão/CNPJ, filtra situação e pagina no servidor. Uma consulta agregada traz módulos efetivamente habilitados/ativados e não desativados, usuários ativos e pendências persistidas sem consulta por linha; a segunda instrução no mesmo round-trip calcula totais sobre todo o filtro. A interface define fonte e período dos indicadores e declara telemetria indisponível em vez de inferir desuso.

O detalhe mantém permanentemente o cliente selecionado, ator real e modo global, com abas **Visão geral**, **Organização e unidades**, **Módulos e contratos**, **Equipe e perfis**, **Cobranças** e **Histórico**. Links reutilizam rotas existentes; não há GUID manual, `alert()`, `confirm()` nem botão fictício. Filtros/tabela usam componentes responsivos existentes.

Suspensão/reativação requer motivo e confirmação acessível. O servidor revalida papel global, bloqueia a linha, torna repetição idempotente e registra auditoria. Situação financeira, situação contratual e situação administrativa permanecem conceitos distintos; nenhuma regra automática de inadimplência foi inventada.

## Testes, evidências e limitações

- A busca de referência privada foi repetida após a correção e não encontrou usos externos.
- O SDK .NET não existe neste executor (`dotnet: command not found`): restore, builds Debug/Release, filtro xUnit e solução completa **não foram executados**, nunca declarados aprovados.
- PostgreSQL descartável e aplicação real não estavam disponíveis. Instalação/upgrade SQL não se aplicam (nenhum SQL/migration alterado). Integrações com dois clientes, contrato expirado, último admin, concorrência, troca de contexto durante request, revogação com sessão aberta e E2E completo permanecem gates obrigatórios no CI.
- Pelo mesmo bloqueio não foi possível iniciar a aplicação real; screenshots desktop/mobile e inspeção do console não foram fabricadas e permanecem para homologação.
- Pendência não relacionada registrada para sprint posterior: consolidar o fluxo de convite individual de funcionário (entrega, expiração, revogação, uso único, aceite concorrente e conta global com vínculo adicional) no modelo canônico antes de declarar a jornada integral concluída.
