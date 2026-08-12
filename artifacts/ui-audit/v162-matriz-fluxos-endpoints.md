# v1.62 — matriz de fluxos e endpoints

Esta matriz registra somente contratos já presentes no código. Disponibilidade final depende de autenticação, tenant, papel e resposta do backend.

| Módulo | Fluxo / ação | Contrato real | Estado nesta revisão |
|---|---|---|---|
| Agendamentos | confirmar, check-in, reagendar e cancelar com motivo | BFF protegido em `Saude360WebControllers.cs` (`ExecutarAcao`) | implementado; validar runtime |
| Agendamentos | chamar paciente | sem contrato confirmado | desabilitar com explicação; não simular |
| Triagem | iniciar, salvar e finalizar | formulário tipado e validação server-side | implementado; validar perfil clínico |
| Consultas | atendimento, histórico e finalização | controllers/views de Consultas | implementado; validar LGPD e permissões |
| Minha Central | assumir, concluir, adiar, reabrir, comentar e histórico | endpoints consumidos por `work-item-drawer.js` | implementado; trata 403/409 |
| Plantões / Escalas | abrir detalhes | drawer global e URL fornecida pelo servidor | implementado |
| Pagamentos | listar e filtrar | controller Pagamentos e modelo paginado | implementado; ações não expostas sem endpoint |
| Financeiro | consolidar e abrir detalhes | controller Financeiro e dados do modelo | implementado; validar competência |
| Fechamentos | conferência e pendências | `/bff/fechamentos` e `/fechamentos/pendentes` referenciados pela view | validar disponibilidade no runtime |
| Notificações | listar não lidas, marcar e excluir | `/bff/operacao/notificacoes` consumido pelo drawer | implementado; sem itens sintéticos |
| Command Palette | busca global | `/GlobalSearch`, atalho Ctrl/⌘+K e Escape | implementado; resultado depende da API |
| Relatórios | abrir visões existentes | actions de `RelatoriosController` | implementado; automação sem CTA |
| Configurações | conta, permissões, assinatura, marca, notificações, LGPD, integrações e parâmetros | rotas MVC explícitas nos cards | implementado; validar autorização |

## Ações críticas

A interface deve preservar antiforgery, confirmação acessível, `aria-busy`, feedback por toast e tratamento explícito de 403. Onde qualquer contrato estiver ausente, a ação deve permanecer indisponível em vez de produzir sucesso local.
