# PlantãoPro v2.16.0 — triagem, consulta e design da jornada

## Escopo e base efetivamente inspecionada

A base é o merge `7074339` da v2.15.9. A inspeção foi feita sobre implementações, SQL e testes — não sobre o título do PR. Pacientes e agenda já possuíam persistência tenant-aware; check-in criava registros idempotentes em `agendamento_checkins`, `painel_chamada_fila` e `triagem_fila`; fila/chamada tinham histórico. Foi identificado que a migration v2.15.9 existia, mas não integrava a cadeia canônica dos manifests, portanto uma instalação real não comprovava aquelas constraints. Ela agora faz parte da cadeia antes da v2.16.0.

Caminhos canônicos reutilizados: `Saude360ClinicalService` para recepção/triagem, `ConsultaApplicationService` e `ConsultaRepository` para o workspace médico, tabelas `pacientes`, `agendamentos`, `atendimentos_fila`, `triagens`, `triagem_encaminhamentos`, `consultas`, `consulta_historico` e `consulta_adendos`. Nenhuma fila ou prontuário paralelo foi criado.

## Estados, identidade e efeitos

| Entidade | Estados relevantes | Responsável/efeito |
|---|---|---|
| Agendamento | AGENDADO → CONFIRMADO → CHECKIN_REALIZADO → EM_TRIAGEM/AGUARDANDO_CONSULTA → EM_ATENDIMENTO → ATENDIDO | Recepção confirma/check-in; transições inválidas são rejeitadas. |
| Entrada operacional | AGUARDANDO → CHAMADO/EM_ATENDIMENTO → FINALIZADO | Chamar não inicia consulta. O índice por `(cliente_id, agendamento_id)` impede duas entradas ativas. |
| Triagem | AGUARDANDO → EM_ANDAMENTO → FINALIZADA | Enfermagem registra rascunho; finalização registra encaminhamento único. Ausência permanece `NULL`. |
| Consulta | AGUARDANDO → EM_ATENDIMENTO → RASCUNHO → FINALIZADA | Médico assume explicitamente; `versao` arbitra gravações. Finalização é transacional e idempotente por estado/versão. |
| Retificação | FINALIZADA → adendo imutável | Motivo, autor, horário e hash são preservados sem sobrescrever a consulta. |

O identificador clínico é `atendimento_id`, ligado a tenant, unidade, paciente e opcionalmente agendamento. Homônimos nunca participam da associação. A migration adiciona autoria/horário de assunção/finalização, versão da triagem, snapshot JSONB da triagem vinculada à consulta e índices estáveis de fila/histórico.

## Regras clínicas corrigidas

* Valores ausentes continuam nulos e IMC só é calculado com peso e altura positivos (kg/m², arredondamento decimal para duas casas).
* Valor extremo deixa de ser bloqueado por uma faixa inventada: é retornável como alerta de conferência profissional. Erro sintático/unidade continua responsabilidade do binding/validação decimal.
* Classificação de risco permanece decisão explícita e obrigatória do profissional; nada é inferido dos sinais.
* Diagnóstico definitivo e CID deixam de ser impedimentos universais. São alertas de revisão; paciente, médico, atendimento, anamnese e conduta permanecem requisitos estruturais atuais.
* “Não informado” não é apresentado como “sem alergias”. O workspace não usa `localStorage`, alerta sobre saída com alterações pendentes, conserva o formulário diante de 409 e só confirma gravação depois da resposta do servidor.
* Respostas do workspace clínico recebem `no-store`; auditoria registra ação e identificadores, não o texto clínico.

## Matriz de acesso

| Ação/dado | Recepção | Financeiro | Enfermagem/triagem | Médico | Admin local | Superadmin assistido |
|---|---:|---:|---:|---:|---:|---:|
| Estado operacional/fila | permitido por política/unidade | metadados mínimos | permitido por vínculo | permitido por vínculo | escopo do tenant | escopo global auditado |
| Conteúdo da triagem | não | não | criar/editar/finalizar por política | leitura vinculada | somente permissão clínica explícita | acesso assistido identificado e auditado |
| Evolução médica | não | não | não por padrão | editar consulta assumida | somente permissão clínica explícita | acesso assistido identificado e auditado |
| Finalizar/retificar | não | não | triagem | consulta/adendo | não implica privilégio clínico | política global específica e auditoria |

A API aplica políticas nos endpoints sensíveis e filtra tenant nas consultas por ID. A interface não é usada como barreira de autorização. Revogação de política passa a valer na requisição seguinte.

## Cenários de regressão

Cobertura automatizada adicionada para: ausência versus zero, extremo como alerta, regra de diagnóstico/CID, optimistic locking existente, unicidade de atendimento/encaminhamento, snapshot, `no-store`, conflito e proteção de alterações pendentes. Os cenários PostgreSQL/E2E requeridos (dois tenants, vínculo, repetição, retomada, retificação, troca de contexto e reload) devem rodar no workflow com PostgreSQL e aplicação reais.

## Evidências e limitações desta execução

O executor não dispõe de SDK .NET, Docker ou `psql`; logo não foram declarados build, teste integrado, E2E, screenshot da aplicação real nem homologação PostgreSQL. O gerador oficial do SQL consolidado e os validadores Python disponíveis foram executados. A CI deve executar Debug/Release, suíte xUnit, instalação limpa/upgrade idempotente, E2E autenticado por perfil, teclado/mobile/desktop e captura real antes de homologar.
