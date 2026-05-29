# Auditoria central

A auditoria central usa `IAuditService.RegistrarAsync` e grava em `plantaopro.auditoria_acoes_criticas`.

## Campos

- `usuario_id`
- `cliente_id`
- `entidade`
- `entidade_id`
- `acao`
- `detalhes` em JSONB
- `sucesso`
- `ip_origem`
- `perfil`
- `reg_date`

## Resiliência

Falhas de auditoria não interrompem a operação principal. O serviço registra erro no logger e retorna.

## Dados sensíveis

Campos contendo senha, password, token, hash, secret ou segredo são mascarados antes de serializar detalhes.
