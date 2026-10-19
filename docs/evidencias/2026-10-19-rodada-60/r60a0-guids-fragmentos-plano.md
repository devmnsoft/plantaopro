# R60 — r60a0 — Inventário completo de GUIDs/fragmentos remanescentes em UI

**Data:** 2026-10-19 · **Prévia desta rodada.** Gates executados no relatório final.
**Fila:** r53b1 §7 item 6 — "Inventário completo de GUIDs/fragmentos remanescentes em UI (R33/R50, E6)".
A própria diretriz (L116) define a pendência: *"inventário completo de fragmentos `[..8]` remanescentes"*.

## Recon — varreduras executadas (fonte, sem bin/obj/Tests)

| Varredura | Padrão | Resultado |
|---|---|---|
| Truncamentos em cs/cshtml | `ToString("N").Substring`, `.ToString().Substring(`, `[..N]` | 2 fora de testes: código interno `CUSTOM_<guid12>` (geração de `codigo` de perfil copiado — serviço, não copy) + 3 fragmentos renderizados no escaneamento fino |
| (2ª passada do contrato — regra mais larga `Substring(0,1..19)` + `[..1..19]`) | pegou 4 ocorrências que a 1ª varredura (focada em GUID) não listou: SHA256 de anexo [..8], **`FechamentoId[..8]`** (o exemplo exato da diretriz R53), Chave de Acesso NFe [..10]+tail (já com title) e máscara de documento CPF (formatação, não id) | ver inventário |
| Fragmentos client-side em wwwroot/js | `substring(0,N)`/`slice(0,N)`/`[0..N]` em contextos id/uuid/guid | apenas máscaras CPF/CNPJ/fone/CRMUF + fatia de data ISO — nenhum GUID |
| Renderização de membros `*Id` em Views | `@\w+(\.\w+)*\w*Id(?!\w|-)` | 120 ocorrências: 117 estruturais (asp-route, hidden inputs, `data-*`, ids de elemento, option values) + **3 visíveis** abaixo |
| Literais com formato GUID em Views | regex de GUID canônico | **3**: 2 placeholders neutros com zeros + 1 placeholder com o **tenant demo como exemplo** |
| Tenant demo `d3f6584c…` fora de testes | literal | 15× em `DevelopmentSeed.cs` (dados) + **1× na UI** (placeholder acima) |

## Inventário — cada ocorrência em UI classificada

| # | Local | O que é | Classe | Ação |
|---|---|---|---|---|
| 1 | `AssistenteIa/Index.cshtml:223` | coluna "Cliente" da tabela de custos IA: `TenantId.ToString().Substring(0, 8)…` (título com o inteiro) | **fragmento visível** | corrigir (D-R60-1) |
| 2 | `Administrativo360/Fiscal/Notas.cshtml:138` | coluna "Origem": badge do tipo + `origemId.ToString()[..8]…` (título com o inteiro) | **fragmento visível** | corrigir (D-R60-1) |
| 2b | `MinhaAgenda/MeusPagamentos.cshtml:9` | demonstrativo do profissional: "Plantão considerado no fechamento **`FechamentoId[..8]`**" — o exemplo exato citado na diretriz R53, sem valor inteiro acessível | **fragmento visível** | corrigir (D-R60-1) |
| 2c | `Administrativo360/Cotacoes/Detalhes.cshtml:408` | anexo: digest `SHA256: <hash[..8]>...` **sem** o inteiro em lugar nenhum | digest fragmentado cego | corrigir (D-R60-1b: título com o inteiro) |
| 2d | `Administrativo360/Cotacoes/Detalhes.cshtml:531` | exportação: `SHA256[..8]…` **com** `title=ExportacaoSha256Hash` | digest c/ inteiro em título | whitelist via D-R60-1b |
| 2e | `Administrativo360/DocumentosXml/Triagens.cshtml:78` | Chave de Acesso NFe `[..10]…[^6..]` **com** `title=ChaveAcesso` | chave fiscal c/ inteiro em título | whitelist via D-R60-1b |
| 2f | `Administrativo360/Colaboradores.cshtml:9` | função `CpfBr`: fatias `[..3]`, `[9..11]` de **formatação** de documento (não identificador) | máscara | whitelist de formatação (contrato ignora a linha) |
| 3 | `Administrativo360/Cotacoes/Detalhes.cshtml:287` | campo "ou identificador manual" (resx `Adm360.CotDet.LblIdentificadorManual`, ajuda já limpa de jargão): placeholder `Ex: d3f6584c-…` = **tenant demo vaza como exemplo** | exemplo com dado real | corrigir (D-R60-2) |
| 4 | `Administrativo360/Fiscal/Nova.cshtml:71-72` | "Código do documento de origem" — input operacional (confere no servidor antes de criar), placeholder zeros | campo etiquetado | whitelist (D-R60-3) |
| 5 | `Administrativo360/Cotacoes/Relacionamento.cshtml:182-183` | "Código interno (opcional)", placeholder zeros (tratado na R28) | campo etiquetado | whitelist (D-R60-3) |
| 6 | `CentralEscala/Substituicoes.cshtml:7` | filtro "Unidade (identificador)" — busca por nome já existe no campo vizinho "Profissional" | filtro etiquetado | whitelist (D-R60-3) |
| 7 | `ConferenciaExecucao/Index.cshtml:8-9` | filtros "Unidade (ID)" / "Profissional (ID)" | filtros etiquetados | whitelist (D-R60-3) |
| 8 | 117 atributos estruturais (rotas, hidden, `data-*`, option values) + arrays JS inline (`<text>{ id: … }</text>`) + `idempotencyKey=@Guid.NewGuid()` | máquina, não copy | estrutural | sem ação |
| 9 | `SecurityAdministrationServices.cs:243` `CUSTOM_<guid12>` | geração interna de `codigo` para perfil copiado (coluna máquina, não exibida como copy) | código interno | registrado, sem ação |
| 10 | Truncamentos em `PlantaoPro.Tests` (`PQT_/_CAPA_<guid8>`, etc.) | códigos de máquina em fixtures de teste | teste | fora de escopo (não é UI) |

