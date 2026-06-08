# Regras Comerciais SaaS

O módulo Comercial SaaS gerencia leads, oportunidades, propostas, descontos, conversão e previsibilidade de receita.

## Regras implementadas

- Lead pode ser criado e editado com plano sugerido por regras determinísticas.
- Oportunidade ganha registra interação e auditoria.
- Oportunidade perdida exige motivo e registra interação.
- Proposta exige oportunidade, valor positivo e validade futura.
- Proposta vencida não pode ser aprovada.
- Desconto acima de 15% exige `ADMINISTRADOR_GLOBAL`.
- Funil comercial agregado por etapa em `GET /api/comercial/funil`.
- Previsão de receita em `GET /api/comercial/previsao-receita`.
- Sugestão de plano em `POST /api/comercial/sugerir-plano`.
