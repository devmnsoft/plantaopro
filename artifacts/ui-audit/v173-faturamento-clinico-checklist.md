# Checklist Faturamento Clínico — v1.73.0

- [x] Um único `FaturamentoClinicoController`, dedicado e baseado em `BaseWebController`.
- [x] Actions existentes migradas e rotas preservadas.
- [x] Index consulta `api/v115/faturamento/contas-receber` com token do usuário.
- [x] Sem massa fictícia; falha e lista vazia têm estados honestos.
- [x] Competência, origem, valor previsto, status e pendência são exibidos somente a partir da resposta real.
- [x] Campos não fornecidos pela API não são inventados.
- [x] Tabela responsiva e cards mobile.
- [x] Links reais para Consultas, Financeiro e Pagamentos.
- [ ] Aprovar, gerar cobrança e enviar para pagamento: não expostos no Index porque faltam contratos seguros de autorização/conferência para essas ações.
- [ ] Homologação visual: depende do runtime .NET e de sessão autenticada real.
