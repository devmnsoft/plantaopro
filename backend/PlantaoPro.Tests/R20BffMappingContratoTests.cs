using System.Text.Json;
using System.Text.RegularExpressions;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Web.Models;
using Xunit;

namespace PlantaoPro.Tests;

// ============================================================================
// R20 (r20b1) / ADR-0003: contrato BFF versionado entre o ViewModel Web e o DTO da Api.
//
// A Web e a Api nao compartilham types de resposta (a Web espelha; binding JSON por nome,
// case-insensitive, via System.Text.Json). Um nome de propriedade que diverge na grafia —
// DataBaixa x DataRecebimento, MotivoEstorno x RecebidoPor, Ativa x Ativo (F9d) — deixa o
// campo SEMPRE no default silenciosamente (0001-01-01, "", 0m) em producao. Este fato trava
// essa familia de defeitos: para cada endpoint BFF da familia Adm360, cada propriedade do
// tipo Web usado em ReadApiResponse<T> precisa existir (case-insensitive) no tipo que a Api
// devolve, em toda a arvore aninhada (itens de colecoes recursivos).
//
// Regras codificadas aqui (ver ADR-0003):
//  - Espelho com grafia idêntica ao DTO da Api (padrao canonico).
//  - Tipo Application compartilhado referencia-do direto pela Web (ex.: ParametrosFiscaisAmbienteDto)
//    eh aceito e sai da comparacao (o nome igual ja e a prova).
//  - Propriedades "espelho aguardando a API" (declaradas na Web, consumidas com degradação
//    suave pela view, ainda nao emitidas no wire) ficam no allowlist EspelhoAguardandoApi com
//    justificativa; a entrada fica obsoleta (e falha o teste) quando a Api passa a emitir.
// ============================================================================
public sealed class R20BffMappingContratoTests
{
    private static readonly string Root = RepositoryPathResolver.RepoRoot;

