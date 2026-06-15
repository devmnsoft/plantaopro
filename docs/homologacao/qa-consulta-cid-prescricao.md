# QA — Consulta + CID + Prescrição

## Roteiro funcional

- [ ] Login admin.
- [ ] Criar paciente.
- [ ] Criar agendamento.
- [ ] Realizar check-in.
- [ ] Iniciar triagem.
- [ ] Finalizar triagem.
- [ ] Iniciar consulta.
- [ ] Preencher anamnese.
- [ ] Preencher exame físico.
- [ ] Buscar CID por código/descrição.
- [ ] Vincular CID.
- [ ] Criar conduta.
- [ ] Criar prescrição.
- [ ] Finalizar prescrição.
- [ ] Imprimir prescrição.
- [ ] Finalizar consulta.
- [ ] Ver histórico do paciente.
- [ ] Confirmar auditoria clínica.
- [ ] Confirmar que recepção não vê diagnóstico/prescrição.
- [ ] Confirmar que financeiro não vê anamnese, diagnóstico ou prescrição.
- [ ] Build API verde.
- [ ] Build Web verde.

## Critérios de aceite

Todos os módulos devem respeitar tenant, perfil e LGPD. A auditoria deve registrar acesso a consulta, histórico, prescrição e impressão sem armazenar conteúdo clínico sensível nos logs técnicos.
