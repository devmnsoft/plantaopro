# PlantãoPro v2.07.0 — notificações e alertas operacionais

## Resumo técnico

- A central canônica existente em `Operation360` foi evoluída, sem criar uma terceira implementação: filtros tenant-safe por tipo, período, prioridade, módulo e status; leitura em lote; arquivamento; resolução e preferências.
- A topbar e o drawer existentes foram preservados. O contador agora interpreta corretamente o envelope `ApiResponse`, e a duplicação do drawer no layout foi removida.
- Alertas automáticos partem de `work_items` reais, atribuídos e ainda ativos. A origem e o destinatário compõem a chave idempotente, evitando alertas duplicados em reavaliações.
- Preferências sempre mantêm o canal in-app habilitado. E-mail e push têm somente preparação de domínio/outbox; nenhum provedor, secret ou dado artificial foi incluído.
- Todas as consultas e mutações da implementação canônica exigem simultaneamente `tenant_id` e `usuario_id`.

## Regras operacionais entregues

| Origem real | Evento interno | Prioridade |
|---|---|---|
| Convite pendente | Plantão aguardando confirmação | Alta |
| Escala sem cobertura | Plantão sem profissional | Crítica |
| Agendamento não confirmado | Check-in pendente | Alta |
| Ocorrência aberta | Ocorrência em plantão | Alta |
| Repasse pendente | Pagamento pendente de aprovação | Média |
| Conta vencida | Fechamento financeiro pendente | Alta |
| Alerta de SLA | Risco de cobertura | Crítica |

Troca/substituição, conflito de escala e conclusão/erro de exportação estão preparados no contrato de eventos, mas não foram inferidos por consultas frágeis: devem ser despachados pelo produtor transacional correspondente quando esses fluxos publicarem um `work_item` atribuído. Essa decisão preserva o código real e impede falsos positivos em produção.

## Telas

- **Central de Notificações:** hero calmo, filtros responsivos, cards ordenados por impacto, badges, ações contextuais e estados de carregamento, vazio e erro.
- **Topbar/drawer:** sino existente, contador de não lidas, últimas notificações, leitura individual/em lote e rota real para a central.
- **Preferências:** cards por categoria, switches sem IDs manuais e microcopy explícita sobre canais ainda não configurados.

## Auditoria e rastreabilidade

Leitura mantém timestamp por usuário; arquivamento e resolução são eventos imutáveis em `notification_actions`; geração registra regra/origem, tenant e destinatário. Logs críticos usam `ILogger` e exceções de despacho não são engolidas.

## Limitações reais

- O container de implementação não disponibiliza o SDK `dotnet`; restore, build e testes .NET precisam ser confirmados pelo CI.
- O checkout foi recebido sem remote Git. O remote foi restaurado para a URL oficial, porém o proxy do ambiente bloqueou o fetch com HTTP 403; por isso, a branch `main` local foi apontada para o baseline recebido.
- E-mail e push não possuem provedor configurado; a entrega externa permanece deliberadamente desativada.

## Validação

Os resultados finais de segurança, compatibilidade, schema, busca de padrões proibidos e `git diff --check` são registrados na descrição do PR. A validação visual automatizada não foi possível sem runtime .NET; os componentes são responsivos por CSS e não introduzem links falsos ou diálogos nativos.
