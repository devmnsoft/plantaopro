# Checklist das jornadas clínicas v1.72.0

- [x] Saúde 360 apresenta Paciente → Agendamento → Check-in → Chamada → Triagem → Consulta → Prescrição → Financeiro.
- [x] Indicadores só aparecem quando retornados pela API; lista vazia informa ausência de registros reais.
- [x] Agenda usa o BFF autenticado e antiforgery para confirmar, check-in e cancelar com motivo.
- [x] Chamada permanece desabilitada, com motivo, enquanto não há endpoint transacional.
- [x] Triagem valida classificação, pressão, temperatura, saturação, frequência cardíaca e observação em alto risco no servidor.
- [x] Consulta carrega contexto pelo identificador real, salva rascunho, trata conflito e finaliza por APIs autenticadas.
- [x] Prescrição é aberta a partir da consulta real.
- [x] Views não semeiam pacientes, riscos, históricos ou valores fictícios.
- [x] LGPD: conteúdo clínico não é colocado em logs técnicos e o acesso permanece sob tenant/perfil.
