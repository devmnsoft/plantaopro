# Demo mode

## Implementado
- API de demo restrita a `ADMINISTRADOR_GLOBAL`.
- Geração de dados demo registra contadores para tenants, clientes, médicos, hospitais, plantões, convites, escalas, pagamentos, faturas, propostas e parceiros.
- Limpeza remove somente marcadores demo gerados pela rotina demonstrável.
- Nenhum dado pessoal real é necessário para apresentação.
- Geração e limpeza registram auditoria e não derrubam a operação se auditoria falhar.

## Pendências reais
- Persistir flag `is_demo` nas tabelas operacionais quando a massa demo for gravada em PostgreSQL.
- Validar que limpeza em banco remove apenas registros marcados como demo.
