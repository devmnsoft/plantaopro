# v1.75.0 — matriz de empty states

| Tela | Condição real | Estado apresentado |
|---|---|---|
| Faturamento Clínico | erro da API | mensagem do erro e ação de tentar novamente |
| Faturamento Clínico | API sem contas | informa ausência para o tenant e que não criou valores/histórico |
| Faturamento Clínico | filtros sem correspondência | orienta ajustar/limpar filtros |
| Pagamentos | API sem itens | informa que nenhum pagamento, valor ou histórico foi estimado |
| Financeiro | lista sem itens | informa ausência para os filtros; consolidação sem dados |
| Dashboard / Minha Central / Notificações | backend sem dados | componentes existentes permanecem vazios; não foram adicionados contadores ou itens sintéticos |

Campos financeiros ausentes usam “Não informado”/“Não informado pela API”; ausência não é traduzida como `R$ 0,00`.
