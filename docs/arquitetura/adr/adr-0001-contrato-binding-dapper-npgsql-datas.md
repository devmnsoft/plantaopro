# ADR-0001 — Contrato de binding Dapper/Npgsql para datas

- **Status:** aceita (rodada 19, r19b1)
- **Contexto:** o reader do Npgsql devolve `System.DateTime` tanto para `timestamp` quanto para `timestamptz`. O Dapper não converte esse valor em `System.DateTimeOffset` quando o alvo é **parâmetro de construtor de record**: exige assinatura exata e lança `InvalidOperationException: A parameterless default constructor or one matching signature (System.DateTime X) is required for <tipo> materialization` na primeira linha materializada. Propriedades de **classe**, por outro lado, são mapeadas com conversão e materializam normalmente (probe versionado em `R19DapperBindingGuardTests.Mecanismo_*`).

## Decisão

1. Todo tipo materializado pelo Dapper (`QueryAsync<T>` e variantes) que recebe coluna de data/hora declara `DateTime` / `DateTime?`:
   - **records**: parâmetro de construtor obrigatoriamente `DateTime` — `DateTimeOffset` é defeito (família F7b: R18 `WorkItemDto`; R19 `OcorrenciaDto`, `OcorrenciaEventoDto`, `WorkItemHistoryDto`, `ProductivityItemDto`);
   - **classes**: padrão da casa também é `DateTime` (uniformidade; o mecanismo tolera DTOFF, mas a divergência esconde intentos).
2. `DateTimeOffset` continua legítimo em fronteiras JSON: requests/filtros de entrada (`NotificationFilter.De/Ate`, `WorkItemPostponeRequest.DueAt`) e DTOs/ViewModels montados em código a partir de valores `DateTime` (conversão implícita preservando Kind UTC do reader).
3. Todo registro que precisa de data local (dia/horário do usuário) converte no ponto de uso, não no tipo de binding (precedente R18: `WorkItemService` `.Date` sobre o UTC do reader).

## Consequências

- Guarda versionada: `R19DapperBindingGuardTests.Guard_FamiliaF7b_NenhumRecordComParâmetroDateTimeOffsetÉLigadoAoDapper` — interseção global ∅ entre records com parâmetro DTOFF e tipos referenciados em `Query*Async<T>`.
- Novo record ligado ao Dapper entra com `DateTime` desde o nascimento; revisor procura a palavra-chave do erro acima em qualquer 500 novo de listagem (a mensagem é o fingerprint da família).
