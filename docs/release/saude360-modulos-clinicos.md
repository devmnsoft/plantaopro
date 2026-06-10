# PlantãoPro Saúde 360 — módulos clínicos

## Escopo entregue

A Fase 5 adiciona a base funcional para módulos clínicos integrados ao SaaS white label multi-tenant:

- Painel de chamada.
- Agendamento clínico.
- Triagem.
- Consultas.
- Tabela CID.
- Prescrição médica.
- Financeiro da clínica.
- Convênios.
- Planos de saúde.

## Componentes técnicos

- Migration idempotente em `database/migrations/2026_plantao_pro_saude360_modulos_clinicos.sql`.
- APIs ASP.NET Core em `/api/*` com validação básica, tenant e auditoria central.
- Serviço clínico `Saude360ClinicalService` com Dapper/PostgreSQL.
- Web MVC com controllers, service, views compartilhadas e menus por perfil.
- Permissões por módulos `SAUDE360_*` e papéis de recepção, triagem, médico, financeiro clínico, faturamento de convênio e administrador de clínica.

## Planos

- Essencial: painel básico, agendamento básico e consulta simples.
- Profissional: triagem, prescrição, CID e financeiro clínica.
- Enterprise: convênios, glosas, relatórios executivos, API e white label completo.

## Pendências reais

- Popular base CID oficial completa via rotina operacional homologada.
- Implementar formulários específicos por subentidade clínica, além do formulário genérico inicial.
- Integrar impressão com template legal definitivo de cada clínica.
- Executar homologação manual em ambiente com PostgreSQL e SDK .NET disponíveis.
