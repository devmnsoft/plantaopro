# v1.67 — diagnóstico visual por rota

> Auditoria estática concluída em 13/08/2026. O SDK .NET não está instalado neste ambiente; portanto, os estados abaixo são **aceitável (estático)**, não constituem homologação navegada nem evidência visual. A execução Playwright permanece pendente em ambiente com runtime e sessão autenticada.

| Rota | Status | Visual / responsivo | Form / card / tabela | Modal / drawer | Correção aplicada | Pendência real |
|---|---|---|---|---|---|---|
| `/` | aceitável | Hero e grid têm contratos responsivos | cards públicos limitados | n/a | smoke valida proporção, cards e overflow | captura navegada |
| `/Account/Login` | aceitável | shell, benefícios e ações empilham no mobile | `pp-form`, labels, erros e ajuda | n/a | smoke valida conteúdo sem corte | captura e teclado real |
| `/cadastro/empresa` | aceitável | stepper e resumo passam a uma coluna | grid/card `pp-onboarding-form` | confirmação deve iniciar oculta | smoke valida contrato self-service | completar fluxo real |
| `/Planos` | aceitável | grade fluida | cards de plano | n/a | incluída como rota pública | captura navegada |
| `/AdminSaas/Index` | aceitável | layout executivo responsivo | KPIs/cards protegidos | portal global | smoke aceita `pp-admin-layout` | sessão Admin SaaS |
| `/Home/Dashboard` | aceitável | hero e KPIs responsivos | dados reais e empty states | portal global | contratos de página/tabela | sessão por perfil |
| `/MinhaCentral` | aceitável | filtros e cards mobile | prioridades e ações reais | drawer acessível | gate mantém 403/409 e ações | testar conflitos reais |
| `/MeuDia` | aceitável | composição de página | listas operacionais | portal global | rota/viewports no smoke | sessão autenticada |
| `/Agenda` | aceitável | alternativa mobile | tabela protegida | drawer global | rota/viewports no smoke | dados reais |
| `/Plantoes` | aceitável | tabela responsiva | badges e ações | drawer de detalhe | gate de ações e drawer | dados reais |
| `/Escalas` | aceitável | tabela responsiva | badges e ações | drawer de detalhe | gate de ações e drawer | dados reais |
| `/Saude360` | aceitável | jornada por etapas | cards clínicos reais | portal global | gate das oito etapas | dados clínicos autorizados |
| `/Pacientes` | aceitável | wrapper/mobile | lista e empty state | portal global | gate operacional | dados reais |
| `/Agendamentos` | aceitável | wrapper/mobile | form e lista | confirmação acessível | gate anti-CSRF/feedback | executar ações reais |
| `/Triagem` | aceitável | composição clínica | limites e validação server-side | portal global | gate clínico | dados reais |
| `/Consultas` | aceitável | wrapper/mobile | lista operacional | portal global | gate operacional | dados reais |
| `/Pagamentos` | aceitável | wrapper/mobile | status e ações | drawer financeiro | smoke e gates | dados reais |
| `/Financeiro` | aceitável | KPIs fluidos; tabela vira cards | consolidação, composição e histórico | detalhe disponível | workspace, KPIs e cards mobile revisados | validar competência real |
| `/Relatorios` | aceitável | wrapper responsivo | filtros/lista | portal global | gate operacional | exportação real |
| `/Configuracoes` | aceitável | composição responsiva | forms existentes | portal global | gate operacional | permissões por perfil |

Nenhuma rota foi marcada como “premium”: sem runtime, seria uma afirmação sem evidência. Nenhuma rota foi marcada como quebrada após a revisão estática.
