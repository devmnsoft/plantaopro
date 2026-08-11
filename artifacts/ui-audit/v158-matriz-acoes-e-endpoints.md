# Matriz de ações e endpoints v1.58

| Módulo | Ação | Rota/controller | Endpoint/API | Permissão | Confirmação | Feedback | Status |
|---|---|---|---|---|---|---|---|
| Pendências | Mover | `OperationBffController` | `POST api/work-items/{id}/mover` | autenticação + escopo tenant | não | toast + aria-live | implementado |
| Pendências | Assumir | `WorkItemsController.Take` | `POST api/work-items/{id}/assumir` | autenticação + escopo tenant | não | toast/403/409 | implementado |
| Pendências | Resolver | `WorkItemsController.Complete` | `POST api/work-items/{id}/concluir` | autenticação + escopo tenant | sim, modal | toast/403/409 | implementado |
| Pendências | Adiar | `WorkItemsController.Postpone` | `POST api/work-items/{id}/adiar` | autenticação + escopo tenant | confirmação explícita do prazo | toast/403/409 | implementado |
| Pendências | Reabrir | `WorkItemsController.Reopen` | `POST api/work-items/{id}/reabrir` | autenticação + escopo tenant | não | toast/403/409 | implementado |
| Pendências | Comentar | `WorkItemsController.Comment` | `POST api/work-items/{id}/comentar` | autenticação + escopo tenant | não | loading + toast | implementado |
| Pendências | Timeline | `WorkItemsController.History` | `GET api/work-items/{id}/historico` | autenticação + escopo tenant | n/a | loading/erro/vazio | implementado |
| Pendências | Reatribuir | `WorkItemsController.Forward` | `POST api/work-items/{id}/encaminhar` | autenticação + escopo tenant | recomendada | — | pendente: falta catálogo de responsáveis elegíveis |
| Fechamentos | Aprovar e gerar financeiro | controllers operacionais existentes | contrato agregado ausente | por definir no agregado | sim | — | pendente |
| Pacientes | Timeline longitudinal mascarada | controllers clínicos existentes | contrato agregado ausente | LGPD + perfil clínico | conforme ação | — | pendente |
| Agendamentos | Check-in | controllers Saúde 360 existentes | rota real específica existente | autenticação | não | existente na tela própria | implementado na tela, drawer pendente |
| Triagem | Finalizar | controllers Saúde 360 existentes | rota real específica existente | perfil clínico | sim | existente na tela própria | implementado na tela, drawer pendente |
| Consultas | Finalizar | `ConsultasWorkspaceController` | endpoint versionado existente | perfil médico | sim | existente no workspace | implementado na tela, drawer pendente |
| Financeiro/Pagamentos | Aprovar/Pagar | controllers financeiros existentes | rotas específicas existentes | perfil financeiro | sim | existente nas telas | drawer agregado pendente |
| Convites | Reenviar/Cancelar | controllers operacionais existentes | tentativas não projetadas | coordenação | sim ao cancelar | — | pendente |
| Relatórios | Gerar/Exportar | `RelatoriosController` | actions por relatório | autorização da action | conforme formato | resposta HTTP | implementado nas telas aplicáveis |
| Configurações | Alterar parâmetro sensível | controllers específicos | actions existentes por card | administração | sim | PRG/toast | implementado nas telas aplicáveis |
