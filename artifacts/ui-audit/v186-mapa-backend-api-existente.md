# Mapa backend/API existente — v1.86.0

| Área | Rotas/serviço | Persistência | Avaliação |
|---|---|---|---|
| Pagamentos médicos | `FinanceiroController`, `PagamentosController`, `FinanceiroService` | `plantaopro.pagamentos`, `historico_pagamento`, auditoria e notificações | geração/baixa/cancelamento existentes; baixa canônica e contestação hardened |
| Financeiro clínico | controllers Saude360/consulta | `clinica_contas_receber` e tabelas clínicas | origem real, mas não é o mesmo agregado de pagamentos médicos |
| Fechamentos | Web `OperacaoPremium/Fechamentos` | nenhum repository de fechamento identificado | somente visualização honesta; mutações bloqueadas |
| Glosas | API/serviços v115/v116 | estruturas de lote/itens | parcial; não habilitada como resolução genérica |
| Repasses | `V115RepasseMedicoService` | pagamentos/estruturas v115 | contrato próprio preservado, sem aliases |
| Configurações | telas Admin SaaS/Configurações | serviços tenant parciais | nenhum save genérico foi criado |
