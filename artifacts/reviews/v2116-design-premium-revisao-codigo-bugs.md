# v2.11.6 — revisão de código e design premium

## Resumo executivo

- **Modo executado:** estático.
- **SDK:** `dotnet` não existe no `PATH`; `dotnet --info` e `dotnet --list-sdks` falharam com `command not found`.
- **Git:** a árvore inicial estava limpa na branch `work`; não havia remoto configurado em `git remote -v`. Foi criada a branch local `codex/v2116-design-premium-revisao-codigo-bugs`.
- **Escopo inventariado:** 921 arquivos-fonte existentes pelo inventário inicial, além da nova folha de estilo desta rodada. Foram pesquisados 403 arquivos C#, 396 Razor, 57 folhas CSS, 40 JavaScript e 26 SQL (a diferença de uma entrada entre a soma por sufixo e o `find` inicial foi preservada, sem estimativa). Foram pesquisados controllers, services, repositories, DTOs, ViewModels, views, partials, scripts, estilos, SQL, testes e configuração.
- **Regra de segurança aplicada:** nenhum `.cs`, SQL, migration, projeto ou configuração de framework foi alterado. Sem compilador, os achados de backend abaixo são pendências para confirmação em ambiente .NET 10, não correções por suposição.

## Evolução visual segura

Foi adicionada uma camada CSS versionada e carregada depois do polimento anterior no layout compartilhado. Como o layout atende as áreas autenticadas e públicas, a evolução alcança login, dashboards, agenda, plantões, escalas, cobertura, portais, Saúde360, relatórios e administração sem alterar models, rotas ou dados.

Melhorias aplicadas:

- paleta sóbria em azul-petróleo, superfícies calmas e elevação leve;
- fundo global com profundidade discreta, sem neon ou blur;
- hierarquia uniforme para cards, painéis e cabeçalhos;
- campos com alvo mínimo confortável, bordas mais legíveis e foco de alto contraste;
- botões primários consistentes e estados `hover` contidos;
- linhas de tabela com leitura por `hover` e navegação por `focus-within`;
- feedback, estados vazios e badges com acabamento consistente;
- ajustes mobile de espaçamento, ações e tabelas roláveis;
- suporte explícito a movimento reduzido e cores forçadas.

Não foi produzida imagem ou screenshot: além de o SDK impedir executar a aplicação, o escopo proíbe adicionar screenshots ou binários.

## Achados confirmados por inspeção estática

### Formulários que ainda solicitam identificadores

1. `Views/Assinaturas/_Form.cshtml` expõe `ClienteId` e `PlanoId` como inputs de texto com placeholders “ID do cliente” e “ID do plano contratado”. Isso contraria a jornada sem ID manual. A correção correta requer carregar opções reais e autorizadas do tenant no ViewModel/controller; foi apenas documentada porque mudaria contrato e backend.
2. `Views/Convites/Index.cshtml` solicita que o operador cole o código do plantão, embora ofereça navegação para a Central de Escala. Substituir o campo por lookup/autocomplete exige fonte real e validação de tenant.

Campos `hidden`, `asp-route-id` e IDs técnicos de elementos HTML não foram classificados como entrada manual: eles são necessários para vinculação segura, rotas e acessibilidade.

### Exceções e resultado parcial

- `DashboardPremiumService.AddCount` captura tabela/coluna inexistente (`42P01`/`42703`) sem log e omite silenciosamente o KPI. O método superior pode retornar o dashboard como se a consulta tivesse sido concluída. Recomenda-se registrar aviso estruturado por tabela e sinalizar indisponibilidade parcial, sem expor detalhes ao usuário.
- A pesquisa encontrou 145 blocos `catch` para revisão manual. Não há base segura, sem build/testes, para generalizar que todos sejam incorretos; o caso vazio acima é o único confirmado nesta rodada.

### Autenticação, autorização e dados simulados

- `Fase6BiIntegracoesController` contém rotas `[AllowAnonymous]` genéricas em `api/public/v1/{recurso}` e respostas fixas/vazias, incluindo financeiro e convênios. O próprio payload informa tenant `api-key`, mas o trecho não demonstra autenticação de API key. É risco alto de contrato enganoso e de exposição futura caso dados reais sejam conectados sem um guard obrigatório.
- Os demais usos de `[AllowAnonymous]` incluem login, erro, planos/cadastro e páginas comerciais públicas, coerentes por nome, mas devem permanecer cobertos por testes de autorização e rate limiting.
- `InteligenciaNegocioService`, `InteligenciaController` e `B2BCommercialOpsServices` contêm GUIDs fixos. Eles devem ser auditados em runtime para separar demonstração explicitamente isolada de fluxo produtivo; não foram removidos por chute.

### Conversões, tempo e assincronicidade

- Foram encontradas 36 ocorrências de `Guid.Parse`; várias leem a claim `uid` com `First(...)`. Token/claim malformado pode resultar em exceção em vez de resposta de autenticação controlada. Recomenda-se centralizar a leitura com `TryParse`, mantendo auditoria e rejeição segura.
- `AgendaViewModels` usa `DateTime.Now` para tempo de espera, enquanto grande parte do projeto usa UTC. `PlantoesController` também cria valores-padrão com horário local. A regra de timezone precisa ser confirmada antes de conversão, para não alterar horário clínico/operacional.
- Não foram encontrados `async void`, `DateTime.Parse`, `decimal.Parse` ou `int.Parse` em produção pelos padrões pesquisados.
- As seis ocorrências textuais de `.Result`/`.Wait(` incluíram nomes como `Resultado` e atribuições MVC, não evidência confirmada de bloqueio síncrono. A busca contextual não confirmou chamada bloqueante.

