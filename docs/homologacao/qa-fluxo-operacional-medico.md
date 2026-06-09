# QA Fluxo Operacional Médico

## Fluxo alvo

Hospital/Coordenação -> Plantão -> Publicação -> Convite -> Médico -> Aceite/Recusa -> Escala -> Realização -> Pagamento -> Relatório.

## Status real

| Etapa | Status | Pendência |
|---|---|---|
| Criar hospital | Parcial | Testar persistência e tenant. |
| Criar especialidade | Parcial | Testar persistência. |
| Criar médico | Parcial | Validar vínculo de usuário médico. |
| Criar plantão | Parcial | Validar limites do plano. |
| Publicar plantão | Parcial | Validar auditoria/notificação. |
| Enviar convite | Parcial | Validar limite mensal e elegibilidade. |
| Médico aceitar convite | Parcial | Validar acesso somente do médico. |
| Médico recusar com motivo | Parcial | Validar motivo obrigatório. |
| Confirmar escala | Parcial | Validar coordenação e conflito. |
| Substituir médico | Parcial | Validar parametrização. |
| Marcar realizado | Parcial | Validar geração financeira. |
| Gerar pagamento | Parcial | Validar API financeiro. |
| Confirmar pagamento | Parcial | Validar auditoria e status. |
| Exportar relatório | Parcial | Validar filtro tenant e LGPD. |

## Pendências

- QA manual completo com autenticação por coordenador, médico e financeiro.
