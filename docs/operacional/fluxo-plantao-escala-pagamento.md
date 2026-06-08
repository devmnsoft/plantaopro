# Fluxo plantão, escala e pagamento

## Fluxo demonstrável
Hospital/Coordenação -> Plantão -> Convite -> Médico -> Aceite/Recusa -> Escala -> Realização -> Pagamento -> Relatório.

## Validações obrigatórias
- Todas as consultas operacionais filtram `tenant_id`/`cliente_id` quando aplicável.
- Médico só acessa convites, agenda, escalas e pagamentos próprios.
- Financeiro só acessa pagamentos do tenant.
- Cancelamento e substituição exigem justificativa quando parametrizado.
- Realização de escala habilita geração de pagamento.
- Pagamento é único por escala e registra auditoria.

## Pendências reais
- Executar QA manual completo com PostgreSQL de homologação.
- Capturar evidências de auditoria, notificações e exportação CSV.
