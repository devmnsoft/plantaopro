# QA de CRUDs em runtime real

Data: 2026-07-09.

## Status por área

| Área | Classificação | Evidência atual |
| --- | --- | --- |
| Pacientes | Funcional pendente QA | Rotas e contratos mapeados; runtime bloqueado por ambiente. |
| Agendamentos | Funcional pendente QA | Smoke cobre listagem, agenda do dia e check-in; runtime bloqueado por ambiente. |
| Triagem | Funcional pendente QA | Fluxo documentado; runtime bloqueado por ambiente. |
| Consultas | Funcional pendente QA | Fluxo Saúde 360 documentado; runtime bloqueado por ambiente. |
| Prescrições | Funcional pendente QA | Contratos e rotas no smoke; runtime bloqueado por ambiente. |
| Financeiro Clínica | Funcional pendente QA | Rotas e documentação existentes; runtime bloqueado por ambiente. |
| Convênios/Planos | Funcional pendente QA | Rotas e documentação existentes; runtime bloqueado por ambiente. |
| Plantões/Escalas/Financeiro Médico | Funcional pendente QA | Smoke cobre endpoints principais; runtime bloqueado por ambiente. |

## Comandos executados

- `dotnet --info`: indisponível.
- `docker --version`: indisponível.
- `bash scripts/smoke/smoke-api.sh`: pendente de serviços reais.

## Pendências reais

Validar criar/listar/filtrar/editar/detalhes/inativar e ações críticas em ambiente homologável com banco aplicado.
