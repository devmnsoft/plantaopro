# Segurança multiempresa

O isolamento multiempresa é responsabilidade do `TenantGuardService`, apoiado por claims `uid`, `cliente_id` e roles.

## Regras

- Admin global pode acessar todos os clientes.
- Usuários comuns acessam apenas `cliente_id` da sessão.
- Médico deve operar apenas dados ligados ao seu usuário/médico.
- Hospital deve operar apenas hospital/unidade vinculada.

## Negativas

Toda negativa por tenant deve retornar 403, registrar `BLOQUEIO_TENANT` ou `ACESSO_NEGADO` e logar warning estruturado.

## Claims obrigatórias

O JWT e o cookie Web devem carregar `uid`, roles normalizadas e, quando o usuário estiver vinculado a um tenant, `cliente_id`. Essa claim é usada pelos guards, filtros de auditoria e logs de request para impedir que usuários comuns consultem registros de outro cliente.

## Rastreamento de bloqueios

Bloqueios por tenant são gravados em `plantaopro.auditoria_acoes_criticas` e em `plantaopro.acessos_negados_log`. O motivo é sanitizado para evitar exposição de senha, token, hash, segredo ou detalhes internos de SQL.
