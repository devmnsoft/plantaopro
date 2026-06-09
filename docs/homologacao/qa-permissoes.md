# QA de permissões

Validar: admin global acessa Admin SaaS, clientes, billing, white label, permissões, auditoria e troca de tenant. Admin cliente acessa apenas seu tenant. Coordenação não acessa billing global. Financeiro não acessa white label/permissões globais. Médico não acessa dashboards administrativos. Parceiro não acessa dados sensíveis. Auditor não altera dados. Suporte registra acesso assistido.

## Fase 1 — QA por perfil

- ADMINISTRADOR_GLOBAL: menu Admin SaaS aparece e redirect vai para `/AdminSaas/Index`.
- ADMINISTRADOR_CLIENTE/ADMINISTRADOR: redirect vai para `/ClientePortal/Index` e menu Admin SaaS global não é exibido pelo builder.
- COORDENADOR/COORDENACAO: redirect vai para `/CentralEscala/Index`.
- FINANCEIRO: menu financeiro e redirect `/Financeiro/Index`.
- MEDICO: menu médico e redirect `/MedicoArea/Index`.
- PARCEIRO: menu parceiro e redirect `/ParceiroPortal/Index`.

Validação manual em browser depende de ambiente com SDK .NET e API disponível.
