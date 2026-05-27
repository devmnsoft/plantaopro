# Financeiro Médico

## Escopo
Fluxo completo de geração, contestação e confirmação de pagamentos vinculados às escalas realizadas.

## Endpoints
- `GET /api/financeiro/resumo`
- `GET /api/financeiro/pagamentos`
- `GET /api/financeiro/pagamentos/{id}`
- `POST /api/financeiro/pagamentos/gerar`
- `POST /api/financeiro/pagamentos/{id}/confirmar`
- `POST /api/financeiro/pagamentos/{id}/cancelar`
- `POST /api/financeiro/pagamentos/{id}/contestar`
- `POST /api/financeiro/pagamentos/{id}/resolver-contestacao`

## Regras de negócio obrigatórias
- Gerar pagamento apenas para escala `REALIZADA`.
- Bloquear geração duplicada para mesma escala.
- Valor do pagamento deve ser `>= 0`.
- Confirmação exige valor, data de pagamento e forma de pagamento.
- Cancelamento exige justificativa.
- Médico só pode contestar pagamento próprio.
- Contestação exige motivo e não pode duplicar contestação aberta.
- Resolução de contestação exige resposta do financeiro/coordenação.
- Toda ação crítica gera auditoria e notificação.

## Estados sugeridos de pagamento
- `PENDENTE`
- `GERADO`
- `CONFIRMADO`
- `CANCELADO`
- `CONTESTADO`
- `EM_ANALISE`
- `RESOLVIDO`

## Checklist de homologação
1. Marcar escala como `REALIZADA`.
2. Gerar pagamento da escala.
3. Validar bloqueio de duplicidade.
4. Contestar pagamento com usuário médico.
5. Resolver contestação no perfil financeiro.
6. Confirmar pagamento com dados obrigatórios.
7. Verificar notificação e histórico de auditoria.
