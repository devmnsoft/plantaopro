# Diagnóstico v1.58 — workflows reais

## Método
Auditoria estática das views, controllers, BFFs, modelos e JavaScript solicitados, confrontando cada ação exibida com uma rota existente. Nenhum número ou registro de demonstração foi adicionado.

## Pendências / Minha Central
- **Existe hoje:** `work_items` persistidos, escopo de tenant/unidade, concorrência otimista por versão, idempotência, auditoria, histórico e BFF autenticado.
- **Ações reais:** mover no Kanban, assumir, encaminhar (API), comentar, adiar, concluir e reabrir.
- **Lacunas visuais encontradas:** drawer interpolava descrição em `innerHTML`, não mostrava histórico, não expunha as mutações existentes e não tratava 403/409. Não havia filtros.
- **Implementado:** filtros por prioridade, tipo, prazo e atribuição; drawer seguro com metadados, timeline real e estados de loading/erro/vazio; assumir, resolver, adiar, reabrir e comentar com feedback. Reatribuição permanece API-only até existir fonte real de usuários permitidos.

## Plantões e Escalas
- **Existe hoje:** drawers canônicos v1.57, details/edit, calendário, substituição e rotas operacionais existentes.
- **Lacuna:** o agregado financeiro/fechamento ainda não é retornado pelo endpoint de detalhe; não se adicionaram CTAs sem destino.
- **Próxima rodada:** ampliar DTOs reais antes de renderizar cobertura, presença e pagamento.

## Fechamentos, Financeiro e Pagamentos
- **Existe hoje:** central em `OperacaoPremium/Fechamentos`, financeiro e pagamentos com controllers próprios.
- **Lacuna:** não há um único agregado versionado que represente o stepper Plantão → pagamento. A aprovação deve permanecer nas rotas já autorizadas, sem sintetizar estados na view.

## Saúde 360, Pacientes, Agendamentos, Triagem e Consultas
- **Existe hoje:** controllers clínicos, workflow Saúde 360, telas de fila, check-in, atendimento, histórico e prescrições.
- **Lacuna:** os endpoints são separados; drawers longitudinais exigem BFF com política LGPD e projeções mascaradas. A rodada não duplica consultas nem expõe documento completo.

## Convites, Notificações, Relatórios e Configurações
- **Existe hoje:** controllers e telas reais; notificações já usam drawer e fonte BFF; command palette usa busca global.
- **Lacuna:** tentativas de convite e última geração de relatório não estão presentes nos view models atuais. Recursos futuros devem continuar sem CTA.

## Decisão de escopo
A implementação concentra a evolução funcional no workflow com contrato de domínio completo (`work_items`). Os demais módulos foram auditados e mantidos sem botões fictícios; suas dependências de backend estão explicitadas na matriz.
