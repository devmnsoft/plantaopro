# Recomendação de médicos por plantão

## Objetivo
Padronizar os critérios operacionais para recomendação de profissionais no preenchimento de plantões.

## Critérios
1. Médico ativo e com cadastro regular.
2. Especialidade compatível.
3. Sem conflito com escalas no mesmo período.
4. Preferência de turno e disponibilidade.
5. Priorização por menor carga semanal.

## Regras de segurança
- Exibir apenas médicos do mesmo `cliente_id` (exceto ADMINISTRADOR_GLOBAL).
- Bloquear convite duplicado para o mesmo plantão.
- Auditoria obrigatória em geração de convites.

## Fluxo
1. Usuário abre detalhes do plantão.
2. Sistema calcula candidatos elegíveis.
3. Usuário revisa score e status.
4. Usuário gera convite em lote.
