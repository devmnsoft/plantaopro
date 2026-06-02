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

## Padrão central consolidado

O contrato central é `IAuditService.RegistrarAsync`. O método legado `LogAsync` permanece apenas como adaptador de compatibilidade: ele normaliza entidades/ações antigas (`CREATE`, `UPDATE`, `STATUS_CHANGE`) para constantes padronizadas e tenta resolver `cliente_id` da entidade auditada antes de gravar o evento central.

## Ações críticas cobertas

Login Web/Mobile, falhas de login, criação/edição/inativação de cadastros existentes, mudanças de status de plantões/escalas/pagamentos, exportação de auditoria CSV, bloqueios de permissão e bloqueios de tenant devem gerar evento central. Falha de auditoria nunca cancela a operação principal.
