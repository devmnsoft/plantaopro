# Checklist LGPD e auditoria

- Não logar senha, token, hash, prescrição, anamnese, diagnóstico ou dados bancários.
- Auditar acesso a resumo clínico, impressão, exportação e alterações críticas.
- Restringir financeiro sem evolução clínica; recepção sem anamnese; médico ao próprio contexto; admin cliente ao tenant.
- Validar RequestLoggingMiddleware e mascaramento antes de produção.
