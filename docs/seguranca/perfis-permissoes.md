# Perfis e permissões

O PlantãoPro mantém autorização em duas camadas: perfil (`role`) e permissão fina. O perfil continua sendo usado para proteger rotas Web/API, enquanto `PermissionGuardService` concentra o fallback de permissões por perfil quando ainda não houver tabela dedicada de permissões.

## Perfis

- `ADMINISTRADOR_GLOBAL`: operação SaaS e visão de todos os clientes.
- `ADMINISTRADOR`: administração restrita ao próprio cliente.
- `COORDENACAO`: operação de plantões e escalas do cliente.
- `OPERADOR`: apoio operacional do cliente.
- `FINANCEIRO`: pagamentos e relatórios financeiros do cliente.
- `MEDICO`: área própria do médico.
- `HOSPITAL`: dados da unidade vinculada.

## Permissões mínimas

As permissões padronizadas incluem `MEDICOS_VER`, `MEDICOS_CRIAR`, `PLANTOES_PUBLICAR`, `ESCALAS_CONFIRMAR`, `FINANCEIRO_CONFIRMAR`, `AUDITORIA_VER`, `OBSERVABILIDADE_VER`, `SUPORTE_VER` e demais constantes em `Permissoes`.

## Diretriz de segurança

Botões podem ser ocultados na Web, mas toda ação sensível deve ser validada também na API. Negativas devem retornar 403 amigável e gerar auditoria `BLOQUEIO_PERMISSAO` ou `BLOQUEIO_TENANT`.
