# QA do menu global

## Objetivo
Validar que o menu do PlantãoPro siga jornada lógica, perfil de acesso e não aponte para 404.

## Checklist
- [ ] Admin Global visualiza Admin SaaS, Relatórios, Ajuda e Governança.
- [ ] Admin Cliente visualiza Gestão do Cliente, Atendimento, Plantões, Financeiro, Convênios, Relatórios e Ajuda.
- [ ] Recepção visualiza Início, Pacientes, Agendamentos, Check-in, Painel e Ajuda.
- [ ] Triagem visualiza Fila, Triagem, Painel e Ajuda.
- [ ] Médico visualiza agenda, consultas, prescrições, CID e plantões próprios.
- [ ] Financeiro visualiza financeiro clínica e convênios financeiros, sem anamnese/evolução.
- [ ] Auditor visualiza relatórios, auditoria e LGPD.
- [ ] Módulo bloqueado mostra CTA comercial, não 404.
- [ ] Nenhum item usa `href="#"`.
- [ ] Nenhum item aponta para action vazia.

## Rotas Saúde 360 ajustadas
As actions de Atendimento, Histórico, Favoritos, Mais Usados, Contas a Receber, Repasses, Glosas, Contratos, Planos, Faturamento, Coberturas e Autorizações possuem título, descrição, endpoint e ações próprias.
