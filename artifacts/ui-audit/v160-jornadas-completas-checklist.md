# Checklist de homologação v1.60

## Entregue

- [x] Saúde 360 com oito etapas e indicadores somente quando retornados pela API.
- [x] Agenda com horário, paciente, profissional, atendimento, convênio, sala, status, próxima ação e drawer.
- [x] Confirmar, check-in e cancelar passam pelo BFF com antiforgery, loading e feedback.
- [x] Reagendar abre a edição real; triagem recebe o vínculo do agendamento.
- [x] Chamada sem endpoint fica desabilitada e explica o motivo.
- [x] Triagem valida classificação, pressão, temperatura, saturação, frequência cardíaca e observação de alto risco no servidor e no navegador.
- [x] Command Palette usa pesquisa real, Ctrl/Cmd+K, Escape e retorno de foco.
- [x] Relatórios futuros continuam sem CTA.
- [x] Smoke visual aponta para screenshots v160 e viewports oficiais.
- [x] Gate operacional cobre jornada, agenda, triagem, drawer e Command Palette.
- [x] Sem `href="#"`, `alert()` ou `confirm()` nas views críticas verificadas.

## Validado estaticamente

- [x] Scripts Python de compatibilidade, UI, layout, formulários, feedback, SaaS, UX operacional e segurança.
- [x] Sintaxe de JavaScript com `node --check`.
- [x] Geração e validação do script consolidado executadas apenas como verificação; mudanças de banco não fazem parte da entrega funcional.

## Bloqueios do ambiente

- [ ] Restore/build/test .NET: SDK `dotnet` ausente no container.
- [ ] Runtime e screenshots pós-login: aplicação não pode iniciar sem SDK; também requer estado autenticado.

## Pendências reais de produto/backend

- [ ] Definir entidade, máquina de estados e permissões da central de Fechamentos.
- [ ] Publicar endpoint transacional de chamada do paciente.
- [ ] Publicar contratos de rascunho/finalização/encaminhamento da triagem quando separados do CRUD.
- [ ] Publicar endpoints auditáveis para aprovar/pagar/contestar/resolver.
- [ ] Publicar reenvio/cancelamento de convite e histórico de tentativas.
