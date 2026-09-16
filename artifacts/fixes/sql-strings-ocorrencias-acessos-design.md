# Correção de strings SQL, ocorrências e administração de acessos

Data: 2026-09-16
Commit inicial inspecionado: `915b905aa9b4c0b1c1b1de4d00b08a31cdecbbf4`

## Escopo e causa-raiz

O escopo auditado foi limitado aos fluxos de ocorrências operacionais e administração de acessos. Não representa auditoria de todos os formulários do PlantãoPro.

### `SecurityAdministrationServices.cs`

Nas duas consultas de capacidade, uma string verbatim (`@"..."`) continha aliases PostgreSQL escapados como string C# normal (`\"Limit\"` e `\"Used\"`). Em uma string verbatim, a barra não escapa aspas: a primeira aspa encerrava o literal e fazia o compilador interpretar o SQL restante como C#. Isso explica os erros em cascata CS1056, CS1010, CS1012, CS1003, CS0742, CS1525 e CS1031.

Antes (representação abreviada):

```csharp
var sql = @"select count(*) as \"Used\"";
```

Depois:

```csharp
var sql = @"select count(*) as ""Used""";
```

Os aliases continuam entre aspas para a materialização Dapper dos membros `Limit` e `Used`, ambos derivados como `integer` (`limite_usuarios` e `count(*)::int`).

### `OcorrenciaService.cs`

O mesmo defeito estava nas consultas interpoladas verbatim de listagem/contadores e na consulta verbatim de detalhe: todos os aliases dos DTOs usavam `\"...\"`. Eles foram convertidos somente nessas strings para `""...""`; escapes válidos em strings C# normais não foram alterados.

Antes (representação abreviada):

```csharp
var sql = $@"select id as \"Id\" from ... where {where}";
```

Depois:

```csharp
var sql = $@"select id as ""Id"" from ... where {where}";
```

Os aliases correspondem aos parâmetros do record `OcorrenciaDto`. Os agregados `count(*)` do PostgreSQL materializam como `long`; `Vencidas` foi corrigido de `long?` para `long`, pois `count` nunca retorna `null`.

## Evoluções funcionais

### Ocorrências

- criação valida unidade, título e descrição também na camada de serviço, antes de abrir a persistência;
- unidade e plantão continuam validados no tenant do usuário;
- listagem e seus contadores agora limitam usuários comuns às ocorrências solicitadas ou atribuídas a eles; gestores preservam a visão do tenant;
- atribuição exige responsável e versão válidos, aceita somente identidade ativa com vínculo válido no tenant e mantém o `UPDATE` otimista;
- transição verifica exatamente uma linha afetada antes de criar o evento e confirmar a transação;
- resolução/cancelamento permanecem transições de domínio, sem `DELETE` físico e sem efeitos implícitos em presença, substituição ou pagamento;
- o detalhe web consulta e apresenta o histórico persistido, a próxima ação e a orientação sobre efeitos da resolução;
- falhas na carga do histórico são apresentadas separadamente, sem transformar o detalhe carregado em sucesso falso.

### Administração de acessos

- remoção do perfil do último administrador habilitado do cliente passa a ser rejeitada, além da proteção já existente para bloqueio/inativação;
- alterações cadastrais/de perfil continuam revogando sessões existentes;
- bloqueio por administrador de cliente passa a alterar `usuario_tenant_acessos` e revogar somente sessões daquele tenant, sem alterar a identidade global;
- bloqueio global permanece reservado ao contexto global e é auditado separadamente;
- a autorização efetiva passa a respeitar vínculo explícito bloqueado mesmo quando o cliente é o tenant primário legado da identidade;
- listagem e confirmação visual distinguem situação da identidade de situação do vínculo.

## Matriz de auditoria dos formulários afetados