    // ------------------------------------------------------------------
    // Dicionario versionado: endpoint normalizado => (elemento Web, elemento Api).
    // Os valores sao os tipos ELEMENTO (o container — List<IReadOnlyList<> — deve ser o mesmo
    // nas duas pontas; o envelope ApiResponse e responsabilidade de LerAsync na Api).
    // Cada novo ReadApiResponse<T> precisa de uma entrada nova; se a Api renomear uma chave,
    // o espelho Web tem que seguir (falha este teste ate atualizar os dois lados).
    // ------------------------------------------------------------------
    private static readonly Dictionary<string, (string Web, string Api)> Contrato = new(StringComparer.OrdinalIgnoreCase)
    {
        ["api/administrativo360/cadastros/lookups"] = ("Lookups360ViewModel", "Lookups360Bundle"),
        ["api/administrativo360/cotacoes"] = ("CotacaoResumoViewModel", "CotacaoResumoDto"),
        ["api/administrativo360/cotacoes/{x}"] = ("CotacaoDetalhesViewModel", "CotacaoDetalhesDto"),
        ["api/administrativo360/cotacoes/respostas"] = ("CotacaoRespostaViewModel", "CotacaoRespostaDto"),
        ["api/administrativo360/cotacoes/mapeamentos"] = ("MapeamentoDeParaViewModel", "MapeamentoDeParaDto"),
        ["api/administrativo360/cotacoes/contas-portal"] = ("PortalContaViewModel", "PortalContaDto"),
        ["api/administrativo360/cotacoes/estabelecimentos"] = ("EstabelecimentoViewModel", "EstabelecimentoDto"),
        ["api/administrativo360/xml"] = ("DocumentoRecebidoResumoViewModel", "DocumentoRecebidoResumoDto"),
        ["api/administrativo360/xml/{x}"] = ("DocumentoRecebidoDetalhesViewModel", "DocumentoRecebidoDetalhesDto"),
        ["api/administrativo360/xml/triagens"] = ("TriagemDocumentoViewModel", "TriagemDocumentoDto"),
        ["api/administrativo360/xml/sincronizacoes"] = ("DfeSincronizacaoViewModel", "DfeSincronizacaoDto"),
        ["api/usuarios"] = ("UsuarioResponsavelViewModel", "UserListVM"),
        ["api/administrativo360/fiscal/parametros"] = ("ParametrosFiscaisAmbienteDto", "ParametrosFiscaisAmbienteDto"),
        ["api/administrativo360/fiscal/notas/{x}"] = ("NotaPreEmitidaDetalhesDto", "NotaPreEmitidaDetalhesDto"),
        ["api/administrativo360/gestao/dashboard"] = ("DashboardAdm360ViewModel", "DashboardAdm360Resposta"),
        ["api/administrativo360/gestao/capacidades-ativas"] = ("string", "string"),
        ["api/administrativo360/orcamentos"] = ("OrcamentoResumoViewModel", "OrcamentoResumo"),
        ["api/administrativo360/orcamentos/{x}"] = ("OrcamentoDetalhesViewModel", "OrcamentoDetalhes"),
        ["api/administrativo360/orcamentos/{x}/revisoes"] = ("OrcamentoRevisaoHistoricoViewModel", "OrcamentoRevisaoHistorico"),
        ["api/administrativo360/orcamentos/{x}/reserva-planejamento"] = ("PlanejamentoReservaOrcamentoViewModel", "PlanejamentoReservaOrcamento"),
        ["api/administrativo360/cirurgias"] = ("CirurgiaResumoViewModel", "CirurgiaResumo"),
        ["api/administrativo360/cirurgias/{x}"] = ("CirurgiaDetalhesViewModel", "CirurgiaDetalhes"),
        ["api/administrativo360/compras"] = ("PedidoCompraResumoViewModel", "PedidoResumo"),
        ["api/administrativo360/qualidade/pendentes"] = ("InspecaoPendenteViewModel", "InspecaoPendente"),
        ["api/administrativo360/ocorrencias"] = ("Ocorrencia360ViewModel", "OcorrenciaResumo"),
        ["api/administrativo360/resumo"] = ("Administrativo360ResumoViewModel", "Administrativo360ResumoDto"),
        ["api/administrativo360/estoque"] = ("SaldoEstoqueViewModel", "SaldoEstoque"),
        ["api/administrativo360/inventarios"] = ("InventarioResumoViewModel", "InventarioResumo"),
        ["api/administrativo360/coleta/tarefas"] = ("TarefaColetaViewModel", "TarefaColeta"),
        ["api/administrativo360/valorizacoes/pendentes"] = ("ValeResumoViewModel", "ValeResumo"),
        ["api/administrativo360/valorizacoes/previa/{x}"] = ("PreviaValorizacaoViewModel", "PreviaValorizacaoDto"),
        ["api/administrativo360/vales"] = ("ValeResumoViewModel", "ValeResumo"),
        ["api/administrativo360/vales/{x}"] = ("ValeDetalhesViewModel", "ValeDetalhes"),
        ["api/administrativo360/vendas"] = ("VendaResumoViewModel", "VendaResumoDto"),
        ["api/administrativo360/vendas/{x}"] = ("VendaDetalhesViewModel", "VendaDetalhesDto"),
        ["api/administrativo360/titulos"] = ("TituloReceberResumoViewModel", "TituloReceberResumoDto"),
        ["api/administrativo360/titulos/{x}"] = ("TituloReceberDetalhesViewModel", "TituloReceberDetalhesDto"),
        ["api/administrativo360/titulos-pagar"] = ("TituloPagarResumoViewModel", "TituloPagarResumoDto"),
        ["api/administrativo360/titulos-pagar/{x}"] = ("TituloPagarDetalhesViewModel", "TituloPagarDetalhesDto"),
        ["api/administrativo360/titulos-pagar/comissoes-pendentes"] = ("ComissaoPendenteViewModel", "ComissaoPendenteDto"),
        ["api/administrativo360/caixa/contas"] = ("ContaFinanceiraViewModel", "ContaFinanceiraDto"),
        ["api/administrativo360/caixa/fluxo"] = ("FluxoCaixaViewModel", "FluxoCaixaDto"),
        ["api/administrativo360/caixa/fechamentos"] = ("CaixaFechamentoViewModel", "CaixaFechamentoDto"),
        ["api/administrativo360/contratos/{x}"] = ("Contrato360DetalheViewModel", "ContratoTrabalhoDetalheDto"),
        ["api/administrativo360/relatorios/vales-pendentes"] = ("RelatorioValesPendentesViewModel", "RelatorioValesPendentesItem"),
        ["api/administrativo360/relatorios/custodia-externa"] = ("RelatorioCustodiaExternaViewModel", "RelatorioCustodiaExternaItem"),
        ["api/administrativo360/relatorios/reconciliacao"] = ("RelatorioReconciliacaoViewModel", "RelatorioReconciliacaoItem"),
        ["api/administrativo360/relatorios/rastreabilidade"] = ("RelatorioRastreabilidadeViewModel", "RelatorioRastreabilidadeItem"),
        ["api/administrativo360/relatorios-financeiros/vendas"] = ("RelatorioVendasItemViewModel", "RelatorioVendasItem"),
        ["api/administrativo360/relatorios-financeiros/comissoes"] = ("RelatorioComissaoItemViewModel", "RelatorioComissaoItem"),
        ["api/administrativo360/relatorios-financeiros/margem"] = ("RelatorioMargemItemViewModel", "RelatorioMargemItem"),
        ["api/administrativo360/relatorios-financeiros/vencimentos"] = ("RelatorioVencimentoItemViewModel", "RelatorioVencimentoItem"),
        ["api/administrativo360/relatorios-financeiros/dre"] = ("DreGerencialDtoWeb", "DreGerencialDto"),
    };

