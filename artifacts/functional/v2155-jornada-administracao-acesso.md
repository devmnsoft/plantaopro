# v2.15.5 — jornada administrativa e critérios de homologação

## Finalidade das páginas

- **Login:** autenticar a pessoa por credencial individual e abrir apenas o contexto autorizado. CPF identifica pessoa e CNPJ identifica organização; nenhum documento substitui a senha individual.
- **Clientes/contexto:** permitir que o Super Administrador MNSOFT escolha uma organização ativa e perceba visualmente o contexto global/assistido.
- **Módulos:** informar contratado, disponível e indisponível a partir do catálogo persistente. A tela não deve chamar seleção de módulo de compra ou pagamento concluído.
- **Usuários:** permitir que a administração do cliente crie, edite, inative e revogue sessões somente no próprio tenant, sem atribuir perfil global.
- **Perfis/permissões:** criar perfis locais e persistir apenas permissões do catálogo permitido. Alterações devem ser transacionais e auditadas.

## Roteiro funcional obrigatório

1. Super Administrador entra, permanece em contexto global identificado, seleciona Cliente A com justificativa e consulta módulos/usuários.
2. Administrador A cria um perfil permitido e um usuário A; a senha temporária é trocada sem exposição em log/JavaScript.
3. Usuário A entra, acessa módulo contratado permitido e recebe acesso negado no módulo não contratado.
4. Administrador A tenta ler/editar usuário B por ID, atribuir perfil global e enviar `tenantId` B: todas as tentativas devem falhar sem revelar a existência do alvo.
5. Bloquear usuário A ou revogar seu perfil/sessão deve invalidar a autorização efetiva já na requisição seguinte.

## Casos preparados para ambiente executável

| Caso | Resultado esperado |
|---|---|
| Credencial válida/inválida | Redirecionamento autorizado / mensagem genérica sem enumeração. |
| API indisponível ou timeout | Botão recuperado, valores não sensíveis preservados e mensagem útil. |
| Duplo clique e retorno pelo navegador | Um POST; botão restaurado em `pageshow`. |
| Tenant A solicita ID do tenant B | 404 genérico antes do cálculo/materialização. |
| Usuário com vínculo secundário ativo/vencido | Permitido no ativo; `CROSS_TENANT_DENIED` no vencido. |
| Módulo não contratado | `MODULE_NOT_CONTRACTED`. |
| Cliente/usuário bloqueado | Sessão recusada e operação bloqueada. |
| Revogação concorrente | Token recusado após persistência da revogação; sem janela de cache permissivo. |

## Limitação explícita

Esta rodada fecha em código o vazamento de consulta/diagnóstico de permissões e o reconhecimento seguro de múltiplos vínculos. A jornada completa já possui componentes persistentes, mas **não está homologada** sem .NET 10, PostgreSQL de teste e Playwright contra a aplicação real. Preview estático não será usado como prova de autenticação.
