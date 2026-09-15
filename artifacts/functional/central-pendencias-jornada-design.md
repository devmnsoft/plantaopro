# Central de Pendências — jornada e design

## Base confirmada

- **Commit inicial:** `47cc197` (`Merge pull request #483`), árvore limpa; branch `codex/central-pendencias-jornada-design`.
- Foram lidos o histórico recente, `.github/workflows/dotnet-ci.yml`, `.github/workflows/database-one-click.yml`, o relatório `v2162-pendencias-jornadas-design.md` e os relatórios recentes de execução/conferência, fechamento, cobertura/substituição e ocorrências.
- A implementação reutiliza `ProductivityActionService`, a rota `/Pendencias` e a Central compartilhada com Meu Dia. Não foi criada tabela, projeção, dashboard ou máquina de estados paralela.
- A consulta é direta, limitada e paginada no PostgreSQL. A chave derivada é estável por origem/registro/ação; a ordenação termina em `Key`, evitando instabilidade entre itens com a mesma prioridade e data.

## Matriz de fontes e resolução

| origem | condição de pendência | responsável elegível | ação permitida | condição de resolução | implementação | teste |
|---|---|---|---|---|---|---|
| `cobertura_convites` | `PENDENTE` | profissional convidado | Responder convite | aceitar, recusar, cancelar ou expirar na origem | já existia; preservada | contrato v2162 |
| `escalas` | solicitada/pendente/aguardando confirmação | profissional vinculado ou equipe autorizada | Confirmar plantão | status sai do conjunto aberto | já existia; rótulo objetivo nesta rodada | contrato v2162 + rótulos |
| `pagamentos` | pendente/em conferência/atrasado | profissional ou financeiro autorizado | Conferir pagamento | confirmar, pagar, cancelar ou inativar | já existia; preservada | contrato v2162 |
| `fechamento_plantao` | em conferência/com divergência/aguardando aprovação | gestão/financeiro autorizado | conferir/revisar fechamento | transição canônica da origem | já existia | contrato v2162 |
| `pagamento_contestacoes` | `ABERTA` | financeiro autorizado | Analisar contestação | resolução/cancelamento na origem | já existia | contrato v2162 |
| `agendamentos` + `checkins` | agendamento do dia sem check-in | recepção autorizada | Registrar chegada | check-in ou cancelamento | já existia | contrato v2162 |
| `consultas` | rascunho/em atendimento | profissional vinculado | Continuar atendimento | finalizar/cancelar | já existia | contrato v2162 |
| `medico_checkins` | registro incompleto ou pendente | gestores com acesso à conferência | Conferir execução | aprovação/correção pela origem | **integrada nesta rodada** | contrato de fonte/estado |
| `medico_presenca_correcoes` | correção `PENDENTE` | gestores com acesso à conferência | Revisar correção | aprovar, recusar ou cancelar | **integrada nesta rodada** | contrato de fonte/estado e ação |
| `ocorrencias_operacionais` | aberta/em atendimento/aguardando informação | gestor; solicitante/responsável conforme autorização da origem | atribuir, acompanhar ou resolver | resolver/cancelar na origem | **integrada nesta rodada**; detalhe e resolução usam API canônica, versão e tenant | contrato de fonte/estado e ação |

### Capacidades apenas descritas ou ainda não integráveis com segurança

- **Vaga descoberta:** há telas de cobertura, mas não foi confirmada uma identidade canônica única sem sobrepor plantão, escala e convite. Não foi inventada uma pendência.
- **Substituição:** `substituicoes_plantao` usa `cliente_id`, enquanto a Central usa contexto `tenant_id`; sem contrato inequívoco entre ambos, a fonte não foi adicionada a uma consulta multi-tenant. O fluxo canônico existente foi preservado.
- **Apuração bloqueada por configuração:** não foi encontrada condição persistida e testável que diferencie bloqueio configuracional de fechamento em andamento.
- **Preferências favoritas/agrupamento/densidade/página inicial:** não há armazenamento canônico confirmado para esse contexto. O estado de filtros permanece na URL; não foi criada tabela equivalente.
- **Falha parcial por fonte:** as fontes confirmadas compartilham o mesmo PostgreSQL e são consultadas em um único statement consistente. Uma falha SQL ainda torna a Central indisponível, explicitamente, em vez de produzir falso vazio. Isolar artificialmente cada `UNION` introduziria múltiplas fotografias e paginação em memória.

