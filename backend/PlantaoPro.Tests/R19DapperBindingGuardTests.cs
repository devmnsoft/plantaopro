using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Operation360.WorkItems;
using PlantaoPro.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace PlantaoPro.Tests;

/// <summary>
/// R19 — auditoria da família F7b (rodada 18: WorkItemDto) + guarda global SRF-em-WHERE (F7c).
///
/// Mecanismo (probe versionado neste arquivo, executado com as mesmas versões de Dapper/Npgsql do
/// app): para uma coluna timestamptz o reader devolve System.DateTime e o Dapper NÃO converte:
///   - RECORD com parâmetro DateTimeOffset  → InvalidOperationException na construção do
///     deserializer ("A parameterless default constructor or one matching signature (System.DateTime
///     X) is required for &lt;tipo&gt; materialization") no momento em que a linha materializa;
///   - CLASSE com propriedade DateTimeOffset → materializa normalmente (o mapeamento de membros
///     faz a conversão); por isso a família ConferenceRow/PresenceRow/Medico* sobreviveu à auditoria.
/// Consequência: apenas records ligados ao Dapper com parâmetro DTOFF são defeito — e os fatos
/// de guarda abaixo travam esse conjunto (F7e) e a forma canônica das SRFs (F7c/F7d).
/// </summary>
public sealed class R19DapperBindingGuardTests
{
    private readonly ITestOutputHelper output;
    public R19DapperBindingGuardTests(ITestOutputHelper output) => this.output = output;

    private static readonly string Root = RepositoryPathResolver.RepoRoot;

    // ---------- mecanismos (versionam o comportamento exato das versões em uso) ----------

    private sealed class ProbeClassDtoff
    {
        public DateTimeOffset X { get; set; }
        public DateTimeOffset? Y { get; set; }
    }
    private sealed class ProbeClassDtoffNulo
    {
        public DateTimeOffset? X { get; set; }
    }
    private sealed record ProbeRecordDtoff(DateTimeOffset X);
    private sealed record ProbeRecordDtoffNulo(DateTimeOffset? X);
    private sealed record ProbeRecordControl(DateTime? X);

    [Fact]
    public async Task Mecanismo_RecordComParâmetroDateTimeOffset_FalhaNaMaterializacaoDeTimestamptz()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        const string sql = "select now() as \"X\"";

        var erroReq = await Roda(() => cn.QueryFirstOrDefaultAsync<ProbeRecordDtoff>(new CommandDefinition(sql)));
        var erroNulo = await Roda(() => cn.QueryFirstOrDefaultAsync<ProbeRecordDtoffNulo>(new CommandDefinition(sql)));

        Assert.Contains("parameterless default constructor", erroReq);
        Assert.Contains("(System.DateTime X)", erroReq);
        Assert.Equal(erroReq.Split(new[] { "(System.DateTime X)" }, StringSplitOptions.None)[0],
            erroNulo.Split(new[] { "(System.DateTime X)" }, StringSplitOptions.None)[0]);

