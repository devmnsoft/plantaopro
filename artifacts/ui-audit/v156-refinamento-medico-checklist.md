# Checklist de refinamento médico v1.56.0

## Pacientes
- [x] Busca/resultado consome registros reais via módulo Saúde 360.
- [x] Tabela legível, status, empty state e aviso LGPD discreto.
- [x] Cadastro em `pp-form`, campos associados e rodapé de ações.
- [ ] Último atendimento e próximo agendamento: expor apenas quando a API fornecer os campos; não inventar contadores.

## Agendamentos
- [x] Agrupamento por horário, filtros nomeados, status e cards responsivos.
- [x] Ações de confirmar, check-in, chamar, triagem, consulta e cancelar visíveis.
- [x] Botões de modal com `type="button"`; diálogo com botão de fechamento nomeado.
- [ ] Persistência das ações: confirmar com endpoint autenticado em homologação integrada.

## Triagem
- [x] Jornada assistencial, estado vazio útil e acesso auditável.
- [x] Formulário compartilhado usa contrato clínico existente.
- [ ] Classificação de risco, espera e sinais vitais: validar com payload real; nenhuma gravidade foi simulada.

## Consultas
- [x] Contexto do paciente, histórico, anamnese, exame, diagnóstico/CID, conduta, orientações e prescrição estão organizados no workspace.
- [x] Controles de rascunho e finalização preservados.
- [ ] Validar autosave, conflito de versão, prescrição e foco com sessão médica real.

## Saúde 360
- [x] Jornada visual com atalhos para paciente, agendamento, check-in, chamada, triagem, consulta, prescrição e financeiro.
- [x] Quantidade deriva de `registros.Count`; sem contador inventado.
- [x] KPIs, registros e alerta LGPD usam componentes clínicos `pp-*`.
- [ ] Etapa atual: requer sinal do backend para marcar `aria-current` sem inferência incorreta.

## Critérios transversais
- [x] Estados sem dados são explícitos e úteis.
- [x] Verde não foi introduzido fora de sucesso; aviso usa semântica própria.
- [x] Nenhum paciente, médico, valor ou status demonstrativo foi adicionado.
- [x] Tabelas permanecem dentro de wrappers responsivos.
- [ ] Contraste AA e comportamento em 360/390/430/768/1024/1366/1920 devem ser medidos no navegador quando o runtime estiver disponível.
