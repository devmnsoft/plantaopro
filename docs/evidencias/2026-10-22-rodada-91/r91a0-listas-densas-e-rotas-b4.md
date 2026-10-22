# R91a0 - Escala/performance/confiabilidade das listas densas: fechamento da paginação do §7 e contrato de rotas/guard do bloco B4

Plano: `docs/evidencias/2026-10-19-rodada-66/r66a0-plano-50-sprints.md` - item R91 do bloco B5
(linhas 82-83). Escopo desta rodada: fechamento formal da pendência 1 do r53b1 §7 (paginação
server-side/virtualização das listas densas), remedição com o volume atual da base; a varredura
ao vivo capturou também um defeito real de rotas/guard nas oito páginas do bloco B4, corrigido
no escopo de confiabilidade desta mesma rodada.

## Revalidação da paginação (pendência 1 do §7)

A paginação server-side está íntegra no código e os contratos da R54 seguem vivos na suíte
(`R54PaginacaoListasDensasContratoTests`): envelopes `OrcamentoResumoPaginado`/`SaldoEstoquePaginado`
em `Contracts.cs`, `LIMIT`/`OFFSET` nos repositórios `OrcamentoCirurgicoRepository`/`EstoqueRepository`,
clamp na Api e o índice da v2354 aplicado no banco de teste.

O volume cresceu desde a R54 (base então com 9.330 orçamentos; agora 11.304 linhas - o seed demo
avança com o uso) sem alterar o comportamento das páginas: o peso permanece no envelope e a
latência na faixa esperada (varredura `r91a_medicao_listas.ps1`, n=3 após warm-up, apps Release
contra `plantaopro_test`; saída em `r91a_medicao_saida.txt`):

| Rota | Bytes | Média ms |
|---|---|---|
| `/Administrativo360/Orcamentos` | 265.056 | 544,3 |
| `/Administrativo360/Orcamentos?pagina=2` | 265.137 | 351,3 |
| `/Administrativo360/Estoque` | 186.188 | 516,3 |
| `/Administrativo360/Estoque?pagina=2` | 186.300 | 452,3 |
| `/Administrativo360/PedidosCompra` | 168.948 | 282,7 |
| `/Administrativo360/RelatoriosFinanceiros` | 73.604 | 321,7 |
| `/Administrativo360/TitulosReceber` | 63.668 | 146,7 |
| `/Administrativo360/Vendas` | 61.880 | 142,7 |

Os endpoints JSON seguem paginados: `/api/administrativo360/orcamentos` 37.786 B / 113,3 ms;
`/api/administrativo360/estoque` 35.528 B / 285,3 ms. Antes da R54 estas listas custavam
17,2 MB / ~12-20 s (orçamentos) e 8,75 MB / ~9-19 s (estoque). **A pendência 1 do §7 está
formalmente fechada**: a linha de base continua plana com o volume maior.

## Defeito capturado pela varredura: rotas/guard das oito páginas do bloco B4

A primeira varredura ao vivo mediou as oito páginas do bloco B4 - todas responderam
**404**, com **500** adicionais em `GET /` e `GET /index`:

| Probe (pré-fix, apps no ar) | Status |
|---|---|
| `/FolhaWeb`, `/SimplesWeb`, `/CalendarioFiscalWeb`, `/DfeNacionalWeb`, `/TrilhosPagamentoWeb`, `/EsocialTrabalhoWeb`, `/VendasInternacionaisWeb`, `/LegislacaoWeb` (8 URLs do menu lateral) | 404 |
| `GET /` | 500 |
| `GET /index` | 500 |
| `POST /FolhaWeb/CalcularMes` (fetch da página) | 404 |

**Causa raiz confirmada**: os oito `*WebController` declaram `[HttpGet("")]`/`[HttpGet("Index")]`
**sem `[Route]` de classe** - sem prefixo, esses templates viram rotas absolutas e os oito
disputaram `/` (junto com o `CommercialDemoWebController`, dono histórico de `/` desde antes da
R82) e `/index`. Dois endpoints com o mesmo verbo + template absoluto produzem
`AmbiguousMatchException` (500) no request que os casa - por isso `/` e `/index` quebrados desde
a R82; e as URLs reais do menu (`/{Controller}/{Acao}`), usadas pelo JS das páginas, nunca
existiram (404).

Segunda camada: seis dos oito estavam **órfãos do catálogo `ControllerModules`** de
`SaasRouteGuardFilter` (somente `EsocialTrabalhoWeb`=ADM360, registrada na R88, e
`VendasInternacionaisWeb`=FATURAMENTO_SAAS, R89, tinham entrada) - mesmo com a rota corrigida, o
acesso direto cairia em `CATALOGO_NAO_CONFIGURADO` (redirect para `Account/AccessDenied` + log
ERROR a cada visita). A mesma verificação por scanner capturou um órfão extra acoplado a uma
superfície viva: `JornadaClientes` - a cadeia `/Clientes` - jornada do cliente - "Abrir jornada
completa" (`Views/Clientes/Jornada.cshtml`) também redirecionava para AccessDenied.

### Correção

- `[Route("{Controller}")]` de classe nos oito controllers (padrão-casa de `CentralEscala`/
  `Billing`/`Adm360*Web`), com comentário de causa raiz em cada arquivo;
