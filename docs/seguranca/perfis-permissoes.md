# Perfis e permissões

O PlantãoPro mantém roles amplas para autenticação/autorização inicial e usa `PermissionGuardService` como ponto central para permissão fina.

## Perfis

- `ADMINISTRADOR_GLOBAL`: operação SaaS, todos os clientes e observabilidade.
- `ADMINISTRADOR`: administração dentro do próprio cliente.
- `COORDENACAO`: operação de plantões e escalas do cliente.
- `OPERADOR`: execução operacional limitada.
- `FINANCEIRO`: pagamentos, relatórios financeiros e conciliações.
- `MEDICO`: área própria do médico, convites, escalas e pagamentos próprios.
- `HOSPITAL`: dados da unidade/hospital vinculados.

## Permissões mínimas

O fallback por perfil cobre permissões como `MEDICOS_VER`, `PLANTOES_CRIAR`, `ESCALAS_CONFIRMAR`, `FINANCEIRO_VER`, `AUDITORIA_VER` e `OBSERVABILIDADE_VER`. Quando uma tabela de permissões fina for ativada, ela deve complementar esse fallback sem remover as roles.

## Bloqueio

Permissão negada deve retornar 403 amigável, registrar auditoria com `BLOQUEIO_PERMISSAO` e nunca expor regra interna ou SQL.

## Permissões auditadas nesta rodada

`PermissionGuardService` passa a registrar cada validação relevante em `plantaopro.permissao_logs`, incluindo usuário, cliente, permissão, decisão e motivo resumido. Quando a permissão é negada, o fluxo também chama o `TenantGuardService.RegistrarAcessoNegadoAsync`, gerando auditoria central `BLOQUEIO_PERMISSAO`.

Lista mínima mantida no fallback: `MEDICOS_*`, `HOSPITAIS_*`, `PLANTOES_*`, `ESCALAS_*`, `FINANCEIRO_*`, `USUARIOS_GERENCIAR`, `CLIENTES_GERENCIAR`, `PLANOS_GERENCIAR`, `ASSINATURAS_GERENCIAR`, `RELATORIOS_VER`, `AUDITORIA_VER`, `OBSERVABILIDADE_VER`, `CONFIGURACOES_EDITAR` e `SUPORTE_VER`.
