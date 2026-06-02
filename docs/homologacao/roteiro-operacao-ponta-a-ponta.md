# Roteiro de homologação — operação ponta a ponta

## Objetivo
Validar o fluxo crítico do PlantãoPro em ambiente controlado: cadastro, publicação, solicitação médica, confirmação de escala, realização, pagamento, notificação, auditoria, dashboard e Central de Escala.

## Pré-condições
- API, Web, Swagger e `/api/health` operacionais.
- Banco PostgreSQL com schema `plantaopro` migrado e dados seed de perfis.
- Usuários de teste para `ADMINISTRADOR_GLOBAL`, `COORDENACAO`, `MEDICO` e `FINANCEIRO`.
- Cliente, hospital, especialidade e médico podem ser criados sem dados reais sensíveis.

## Massa mínima
| Entidade | Dado esperado |
|---|---|
| Cliente | Cliente de homologação ativo |
| Hospital | Unidade ativa vinculada ao cliente |
| Especialidade | Especialidade ativa compatível com o médico |
| Médico | CRM/UF, e-mail e especialidade compatível |
| Plantão | Início futuro, fim posterior, valor >= 0 e vagas > 0 |

## Fluxo principal
1. Entrar como `ADMINISTRADOR_GLOBAL` ou `COORDENACAO`.
2. Criar hospital e confirmar listagem/detalhes.
3. Criar especialidade e confirmar status ativo.
4. Criar médico compatível com a especialidade.
5. Criar plantão; validar que nasce como `rascunho` e vagas disponíveis = vagas totais.
6. Publicar plantão; validar transição para `aberto`, toast, auditoria e histórico.
7. Entrar como `MEDICO`.
8. Abrir Plantões Disponíveis e solicitar plantão.
9. Validar bloqueio de duplicidade, conflito e especialidade incompatível.
10. Entrar como `COORDENACAO`.
11. Confirmar escala solicitada; validar redução de vaga somente neste momento.
12. Marcar escala como realizada.
13. Entrar como `FINANCEIRO`.
14. Gerar pagamento apenas para escala realizada.
15. Confirmar pagamento com valor, data e forma.
16. Entrar como `MEDICO`.
17. Validar pagamento confirmado em Meus Pagamentos.
18. Validar notificação gerada.
19. Validar auditoria das ações críticas.
20. Conferir Dashboard, Agenda e Central de Escala.

## Critérios de aceite
- Nenhuma etapa exibe stack trace ou SQL bruto.
- Todos os POSTs retornam `ApiResponse<T>` ou padrão equivalente.
- Ações críticas têm auditoria e mensagem amigável.
- Médico não acessa dados de outro médico.
- Vagas nunca ficam negativas.
- Pagamento não é duplicado para a mesma escala.
- Build final permanece verde.

## Evidências recomendadas
- Prints das telas de criação, publicação, solicitação, confirmação, pagamento e notificações.
- Logs sem payload sensível.
- Exportação CSV de relatório quando aplicável.
