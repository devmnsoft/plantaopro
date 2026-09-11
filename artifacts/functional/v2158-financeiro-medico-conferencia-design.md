# PlantãoPro v2.15.8 — financeiro médico, conferência e pagamento registrado

## Diagnóstico e continuidade

A base já contém a apuração canônica em `FechamentoOperacionalService`: geração a partir de plantão realizado, itens por escala, fila filtrável, divergências, conferência, aprovação e geração financeira idempotente. Também contém `FinanceiroService` como consumidor canônico de `plantaopro.pagamentos`, e contestação com índice único de abertura. A implementação preserva esses serviços e mantém isolados `pagamentos` (plantonista), `v113_faturas`/`v115_recebimentos` (clínico) e cobrança SaaS.

Defeitos comprovados corrigidos:

- o pagamento criado pelo fechamento não persistia `tenant_id`/`cliente_id`, embora as leituras atuais exijam esses campos;
- a baixa aceitava um pagamento apenas `pendente`, qualquer valor positivo e não bloqueava o registro por tenant dentro da transação;
- médico podia alcançar pagamento de outro profissional do mesmo tenant pelo vínculo de origem;
- busca do cadastro médico recebia `tenantId`, mas não o aplicava;
- abrir contestação mudava o estado da obrigação e confundia contestação com liquidação;
- DTO de detalhe não distinguia previsto, apurado, aprovado, pago e saldo.

## Contrato e transições reais

1. Plantão e escala precisam estar `realizado` para gerar apuração.
2. Fechamento: `ABERTO → EM_CONFERENCIA ↔ COM_DIVERGENCIA → AGUARDANDO_APROVACAO → APROVADO → FINANCEIRO_GERADO → CONCLUIDO`. Devolução e rejeição continuam no workflow existente.
3. Somente `APROVADO` gera obrigação médica. O snapshot registra valor previsto/apurado/aprovado, horas, valor-hora, modo de cálculo e fechamento de origem.
4. Pagamento é **registrado manualmente**, integral nesta versão, apenas sobre obrigação `aprovado`, sob lock transacional. `pago` significa registro persistido, não comprovação bancária externa.
5. Cancelamento é permitido antes da quitação. Pagamento já registrado não é editado/apagado; ajuste ou cancelamento após quitação exige estorno (a operação de estorno permanece uma limitação explícita desta rodada).
6. Contestação pode ser aberta para obrigação aprovada ou paga, não altera valor nem estado da quitação, é única enquanto aberta e preserva o valor original. Ajuste de item pago é bloqueado até um estorno.

## Autorização e concorrência

Financeiro opera somente no tenant e cliente ativos; o administrador global também precisa de contexto alvo explícito. O médico consulta/contesta apenas por `usuario_id` e tenant. A baixa relê estado, valor aprovado e versão com `FOR UPDATE`; atualização exige estado esperado e incrementa `versao`. Referência manual, quando informada, possui unicidade por tenant/cliente.

## Cálculo e dados

O cálculo canônico continua decimal, com duração real entre início e fim (inclusive virada do dia) e `MidpointRounding.AwayFromZero`. Nenhum adicional, desconto, intervalo ou arredondamento novo foi inventado. A migration incremental adiciona snapshots e constraints sem alterar migrations aplicadas; o SQL consolidado e os relatórios do manifesto foram regenerados pelo gerador oficial.

## UX

O detalhe passou a oferecer **Registrar pagamento** somente para obrigação aprovada e informa que a origem é manual. A tela mantém chave Pix mascarada, valores/datas em apresentação brasileira, modal acessível já provido pelo componente de formulários e mensagens somente após resposta persistida. O texto para item pago esclarece que contestação não desfaz a quitação.

## Verificação e limitações

O SDK .NET não está instalado neste agente, portanto builds Debug/Release, testes .NET e E2E com aplicação real não puderam ser executados localmente. Também não havia PostgreSQL/aplicação disponível para prova transacional, concorrência, dois tenants ou screenshots reais; esses gates não são declarados homologados e devem rodar no CI. A versão não implementa pagamentos parciais nem integração bancária. Estorno pós-pagamento permanece bloqueio explícito, não uma alteração silenciosa.
