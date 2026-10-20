# Rodada 74 — guardas de design: contrato travando o mapa de tokens + varredura anti-padrões

Item do plano (r66a0, bloco B2, R74): "guardas de design: contrato travando o mapa de tokens +
varredura anti-padrões (cores de função sem texto, contraste < AA conhecido)".

## Decisões de escopo

1. **Mapa canônico travado**: o "mapa de tokens" da casa são as custom properties declaradas em
   `:root` nas três camadas canônicas da cascata (ordem documentada em
   `WcagContrasteAaContratoTests`):
   - `design-system/tokens.css` — 170 tokens;
   - `design-system/v2150-bloco-c-canonical.css` — 21 tokens;
   - `design-system/v2151-wp-c-ajuste-canonical.css` — 30 tokens.

   Total: **221 pares nome→valor**, todos literais no contrato
   (`backend/PlantaoPro.Tests/R74GuardasDesignContratoTests.cs`, fato 1). Toda mudança de nome ou
   valor exige atualizar esse contrato e registrar aqui a rodada correspondente.
2. **Fora do travamento** (documentado): vendor Bootstrap e `bootstrap-compat.css` (camada de
   terceiros; os valores efetivos `--bs-*-rgb` já são guardados pelo contrato de contraste da R69),
   tokens de tenant (`--tenant-*` em `white-label.css`) e namespaces locais de módulos
   (`s360-*`, `--fc-*` etc.), que permanecem cobertos indiretamente pelos contratos de contraste.
3. **Contraste conhecido — fora de escopo declarado do scanner**: pares com fundo translúcido
   (alpha < 1 em `rgba`/hex 8) ou em gradiente, porque a composição efetiva depende da superfície
   por baixo, e valores indiretos via `var()` fora das camadas canônicas (já cobertos em nível de
   token pela R69). Esses pares aparecem na saída como classe `INFO`, não como violação.
4. **Pisos de contraste**: 4,5:1 para texto; 3:1 para indicador não-texto (seletor de ícone:
   contém "icon" ou `>i`); componente desabilitado (`:disabled`/`.disabled`) isento, conforme a
   exclusão de interface inativa do critério 1.4.3 do WCAG.
5. **Cor de função sem texto**: elemento com utilitário de cor funcional
   (`bg|text|border`-{success,danger,warning,info} ou `text-bg`-{...}) que não carrega texto nem
   símbolo é candidato. Exceções documentadas (mesmas regras no scanner e no contrato):
   - atributos `asp-validation-for`/`asp-validation-summary`: a mensagem de erro chega junto com a cor;
   - conteúdo direto não-vazio (ex.: `*` de obrigatório): símbolo carrega a informação, não a cor;
   - ícone como primeiro filho com texto logo após o `</i>`; banner rotulado (`role=`/`alert`)
     com texto no primeiro container filho;
   - `bg-opacity-25` nos classes do elemento: chip decorativo de ícone (precedente R71);
   - `fs-N` nos classes do elemento: glifo decorativo dimensionado, rótulo vive fora do elemento.

## Reconhecimento e varredura

Scanner `r74_scan.ps1` (mesmos critérios do contrato), saída em `r74_gap_scan.txt`:

- Passada 1 — mapa de tokens: 221 pares extraídos dos três arquivos canônicos.
- Passada 2 — pares literais cor+fundo no CSS não-vendor: **18 acertos brutos** antes das regras de
  piso/classificação; após classificar, **0 violações estritas**, 13 pares `INFO` (translúcido ou
  gradiente) e 1 par com imagem de fundo em botão desabilitado (duplamente isento). Nenhum par
  exigiu correção: os 7 casos abaixo de 4,5 eram seletores de ícone passando no piso de 3:1
  (ex.: `.kpi-success .kpi-icon` 3,94:1; `.risk-panel__icon--waiting` 4,42:1) e 1 botão
  `:disabled` (1,74:1, isento).
- Passada 3 — estilos inline das views com cor+fundo literais: **0 violações**.
- Passada 4 — cor de função sem texto: **108 candidatos brutos**; aplicadas as exceções
  documentadas acima, restaram **9**, todos classificados e cobertos por regra (nada a corrigir
  no markup):

  | Candidato | Classificação |
  | --- | --- |
  | `Administrativo360/Index.cshtml` L73, L84, L95 (chips de KPI) | `bg-opacity-25`: chip decorativo de ícone em cartão KPI rotulado (regra chip decorativo, precedente R71) |
  | `Administrativo360/Inspecoes.cshtml` L48, `Inventarios.cshtml` L28, `Recebimentos.cshtml` L28 (banners `role="alert"`) | ícone + container rotulado com texto imediato (regra banner) |
  | `Lgpd/MinhaPrivacidade.cshtml` L21, L31, L41 (glifos `fs-3` de seção) | glifo decorativo dimensionado em cartão com `h2`+parágrafo (regra glifo) |

Os ~99 demais candidatos eram dois padrões legítimos em massa: spans vazios de validação do ASP
(`asp-validation-for`) que carregam o texto do erro quando ativo, e `*` de obrigatório dentro de
label (símbolo, não cor).

## Mudança

- Novo contrato: `backend/PlantaoPro.Tests/R74GuardasDesignContratoTests.cs` com 4 fatos:
  1. `Mapa_de_tokens_canonico_travado` — 221 pares literais por arquivo (contagem + nome + valor).
  2. `Css_fora_dos_tokens_sem_par_literal_cor_fundo_abaixo_da_Aa` — varredura permanente de pares
     literais em todo CSS não-vendor, com pisos e isenções do item 3.
  3. `Views_estilos_inline_cor_fundo_conhecidos_AtenAa` — varredura permanente de estilos inline.
  4. `Views_sem_cor_de_funcao_sem_texto_ou_simbolo` — varredura permanente anti "cor como único canal".
- **Zero alterações em CSS ou views**: a varredura não encontrou violação efetiva a corrigir nesta
  rodada; o valor da rodada é a trava permanente (regressão futura falha a suíte com o arquivo,
  linha e a razão medida).

## Lição nova de casa

Contratos C# com tabela grande de tuplas: array de tuplas (structs) não aceita inicializador por
chaves simples — precisa de `new (T1, T2, ...)[]` e cada elemento entre parênteses (CS0623).
Padrão para rodadas futuras que travarem mapas dados em literal.

## Gates (preenchidos no relatório final)

- build A/B Release `--no-incremental` (esperado: base 81 / nova 81 avisos, 0 erros, diferença nula);
- suíte completa em Release com apps dev parados (esperado: 1684 + 4 novos = 1688 aprovados, 0 falhas,
  higiene da casa 8/8 incluindo reexecução pós-documentos);
- docscan canônico (`r62_docscan.ps1`) sobre `git diff HEAD` + untracked (esperado: 0 achados);
- commits ASCII `(rodada 74 ...)` + push canônico `git push origin main`.
