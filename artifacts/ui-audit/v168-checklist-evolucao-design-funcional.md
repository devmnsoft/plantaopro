# Checklist de evolução de design e funcional — v1.68.0

- [x] Smoke aponta exclusivamente para evidências `v168` e cobre 20 rotas em oito viewports.
- [x] Smoke verifica overflow, shell, topbar, sidebar, cards, tabelas, formulário, posição de labels, dialogs, drawers, toasts e botões apenas por ícone.
- [x] Formulário clínico Saúde 360 usa `pp-form`, foco no primeiro erro e proteção contra saída com alterações.
- [x] Fechamentos apresenta as seis etapas operacionais e renderização responsiva condicionada a dados reais.
- [x] Fechamentos não cria valores, SLA, histórico, responsável ou estado fictício quando a fonte está vazia.
- [x] Empty states explicam a ausência da fonte e a próxima ação válida.
- [x] Gates estáticos v1.68 protegem jornada de fechamentos, tabela mobile e timeline real.
- [ ] Screenshots públicas reais — bloqueadas pela ausência do runtime `.NET`.
- [ ] Screenshots autenticadas reais — bloqueadas pela ausência do runtime e de `PLANTAOPRO_STORAGE_STATE`.
- [ ] Build/teste .NET — bloqueados porque `dotnet` não está instalado.
