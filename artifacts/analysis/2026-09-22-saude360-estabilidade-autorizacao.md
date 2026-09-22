# Saúde 360 — auditoria de estabilidade e autorização (2026-09-22)

## Escopo executado

O repositório já contém a base funcional de Pacientes, Agendamentos, Painel de Chamada e
Triagem, incluindo controllers API/MVC, views, serviço Dapper, migration clínica e testes de
contrato. Nesta rodada foi respeitada a regra de estabilizar antes de expandir: nenhum novo
módulo clínico foi criado.

## Correção aplicada

- As APIs de pacientes e agendamentos agora exigem explicitamente os perfis Saúde 360
  assistenciais, além de autenticação.
- O painel operacional exige os perfis de recepção autorizados.
- A triagem ganhou uma fronteira própria de perfis clínicos. `RECEPCAO`, `OPERADOR` e os
  demais perfis exclusivamente administrativos/operacionais não fazem parte dessa fronteira,
  impedindo que a recepção leia ou altere sinais vitais e conteúdo de triagem apenas por estar
  autenticada.
- Foi adicionado teste de contrato para evitar a remoção acidental dessas autorizações.

O isolamento por tenant já existente no serviço clínico foi preservado: consultas e mutações
usam o tenant do contexto autenticado e retornam `404` quando a entidade não pertence ao
escopo permitido.

## Evidências executadas

| Verificação | Resultado |
| --- | --- |
| Estado Git e inventário de binários/build outputs rastreados | Limpo no início; nenhum artefato proibido encontrado |
| Validadores de controllers, C# 10, banco, segurança e UX | 9/9 aprovados |
| Testes Python dos validadores | 11 aprovados |
| `dotnet restore`, `dotnet build`, `dotnet test` | Bloqueados: executável `dotnet` ausente |
| Homologação Linux | Bloqueada pelo próprio preflight: SDK .NET 10 ausente |
| Docker / PostgreSQL (`psql`) | Ausentes no ambiente |

## Estado real e pendências

- Não foi possível compilar, executar testes .NET, abrir Swagger, validar login em runtime,
  aplicar a instalação limpa/upgrade do PostgreSQL ou concluir a jornada manual ponta a ponta.
- Por isso, esta entrega **não** é declarada pronta para produção e não afirma ausência de bugs.
- Não houve alteração de migration ou manifest nesta rodada, pois a correção é de autorização
  na aplicação e não modifica o contrato de persistência.
- A próxima rodada deve iniciar em ambiente com .NET SDK, PostgreSQL e, idealmente, Docker;
  executar restore/build/test, instalação limpa e upgrade, iniciar API/Web e percorrer a jornada
  Admin → paciente → agendamento → check-in → painel → triagem, incluindo tentativas cruzadas
  de tenant e o retorno 403 para `RECEPCAO` na API de triagem.

## Próximo prompt recomendado

> Em ambiente com .NET SDK e PostgreSQL disponíveis, execute a homologação real do Saúde 360.
> Não crie módulos novos. Corrija apenas falhas de build/runtime, banco, login, Swagger, tenant
> isolation e permissões encontradas na jornada Paciente → Agendamento → Check-in → Painel →
> Triagem; registre evidências HTTP e screenshots responsivos antes de declarar a base validada.
