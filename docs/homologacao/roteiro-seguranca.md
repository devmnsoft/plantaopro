# Roteiro de homologação de segurança

1. Login admin global.
2. Acessar Auditoria e Observabilidade.
3. Gerar ação crítica e confirmar registro.
4. Tentar acesso com perfil sem permissão e validar 403 amigável.
5. Confirmar auditoria de acesso negado.
6. Login médico e tentar acessar dados de outro médico.
7. Login administrador de cliente e tentar acessar outro cliente.
8. Testar endpoint mobile sem token e confirmar 401.
9. Testar endpoint mobile com médico e confirmar isolamento.
10. Validar build, Swagger e `/api/health`.
