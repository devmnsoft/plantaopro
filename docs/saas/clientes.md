# Clientes SaaS

## Como criar cliente

Use a tela **Clientes > Novo** ou a API `POST /api/clientes` informando razão social, nome fantasia, CNPJ, e-mail, telefone, cidade, estado e status inicial.

## Status suportados

- `TESTE`
- `ATIVO`
- `SUSPENSO`
- `CANCELADO`
- `INATIVO`

## Operações críticas

- Suspender: `POST /api/clientes/{id}/suspender`.
- Reativar: `POST /api/clientes/{id}/reativar`.
- Cancelar: `POST /api/clientes/{id}/cancelar`.

Clientes suspensos não publicam plantões. Clientes cancelados não operam.
