# Scanner de controllers duplicados — v1.73.0

## Resultado inicial

O scanner identificou `FaturamentoClinicoController` em `V114ProdutoWebControllers.cs` e `V115FaturamentoWebControllers.cs`. As partes eram `partial`; a base `Controller` aparecia somente na primeira parte. As actions eram distintas, mas a distribuição mantinha risco de regressão estrutural.

`MinhaAssinaturaController` permaneceu com uma única declaração dedicada.

## Correção e resultado final

As actions úteis foram consolidadas em `Controllers/FaturamentoClinicoController.cs`, com rota pública MVC preservada em `/FaturamentoClinico`. As duas declarações parciais foram removidas. O scanner final não encontrou controller declarado em mais de um arquivo.

O gate `scripts/check-controllers-uniqueness.py` também verifica bases de partials, actions `Index` de assinatura idêntica e unicidade dos dois controllers críticos.