    // "tipoWeb::propriedade" — declarados na Web (consumidos por views com degradacao suave)
    // mas ainda NAO emitidos pela Api no wire. Mantidos como espelho do padrao R8-B27
    // (definicoes de indicadores fieis ao SQL + criterio de escopo), ja usado em resumo/dashboard.
    private static readonly HashSet<string> EspelhoAguardandoApi = new(StringComparer.OrdinalIgnoreCase)
    {
        // FluxoCaixa.cshtml L260-273: exibe "Sem definicoes informadas pela fonte." quando vazio;
        // TimeCriterion renderiza linha em branco. A Api (FluxoCaixaDto) ainda nao carrega as
        // 8 definicoes de fluxo de caixa — extensao pendente da familia R8-B27.
        "FluxoCaixaViewModel::Definicoes",
        "FluxoCaixaViewModel::TimeCriterion",
    };

    [Fact]
    public void toda_chamada_bff_tem_contrato_versionado()
    {
        var chamadas = ExtrairChamadasBff();
        Assert.True(chamadas.Count >= 80, $"Scan suspeito: apenas {chamadas.Count} chamadas ReadApiResponse (superficie R20: ~90).");

        var erros = new List<string>();
        var porEndpoint = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // endpoint => elemento Web

        foreach (var c in chamadas)
        {
            if (!Contrato.TryGetValue(c.Endpoint, out var par))
            {
                erros.Add($"{c.Arquivo}:L{c.Linha} '{c.Endpoint}' sem entrada no dicionario (T={c.TipoBruto})");
                continue;
            }
            if (!string.Equals(par.Web, c.Elemento, StringComparison.Ordinal))
                erros.Add($"{c.Arquivo}:L{c.Linha} '{c.Endpoint}' usa elemento '{c.Elemento}', dicionario fixa '{par.Web}' — atualize o contrato (ADR-0003).");
            porEndpoint[c.Endpoint] = c.Elemento;
        }
        Assert.Empty(erros);

        // Antivacuidade inversa: toda entrada do dicionario precisa ter chamada viva (sem row fantasma).
        var obsoletas = Contrato.Keys.Where(k => !porEndpoint.ContainsKey(k)).OrderBy(x => x).ToList();
        Assert.Empty(obsoletas);

        Assert.True(porEndpoint.Count >= 50, $"Scan suspeito: apenas {porEndpoint.Count} endpoints distintos (superficie R20: 53).");
    }

