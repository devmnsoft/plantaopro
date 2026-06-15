# CID — Saúde 360 Fase 5.2

O módulo CID permite cadastro, busca, favoritos por médico/tenant e acompanhamento de códigos mais usados. A base pode conter registros globais (`cliente_id` nulo) e registros específicos do tenant.

## Endpoints

- `GET /api/cid`
- `GET /api/cid/{id}`
- `GET /api/cid/buscar?termo=`
- `POST /api/cid`
- `PUT /api/cid/{id}`
- `POST /api/cid/{id}/inativar`
- `POST /api/cid/importar`
- `POST /api/cid/{id}/favoritar`
- `GET /api/cid/favoritos`
- `GET /api/cid/mais-usados`

## Regras

- Código CID não pode duplicar.
- Busca considera código e descrição.
- Favoritos são vinculados ao médico/tenant.
- Uso em consulta pode registrar histórico em `cid_uso_historico`.
- Importações são auditadas e idempotentes no banco.
