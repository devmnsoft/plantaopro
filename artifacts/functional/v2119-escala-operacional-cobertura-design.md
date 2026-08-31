# v2.11.9 — Escala operacional, cobertura e design

## Modo e SDK

- **Modo usado:** MODO ESTÁTICO SEGURO.
- `dotnet` não está instalado no ambiente (`command not found`); por isso nenhum arquivo C#, contrato, projeto, banco ou migration foi alterado.
- A implementação foi deliberadamente limitada a Razor, CSS, JavaScript e esta documentação.

## Jornada revisada

A estrutura existente já conecta solicitação da unidade, plantão, convite, escala, confirmação/recusa, presença ou substituição, fechamento e financeiro. Também já há telas e ações para publicação, cancelamento com justificativa, confirmação, recusa motivada e substituição com seleção de profissional. Não foi criado fluxo paralelo.

As lacunas reais observadas são: calendário ainda sem fonte integrada; listagem não expõe conflito, check-in/check-out ou histórico de substituição em seus DTOs; e ações operacionais completas dependem das APIs. A interface identifica essas ausências sem inventar dados.

## Regras e bugs

As regras obrigatórias de conflito, disponibilidade, especialidade, situação cadastral, tenant, check-in/out, no-show, cancelamento, substituição e pagamento exigem validação de backend. Elas **não foram alteradas nem simuladas** neste modo sem SDK. Permanecem preservadas as ações existentes e seus avisos de validação no servidor.

Foi corrigida a jornada visual incompleta: a central agora alterna entre lista e kanban usando os mesmos registros reais, agrupa cobertura crítica, parcial e concluída e apresenta erro recuperável. O calendário deixou de ser uma tela crua de demonstração e passou a oferecer um estado indisponível claro, com rotas válidas para a operação.

## Telas e design

- `Plantoes/Index`: seletor acessível de lista/kanban/calendário, kanban por cobertura, progresso, contadores, feedback de erro e anúncios em região viva.
- `Plantoes/Calendario`: estado de integração vazio, legenda operacional e chamadas para lista ou criação.
- Layout responsivo para colunas, controles e ações; visual médico sóbrio em verde clínico, superfícies leves e sinais sem depender apenas de cor.
- Nenhuma tela adicionada solicita IDs, usa dados fake, `href="#"`, `alert()` ou `confirm()`.

## Testes e validações

Foram executadas as verificações estáticas solicitadas, incluindo `git diff --check`, varredura de padrões inseguros e scripts de segurança/compatibilidade disponíveis. Restore, builds e testes .NET não puderam ser executados porque o SDK não existe no container.

## Limitações restantes

- Conectar o calendário a dados reais requer evolução do controller/API e testes com .NET disponível.
- Exibir disponibilidade, conflitos, check-in, check-out, ocorrências, no-show e histórico de substituição requer DTOs/endpoints reais; nada disso foi inferido no cliente.
- As regras transacionais e o isolamento por tenant precisam de uma rodada em MODO COMPLETO para validação automatizada.
