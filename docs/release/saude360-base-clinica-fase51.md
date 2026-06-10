# PlantãoPro Saúde 360 — Base Clínica Fase 5.1

## Escopo entregue

A Fase 5.1 estabelece a jornada assistencial base:

**Paciente -> Agendamento -> Check-in -> Painel de chamada -> Triagem -> Encaminhamento para consulta futura**.

Foram adicionados SQL incremental, endpoints API, telas MVC reutilizando o design system, menus clínicos, perfis/permissões e documentação operacional.

## Módulos

- Pacientes: cadastro, busca, histórico, inativação/reativação e resumo clínico auditado.
- Agendamento: criação, edição, confirmação, check-in, cancelamento, reagendamento, falta, agenda diária e filtros base.
- Painel de chamada: fila, chamada, rechamada, ausência, finalização, histórico e TV com token público previsto na modelagem.
- Triagem: fila, criação, início, finalização, cancelamento, sinais vitais, classificação de risco e encaminhamento para consulta.
- Dashboard clínico: KPIs operacionais base por tenant.

## Não escopo

- Consulta médica completa.
- CID integrado à consulta.
- Prescrição médica completa.
- Convênios avançados, glosas e faturamento TISS.
- Financeiro clínico completo.

## Pendências reais

- Validar fluxo com banco real e usuários de cada perfil em ambiente com `dotnet` disponível.
- Evoluir telas específicas por módulo após homologação da base.
- Integrar autorização fina a políticas centralizadas quando o módulo de permissões dinâmicas estiver consolidado.
