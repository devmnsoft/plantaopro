# r8 — ANVISA em itens de cotação: seleção, linha, detalhe, comparativo e snapshot por versão enviada (§8)

**Data:** 2026-10-14 · **Base:** commit local r9d (`27e1d8d`) · **Banco:** `plantaopro` local (PostgreSQL 18) · **Tenant de prova:** Sistema Vertical (`…ca301`)

## 1. Escopo do corte

A regulação ANVISA do produto ofertado agora acompanha o item de cotação em **todos os pontos de exibição** exigidos pelo §8, com congelamento **por versão enviada**:

| Ponto | Como ficou |
|---|---|
| **Seleção de item** | O modal "Mapear" deixou de ser só um campo de UUID: agora há um `<select>` com o catálogo (via `api/administrativo360/cadastros/lookups`) que exibe a regulação ANVISA de cada candidato (nº / não aplicável / pendente) num painel ao vivo; a entrada manual por UUID continua disponível (`oninput` atualiza o painel). |
| **Linha / detalhe / comparativo** | A tabela da tela `Detalhes` (itens solicitados × ofertados) ganha, na célula do produto ofertado, um badge ANVISA do estado do cadastro do produto (verde = nº informado; cinza = "n/a" c/ justificação no tooltip; âmbar = pendente). |
| **PDF/Export (arquivo da proposta)** | O arquivo real exportado no canal manual (`PROPOSTA_…_PROPOSTA_EXPORTADA.json`) carrega o bloco `anvisa` por item, derivado do snapshot — ver §4. |

**Semântica (duas camadas, definidas e verificadas):**
- **Exibição viva (pré-envio):** o detalhe lê a regulação **atual** do catálogo via JOIN `adm360_cotacao_itens → adm360_produtos`. Se o cadastro do produto muda antes do envio, a tela reflete o novo estado (comportamento verificado: mutar o nº → detalhe mostra o novo valor).
- **Snapshot por versão enviada:** na aprovação da resposta (`AprovarRespostaAsync`), o jsonb `snapshot_proposta` congela o bloco `anvisa` de cada item **no momento da aprovação**. Versões já enviadas não mudam quando o catálogo é alterado depois — e o arquivo exportado deriva desse jsonb. Imutabilidade adicional já existia e foi exercitada: o arquivo exportado tem `UNIQUE(tenant_id, resposta_id)` + **trigger de banco** `trg_adm360_cotacao_exportacao_imutavel` que bloqueia UPDATE/DELETE na `adm360_cotacao_exportacoes`.

## 2. Migração

**Nenhuma nova DDL.** A regulação já vive em `adm360_produtos` (v2352) e o destino do congelamento já existe (`adm360_cotacao_respostas.snapshot_proposta jsonb`). O corte é 100% leitura via JOIN + inclusão de campos nos DTOs/snapshot.

## 3. Alterações por arquivo

| Arquivo | Alteração |
|---|---|
| `backend/PlantaoPro.Application/Administrativo360/CotacoesXmlContracts.cs` | `CotacaoItemDetalheDto`: +5 parâmetros **finais opcionais** `AnvisaNumero/AnvisaAplicabilidade/AnvisaEntradaManual/AnvisaVigencia/AnvisaJustificacao` (compatibilidade de fonte preservada para construtores posicionais existentes) |
| `backend/PlantaoPro.Infrastructure/Administrativo360/CotacoesRepository.cs` | (1) `ObterCotacaoPorIdAsync`: SELECT dos itens passa a selecionar as 5 colunas `pr.anvisa_*` (LEFT JOIN já existente) + mapeamento nos novos parâmetros (nulos sem produto); (2) `AprovarRespostaAsync`: SELECT do snapshot ganha alias `i`, `LEFT JOIN adm360_produtos` e as 5 colunas; objeto do jsonb ganha bloco anônimo `anvisa { numero, aplicabilidade, entrada_manual, vigencia (yyyy-MM-dd), justificacao }` por item |
| `backend/PlantaoPro.Web/Models/Administrativo360CotacoesXmlModels.cs` | `CotacaoItemDetalheViewModel`: +5 espelhos opcionais finais (deserialização JSON automática — sem mudança de mapping no controller); `CotacaoDetalhesPageViewModel`: +`Produtos` (catálogo para o modal) |
| `backend/PlantaoPro.Web/Controllers/Adm360CotacoesWebController.cs` | Ação `Detalhes`: busca `cadastros/lookups` (tolerante a falha — lista vazia degrada a seleção) e passa `Produtos` ao view model |
| `backend/PlantaoPro.Web/Views/Administrativo360/Cotacoes/Detalhes.cshtml` | (1) Célula do produto ofertado: badge ANVISA por estado (title c/ justificação no caso n/a); (2) modal Mapear: select de catálogo com `data-anvisa-*` por candidato + painel `anvisaInfo-{item}` (aria-live) + campo UUID mantido nomeado `ProdutoId` (binding `Guid?` intacto); (3) script único: `selecionarProduto()` preenche o UUID e atualiza o painel, `atualizarAnvisaInfo()` resolve o candidato e renderiza o estado; init por item ao carregar; (4) guia da tela: parágrafo "Regulação ANVISA" explicando os estados e o congelamento na aprovação |

## 4. Evidência de execução (tenant SV, saída real)

Jornada completa via API + Web autenticado (`admin.sistemavertical@plantaopro.local`):