    [Fact]
    public void tipos_do_contrato_existem_e_sao_referenciados_na_api()
    {
        var apiMap = CarregarTiposApi();
        // Uso provado em Application OU Api (controllers costumam apenas invocar o service;
        // o tipo aparece nas assinaturas do repositório/serviço em Application).
        var roots = new[] { Path.Combine(Root, "backend/PlantaoPro.Application"), Path.Combine(Root, "backend/PlantaoPro.Api") };
        var texto = string.Join("\n",
            roots.SelectMany(d => Directory.GetFiles(d, "*.cs", SearchOption.AllDirectories))
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .Select(File.ReadAllText));

        var erros = new List<string>();
        foreach (var par in Contrato.Values)
        {
            if (par.Web == par.Api && EscalaresConhecidos(par.Api)) continue; // escalar (ex.: capacidades-ativas devolve strings)
            if (!apiMap.ContainsKey(par.Api))
            {
                erros.Add($"tipo Api '{par.Api}' nao encontrado em Application/Api");
                continue;
            }
            var ocorrencias = texto.Split(par.Api, StringSplitOptions.None).Length - 1;
            if (ocorrencias < 2)
                erros.Add($"tipo Api '{par.Api}' declarado mas sem uso visivel (ocorrencias={ocorrencias}; suspeito de tipo morto ou rota ausente)");
        }
        Assert.Empty(erros);
    }

    [Fact]
    public void props_web_subconjunto_das_props_api_em_toda_arvore()
    {
        var webMap = CarregarTiposWeb();
        var apiMap = CarregarTiposApi();

        // ----------------------------- comparacao recursiva -----------------------------

        var violacoes = new List<string>();
        var visitados = new HashSet<(string W, string A)>();
        int propsComparedas = 0;
        int comparacoesAninhadas = 0;

        void Comparar(string w, string a, string caminho)
        {
            if (!visitados.Add((w, a))) return;
            if (w == a) return; // tipo compartilhado (Application) ou escalar — a igualdade de nome ja e a prova

            var wd = webMap[w];
            var ad = apiMap[a];

            foreach (var (nome, _) in wd.Todos)
            {
                propsComparedas++;
                var temNoApi = ad.NomeIgnoreCase(nome);
                if (!temNoApi && EspelhoAguardandoApi.Contains(w + "::" + nome)) continue;
                if (!temNoApi)
                {
                    violacoes.Add($"{w}.{nome} (em {caminho}) — chave ausente no DTO {a}; o STJ deixaria o campo sempre no default");
                    continue;
                }
                var propApi = ad.PorNome![nome.ToLowerInvariant()];
                var propWeb = wd.PorNome![nome.ToLowerInvariant()];
                var eW = ElementoDoTipo(propWeb.Tipo);
                var eA = ElementoDoTipo(propApi.Tipo);
                if (eW == eA) continue;
                if (EscalaresConhecidos(eW) && EscalaresConhecidos(eA))
                {
                    violacoes.Add($"{w}.{nome} (em {caminho}): tipo '{propWeb.Tipo}' x Api '{propApi.Tipo}' — primitivos divergentes quebram o binding");
                    continue;
                }
                if (webMap.ContainsKey(eW) && apiMap.ContainsKey(eA))
                {
                    comparacoesAninhadas++;
                    Comparar(eW, eA, $"{caminho}/{nome}");
                }
            }
        }

        foreach (var (endpoint, par) in Contrato)
            Comparar(par.Web, par.Api, endpoint);

        Assert.Empty(violacoes);

        // Antivacuidade: superficies medidas em r20b1 (90 chamadas / 53 endpoints / 809 props /
        // 21 comparacoes aninhadas). Os pisos quebram se o scan perder a superficie real.
        Assert.True(propsComparedas >= 700, $"Scan suspeito: apenas {propsComparedas} propriedades comparadas (superficie R20: 809).");
        Assert.True(comparacoesAninhadas >= 15, $"Scan suspeito: apenas {comparacoesAninhadas} comparacoes de tipos aninhados (superficie R20: 21; recursao morreu?).");
    }

