# Resultado do smoke runtime real

Data: 2026-07-09.

## Comandos previstos

```bash
bash scripts/smoke/smoke-api.sh
```

## Evidência

O smoke está preparado para validar API e Web reais com `PLANTAOPRO_API_BASE_URL`, `PLANTAOPRO_WEB_BASE_URL`, `PLANTAOPRO_ADMIN_EMAIL` e `PLANTAOPRO_ADMIN_PASSWORD` informados pelo ambiente.

## Resultado no executor atual

**Bloqueado por ambiente**: sem SDK .NET, Docker, PostgreSQL/psql e serviços API/Web ativos. Nenhum token foi impresso durante a preparação.

## Rotas cobertas

- API: health, health/db, login, usuário autenticado, dashboards, Operação Inteligente, agendamentos, triagens, consultas, CID, prescrições, financeiro clínica, convênios, planos de saúde, plantões, escalas, pagamentos e notificações.
- Web: Dashboard, dashboards premium, Operação Inteligente, Pacientes, Agendamentos, Painel de Chamada, Triagem, Consultas, Prescrições, CID, Financeiro Clínica, Convênios, Planos Saúde, Plantões, Escalas, Financeiro, Notificações e Relatórios.

## Pendências

Executar o smoke em ambiente com serviços reais e registrar códigos HTTP finais.
