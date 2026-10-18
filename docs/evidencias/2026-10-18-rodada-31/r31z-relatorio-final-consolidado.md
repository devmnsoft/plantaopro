# R31 · r31z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R30 (`1f0e73b`, suíte 1548).

## Escopo da rodada

Epic **E5** da diretriz (r24a1), parte 2: validação dos formulários operacionais Adm360 — mesma régua
**D-R30-1** (teto = tamanho exato da coluna; piso para valores; pattern só para formato fechado; texto em
coluna `text` sem teto; campos de `<select>` sem teto). **Fecha o E5 no escopo Adm360.**

## Commits

| Commit | Conteúdo |
|---|---|
| `8c71363` | r31b1: 13 pontos em 11 views — Recebimentos (documento/lote 80), Coleta (código 100/lote 80), PedidosCompra (frete min 0), FluxoCaixa modal conta (120/40/20/30), ContasFinanceiras (edição inline + nova conta, 8 campos idem), TituloDetalhes (valorRecebido min 0.01), TituloPagarDetalhes (nº documento 80), NovaDespesa (documento 80/centroCusto 80), OrcamentoNovo+Editar (procedimento 180/vendedorId 36 = uuid) + residual E1 "ID do Produto"→"Código do produto" + `R31ValidacaoFormulariosOperacionaisContratoTests` (2 fatos) + r31a0 |

## Whitelist versionada (r31a0)

Colunas `text` auditadas colunas-a-coluna (movimentos.motivo, inspecoes.justificativa, ocorrencias.descricao,
ocorrencias.destino, vales.observacoes, titulo_baixas.referencia, orcamentos.observacoes) e campos controlados
por select (destino de inspeção, tipos de conta, condições). Filtros de listagem sem teto por design.

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Suíte completa: **Total 1550** (= base 1548 + 2 fatos), 2 falhas sob carga paralela, ambas **isolate 5× = 5/5 verde**:
  - `SaasGovernancaB6Rodada4Tests.G1b_PrefisGlobais_…` (40P01 — família já documentada em R24/R25/R28);
  - `R7OBOnboardingPreferenciasIdiomaTests.O1_Iniciar_MaterializaEtapaDeIdioma_OpcionalNoNucleo_Idempotente`
    (novidade na família rotativa sob carga — teste e2e de banco compartilhado).
- Rodada de carga: 4 falhas distintas de flake nas suítes R28–R31 (G1b ×2, R19Dapper ×2, G1b+R7O ×1) —
  a família rotativa pressiona cada vez mais sob a suíte cresida; consolidação em snapshot/CTE única segue
  como prioridade da fila E9 (R40+).

## Recon já iniciado para a R32 (E6 parte 1 — listas buscam por nome / salvam id)

Scan `%TEMP%\opencode\r32_scan_e6.ps1`:
- Listas Adm360 JÁ exibem dados legíveis (sem GUID em célula de tabela; busca por nome/código) ✓.
- Alvo real: **13 campos de ID livres** em 7 views onde o usuário digita UUID na mão:
  OrcamentoEditar (HospitalId/MedicoId/ResponsavelFinanceiroId/VendedorId/Itens[].ProdutoId ×2),
  OrcamentoNovo (VendedorId), ValeNovo (ReservaId ×2), Cotacoes/Detalhes (ProdutoId),
  Cotacoes/Relacionamento (EntidadeInternaId), DocumentosXml/Detalhes (PedidoId/LocalId).
- **Quick-win identificado**: `OrcamentoEditar` GET não chama `PreencherLookupsOrcamentoAsync`
  (Orcamentos.cs L93–129) enquanto o VM JÁ carrega Hospitais/Medicos/Pagadores/ProdutosDisponiveis no NOVO —
  1 linha de controller + trocar os 4 inputs text por selects espelhando o NOVO resolve 6 dos 13 campos.

## Estado de casa

- Apps PARADOS, portas livres. `origin/main` sincronizada neste push.
- Fila: R32 = E6 parte 1 (lookups nos 13 campos, começando pelo quick-win do OrcamentoEditar); depois
  R33 E6 parte 2 (números incrementais visuais), R34 E7, R35–R37 E3, R38–R39 E2, R40+ E9.
