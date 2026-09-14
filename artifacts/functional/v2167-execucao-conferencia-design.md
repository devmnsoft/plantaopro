# PlantãoPro v2.16.7 — execução, conferência e design

## Base confirmada

- Commit inicial preservado: `88a0536` (merge da v2.16.6); a árvore estava limpa.
- `V2160ClinicalJourneyContractTests` usa `RepositoryPathResolver.RepoRoot`.
- O repositório fixa C# 10 e `ModuleContractingService` não recebeu sintaxe posterior.
- Validado no código: login/JWT, contexto de cliente, perfis, contratos, escalas, convites e unicidade de alocação. Parcial antes desta rodada: `medico_checkins`, fechamento e origem financeira. Ausentes antes desta rodada: proposta de correção versionada e decisão com antes/depois.

## Estados e regras implementados

| Estado | Ator/transição | Pré-condição | Efeito/correção |
|---|---|---|---|
| Confirmado | profissional registra entrada | alocação ativa, própria e mesmo cliente | instante recebido no servidor; declaração manual separada |
| Registro incompleto | profissional registra saída | entrada existente, saída ausente | saída do servidor; passa a pendente |
| Pendente | profissional solicita correção | versão atual e justificativa | original preservado; proposta separada |
| Correção pendente | gestor decide | contexto do cliente, versão atual e autoria distinta | responsável, data, justificativa, antes/depois no histórico |
| Aprovada | fluxo canônico de fechamento/apuração | decisão autorizada | valores aprovados ficam disponíveis; não representa pagamento |
| Ajuste pós-apuração | gestor aprova correção tardia | pagamento/apuração consolidado | consolidado não é reescrito; ajuste permanece explícito |

Não foram inventadas tolerâncias, intervalos, descontos ou adicionais. Ausência de marcação permanece pendência, não falta/fraude. `timestamptz`, o offset recebido e o identificador do fuso preservam plantões atravessando meia-noite.

## Implementação

- Migração incremental `400`: instantes recebidos/declarados/aprovados, status, versão, correções e histórico.
- Registro transacional com lock da alocação, relógio do PostgreSQL, restrição única e resposta 409 idempotente.
- API de conferência filtra período, unidade, profissional e situação; indicadores usam o conjunto filtrado inteiro.
- Aprovação revalida tenant/cliente, autoria, status e versão dentro da transação.
- A tela profissional compara previsto, registrado, proposto e aprovado, com estados de erro/vazio, ajuda, foco e layout mobile.

## Testes e evidências

- Contratos v2.16.7 cobrem transação/unicidade, concorrência otimista, autoaprovação, ajuste pós-apuração e rótulos acessíveis.
- O gerador oficial produziu SQL consolidado e artefatos de dependência sem conflitos.
- `dotnet` não está instalado no agente; builds e testes .NET/E2E com PostgreSQL descartável não puderam ser executados localmente.

## Screenshots e limitações

Não foi possível produzir screenshot real: sem SDK não há aplicação executável para autenticar e navegar. Nenhuma imagem simulada foi usada. O E2E completo e a instalação/upgrade PostgreSQL devem ser executados no CI com dados sintéticos. A rodada não cria transferência, cobrança SaaS, regra remuneratória, biometria ou rastreamento.
