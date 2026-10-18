# R42 · r42a0 — Consolidação dos scanner-fatos permanentes (E4/E5/E6/E8 em um suíte dedicado)

Data: 2026-10-18 · Sessão: `opencode` · Base: R41 (`b9755d2`, suíte 1572).

## O problema que motivou o roadmap

Cada rodada r2x criou sua classe de contrato-scanner com o prefixo da rodada (R25, R26, R30…R33).
As regras já são **permanentes** — não mudam mais com a rodada de origem — mas estão espalhadas em
classes que carregam o nome histórico. Consequências práticas:

1. Na suíte integral, os scans pesados de arquivo (views do repo inteiro) rodam **em paralelo por
   classes diferentes**, disputando disco/CPU exatamente sob carga — o mesmo ambiente em que as
   famílias flake conhecidas (`ProvisionamentoB5Tests`, `R7OBOnboardingPreferenciasIdiomaTests`)
   falham rotativamente. Menos paralelismo entre scans de arquivo = menos pressão nesse ambiente.
2. Não há um ponto de entrada para "rodar toda a regressão de UX/texto permanente" sem decorar filtros.

## O que a fatia b1 entrega

`R42ScannersPermanentesContratoTests` — **um suíte dedicado** com 5 fatos, um por regra:

| Fato | Regra consolidada | Fonte canônica (intacta) |
|---|---|---|
| `E4_ContrasteZonasEscuras` | sem texto muted sobre zona escura + controles positivo/negativo do scanner | `R26ContrasteZonasEscurasContratoTests` |
| `E5_FormulariosCadastros_TetoEPatternDoFormato` | tetos de coluna + pattern de formato nos cadastros | `R30ValidacaoFormulariosCadastrosContratoTests` |
| `E5_FormulariosOperacionais_TetosEPisos` | tetos e pisos operacionais (estoque, financeiro, orçamento) | `R31ValidacaoFormulariosOperacionaisContratoTests` |
| `E6_LookupsSemCampoDeIdLivre` | nome-vs-id: lookups carregados e selects no lugar de input livre | `R32LookupsCampoDeIdContratoTests` + `R33E6LookupsReservaEPedidoContratoTests` |
| `E8_TextosSemHomologacao` | banners/rotas/literais de demo + whitelist fiscal SEFAZ | `R25TextosSemHomologacaoContratoTests` |

**Zero cópia de lógica**: o agregador instancia as classes originais e invoca seus métodos públicos.
Se uma regra mudar, muda em um único lugar (a classe canônica); o suíte dedicado acompanha sozinho.
Execução dedicada:

    dotnet test --filter "FullyQualifiedName~R42ScannersPermanentes"

## Decisões

- **D-R42-1**: as classes originais **não foram removidas nem modificadas** nesta fatia. Elas seguem
  valendo como contrato individual por regra (granularidade de diagnóstico), e o suíte dedicado é o
  ponto de execução agregado. Duplo dono do código: nenhum — só um dono de cada regra.
- **D-R42-2**: o escopo do roadmap é literalmente "regras E4/E5/E6/E8". Os scanners de E1/E2/E3
  (R29/R34/R38/R39 e os lotes de guias R35–R37) têm outra natureza (frases exatas por tela, não
  regras de formulário/contraste) e entram na fatia b2 se a passagem crítica (R46) confirmar que o
  agrupamento agrega valor operacional.
- **Famílias flake sob carga** (`ProvisionamentoB5Tests.P2/P3`, `R7OBOnboardingPreferenciasIdiomaTests`):
  seguem o protocolo imutável (isolate 5× antes de atribuir à rodada; candidata a consolidação em
  snapshot único quando a carga diminuir). Esta rodada ataca a causa indireta (paralelismo de scans)
  e não altera nenhuma dessas classes.

## Gates e suíte

- Rebuild real → multiset 160/160 idêntico ao pristino, 0 erros.
- Classes: R42 + as 5 canônicas + higiene.
- Suíte integral base **1577** = 1572 + 5 fatos R42.
