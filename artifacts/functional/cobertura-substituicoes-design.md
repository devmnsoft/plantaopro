# Cobertura e substituições — evolução v2.17.0

## Base verificada

A base já possuía `plantoes`, `escalas`, `plantao_convites`, disponibilidade/indisponibilidade, candidatos, aprovações, histórico, notificações, check-ins e pagamentos. Também havia aceite transacional de convite para vaga descoberta. O fluxo de substituição, porém, permitia pedido duplicado, aceitava uma escala não vinculada ao plantão e substituía o médico **na própria atribuição original**, o que quebrava autoria e poderia associar presença/financeiro à pessoa errada. Os endpoints de gestão também estavam disponíveis a qualquer usuário autenticado.

## Estados e transições

| Agregado | Origem | Evento | Destino | Efeito |
|---|---|---|---|---|
| Solicitação | — | profissional solicita | `SOLICITADA` | original permanece responsável |
| Solicitação | `SOLICITADA` | gestor aprova busca | `APROVADA` | nenhuma alteração na escala |
| Solicitação | ativa | gestor convida | `SUBSTITUTO_CONVIDADO` | convite/candidato persistido |
| Solicitação | convite aceito | política exige gestor | `AGUARDANDO_APROVACAO` | nenhuma alteração na escala |
| Solicitação | pronta | gestor efetiva | `CONFIRMADA` | nova escala criada; original `substituido` |
| Solicitação | ativa | cancelamento/recusa | `CANCELADA`/`RECUSADA` | histórico preservado; original mantido |
| Convite | `CONVIDADO` | recusa/expiração | `RECUSADO`/`EXPIRADO` | solicitação não é concluída |
| Outros convites | ativo | outra cobertura efetivada | `REVOGADO` | impede segunda atribuição |

A versão otimista e locks transacionais protegem duplo clique, aprovação antiga e dois candidatos concorrentes. A elegibilidade (vínculo ativo, disponibilidade afirmativa, indisponibilidade e conflito) é revalidada imediatamente antes da efetivação. Ausência de disponibilidade cadastrada é tratada como **não confirmada**, nunca como disponibilidade presumida.

## Regras e autorização

- Pedido exige vínculo ativo, atribuição própria confirmada, plantão futuro, justificativa, inexistência de pedido ativo e ausência de presença/pagamento.
- Plantão iniciado segue ocorrência/passagem de responsabilidade; consolidados seguem ajuste operacional.
- Gestores acessam endpoints de consulta, convite, decisão e efetivação; o profissional usa somente `/api/medicos/me/substituicoes`.
- A efetivação cria nova atribuição e relaciona-a à solicitação. Não move check-in, assinatura, correção, pagamento ou autoria.
- Convites simultâneos são permitidos para candidatos diferentes; apenas um é efetivado e os restantes são revogados.
- “Urgente” não ganhou SLA inventado: a central mantém prioridades configuradas/persistidas.

## Matriz de formulários

| Operação | Interface | Controller | Serviço | Persistência | Consulta independente |
|---|---|---|---|---|---|
| Solicitar | área profissional | `MedicosMeDisponibilidadeController` | `SolicitarSubstituicaoAsync` | solicitação + histórico | minhas substituições |
| Consultar | central/área profissional | controllers por perfil | listagem/detalhe tenant-scoped | leitura canônica | detalhe + histórico |
| Convidar | central | `SubstituicoesFase4Controller` | `ConvidarSubstitutoAsync` | candidato | detalhe/histórico |
| Efetivar | central | `SubstituicoesFase4Controller` | `ConfirmarSubstitutoAsync` | solicitação + duas escalas + candidatos + histórico | escala e histórico |

As telas agora incluem ajuda contextual, consequências explícitas e filtros preservados na query string. A seleção usa fluxos de candidatos existentes, sem entrada manual de identificador na interface.

## Testes e limitações

Foram adicionados contratos para propriedade, duplicidade, bloqueios de presença/financeiro, autorização, versionamento, idempotência e índices. O ambiente entregue não contém o SDK .NET nem PostgreSQL em execução, portanto build, testes integrados, instalação/upgrade e E2E autenticado não puderam ser executados localmente. A aplicação real também não pôde ser iniciada; por isso não há screenshot fabricado. CI deve executar restore, Debug/Release, testes e os dois gates PostgreSQL antes da homologação.
