# Central de Escala — operação pré-homologação

## Objetivo
Ser a tela de controle operacional da coordenação para identificar lacunas, riscos e próximas ações.

## Deve exibir
- Plantões abertos e em escala.
- Plantões sem médico confirmado.
- Escalas solicitadas aguardando decisão.
- Convites pendentes ou próximos de expirar.
- Conflitos críticos de horário.
- Pagamentos pendentes após escala realizada.
- Alertas operacionais de alto impacto.

## Regras operacionais
- Solicitação médica não consome vaga definitiva.
- Confirmação da coordenação consome uma vaga disponível.
- Cancelamento de escala confirmada libera uma vaga, limitado ao total do plantão.
- Vagas zeradas levam o plantão para status preenchido.
- Não comparecimento não libera pagamento automático.

## Indicadores sugeridos
| Indicador | Uso |
|---|---|
| Plantões abertos | Capacidade ainda disponível |
| Escalas solicitadas | Pendências de decisão da coordenação |
| Conflitos | Risco de falha operacional |
| Pagamentos pendentes | Entregável para financeiro |
| Alertas críticos | Prioridade de intervenção |
