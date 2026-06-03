# Mobile API endpoints

## Objetivo
Documento de homologação e demonstração para o PlantãoPro como MVP comercial homologável em produção controlada. Deve validar fluxo operacional médico, fluxo SaaS básico, API Mobile MVP, segurança e multiempresa, auditoria, observabilidade, suporte, Customer Success e faturamento SaaS.

## Usuários de teste
| Perfil | Usuário sugerido | Uso esperado |
| --- | --- | --- |
| ADMINISTRADOR_GLOBAL | admin.global@plantaopro.local | Gestão de clientes, planos, assinaturas, faturamento SaaS, observabilidade e auditoria global. |
| ADMINISTRADOR | admin.cliente@hospital.local | Administração do cliente, hospitais, médicos, usuários e relatórios do próprio cliente. |
| COORDENACAO | coordenacao@hospital.local | Criação/publicação de plantões, confirmação de escalas e Central de Escala. |
| FINANCEIRO | financeiro@hospital.local | Geração, confirmação, cancelamento e contestação de pagamentos médicos. |
| MEDICO | medico@hospital.local | Minha Agenda, plantões disponíveis, convites, escalas, pagamentos e notificações. |
| HOSPITAL | hospital@hospital.local | Visualização de plantões, escalas confirmadas e comunicação da unidade. |

## Passo a passo principal
1. Entrar como ADMINISTRADOR_GLOBAL e validar Dashboard com clientes ativos, suspensos, faturas vencidas e clientes em risco.
2. Criar cliente, plano ativo e assinatura ativa; validar uso do plano em barras/progresso na Web ou via API.
3. Criar hospital, especialidade e médico respeitando cliente_id e limites contratados.
4. Criar plantão em RASCUNHO, publicar e validar bloqueio caso cliente esteja SUSPENSO/CANCELADO ou limite mensal tenha sido atingido.
5. Entrar como MEDICO, abrir área mobile-first, listar plantões disponíveis, solicitar plantão e validar conflito/duplicidade.
6. Entrar como COORDENACAO, confirmar escala, reduzir vaga disponível, marcar escala como REALIZADA e conferir auditoria/notificação.
7. Entrar como FINANCEIRO, gerar pagamento somente de escala REALIZADA, confirmar com valor/data/forma e bloquear duplicidade.
8. Entrar como MEDICO, conferir pagamento confirmado, notificação e histórico de agenda.
9. Gerar fatura SaaS mensal, marcar paga, contestar/cancelar com motivo quando aplicável e consultar inadimplência.
10. Criar chamado de suporte, responder/resolver com descrição, registrar contato de Customer Success e plano de ação.
11. Exportar relatório CSV permitido e verificar auditoria de exportação.
12. Abrir Swagger, validar /api/health, login JWT, endpoints Mobile e retorno 401 sem token.

## Resultado esperado
- Login, JWT, Cookie Authentication, Swagger e /api/health permanecem funcionais.
- Médico acessa somente dados próprios; usuário comum acessa somente dados do próprio cliente; admin global visualiza todos os clientes.
- Toda ação crítica registra auditoria, usa mensagem amigável e não expõe stack trace, SQL, token, senha ou segredo.
- Faturas SaaS seguem status ABERTA, PAGA, VENCIDA, CANCELADA e EM_CONTESTACAO sem duplicar competência da mesma assinatura.
- Suporte, Customer Success, dashboards, relatórios e API Mobile estão demonstráveis para homologação controlada.

## Critérios de aprovação
- Build da API e Web verde no ambiente com SDK .NET instalado.
- Varredura sem @page/asp-page em Views MVC, sem href="#", sem alert/confirm nativo e sem collection expression incompatível.
- Fluxo operacional médico ponta a ponta concluído sem exceção técnica.
- Fluxo SaaS básico concluído com bloqueios de plano/assinatura e auditoria.
- Testes mínimos compilam e contratos de segurança/mobile/SaaS passam.

## Pendências conhecidas
- Validar dados reais de SMTP/push antes de ativar notificações externas.
- Executar carga inicial e índices incrementais em homologação antes do teste com cliente real.
- Evoluir app mobile nativo a partir dos contratos Mobile documentados.

## Endpoints Mobile MVP
Base: `/api/mobile`.
- `POST /auth/login`
- `GET /me`
- `GET /dashboard`
- `GET /plantoes-disponiveis`
- `GET /plantoes/{id}`
- `POST /plantoes/{id}/solicitar`
- `GET /convites`
- `POST /convites/{id}/aceitar`
- `POST /convites/{id}/recusar`
- `GET /minhas-escalas`
- `GET /meus-pagamentos`
- `GET /notificacoes`
- `PUT /notificacoes/{id}/lida`
- `GET /perfil`
- `PUT /perfil`
- `GET /disponibilidade`
- `PUT /disponibilidade`
- `GET /preferencias`
- `PUT /preferencias`
- `GET /suporte/chamados`
- `POST /suporte/chamados`

### Como testar API mobile
1. Autenticar com `POST /api/mobile/auth/login`.
2. Copiar o JWT para Swagger/cliente HTTP com `Authorization: Bearer <token>`.
3. Validar payload leve, paginação e ausência de senha/hash/token em DTOs de listagem.
4. Chamar um endpoint sem token e confirmar 401 amigável.
5. Usar cliente com plano sem mobile e confirmar 403 amigável.
