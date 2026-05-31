# Segurança multiempresa

O isolamento multiempresa é responsabilidade do `TenantGuardService`, apoiado por claims `uid`, `cliente_id` e roles.

## Regras

- Admin global pode acessar todos os clientes.
- Usuários comuns acessam apenas `cliente_id` da sessão.
- Médico deve operar apenas dados ligados ao seu usuário/médico.
- Hospital deve operar apenas hospital/unidade vinculada.

## Negativas

Toda negativa por tenant deve retornar 403, registrar `BLOQUEIO_TENANT` ou `ACESSO_NEGADO` e logar warning estruturado.