### Dapper, SQL, DTOs e tenant

- 82 arquivos combinam chamadas Dapper ou declarações de DTO/Response; a busca ampla encontrou 663 referências Dapper. A inspeção estática confirmou uso extensivo de aliases e parâmetros, mas a compatibilidade de materialização exige build e testes de integração.
- Foram confirmados `select *` interpolados em `Saude360ClinicalService` e `SaasComercialOperacaoController`. Além de ampliar acoplamento de materialização e tráfego, ambos combinam nome de tabela; é necessário confirmar que as whitelists que originam esses nomes são fechadas e substituir o asterisco por colunas/aliases explícitos.
- Há consultas com nomes de tabela concatenados em `DashboardPremiumService`. Os nomes são constantes internas passadas pelo próprio serviço, não entrada do usuário; ainda assim, uma enumeração/whitelist explícita reduziria risco de manutenção.
- Fixtures de integração possuem fallback local `Username=postgres;Password=postgres`. Não é segredo produtivo, mas a execução em CI deve exigir variável dedicada e banco efêmero isolado para evitar conexão acidental fora do ambiente de testes.
- A cobertura de `tenant_id` não pode ser inferida apenas por contagem. Consultas clínicas inspecionadas usam o filtro, mas cada uma das centenas de chamadas deve ser exercitada em testes de isolamento multi-tenant antes de qualquer correção.
- DTOs posicionais podem ser aceitos pelo Dapper quando a assinatura é compatível; sem execução, nenhum construtor ou nullable foi alterado.

### Razor, referências e rotas

- 414 arquivos contêm referências Razor/rota/model pesquisadas. Partials centrais usados pelo layout existem, inclusive validação, estados, overlays, toasts e navegação.
- A busca obrigatória não encontrou `href="#"`, `alert(` ou `confirm(` nas árvores Web/API.
- Confirmações existentes usam atributos `data-confirm` e o modal compartilhado, em vez de `window.confirm`.
- Não foram alterados action names, models ou formulários, pois isso exigiria compilação e testes. Os dois fluxos com ID manual permanecem pendentes conforme descrito.

## Inconsistências por categoria

| Categoria | Situação | Prioridade / encaminhamento |
|---|---|---|
| Lógica | KPI é omitido silenciosamente quando schema não existe | Alta; log estruturado e indicador de resultado parcial após build |
| Métodos | Endpoints públicos genéricos retornam sucesso e coleções vazias/fixas | Alta; confirmar contrato, autenticação por API key e semântica HTTP |
| Conversão | `Guid.Parse(First(claim))` em múltiplos controllers | Alta; centralizar validação da identidade |
| Tempo | mistura potencial de `DateTime.Now` e UTC | Média; definir política de timezone e testar horários |
| Referências | nenhuma partial inexistente confirmada estaticamente | Validar Razor compilation no SDK |
| Dapper/DTO | grande superfície, dois `select *` e risco de aliases/nullability | Testes de integração com schema real e tenants distintos |
| Razor/rotas | IDs manuais em assinatura e convite | Alta de UX; implementar lookup real autorizado |
| Segurança | `[AllowAnonymous]` em API pública genérica | Crítica antes de conectar dados reais |

## Alterações não realizadas

- Nenhum bug de backend foi corrigido, pois não há SDK.
- Nenhuma migration, SQL, `.csproj`, `Directory.Build.props`, `TargetFramework`, `LangVersion` ou `global.json` foi alterado.
- Nenhum mock, segredo, dado fixo novo, binário ou imagem foi adicionado.
- IDs manuais não foram apenas escondidos: isso preserva a funcionalidade até existir seletor real, filtrado por tenant e permissões.

## Comandos e resultados

| Comando / grupo | Resultado |
|---|---|
| `pwd`, `git status --short --branch`, `git branch --show-current`, `git remote -v` | repositório correto; árvore limpa; branch inicial `work`; nenhum remoto listado |
| `which dotnet`, `dotnet --info`, `dotnet --list-sdks` | SDK ausente; comandos `dotnet` falharam com `command not found` |
| `find backend` para soluções/projetos e `rg` de frameworks | `net10.0`, C# `10.0`, sem alteração |
| inventário `find` de `.cs`, `.cshtml`, `.js`, `.css`, `.sql` | 921 fontes existentes mapeadas; 922 após adicionar o CSS |
| buscas de lógica, exceções, parses, Dapper, tenant, permissões e autorização | executadas; achados triados acima |
| buscas de Razor, actions, partials, IDs, validação e rotas | executadas; 414 arquivos de referência e dois fluxos de ID manual confirmados |
| busca de `SELECT *`, SQL interpolado e credenciais | dois `select *` minúsculos, SQL interpolado controlado a revisar e fallbacks locais de teste; nenhum segredo produtivo confirmado |
| `python3 scripts/repository-security-check.py` | sucesso: `repository-security ok` |
| `python3 scripts/check-csharp10-compatibility.py` | sucesso: compatibilidade C# 10 e CSS Razor validada |
| `python3 scripts/validate-scrpt-completo.py` | sucesso: cobertura reportada em 100% |
| `dotnet clean/restore/build/test` | não executados: executável ausente |

## Limitações e retomada

Build e testes não executados porque SDK .NET 10 não está disponível no PATH. Correções de backend não foram aplicadas. Revisão de código foi documentada em modo estático.

Em ambiente compatível, a retomada deve executar `dotnet clean`, `restore`, builds Debug/Release e testes Release; depois, confirmar os achados com testes focados em claims inválidas, API pública, schema parcial, timezone, materialização Dapper e isolamento entre dois tenants. A inspeção visual navegável também deve validar contraste, foco, overflow e tabelas em 320 px, 768 px e desktop.
