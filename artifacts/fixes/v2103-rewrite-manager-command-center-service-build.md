# Correção v2.10.3 — Manager Command Center

## Causa-raiz

O bloco que reunia as duas consultas do Command Center havia sido editado como
uma sequência compacta e ficou estruturalmente inconsistente entre os
delimitadores da string C# e o SQL. Com isso, o compilador passou a interpretar
trechos como `select`, `from`, `where`, aspas SQL e quebras de linha como código
C#, produzindo em cascata `CS1001`, `CS1002`, `CS1003`, `CS1010`, `CS1012`,
`CS0742`, `CS0744`, `CS1031`, `CS1513` e `CS1525`. O `CS0006` dos testes era uma
consequência: a DLL da API não era produzida depois desses erros de sintaxe.

## Por que as tentativas anteriores não resolveram

As correções pontuais atuaram em aspas ou linhas isoladas, mas não substituíram
a unidade estrutural inteira. Em um bloco SQL multilinha já corrompido, ajustar
um delimitador não garante que todos os tokens seguintes retornem ao interior da
string. Esta rodada, portanto, não reaproveitou o bloco quebrado.

## Reescrita realizada

`ManagerCommandCenterService.cs` foi reescrito integralmente. As duas consultas
ficam agora em uma única constante verbatim `SqlCommandCenter`, iniciada e
encerrada explicitamente. Todos os aliases com aspas duplas usam o escape
correto de uma string verbatim C# e todos os valores variáveis são parâmetros
Dapper (`TenantId`, `From`, `To` e `Status`). Não há interpolação SQL,
`SELECT *`, dados de produção simulados nem secrets.

A validação de tenant vazio, os filtros por tenant e registro ativo, o
`CancellationToken`, o tratamento com `try/catch` e o log estruturado com
`ILogger<ManagerCommandCenterService>` foram preservados. A autorização
permanece no controller existente e nenhuma regra de permissão ou auditoria foi
removida.

## Assinaturas públicas preservadas

- `CommandCenterSummary`
- `CoverageItem`
- `ManagerCommandCenterDto`
- `ManagerCommandCenterService(IConfiguration, ILogger<ManagerCommandCenterService>)`
- `Task<ApiResponse<ManagerCommandCenterDto>> GetAsync(Guid, DateOnly, DateOnly, string?, CancellationToken)`

## Consultas SQL revisadas

As consultas continuam usando somente as tabelas e colunas operacionais que já
eram consumidas pelo serviço: `plantoes`, `medicos`, `pagamentos`,
`notificacoes`, `hospitais` e `especialidades`. Foram mantidos o resumo
operacional e a listagem de cobertura, os filtros de `cliente_id`,
`reg_status`, período e status, e o limite de 200 registros.

O comando
`rg -n "^\\s*(select|from|where|join|on)\\b" backend/PlantaoPro.Api/ManagerCommandCenterService.cs`
encontra somente linhas compreendidas entre a abertura e o fechamento da
constante `SqlCommandCenter`; a inspeção numerada confirma que não existe SQL
solto. O comando de busca por atribuições de string com aspas simples não
retornou ocorrências.

## Evidências de validação

O ambiente fornecido não possui o executável `dotnet` no `PATH` (retorno 127).
Por essa limitação externa, os quatro comandos .NET abaixo foram invocados,
mas não puderam compilar nem executar os testes localmente:

| Validação | Resultado |
|---|---|
| Build isolado da API (Debug) | Não executado: `dotnet: command not found` |
| Build da solution (Debug, `--no-restore`) | Não executado: `dotnet: command not found` |
| Build da solution (Release, `--no-restore`) | Não executado: `dotnet: command not found` |
| Testes (Release, `--no-build`) | Não executado: `dotnet: command not found` |

As validações disponíveis passaram:

- `python3 scripts/repository-security-check.py`: `repository-security ok`;
- `python3 scripts/check-csharp10-compatibility.py`: compatibilidade validada;
- `python3 scripts/validate-scrpt-completo.py`: cobertura de 100%;
- `git diff --check`: sem erros.

## Confirmação sobre o CS0006

A causa primária de sintaxe que impedia a geração da
`PlantaoPro.Api.dll` foi removida pela substituição integral do bloco. A
confirmação binária de que o `CS0006` desapareceu depende da execução do build
em um agente com o SDK .NET 10 instalado; ela não pode ser afirmada como
executada neste ambiente sem `dotnet`.
