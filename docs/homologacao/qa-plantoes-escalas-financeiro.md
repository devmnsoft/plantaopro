# QA Plantões, Escalas e Financeiro Médico

Status geral: **Funcional pendente QA**. Não declarar produção.

## Fluxo operacional

| Etapa | Regra de aceite | Status |
|---|---|---|
| Criar e publicar plantão | Vagas não negativas e tenant respeitado | Funcional pendente QA |
| Médico visualiza disponível | Médico só vê dados próprios/permitidos | Funcional pendente QA |
| Solicitar ou aceitar convite | Médico inativo bloqueado; recusa exige justificativa | Funcional pendente QA |
| Coordenação confirma escala | Não duplicar escala e conflito crítico bloqueia | Funcional pendente QA |
| Realizar plantão | Auditoria e notificação geradas | Funcional pendente QA |
| Gerar pagamento | Não duplicar pagamento | Funcional pendente QA |
| Médico contesta | Justificativa obrigatória | Parcial |
| Financeiro resolve e paga | Financeiro não acessa conteúdo clínico sensível | Funcional pendente QA |

## Evidências desta rodada

- Mobile médico consome endpoints reais para plantões, convites, escalas e pagamentos quando disponíveis.
- Roteiro de smoke inclui dashboards e Operação Inteligente.
- Validação real de concorrência/vagas depende de ambiente com PostgreSQL.
