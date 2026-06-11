# Base clínica mínima — Saúde 360

## Objetivo

A base clínica mínima inicializa as tabelas necessárias para os endpoints clínicos já publicados no Saúde 360 sem apagar dados existentes.

## Tabelas criadas ou compatibilizadas

- `plantaopro.pacientes`
- `plantaopro.agendamentos`
- `plantaopro.painel_chamada_fila`
- `plantaopro.painel_chamada_historico`
- `plantaopro.triagens`
- `plantaopro.triagem_sinais_vitais`
- `plantaopro.triagem_classificacoes_risco`
- `plantaopro.triagem_historico`

## Regras de multi-tenant

As tabelas possuem `tenant_id` e `cliente_id` para escopo SaaS. O CPF do paciente não é obrigatório e o índice único parcial considera `cliente_id`, `cpf` não vazio e `reg_status = 'A'`.

## Dados padrão

A migration insere classificações de risco padrão sem duplicar registros:

- `EMERGENCIA`
- `MUITO_URGENTE`
- `URGENTE`
- `POUCO_URGENTE`
- `NAO_URGENTE`

## Comportamento sem dados

Quando as tabelas existem mas estão vazias:

- O dashboard clínico retorna contadores zerados.
- A listagem de pacientes retorna uma lista vazia.

## Privacidade e LGPD

A base evita exigir campos sensíveis como CPF. Logs e auditorias técnicas não devem registrar prescrição, diagnóstico, anamnese, triagem detalhada, senha, token, hash ou headers de autorização.
