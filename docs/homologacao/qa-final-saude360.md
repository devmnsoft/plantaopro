# QA final ponta a ponta Saúde 360

## Técnico
`dotnet clean` API/Web, `dotnet build` API/Web e `dotnet test` devem ser executados no ambiente com SDK instalado. Neste ambiente, `dotnet` não estava disponível.

## Fluxos validados por contrato
Swagger, login, menus sem 404, dashboards, manual, pacientes, agendamentos, check-in, painel, triagem, consultas, CID, prescrições, financeiro, convênios, planos, relatórios e auditoria.

## Pendências reais
Executar smoke test com PostgreSQL e navegador no ambiente de homologação para confirmar status HTTP, dados demo e console limpo.
