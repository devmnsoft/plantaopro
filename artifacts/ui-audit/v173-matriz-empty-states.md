# Matriz de empty states — v1.73.0

| Superfície | Condição real | Resposta |
|---|---|---|
| Faturamento Clínico | API retorna coleção vazia | Informa que não há contas no tenant e oferece revisar Consultas |
| Faturamento Clínico | API indisponível ou resposta negada | Exibe a mensagem tratada pelo BFF e ação para tentar novamente |
| Faturamento Clínico | Campo não retornado | Mostra “não informado pela API”; não gera valor substituto |
| Faturamento Clínico | Ação sem contrato seguro | Ação não é apresentada como funcional |
| Minha Assinatura | Sem assinatura retornada | Estado honesto preservado da v1.72 |
