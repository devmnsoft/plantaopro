# Funcionalidades abertas e regras de negócio — v1.76.0

> Inventário estático da interface existente. “Ativa” significa que há rota/controller real no repositório; não implica homologação de runtime. Ações dependentes de dados continuam condicionadas ao identificador e ao estado retornados pelo backend.

| Tela | Funcionalidade e regra esperada | Controller/action e view/JS | Backend/rota/endpoint | Estado da ação | Correção e pendência real |
|---|---|---|---|---|---|
| Login | Autenticar, recuperar senha e impedir envio duplo | `Account/Login`; `Views/Account/Login.cshtml`; `auth-login.js` | Sim/sim/sim | Ativa | Feedback e acessibilidade presentes; runtime pendente |
| Cadastro self-service | Validar campos e criar empresa somente no submit real | `Cadastro`; `Views/Cadastro/Cadastro.cshtml`; `form-experience.js` | Sim/sim/sim | Ativa | Estado sujo, foco inválido e feedback presentes |
| Planos | Exibir somente planos retornados | `Planos/Index`; `Views/Planos/*` | Sim/sim/sim | Ativa conforme resposta | Não presumir plano quando a fonte estiver vazia |
| Admin SaaS | Governar tenants conforme autorização | `AdminSaas/Index`; `Views/AdminSaas/*` | Sim/sim/sim | Restrita | Política real permanece no servidor |
| Minha Assinatura | Exibir assinatura real e seu estado vazio | `MinhaAssinatura/Index`; view homônima | Sim/sim/sim | Restrita | Sem plano ou valor presumido |
| Configurações | Persistir preferências permitidas | `Configuracoes/Index`; `Views/Configuracoes/*` | Sim/sim/sim | Restrita | Servidor valida permissão |
| Relatórios | Consultar somente relatórios implementados | `Relatorios/Index`; `Views/Relatorios/*` | Parcial/sim/parcial | Limitada | Gerar/exportar fica indisponível quando não há endpoint |
| Dashboard | Consolidar indicadores por perfil sem completar ausências com zero | `Home/Dashboard`; `Views/Home/Dashboard.cshtml` | Sim/sim/sim | Ativa | Empty state distingue ausência de indicador |
| Minha Central | Priorizar itens reais do usuário | `MinhaCentral/Index` | Sim/sim/sim | Ativa | Coleção vazia não gera contador |
| Meu Dia | Exibir compromissos retornados | `MeuDia/Index` | Sim/sim/sim | Ativa | Sem agenda fictícia |
| Agenda | Ordenar agenda e filtrar dados reais | `Agenda/Index`; `Views/Agenda/*` | Sim/sim/sim | Ativa | Atraso depende dos horários recebidos |
| Agendamentos | Check-in/triagem/consulta somente no estado e vínculo permitidos | `Agendamentos`; `Views/Agendamentos/*` | Sim/sim/parcial | Condicionada | Ações sem vínculo permanecem indisponíveis com motivo |
| Saúde 360 | Navegar pela jornada assistencial existente | `Saude360/Index` | Sim/sim/sim | Ativa | Não cria vínculo clínico |
| Pacientes | Exibir/editar dados reais com proteção LGPD | `Pacientes`; `Views/Pacientes/*` | Sim/sim/sim | Condicionada | Histórico vazio permanece explícito |
| Triagem | Validar sinais e finalizar somente com mínimos clínicos | `Triagem`; `Views/Triagem/*` | Sim/sim/parcial | Condicionada | Finalização depende de vínculo e validação do servidor |
| Consultas | Evoluir/finalizar e abrir faturamento somente com origem real | `Consultas`; `Views/Consultas/*` | Sim/sim/parcial | Condicionada | Prescrição/CID não são simulados |
| Faturamento Clínico | Preservar valor/status ausentes e origem real | `FaturamentoClinico/Index`; view e JS homônimos | Sim/sim/sim | Ativa conforme dado | Não converte ausente em zero/pago |
| Financeiro | Abrir itens gerados por fluxo real | `Financeiro/Index`; `Views/Financeiro/*` | Sim/sim/sim | Ativa conforme dado | Sem pagamento presumido |
| Pagamentos | Exibir pagamento existente e seu status real | `Pagamentos/Index`; `Views/Pagamentos/*` | Sim/sim/sim | Ativa conforme dado | Empty state quando não há pagamento |
| Plantões | Publicar/cancelar conforme mínimos e estado | `Plantoes`; `Views/Plantoes/*` | Sim/sim/parcial | Condicionada | Convite/cancelamento dependem de endpoint e motivo |
| Escalas | Confirmar/substituir conforme escala real | `Escalas`; `Views/Escalas/*` | Sim/sim/parcial | Condicionada | Conflito não é inferido na UI |
| Fechamentos | Conferir → divergir → aprovar → financeiro | controllers/views de fechamento e financeiro | Parcial/sim/parcial | Condicionada | Motivos e transições são validados no backend |
| Notificações | Contador e itens somente da API, same-origin | API + `notification-drawer.js` | Sim/sim/sim | Ativa | Filtro vazio não inventa item |
| Command Palette | Navegar apenas para catálogo same-origin | layout + `command-palette.js` | Sim/sim/não aplicável | Ativa | Teclado, seleção ARIA, Escape e retorno de foco |

## Pendências transversais

- Homologar autorização e transições com usuários reais de cada perfil em runtime.
- Homologar faixas clínicas e motivos obrigatórios contra as validações da API.
- Manter desabilitada qualquer ação de relatório/exportação sem endpoint contratado.
- Executar o smoke autenticado v176 com estado Playwright válido; o SDK .NET não está instalado neste ambiente.
