# Fechamento e demonstrativos — evolução funcional e de design

**Base registrada:** `efbe763` (branch de trabalho limpa no início). Esta rodada preservou o domínio canônico `fechamento_plantao`, `fechamento_plantao_escalas`, `pagamentos`, `financeiro_pagamento_origem` e `pagamento_contestacoes`; não criou um financeiro paralelo.

## Matriz curta

| Regra | Implementação encontrada / situação | Lacuna tratada | Teste |
|---|---|---|---|
| Execução elegível | Check-in possui conferência `APROVADA`; fechamento já tinha transação e locks. Parcial. | Geração agora restringe tenant/cliente e exige conferência aprovada (ausência continua explícita). | `Fechamento_DeveExigirTenantEExecucaoConferida` |
| Snapshot e idempotência | Itens preservam horas/valores; índices únicos protegem plantão, escala e origem; financeiro usa `FOR UPDATE`. Validado por inspeção. | Mensagem explicita conflito/inelegibilidade; retry de financeiro retorna o documento existente. | contratos v1.87/v2.05 e v2.16.9 |
| Estados | `ABERTO → EM_CONFERENCIA ↔ COM_DIVERGENCIA → AGUARDANDO_APROVACAO → APROVADO → FINANCEIRO_GERADO`; rejeição/devolução preservam histórico. Implementado. | Ajuda diferencia aprovação, obrigação e pagamento. Reabertura não é oferecida: correção ocorre por ajuste/contestação rastreável. | `Csv_DeveNeutralizarFormulaEViewsDevemExplicarUso` |
| Demonstrativo próprio | Portal médico já filtrava pelo médico vinculado. Parcial. | Exibe horas, regra histórica materializada, apurado, aprovado, pago, saldo canônico, fechamento e contestação. | `Demonstrativo_DeveUsarValoresPersistidosESaldoCanonico` |
| Divergência | Tabela e índice de solicitação ativa já existiam, sem jornada profissional. Parcial. | POST valida autoria pelo vínculo, persiste solicitação atômica e não altera valor. | `Divergencia_DeveSerAutorizadaPorPropriedadeEPreservarValor` |
| Impressão/exportação | CSV autorizado e tela básica existiam. Parcial. | Impressão responsiva oculta ações e identifica documento não fiscal; CSV neutraliza fórmulas. | contrato v2.16.9 |

## Auditoria dos formulários alterados

| Operação | Interface → binding | Autorização → serviço → SQL | Persistência → consulta → feedback |
|---|---|---|---|
| Solicitar análise | Textarea 10–500 + antiforgery → `pagamentoId`/justificativa | role Médico + vínculo `medicos.usuario_id` → transação → insert parametrizado | índice único impede ativa equivalente → listagem lê último estado → mensagem não promete ajuste |
| Ações de fechamento | form existente + antiforgery + trava de submit | roles por ação + tenant/cliente → locks e transição condicional | histórico na mesma transação → detalhe/timeline → conflito exige revisão |
| Exportar | filtros GET preservados | role financeiro/admin + contexto no serviço → consulta parametrizada | download privado em resposta autenticada; texto neutralizado contra fórmula |

## Totais, período e estados

Na central, o período significa **data de execução** (`plantoes.data_inicio`). A consulta é limitada a 500 registros e a tela soma esse conjunto retornado, não uma página visual; não há join multiplicador nos totais porque divergências e profissionais são subconsultas agregadas. Uma falha retorna alerta, não KPIs simulados. “Apurado”, “aprovado para pagamento” e “pago” permanecem estados distintos. O saldo mostrado é `max(valor aprovado/apurado/base − valor pago, 0)`, sobre os campos canônicos persistidos.

## Limitações verificáveis

- Não foi criada competência multi-plantão: o agregado canônico continua por plantão. A tela explicita data de execução para não misturar critérios.
- Não há política de prazo persistida para contestação; nenhum prazo foi inventado.
- Reabertura não é exposta nesta entrega porque o modelo não representa versão imutável completa para ela; correções seguem por contestação/ajuste, preservando o consolidado.
- O ambiente não possui .NET SDK, PostgreSQL nem navegador configurado; build, testes integrados, instalação limpa e evidência visual da aplicação real não puderam ser executados. Foram executadas verificações estáticas disponíveis, sem alegar homologação.
