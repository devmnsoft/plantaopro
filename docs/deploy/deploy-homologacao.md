# Deploy em Homologação

1. Provisionar PostgreSQL e aplicar schema `plantaopro`.
2. Configurar `appsettings.Homologacao.json` (ConnectionStrings, JWT, CORS, logs).
3. Executar migrations/scripts SQL e seed de dados.
4. Publicar API e Web com variáveis seguras (secret manager/cofre).
5. Validar endpoints críticos, login e permissões por perfil.
6. Executar checklist pós-deploy e smoke tests.
