# Checklist LGPD, segurança e auditoria Saúde 360

- Recepção visualiza dados administrativos mínimos e não acessa evolução clínica completa.
- Financeiro não acessa anamnese, diagnóstico ou prescrição.
- Médico acessa consultas próprias/autorizadas e histórico sensível com auditoria.
- Auditor possui leitura e trilhas de auditoria.
- Admin cliente fica restrito ao tenant; admin global audita visualizações sensíveis.
- Exportações, impressões e acesso ao histórico do paciente devem gerar evento de auditoria.
- Logs não devem conter senha, token, hash, API key ou conteúdo clínico sensível.
- Query strings sensíveis devem ser sanitizadas no middleware de request.
- API key deve ser exibida apenas na criação, nunca em listagem posterior.
