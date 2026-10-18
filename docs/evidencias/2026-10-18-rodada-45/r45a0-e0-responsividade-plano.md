# R45 · r45a0 — E0 design: responsividade (mobile/campo)

Data: 2026-10-18 · Sessão: `opencode` · Base: R44 (`a5e49c5`, suíte 1583).

## Recon

| Verificação | Resultado |
|---|---|
| Viewport no layout raiz | Já presente (`width=device-width, initial-scale=1.0`) |
| Tabelas de dados em `.table-responsive` | 114+ já corretas em todo o Web |
| Grid Bootstrap nas novas telas de detalhe (R44) | `container` + `row/col-sm-*` — empilham no mobile |

Gap real encontrado: **três telas institucionais com tabela larga e sem `.table-responsive`** —
no celular a tabela estoura a largura da tela:

| Tela | Uso no campo | Correção |
|---|---|---|
| `ConvitesEquipe\Index` | gestor convita equipe do celular | wrapper |
| `MinhaAssinatura\Faturas` | cliente confere faturas/vencimentos (tema E9) | wrapper |
| `Usuario\Admin` | gestão de usuários da conta | wrapper |

Keeps declarados: `Administrativo360\OrcamentoImprimir` e `ValeImprimir` são telas de
impressão A4 paisagem — largura fixa é o comportamento esperado e fica fora do scanner.

## Decisão

- **D-R45-1**: contrato permanente "nenhuma view tem tabela fora de `.table-responsive`" com os
  dois keeps de impressão nomeados na classe. Comentários HTML/Razor que citam `<table>`
  (nota de parsing em `DocumentosXml/Triagens`) não contam como tabela.
- O restante da responsividade (dashboards MeuDia/CommandCenter, grids existentes) foi verificado
  na varredura e usa grid/flex do Bootstrap — sem estouro identificado nesta passada. Revisão
  visual detalhada em dispositivo segue para a passada crítica R46.

## Gates e suíte

- Rebuild real → multiset 160/160 idêntico ao pristino, 0 erros.
- Classes: R45 + R44 (estados) + R39 (tenant em views) + higiene.
- Suíte integral base **1585** = 1583 + 2 fatos R45.
