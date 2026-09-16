# Portal profissional — agenda e template

## Estado inicial verificado

Commit inicial: `f133cd0` (`Merge pull request #488 from devmnsoft/codex/corrigir-erro-cs8936-e-refinar-relatorios`). A árvore estava limpa na branch `work`; nenhuma alteração anterior precisou ser preservada. Não foi encontrado `AGENTS.md` no repositório ou no diretório pai.

Já existiam login persistido e seleção de contexto, área `MinhaAgenda`, resumo `meu-dia`, oportunidades, disponibilidade, convites, check-in/check-out, correções com concorrência, contestação financeira e demonstrativos individuais. A API do portal já resolvia o profissional pelo usuário autenticado. A tela inicial, entretanto, mostrava somente cinco próximos plantões: não havia agenda profissional filtrável nem detalhe integrado. A confirmação canônica existia em `EscalaService`, mas não havia endpoint profissional que validasse titular/contexto antes de acioná-la.

O repositório fixa `LangVersion` em `10.0`. O SDK .NET não está instalado no ambiente (`dotnet: command not found`), portanto restore, compilação e execução dos testes não puderam ser comprovados nesta rodada.

## Implementação e reaproveitamento

- Agenda semanal, mensal (período selecionável) e lista cronológica acessível no controller e endpoint existentes da área profissional; filtros de período, unidade e situação são aplicados no servidor.
- Isolamento simultâneo por usuário→profissional e cliente atual em todas as consultas. Nenhum dado de clientes diferentes é agregado.
- Detalhe com resumo, participação, execução, financeiro e histórico. Execução reutiliza `ProfessionalPortalService.CheckInsAsync`; financeiro lê a apuração persistida sem recalcular valores na interface.
- Confirmação profissional revalida vínculo, tenant, cliente e titular e delega a transição, bloqueio de conflito, controle de vaga, histórico e notificação ao `EscalaService` canônico.
- Navegação de ida e volta preserva a URL completa de filtros. O Meu Dia aponta para a agenda e para o detalhe.
- Template da jornada usa fundo claro, estrutura azul-marinho, bordas discretas, foco visível, estados textuais, lista prioritária no mobile e redução de movimento.

## Matriz de regras e transições

| Entidade | Estado atual | Ação | Próximo estado | Perfil | Pré-condições | Efeitos persistidos | Teste |
|---|---|---|---|---|---|---|---|
| Escala | `solicitado` | Confirmar compromisso futuro | `confirmado` | Médico titular | vínculo ativo, mesmo cliente, escala ativa, plantão futuro, vaga, elegibilidade e ausência de conflito | escala, vaga, histórico, notificação e auditoria canônicos | `Confirmation_revalidates_ownership_and_uses_canonical_transition_service` |
| Presença | sem check-in / incompleta | Check-in/out | estado de conferência existente | Médico titular | escala confirmada/realizada, janela e timezone válidos | horários recebidos, versão e status de conferência | testes preexistentes de execução/conferência |
| Correção | sem solicitação ativa | Solicitar correção | `CORRECAO_PENDENTE` | Médico titular | presença própria, versão atual, intervalo válido | solicitação e versão; original preservado | testes preexistentes de execução/conferência |
| Pagamento | apurado/aprovado/pago | Consultar ou contestar | financeiro inalterado / contestação `ABERTA` | Médico titular | pagamento próprio e ausência de contestação ativa | somente contestação e auditoria | testes preexistentes de portal financeiro |

Convite, alocação, confirmação futura, execução, substituição, conferência e apuração continuam conceitos distintos. A solicitação de substituição não foi exposta no novo detalhe porque não foi identificado um backend profissional canônico completo para criação/cancelamento; não foi criado botão simulado. Também não foi criada política de reconfirmação: alterações posteriores são apresentadas conforme os dados atuais, sem invalidar confirmação silenciosamente.

## Correções e segurança

- O detalhe por ID responde `404` quando a escala não pertence ao profissional no cliente atual.
- A confirmação não confia no botão: o servidor repete todas as verificações e a transição canônica usa lock/transação e protege vaga/estado duplicado.
- A agenda limita consultas a 366 dias, usa parâmetros Dapper e inclui intervalos que cruzam a borda do período; plantões atravessando meia-noite exibem término com data.
- Datas ausentes são exibidas como não informadas, sem horário fictício. Registros cancelados permanecem consultáveis por filtro e não oferecem confirmação.
- Histórico mostra apenas confirmação comprovada pela tabela de histórico; datas genéricas não viram eventos inventados.

## Testes e resultados

- `dotnet restore backend/PlantaoPro.sln`: bloqueado, SDK `dotnet` ausente.
- Builds Debug/Release e `dotnet test`: bloqueados pela mesma limitação.
- PostgreSQL isolado e E2E com login persistido: bloqueados porque não há runtime .NET/banco de teste iniciado no ambiente.
- Screenshots em 360/768/1440: não produzidos, pois a aplicação não pôde ser iniciada sem SDK. As regras responsivas foram verificadas estaticamente.
- `git diff --check`: aprovado.

## Limitações explícitas

Sem execução não há declaração de funcionamento comprovado. Não houve alteração SQL. A seleção de múltiplos vínculos, login/hash, menus por perfil, páginas representativas de gestor/financeiro/super administrador e serviços de cobertura/disponibilidade foram preservados, mas não foram exercitados em navegador nesta rodada. A visualização “mês” usa a lista cronológica sobre o período mensal, pois o projeto não possui componente de calendário mensal adequado já integrado.
