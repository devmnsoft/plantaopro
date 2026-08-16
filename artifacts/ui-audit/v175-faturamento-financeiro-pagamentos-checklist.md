# v1.75.0 — checklist Faturamento → Financeiro → Pagamentos

- [x] Faturamento usa somente itens da API `api/v115/faturamento/contas-receber`.
- [x] Valor, status, origem, competência e convênio ausentes são apresentados como não informados.
- [x] Filtros de status, competência e convênio não fabricam opções nem dados.
- [x] Empty state distingue ausência na API de ausência após filtros.
- [x] Origem abre consulta somente quando `AtendimentoId` existe.
- [x] Financeiro clínico aponta para controller/action existentes.
- [x] Aprovação, glosa e exportação permanecem desabilitadas com motivo porque este contrato não oferece endpoints.
- [x] Pagamentos e Financeiro não convertem ausência de `ValorPago` em zero visual.
- [x] Tabelas possuem wrapper e Faturamento possui cards mobile.
- [ ] Restore/build/test/runtime: bloqueados por ausência do SDK .NET.
- [ ] Smoke visual autenticado: aguarda aplicação real e storage state.
