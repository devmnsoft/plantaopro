# Homologação visual PlantãoPro v1.56.0

## Escopo e método

Auditoria realizada em 11/08/2026 por inspeção estrutural das views Razor, contratos `pp-*`, CSS responsivo e scripts de interação. O contêiner não possui o SDK .NET (`dotnet: command not found`); portanto, não foi possível iniciar o runtime, autenticar, medir contraste renderizado nem gerar screenshots reais. Os itens “carregou”, alinhamento do shell, overflow e mobile ficam **bloqueados para confirmação visual**, e não são declarados como aprovados por inferência.

Legenda: **S** = atendido por inspeção estática; **B** = bloqueado sem runtime; **N/A** = não se aplica. Em “Shell”, a sequência é topbar/sidebar/footer. Em “Conteúdo”, a sequência é cards/forms/botões. Em “Riscos”, a sequência é bullets/Bootstrap dominante/contraste/overflow/mobile.

| Rota | Layout | Shell | Conteúdo | Riscos | Correção v1.56 | Pendente real |
|---|---|---|---|---|---|---|
| `/login` | B | N/A/N/A/N/A | S/S/S | S/S/B/B/B | Contrato v1.55 preservado | Render e contraste nos 4 breakpoints |
| `/MinhaCentral` | B | B/B/B | S/N/A/S | S/S/B/B/B | Contrato `pp-*` preservado | Homologação autenticada |
| `/MeuDia` | B | B/B/B | S/N/A/S | S/S/B/B/B | Workspace preservado | Homologação autenticada |
| `/Home/Dashboard` | B | B/B/B | S/N/A/S | S/S/B/B/B | Shell validado estaticamente | Homologação autenticada |
| `/Agenda` | B | B/B/B | S/S/S | S/S/B/B/B | Drawer e composição operacional validados por gate | Testar conflitos e foco no drawer |
| `/Plantoes` | B | B/B/B | S/S/S | S/S/B/S/B | KPIs reais, cobertura, tabela desktop e cards mobile preservados | Testar drawer/detalhe com dados reais |
| `/Escalas` | B | B/B/B | S/N/A/S | S/S/B/S/B | Tabela ganhou contexto de cobertura/pagamento, empty state e ações tipadas | Timeline depende de fonte de dados/API |
| `/Fechamentos` | B | B/B/B | S/N/A/S | S/S/B/B/B | Estrutura existente auditada | Validar estados de divergência no runtime |
| `/Saude360` | B | B/B/B | S/N/A/S | S/S/B/S/B | Jornada, KPIs reais, tabela e aviso LGPD migrados para composição clínica `pp-*` | Validar etapa ativa com rota real |
| `/Pacientes` | B | B/B/B | S/S/S | S/S/B/S/B | Herdou jornada Saúde 360 refinada; formulário e LGPD preservados | Validar busca e ações com retorno real |
| `/Agendamentos` | B | B/B/B | S/S/S | S/S/B/S/B | Agenda premium migrou hero, filtros, status e cards; botões receberam tipo explícito | Ligar confirmação do modal à API real |
| `/Triagem` | B | B/B/B | S/S/S | S/S/B/S/B | Herdou jornada e empty state clínicos refinados | Validar risco, espera e sinais vitais com API |
| `/Consultas` | B | B/B/B | S/S/S | S/S/B/S/B | Workspace clínico e jornada existentes auditados | Testar rascunho/finalização autenticados |
| `/Convites` | B | B/B/B | S/S/S | S/S/B/B/B | Padrões existentes preservados | Homologação autenticada |
| `/Pagamentos` | B | B/B/B | S/S/S | S/S/B/B/B | Padrões existentes preservados | Homologação financeira autenticada |
| `/Relatorios` | B | B/B/B | S/S/S | S/S/B/B/B | Form contract auditado | Validar exportações e tabelas extensas |
| `/Configuracoes` | B | B/B/B | S/S/S | S/S/B/B/B | Form contract auditado | Validar sticky footer em formulário longo |
| `/AdminSaas` | B | B/B/B | S/N/A/S | S/S/B/S/B | Cockpit `pp-*` preservado e coberto pelo gate SaaS | Validar tenants/limites com carga real |
| `/Planos` | B | B/B/B | S/S/S | S/S/B/S/B | Grid responsivo e catálogo real preservados | Validar moedas e textos extremos |
| `/Onboarding` | B | B/B/B | S/S/S | S/S/B/S/B | Landing deixou Bootstrap cru e passou a hero, fluxo e stepper `pp-*` | Wizard ainda envia em página única; validar feedback do servidor |

## Acessibilidade e responsividade

- O gate estático cobre `type` em botões críticos, ausência de `href="#"`, composição das views, menu sem listas cruas, footer no shell e `pp-content` flexível.
- Modais/toasts compartilhados mantêm `role="dialog"`, região `aria-live` e confirmação sem `alert()`/`confirm()` nativos.
- CSS possui regras móveis em 430 px e 767 px; tabelas críticas usam contêiner responsivo e Plantões possui cartões dedicados abaixo de `lg`.
- A confirmação de AA, Escape, retorno de foco e ausência de overflow exige navegador e permanece bloqueada, sem falso positivo documental.

## Screenshots

Não gerados: o runtime ASP.NET não pode ser iniciado sem SDK .NET. A pasta de screenshots não foi criada para evitar artefatos vazios ou imagens que não representem o produto real.
