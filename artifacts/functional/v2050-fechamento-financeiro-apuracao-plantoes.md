# v2.05.0 — fechamento financeiro e apuração de plantões

## Telas e fluxos

- A tela **Fechamento Operacional** foi consolidada como apuração: período, unidade, profissional, especialidade e status são filtros sem IDs digitáveis.
- A grade apresenta data/horário, unidade, profissional, especialidade, status, valores previsto/aprovado, divergências e ação contextual.
- A conferência preserva horário previsto/realizado, presença, horas, valores, divergência, pagamento e timeline auditável.
- Aprovação, devolução controlada, rejeição motivada e geração financeira seguem transições explícitas. Pagamentos existentes são reutilizados, impedindo duplicidade.
- O endpoint `GET /api/fechamentos/exportar.csv` exporta a visão filtrada do tenant e registra auditoria.

## Status financeiros

O agregado persiste `ABERTO`, `EM_CONFERENCIA`, `COM_DIVERGENCIA`, `AGUARDANDO_APROVACAO`, `APROVADO`, `DEVOLVIDO`, `REJEITADO`, `FINANCEIRO_GERADO` e `CONCLUIDO`. O plantão de origem continua representando execução e cancelamento; a UI traduz os estados do fechamento em badges e etapas.

## Validações, permissões e auditoria

- Períodos parciais ou invertidos são rejeitados (HTTP 422).
- Todas as consultas e mutações incluem tenant e cliente; seletores retornam somente relações presentes no resultado autorizado.
- Conferência exige gestão de escalas; aprovação/rejeição exige administração; geração e exportação exigem gestão financeira ou administração.
- Plantão inexistente/de outro tenant não é encontrado; apenas realizado gera fechamento; divergências abertas bloqueiam aprovação; transições inválidas e concorrentes retornam conflito.
- Rejeição aceita somente motivo da lista controlada. A regra financeira permanece `PlantaoPaymentCalculator`; nenhuma fórmula foi alterada.
- Histórico registra conferência, divergência, aprovação, rejeição e geração financeira. A exportação registra evento no serviço de auditoria sem conteúdo sensível.

## Testes criados/ajustados

- `V2050FechamentoFinanceiroContractTests` cobre escopo tenant, filtros sem ID manual, transições, motivo controlado, idempotência financeira, autorização e auditoria da exportação.
- Os testes de workflow e contratos financeiros anteriores foram preservados.

## Comandos executados

Os comandos obrigatórios de segurança, compatibilidade, validação, restore, build, testes, buscas de IDs/segredos e `git diff --check` foram executados na entrega; os resultados constam no resumo final da PR.

## Limitações

- Não foi criada integração bancária nem PDF, pois o ciclo atual possui baixa textual/CSV e não há necessidade de improvisar infraestrutura.
- A apuração usa os horários executados persistidos quando disponíveis; na ausência, mantém a evidência operacional existente sem inventar dados.
