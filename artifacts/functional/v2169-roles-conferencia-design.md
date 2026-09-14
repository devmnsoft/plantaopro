# PlantãoPro v2.16.9 — papéis, conferência e desenho operacional

## Causa-raiz

A Web referenciava `RolesConstants.EscalasGestao`, constante existente somente na API, causando CS0117. O POST Web também recebia um `bool` cujo valor ausente era `false`, mas ignorava esse valor na ausência de correção e chamava a rota de aprovação normal. Assim, uma recusa manipulada ou decisão omitida podia ser convertida em aprovação.

## Alterações

- O grupo Web agora replica exatamente os códigos persistidos da API, sem incluir Hospital, Financeiro, Médico ou Auditor. Web e API usam somente `EscalasGestao` como grupo de entrada.
- A decisão Web usa enum anulável e validador compartilhável. Execução normal admite exclusivamente `AprovarExecucao`; correções admitem exclusivamente aprovação ou recusa de correção. IDs, versões e justificativa são validados antes da chamada.
- O contrato de correção leva `PresencaId`; a API bloqueia IDs incompatíveis, versão inválida, autoria própria, contexto incorreto, ausência das permissões canônicas e vínculo fora do cliente/unidade.
- Permanecem as travas transacionais, versões otimistas, contagem de linhas, histórico antes/depois e distinção entre aprovação operacional e pagamento já introduzidas na v2.16.8.
- Filtros usam `QueryHelpers`, datas invariantes e parâmetros opcionais omitidos. A página ganhou navegação por estados reais, ações nomeadas, solicitação datada e mensagens específicas para validação, acesso, indisponibilidade, conflito e comunicação.

## Matriz de acesso e decisão

| Ator/contexto | Consultar | Aprovar execução/correção | Recusar correção |
|---|---:|---:|---:|
| Administrador global com contexto explícito | `ESCALAS_VER` | `ESCALAS_CONFIRMAR` | `ESCALAS_RECUSAR` |
| Administrador/administrador cliente do cliente atual | conforme permissão | conforme permissão | conforme permissão |
| Diretor, coordenação, coordenador ou operador | conforme permissão e vínculo | conforme permissão e vínculo | conforme permissão e vínculo |
| Mesmo papel em outro cliente/unidade | não | não | não |
| Hospital, médico, financeiro ou auditor | não entra | não | não |
| Autor da correção | consulta conforme escopo | não decide a própria correção | não decide a própria correção |

## Testes e evidências

- Testes v2.16.9 exercitam em código o conteúdo do grupo e o atributo MVC, além de decisão ausente, decisão de correção enviada à presença normal, IDs/versões inválidos e ação incompatível.
- As garantias v2.16.8 continuam cobertas pelos testes de materialização Dapper, alias `PresenceId`, replay da migração 400, travas, versões e intervalo efetivo.
- `dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj --no-restore`: não executado; SDK `dotnet` ausente no agente.
- Build da solução, Debug/Release, testes .NET, integração PostgreSQL e E2E: não executados pela mesma limitação inicial de SDK e pela ausência de ambiente autenticado/banco preparado.
- `git diff --check`: aprovado no workspace.

## Limitações

Não há evidência local suficiente para declarar homologação, responsividade visual em navegador real ou ausência absoluta de defeitos. Screenshots não foram produzidos porque a aplicação não pôde ser compilada/iniciada sem o SDK. A integração PostgreSQL, cenários concorrentes e a matriz autenticada completa devem ser executados no CI com banco efêmero antes do merge.
