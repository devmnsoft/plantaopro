# Diagnóstico funcional e de design — v1.59.0

## Escopo auditado
Foram revisadas as views Saúde 360, Agendamentos, Triagem, Consultas, Pacientes, Financeiro, Pagamentos, Convites, Relatórios, Configurações e Admin SaaS; os drawers compartilhados; command palette, toast e serviços/controllers web e API relacionados.

## Diagnóstico por módulo
| Módulo | Existente e ações reais | Lacunas encontradas | Correção aplicada / pendência real |
|---|---|---|---|
| Saúde 360 | API autenticada `clinica-dashboard/resumo`, jornada navegável e isolamento por tenant | resumo encerrava em triagem e não explicitava consulta, prescrição e financeiro | agregado passou a consultar consultas em andamento/finalizadas, prescrições e contas abertas; jornada exibe os totais apenas quando retornados |
| Agendamentos | CRUD, confirmar, check-in, cancelar e reagendar existem na API | modal encerrava sem chamar backend; toast afirmava apenas ação “preparada”; campos ausentes eram preenchidos como se fossem dados | BFF web autenticado, confirmação modal, motivo obrigatório, loading, erro/sucesso e recarga; ausências da API são identificadas honestamente |
| Triagem | fila, rascunho/início/finalização e validação tipada de sinais vitais | telas genéricas ainda não concentram fila e formulário no mesmo workspace | pendente: consolidar workspace sem duplicar regra clínica existente |
| Consultas | workspace de atendimento, rascunho, histórico, CID e prescrição | finalização segue rota legada na API | pendente: estabilizar rota canônica antes de novo CTA |
| Pacientes | cadastro, busca, histórico e resumo clínico protegidos | drawer longitudinal unificado não existe | pendente; manter navegação real e mascaramento já usado nos formulários |
| Fechamentos | fechamento operacional existe em Operação Premium | workflow financeiro não possui agregado web único | pendente de contrato canônico de fechamento/pagamento |
| Financeiro/Pagamentos | filtros e confirmar/cancelar pagamento; resumo clínico financeiro | contestação não tem endpoint canônico | ação não deve ser exibida até existir regra de domínio |
| Convites | listagem e fluxo operacional existente | histórico/tentativas não têm BFF dedicado | pendente de endpoint de timeline |
| Notificações | fonte real no serviço e empty state | agrupamento clínico ainda depende dos tipos retornados | não criar notificações sintéticas |
| Command Palette | Ctrl/Cmd+K, Escape nativo do dialog, busca `/GlobalSearch` e estados vazio/erro | nenhum bloqueio funcional localizado | mantida sem resultados locais artificiais |
| Relatórios | rotas de geração/exportação existentes para relatórios implementados | biblioteca não normaliza metadados de todos os formatos | recursos sem endpoint permanecem em implantação |
| Configurações | cards navegam para controllers reais | “última atualização” não está disponível em todos os domínios | não exibir data inventada |
| Admin SaaS | container autenticado, cards e links reais | dados de plano/limites não chegam ao view model comercial | pendente de BFF SaaS; layout preservado sem KPIs falsos |

## UX, responsividade e acessibilidade
A agenda mantém cards responsivos, filtros em grid e modal Bootstrap. O modal agora possui `role=dialog`, nome/descrição acessíveis, motivo vinculado por `aria-describedby`, estado ocupado e região viva no toast. O drawer compartilhado continua full-screen no mobile e restaura foco. Nenhum novo `href="#"`, `alert()` ou `confirm()` foi introduzido.

## Segurança e LGPD
O browser envia apenas identificador, operação e motivo. O BFF recupera o JWT da sessão, restringe operações por allowlist e evita dados identificáveis em logs; falhas usam mensagens amigáveis.
