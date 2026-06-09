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

## QA complementar da Fase 1

Cenários adicionados à homologação manual:

1. Login com `ADMINISTRADOR_GLOBAL` deve abrir `AdminSaas/Index` e exibir grupos globais.
2. Login com `ADMINISTRADOR_CLIENTE` ou `ADMINISTRADOR` deve abrir `ClientePortal/Index` e não exibir o grupo **Admin SaaS**.
3. Login com `COORDENADOR` deve abrir `CentralEscala/Index` e exibir operação sem áreas financeiras globais.
4. Login com `FINANCEIRO` deve abrir `Financeiro/Index` e exibir pagamentos, faturas e relatórios.
5. Login com `MEDICO` deve abrir `MedicoArea/Index` e limitar navegação à área médica, convites e pagamentos próprios.
6. Login com `PARCEIRO` deve abrir `ParceiroPortal/Index` e listar clientes via portal parceiro, sem acessar o portal administrativo do cliente.
7. Acesso direto a controller mapeado sem permissão deve redirecionar para `Account/AccessDenied` e gerar log do guard SaaS.