```text
API_LOGIN_OK TOKEN_LEN=10164
RESTAURAR_P2_STATUS=200                                  # P2 -> NAO_APLICAVEL + justificação (estado demo r9c)
CAPTURAR_STATUS=200 BODY={"id":"ced7c533-91cd-…"}        # 2 itens (SVITEM-1, SVITEM-2)
REL1_STATUS=200 / REL2_STATUS=200                        # item1->P1(SV-LTE-001), item2->P2(SV-NOL-001)
IT1_ANVISA_NUMERO_OK=True[0001234567890123]              # detalhe: ANVISA via JOIN (zeros preservados)
IT1_ANVISA_APL=INFORMADO VIGENCIA=2029-01-31 MANUAL=True
IT2_ANVISA_APL_OK=True
IT2_ANVISA_JUST=[Produto de limpeza fora do escopo de regulacao de OPME (exemplo de homologacao).]
APROVAR_STATUS=200 BODY={"respostaId":"6529685f-b408-…"}
SNAP_ANVISA_KEY_OK=True SNAP_NUMERO_OK=True SNAP_NAOAPL_OK=True SNAP_VIGENCIA_OK=True
TRANSMITIR_STATUS=200 RESPOSTA_EXPORTADA_MANUALMENTE=True
EXPORT_NUMERO_OK=True EXPORT_NAOAPL_OK=True              # arquivo real contém os dois estados
MUTAR_P1_STATUS=200                                       # nº -> 0009876543210987
SNAP_INVARIAVEL_OK=True EXPORT_INVARIAVEL_OK=True        # versão enviada NÃO muda com o catálogo
DETALHE_LIVE_REFLETE_NOVO=True[0009876543210987]         # exibição pré-envio reflete o atual
RESTAURAR_P1_STATUS=200
P1_FINAL NUMERO=0001234567890123 APL=INFORMADO VIGENCIA=2029-01-31 VENDA=16.0000 MARGEM=37.50 MARKUP=60.0
WEB_DETALHES_STATUS=200
WEB_BADGE_NUM=True WEB_OPTION_DATA_NUM=True WEB_OPTION_NAOAPL=True
WEB_INFO_PANEL=True WEB_SELECT_HANDLER=True WEB_GUIDE_ANVISA=True
```

O snapshot jsonb (trecho real do banco após aprovação):

```json
{"Itens":[
  {"anvisa":{"numero":"0001234567890123","vigencia":"2029-01-31","aplicabilidade":"INFORMADO","entrada_manual":true},
   "numero_item":1,"preco_unitario_ofertado":20.5,"status_relacionamento":"RELACIONADO", …},
  {"anvisa":{"justificacao":"Produto de limpeza fora do escopo de regulacao de OPME (exemplo de homologacao).",
   "aplicabilidade":"NAO_APLICAVEL","entrada_manual":true},"numero_item":2, …}],
 "CotacaoId":"ced7c533-…","AprovadoEm":"2026-10-14T23:56:04Z","IdentificadorExterno":"SV-ANVISA-R9"}
```

Dados de teste criados no tenant SV (marcados como homologação): estabelecimento CNPJ sintético `12345678000199` ("Estabelecimento Homologacao Sistema Vertical"), conta portal `CONTAMANUAL-SV` (provedor `IMPORTACAO_MANUAL`), cotação `SV-ANVISA-R9` v1 com a proposta aprovada/exportada acima.

## 5. Artefatos

- `r8-sv-cotacao-anvisa.html` — snapshot real da tela `Administrativo360/Cotacoes/Detalhes/{id}` logado como SV (badges nas linhas, select com `data-anvisa-*`, painel de informação, guia da tela).
- `r8-sv-proposta-exportada.json` — arquivo da proposta exportada baixado de `respostas/{id}/exportacao` (bloco `anvisa` por item).

## 6. Observações e pendências registradas

1. **`SalvarProduto` faz overwrite completo da regulação ANVISA** — descoberto ao reproduzir o corte: o payload r9d (sem os campos ANVISA) zerou o estado do P2 para PENDENTE. Comportamento consistente (formulário completo = estado completo), mas qualquer novo cliente da API deve sempre enviar os 5 campos (o modal Web já envia todos). O script de evidência restaura o P2 antes da jornada e o corte termina com o estado demo r9c/r9d intacto (verificado: `P1_FINAL … MARGEM=37.50 MARKUP=60.0`).
2. **Imutabilidade em duas camadas verificada**: aplicação (ON CONFLICT DO NOTHING no registro da exportação) + banco (trigger bloqueando UPDATE/DELETE em `adm360_cotacao_exportacoes`). A limpeza da jornada de teste precisou drop/recriar o trigger (o caminho documentado para testes na própria migration v2200 é TRUNCATE; aqui havia exportações de outras jornadas, então drop→delete→recreate preservou o resto).
3. **`EXPORT_LEN` vazio na saída** — cosmético (header `Content-Length` não chega no cliente PS5 para resposta `File()`); o tamanho real do artefato é 1.867 bytes (arquivo salvo acima).
4. **Flake de teste isolado** — uma execução da suite acusou 1 falha; as reexecuções subsequentes saíram 1394/1394 duas vezes seguidas (máquina de RAM baixa, hosts concorrentes). Gate final: build 0 erros + suite 1394 verde.
5. **Sem PDF real** — o canal IMPORTACAO_MANUAL gera o arquivo da proposta em JSON (formato existente do módulo, imutável por hash SHA-256). O "PDF" do comparativo citado no prompt corresponde, nesta base, a esse artefato + a tela Detalhes (que é o comparativo solicitando×ofertado); nada de engine de PDF existe hoje no módulo — registrado, não inventado.
6. Sem alterações em resx (i18n 246 chaves intacto); sem novas permissões/policies (a jornada usa as policies Adm360 existentes já cobertas pelos grants escopados do usuário SV — login e toda a jornada executaram com sucesso).
