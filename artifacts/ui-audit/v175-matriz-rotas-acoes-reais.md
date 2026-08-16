# v1.75.0 — matriz de rotas e ações reais

| Origem | Ação | Destino/contrato | Estado |
|---|---|---|---|
| Consultas | Abrir faturamento | `FaturamentoClinico.Index` | ativa; rota real |
| Consulta detalhada | Abrir financeiro | `ClinicaFinanceiro.ContasReceber` | ativa; controller/action reais |
| Faturamento | Abrir consulta | `Consultas.Details(id)` | ativa apenas com `AtendimentoId`; senão desabilitada com motivo |
| Faturamento | Abrir financeiro clínico | `ClinicaFinanceiro.ContasReceber` | ativa; rota real |
| Faturamento | Abrir Financeiro/Pagamentos | respectivas actions `Index` | ativas; rotas reais |
| Faturamento | Aprovar/glosar/exportar | endpoint não presente no contrato consumido | desabilitada com motivo |
| Pagamentos | Revisar financeiro | `Financeiro.Index` | ativa; rota real |
| Financeiro | Detalhes | `Financeiro.Details(id)` | ativa; action real |
| Financeiro | Confirmar/cancelar | backend operacional não comprovado nesta tela | desabilitada com explicação acessível |

Nenhuma rota foi inferida a partir de texto: os destinos foram conferidos nas declarações de controller/action do repositório.
