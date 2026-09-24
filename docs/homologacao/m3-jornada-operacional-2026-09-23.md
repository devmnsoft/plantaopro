# M3 — jornada operacional (A11–A14)

Data da auditoria: 2026-09-23. Commit inicial: `abbb365`. Branch examinada: `work`.

## Decisão de homologação

**M3 não está homologado neste ambiente.** A imagem não contém o SDK .NET, cliente/servidor
PostgreSQL ou runtime de contêiner e o download do SDK foi recusado pelo endpoint externo (HTTP
403). Assim, não foi possível executar build, testes .NET, as consultas Dapper contra PostgreSQL,
a disputa com conexões independentes nem a navegação autenticada. Os gates estáticos disponíveis
passaram, mas não substituem essas evidências.

## Base reconfirmada

| Marco | Item | Estado | Evidência desta rodada |
|---|---|---|---|
| M1 | Build e testes .NET | Bloqueado | `dotnet` ausente; não inferido de documentação/commit. |
| M1 | Instalação limpa, upgrade e reaplicação | Bloqueado | PostgreSQL e runtime de contêiner ausentes. Scripts e manifesto foram apenas validados estaticamente. |
| M1 | Login PostgreSQL, sessão e logout | Não verificado | Exige API/Web e banco reais. |
| M1 | Inicialização API/Web | Bloqueado | SDK/runtime .NET ausente. |
| M2 | Separação de identidade e acesso | Parcial | `LoginIdentity` e contratos existem; não houve execução real. |
| M2 | Resolução explícita de tenant/cliente | Parcial | Os fluxos novos de fechamento usam contexto explícito; ainda há serviços operacionais legados com escopo implícito. |
| M2 | Autorização por perfil, módulo e registro | Parcial | Controllers possuem roles e consultas críticas de fechamento filtram tenant; autorização por registro não foi integralmente comprovada. |
| M2 | Comandos sem persistência fictícia | Parcial | Há respostas baseadas no resultado persistido nos fluxos auditados; falhas reais não foram injetadas nesta rodada. |
| M2 | Compatibilidade de rotas/consumidores | Não verificado | Requer testes .NET e navegação. |

O repositório começou limpo. O estado público do CI não pôde ser consultado: o endpoint do
repositório retornou autenticação obrigatória. Portanto, o estado do CI é **não verificado**, e não
“verde”.

## Inventário canônico e fluxo observado

- Plantão/capacidade: `Domain/Plantoes/PlantaoPolicies.cs`; consumidor principal `PlantaoService`
  em `Api/Data.cs`; contrato HTTP em `PlantoesController`.
- Escala/transições: `Domain/Escalas/EscalaStateMachine.cs`; persistência transacional em
  `EscalaService`; contrato em `EscalasController`.
- Convites: `plantao_convites`, `EscalaService.AceitarConviteAsync` e rotas profissional/mobile.
  O aceite bloqueia por médico (`pg_advisory_xact_lock`), bloqueia convite e plantão (`FOR UPDATE`),
  revalida vínculo, expiração, capacidade e conflito, e possui índices parciais de unicidade.
- Disponibilidade/conflitos: `MedicoElegibilidadeService`, `ConflitoHorarioService` e as consultas
  transacionais do aceite. Intervalos adjacentes são permitidos.
- Substituições: jornada separada em `Fase4OperationalServices.cs` e
  `Fase4OperationalController.cs`; o endpoint legado de troca imediata em `EscalasController`
  permanece como compatibilidade e merece retirada controlada após inventário dos consumidores.
- Execução/conferência: `ExecutionConferenceService` e `ExecutionConferenceController`, com versão,
  histórico de correção e conferência.
- Fechamento: `Domain/Financeiro/FechamentoWorkflow.cs`, `FechamentoOperacionalService` e
  `FechamentosController`.
- Remuneração: `RemunerationCalculator` é a regra explícita por modalidade. Nesta rodada o adaptador
  legado de base 12 horas passou a delegar a ele, eliminando uma segunda fórmula.
- Pagamentos: `FinanceiroService`, `PagamentosController`, origem única por escala e histórico de
  pagamento. Gerar obrigação mantém status financeiro distinto de registrar quitação.
- Notificações: `NotificacaoService`; nos fluxos transacionais auditados a gravação ocorre na mesma
  transação. Entrega externa não foi declarada como comprovada.

