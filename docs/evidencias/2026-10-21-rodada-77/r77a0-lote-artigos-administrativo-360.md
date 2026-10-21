# R77 - Lote de artigos: administrativo 360 (bloco B3)

## Escopo do plano

R77 do plano R66a0: "lotes - Administrativo 360 (cadastros, estoque, compras,
vendas, faturamento, financeiro, compras/recebimentos)". Este lote cobre as
telas principais de cada fluxo administrativo com artigos escritos sobre o que
a tela realmente faz hoje (recon controller + view arquivo a arquivo).

## O que mudou

`backend/PlantaoPro.Web/Controllers/AjudaController.cs`, no `CriarArtigos()`:

- 22 novos artigos no mesmo formato de linha dos existentes (o parser do
  script da R75 e o contrato R75 continuam validando o formato).
- Todos ficaram no topico existente `instituicao` (perfil
  `ADMINISTRADOR_CLIENTE`, ordem 60 a 270), estendendo os 5 artigos `adm-*`
  ja existentes. Sem topico novo nesta rodada: a familia ADMINISTRADOR /
  DIRETOR / ADMINISTRADOR_CLIENTE compartilha o manual "Operacao da
  instituicao" (D-R51-1 e D-R58-1 ja tratam a normalizacao de perfil).

### Tabela do lote

| Id | Pagina | Titulo |
|---|---|---|
| adm-panorama | Administrativo360/Index | Como acompanhar o panorama organizacional |
| adm-cadastros | Administrativo360/Cadastros | Como manter parceiros, produtos e locais de armazenamento |
| adm-produtos | Administrativo360/Produtos | Como gerir o catalogo de produtos e OPME |
| adm-locais | Administrativo360/Locais | Como configurar locais de estoque internos e de consignacao |
| adm-inventarios | Administrativo360/Inventarios | Como realizar a contagem fisica de inventario |
| adm-transferencias | Administrativo360/Movimentacoes | Como transferir estoque entre locais |
| adm-valorizacao | Administrativo360/Valorizacao | Como valorizar vales de consignacao |
| adm-vendas | Administrativo360/Vendas | Como consultar as vendas internas |
| adm-venda-detalhes | Administrativo360/VendaDetalhes | Como revisar e cancelar uma venda interna |
| adm-fechamento-caixa | Administrativo360/FechamentoCaixa | Como fechar e conferir o caixa |
| adm-fluxo-caixa | Administrativo360/FluxoCaixa | Como acompanhar fluxo de caixa e contas bancarias |
| adm-contas-pagar | Administrativo360/TitulosPagar | Como gerenciar as contas a pagar |
| adm-despesa-manual | Administrativo360/NovaDespesa | Como registrar uma despesa manual |
| fat-contas-clinicas | FaturamentoClinico/Index | Como acompanhar as contas clinicas a receber |
| fat-titulos | FaturamentoClinico/Titulos | Como acompanhar os boletos das contas clinicas |
| fat-repasses | FaturamentoClinico/RepassesMedicos | Como acompanhar os repasses medicos |
| fat-relatorios | ClinicaFinanceiro/Relatorios | Como acessar os relatorios do financeiro clinico |
| fiscal-sincronizacao | Adm360DocumentosXmlWeb/Sincronizacao | Como sincronizar o DF-e da Sefaz |
| fiscal-importacao-xml | Adm360DocumentosXmlWeb/Importar | Como importar notas fiscais em XML manualmente |
| fiscal-notas | Adm360FiscalWeb/Notas | Como preparar a pre-emissao de NF-e |
| fech-apuracao | Fechamentos/Index | Como acompanhar a apuracao de plantoes |
| fech-conferencia | Fechamentos/Conferencia | Como conferir e aprovar um fechamento |

Decisoes de conteudo registradas:

- `fat-titulos` e `fat-repasses` apontam para telas que ainda sao cartoes de
  finalizacao (placeholder com botao "Central O que fazer agora"); os passos
  dizem isso explicitamente em vez de prometer funcionalidade que a view nao
  renderiza ainda. Quando ficarem funcionais, os artigos devem ser reescritos.