| Tela | Operação | Endpoint | Serviço | Tabela(s) | Permissão/escopo | Teste | Resultado |
|---|---|---|---|---|---|---|---|
| Equipe | consultar | `GET api/seguranca/usuarios` | `UsuariosAsync` | `usuarios`, `usuarios_perfis`, `perfis`, `usuario_tenant_acessos` | roles administrativos + `TenantScope` | contrato de fonte | coberto; execução em PostgreSQL pendente |
| Vínculo/perfis | criar/editar | `POST/PUT api/seguranca/usuarios` | `SalvarUsuarioAsync` | `usuarios`, `usuarios_perfis`, `auth_sessoes` | tenant fixado no servidor; perfil global excluído | contrato de fonte | proteção do último admin adicionada; execução em PostgreSQL pendente |
| Equipe | bloquear/reativar vínculo | `POST api/seguranca/usuarios/{id}/{ação}` | `AlterarStatusUsuarioAsync` | `usuario_tenant_acessos`, `auth_sessoes`, auditoria | tenant do administrador; global somente sem tenant | contrato de fonte | isolamento implementado; execução em PostgreSQL pendente |
| Ocorrência | criar | `POST api/ocorrencias` | `CriarAsync` | `ocorrencias_operacionais`, `ocorrencia_eventos` | unidade/plantão no tenant | workflow/contrato de fonte | validações e transação cobertas estruturalmente; execução em PostgreSQL pendente |
| Central/detalhe | consultar | `GET api/ocorrencias[/{id}]` | `ListarAsync`/`ObterAsync` | `ocorrencias_operacionais` | tenant + solicitante/responsável ou gestor | contrato de fonte | filtro servidor adicionado; execução em PostgreSQL pendente |
| Detalhe | atribuir | `POST api/ocorrencias/{id}/atribuir` | `AtribuirAsync` | `ocorrencias_operacionais`, `ocorrencia_eventos` | gestor + responsável elegível | contrato de fonte | versão e vínculo validados; concorrência real pendente |
| Detalhe | resolver/cancelar | `POST api/ocorrencias/{id}/situacao` | `TransicionarAsync` | `ocorrencias_operacionais`, `ocorrencia_eventos` | gestor ou responsável conforme matriz | workflow/contrato de fonte | justificativa, versão e linha afetada verificadas; execução real pendente |
| Detalhe | histórico | `GET api/ocorrencias/{id}/historico` | `HistoricoAsync` | `ocorrencia_eventos` | mesmo acesso ao detalhe; visibilidade filtrada | contrato de fonte | integrado à tela; execução web pendente |

## Design, UX e acessibilidade

- equipe ganhou colunas semânticas separadas para identidade e vínculo, textos de confirmação com pessoa/cliente/consequência e ações nomeadas como vínculo;
- detalhe da ocorrência ganhou “Como usar esta página”, próxima ação e histórico persistido com lista semântica e `time` legível;
- formulários existentes mantêm antiforgery, labels visíveis, preservação do texto de resolução após falha e proteção contra envio repetido;
- foram reutilizados os componentes e estilos existentes, sem nova folha de estilo, navegação ou logotipo concorrente.

O manifesto de instalação também foi alinhado ao manifesto de migrations: `430_ocorrencias_operacionais.sql`, já marcado como ativo e obrigatório, não constava na sequência de instalação limpa. A seção canônica v2171 foi incluída depois das dependências operacionais, sem editar migration já aplicada nem duplicar o SQL consolidado.

## Testes e evidências

Testes de contrato foram adicionados para os escapes verbatim, isolamento do vínculo, revogação de sessões, proteção do último administrador, tenant/versão/histórico de ocorrência e ausência de exclusão física. Os testes puros existentes continuam cobrindo a matriz canônica de transições.

O ambiente não contém `dotnet`, `psql` nem `docker`. Uma tentativa de instalar o SDK pelo `mise` também falhou por indisponibilidade de rede. Portanto, restore, build, testes .NET, instalação/upgrade em PostgreSQL, E2E autenticado e captura de tela da aplicação não puderam ser executados localmente. Nenhuma dessas validações é declarada como aprovada. `git diff --check`, a validação dos manifestos e os testes Python do validador foram executados e aprovados após a correção do manifesto.

## Limitações restantes

- executar a suíte e os builds com o SDK .NET compatível com `net10.0`;
- executar instalação limpa e upgrade pelo manifesto oficial em PostgreSQL descartável;
- executar os cenários concorrentes e cross-tenant contra PostgreSQL real;
- executar E2E autenticado completo e smoke responsivo desktop/tablet/celular;
- capturar evidência visual após iniciar API e Web; isso não foi simulado a partir de HTML estático.