O encadeamento predominante é: DTO HTTP → role/contexto → serviço de caso de uso → regra de
domínio/validação → transação Dapper → histórico/auditoria/notificação → `ApiResponse` → recarga da
interface. Ainda há concentração histórica em `Api/Data.cs`; esta rodada evitou criar outro serviço
universal ou uma máquina de estados paralela.

## Alterações verificáveis desta rodada

1. A regra de remuneração passou a oferecer uma memória imutável com modalidade, valor-base,
   duração, quantidade de escalas, versão e resultado arredondado.
2. O calculador legado usado por prévia, fechamento e pagamento delega à modalidade
   `ValorBase12H`; divisão por 12 não foi aplicada às demais modalidades.
3. Elegibilidade temporal do convite, conflito de intervalos e pré-condições de substituição foram
   consolidados no domínio e o aceite transacional passou a consumir a política de convite.
4. Testes de domínio caracterizam as quatro modalidades, memória de cálculo, travessia da
   meia-noite com offset, expiração, intervalos adjacentes, substituição e capacidade.

### Decisões comerciais pendentes

- O esquema operacional legado só persiste `valor` e os consumidores históricos o interpretam como
  base de 12 horas. Não foi inventada migração automática para modalidade contratual. Antes de
  habilitar outras modalidades em produção é necessário definir origem contratual, reconciliação dos
  dados existentes e congelamento da regra em cada item aprovado.
- O endpoint legado de substituição imediata coexiste com a jornada moderna de solicitação/análise.
  Deve-se inventariar clientes e descontinuá-lo sem quebrar consumidores; até lá, a interface canônica
  deve usar a jornada separada.

## Interfaces encontradas

Foram encontradas telas separadas para central/detalhe do plantão, substituições, conferência,
fechamentos, pagamentos e agenda/pagamentos do médico. Nenhuma tela foi alterada nesta rodada porque
não foi possível iniciar API/Web e obter evidência antes/depois. Logo, responsividade, teclado,
feedback de falha e a navegação completa permanecem **não verificados**, não aprovados.

## Banco e SQL

Não houve migration nova. Os mecanismos existentes relevantes são checks de capacidade, unicidade
parcial de convite/ocupação/obrigação, locks de linha, advisory lock por médico, versão de execução e
origem única do financeiro. A integridade sintática dos scripts shell foi conferida; instalação,
upgrade, equivalência e materialização Dapper real ficaram bloqueados pela ausência do PostgreSQL.

## Matriz A14

| Testes | Estado | Observação |
|---|---|---|
| T02, T14, T15 | Parcial | Regras cobertas por testes .NET adicionados, mas runner indisponível. |
| T03–T08, T10–T13, T16–T18 | Não verificado | Proteções/fluxos existem no código e SQL; é obrigatório executar com PostgreSQL real e conexões independentes. |
| T01, T09 | Não verificado | Exigem jornada API persistida. |
| T19–T21 | Não verificado | Exigem identidades reais de médico, perfis e dois tenants. |
| T22 | Não verificado | Memória de domínio foi caracterizada; conciliação persistida/relatório exige banco. |
| T23 | Não verificado | Exige injeção de falha na API/banco e observação da interface. |
| T24 | Bloqueado | API/Web não puderam iniciar; sem evidência desktop/celular. |

Não há teste de concorrência declarado aprovado por inspeção textual. O aceite da última vaga,
conflito do mesmo médico e geração financeira precisam ser executados por duas conexões reais antes
da homologação.

## Status A11–A14 e condição para M4

- **A11 — parcial:** políticas centrais existem e as regras adicionadas ficaram no domínio; serviços
  legados ainda concentram responsabilidades e a dupla rota de substituição requer depreciação.
- **A12 — parcial:** interfaces por tarefa existem, mas não foram navegadas nem ajustadas sem runtime.
- **A13 — parcial:** execução → conferência → fechamento → obrigação → pagamento existe e mantém
  estados distintos; modalidades contratuais ainda precisam de persistência inequívoca.
- **A14 — bloqueado:** faltam banco real, concorrência, autorização por registro e navegação.

M4 pode iniciar apenas depois de restaurar o toolchain, executar o CI obrigatório, completar T01–T24
com evidências sanitizadas, resolver a modalidade contratual e eliminar qualquer falha crítica/alta
da jornada operacional. O módulo clínico não foi reescrito ou alterado nesta rodada.