- `fiscal-notas` descreve a pre-emissao como esta hoje (lista real com
  situacoes); a emissao autorizada depende do conector fiscal, que permanece
  fora do escopo deste artigo.
- `fech-apuracao` e `fech-conferencia` compartilham o mesmo componente de
  view (a rota de conferencia abre ja posicionada no detalhe); os artigos
  tratam cada uma das duas entradas separadamente.
- As telas do modulo sao funcionais de ponta a ponta conforme verificado no
  recon, excetuados os dois placeholders acima; por isso os passos citam
  nomes exatos de botoes ("Abrir Novo Inventario", "Validar e Importar XML",
  "Realizar Fechamento", "Nova Despesa" etc.).

## Numeros apurados

Rerun do `r75_gap.ps1` apos a mudanca (inventario da R75 atualizado no commit):

```
RESUMO|RETURNS_VIEW_BRUTOS=321|ACOES_EFETIVAS_RESOLVIDAS=674|FILE_OK=616|ARTIGOS=64|COBERTAS=63|FORA_ESCOPO=19|GAP_SEM_ARTIGO=534|ACO_AUX=29|POTE_QUEBRA=29
```

- Artigos: 42 -> 64. Cobertas: 41 -> **63** (nenhum artigo duplicado de pagina
  neste lote; a duplicacao historica em `Financeiro/Index` permanece).
- Gap: 556 -> **534**. O teto travado em `R75InventarioManualContratoTests`
  (`GapMaximoTravado`) foi atualizado de 556 para 534.

## Backlog restante para os proximos lotes

Administrativo/financeiro remanescente neste dominio (82 paginas):

- Administrativo360 (31): Cargos, Colaboradores, Contrato, Contratos,
  ContratoDetalhes, Cirurgias (+Nova/Detalhes), Coleta, ComissoesPendentes,
  ContasFinanceiras, Departamentos, EncerrarContrato, Inspecoes, Lotes,
  Ocorrencias, Orcamento* (Novo/Detalhes/Editar/Imprimir/Reserva), Parceiros,
  Recebimentos, Relatorios (agregada), TituloDetalhes, TituloPagarDetalhes,
  Vale* (Vales/Novo/Imprimir/Detalhes), ValorizacaoPrevia
- ClinicaFinanceiro (7): Caixa, ContasReceber, FechamentoCaixa, Glosas, Index,
  Receber, Repasses
- Financeiro (7): Confirmados, Contestacoes, Details, Exportacoes, Pagamentos,
  Pendentes, Relatorios
- FaturamentoClinico (4): Configuracoes, DemoBoleto, Glosas, Regras
- Adm360CotacoesWeb (5) · Adm360DocumentosXmlWeb (3) · Adm360FiscalWeb (3) ·
  Adm360GestaoWeb (2) · Fechamentos (Details, Pendentes) · ItensFaturaveis (4)
- Telas fiscais unitarias (8): Pagamentos, Recebimentos, Caixa, Glosas,
  LotesFaturamento, FaturamentoRegras, RepassesMedicos, GuiasConvenio
- Cadastro (6): fluxo de onboarding (Empresa, Plano, Usuario, Confirmar,
  Confirmacao, Sucesso) — candidato ao lote SaaS/Centrais da R78

Nucleo clinico remanescente da R76, agora em 45 paginas (os 3 do
FaturamentoClinico entraram neste lote). Soma clinico + administrativo: 127;
o restante dos 534 (407) fica distribuido entre SaaS/Centrais, Relatorios,
Bi, Portais e demais modulos — os lotes das rodadas seguintes consomem esse
backlog em ordem de dominio.

## Gates desta rodada

- Build A/B Release sem novos avisos e sem erros.
- Suite completa com apps parados (total anterior 1691; nenhum fato novo nesta
  rodada, apenas ajuste da constante no contrato R75).
- Docscan sobre os arquivos novos/alterados com delta zero.
- Higiene reexecutada apos este documento.
