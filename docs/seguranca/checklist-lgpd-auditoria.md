# Checklist LGPD e auditoria

- Não logar senha, token, hash, API key, anamnese, diagnóstico, prescrição ou dados clínicos sensíveis.
- Exportação, impressão e resumo clínico devem registrar auditoria.
- Recepção não acessa evolução clínica; financeiro não acessa anamnese; médico acessa apenas escopo permitido.
- Admin cliente limitado ao tenant; admin global com trilha auditável.

## Revisão RC — julho/2026

- Appsettings sanitizados com placeholders para connection string e JWT.
- Logs técnicos não devem registrar senha, hash, token, JWT, secret, connection string ou conteúdo clínico sensível.
- Auditoria crítica deve ser best-effort: falha de auditoria não pode derrubar operação principal, mas deve gerar log técnico sem dado sensível.
- Perfis clínicos devem manter segregação: recepção sem evolução/prescrição, financeiro sem conteúdo clínico e médico restrito a dados próprios/autorizados.
