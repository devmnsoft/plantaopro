# ADR-0003 — Contrato BFF: espelho ViewModel × DTO da Api (familia Administrativo360)

- **Status:** aceita (rodada 20 r20b1)
- **Contexto:** a Web não referencia o projeto Api; a camada BFF (`ReadApiResponse<T>` nos controllers `Administrativo360Controller*` / `Adm360*WebController*`) deserializa a resposta JSON em ViewModels espelho declarados em `PlantaoPro.Web/Models`. O binding System.Text.Json é **por nome, case-insensitive**: uma divergência de grafia na chave (`DataBaixa` × `DataRecebimento`, `MotivoEstorno` × `RecebidoPor`, `Ativa` × `Ativo` — família F9d) deixa o campo **sempre no default silenciosamente** (`0001-01-01`, `""`, `0m`) em produção, sem erro em nenhum dos dois lados. A auditoria R20 (r20b1) mediu a superfície (90 call-sites, 53 endpoints distintos) e encontrou 6 defeitos reais dessa família (F-R20-A..G): coluna de baixa toda em `0001-01-01` no relatório de comissões e CSV, todas as linhas do fluxo de caixa classificadas como "Prev. Pagar", motivo de estorno em branco no detalhe do título, e campos/tipos mortos (`Observacoes`, `CriadoEm` ×2, `ExtratoLancamentoViewModel` + `ExtratoContaViewModel`).

## Decisão

1. **Espelho com grafia idêntica à chave que a Api emite** é o padrão canônico: o nome da propriedade do ViewModel espelha a chave JSON do DTO de destino. Renomear chave na Api exige atualizar o espelho Web na mesma rodada (o contrato versionado falha até os dois lados casarem — precedente F9d).
2. **Tipo Application compartilhado** pode ser referenciado direto pela Web quando o DTO é neutro de apresentação (ex.: `ParametrosFiscaisAmbienteDto`, `NotaPreEmitidaDetalhesDto`) — sem duplicação; nesse caso o nome igual já é a prova do binding.
3. **Derivação de apresentação sobre valores já transportados** é permitida na view/BFF (ex.: classificação realizado/previsto do fluxo de caixa derivada dos valores `RealizadoEntrada/Saida`/`PrevistoEntrada/Saida`) — mas nunca pode depender de chave que o wire não emite.
4. Propriedade **"espelho aguardando a Api"** (declarada na Web, consumida pela view com degradação suave, ainda não emitida no wire — padrão R8-B27 de definições de indicadores) deve estar registrada no allowlist `EspelhoAguardandoApi` do contrato, com justificativa; a entrada fica obsoleta (e falha o teste) assim que a Api passa a emitir a chave, forçando a limpeza.
5. O mapeamento endpoint → tipo de resposta é **versionado** num dicionário fixo dentro do teste de contrato (53 entradas). Cada novo `ReadApiResponse<T>` exige nova entrada; entrada sem call-site vivo falha (sem row fantasma).

## Consequências

- Guarda versionada em `R20BffMappingContratoTests` (3 fatos):
  - todo call-site BFF cobre entrada do dicionário com o elemento Web exato, e todo endpoint do dicionário tem call-site vivo (antivacuidade nos dois sentidos — superfície medida: 90 chamadas / 53 endpoints);
  - todo tipo do dicionário existe em Application/Api com uso visível (≥2 ocorrências — proxy de rota viva; prova definitiva de rota segue por smoke nível-API c/ Bearer, critério R19);
  - propriedades do lado Web (posicionais sem default + init/set; get-only calculado excluído) ⊆ propriedades do DTO da Api, **case-insensitive, recursivo em toda a árvore** (elementos de coleção — 809 propriedades comparadas, 21 comparações aninhadas), exceto o allowlist `EspelhoAguardandoApi`.
- Custo de manutenção: renomear um DTO exige 2 lados + o dicionário na mesma rodada; o teste quebra de forma explícita com o par de nomes, então o custo é encontrar e corrigir, não descobrir em produção.
- Escopo: familia `api/administrativo360/*` + `api/usuarios` (consumo Adm360). Outros módulos BFF entram no mesmo esquema se/quando ganharem superfície relevante.
