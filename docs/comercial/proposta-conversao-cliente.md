# Proposta comercial e conversão em cliente

## Implementado
- Proposta possui cliente, e-mail, empresa/CNPJ, plano, módulos, setup, mensalidade, desconto, validade, SLA, prazo, condições e timeline.
- Proposta vencida não pode ser aprovada nem convertida.
- Conversão exige status aprovado.
- Conversão bloqueia duplicidade por empresa/CNPJ normalizado.
- Conversão retorna IDs de tenant, cliente, assinatura, administrador cliente e onboarding.
- Cobrança/gateway é explicitamente `MANUAL_SANDBOX` nesta versão demonstrável.
- Evento de conversão registra auditoria.

## Pendências reais
- Persistir provisionamento em transação PostgreSQL com rollback/reprocessamento operacional.
- Configurar gateway real somente após contrato e homologação técnica do provedor.
