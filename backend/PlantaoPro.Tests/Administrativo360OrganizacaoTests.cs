using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// Regressão da gestão organizacional (Bloco do materialização Dapper + validações de negócio
/// em PT-BR + regras de contrato + isolamento multi-tenant + paginação/filtros).
/// Roda contra o PostgreSQL local (plantaopro_test) através da API real (WebApplicationFactory).
/// Cada teste usa códigos/matrículas únicos por execução e limpa o que cria no finally.
/// </summary>
public sealed class Administrativo360OrganizacaoTests : IClassFixture<PlantaoProApiFactory>
{
    private readonly PlantaoProApiFactory _factory;
    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid TenantIsolado = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647599");
    private const string GestorEmail = "gestor@santacasa-demo.example";
    private const string GestorSenha = "SantaCasa!Demo2026#Gestor";

    public Administrativo360OrganizacaoTests(PlantaoProApiFactory factory) => _factory = factory;

    // =========================================================================
    // Infra de teste
    // =========================================================================
    private static string ObterConnectionString() => TestDatabase.ConnectionString;

    private static string Sufixo() => Guid.NewGuid().ToString("N")[..8];
    private static string CpfValido() => "1" + Random.Shared.NextInt64(1_000_000_000L, 9_999_999_999L).ToString("D10");
    private static NpgsqlConnection ConexaoDb() => new(ObterConnectionString());

