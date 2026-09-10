# PlantãoPro v2.15.7 — Central operacional, escalas e invariantes

## Escopo e diagnóstico verificável

Base inicial: commit `550fed7`, após a entrega v2.15.6. A inspeção foi feita no código e no schema, e não inferida por título de PR. A troca de cliente emite identidade renovada com `tenant_id`, `cliente_id`, `session_id` e contexto selecionado; o middleware consulta a sessão persistida a cada requisição, e a Central de Segurança lê `auth_sessoes`, incluindo revogação e motivo. Portanto a jornada reutiliza essa fronteira em vez de criar autenticação paralela.

O baseline local não pôde compilar: `dotnet` e `psql` não estão instalados no container. Isso é limitação preexistente do ambiente, anterior a qualquer alteração. A validação Python dos manifestos era executável e foi usada durante a rodada.

## Fluxo canônico e consumidores

| Interface/consumidor | Controller | Serviço/query canônica | Autorização |
|---|---|---|---|
| Central de escala | `CentralEscalaController` | `OperacaoService.GetResumoAsync` / `PlantaoService.GetAllAsync` | papéis de plantão, escala ou financeiro + contexto |
| Gestão de plantões | `PlantoesController` | `PlantaoService` | `PLANTOES_GESTAO` + guarda de assinatura na publicação |
| Gestão de escalas | `EscalasController` | `EscalaService` | `ESCALAS_GESTAO`; ações do profissional mantêm vínculo autenticado |
| Convite mobile | `MobileController` | reivindicação idempotente do convite + `EscalaService.AceitarAsync` | médico autenticado, vínculo e módulo mobile |
| Financeiro | controllers financeiros existentes | `FinanceiroService.GerarAsync` | módulo/perfil financeiro e contexto atual |

`PlantaoService`, `EscalaService` e `FinanceiroService` permanecem os fluxos canônicos. Rotas sinônimas (`aceitar/solicitar`, `realizar/marcar-realizado`, `nao-compareceu/ausencia`) continuam encaminhadas ao mesmo método para preservar contratos. Implementações B2B/Fase4 são automações e projeções; não substituem a escrita transacional canônica.

## Matriz de transições

| Estado inicial | Ação | Perfil | Validações | Estado final | Efeitos persistidos |
|---|---|---|---|---|---|
| rascunho | publicar | gestão de plantões | módulo, unidade/especialidade ativas, intervalo, valor, capacidade | aberto | status, histórico, auditoria |
| aberto/em_escala | solicitar/aceitar | médico elegível | vínculo ativo, CRM/UF, vaga, disponibilidade, conflito global | solicitado | escala e histórico; vaga ainda não consumida |
| convite pendente | aceitar convite | médico destinatário | não expirado, reivindicação atômica, mesmas revalidações da solicitação | convite aceito + escala solicitada | convite, escala, histórico e auditoria; repetição não produz efeito |
| solicitado | confirmar | gestão de escalas | escala bloqueada, plantão bloqueado, vaga, elegibilidade e conflito revalidados | confirmado | decremento condicional da vaga na mesma transação, histórico e notificação |
| solicitado | recusar | gestão/profissional autorizado | justificativa | recusado | status, recomposição do status do plantão, histórico |
| confirmado | cancelar | gestão/profissional autorizado | justificativa; operação única pelo lock/estado | cancelado | vaga liberada com teto da capacidade, histórico |
| confirmado | substituir | gestão autorizada | justificativa, substituto distinto/ativo/elegível/sem conflito | substituído + nova confirmada | ocupação trocada sem alterar vaga, dois históricos |
| confirmado | realizar | gestão autorizada | estado confirmado | realizado | status e histórico; habilita pagamento |
| confirmado | não compareceu | gestão de escalas | justificativa | nao_compareceu | status/histórico; não habilita pagamento |
| realizado | gerar pagamento | financeiro autorizado | mesmo tenant, origem sem pagamento ativo | pendente | pagamento, histórico, notificação e auditoria |

## Regras fechadas nesta evolução

* A capacidade é invariável no banco: positiva, disponível entre zero e total, valor não negativo e fim posterior ao início.
* Edição não recalcula vagas cegamente: conta ocupações confirmadas e bloqueia redução incompatível. Mudanças de unidade, especialidade, período e valor são recusadas após confirmação para obrigar o fluxo histórico.
* Confirmação serializa a escala e o plantão com `FOR UPDATE`; o decremento é condicional. Repetição encontra estado final, e concorrência na última vaga deixa apenas uma confirmação.
* Convite é reivindicado por atualização condicional antes do aceite. Expiração e repetição retornam conflito; a constraint parcial também impede convite pendente e ocupação ativa duplicados.
* Cancelamento de escala confirmada libera uma vaga somente na transição original. Substituição exige escala confirmada, outro profissional elegível e sem conflito, preservando a ocupação e ambos os históricos.
* O conflito é global por profissional entre organizações, pois segurança assistencial prevalece. A resposta revela apenas que existe conflito, sem unidade, horário ou tenant externo.
* Pagamento consulta a escala realizada dentro do cliente atual e bloqueia a origem. O índice único e o tratamento de `23505` fecham a concorrência; publicação, solicitação, confirmação e ausência não são elegíveis.
* Pagamentos confirmados não são apagados nem reescritos por esta jornada; ajustes/estornos continuam no módulo financeiro existente.

## Testes e evidências

Planejados para PostgreSQL descartável/CI: última vaga concorrente, convite expirado, duplo aceite, sobreposição pela desigualdade `inicioNovo < fimExistente && fimNovo > inicioExistente`, cancelamento repetido, substituição, pagamento duplicado, dois tenants, ID de outro tenant, módulo ausente, médico inativo e formulário anterior à troca de contexto.

Neste agente, builds Debug/Release, testes .NET, migration real e navegador executável ficaram **não executados**, porque não há SDK .NET nem cliente/servidor PostgreSQL. Consequentemente não há captura visual legítima; preview estático não foi usado como evidência. A entrega não declara homologação. O CI deve executar os testes de contrato v2.15.7 e os testes PostgreSQL acima antes do merge.

## Limitações conscientes

Esta rodada prioriza as invariantes transacionais críticas. A projeção visual existente da Central Operacional foi preservada, sem inventar totais nem endpoints. Refinamentos adicionais de tabela/mobile e screenshots dependem da aplicação executável. O schema adiciona a expiração, mas convites antigos sem prazo continuam válidos por compatibilidade; novos emissores devem preencher `expira_em` conforme a política comercial vigente.
