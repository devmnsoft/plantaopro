# Auditoria central

A auditoria central usa `IAuditService.RegistrarAsync` e grava em `plantaopro.auditoria_acoes_criticas`.

## Campos

- usuário, cliente, perfil e IP;
- entidade e entidade_id;
- ação padronizada;
- detalhes JSON sanitizados;
- sucesso/falha e data.

## Garantias

A auditoria não derruba a operação principal. Falhas de gravação são registradas em log e a ação principal continua. Senhas, tokens, hashes e segredos são mascarados.