    private async Task<string> TokenGestorAsync()
    {
        using var client = _factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("api/auth/login", new { Email = GestorEmail, Senha = GestorSenha });
        resposta.EnsureSuccessStatusCode();
        var json = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("data").GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token), "Login do gestor não retornou token.");
        return token!;
    }

    private async Task<HttpClient> ClienteAutenticadoAsync()
    {
        var token = await TokenGestorAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<JsonElement> LerCorpoAsync(HttpResponseMessage r)
    {
        var texto = await r.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(texto) ? default : JsonSerializer.Deserialize<JsonElement>(texto);
    }

    /// <summary>Mensagem amigável de erro de negócio ({"message": "..."}) ou o corpo bruto.</summary>
    private static async Task<string> MensagemErroAsync(HttpResponseMessage r)
    {
        var texto = await r.Content.ReadAsStringAsync();
        try
        {
            var json = JsonSerializer.Deserialize<JsonElement>(texto);
            if (json.ValueKind == JsonValueKind.Object && json.TryGetProperty("message", out var msg))
                return msg.ToString();
        }
        catch (JsonException) { /* corpo não-JSON: devolve o bruto */ }
        return texto;
    }

    private static async Task AssertMensagemNegocioAsync(HttpResponseMessage r, string contexto)
    {
        var corpo = await MensagemErroAsync(r);
        Assert.True(r.StatusCode == HttpStatusCode.BadRequest, $"{contexto} — status esperado 400, obtido {(int)r.StatusCode}; corpo: {corpo}");
    }

    // ---------- Criação via API (asserts sucesso inline) ----------
    private async Task<JsonElement> CriarDepartamentoAsync(HttpClient c, string codigo, string nome)
    {
        var r = await c.PostAsJsonAsync("api/administrativo360/departamentos", new { codigo, nome });
        Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, $"Cadastrar departamento: {(int)r.StatusCode} {await MensagemErroAsync(r)}");
        return await LerCorpoAsync(r);
    }

    private async Task<JsonElement> CriarCargoAsync(HttpClient c, string codigo, string nome, Guid? departamentoId)
    {
        var r = await c.PostAsJsonAsync("api/administrativo360/cargos", new { codigo, nome, departamentoId });
        Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, $"Cadastrar cargo: {(int)r.StatusCode} {await MensagemErroAsync(r)}");
        return await LerCorpoAsync(r);
    }

    private async Task<JsonElement> CriarColaboradorAsync(HttpClient c, string matricula, string nome, string cpf, string email, Guid cargoId)
    {
        var r = await c.PostAsJsonAsync("api/administrativo360/colaboradores", new { matricula, nome, cpf, email, cargoId });
        Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, $"Cadastrar colaborador: {(int)r.StatusCode} {await MensagemErroAsync(r)}");
        return await LerCorpoAsync(r);
    }

    private static Task<HttpResponseMessage> ContratarBrutoAsync(HttpClient c, object payload) =>
        c.PostAsJsonAsync("api/administrativo360/contratos", payload);

    private static async Task<(long Departamentos, long Cargos, long ColaboradoresAtivos, long ContratosVigentes)> ResumoAsync(HttpClient c)
    {
        var r = await c.GetAsync("api/administrativo360/resumo");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await LerCorpoAsync(r);
        return (j.GetProperty("departamentos").GetInt64(),
                j.GetProperty("cargos").GetInt64(),
                j.GetProperty("colaboradoresAtivos").GetInt64(),
                j.GetProperty("contratosVigentes").GetInt64());
    }

    // ---------- Inserts diretos (dados do tenant B para isolamento) ----------
    private sealed record DepartamentoIntegridade(string Nome, string RegStatus);

    private async Task<Guid> InsertDepartamentoBAsync(Guid tenant, string codigo, string nome)
    {
        await using var cn = ConexaoDb();
        return await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.adm_departamentos(tenant_id,codigo,nome) values(@tenant,@codigo,@nome) returning id", new { tenant, codigo, nome });
    }

    private async Task<Guid> InsertCargoBAsync(Guid tenant, string codigo, string nome, Guid departamentoId)
    {
        await using var cn = ConexaoDb();
        return await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.adm_cargos(tenant_id,codigo,nome,departamento_id) values(@tenant,@codigo,@nome,@dep) returning id", new { tenant, codigo, nome, dep = departamentoId });
    }

    private async Task<Guid> InsertColaboradorBAsync(Guid tenant, string matricula, string nome, string cpf, Guid cargoId)
    {
        await using var cn = ConexaoDb();
        return await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.adm_colaboradores(tenant_id,matricula,nome,cpf,email,cargo_id) values(@tenant,@matricula,@nome,@cpf,@email,@cargo) returning id", new { tenant, matricula, nome, cpf, email = $"{matricula.ToLowerInvariant()}@isolado.example", cargo = cargoId });
    }

    private async Task<Guid> InsertContratoBAsync(Guid tenant, Guid colaboradorId, DateOnly inicio, decimal salario)
    {
        await using var cn = ConexaoDb();
        // DateOnly vira DateTime à meia-noite: o Dapper não mapeia o tipo nativo em parâmetros.
        return await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.adm_contratos_trabalho(tenant_id,colaborador_id,tipo,inicio,salario,carga_horaria_semanal) values(@tenant,@colab,'CLT',@inicio,@salario,40) returning id", new { tenant, colab = colaboradorId, inicio = inicio.ToDateTime(TimeOnly.MinValue), salario });
    }

    /// <summary>Remove os registros criados (ordem reversa das FKs). Ignora ids vazios.</summary>
    private async Task LimparAsync(IEnumerable<Guid> contratos, IEnumerable<Guid> colaboradores, IEnumerable<Guid> cargos, IEnumerable<Guid> departamentos)
    {
        await using var cn = ConexaoDb();
        var cts = contratos.Where(g => g != Guid.Empty).ToArray();
        var cls = colaboradores.Where(g => g != Guid.Empty).ToArray();
        var cgs = cargos.Where(g => g != Guid.Empty).ToArray();
        var dps = departamentos.Where(g => g != Guid.Empty).ToArray();
        if (cts.Length > 0) await cn.ExecuteAsync("delete from plantaopro.adm_contratos_trabalho where id = any(@ids)", new { ids = cts });
        if (cls.Length > 0) await cn.ExecuteAsync("delete from plantaopro.adm_colaboradores where id = any(@ids)", new { ids = cls });
        if (cgs.Length > 0) await cn.ExecuteAsync("delete from plantaopro.adm_cargos where id = any(@ids)", new { ids = cgs });
        if (dps.Length > 0) await cn.ExecuteAsync("delete from plantaopro.adm_departamentos where id = any(@ids)", new { ids = dps });
    }

    // =========================================================================
    // 1. MATERIALIZAÇÃO (COUNT(*) bigint -> long; carga smallint -> int ::integer)
    // =========================================================================

    [Fact]
    public async Task Resumo_MaterializaContadoresSemErroERefleteCadastrosNovos()
    {
        var c = await ClienteAutenticadoAsync();
        var antes = await ResumoAsync(c);

        var suffixo = Sufixo();
        var dep = Guid.Empty; var cargo = Guid.Empty; var colab = Guid.Empty; var contrato = Guid.Empty;
        try
        {
            var depEl = await CriarDepartamentoAsync(c, $"RZ{suffixo}", "Regressão Departamento");
            dep = depEl.GetProperty("id").GetGuid();
            var cargoEl = await CriarCargoAsync(c, $"RC{suffixo}", "Regressão Cargo", dep);
            cargo = cargoEl.GetProperty("id").GetGuid();
            var colabEl = await CriarColaboradorAsync(c, $"M{suffixo}", "Regressão Colab", CpfValido(), $"regressao{suffixo}@exemplo.com.br", cargo);
            colab = colabEl.GetProperty("id").GetGuid();

            var rCt = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "CLT", inicio = DateOnly.FromDateTime(DateTime.Today), fim = (DateOnly?)null, salario = 1500m, cargaHorariaSemanal = 40 });
            Assert.True(rCt.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, await MensagemErroAsync(rCt));
            contrato = (await LerCorpoAsync(rCt)).GetProperty("id").GetGuid();

            var depois = await ResumoAsync(c);
            Assert.Equal(antes.Departamentos + 1, depois.Departamentos);
            Assert.Equal(antes.Cargos + 1, depois.Cargos);
            Assert.Equal(antes.ColaboradoresAtivos + 1, depois.ColaboradoresAtivos);
            Assert.Equal(antes.ContratosVigentes + 1, depois.ContratosVigentes);
        }
        finally { await LimparAsync(new[] { contrato }, new[] { colab }, new[] { cargo }, new[] { dep }); }
    }

    // =========================================================================
    // 2. PAGINAÇÃO / FILTROS NO BANCO (envelope {items,page,pageSize,total,totalPages})
    // =========================================================================

    [Fact]
    public async Task Departamentos_PaginacaoEnvelopeBuscaETamanhoNormalizado()
    {
        var c = await ClienteAutenticadoAsync();
        var suffixo = Sufixo();
        var ids = new List<Guid>();
        try
        {
            for (var i = 1; i <= 3; i++)
            {
                var d = await CriarDepartamentoAsync(c, $"REG{suffixo}{i}", $"Pág Dep {i}");
                ids.Add(d.GetProperty("id").GetGuid());
            }
            var dFora = await CriarDepartamentoAsync(c, $"OUT{suffixo}", "Fora da Busca");
            ids.Add(dFora.GetProperty("id").GetGuid());

            // Busca exata retorna só os 3 criados, com envelope completo.
            var rBusca = await c.GetAsync($"api/administrativo360/departamentos?busca=REG{suffixo}");
            Assert.True(rBusca.IsSuccessStatusCode);
            var jBusca = await LerCorpoAsync(rBusca);
            Assert.Equal(3, jBusca.GetProperty("total").GetInt64());
            Assert.Equal(3, jBusca.GetProperty("items").GetArrayLength());
            Assert.Equal(1, jBusca.GetProperty("page").GetInt32());
            Assert.Equal(50, jBusca.GetProperty("pageSize").GetInt32());
            Assert.Equal(1, jBusca.GetProperty("totalPages").GetInt32());

            // Página além do fim: lista vazia, contagens consistentes.
            var rFora = await LerCorpoAsync(await c.GetAsync("api/administrativo360/departamentos?pagina=999&tamanho=2"));
            var totalGeral = rFora.GetProperty("total").GetInt64();
            Assert.True(totalGeral >= 4, $"Esperado ao menos os 4 cadastros deste teste, obtido {totalGeral}.");
            Assert.Equal(0, rFora.GetProperty("items").GetArrayLength());
            Assert.Equal((int)Math.Ceiling(totalGeral / 2.0), rFora.GetProperty("totalPages").GetInt32());

            // tamanho acima do teto é normalizado para 100.
            var rGrande = await LerCorpoAsync(await c.GetAsync("api/administrativo360/departamentos?tamanho=5000"));
            Assert.Equal(100, rGrande.GetProperty("pageSize").GetInt32());
            Assert.True(rGrande.GetProperty("items").GetArrayLength() <= 100);

            // Páginas consecutivas de 2 em 2 não se sobrepõem.
            var p1 = await LerCorpoAsync(await c.GetAsync("api/administrativo360/departamentos?pagina=1&tamanho=2"));
            var p2 = await LerCorpoAsync(await c.GetAsync("api/administrativo360/departamentos?pagina=2&tamanho=2"));
            Assert.Equal(2, p1.GetProperty("items").GetArrayLength());
            if (p2.GetProperty("items").GetArrayLength() > 0)
            {
                var primeiroDaPagina1 = p1.GetProperty("items")[0].GetProperty("id").GetString();
                var idsPagina2 = p2.GetProperty("items").EnumerateArray().Select(e => e.GetProperty("id").GetString()).ToList();
                Assert.DoesNotContain(primeiroDaPagina1, idsPagina2);
            }
        }
        finally { await LimparAsync(Array.Empty<Guid>(), Array.Empty<Guid>(), Array.Empty<Guid>(), ids.ToArray()); }
    }

    // =========================================================================
    // 2B. INATIVOS CONTINUAM VISÍVEIS NA LISTA (reativação pela interface)
    //     e somenteAtivos=true exclui os inativos dos seletores de novos cadastros
    // =========================================================================

    [Fact]
    public async Task Departamentos_InativoPermaneceVisivelNaLista_E_SomenteAtivos_ExcluiOsInativos()
    {
        var c = await ClienteAutenticadoAsync();
        var suffixo = Sufixo();
        var dep = Guid.Empty;
        try
        {
            var d = await CriarDepartamentoAsync(c, $"INAC{suffixo}", "Dep Inativo Visivel");
            dep = d.GetProperty("id").GetGuid();

            Assert.True((await c.PostAsync($"api/administrativo360/departamentos/{dep}/inativar", new StringContent(string.Empty))).IsSuccessStatusCode);

            // A lista padrão continua exibindo o inativo (com ativo=false): é por ela que a interface permite reativar.
            var rLista = await LerCorpoAsync(await c.GetAsync($"api/administrativo360/departamentos?busca=INAC{suffixo}"));
            Assert.Equal(1, rLista.GetProperty("total").GetInt64());
            Assert.False(rLista.GetProperty("items")[0].GetProperty("ativo").GetBoolean());

            // O seletor de novos cadastros (somenteAtivos=true) exclui o inativo.
            var rSel = await LerCorpoAsync(await c.GetAsync($"api/administrativo360/departamentos?busca=INAC{suffixo}&somenteAtivos=true"));
            Assert.Equal(0, rSel.GetProperty("total").GetInt64());
            Assert.Equal(0, rSel.GetProperty("items").GetArrayLength());

            // Reativado, o departamento volta à lista como ativo e entra de novo no seletor.
            Assert.True((await c.PostAsync($"api/administrativo360/departamentos/{dep}/ativar", new StringContent(string.Empty))).IsSuccessStatusCode);
            var rLista2 = await LerCorpoAsync(await c.GetAsync($"api/administrativo360/departamentos?busca=INAC{suffixo}"));
            Assert.True(rLista2.GetProperty("items")[0].GetProperty("ativo").GetBoolean());
            var rSel2 = await LerCorpoAsync(await c.GetAsync($"api/administrativo360/departamentos?busca=INAC{suffixo}&somenteAtivos=true"));
            Assert.Equal(1, rSel2.GetProperty("total").GetInt64());
        }
        finally { await LimparAsync(Array.Empty<Guid>(), Array.Empty<Guid>(), Array.Empty<Guid>(), new[] { dep }); }
    }

    // =========================================================================
    // 3. VALIDAÇÕES DE NEGÓCIO — DEPARTAMENTOS (PT-BR, sem "Sequence contains no elements")
    // =========================================================================

    [Fact]
    public async Task Departamentos_DuplicidadeInativacaoEInexistencia_MensagensEmPortugues()
    {
        var c = await ClienteAutenticadoAsync();
        var suffixo = Sufixo();
        var idX = Guid.Empty; var idY = Guid.Empty;
        try
        {
            var x = await CriarDepartamentoAsync(c, $"DUP{suffixo}", "Duplicado A");
            idX = x.GetProperty("id").GetGuid();
            Assert.True(x.GetProperty("ativo").GetBoolean());

            // Código duplicado ativo (maiúscula/minúscula é irrelevante: normalização lower()).
            var rDup = await c.PostAsJsonAsync("api/administrativo360/departamentos", new { codigo = $"dup{suffixo}", nome = "Duplicado B" });
            await AssertMensagemNegocioAsync(rDup, $"duplicidade do código: ");
            Assert.Contains("Já existe um departamento ativo com este código.", await MensagemErroAsync(rDup));

            // Atualizar registro inexistente.
            var rPut = await c.PutAsJsonAsync($"api/administrativo360/departamentos/{Guid.NewGuid()}", new { codigo = $"NAO{suffixo}", nome = "Não existe" });
            await AssertMensagemNegocioAsync(rPut, "PUT inexistente: ");
            Assert.Contains("Departamento não encontrado ou já inativo.", await MensagemErroAsync(rPut));

            // Inativar libera o código para um novo departamento ativo.
            Assert.True((await c.PostAsync($"api/administrativo360/departamentos/{idX}/inativar", new StringContent(string.Empty))).IsSuccessStatusCode);
            var y = await CriarDepartamentoAsync(c, $"DUP{suffixo}", "Duplicado Reutilizado");
            idY = y.GetProperty("id").GetGuid();
            Assert.True(y.GetProperty("ativo").GetBoolean());
            Assert.NotEqual(idX, idY);

            // Transições repetidas são recusadas com mensagem clara.
            Assert.True((await c.PostAsync($"api/administrativo360/departamentos/{idY}/inativar", new StringContent(string.Empty))).IsSuccessStatusCode);
            var rInat2 = await c.PostAsync($"api/administrativo360/departamentos/{idY}/inativar", new StringContent(string.Empty));
            await AssertMensagemNegocioAsync(rInat2, "inativar 2x: ");
            Assert.Contains("não encontrado ou já inativo", await MensagemErroAsync(rInat2));

            Assert.True((await c.PostAsync($"api/administrativo360/departamentos/{idY}/ativar", new StringContent(string.Empty))).IsSuccessStatusCode);
            var rAtv2 = await c.PostAsync($"api/administrativo360/departamentos/{idY}/ativar", new StringContent(string.Empty));
            await AssertMensagemNegocioAsync(rAtv2, "ativar 2x: ");
            Assert.Contains("já ativo", await MensagemErroAsync(rAtv2));
        }
        finally { await LimparAsync(Array.Empty<Guid>(), Array.Empty<Guid>(), Array.Empty<Guid>(), new[] { idX, idY }); }
    }

    // =========================================================================
    // 4. VALIDAÇÕES DE NEGÓCIO — CARGOS
    // =========================================================================

    [Fact]
    public async Task Cargos_DepartamentoInexistenteOuInativoRejeitadoComMensagemAmigavel()
    {
        var c = await ClienteAutenticadoAsync();
        var suffixo = Sufixo();
        var depId = Guid.Empty; var cargoOk = Guid.Empty; var cargoSemDep = Guid.Empty;
        try
        {
            // Departamento inexistente (GUID aleatório) nunca vira erro de materialização.
            var rAle = await c.PostAsJsonAsync("api/administrativo360/cargos", new { codigo = $"CG{suffixo}A", nome = "Cargo Dept Aleatório", departamentoId = Guid.NewGuid().ToString() });
            await AssertMensagemNegocioAsync(rAle, "cargo com dept aleatório: ");
            Assert.Contains("Departamento inexistente, inativo ou pertencente a outra organização.", await MensagemErroAsync(rAle));

            var dep = await CriarDepartamentoAsync(c, $"DEP{suffixo}", "Dept dos Cargos");
            depId = dep.GetProperty("id").GetGuid();

            var okEl = await CriarCargoAsync(c, $"CG{suffixo}B", "Cargo Válido", depId);
            cargoOk = okEl.GetProperty("id").GetGuid();
            Assert.Equal(depId, okEl.GetProperty("departamentoId").GetGuid());
            Assert.NotNull(okEl.GetProperty("departamento").GetString());

            // J12 WP-A3: atualização bem-sucedida precisa materializar no MESMO formato
            // da criação (6 colunas incl. nome do departamento via join). Sem isso o
            // Dapper lançava InvalidOperationException e o PUT respondia 500.
            var rUpd = await c.PutAsJsonAsync($"api/administrativo360/cargos/{cargoOk}", new { codigo = $"CG{suffixo}B2", nome = "Cargo Válido Editado", departamentoId = depId.ToString() });
            Assert.True(rUpd.IsSuccessStatusCode, $"PUT de cargo válido deve retornar sucesso: {(int)rUpd.StatusCode} {await MensagemErroAsync(rUpd)}");
            var updEl = await LerCorpoAsync(rUpd);
            // O serviço normaliza codigo com upper(trim(...)) — a comparação segue o mesmo contrato.
            Assert.Equal($"CG{suffixo}B2", updEl.GetProperty("codigo").GetString(), ignoreCase: true);
            Assert.Equal("Cargo Válido Editado", updEl.GetProperty("nome").GetString());
            Assert.NotNull(updEl.GetProperty("departamento").GetString());

            // Código duplicado ativo (mesmo departamento) é recusado — o código já é B2 após o PUT acima.
            var rDup = await c.PostAsJsonAsync("api/administrativo360/cargos", new { codigo = $"cg{suffixo}b2", nome = "Cargo Dup", departamentoId = depId });
            await AssertMensagemNegocioAsync(rDup, "código duplicado: ");
            Assert.Contains("Já existe um cargo ativo com este código.", await MensagemErroAsync(rDup));

            // Sem departamento é permitido na API (nulo explícito).
            var semDep = await CriarCargoAsync(c, $"CG{suffixo}C", "Cargo Sem Dept", null);
            cargoSemDep = semDep.GetProperty("id").GetGuid();

            // Departamento inativo rejeita novo cargo vinculado.
            Assert.True((await c.PostAsync($"api/administrativo360/departamentos/{depId}/inativar", new StringContent(string.Empty))).IsSuccessStatusCode);
            var rCg = await c.PostAsJsonAsync("api/administrativo360/cargos", new { codigo = $"CG{suffixo}D", nome = "Cargo Com Dept Inativo", departamentoId = depId });
            await AssertMensagemNegocioAsync(rCg, "dept inativo: ");
            Assert.Contains("Departamento inexistente, inativo ou pertencente a outra organização.", await MensagemErroAsync(rCg));

            // Atualizar cargo inexistente.
            var rPut = await c.PutAsJsonAsync($"api/administrativo360/cargos/{Guid.NewGuid()}", new { codigo = $"NAO{suffixo}", nome = "Não existe", departamentoId = (object?)null });
            await AssertMensagemNegocioAsync(rPut, "PUT inexistente: ");
            Assert.Contains("Cargo não encontrado ou já inativo.", await MensagemErroAsync(rPut));
        }
        finally { await LimparAsync(Array.Empty<Guid>(), Array.Empty<Guid>(), new[] { cargoOk, cargoSemDep }, new[] { depId }); }
    }

    // =========================================================================
    // 5. VALIDAÇÕES DE NEGÓCIO — COLABORADORES
    // =========================================================================

    [Fact]
    public async Task Colaboradores_CargoInativoMatriculaECpfDuplicadosRejeitadosComMensagensAmigaveis()
    {
        var c = await ClienteAutenticadoAsync();
        var suffixo = Sufixo();
        var depId = Guid.Empty; var cargoA = Guid.Empty; var cargoB = Guid.Empty; var colab = Guid.Empty;
        try
        {
            var dep = await CriarDepartamentoAsync(c, $"DEP{suffixo}", "Dept Colabs");
            depId = dep.GetProperty("id").GetGuid();
            var a = await CriarCargoAsync(c, $"CA{suffixo}", "Cargo A", depId);
            cargoA = a.GetProperty("id").GetGuid();
            var b = await CriarCargoAsync(c, $"CB{suffixo}", "Cargo B", depId);
            cargoB = b.GetProperty("id").GetGuid();

            var cpf1 = CpfValido();
            var el = await CriarColaboradorAsync(c, $"M{suffixo}", "Colab Base", cpf1, $"colab{suffixo}@exemplo.com.br", cargoA);
            colab = el.GetProperty("id").GetGuid();
            Assert.Equal("ATIVO", el.GetProperty("status").GetString());

            // Matrícula duplicada (outro CPF).
            var rMat = await c.PostAsJsonAsync("api/administrativo360/colaboradores", new { matricula = $"m{suffixo}", nome = "Colab Dup Matricula", cpf = CpfValido(), email = $"dm{suffixo}@exemplo.com.br", cargoId = cargoA });
            await AssertMensagemNegocioAsync(rMat, "matrícula duplicada: ");
            Assert.Contains("Já existe colaborador ativo com esta matrícula.", await MensagemErroAsync(rMat));

            // CPF duplicado (outra matrícula).
            var rCpf = await c.PostAsJsonAsync("api/administrativo360/colaboradores", new { matricula = $"N{suffixo}", nome = "Colab Dup Cpf", cpf = cpf1, email = $"dc{suffixo}@exemplo.com.br", cargoId = cargoA });
            await AssertMensagemNegocioAsync(rCpf, "CPF duplicado: ");
            Assert.Contains("Já existe colaborador ativo com este CPF.", await MensagemErroAsync(rCpf));

            // Cargo inexistente (GUID aleatório).
            var rAle = await c.PostAsJsonAsync("api/administrativo360/colaboradores", new { matricula = $"X{suffixo}", nome = "Colab Cargo Aleatório", cpf = CpfValido(), email = $"xa{suffixo}@exemplo.com.br", cargoId = Guid.NewGuid().ToString() });
            await AssertMensagemNegocioAsync(rAle, "cargo aleatório: ");
            Assert.Contains("Cargo inexistente ou inativo.", await MensagemErroAsync(rAle));

            // Cargo inativo bloqueia nova vinculação.
            Assert.True((await c.PostAsync($"api/administrativo360/cargos/{cargoB}/inativar", new StringContent(string.Empty))).IsSuccessStatusCode);
            var rInat = await c.PostAsJsonAsync("api/administrativo360/colaboradores", new { matricula = $"Y{suffixo}", nome = "Colab Cargo Inativo", cpf = CpfValido(), email = $"ci{suffixo}@exemplo.com.br", cargoId = cargoB });
            await AssertMensagemNegocioAsync(rInat, "cargo inativo: ");
            Assert.Contains("Cadastre ou ative o cargo antes de vincular o colaborador.", await MensagemErroAsync(rInat));

            // Edição de contato de colaborador inexistente.
            var rPut = await c.PutAsJsonAsync($"api/administrativo360/colaboradores/{Guid.NewGuid()}", new { nome = "Não Existe", email = "naoexiste@example.com" });
            await AssertMensagemNegocioAsync(rPut, "PUT inexistente: ");
            Assert.Contains("Colaborador não encontrado ou já inativo.", await MensagemErroAsync(rPut));
        }
        finally { await LimparAsync(Array.Empty<Guid>(), new[] { colab }, new[] { cargoA, cargoB }, new[] { depId }); }
    }

    // =========================================================================
    // 6. TRANSIÇÕES DE STATUS DO COLABORADOR (afastar/desligar/reativar + filtros)
    // =========================================================================

    [Fact]
    public async Task Colaboradores_TransicaoDeStatusFiltraListaBloqueiaContratacaoELiberaAposReativacao()
    {
        var c = await ClienteAutenticadoAsync();
        var suffixo = Sufixo();
        var depId = Guid.Empty; var cargo = Guid.Empty; var colab = Guid.Empty; var contrato = Guid.Empty;
        try
        {
            var dep = await CriarDepartamentoAsync(c, $"DEP{suffixo}", "Dept Status");
            depId = dep.GetProperty("id").GetGuid();
            var cg = await CriarCargoAsync(c, $"CS{suffixo}", "Cargo Status", depId);
            cargo = cg.GetProperty("id").GetGuid();
            var el = await CriarColaboradorAsync(c, $"MS{suffixo}", "Colab Status", CpfValido(), $"st{suffixo}@exemplo.com.br", cargo);
            colab = el.GetProperty("id").GetGuid();
            var matricula = $"MS{suffixo}";
            var hoje = DateOnly.FromDateTime(DateTime.Today);

            // Afastado: visível só no filtro AFASTADO e fora do ATIVO; contratação bloqueada.
            var rAf = await c.PostAsJsonAsync($"api/administrativo360/colaboradores/{colab}/status", new { status = "AFASTADO" });
            Assert.True(rAf.IsSuccessStatusCode, await MensagemErroAsync(rAf));

            var rListaAfa = await LerCorpoAsync(await c.GetAsync($"api/administrativo360/colaboradores?status=AFASTADO&busca={matricula}"));
            Assert.Equal(1, rListaAfa.GetProperty("total").GetInt64());
            Assert.Equal("AFASTADO", rListaAfa.GetProperty("items")[0].GetProperty("status").GetString());

            var rListaAt = await LerCorpoAsync(await c.GetAsync($"api/administrativo360/colaboradores?status=ATIVO&busca={matricula}"));
            Assert.Equal(0, rListaAt.GetProperty("total").GetInt64());

            var rCtAfa = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "CLT", inicio = hoje, fim = (DateOnly?)null, salario = 1000m, cargaHorariaSemanal = 40 });
            await AssertMensagemNegocioAsync(rCtAfa, "contratar afastado: ");
            Assert.Contains("está inativo ou foi desligado", await MensagemErroAsync(rCtAfa));

            // Desligado: também bloqueia.
            var rDe = await c.PostAsJsonAsync($"api/administrativo360/colaboradores/{colab}/status", new { status = "DESLIGADO" });
            Assert.True(rDe.IsSuccessStatusCode, await MensagemErroAsync(rDe));
            var rCtDe = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "CLT", inicio = hoje, fim = (DateOnly?)null, salario = 1000m, cargaHorariaSemanal = 40 });
            await AssertMensagemNegocioAsync(rCtDe, "contratar desligado: ");

            // Reativado: volta a ser contratável.
            var rAt = await c.PostAsJsonAsync($"api/administrativo360/colaboradores/{colab}/status", new { status = "ATIVO" });
            Assert.True(rAt.IsSuccessStatusCode, await MensagemErroAsync(rAt));
            var rCtOk = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "CLT", inicio = hoje, fim = (DateOnly?)null, salario = 1000m, cargaHorariaSemanal = 40 });
            Assert.True(rCtOk.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, await MensagemErroAsync(rCtOk));
            contrato = (await LerCorpoAsync(rCtOk)).GetProperty("id").GetGuid();

            // Status fora do domínio e colaborador inexistente: recusa clara.
            var rInv = await c.PostAsJsonAsync($"api/administrativo360/colaboradores/{colab}/status", new { status = "FERIAS" });
            Assert.Equal(HttpStatusCode.BadRequest, rInv.StatusCode);
            var rNaoEncontrado = await c.PostAsJsonAsync($"api/administrativo360/colaboradores/{Guid.NewGuid()}/status", new { status = "ATIVO" });
            await AssertMensagemNegocioAsync(rNaoEncontrado, "status de inexistente: ");
            Assert.Contains("Colaborador não encontrado ou pertencente a outra organização.", await MensagemErroAsync(rNaoEncontrado));
        }
        finally { await LimparAsync(new[] { contrato }, new[] { colab }, new[] { cargo }, new[] { depId }); }
    }

    // =========================================================================
    // 7. REGRAS DE CONTRATO (vigência, sobreposição, encerramento, cancelamento, histórico)
    // =========================================================================

    [Fact]
    public async Task Contratos_RegrasDeVigenciaSobreposicaoEncerramentoEHistoricoPreservado()
    {
        var c = await ClienteAutenticadoAsync();
        var suffixo = Sufixo();
        var depId = Guid.Empty; var cargo = Guid.Empty; var colab = Guid.Empty;
        var c1 = Guid.Empty; var c2 = Guid.Empty; var c3 = Guid.Empty; var c4 = Guid.Empty;
        try
        {
            var dep = await CriarDepartamentoAsync(c, $"DEP{suffixo}", "Dept Contratos");
            depId = dep.GetProperty("id").GetGuid();
            var cg = await CriarCargoAsync(c, $"CC{suffixo}", "Cargo Contratos", depId);
            cargo = cg.GetProperty("id").GetGuid();
            var el = await CriarColaboradorAsync(c, $"MC{suffixo}", "Colab Contratos", CpfValido(), $"ct{suffixo}@exemplo.com.br", cargo);
            colab = el.GetProperty("id").GetGuid();

            // --- Validações de entrada ---
            var rSemInicio = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "CLT", salario = 1000m, cargaHorariaSemanal = 40 });
            await AssertMensagemNegocioAsync(rSemInicio, "sem início: ");
            Assert.Contains("Informe a data de início do contrato.", await MensagemErroAsync(rSemInicio));

            var rFimAntes = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "CLT", inicio = new DateOnly(2026, 7, 20), fim = new DateOnly(2026, 7, 5), salario = 1000m, cargaHorariaSemanal = 40 });
            await AssertMensagemNegocioAsync(rFimAntes, "fim antes do início: ");
            Assert.Contains("Data final anterior ao início do contrato.", await MensagemErroAsync(rFimAntes));

            var rSalario = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "CLT", inicio = new DateOnly(2026, 7, 10), salario = 0m, cargaHorariaSemanal = 40 });
            Assert.Equal(HttpStatusCode.BadRequest, rSalario.StatusCode);
            var rTipo = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "FREELANCER", inicio = new DateOnly(2026, 7, 10), salario = 1000m, cargaHorariaSemanal = 40 });
            Assert.Equal(HttpStatusCode.BadRequest, rTipo.StatusCode);

            // --- Vigência e sobreposição ---
            var rc1 = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "TEMPORARIO", inicio = new DateOnly(2026, 7, 1), fim = new DateOnly(2026, 7, 15), salario = 1200m, cargaHorariaSemanal = 20 });
            Assert.True(rc1.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, await MensagemErroAsync(rc1));
            c1 = (await LerCorpoAsync(rc1)).GetProperty("id").GetGuid();
            Assert.Equal("VIGENTE", (await LerCorpoAsync(rc1)).GetProperty("status").GetString());

            var rSobreposto = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "PJ", inicio = new DateOnly(2026, 7, 10), fim = new DateOnly(2026, 7, 31), salario = 5000m, cargaHorariaSemanal = 40 });
            await AssertMensagemNegocioAsync(rSobreposto, "sobreposição: ");
            Assert.Contains("Já existe contrato vigente com período sobreposto para este colaborador.", await MensagemErroAsync(rSobreposto));

            // Adjacente (começa dia após o fim do anterior) é permitido.
            var rc2 = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "CLT", inicio = new DateOnly(2026, 7, 16), fim = new DateOnly(2026, 7, 31), salario = 3000m, cargaHorariaSemanal = 40 });
            Assert.True(rc2.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, await MensagemErroAsync(rc2));
            c2 = (await LerCorpoAsync(rc2)).GetProperty("id").GetGuid();

            // --- Encerramento com data final e histórico preservado ---
            Assert.True((await c.PostAsJsonAsync($"api/administrativo360/contratos/{c2}/encerrar", new { fim = "2026-07-25" })).IsSuccessStatusCode);
            var rDet2 = await c.GetAsync($"api/administrativo360/contratos/{c2}");
            var jDet2 = await LerCorpoAsync(rDet2);
            Assert.Equal("ENCERRADO", jDet2.GetProperty("status").GetString());
            Assert.Equal(new DateOnly(2026, 7, 25).ToString("yyyy-MM-dd"), jDet2.GetProperty("fim").GetString());
            Assert.Equal(new DateOnly(2026, 7, 16).ToString("yyyy-MM-dd"), jDet2.GetProperty("inicio").GetString());

            // Período sobreposto ao ENCERRADO não bloqueia nova contratação.
            var rc3 = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "PJ", inicio = new DateOnly(2026, 7, 20), fim = new DateOnly(2026, 7, 30), salario = 5500m, cargaHorariaSemanal = 30 });
            Assert.True(rc3.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, await MensagemErroAsync(rc3));
            c3 = (await LerCorpoAsync(rc3)).GetProperty("id").GetGuid();

            // Encerrar com fim anterior ao início é recusado; reencerrar também.
            var rFimRuim = await c.PostAsJsonAsync($"api/administrativo360/contratos/{c1}/encerrar", new { fim = "2026-06-20" });
            await AssertMensagemNegocioAsync(rFimRuim, "encerrar fim < início: ");
            Assert.Contains("Data final anterior ao início do contrato.", await MensagemErroAsync(rFimRuim));
            Assert.True((await c.PostAsJsonAsync($"api/administrativo360/contratos/{c1}/encerrar", new { fim = "2026-07-10" })).IsSuccessStatusCode);
            var rReenc = await c.PostAsJsonAsync($"api/administrativo360/contratos/{c1}/encerrar", new { fim = "2026-07-12" });
            await AssertMensagemNegocioAsync(rReenc, "reencerrar: ");
            Assert.Contains("Somente contratos vigentes", await MensagemErroAsync(rReenc));

            // --- Cancelamento (erro de cadastro) e releitura ---
            Assert.True((await c.PostAsync($"api/administrativo360/contratos/{c3}/cancelar", new StringContent(string.Empty))).IsSuccessStatusCode);
            var jDet3 = await LerCorpoAsync(await c.GetAsync($"api/administrativo360/contratos/{c3}"));
            Assert.Equal("CANCELADO", jDet3.GetProperty("status").GetString());
            // O cancelamento preserva o histórico: a data final registrada permanece.
            Assert.Equal("2026-07-30", jDet3.GetProperty("fim").GetString());

            // O CANCELADO também não bloqueia período.
            var rc4 = await ContratarBrutoAsync(c, new { colaboradorId = colab, tipo = "CLT", inicio = new DateOnly(2026, 7, 21), fim = new DateOnly(2026, 7, 28), salario = 3100m, cargaHorariaSemanal = 40 });
            Assert.True(rc4.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created, await MensagemErroAsync(rc4));
            c4 = (await LerCorpoAsync(rc4)).GetProperty("id").GetGuid();

            // Contrato inexistente: detalhe e transição com mensagens PT-BR.
            var rDetInex = await c.GetAsync($"api/administrativo360/contratos/{Guid.NewGuid()}");
            await AssertMensagemNegocioAsync(rDetInex, "detalhe inexistente: ");
            Assert.Contains("Contrato não encontrado ou pertencente a outra organização.", await MensagemErroAsync(rDetInex));
            var rEncInex = await c.PostAsJsonAsync($"api/administrativo360/contratos/{Guid.NewGuid()}/encerrar", new { fim = "2026-07-18" });
            await AssertMensagemNegocioAsync(rEncInex, "encerrar inexistente: ");
            Assert.Contains("Somente contratos vigentes", await MensagemErroAsync(rEncInex));

            // Detalhe expõe criadoEm (auditoria).
            var jDet4 = await LerCorpoAsync(await c.GetAsync($"api/administrativo360/contratos/{c4}"));
            Assert.True(DateTime.TryParse(jDet4.GetProperty("criadoEm").GetString(), out _), $"criadoEm inválido: {jDet4.GetProperty("criadoEm").GetString()}");
        }
        finally { await LimparAsync(new[] { c1, c2, c3, c4 }, new[] { colab }, new[] { cargo }, new[] { depId }); }
    }

    // =========================================================================
    // 8. CONCORRÊNCIA — contratações simultâneas do mesmo colaborador/período
    // =========================================================================

    [Fact]
    public async Task Contratos_ConcorrenciaDeContratacaoExatamenteUmaVence()
    {
        var c = await ClienteAutenticadoAsync();
        var suffixo = Sufixo();
        var depId = Guid.Empty; var cargo = Guid.Empty; var colab = Guid.Empty;
        var vitoriosos = new List<Guid>();
        try
        {
            var dep = await CriarDepartamentoAsync(c, $"DEP{suffixo}", "Dept Concorrência");
            depId = dep.GetProperty("id").GetGuid();
            var cg = await CriarCargoAsync(c, $"CX{suffixo}", "Cargo Concorrência", depId);
            cargo = cg.GetProperty("id").GetGuid();
            var el = await CriarColaboradorAsync(c, $"MX{suffixo}", "Colab Concorrência", CpfValido(), $"cc{suffixo}@exemplo.com.br", cargo);
            colab = el.GetProperty("id").GetGuid();

            object Payload() => new { colaboradorId = colab, tipo = "CLT", inicio = new DateOnly(2026, 8, 1), fim = (DateOnly?)null, salario = 1000m, cargaHorariaSemanal = 40 };
            var respostas = await Task.WhenAll(
                ContratarBrutoAsync(c, Payload()),
                ContratarBrutoAsync(c, Payload()),
                ContratarBrutoAsync(c, Payload()));

            var sucos = respostas.Where(r => r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created).ToList();
            var erros = respostas.Where(r => !r.IsSuccessStatusCode).ToList();
            Assert.True(sucos.Count == 1, $"Concorrência deve eleger exatamente uma contratação. Obtido: {string.Join(",", respostas.Select(r => (int)r.StatusCode))}");
            foreach (var vitoria in sucos)
                vitoriosos.Add((await LerCorpoAsync(vitoria)).GetProperty("id").GetGuid());
            foreach (var perdedor in erros)
            {
                Assert.Equal(HttpStatusCode.BadRequest, perdedor.StatusCode);
                Assert.Contains("sobreposto", await MensagemErroAsync(perdedor));
            }
        }
        finally { await LimparAsync(vitoriosos, new[] { colab }, new[] { cargo }, new[] { depId }); }
    }

    // =========================================================================
    // 9. ISOLAMENTO MULTI-TENANT — dados de outra organização são invisíveis e intocáveis
    // =========================================================================

    [Fact]
    public async Task IsolacaoMultiTenant_DadosDeOutraOrganizacaoInvisiveisEIntocaveis()
    {
        var c = await ClienteAutenticadoAsync();
        var suffixo = Sufixo();
        var depB = Guid.Empty; var cargoB = Guid.Empty; var colabB = Guid.Empty; var ctB = Guid.Empty; var depA = Guid.Empty;
        var nomeB = $"Colab Isolado {suffixo}";
        try
        {
            // Estrutura completa pertencente ao TenantIsolado, criada direto no banco.
            depB = await InsertDepartamentoBAsync(TenantIsolado, $"ISO{suffixo}A", $"Dep Isolado {suffixo}");
            cargoB = await InsertCargoBAsync(TenantIsolado, $"ISO{suffixo}C", $"Cargo Isolado {suffixo}", depB);
            colabB = await InsertColaboradorBAsync(TenantIsolado, $"ISO{suffixo}M", nomeB, CpfValido(), cargoB);
            ctB = await InsertContratoBAsync(TenantIsolado, colabB, DateOnly.FromDateTime(DateTime.Today), 2000m);

            // Mesmo código em outro tenant pode conviver (índice único é por tenant).
            var a = await CriarDepartamentoAsync(c, $"ISO{suffixo}A", $"Dep Local {suffixo}");
            depA = a.GetProperty("id").GetGuid();
            Assert.NotEqual(depB, depA);

            // Invisibilidade nas listagens do tenant atual: só o departamento local aparece.
            var rLista = await LerCorpoAsync(await c.GetAsync($"api/administrativo360/departamentos?busca=ISO{suffixo}A"));
            Assert.Equal(1, rLista.GetProperty("total").GetInt64());
            Assert.Equal(depA.ToString(), rLista.GetProperty("items")[0].GetProperty("id").GetString());

            var rListaCt = await LerCorpoAsync(await c.GetAsync($"api/administrativo360/contratos?busca=ISO{suffixo}M"));
            Assert.Equal(0, rListaCt.GetProperty("total").GetInt64());

            // Escrita cruzada é recusada com mensagem de negócio (nunca 500 nem SQL).
            var rPut = await c.PutAsJsonAsync($"api/administrativo360/departamentos/{depB}", new { codigo = $"ISO{suffixo}A", nome = "Hackeado pelo tenant A" });
            await AssertMensagemNegocioAsync(rPut, "PUT dept B: ");
            Assert.Contains("Departamento não encontrado ou já inativo.", await MensagemErroAsync(rPut));

            var rInat = await c.PostAsync($"api/administrativo360/departamentos/{depB}/inativar", new StringContent(string.Empty));
            await AssertMensagemNegocioAsync(rInat, "inativar dept B: ");

            var rEnc = await c.PostAsJsonAsync($"api/administrativo360/contratos/{ctB}/encerrar", new { fim = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd") });
            await AssertMensagemNegocioAsync(rEnc, "encerrar contrato B: ");
            Assert.Contains("Somente contratos vigentes", await MensagemErroAsync(rEnc));

            var rDet = await c.GetAsync($"api/administrativo360/contratos/{ctB}");
            await AssertMensagemNegocioAsync(rDet, "detalhe contrato B: ");
            Assert.Contains("Contrato não encontrado ou pertencente a outra organização.", await MensagemErroAsync(rDet));

            // O dado de B continua íntegro no banco após as tentativas de escrita cruzada.
            await using var cn = ConexaoDb();
            var intacto = await cn.QuerySingleAsync<DepartamentoIntegridade>(
                "select nome as Nome, reg_status as RegStatus from plantaopro.adm_departamentos where id=@id", new { id = depB });
            Assert.Equal($"Dep Isolado {suffixo}", intacto.Nome);
            Assert.Equal("A", intacto.RegStatus);
        }
        finally
        {
            await LimparAsync(Array.Empty<Guid>(), Array.Empty<Guid>(), Array.Empty<Guid>(), new[] { depA });
            // Tenant B: ordem reversa das FKs.
            await using var cn = ConexaoDb();
            if (ctB != Guid.Empty) await cn.ExecuteAsync("delete from plantaopro.adm_contratos_trabalho where id=@id", new { id = ctB });
            if (colabB != Guid.Empty) await cn.ExecuteAsync("delete from plantaopro.adm_colaboradores where id=@id", new { id = colabB });
            if (cargoB != Guid.Empty) await cn.ExecuteAsync("delete from plantaopro.adm_cargos where id=@id", new { id = cargoB });
            if (depB != Guid.Empty) await cn.ExecuteAsync("delete from plantaopro.adm_departamentos where id=@id", new { id = depB });
        }
    }
}
