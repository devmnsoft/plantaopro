# R76 - Lote de artigos: nucleo clinico (bloco B3)

## Escopo do plano

R76 do plano R66a0: "lotes de artigos - nucleo clinico (triagem, agendamento,
consulta, longitudinal, receitas/exames)". Este lote cobre as telas principais
de cada fluxo com artigos escritos sobre o que a tela realmente faz hoje.

## O que mudou

`backend/PlantaoPro.Web/Controllers/AjudaController.cs`:

- Novo topico `clinico` (Perfil `TODOS`, icone `bi-clipboard-pulse`, titulo
  "Cuidado clinico") no `CriarTopicos()` — visivel para todos os perfis na
  pagina inicial da Ajuda e na busca.
- 17 novos artigos no `CriarArtigos()`, no mesmo formato de linha dos existentes
  (o parser do script da R75 e o contrato R75 permanecem validando esse formato).

### Tabela do lote

| Id | Pagina | Titulo |
|---|---|---|
| clin-triagem-fila | Triagem/Fila | Como acompanhar a fila de triagem |
| clin-triagem-nova | Triagem/Create | Como registrar a triagem do paciente |
| clin-triagem-finalizar | Triagem/Finalizar | Como finalizar a triagem e encaminhar à consulta |
| clin-agendamento-agenda | Agendamentos/Index | Como acompanhar a agenda clínica do dia |
| clin-agendamento-novo | Agendamentos/Create | Como criar um agendamento |
| clin-agendamento-cancelar | Agendamentos/Cancelamentos | Como cancelar um agendamento com trilha de auditoria |
| clin-agenda-operacional | Agenda/Index | Como visualizar a agenda operacional de plantões |
| clin-consulta-lista | Consultas/Index | Como acessar a lista de consultas |
| clin-consulta-atendimento | Consultas/Atendimento | Como conduzir o atendimento da consulta |
| clin-paciente-ficha | Pacientes/Details | Como consultar a ficha do paciente |
| clin-paciente-prontuario | Pacientes/Prontuario | Como consultar o prontuário longitudinal do paciente |
| clin-paciente-busca | Pacientes/Buscar | Como localizar um paciente |
| clin-prescricao-modulo | Prescricoes/Index | Como acessar o módulo de prescrições |
| clin-prescricao-editor | Prescricoes/Editor | Como abrir a prescrição de uma consulta |
| clin-prescricao-imprimir | Prescricoes/Imprimir | Como imprimir a prescrição com auditoria |
| medico-agenda | MinhaAgenda/Agenda | Como consultar sua agenda de plantões |
| medico-meuspagamentos | MinhaAgenda/MeusPagamentos | Como consultar seus demonstrativos de pagamento |

Decisões de conteúdo registradas:

- `medico-agenda` e `medico-meuspagamentos` ficaram no topico existente `medico`
  (area do medico), mantendo a convencao de 1 topico por area.
- Os 3 artigos de Prescricoes descrevem o fluxo real de entrada (botão
  "Abrir prescrição" no atendimento da consulta); as views do módulo ainda sao
  telas iniciais com cartao de contexto, entao os passos nao prometem campos ou
  botoes que a view nao renderiza ainda.
- `clin-triagem-finalizar` aponta para o par `Triagem/Finalizar` (acao POST do
  formulario com reabertura em caso de erro, que resolve view); o artigo descreve
  o gesto feito a partir do formulario ja salvo.

## Numeros apurados

Rerun do `r75_gap.ps1` apos a mudanca (inventario da R75 atualizado no commit):

```
RESUMO|RETURNS_VIEW_BRUTOS=321|ACOES_EFETIVAS_RESOLVIDAS=674|FILE_OK=616|ARTIGOS=42|COBERTAS=41|FORA_ESCOPO=19|GAP_SEM_ARTIGO=556|ACO_AUX=29|POTE_QUEBRA=29
```

- Artigos: 25 → 42 (os 2 artigos ja existentes apontam para a mesma pagina
  `Financeiro/Index`, por isso cobertas sobe 17 e nao 18).
- Gap: 573 → **556**. O teto travado em `R75InventarioManualContratoTests`
  (`GapMaximoTravado`) foi atualizado de 573 para 556.

## Backlog restante do nucleo clinico (para os proximos lotes)

Paginas `SEM_ARTIGO` remanescentes nos domínios clinicos, por dominio:

- Agendamentos (5): AgendaDia, AgendaMedico, Calendario, Details, Edit
- Cid (7): Create, Details, Edit, Favoritos, Importar, Index, MaisUsados
- Consultas (6): Create, Details, Edit, HistoricoPaciente, Imprimir, Resumo
- FaturamentoClinico (7): Configuracoes, DemoBoleto, Glosas, Index, Regras, RepassesMedicos, Titulos
- MinhaAgenda (3): Detalhe, Index, Presencas
- Pacientes (5): Edit, Historico, Index, Mesclar, ResumoClinico
- Prescricoes (5): Create, Details, Edit, HistoricoPaciente, Modelos
- Triagem (5): ClassificacaoRisco, Details, Edit, HistoricoPaciente, Index

Total remanescente do nucleo clinico: 48 paginas. Observacao para o planejamento
do bloco B3: o universo real medido (556 no gap geral) e maior do que a
aproximacao original do plano (~294); o contrato de zero gap da R79 deve ser
acionado quando a cobertura efetiva chegar ao fim da fila, e os lotes das
rodadas seguintes devem consumir este backlog em ordem de dominio.

## Gates desta rodada

- Build A/B Release sem novos avisos e sem erros.
- Suite completa com apps parados (total anterior 1691; nenhum fato novo nesta
  rodada, apenas ajuste de constante no contrato R75).
- Docscan sobre os arquivos novos/alterados com delta zero.
- Higiene reexecutada apos este documento.
