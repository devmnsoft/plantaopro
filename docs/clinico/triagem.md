# Triagem clínica — PlantãoPro Saúde 360

## Funcionalidade implementada

- Migration incremental em `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql` com tabelas, índices por tenant/status/data e auditoria mínima.
- API ASP.NET Core com validação de entrada, filtro por tenant/cliente e registro de auditoria via serviço central.
- Web MVC com ações reais de navegação para as telas do módulo e endpoint de referência para integração operacional.
- Permissões por perfis clínicos e amarração ao catálogo de plano Saúde 360.

## Controles obrigatórios

- Tenant obrigatório para operações de escrita.
- Histórico/auditoria para ações críticas.
- Dados clínicos sensíveis não são enviados para log técnico pelo serviço do módulo.
- Integração preserva SaaS, white label, billing e permissões existentes.

## Pendências reais

- Homologar fluxos com banco PostgreSQL real após aplicar a migration.
- Evoluir formulários MVC para edição rica campo a campo após validação clínica com usuários-chave.
- Integrar impressões clínicas a templates oficiais da clínica/tenant.
