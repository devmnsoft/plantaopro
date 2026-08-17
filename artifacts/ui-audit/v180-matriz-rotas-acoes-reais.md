# Matriz de rotas e ações reais — v1.80.0

| Rota | Ação real | Condição | Desabilitada com motivo |
|---|---|---|---|
| `/FaturamentoClinico` | listar/filtrar/abrir consulta | API e AtendimentoId | aprovar, glosar, exportar |
| `/Financeiro` | listar/detalhar/confirmar/cancelar | autorização, ID e status | glosa, repasse e ações fora do detalhe |
| `/Pagamentos` | listar/filtrar | API financeira | pagar, contestar, comprovante e exportar sem contrato |
| `/Fechamentos` | consultar fonte | dados operacionais | mutações e financeiro sem vínculo |
| `/Relatorios` | abrir relatórios implementados | rota e autorização | biblioteca financeira sem endpoint |