        var ok = await Roda(() => cn.QueryFirstOrDefaultAsync<ProbeRecordControl>(new CommandDefinition(sql)));
        Assert.True(string.IsNullOrEmpty(ok), $"esperado sucesso (vazio=ok), veio: {ok}");
    }

    [Fact]
    public async Task Mecanismo_ClasseComPropriedadeDateTimeOffset_MaterializaNormalmente()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        const string sql = "select now() as \"X\", now() as \"Y\"";

        var req = await Roda(() => cn.QueryFirstOrDefaultAsync<ProbeClassDtoff>(new CommandDefinition(sql)));
        var nulo = await Roda(() => cn.QueryFirstOrDefaultAsync<ProbeClassDtoffNulo>(new CommandDefinition("select nullif('x','x')::timestamptz as \"X\"")));

        Assert.True(string.IsNullOrEmpty(req), $"esperado sucesso, veio: {req}");
        Assert.True(string.IsNullOrEmpty(nulo), $"esperado sucesso, veio: {nulo}");
    }

    private static async Task<string> Roda<T>(Func<Task<T?>> fn) where T : class
    {
        try { _ = await fn(); return string.Empty; }
        catch (Exception ex) { return $"{ex.GetType().Name}: {ex.Message}"; }
    }

    // ---------- guardas globais de fonte ----------

    private static IEnumerable<string> ArquivosBackend(bool incluirTestes)
    {
        // Arquivos *Tests.cs (mesmo fora do projeto de testes) contem trechos de SQL em assercao;
        // o contrato guardado se aplica a codigo de producao.
        var padrao = incluirTestes ? "\\bin\\|\\obj\\" : "\\bin\\|\\obj\\|\\PlantaoPro.Tests\\";
        return Directory.GetFiles(Path.Combine(Root, "backend"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(padrao))
            .Where(p => !p.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Guard_FamiliaF7b_NenhumRecordComParâmetroDateTimeOffsetÉLigadoAoDapper()
    {
        var registrosDtoff = new HashSet<string>(StringComparer.Ordinal);
        var tiposQueryAsync = new HashSet<string>(StringComparer.Ordinal);

        foreach (var arquivo in ArquivosBackend(incluirTestes: true))
        {
            var src = File.ReadAllText(arquivo);
            var regs = System.Text.RegularExpressions.Regex.Matches(src, @"public sealed record (\w+)\s*\(([^)]*)\)");
            for (var i = 0; i < regs.Count; i++)
            {
                if (regs[i].Groups[2].Value.Contains("DateTimeOffset", StringComparison.Ordinal))
                    registrosDtoff.Add(regs[i].Groups[1].Value);
            }
            var qs = System.Text.RegularExpressions.Regex.Matches(src, @"Query\w*Async<(\w+)>");
            for (var i = 0; i < qs.Count; i++)
                tiposQueryAsync.Add(qs[i].Groups[1].Value);
        }

        var violacao = registrosDtoff.Intersect(tiposQueryAsync).OrderBy(x => x).ToArray();
        Assert.True(violacao.Length == 0,
            "record com parâmetro DateTimeOffset ligado a Query*Async<T> (família F7b — troque para DateTime): "
            + string.Join(", ", violacao));
    }

    [Fact]
    public void Guard_SRF_NãoHáAnyPlantaoproNemUnnestDeSrfDoAppNoSqlDoBackend()
    {
        foreach (var arquivo in ArquivosBackend(incluirTestes: true))
        {
            var src = File.ReadAllText(arquivo);
            var rel = Path.GetRelativePath(Root, arquivo);
            Assert.DoesNotMatch(@"\bany\(\s*plantaopro\.", src);
            Assert.DoesNotMatch(@"unnest\(\s*plantaopro\.\w+\s*\(", src);
            _ = rel;
        }
    }

    [Fact]
    public void Guard_SRF_TodasChamadasAModulosEfetivosEstãoEmPosicaoFrom()
    {
        // A única SRF do app é plantaopro.modulos_efetivos (RETURNS TABLE — ver pg_proc).
        // Forma canônica (ADR-0002): posição FROM direta ou cross/left join lateral; em expressão
        // (WHERE/SELECT/ANY/UNNEST) o PG18 rejeita com 0A000 ou "funcao unnest(record) nao existe".
        var violacoes = new List<string>();
        foreach (var arquivo in ArquivosBackend(incluirTestes: false))
        {
            var src = File.ReadAllText(arquivo);
            var rel = Path.GetRelativePath(Root, arquivo);
            var chaves = System.Text.RegularExpressions.Regex.Matches(src, "modulos_efetivos\\s*\\(");
            for (var i = 0; i < chaves.Count; i++)
            {
                var m = chaves[i];
                var startLookback = Math.Max(0, m.Index - 200);
                var lookback = src.Substring(startLookback, m.Index - startLookback);
                var lineStart = startLookback + lookback.LastIndexOf('\n') + 1;
                var linha = lookback.Substring(lookback.LastIndexOf('\n') + 1);
                if (linha.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
                var cauda = linha.Substring(0, m.Index - lineStart).TrimEnd();
                // o match fica em "plantaopro.modulos_efetivos("; a chave de posicao vem antes do schema
                if (cauda.EndsWith("plantaopro.", StringComparison.OrdinalIgnoreCase))
                    cauda = cauda.Substring(0, cauda.Length - "plantaopro.".Length).TrimEnd();
                if (!cauda.EndsWith("from", StringComparison.OrdinalIgnoreCase)
                    && !cauda.EndsWith("join", StringComparison.OrdinalIgnoreCase)
                    && !cauda.EndsWith("lateral", StringComparison.OrdinalIgnoreCase))
                {
                    violacoes.Add($"{rel}: ...{cauda.Substring(Math.Max(0, cauda.Length - 60))} plantaopro.modulos_efetivos(");
                }
            }
        }
        Assert.True(violacoes.Count == 0, "modulos_efetivos fora de posição FROM (ver ADR-0002):\n" + string.Join("\n", violacoes));
    }

    [Fact]
    public void Guard_SRF_FormaFixadaDoCatalogoEmSaasServices_MantemCrossJoinLateral()
    {
        var src = File.ReadAllText(Path.Combine(Root, "backend", "PlantaoPro.Api", "SaasServices.cs"));
        Assert.Contains("cross join lateral plantaopro.modulos_efetivos(t.id) ef", src);
        Assert.Contains("where t.cliente_id=c.id and t.reg_status='A'", src);
        Assert.DoesNotContain("unnest(plantaopro.modulos_efetivos", src);
    }

    // ---------- regressão de banco (mesmo padrão r18b4: seed aleatório + método real/SQL da fonte) ----------

    [Fact]
    public async Task Db_HistoricoDeWorkItem_MaterializaLinhaRealPeloMetodoRealDoRepository()
    {
        var id = Guid.NewGuid();
        var item = Guid.NewGuid();
        var tenant = Guid.NewGuid(); // tenant aleatório: não conflita com dados existentes

        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(
            "insert into plantaopro.work_items(id,tenant_id,tipo,titulo,responsavel_id,posicao,vence_em,criado_por) values(@item,@tenant,'CONVITE_PENDENTE','Item anti-regressao R19',@resp,1,now() + interval '1 day',@resp)",
            new { item, tenant, resp = Guid.NewGuid() });
        await cn.ExecuteAsync(
            "insert into plantaopro.work_item_history(id,work_item_id,usuario_id,acao,origem,destino) values(@id,@item,@usuario,'MOVER','ENTRADA','EM_ANDAMENTO')",
            new { id, item, usuario = Guid.NewGuid() });

        try
        {
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
                .Build();
            var repo = new WorkItemRepository(cfg);

            var linhas = await repo.HistoryAsync(tenant, null, item, CancellationToken.None);

            var linha = Assert.Single(linhas);
            Assert.Equal(id, linha.Id);
            Assert.Equal("MOVER", linha.Acao);
            Assert.InRange(linha.CriadoEm, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.work_item_history where id=@id", new { id });
            await cn.ExecuteAsync("delete from plantaopro.work_items where id=@item", new { item });
        }
    }

    [Fact]
    public async Task Db_Ocorrencias_ListSqlDaFonte_MaterializaRecordComColunasDeData()
    {
        // Extrai o trecho real de OcorrenciaService.ListAsync da fonte (mesma técnica r18b4 F7b)
        // e executa contra um row semeado — trava a família F7e onde havia dado vivo em produção
        // (1 ocorrencia_operacional no banco operacional: a listagem voltava 500).
        var src = File.ReadAllText(Path.Combine(Root, "backend", "PlantaoPro.Api", "OcorrenciaService.cs"));
        var inicio = src.IndexOf("select id as \"\"Id\"\"", StringComparison.Ordinal);
        Assert.True(inicio >= 0, "trecho do SELECT da listagem de ocorrencias nao localizado na fonte");
        var fim = src.IndexOf(" from plantaopro.ocorrencias_operacionais o where ", inicio, StringComparison.Ordinal);
        Assert.True(fim > inicio, "fim do trecho do SELECT nao localizado");
        var colunas = src.Substring(inicio, fim - inicio).Replace("\"\"", "\"");
        Assert.Contains("prazo_resolucao as \"PrazoResolucao\"", colunas);
        Assert.Contains("criado_em as \"CriadoEm\"", colunas);

        var id = Guid.NewGuid();
        var tenant = Guid.NewGuid();
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync(
            "insert into plantaopro.ocorrencias_operacionais(id,tenant_id,unidade_id,titulo,descricao,categoria,prioridade,situacao,solicitante_id,responsavel_id,prazo_resolucao) values(@id,@tenant,@unidade,'Ocorrencia anti-regressao R19','Descricao minima para o teste de binding R19.','ATRASO','ALTA','ABERTA',@solicitante,@resp,now() + interval '2 hours')",
            new { id, tenant, unidade = Guid.NewGuid(), solicitante = Guid.NewGuid(), resp = Guid.NewGuid() });
        await cn.ExecuteAsync(
            "insert into plantaopro.ocorrencia_eventos(id,tenant_id,ocorrencia_id,tipo,autor_id,descricao) values(@ev,@tenant,@id,'CRIACAO',@autor,'Evento criado para o teste de binding R19.')",
            new { ev = Guid.NewGuid(), tenant, id, autor = Guid.NewGuid() });

        try
        {
            var itens = (await cn.QueryAsync<OcorrenciaDto>(
                new CommandDefinition(colunas + " from plantaopro.ocorrencias_operacionais o where o.tenant_id=@tenantId limit 5",
                    new { tenantId = tenant }))).AsList();
            var item = Assert.Single(itens);
            Assert.Equal(id, item.Id);
            Assert.NotNull(item.PrazoResolucao);
            Assert.InRange(item.CriadoEm, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
            Assert.InRange(item.AtualizadoEm, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));

            var eventos = (await cn.QueryAsync<OcorrenciaEventoDto>(
                new CommandDefinition("select id as \"Id\",tipo as \"Tipo\",autor_id as \"AutorId\",descricao as \"Descricao\",visivel_solicitante as \"VisivelSolicitante\",criado_em as \"CriadoEm\" from plantaopro.ocorrencia_eventos where tenant_id=@tenantId and ocorrencia_id=@id limit 5",
                    new { tenantId = tenant, id }))).AsList();
            Assert.Single(eventos);
            Assert.InRange(eventos[0].CriadoEm, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        }
        finally
        {
            await cn.ExecuteAsync("delete from plantaopro.ocorrencia_eventos where ocorrencia_id=@id", new { id });
            await cn.ExecuteAsync("delete from plantaopro.ocorrencias_operacionais where id=@id", new { id });
        }
    }

    [Fact]
    public async Task Db_CentralClientes_SqlCompletaDaFonte_MaterializaItensETotais()
    {
        // Extrai as duas statements exatas de ClienteService.ListCentralAsync (mesma tecnica r18b4)
        // e executa contra o schema real: trava os dois estagios do F7d — o label via cross join
        // lateral e o totais (sum() de contagem e numeric no PG; cast ::bigint para o record de longs).
        var src = File.ReadAllText(Path.Combine(Root, "backend", "PlantaoPro.Api", "SaasServices.cs"));
        var filtro = ExtrairVerbatim(src, "const string filtered = @\"");
        var itens = ExtrairVerbatim(src, "var sql = @\"");
        var totais = ExtrairVerbatim(src, "+ filtered + @\"");
        var sql = itens + filtro + totais + filtro + ";"; // a fonte concatena filtered uma vez por statement
        Assert.Contains("cross join lateral plantaopro.modulos_efetivos(t.id) ef", sql);
        Assert.Contains("::bigint as \"UsuariosAtivos\"", sql);
        Assert.Contains("::bigint as \"Pendencias\"", sql);

        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        using var multi = await cn.QueryMultipleAsync(sql, new { search = (string?)null, status = (string?)null, take = 100, skip = 0 });
        var itensLidos = (await multi.ReadAsync<PlantaoPro.Api.Models.ClienteCentralDto>()).AsList();
        var totaisLidos = await multi.ReadSingleAsync<PlantaoPro.Api.Models.ClienteCentralTotais>();

        Assert.NotNull(itensLidos);
        Assert.True(itensLidos.Count <= 100, "pagina maior que take");
        Assert.True(totaisLidos.Clientes >= itensLidos.Count, "totais inferior a pagina lida");
        Assert.True(itensLidos.Any(i => !string.IsNullOrEmpty(i.ModulosVigentes)),
            "nenhum item com label de modulos vigentes — a seed demo deveria expor modulos efetivos via SRF");
    }

    private static string ExtrairVerbatim(string src, string marcador)
    {
        var i = src.IndexOf(marcador, StringComparison.Ordinal);
        Assert.True(i >= 0, "marcador nao encontrado na fonte: " + marcador);
        var j = i + marcador.Length; // char apos a aspa inicial do literal verbatim
        var k = j;
        while (k < src.Length)
        {
            if (src[k] == '"')
            {
                if (k + 1 < src.Length && src[k + 1] == '"') { k += 2; continue; } // "" escapada
                break;
            }
            k++;
        }
        return src.Substring(j, k - j).Replace("\"\"", "\"");
    }

    [Fact]
    public async Task Db_FavoritosProdutoividade_InsertReturning_MaterializaRecordComData()
    {
        var tenant = Guid.NewGuid();
        var user = Guid.NewGuid();

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
            .Build();
        var repo = new PlantaoPro.Api.Operation360.Productivity.ProductivityRepository(cfg);

        var salvo = await repo.AddFavoriteAsync(tenant, user,
            new PlantaoPro.Api.Operation360.Productivity.FavoriteRequest("ESCALA", Guid.NewGuid(), "Escala anti-regressao R19", "/Escalas"), CancellationToken.None);

        try
        {
            Assert.InRange(salvo.Data, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
            var lista = await repo.FavoritesAsync(tenant, user, CancellationToken.None);
            Assert.Single(lista);
        }
        finally
        {
            await repo.DeleteFavoriteAsync(salvo.Id, tenant, user, CancellationToken.None);
        }
    }
}
