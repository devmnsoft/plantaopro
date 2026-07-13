# Otimizações de performance Saúde 360

Endpoints revisados: dashboard clínico, pacientes, agendamentos, triagens, consultas, CID, prescrições, contas a receber, convênios e pendências clínicas.

Diretrizes do RC: listagens paginadas com limite padrão, buscas textuais limitadas, filtros por tenant em todas as consultas, filtros por data/status nos fluxos operacionais, agregações específicas para dashboards, sem `select *`, sem N+1 e logs de lentidão sem dados sensíveis.

Migration criada: `database/migrations/2026_saude360_indices_performance.sql`.
