# Rodada 74 — relatório final

**Entrega** (plano r66a0, bloco B2, item R74): guardas de design — contrato travando o mapa de
tokens + varredura anti-padrões.

## O que foi feito

1. **Mapa de tokens canônico travado**: 221 pares nome→valor (tokens.css 170 + v2150 21 + v2151 30)
   viraram literal permanente em `backend/PlantaoPro.Tests/R74GuardasDesignContratoTests.cs`
   (fato `Mapa_de_tokens_canonico_travado`). Mudar qualquer token exige atualizar o contrato e
   registrar a rodada aqui.
2. **Varredura anti-padrões permanente** (mesmos critérios no contrato e no `r74_scan.ps1`):
   - pares literais cor+fundo abaixo de AA em CSS não-vendor — **0 violações** (13 pares
     translúcidos/gradiente classificados `INFO`, fora de escopo declarado; 1 par em botão
     desabilitado com imagem de fundo, duplamente isento);
   - estilos inline de views com cor+fundo literais — **0 violações**;
   - cor de função sem texto/simbolo em views — **0 violações** (108 candidatos brutos triados;
     9 restantes classificados por exceções documentadas no plano; nenhum exigiu correção).
3. **Zero alterações em CSS ou views**: a árvore de estilo já estava dentro dos pisos; a rodada
   trava o estado atual contra regressão.

## Gates

| Gate | Resultado |
| --- | --- |
| build A/B Release `--no-incremental` | base 81 / nova 81 avisos, 0 erros, DIFF=NENHUMA (`r74_build_base.txt` / `r74_build_nova.txt`) |
| suíte completa Release (apps dev parados) | APROVADOS=1688 FALHADOS=0 TOTAL=1688 (`r74_tests_suites_completa.txt`) — inclui os 4 fatos novos |
| higiene da casa (pós-documentos) | 8/8 aprovados |
| docscan canônico (`r62_docscan.ps1`) | DOCSCAN_BAD=0 em 8 arquivos verificados (`r74_docscan.txt`) |

Nota de transparência: a primeira suíte completa pós-documentos produziu 2 falhas em
`Saude360R4B8FinanceiroCanonicidadeTests` por impasse 40P01 do PostgreSQL na limpeza de dados
própria dos testes (independente da mudança desta rodada, que só adiciona varreduras estáticas
de arquivos). Reexecução completa da suíte confirmou 1688/1688 aprovados, resultado registrado em
`r74_tests_suites_completa.txt`.

## Commits e push

- `(rodada 74 r74a)` — contrato de guardas de design (tests);
- `(rodada 74 r74b)` — evidência da rodada (docs);
- push canônico `git push origin main` concluído; `main == origin/main`.

## Status

R74 fechada. Próxima do plano: **R75 — inventário máquina páginas×artigos do manual**.