## Jornada, autorização e concorrência

Itens continuam sendo derivados do estado atual da origem. A Central não possui “Concluir”; adiar altera somente a apresentação do usuário e relê autorização/estado antes do `upsert`. Conferência e correções são expostas apenas para operação gestora (`not doctorOnly`), e ocorrências continuam filtradas por tenant e, para profissional, por solicitante/responsável.

O detalhe de ocorrência consulta novamente a API autorizada. A resolução envia a `versao` observada ao comando canônico; `409` informa mudança concorrente ou item já resolvido, sem alegar sucesso. O formulário usa antiforgery, validação cliente/servidor, texto preservado após falha e não pede GUID. Reenvio é tratado pela concorrência otimista do domínio.

Os links de ação agora descrevem a consequência (`Confirmar plantão`, `Conferir execução`, `Revisar correção`, `Atribuir responsável`) em vez de “Abrir”. Ao voltar, módulo e filtros permanecem na URL. Após adiar, a página é recarregada para sincronizar lista e totais; ler notificação continua sem qualquer efeito sobre a entidade de origem.

## Filtros, prazos e design

- módulo, situação, prazo, intervalo e “Atribuídas a mim” são enviados ao servidor;
- o intervalo usa a data relevante: prazo da origem quando presente, senão criação do item; limite final é exclusivo e inclui todo o dia informado;
- paginação ocorre no SQL, com desempate pela chave estável;
- cada item explicita módulo, situação, motivo, contexto, responsabilidade e ação;
- ausência de prazo aparece como **Sem prazo definido**; um prazo existente é chamado de **Prazo**, sem tratar toda data como vencimento;
- a ajuda explica origem, filtro, responsabilidade, leitura versus resolução e atualização;
- a lista responsiva e os componentes PlantãoPro existentes foram reutilizados, sem novo menu, logo ou cartão decorativo.

## Auditoria dos formulários alterados

| ação | interface → binding | autorização | serviço → SQL/persistência | consulta independente → retorno |
|---|---|---|---|---|
| filtrar pendências | GET tipado (`DateOnly`, booleano, seleções) | categorias decididas pela API e tenant em todas as fontes | `ProductivityActionService` → CTE derivada, filtros parametrizados, `OFFSET/LIMIT` | listagem e total da janela retornam juntos; resumo geral vem da mesma CTE |
| adiar apresentação | diálogo com label, data futura e antiforgery | item relido com usuário/tenant/perfil | `SnoozeAsync` → `upsert` idempotente por tenant/usuário/chave | reload preserva query string e atualiza totais |
| resolver ocorrência | textarea, ids ocultos gerados pelo servidor, antiforgery | API revalida tenant, gestor/responsável e estado | `OcorrenciaService.TransicionarAsync` → lock/versão, evento e commit | redirect faz GET independente; conflito não simula sucesso |

Cadastro, atribuição e cancelamento de ocorrência não foram alterados nem declarados validados. Não foi adicionado `DELETE` físico.

## Validação e limitações

- `git diff --check`: aprovado.
- `python3 scripts/check-csharp10-compatibility.py`: aprovado após corrigir regressões de C# 12 e escape Razor herdadas dos commits recentes de ocorrência/demonstrativos.
- O executor não possui SDK .NET, `psql`, Docker nem navegador configurado. Restore, builds Debug/Release, xUnit, PostgreSQL isolado, instalação/upgrade, E2E autenticado e screenshots reais não puderam ser executados; não há alegação de homologação.
- Devem ser executados no CI e em ambiente descartável: central vazia/múltiplas fontes/sem prazo; consistência de contagem e paginação; resolução e duas decisões concorrentes; tenant cruzado, revogação e troca de organização; notificação lida sem resolução; profissional, gestor e super administrador; captura desktop/mobile da aplicação real.

Não houve alteração SQL nesta rodada. As migrations aplicadas e o consolidado foram preservados.