## Decisões

### D-R60-1 — fragmento visível some: o identificador aparece inteiro ou não aparece
Nos três pontos (#1, #2, #2b) o valor passa a renderizar o **identificador completo**, sem
elipse. A direção E6 proibia o fragmento truncado
(ex.: `FechamentoId[.8]`) como conteúdo; onde a referência é operacionalmente necessária
(custo de qual cliente, nota de qual documento de origem), o inteiro é a forma honesta.
Sem mudança de backend (o nome do cliente/tenant não existe no viewmodel da linha de uso).

### D-R60-2 — placeholder de exemplo sem dado real
O campo manual de produto herda o padrão já usado nos outros campos operacionais (#4/#5):
placeholder neutro de formato `00000000-0000-0000-0000-000000000000`. O rótulo resx
("ou identificador manual") e a ajuda permanecem — já foram limpos de jargão pela R28/E6.

### D-R60-3 — whitelist dos campos operacionais etiquetados
Os campos #4–#7 aceitam/pedem identificadores porque o fluxo opera por id (fiscal confere
servidor; cotações cruzam com estoque interno; filtros de escala trabalham por unidade).
O contrato trava cada um **com seu rótulo** — se o rótulo sumir ou o placeholder voltar a
carregar dado de exemplo real, o contrato falha.

### D-R60-1b — chave longa/digest: abreviação só com o inteiro a um hover de distância
Hashes SHA256 (64 hex) e a Chave de Acesso NFe (44 dígitos) não cabem legíveis em linha de
lista; o padrão aceito — já existente na base antes desta rodada (exportações, triagens) — é
abreviar no visível e expor **o valor inteiro no `title` do mesmo elemento**. O digest de anexo
(#2c), que aparecia sem o inteiro, ganhou o título. O contrato codifica a regra: qualquer
`[..N]` em Views exige `title="` na mesma linha (ou é máscara de documento, linha isentada).

## Contrato novo — `R60GuidsEFragmentosContratoTests` (3 fatos, scanner de fonte)

1. `Views_SemFragmentosTruncadosDeIdentificador` — regra A: zero `Substring(0, 1..19)` em Views;
   regra B: zero fatias `[..1..19]` sem valor inteiro em `title` na mesma linha (exceto máscara).
2. `Views_SemLiteralGuidComDadosDeExemplo` — todo literal em formato GUID nos cshtml deve ser
   o placeholder neutro de zeros (nenhum guid com dígito não-zero).
3. `CamposOperacionaisDeIdentificador_RotulosTravados` — os 4 campos/filtros #4–#7 presentes
   com seus rótulos/plholders travados.

Suíte esperada: 1633 → **1636**.

## Gates (executados no relatório final)

Rebuild `--no-incremental` → gate `r20b1_gate.ps1`; dirigidos R60+R59; suíte integral; smoke R52;
mini-e2e R60 (páginas AssistenteIa + Fiscal/Notas renderizadas sem fragmento); docscan.
