# Checklist v1.57 — fluxos operacionais

## Entregue
- [x] Branch de trabalho v1.57 criada.
- [x] Diagnóstico por área com dados disponíveis e pendências reais.
- [x] Drawer canônico `pp-detail-drawer` no shell autenticado.
- [x] Cabeçalho, status, resumo, metadados, timeline/histórico e ações.
- [x] Loading, erro, `aria-live`, `role=dialog` e `aria-modal`.
- [x] Escape, contenção de foco e retorno ao gatilho.
- [x] Full-screen mobile e ações persistentes.
- [x] Plantões com detalhe lateral alimentado pelo DTO real.
- [x] Escalas com médico/CRM, plantão, período, composição e histórico real.
- [x] Ações no drawer limitadas a rotas existentes.
- [x] Script `check-operational-ux.py` e gates existentes ampliados.
- [x] Sem `href="#"`, `alert()` ou `confirm()` nos arquivos alterados.
- [x] JavaScript sem interpolação de HTML de dados operacionais.

## Dependências reais
- [ ] Enriquecer DTOs/BFFs de fechamentos, pacientes, agendamentos, consultas e pagamentos.
- [ ] Expor timeline auditada para todos os domínios.
- [ ] Expor permissões por ação no view model antes de habilitar CTAs críticos.
- [ ] Normalizar tipos de notificação no backend.
- [ ] Disponibilizar métricas agregadas e tempos médios do Saúde 360.

## Runtime
O SDK .NET não está instalado no ambiente desta execução. Restore, build, testes e captura visual autenticada ficam bloqueados por essa limitação; validações estáticas, mobile e sintaxe JS são executadas separadamente.