    [Fact]
    public void binding_stj_liga_o_wire_ao_espelho_nos_fixes_r20()
    {
        // F-R20-A/F-R20-C (mecanismo, offline): serializa o DTO real da Api como o ASP.NET Core
        // emite (web defaults => camelCase) e desserializa no espelho Web com as opcoes do BFF
        // (BaseWebController.JsonOptions). O binding STJ e por nome: grafia casada garante o valor;
        // sob a grafia antiga (dataBaixa/motivoEstorno) o campo ficava para sempre 0001-01-01 / "".
        var web = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var emissao = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var dtoBaixa = new TituloBaixaDto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Caixa Geral",
            new DateOnly(2026, 10, 18), 1234.56m, "PIX", null, Estornado: true, RecebidoPor: "Maria Silva",
            new DateTime(2026, 10, 18, 9, 30, 0));
        var wireBaixa = JsonSerializer.Serialize(dtoBaixa, emissao);
        var vmBaixa = JsonSerializer.Deserialize<TituloBaixaViewModel>(wireBaixa, web);
        Assert.Equal(new DateOnly(2026, 10, 18), vmBaixa!.DataRecebimento);
        Assert.Equal("Maria Silva", vmBaixa.RecebidoPor);
        Assert.Equal(true, vmBaixa.Estornado);
        Assert.DoesNotContain("\"dataBaixa\"", wireBaixa); // a chave antiga nem existia no wire

