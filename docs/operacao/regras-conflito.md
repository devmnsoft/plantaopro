# Motor de conflito de horário

## Fórmula de conflito
Há conflito quando:

`novoInicio < escalaExistenteFim AND novoFim > escalaExistenteInicio`

## Status considerados
Considerar:
- `SOLICITADA`
- `CONFIRMADA`
- `EM_ANDAMENTO`

Não considerar:
- `CANCELADA`
- `RECUSADA`
- `SUBSTITUIDA`

## Contrato esperado
- `PossuiConflito`
- `TotalConflitos`
- `Conflitos[]`
- `Grau` (`LEVE`, `MODERADO`, `CRITICO`)

## Aplicação obrigatória
- Solicitação de plantão.
- Aceite de convite.
- Confirmação de escala.
- Substituição de escala.
- Geração de recomendação médica.
