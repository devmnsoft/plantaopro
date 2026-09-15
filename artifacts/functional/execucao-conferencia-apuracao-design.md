# Evolução — execução, conferência e apuração rastreável

## Base examinada

Commit inicial preservado: `094a64408e9512ac79c73d13b3e412bfaaddcf6f` (`Merge pull request #479 ... auditoria de formulários`), com árvore limpa. Não havia `AGENTS.md` no repositório, em seus ascendentes ou até profundidade 2 na raiz. Foram revistos o histórico recente, `.github/workflows/dotnet-ci.yml`, a auditoria `artifacts/quality/auditoria-crud-formularios-template.md` e os relatórios v2.16.7–v2.16.9.

## Fontes canônicas e estados

| Dado | Fonte canônica | Estados/referência |
|---|---|---|
| identidade e contexto SaaS | claims validados por `ICurrentUserService`, vínculo ativo em `medicos`, guardas de tenant/unidade | usuário autenticado + tenant/cliente explícitos |
| plantão e atribuição | `plantaopro.plantoes` + `plantaopro.escalas` | atribuição confirmada/realizada e `reg_status='A'` |
| execução | `medico_checkins` | `REGISTRO_INCOMPLETO` → `PENDENTE` → `APROVADA`; `CORRECAO_PENDENTE`; `AJUSTE_POS_APURACAO` |
| correção | `medico_presenca_correcoes` | `PENDENTE` → `APROVADA`, `RECUSADA` ou `CANCELADA`; originais nunca são sobrescritos |
| trilha de decisão | `medico_presenca_historico` | eventos com antes/depois, motivo, ator e instante do banco |
| conferência | a própria presença e a correção mais recente, sem tabela paralela | decisão protegida pelas versões de ambas as linhas |
| apuração | `fechamento_plantao` + `fechamento_plantao_escalas` | aberto, conferência, divergência, aprovação e conclusão |
| obrigação/pagamento | `pagamentos` + `financeiro_pagamento_origem` + `historico_pagamento` | previsto, apurado, aprovado e pago permanecem valores distintos |
| regra remuneratória atual | valor persistido no plantão e `PlantaoPaymentCalculator` | não foi criada regra, desconto, adicional ou arredondamento novo |
| notificações/auditoria | `notificacoes`, `medico_presenca_historico` e `IAuditService` | eventos somente depois de mutações efetivas |

## Matriz etapa × implementação × regra × lacuna × aceite

| Etapa | Implementação verificada/evoluída | Regra | Lacuna remanescente | Teste de aceite |
|---|---|---|---|---|
| entrada | comando profissional usa `now()` do PostgreSQL, vínculo e atribuição próprios, unicidade por tenant/escala | confirmação de escala não cria presença | execução PostgreSQL local indisponível | entrada válida, duplicada e outro tenant |
| saída | update condicionado à entrada e à ausência de saída; evento e recebimento são exibidos separadamente | saída não pode anteceder entrada; `timestamptz` preserva virada do dia | E2E autenticado pendente | saída válida, anterior e meia-noite |
| correção | intervalo efetivo preserva a ponta omitida; originais e versão-base persistidos | proposta não altera horários aprovados | seletores `datetime-local` dependem do parsing MVC e devem ser exercitados no fuso da unidade | alterar só entrada/só saída e versão antiga |
| cancelamento | novo comando POST versionado, do próprio solicitante, atualiza correção e presença na mesma transação e grava histórico | somente `PENDENTE`; restaura `PENDENTE` ou `REGISTRO_INCOMPLETO`; permite nova solicitação | integração PostgreSQL pendente | cancelar, repetir, cancelar após decisão e solicitar novamente |
| fila | agora parte de todas as presenças, inclusive incompletas e sem correção; filtro de divergência e ordenação estável | estado não é inferido pela cor; incompleta não pode ser aprovada | filtros de unidade/profissional ainda recebem GUID pela tela existente; substituir por autocomplete tenant-scoped em rodada dedicada | presença sem correção, incompleta, filtros e páginas sem repetição |
| decisão | ações nomeadas, versão da presença/correção, lock, tenant/unidade, segregação e contagem de linhas | parâmetro ausente nunca aprova; conflito usa mensagem canônica | concorrência real depende de duas conexões PostgreSQL | aprovação, recusa, repetição e corrida |
| pós-consolidação | horários/valores consolidados são preservados e estado vira pendência | aprovação operacional não paga nem reprocessa histórico | domínio atual registra pendência auditável, mas ainda não possui ajuste financeiro materializado; nenhuma mensagem afirma encaminhamento criado | correção após consolidado sem alterar pagamento |
| apuração | fluxo existente separa previsto/apurado/aprovado/pago e origem tem chave única | usa somente valor persistido; pagamento não nasce da conferência de presença | fechamento legado ainda calcula pela escala planejada e representa falta de configuração por valor zero; precisa evolução transacional específica antes de homologação financeira | regra ausente, regra persistida, repetição, origem e versão |

