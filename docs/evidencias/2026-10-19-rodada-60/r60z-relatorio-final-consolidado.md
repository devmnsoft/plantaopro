# R60 — Relatório final consolidado — Inventário completo de GUIDs/fragmentos remanescentes em UI

**Data:** 2026-10-19 · **Fila:** r53b1 §7 **item 6** — "Inventário completo de GUIDs/fragmentos remanescentes em UI (R33/R50, E6)".
**Plano:** `r60a0-guids-fragmentos-plano.md` (mesma pasta). **Status:** FECHADA.

## O que foi entregue

### D-R60-1 — fragmento visível some: o identificador aparece inteiro ou não aparece
| Local | Antes | Depois |
|---|---|---|
| `AssistenteIa/Index.cshtml` (coluna "Cliente", custos IA) | `TenantId.ToString().Substring(0, 8)…` + title c/ inteiro | identificador completo em fonte monoespaçada, title descritivo |
| `Administrativo360/Fiscal/Notas.cshtml` (coluna "Origem") | badge tipo + `origemId.ToString()[..8]…` + title c/ inteiro | badge + identificador completo monoespaçado, title descritivo |
| `MinhaAgenda/MeusPagamentos.cshtml` (demonstrativo profissional) | `FechamentoId.Value.ToString("N")[..8]` — **o exemplo exato citado na diretriz R53**, sem inteiro acessível | guid completo `ToString()` (quebra natural nos hífens), sem truncamento |

Sem mudança de backend: o nome do cliente/tenant não existe no viewmodel da linha de uso; onde a referência é
operacional, o inteiro é a forma honesta (direção E6 proibia o fragmento como conteúdo).

### D-R60-1b — chave longa/digest: abreviação só com o inteiro a um hover de distância
SHA256 (64 hex) e Chave de Acesso NFe (44 dígitos) não cabem legíveis em linha de lista. Padrão aceito — já
existente antes desta rodada — é abreviar no visível e expor **o valor inteiro no `title` do mesmo elemento**:
- **corrigido:** digest de anexo em `Cotacoes/Detalhes.cshtml` aparecia como `SHA256: <hash[..8]>...` sem o inteiro
  em lugar nenhum → ganhou `<span title="@anexo.Sha256Hash">`;
- **já conformes (whitelist):** exportação (`ExportacaoSha256Hash[..8]…` c/ title) e Triagens
  (`ChaveAcesso[..10]…[^6..]` c/ title);
- **máscara de documento** (`CpfBr` em `Colaboradores.cshtml`: `[..3]`, `[9..11]`) é formatação, não identificador —
  linha isentada pelo contrato.

### D-R60-2 — placeholder de exemplo sem dado real
`Cotacoes/Detalhes.cshtml` campo "ou identificador manual": o placeholder era
`Ex: d3f6584c-2c64-4e5a-9ea9-4e1428647520` — o **tenant demo vazando como exemplo na UI** (única ocorrência fora dos
seeds). Passou ao padrão neutro de formato `00000000-0000-0000-0000-000000000000`, consistente com os outros campos
operacionais (Fiscal/Nova "Código do documento de origem" e Cotacoes/Relacionamento "Código interno (opcional)", ambos
já com zeros). Rótulo resx ("ou identificador manual") e ajuda permanecem — já limpos de jargão pela R28/E6.

### D-R60-3 — whitelist dos campos operacionais etiquetados (travada por contrato)
- `Fiscal/Nova.cshtml` — "Código do documento de origem" (`name="OrigemId"`, placeholder zeros);
- `Cotacoes/Detalhes.cshtml` — resx `Adm360.CotDet.LblIdentificadorManual` + placeholder zeros;
- `CentralEscala/Substituicoes.cshtml` — filtro "Unidade (identificador)" (busca por nome vizinha existe);
- `ConferenciaExecucao/Index.cshtml` — filtros "Unidade (ID)" / "Profissional (ID)".

## Contrato novo — `R60GuidsEFragmentosContratoTests` (3 fatos, scanner de fonte)

1. `Views_SemFragmentosTruncadosDeIdentificador` — regra A: zero `Substring(0, 1..19)` em Views; regra B: zero fatias
   `[..1..19]` sem valor inteiro em `title="` na mesma linha (exceto a linha de máscara CPF, isentada).
   O fato é mais largo que a varredura inicial e foi ele que capturou as ocorrências extras (digest de anexo,
   `FechamentoId[..8]`) — registrado como incidente de processo abaixo.
2. `Views_SemLiteralGuidComDadosDeExemplo` — todo literal em formato GUID nos cshtml deve ser o placeholder neutro
   de zeros; nenhum guid com dígito não-zero (o tenant demo só pode existir nos seeds).
3. `CamposOperacionaisDeIdentificador_RotulosTravados` — os 4 campos/filtros #D-R60-3 presentes com rótulo/placeholder
   travados (rótulo sumiu ou placeholder com dado real voltou, contrato falha).

