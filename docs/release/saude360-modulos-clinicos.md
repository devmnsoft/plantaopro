# Release — PlantãoPro Saúde 360 módulos clínicos

## Resumo

Esta rodada adiciona a base funcional dos módulos clínicos Saúde 360: pacientes, painel de chamada, agendamentos, triagem, consultas, CID, prescrição médica, financeiro clínico, convênios e planos de saúde.

## Entregas

- Migration idempotente com estrutura assistencial multi-tenant.
- Controllers API com endpoints solicitados, validações obrigatórias e services registrados em DI.
- Controllers Web MVC e view compartilhada para navegação operacional dos módulos.
- Menus, perfis clínicos e catálogo de módulos por plano.
- Documentação clínica e roteiro de QA.

## Pendências reais

- O ambiente de execução desta rodada não possui `dotnet`, portanto build/testes precisam ser repetidos em ambiente com SDK .NET instalado.
- É necessário aplicar a migration em PostgreSQL de homologação antes do QA manual ponta a ponta.
- Formulários clínicos detalhados e templates de impressão devem ser refinados conforme protocolos de cada clínica.
