# Indicadores e relatórios — evolução integrada

**Base registrada:** `abdfa17` (`work`), árvore inicialmente limpa. Revisados histórico recente, workflows `dotnet-ci`/`database-one-click`, relatórios anteriores de cobertura, execução, fechamento e Central de Pendências.

## Capacidades confirmadas

| Capacidade | Estado | Evidência/decisão |
|---|---|---|
| Dashboards e catálogo de relatórios | parcial | Central existia; cobertura era estado vazio e foi integrada ao template novo. Dashboards paralelos não foram criados. |
| Plantões, vagas, atribuições e confirmações | validada no código/DDL; runtime não executado | `plantoes.vagas` e `escalas.status`; vaga é unidade de cobertura, plantão é o evento. |
| Execução, conferência e correções | validada no código/DDL; runtime não executado | `medico_checkins` e última situação canônica; correção pendente não multiplica presença. |
| Cobertura e substituições | implementada sem execução nesta rodada | efetivação exige `status=CONFIRMADA` e `nova_escala_id`. |
| Ocorrências | validada por implementação anterior; fora dos indicadores | ocorrência não foi tratada como ausência comprovada. |
| Apuração, fechamento e pagamentos | validada no código/DDL; runtime não executado | fechamento e pagamento permanecem etapas distintas. |
| Central de Pendências | validada por implementação anterior | ação permanece nos comandos canônicos; relatório apenas navega para origem. |
| Permissões e design | parcial | servidor valida relatório, financeiro, tenant, unidade e profissional. Lookups amigáveis ainda são lacuna. |

## Dicionário/matriz dos indicadores

| Indicador | Finalidade | Fonte | Cálculo | Filtro temporal | Permissão | Detalhamento | Teste |
|---|---|---|---|---|---|---|---|
| Vagas planejadas | dimensionar demanda | `plantoes.vagas` | soma de vagas | início do plantão `[início,fim+1)` | `RELATORIOS_VER` + escopo | plantões | contrato |
| Vagas atribuídas | ocupação atual | `escalas` | atribuições ativas | início do plantão | idem | plantões atribuídos | contrato |
| Vagas confirmadas | compromisso confirmado | `escalas.status` | confirmada/realizada | início do plantão | idem | plantões | contrato |
| Vagas descobertas | necessidade acionável | fontes acima | `max(vagas-atribuições,0)` | início do plantão | idem | plantões descobertos | contrato |
| Substituições efetivadas | troca concluída | `substituicoes_plantao` | confirmada com nova escala | plantão relacionado | idem | origem | contrato |
| Execuções registradas | presença registrada | `medico_checkins` | uma presença com check-in | início do plantão | `RELATORIOS_VER` | conferência | contrato |
| Incompletas/aguardando/aprovadas | etapa atual | `status_conferencia` | uma presença por estado | início do plantão | idem | conferência | contrato |
| Correções pendentes | decisão necessária | presença em `CORRECAO_PENDENTE` | uma presença, não quantidade histórica de solicitações | início do plantão | idem | conferência canônica | contrato |
| Valores apurados | apuração operacional | `fechamento_plantao.valor_apurado` | soma no nível do fechamento | competência pelo início do plantão | `FINANCEIRO_VER` | fechamento | contrato |
| Valores fechados | consolidação | fechamento | soma apurada de FECHADO/CONCLUIDO | competência | financeiro | fechamento | contrato |
| Valores pagos | desembolso | `pagamentos` | soma `valor_pago`, sem juntar histórico | data do pagamento | financeiro | listagem (lacuna: detalhe pago dedicado) | contrato |
| Ajustes | efeito de contestação | `pagamento_contestacoes` | resolvido menos original | resolução no período | financeiro | fechamento | contrato |
| Pendências impeditivas | bloquear consolidação | `fechamento_divergencias` | abertas, sem somar dinheiro | competência do fechamento | financeiro | fechamento | contrato |

Não há percentual nesta versão: nenhum denominador foi apresentado implicitamente; portanto não há crescimento artificial nem divisão por zero. Valores apurado, fechado e pago são etapas do mesmo fluxo e nunca são somados como total geral.

## Jornada e consistência

Os três relatórios compartilham período inclusivo na interface, convertido para intervalo semiaberto no servidor. O critério inclui plantão iniciado no período mesmo que termine após meia-noite. Resumo e detalhe são lidos na mesma conexão e informam horário de atualização, mas não prometem snapshot imutável. Paginação limita apenas linhas, nunca os totais. Ordenação é fixa e estável por data e UUID. Filtros, retorno por query string, impressão e CSV são preservados. O CSV inclui BOM UTF-8, ponto-e-vírgula, escape e neutralização de fórmulas.

## Auditoria de formulários desta rodada

| Interface | Binding | Autorização | Serviço/consulta | Resposta |
|---|---|---|---|---|
| filtros dos 3 relatórios | datas e GUIDs via MVC/API | permissão + tenant + recurso | SQL Dapper parametrizado | erro distinto de vazio |
| card/detalhe | situação na query string | revalidada a cada GET | mesmo predicado de escopo | filtros preservados |
| exportação CSV | mesmos filtros | revalidada na geração | mesma consulta, limite atual 5.000 | arquivo privado na resposta autenticada |
| impressão | navegador | página já autorizada | sem mutação | navegação/ações ocultas |

Nenhum INSERT/UPDATE/DELETE foi adicionado. A auditoria não declara os demais formulários do produto validados.

## Desempenho, execução e limitações

As consultas agregam atribuições antes de combiná-las, não juntam históricos/pagamentos aos fechamentos e usam paginação estável. O volume máximo síncrono do CSV está limitado a 5.000 linhas nesta entrega; exportação integral/assíncrona é lacuna explícita, não simulada. Não foi criado índice sem plano comprovado.

O contêiner não possui SDK .NET, PostgreSQL nem navegador configurado. Assim, build, testes, plano/tempo real, E2E autenticado e capturas da aplicação real não puderam ser executados. A evidência visual permanece indisponível; não se declara homologação. Os campos de unidade/especialidade/profissional usam GUID porque um lookup canônico autorizado não foi confirmado. O detalhe `PAGO` dedicado ainda não é exposto, por isso a lista financeira principal é a fonte disponível. Situação histórica não é reconstruída.
