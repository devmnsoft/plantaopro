# Checklist clínico-financeiro — v1.74.0

- [x] Conta clínica consumida de `api/v115/faturamento/contas-receber`.
- [x] Competência, origem, valor previsto e status usam somente resposta real.
- [x] Campos não fornecidos (paciente, profissional, convênio, procedimento, aprovado, pago, glosas, repasses e histórico) são identificados honestamente.
- [x] Origem abre a consulta somente quando `AtendimentoId` existe.
- [x] Financeiro clínico usa rota MVC e endpoint existentes.
- [x] Aprovação, glosa e exportação permanecem desabilitadas, com motivo, pois não há contrato seguro por conta nesta tela.
- [x] Pagamentos não convertem `ValorPago` ausente em zero.
- [x] Empty states não criam registros ou totais fictícios.
- [ ] Build, teste, banco, autenticação e HTTP: bloqueados pela ausência do SDK/runtime .NET.
