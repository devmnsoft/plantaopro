# R35 · r35a0 — E3 instruções de uso por página: inventário e plano em 3 rodadas

Data: 2026-10-18 · Sessão: `opencode` · Base: R34 (`90ec664`, suíte 1554+2).

## Inventário (scan `%TEMP%\opencode\r35_scan_instrucoes.txt`)

64 views Adm360 varridas por marcadores de instrução (Guia/alert-info/como-usar/help):

- **32 têm** algum bloco de orientação (guia completo, alert-info ou seção help).
- **Zonas fora do escopo**: portal Saúde 360 já cobre com os blocos FormHelp/Help (família R7,
  chaves Clinical.* reescritas na R34); CommercialDemoWeb/B2BLaunch é marketing; AdminSaas é cockpit
  de operador MNSOFT (audiência interna, D-R24-3).
- **Alvos Adm360 sem instrução: 29 páginas** (menos as exclusões abaixo).
- **Exclusões justificadas**: `OrcamentoImprimir.cshtml` e `ValeImprimir.cshtml` (layout de impressão
  — sem interação, instrução não agrega), `_AlertasAcao.cshtml` (parcial reutilizável, não é página).

## Padrão da casa (extrado de `Cirurgias.cshtml` L21–41, rodadas anteriores)

Card "Guia da tela" logo após o cabeçalho da página, antes do conteúdo:

```
<!-- Guia da tela -->
<div class="card bg-dark border-secondary mb-4 text-light">
    <div class="card-body">
        <h5 class="card-title text-info h6 mb-2"><i class="bi bi-info-circle me-2"></i>{título simples}</h5>
        <div class="row g-3 small text-secondary">
            <div class="col-md-3"><strong class="text-light d-block mb-1">1. Para que serve</strong> …</div>
            <div class="col-md-3"><strong class="text-light d-block mb-1">2. Como utilizar</strong> …</div>
            <div class="col-md-3"><strong class="text-light d-block mb-1">3. Efeito da confirmação</strong> …</div>
            <div class="col-md-3"><strong class="text-light d-block mb-1">4. Próxima ação</strong> …</div>
        </div>
    </div>
</div>
```

Regras de redação: português simples do usuário (E1 já aplicado — nada de jargão novo), títulos das
4 seções invariáveis, conteúdo específico da operação da página (o que a ação muda no fluxo, para quem
serve o próximo passo). Literais pt-BR crus, mesmo estilo local das telas de Suprimentos (não resx).

## Divisão em 3 rodadas (R35–R37)

| Rodada | Lote | Páginas (7 / 11 / 8) |
|---|---|---|
| **R35** | Fluxo cirúrgico central + compras/estoque | `OrcamentoNovo`, `OrcamentoEditar`, `OrcamentoDetalhes`, `PedidosCompra`, `Estoque`, `Coleta`, `ValeDetalhes` |
| R36 | Qualidade/fiscal/XML/cotações | `Inspecoes`, `Movimentacoes`, `TituloPagarDetalhes`, `Fiscal\Configurar`, `Fiscal\Detalhes`, `Fiscal\Notas`, `Fiscal\Nova`, `DocumentosXml\Importar`, `DocumentosXml\Sincronizacao`, `Cotacoes\ContasPortal`, `Cotacoes\Estabelecimentos`, `Cotacoes\Relacionamento` |
| R37 | Cadastros organizacionais + gestão | `Cadastros`, `Cargos`, `Colaboradores`, `Contratos`, `Departamentos`, `Index`, `Gestao\Dashboard`, `Gestao\Monitor` |

Fato por rodada trava o lote da própria rodada: cada view deve conter o comentário `<!-- Guia da tela -->`
e as quatro seções (`1. Para que serve`, `2. Como utilizar`, `3.` e `4. Próxima ação`).

## Plano de fatias R35

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r35b1 | 7 guias do fluxo cirúrgico/compras | 1 fato (7 asserts) |
| r35z | Consolidado + suíte + push | — |

Antes de redigir cada guia, ler a ação correspondente (controller) para descrever com precisão o
efeito real da operação (mesmo critério E1/E6: texto não pode prometer o que a tela não faz).
