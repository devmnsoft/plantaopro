# PlantãoPro — Fase 4: Automação operacional

## Implementado
- SQL incremental idempotente para disponibilidade médica, sugestões de escala, substituições, comunicação, notificações, pendências e relatórios.
- APIs autenticadas para médico configurar disponibilidade, indisponibilidade e preferências.
- APIs de coordenação para consultar médicos disponíveis, gerar sugestões determinísticas e convidar sugeridos.
- Fluxo de substituição com solicitação pelo médico, aprovação/recusa, convite de substituto, confirmação e histórico.
- Preferências de notificações, reprocessamento de fila e endpoints complementares de comunicação/templates.
- Pendências operacionais com criação, atribuição, resolução, adiamento, resumo e recálculo.
- Relatórios executivos operacional/financeiro/SaaS com CSV UTF-8 e registro de exportação.

## Validações executadas
O ambiente desta rodada não possui SDK `dotnet`; os comandos de build/test foram executados e falharam por limitação de ambiente. A validação automatizada adicionada cobre contratos estáticos de SQL, rotas e serviços.

## Pendências reais
- Aplicar a migration em banco de homologação e validar permissões por perfil com dados reais.
- Executar build e testes em ambiente com SDK .NET instalado.
- Evoluir a UI para consumir todos os endpoints via AJAX/telas ricas; nesta rodada foram expostas rotas MVC e páginas operacionais compatíveis com o padrão existente.

## Ajuste complementar de comunicação interna

- A API de comunicação agora grava mensagem inicial ao criar conversa, filtra conversas por busca/tipo/status e expõe marcação de mensagem como lida.
- O detalhe de conversa retorna o contrato consumido pela Web (`Mensagens`, `Participantes` e `MinhaMensagem`).
- A migration da fase 4 também consolida as tabelas base de conversas e leituras para ambientes que ainda não aplicaram o incremental histórico de comunicação.
