# PlantãoPro v2.16.3 — Portal do Cliente e contratação de módulos

## Baseline verificada

- **Commit inicial:** `fa1cec0`, merge da v2.16.2; árvore de trabalho estava limpa.
- **Pronto e reutilizado:** autenticação individual/JWT e sessão revogável; contexto por tenant; catálogo canônico `modulos_sistema`; vínculo `tenant_modulos`; perfis/permissões; assinatura, limites e faturas SaaS; Portal `MinhaAssinatura`; auditoria existente.
- **Parcial antes desta entrega:** catálogo administrativo possuía preço base e funcionalidades, porém a página de módulos do cliente era estática e o toggle administrativo ativava diretamente o módulo.
- **Ausente antes desta entrega:** revisão versionada, snapshot comercial imutável, idempotência da solicitação, fila de aprovação global e ativação transacional concorrente.
- **Duplicidade evitada:** não foi criado marketplace, billing nem autorização alternativos. A evolução usa `modulos_sistema`, `tenant_modulos`, `assinaturas`, o contexto autenticado e o serviço de permissões existentes. Cobrança clínica/pagamentos de plantonistas não são consultados ou alterados.

## Jornada entregue

1. O catálogo do portal deriva tenant e estado do contrato no servidor e distingue `ATIVO`, `SUSPENSO`, `NAO_CONTRATADO` e `INDISPONIVEL`.
2. Administrador do cliente revisa módulos e dependências. Preço ausente gera `PROPOSTA`, nunca “grátis”. O hash SHA-256 inclui tenant, início, itens, preços e periodicidade.
3. Confirmação exige a versão revisada e chave de idempotência. Itens persistem nome, descrição, funcionalidades, dependências e preço aceitos, sem alteração retroativa.
4. A solicitação permanece `PENDENTE`; solicitar não ativa módulo nem cria/liquida cobrança.
5. Apenas `ADMINISTRADOR_GLOBAL` aprova ou recusa. A linha é bloqueada e estado/versão revalidados na transação; índice e upsert impedem duplicidade.
6. Aprovação ativa imediatamente ou grava `AGENDADO`, conforme início UTC. Permissões de perfis não são alteradas: o administrador ainda deve atribuí-las dentro do catálogo contratado.
7. Solicitação pendente pode ser cancelada pelo próprio tenant com justificativa; histórico e snapshot permanecem.

## Matriz de estados

| Estado | Ação do cliente | Ação MNSOFT | Acesso efetivo |
|---|---|---|---|
| Indisponível | consultar, sem solicitar | configurar catálogo | não |
| Disponível / sem preço | solicitar proposta | avaliar | não |
| Disponível / com preço | revisar e confirmar | avaliar | não |
| Pendente | acompanhar ou cancelar | aprovar/recusar | não |
| Cancelada/recusada | consultar histórico | nenhuma | não |
| Aprovada futura | acompanhar | idempotente | agendado |
| Contratada ativa | gerenciar permissões | suspender pelo contrato canônico | somente com permissão |
| Contratada suspensa | regularização/suporte | reativar conforme política | não |

## Evidências e limites da execução

- Manifestos e SQL consolidado foram regenerados pelo mecanismo oficial.
- SDK .NET não está instalado neste agente; builds/testes .NET ficaram bloqueados pelo ambiente e não foram declarados como aprovados.
- PostgreSQL/aplicação autenticada não estavam disponíveis para E2E e screenshots reais. Os cenários de dois tenants, transições temporais, teclado, console e mobile permanecem obrigatórios no CI/homologação antes do merge.
- A ativação futura é persistida como `AGENDADO`; o reconciliador operacional que transforma agendamentos vencidos em acesso efetivo deve ser validado/implantado antes de homologar vigências futuras.
