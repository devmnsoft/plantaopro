# Roteiro de homologação de segurança

1. Login com administrador global.
2. Abrir Auditoria e validar cards/resumo.
3. Executar ação crítica e conferir registro em auditoria.
4. Tentar acesso com perfil sem permissão e validar 403 amigável.
5. Verificar auditoria de acesso negado.
6. Login médico e tentativa de acessar dados de outro médico.
7. Login administrador de cliente e tentativa de acessar outro cliente.
8. Chamar `/api/mobile/me` sem token e validar 401.
9. Chamar endpoint mobile com médico e validar isolamento.
10. Abrir Observabilidade e conferir requests, erros, acessos negados e logins.
11. Rodar build/testes automatizados.
12. Revisar `git diff --stat` e arquivos versionados antes do PR.
