# Matriz de perfis e permissões — v1.81.0

## Perfis esperados
Admin, Coordenação, Médico, Hospital, Financeiro e Operador.

## Matriz efetiva
Não disponível em um endpoint agregador. Nenhuma permissão é marcada como concedida para evitar uma matriz fictícia. A autorização efetiva continua nas roles, claims e policies do servidor; a UI nunca a substitui.

| Capacidade | Admin | Coordenação | Médico | Hospital | Financeiro | Operador |
|---|---|---|---|---|---|---|
| Clínica | Validar no backend | Validar no backend | Validar no backend | Validar no backend | Validar no backend | Validar no backend |
| Operação | Validar no backend | Validar no backend | Validar no backend | Validar no backend | Validar no backend | Validar no backend |
| Financeiro | Validar no backend | Validar no backend | Validar no backend | Validar no backend | Validar no backend | Validar no backend |
| Administração | Validar no backend | Validar no backend | Validar no backend | Validar no backend | Validar no backend | Validar no backend |
| Auditoria/LGPD | Validar no backend | Validar no backend | Validar no backend | Validar no backend | Validar no backend | Validar no backend |

Convite, ativação, troca de perfil e reset de senha só podem ser habilitados após confirmar endpoint, policy, antiforgery e auditoria.
