# Análise da Jornada Operacional MVP — v1.79.0

| Tela | Rota | Controller/action | Estado atual | Regra esperada | Ação real | Sem backend | Correção | Pendência |
|---|---|---|---|---|---|---|---|---|
| Plantões | `/Plantoes` | `Plantoes.Index` | API paginada | cobertura/risco só de dados | filtrar, detalhe, criar | fechamento sem vínculo | badges e próxima ação | conflito não está no DTO |
| Plantão | `/Plantoes/Details/{id}` | `Details`, `Publicar`, `Cancelar` | transições reais | mínimos para publicar; motivo ao cancelar | publicar/cancelar/editar | fechamento e financeiro | habilitação e motivos visíveis | API não retorna fechamento |
| Convites | `/Convites?plantaoId=` | `Convites.Index` | leitura por plantão | não presumir resposta/expiração | listar e abrir plantão | reenviar | KPI só com dados; reenvio bloqueado | aceitar/recusar não expostos aqui |
| Escalas | `/Escalas` | `Escalas.Index` | API paginada | médico, plantão, status e unidade | filtrar e abrir detalhe | pagamento sem vínculo | próxima ação e ausência explícita | conflito/presença não vêm no resumo |
| Escala | `/Escalas/Details/{id}` | `Confirmar`, `Recusar`, `MarcarRealizado` | endpoints reais por status | motivo em recusa/substituição | transições condicionais | fechamento/pagamento sem ID | validação server-side de motivo | homologar transições no runtime |
| Substituição | `/Escalas/Substituir/{id}` | GET/POST `Substituir` | endpoint real | médico real e motivo | enviar substituição | histórico/aprovação separados | validação e ajuda acessível | seletor de médicos não contratado |
| Fechamentos | `/Fechamentos` | `Fechamentos.Index` | BFF retorna estado vazio honesto | realizado → conferência → aprovação | atualizar/filtro | mutações e financeiro | regras e ações bloqueadas | backend transacional de fechamento |
| Financeiro | `/Financeiro` | `Financeiro.Index` | rota real independente | só abrir com vínculo | consultar módulo | vínculo pelo fechamento | limite explicitado | contrato FechamentoId |
| Pagamentos | `/Pagamentos` | `Pagamentos.Index` | API real | não converter ausente em zero/pago | consultar pagamentos | vínculo ausente | navegação não presumida | contrato financeiro operacional |