        var dtoComissao = new RelatorioComissaoItem("Joao Vendedor", "V-102", new DateOnly(2026, 9, 30), 1000m, 5m, 50m, "APROVADA");
        var wireComissao = JsonSerializer.Serialize(dtoComissao, emissao);
        var vmComissao = JsonSerializer.Deserialize<RelatorioComissaoItemViewModel>(wireComissao, web);
        Assert.Equal(new DateOnly(2026, 9, 30), vmComissao!.DataRecebimento);
        Assert.Equal(50m, vmComissao.ComissaoApropriada);
        Assert.DoesNotContain("\"dataBaixa\"", wireComissao);
    }

    // ----------------------------- extracao de call-sites -----------------------------

    private sealed record ChamadaBff(string Arquivo, int Linha, string Endpoint, string TipoBruto, string Elemento);

    private static List<ChamadaBff> ExtrairChamadasBff()
    {
        var dir = Path.Combine(Root, "backend/PlantaoPro.Web/Controllers");
        var resultado = new List<ChamadaBff>();
        var padrao = new Regex("ReadApiResponse(?:Async)?\\s*<\\s*(.+?)\\s*>\\s*\\(\\s*(?:client|httpClient)\\s*,\\s*(.+?)\\)");

        foreach (var arquivo in Directory.GetFiles(dir, "*.cs"))
        {
            var nome = Path.GetFileName(arquivo);
            var ehFamilia = nome == "Administrativo360Controller.cs"
                || nome.StartsWith("Administrativo360Controller.")
                || (nome.StartsWith("Adm360") && nome.EndsWith("WebController.cs"));
            if (!ehFamilia) continue;

            var linhas = File.ReadAllLines(arquivo);
            var texto = string.Join("\n", linhas);
            for (var i = 0; i < linhas.Length; i++)
            {
                if (!linhas[i].Contains("ReadApiResponse")) continue;
                var m = padrao.Match(linhas[i]);
                if (!m.Success) continue;
                var tipoBruto = m.Groups[1].Value.Trim();
                var argumento = m.Groups[2].Value.Trim();
                var url = ResolverUrl(argumento, texto, i);
                resultado.Add(new ChamadaBff(nome, i + 1, NormalizarEndpoint(url), tipoBruto, ElementoDoTipo(tipoBruto)));
            }
        }
        return resultado;
    }

    /// <summary>Resolve o argumento de URL (literal, interpolado, local ou const) para o path base.</summary>
    private static string ResolverUrl(string argumento, string textoArquivo, int linhaChamada)
    {
        var mInterp = Regex.Match(argumento, "^\\$\"(.+)\"$");
        var mLit = Regex.Match(argumento, "^\"(.+)\"$");
        if (mInterp.Success) return SubstituirExpressoes(mInterp.Groups[1].Value, textoArquivo, linhaChamada);
        if (mLit.Success) return mLit.Groups[1].Value;

        // identificador (com possivel sufixo + "literal"): sobe ate a declaracao mais proxima e
        // toma o primeiro literal citado no RHS (o resto e query-string ou concatenacao condicional).
        var mid = Regex.Match(argumento, "^[A-Za-z_][A-Za-z0-9_]*");
        if (!mid.Success) throw new InvalidOperationException($"Argumento de URL nao suportado pelo scanner: '{argumento}'");
        var ident = mid.Value;
        var sufixo = "";
        foreach (Match mc in Regex.Matches(argumento, @"\+\s*""([^""]*)"""))
            sufixo += mc.Groups[1].Value; // ex.: RotaApi + "/parametros"
        var linhas = textoArquivo.Split('\n');
        for (var i = linhaChamada - 1; i >= 0; i--)
        {
            var md = Regex.Match(linhas[i], $"(?:var|string)[^=]*\\b{Regex.Escape(ident)}\\s*=\\s*(.*)$");
            if (!md.Success) continue;
            var mq = Regex.Match(md.Groups[1].Value, "\"([^\"]*)\"");
            if (!mq.Success) throw new InvalidOperationException($"Declaracao de '{ident}' sem literal no RHS: '{linhas[i].Trim()}'");
            return SubstituirExpressoes(mq.Groups[1].Value + sufixo, textoArquivo, linhaChamada);
        }
        throw new InvalidOperationException($"Local '{ident}' sem declaracao acima do call-site (file/texto incompleto?)");
    }

    /// <summary>Processa as expressoes {expr} de um literal interpolado: consts e locals viram seu
    /// prefixo de path (query cortada); o resto vira o placeholder anonimo {x}.</summary>
    private static string SubstituirExpressoes(string bruto, string textoArquivo, int linhaChamada)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < bruto.Length; i++)
        {
            if (bruto[i] != '{') { sb.Append(bruto[i]); continue; }
            var fim = bruto.IndexOf('}', i);
            if (fim < 0) { sb.Append(bruto[i]); continue; }
            var expr = bruto[(i + 1)..fim];
            var simples = Regex.IsMatch(expr, "^[A-Za-z_][A-Za-z0-9_]*$");
            if (simples)
            {
                var mConst = Regex.Match(textoArquivo, $"(?:const string|static readonly string)\\s+{Regex.Escape(expr)}\\s*=\\s*\"([^\"]*)\"");
                if (mConst.Success)
                {
                    sb.Append(NormalizarEndpoint(mConst.Groups[1].Value));
                    i = fim;
                    continue;
                }
                var linhas = textoArquivo.Split('\n');
                bool resolvido = false;
                for (var k = linhaChamada - 1; k >= 0 && !resolvido; k--)
                {
                    var md = Regex.Match(linhas[k], $"(?:var|string)[^=]*\\b{Regex.Escape(expr)}\\s*=\\s*(.*)$");
                    if (!md.Success) continue;
                    var mq = Regex.Match(md.Groups[1].Value, "\"([^\"]*)\"");
                    if (!mq.Success) continue;
                    var prefixo = NormalizarEndpoint(mq.Groups[1].Value);
                    sb.Append(prefixo);
                    resolvido = true;
                }
                if (!resolvido) sb.Append("{x}");
            }
            else sb.Append("{x}");
            i = fim;
        }
        return NormalizarEndpoint(sb.ToString());
    }

    /// <summary>minúsculas, sem query-string, placeholders de path anonimos ({x}), cauda dinamica cortada.</summary>
    internal static string NormalizarEndpoint(string url)
    {
        var s = (url ?? "").Trim().Trim('"', '$').ToLowerInvariant();
        var q = s.IndexOf('?');
        if (q >= 0) s = s[..q];
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] != '{') { sb.Append(s[i]); continue; }
            var fim = s.IndexOf('}', i);
            if (fim < 0) { sb.Append(s[i]); continue; }
            if (sb.Length > 0 && sb[^1] == '/')
            {
                sb.Append("{x}");
                i = fim; // pula o conteudo do placeholder ate o '}'
            }
            else i = s.Length; // cauda dinamica sem '/' antes (dre{query}) — cortada
        }
        return sb.ToString().TrimEnd('/');
    }

    private static readonly HashSet<string> Escalares = new(StringComparer.Ordinal)
    {
        "string", "int", "long", "double", "float", "decimal", "bool", "Guid", "DateTime", "DateTimeOffset", "DateOnly", "TimeOnly"
    };

    private static bool EscalaresConhecidos(string t) => Escalares.Contains(t);

    /// <summary>Tipo base sem nullability/generics: IReadOnlyList&lt;X&gt; => X (elemento), Guid? => Guid.</summary>
    internal static string ElementoDoTipo(string bruto)
    {
        var s = (bruto ?? "").Trim();
        while (s.EndsWith("?")) s = s[..^1].Trim();
        var lt = s.IndexOf('<');
        if (lt > 0)
        {
            var arg = s[(lt + 1)..];
            var gt = arg.LastIndexOf('>');
            if (gt >= 0) s = arg[..gt].Split(',')[0].Trim().TrimEnd('?');
        }
        return s;
    }

    // ----------------------------- parsing das arvores de tipos -----------------------------

    private sealed class TipoFonte
    {
        public Dictionary<string, (string Nome, string Tipo)> TodosPorChave = new(StringComparer.OrdinalIgnoreCase);
        public List<(string Nome, string Tipo)> Todos = new();
        public List<string> Requeridos = new(); // Web: posicional sem default + props de corpo (viram do JSON)
        public bool NomeIgnoreCase(string n) => TodosPorChave.ContainsKey(n);
        public Dictionary<string, (string Nome, string Tipo)> PorNome => TodosPorChave;
    }

    private static Dictionary<string, TipoFonte> CarregarTiposWeb() =>
        CarregarTipos(Path.Combine(Root, "backend/PlantaoPro.Web/Models"), exigirCorpoParametros: true);

    private static Dictionary<string, TipoFonte> CarregarTiposApi()
    {
        // Ordem importa em colisao de nome: contratos familiares primeiro, depois Application e Api.
        var mapa = new Dictionary<string, TipoFonte>();
        Adicionar(mapa, Path.Combine(Root, "backend/PlantaoPro.Application/Administrativo360"));
        Adicionar(mapa, Path.Combine(Root, "backend/PlantaoPro.Application"));
        Adicionar(mapa, Path.Combine(Root, "backend/PlantaoPro.Api"));
        return mapa;

        void Adicionar(Dictionary<string, TipoFonte> destino, string dir)
        {
            foreach (var (nome, tipo) in CarregarTipos(dir, exigirCorpoParametros: false))
                destino.TryAdd(nome, tipo);
        }
    }

    private static Dictionary<string, TipoFonte> CarregarTipos(string dir, bool exigirCorpoParametros)
    {
        var mapa = new Dictionary<string, TipoFonte>();
        var decl = new Regex(@"\b(?:public\s+|private\s+|internal\s+)?(?:sealed\s+|static\s+|abstract\s+)*(record|class)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(\(|\{)");
        foreach (var arquivo in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\")))
        {
            var txt = File.ReadAllText(arquivo);
            foreach (Match m in decl.Matches(txt))
            {
                var nome = m.Groups[2].Value;
                var corpo = "";
                var paramsTexto = "";
                var idx = m.Index + m.Length;
                if (m.Groups[3].Value == "(")
                {
                    var fecha = FindMatching(txt, idx - 1, '(', ')');
                    if (fecha < 0) continue;
                    paramsTexto = txt[(idx)..fecha];
                    var j = SkipSpaces(txt, fecha + 1);
                    if (j < txt.Length && txt[j] == '{')
                        corpo = txt.Substring(j, FindMatching(txt, j, '{', '}') - j + 1);
                }
                else if (idx < txt.Length)
                {
                    var j = SkipSpaces(txt, idx);
                    if (j < txt.Length && txt[j] == '{')
                        corpo = txt.Substring(j, FindMatching(txt, j, '{', '}') - j + 1);
                }
                var tipo = new TipoFonte();
                ExtrairProps(paramsTexto, corpo, tipo, exigirCorpoParametros);
                mapa.TryAdd(nome, tipo);
            }
        }
        return mapa;
    }

    private static void ExtrairProps(string paramsTexto, string corpo, TipoFonte destino, bool webLado)
    {
        // parametros posicionais
        if (paramsTexto.Trim().Length > 0)
        {
            foreach (var chunk in DividirTopoLevel(paramsTexto))
            {
                var p = RemoverAtributos(chunk.Trim());
                if (p.Length == 0) continue;
                var temDefault = ContemIgualTopoLevel(p);
                var esquerda = TemSeparadorTopoLevel(p) ? p[..IndiceIgualTopoLevel(p)] : p;
                var tok = esquerda.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tok.Length < 2) continue;
                var nomeProp = tok[^1];
                var tipoBruto = tok[^2];
                if (!Regex.IsMatch(nomeProp, "^[A-Za-z_][A-Za-z0-9_]*$")) continue;
                if (tipoBruto.StartsWith("[")) continue;
                AdicionarProp(destino, nomeProp, tipoBruto, webLado && !temDefault);
            }
        }
        // propriedades de corpo com get; init/ (JSON-bound). Get-only puro (calculado na Web,
        // ex.: Produto360ViewModel.LucroBrutoUnitario) NAO entra no contrato — nunca vem do wire.
        foreach (Match m in Regex.Matches(corpo, @"public\s+([\w<>,\[\]?]+?)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get;\s*(init|set)\s*;?\s*\}"))
            AdicionarProp(destino, m.Groups[2].Value, m.Groups[1].Value, webLado);
    }

    private static void AdicionarProp(TipoFonte destino, string nome, string tipoBruto, bool requerido)
    {
        var limpo = tipoBruto.Trim().TrimEnd('?').Trim();
        destino.Todos.Add((nome, limpo));
        destino.TodosPorChave[nome] = (nome, limpo);
        if (requerido) destino.Requeridos.Add(nome);
    }

    private static IEnumerable<string> DividirTopoLevel(string s)
    {
        var d = 0; var cur = new System.Text.StringBuilder();
        foreach (var ch in s)
        {
            if (ch is '(' or '[' or '{' or '<') d++;
            else if (ch is ')' or ']' or '}' or '>') d--;
            if (ch == ',' && d == 0) { yield return cur.ToString(); cur.Clear(); }
            else cur.Append(ch);
        }
        if (cur.Length > 0) yield return cur.ToString();
    }

    private static string RemoverAtributos(string p)
    {
        while (p.StartsWith('['))
        {
            var d = 0;
            var achou = false;
            for (var i = 0; i < p.Length; i++)
            {
                if (p[i] == '[') d++;
                else if (p[i] == ']') { d--; if (d == 0) { p = p[(i + 1)..].TrimStart(); achou = true; break; } }
            }
            if (!achou) break; // abre sem fecha — nao e atributo; sai sem loop
        }
        return p;
    }

    private static bool ContemIgualTopoLevel(string s)
    {
        var d = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] is '(' or '[' or '{') d++;
            else if (s[i] is ')' or ']' or '}') d--;
            else if (s[i] == '=' && d == 0) return true;
        }
        return false;
    }

    private static bool TemSeparadorTopoLevel(string s) => ContemIgualTopoLevel(s);

    private static int IndiceIgualTopoLevel(string s)
    {
        var d = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] is '(' or '[' or '{') d++;
            else if (s[i] is ')' or ']' or '}') d--;
            else if (s[i] == '=' && d == 0) return i;
        }
        return s.Length;
    }

    private static int FindMatching(string txt, int abreIdx, char abre, char fecha)
    {
        var d = 0;
        for (var i = abreIdx; i < txt.Length; i++)
        {
            if (txt[i] == abre) d++;
            else if (txt[i] == fecha) { d--; if (d == 0) return i; }
        }
        return -1;
    }

    private static int SkipSpaces(string txt, int i)
    {
        while (i < txt.Length && char.IsWhiteSpace(txt[i])) i++;
        return i;
    }
}
