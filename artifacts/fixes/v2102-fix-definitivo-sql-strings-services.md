# Correção definitiva das strings SQL nos services — v2.10.2

## Causa-raiz

Em `UnitPortalServices.cs`, a consulta estava declarada como string verbatim (`@"..."`), mas os aliases SQL usavam escapes de string comum (`\"Today\"`, por exemplo). Em uma string verbatim, uma aspa dupla deve ser representada por `""`; a barra invertida não funciona como caractere de escape. Por isso, a primeira aspa encerrava a string C# antes do esperado e a barra invertida seguinte produzia `CS1056`.

O bloco de `ManagerCommandCenterService.cs` foi reescrito integralmente no formato verbatim canônico, sem raw strings e com todas as aspas duplas dos aliases duplicadas. Isso elimina qualquer término prematuro da constante e torna explícitos os limites das duas consultas entregues ao `QueryMultipleAsync`.

Quando uma string termina prematuramente, o restante do SQL deixa de ser texto para o parser de C#. Assim, palavras SQL como `where`, `join`, `on` e `is` passam a ser analisadas como palavras contextuais ou expressões C#/LINQ, originando a cascata de `CS0742`, `CS0744`, `CS1525`, `CS1010`, `CS1001`, `CS1002` e `CS1003`.

## Trechos corrigidos

- `backend/PlantaoPro.Api/ManagerCommandCenterService.cs`: constante `sql` de `ManagerCommandCenterService.GetAsync`.
- `backend/PlantaoPro.Api/UnitPortalServices.cs`: constante `sql` de `UnitDashboardService.GetAsync`.

As consultas continuam usando Dapper e PostgreSQL, projeções explícitas, parâmetros (`@tenantId`, `@unitId`, `@from`, `@to` e `@status`) e os filtros de tenant e de registro ativo já existentes. Não houve alteração de permissão, auditoria, logging, tratamento de exceção, projeto ou pipeline.

## Evidências de validação

### Build da API e solution

O ambiente fornecido não contém o executável `dotnet` (`/bin/bash: dotnet: command not found`, exit code 127). Por essa limitação real, não foi possível gerar localmente `PlantaoPro.Api.dll`, executar os builds Debug/Release ou comprovar por build que o `CS0006` desapareceu. A causa sintática que impedia a geração da DLL foi removida, mas a confirmação binária deve ocorrer no CI com o SDK .NET instalado.

### Testes

Os testes também não puderam ser executados porque dependem do mesmo executável `dotnet`, ausente no ambiente. Não foi produzido resultado de teste nem TRX local.

### Verificações estáticas

- `python3 scripts/repository-security-check.py`: aprovado (`repository-security ok`).
- `python3 scripts/check-csharp10-compatibility.py`: aprovado (`Compatibilidade C# 10 e CSS Razor validada.`).
- `python3 scripts/validate-scrpt-completo.py`: aprovado (`{"ok": true, "coveragePercent": 100.0}`).
- `git diff --check`: aprovado.
- A busca solicitada por padrões proibidos foi executada. Ela encontrou ocorrências preexistentes em outros módulos e documentação; nenhuma foi introduzida nos dois services corrigidos. As consultas alteradas não usam `SELECT *`, interpolação ou credenciais.

## Escopo

Nenhuma funcionalidade nova ou mudança de design foi adicionada. A rodada se limita às duas constantes SQL quebradas e a este registro de evidências.