- catálogo `ControllerModules` +7 entradas: `FolhaWeb`, `SimplesWeb`, `CalendarioFiscalWeb`,
  `DfeNacionalWeb`, `TrilhosPagamentoWeb`, `LegislacaoWeb` - todas `ADM360`; e
  `JornadaClientes` - `CLIENTES`.

### Prova ao vivo pós-fix

| Probe | Pré-fix | Pós-fix |
|---|---|---|
| As 8 páginas do menu | 404 | 200 - HTML de 40 a 74 kB, 60-105 ms |
| `GET /` | 500 | 200 - landing comercial `CommercialDemoWeb` (estado pré-R82 restaurado) |
| `GET /index` | 500 | 404 - nenhum endpoint disputa o template mais (ambiguidade sanada) |
| `POST /FolhaWeb/CalcularMes` | 404 | 200 - envelope BFF `{ ok, data/erro }` (118 B) |
| `r52_smoke_e2e.ps1` | - | `R52_E2E_OK FALHAS=0` (13 páginas-ok, acessibilidade estável) |
| `r54_pager_e2e.ps1` | - | `R54_PAGER_E2E_OK FALHAS=0` (página 2 mais leve: orçamentos 265-163 kB; busca preserva filtros) |

As oito páginas, agora alcançáveis, confirmam que **nenhuma é HEAVY** para fins da pendência do
§7: cada uma entrega 40-74 kB de HTML em <110 ms e consome os dados por fetch de ação com
payload pequeno - não há lista densa que justifique paginação nelas nesta rodada.

## Contrato novo: scanner de rotas do Web

`R91ConfiabilidadeRotasWebContratoTests` (2 fatos, leitura de fonte):

1. **Unicidade de template absoluto por verbo em todo o Web**: calcula o template absoluto de
   cada atributo de rota de ação por classe (comentários removidos - uma linha de comentário
   citando `[HttpPost("Iniciar")]` no `OnboardingController` prova a necessidade; arquivos com
   vários controllers, como `V116WebControllers.cs` e `PublicSelfServiceWebControllers.cs`, são
   processados classe por classe). Qualquer par de endpoints que produziria o mesmo verbo +
   template absoluto falha o fato - exatamente a classe de defeito que quebrou `/` e `/index`.
2. **Páginas autenticadas constam do catálogo do guard**: todo controller que renderiza view e
   não carrega `[AllowAnonymous]`/`[ApiController]` deve aparecer em `ControllerModules`, no
   conjunto público ou na allow-list legacy documentada no próprio fato (45 nomes em 4 famílias:
   órfãs de fase 1 fora do menu ativo; ilha V116 em `Views/V116`; ilha de launch B2B
   autoligada sem entrada no menu ativo - a única navegação que referencia alguma delas,
   `_NavigationRail.cshtml`, tem zero referências no Views; auxiliar de convites públicos). Quem
   reativar uma delas deve registrá-la no catálogo antes de linkar.

Controle negativo executado na rodada: removendo um único `[Route]` o fato 1 falha (colisão em
`/`); revertendo o catálogo do guard o fato 2 falha (órfãos enumerados).

## Mudanças desta rodada

- `backend/PlantaoPro.Web/Controllers/`: `[Route(...)]` + comentário nos oito controllers do
  bloco B4 (`FolhaWeb`, `SimplesWeb`, `CalendarioFiscalWeb`, `DfeNacionalWeb`,
  `TrilhosPagamentoWeb`, `EsocialTrabalhoWeb`, `VendasInternacionaisWeb`, `LegislacaoWeb`);
- `backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs`: +7 entradas de catálogo
  com comentários de origem;
- `backend/PlantaoPro.Tests/R91ConfiabilidadeRotasWebContratoTests.cs`: contrato novo (2 fatos);
- `docs/evidencias/2026-10-22-rodada-91/r91a_medicao_listas.ps1` (+ saída): varredura
  reproduzível - volumes no banco, subida API :51976 + Web :51977 (DLLs Release contra
  `plantaopro_test`), login demo, rotas raiz 1x, páginas densas e do B4 n=3, endpoints API.

Sem página nova, sem artigo novo e sem mudança de contrato de view (nenhum `.cshtml` alterado
nesta rodada); os 8 controllers seguem o contrato `{ ok, data/erro }` do bloco.

## Suíte e gates

- A/B Release `--no-incremental` (`r87abuild.ps1 -Tag r91`): base-exit=0, nova-exit=0,
  **DIFF=NENHUMA** (81 avisos brutos / 76 únicos por lado - idêntico ao canônico da R90);
- Suíte completa: **1777/1777 verde** (canônico pós-R90 era 1775; +2 fatos novos desta rodada);
- Inventário `r75_gap`: inalterado (nenhuma página/artigo adicionado) - 683/91/89/517/aux 29;
- `r62_docscan` após o documento: 0 defeitos;
- Higiene `RepositoryHygieneContractTests`: 8/8 (suíte final rodou depois de gravar a evidência).

## Próxima rodada

R92 (bloco B5): planos de execução - `EXPLAIN ANALYZE` das consultas mais pesadas dos módulos
densos com criação de índice onde faltando (linhas 84-85 do plano).
