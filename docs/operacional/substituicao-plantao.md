# Substituição de plantão

Fluxo implementado:
1. Médico solicita substituição de plantão próprio.
2. Sistema gera pendência operacional.
3. Coordenação aprova ou recusa com justificativa quando necessário.
4. Coordenação convida substituto.
5. Confirmação atualiza a escala quando há `escala_id` vinculado.
6. Histórico e auditoria registram todos os passos.

Endpoints: `/api/medicos/me/substituicoes` e `/api/substituicoes`.