## Transições canônicas

1. `CONFIRMADO` (atribuição, sem presença) → `REGISTRO_INCOMPLETO` por entrada única do profissional.
2. `REGISTRO_INCOMPLETO` → `PENDENTE` por saída única e coerente.
3. `REGISTRO_INCOMPLETO|PENDENTE` → `CORRECAO_PENDENTE` por proposta versionada com intervalo efetivo válido.
4. Correção `PENDENTE` → `CANCELADA` somente pelo solicitante e versões atuais; a presença volta ao estado derivado da existência da saída.
5. `PENDENTE` → `APROVADA` por gestor autorizado, de outra autoria quando aplicável.
6. Correção `PENDENTE` → `APROVADA|RECUSADA`; aprovação aplica os horários efetivos, recusa preserva os registrados.
7. Havendo origem financeira aprovada/paga, a decisão não muda valores: presença → `AJUSTE_POS_APURACAO` e histórico explicita apenas a pendência.

Toda divergência otimista responde: **“Este registro foi atualizado. Revise os dados antes de decidir.”** Repetição não cria outro histórico de sucesso porque os updates exigem estado e versão anteriores.

## Matriz de formulários atualizada

| Tela/operação | rota Web | API | binding/validação | persistência e confirmação independente | resultado desta rodada |
|---|---|---|---|---|---|
| presença/entrada-saída | `POST /MinhaAgenda/RegistrarPresenca` + antiforgery | `POST /api/medico-area/escalas/{id}/check-in|check-out` | escala, operação e fuso; servidor rejeita vínculo/estado/fuso/duplicidade | insert/update transacional; GET `/presencas` após redirect | revisado; mensagem e horários de recebimento visíveis |
| presença/solicitar | `POST /MinhaAgenda/SolicitarCorrecao` | `POST .../correcoes` | duas pontas opcionais, justificativa e versão; intervalo completo no servidor | correção + versão/status da presença na transação; GET após redirect | revisado; duração efetiva e ponta preservada visíveis |
| presença/cancelar | `POST /MinhaAgenda/CancelarCorrecao` + antiforgery | `POST .../correcoes/{id}/cancelar` | IDs internos ocultos e duas versões; autoria e tenant no servidor | dois updates condicionais + histórico na mesma transação; GET após redirect | implementado, sem DELETE físico |
| conferência/consultar | `GET /ConferenciaExecucao` | `GET /api/conferencia-execucao` | período, unidade, profissional, estado, divergência, página | consulta tenant-scoped, total independente da página | evoluído com incompletas, divergência e ordem estável |
| conferência/decidir | `POST /ConferenciaExecucao/Decidir` + antiforgery | POST explícito de presença ou correção | enum anulável, ação compatível, justificativa e versões | locks, updates condicionais, histórico, commit e nova consulta após redirect | revisado; filtros preservados e conflito uniforme |
| apuração/consultar | telas existentes de fechamento/pagamento | APIs existentes de fechamento/pagamento | IDs obtidos pela listagem | joins por origem e histórico | mapeado; nenhuma persistência financeira simulada |

Não há `alert()`, `confirm()`, `href="#"` nem DELETE novo nas telas alteradas. O botão de mutação usa POST, antiforgery e bloqueio de reenvio; `pageshow` recupera a interface sem presumir o resultado de timeout.

## Interface

As telas reutilizam Bootstrap e os componentes PlantãoPro existentes. “Meus plantões” distingue horário do evento do recebimento, explicita a duração proposta e oferece cancelamento real. A conferência ganhou execução incompleta, filtro recolhível de divergência, paginação que preserva filtros e ajuda contextual. Comparações continuam em `dl` responsivo, virando blocos sequenciais no mobile, sem tabela larga nem biblioteca visual nova.

## Evidências e limitações

Foram adicionados contratos de regressão para inclusão de presença incompleta, ordenação estável, filtro de divergência, cancelamento versionado/autorado/histórico, separação dos instantes e ações acessíveis. Nesta máquina não existem `dotnet`, `psql` ou `docker`; restore, builds, testes .NET, PostgreSQL isolado, E2E autenticado e screenshot da aplicação real não foram executados. Não foi produzida imagem simulada e este relatório não declara homologação nem ausência de defeitos.

O próximo gate obrigatório é executar o workflow com PostgreSQL 16 descartável, incluindo duas conexões concorrentes, e então capturar desktop/mobile da aplicação autenticada. A lacuna financeira descrita na matriz deve ser resolvida antes de considerar o critério de apuração integral homologado; esta rodada deliberadamente não inventa uma nova estrutura ou regra remuneratória.