## Inventário — resumo da classificação completa (tabela inteira no plano r60a0)

- **Fragmentos visíveis corrigidos:** 3 (AssistenteIa, Notas fiscais, MeusPagamentos) + 1 digest sem inteiro (anexo).
- **Whitelist com justificativa:** 5 (digest/exportação, chave NFe/triagens, máscara CPF, 4 campos/filtros etiquetados).
- **Estrutural/máquina (sem ação):** 117 atributos em Views (rotas, hidden, `data-*`, option values) + arrays JS inline
  + `idempotencyKey=@Guid.NewGuid()`.
- **Código interno (não é UI):** `SecurityAdministrationServices.cs` gera `CUSTOM_<guid12>` como `codigo` de perfil
  copiado (coluna máquina). Truncamentos em `PlantaoPro.Tests` são fixtures de máquina — fora de escopo.
- **JS client-side:** nenhum fragmento de GUID em `textContent` (apenas máscaras CPF/CNPJ/fone/CRMUF e fatias de data ISO).

## Gates executados

| Gate | Resultado |
|---|---|
| Rebuild `dotnet build backend\PlantaoPro.sln -c Release --no-incremental` | exit 0, 0 erros |
| `r20b1_gate.ps1` (log `r20b1_build.log`) | `Now=160 Base=160 · DIFF=NENHUMA (multiset identico chave-a-chave)` |
| Dirigidos R60×3 + R59×5 (regressão do contrato a11y que toca as mesmas views) | **8/8** |
| Suíte integral | **1636/1636 aprovados** (~33 s) — base 1633 + 3 novos |
| Smoke canônica R52 (`r52_smoke_e2e.ps1`) | `FALHAS=0 · R52_E2E_OK` |
| Mini-e2e R60 (`r60_guids_e2e.ps1`, BOM UTF-8) | **FALHAS=0** — login demo OK; `/AssistenteIa` e `/Administrativo360/Fiscal/Notas` renderizados sem fragmento 8-hex+elipse nem artifact `Substring(`; `/MinhaAgenda/MeusPagamentos` fora do perfil demo (AccessDenied — zona MEDICO), branch registrado |
| Docscan (evidências da rodada) | TODO/FIXME=0 · `plantaouro`=0 |

### Honestidade das evidências
O demo não possui usos incertos IA, notas fiscais com origem nem demonstrativo profissional no banco de teste, então as
três linhas corrigidas não renderizaram dados via HTTP nesta execução — a garantia determinística dessas linhas vem do
contrato scanner (fatos 1 e 2) + o e2e validou que nenhuma regressão de fragmento/artifact aparece nas páginas
renderizadas. O caminho renderizado completo será exercitado sempre que o contrato passar num novo fragmento.

## Incidentes recuperados nesta rodada

1. **Recon subcontou; o contrato cobriu.** A varredura inicial focou `ToString().Substring` em GUIDs e achou 2
   fragmentos; a regra mais larga do contrato (`Substring(0,1..19)` + `[..1..19]`) revelou 4 ocorrências adicionais
   (digest de anexo, `FechamentoId[..8]` — o exemplo exato da diretriz R53 — exportação e triagens já conformes, e a
   máscara CPF). Lição registrada: o scanner do contrato deve ser sempre ≥ a varredura manual.
2. **Off-by-one na atribuição de linha** do scanner (checagem `pos <= 0` após subtrair em vez de `pos < length` antes
   de subtrair) descrenteava todas as linhas isentas em uma linha; primeira falha do fato 1 listando 4 falsas
   violações; correção trivial no loop.
3. **Rota do e2e:** `/AssistenteIa/Index` não existe — a ação é `[HttpGet("")]`, rota real `/AssistenteIa`. Corrigido
   no script versionado; `MeusPagamentos` virou fetch condicional (302 AccessDenied para perfis não-MEDICO).
4. **Flake transiente de suíte** (execução 1: 1 falha de 1636 sob execuções consecutivas, nome não capturado; execução
   2 com log: 1636/1636) — mesmo padrão documentado na R59; segue para o item 10 da fila (consolidação de flakes).

## Sucessoras / fila restante

Itens restantes da fila r53b1 §7: **7** (auditoria cadastro×operação), **8** (login CPF/CNPJ/e-mail guiado),
**9** (smoke de escrita c/ seed demo idempotente), **10** (consolidação de flakes — agora com a 2ª observação
transiente desta rodada).

## Arquivos da evidência (esta pasta)

- `r60a0-guids-fragmentos-plano.md` — recon, varreduras, inventário classificado e decisões D-R60-1/1b/2/3
- `r60_guids_e2e.ps1` — mini-e2e HTTP (BOM UTF-8) + `r60_guids_e2e_saida.txt` (saída da execução)
- `r60z-relatorio-final-consolidado.md` — este arquivo
