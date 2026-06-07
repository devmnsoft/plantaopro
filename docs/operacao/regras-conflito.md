# Regras de conflito de horário

## Fórmula
Um conflito existe quando:

```text
novoInicio < escalaExistenteFim
AND
novoFim > escalaExistenteInicio
```

## Status que contam
- `solicitado`
- `confirmado`
- `em_andamento`, quando existir no ciclo operacional

## Status ignorados
- `cancelado`
- `recusado`
- `substituido`
- `nao_compareceu`

## Aplicação obrigatória
- Solicitar plantão.
- Aceitar convite.
- Confirmar escala.
- Substituir médico.
- Recomendar médicos.

## UX esperada
- Exibir badge “Conflito”.
- Apresentar modal com detalhes do plantão conflitante.
- Bloquear conflito crítico com toast amigável.
- Registrar auditoria em tentativas bloqueadas quando a ação for crítica.
