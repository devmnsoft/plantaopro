# v2.10.4 — correção do detalhe de pagamento e evolução financeira

## Causa-raiz

`PagamentoDetailsDto` era um `record` posicional cujo construtor esperava 19 argumentos, inclusive `DataPlantao`. A consulta de `FinanceiroService.GetByIdAsync` retornava 24 colunas com outro contrato (`DataInicioPlantao`, `DataFimPlantao`, cidade/UF do hospital, chave Pix e data de registro). Como não havia construtor sem parâmetros nem assinatura exatamente compatível, o Dapper interrompia a materialização com `InvalidOperationException`.

## Correção Dapper e consulta financeira

- `PagamentoDetailsDto` passou a ser uma classe selada com construtor padrão implícito e propriedades públicas graváveis.
- O DTO preserva todos os campos financeiros. Campos originados de colunas opcionais ou integrações (`ValorPago`, datas de previsão/pagamento, textos cadastrais, forma, Pix e observações) são nullable.
- A query agora declara cada coluna, usa aliases PostgreSQL explícitos e idênticos às propriedades e lê `pg.chave_pix` em vez de fabricar `null`.
- A consulta usa os parâmetros `Id`, `TenantId` e `IsGlobal`. Usuários comuns somente alcançam pagamentos do tenant atual; administrador global mantém a regra já existente de acesso global.
- A geração de novos pagamentos passou a persistir `tenant_id` e `cliente_id`, mantendo esses registros acessíveis somente no contexto que os criou.
- Registro inexistente ou pertencente a outro tenant mantém o retorno 404 do padrão atual. Contexto tenant ausente retorna 403.
- `FinanceiroController.Get` registra exceções inesperadas com o identificador do pagamento e relança a exceção; não há mock, fallback falso ou exceção silenciosamente engolida.

## DateOnly, DateTime e nullability

O provider Npgsql/Dapper já usa `DateOnly` em outros read models do projeto. Assim, `DataPrevista` e `DataPagamento` continuam como `DateOnly?`, refletindo colunas PostgreSQL `date` opcionais. Datas e horários do plantão e `RegDate` continuam `DateTime`, refletindo `timestamp`/`timestamptz`. `ValorPrevisto` permanece `decimal` porque a coluna canônica é `NOT NULL`; `ValorPago` permanece `decimal?` porque um pagamento pendente legitimamente não possui baixa.

## Testes adicionados

`FinanceiroPagamentoDetailsV2104Tests` cobre:

- construtor padrão e propriedades graváveis exigidos pelo Dapper;
- pagamento pendente sem valor pago, data de pagamento ou data prevista;
- aliases explícitos, ausência de `SELECT *`, parâmetros e filtro tenant da query;
- contrato visual de estados, informações, dropdown e ausência de ID manual/padrões nativos proibidos.

O cenário de dados completos é coberto pelo conjunto completo de propriedades exigidas na verificação de materialização. Pagamento inexistente e isolamento são garantidos pelo contrato da cláusula `where`, mantendo o retorno 404 existente.

## Tela financeira alterada

A view `Views/Financeiro/Details.cshtml` e a camada `design-system/financial.css` foram evoluídas com:

- hero contextual e badge semântico para pendente, pago, cancelado ou recusado;
- cards separados para resumo monetário, profissional/plantão e dados de liquidação;
- estados de erro, vazio, encontrado e status sem ações;
- datas, cidade/UF, forma de pagamento, chave Pix, observações e histórico básico;
- formulário de baixa com labels, seletor de forma, date picker, valor numérico validado e feedback de carregamento já integrado ao padrão AJAX;
- ações auditáveis sem `alert()`, `confirm()` ou links vazios;
- grid responsivo, empilhamento mobile, contraste semântico e tipografia numérica tabular.

## Comandos e resultados

| Comando | Resultado |
|---|---|
| `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj -c Debug` | Não executado: SDK `dotnet` não está instalado na imagem (`command not found`). |
| `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Debug --filter Financeiro` | Não executado pelo mesmo limite da imagem. |
| `dotnet clean backend/PlantaoPro.sln` | Não executado pelo mesmo limite da imagem. |
| `dotnet restore backend/PlantaoPro.sln` | Não executado pelo mesmo limite da imagem. |
| builds Debug/Release e teste Release obrigatórios | Não executados pelo mesmo limite da imagem. |
| `python3 scripts/repository-security-check.py` | Aprovado: `repository-security ok`. |
| `python3 scripts/check-csharp10-compatibility.py` | Aprovado: compatibilidade C# 10 e CSS Razor validada. |
| `python3 scripts/validate-scrpt-completo.py` | Aprovado: cobertura 100%. |
| `git diff --check` | Aprovado. |
| busca de padrões proibidos solicitada | Executada; produz ocorrências preexistentes em arquivos fora desta entrega. Nenhuma ocorrência nova foi introduzida nos arquivos alterados. |

## Limitações reais restantes

- A imagem não contém o SDK .NET; por isso build, execução da API e testes xUnit devem ser repetidos no CI ou em ambiente com .NET 10 antes do merge.
- Pelo mesmo motivo, a aplicação web não pôde ser iniciada e não foi possível capturar screenshot runtime. A validação visual ficou limitada ao contrato Razor/CSS e ao verificador de compatibilidade do repositório.
- O endpoint atual não expõe a coleção completa de eventos de `historico_pagamento`; a tela apresenta somente os marcos básicos disponíveis no DTO (`RegDate` e `DataPagamento`), sem inventar eventos.
