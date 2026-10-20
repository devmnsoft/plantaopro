using System.Security.Cryptography;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R72 — anexos de arquivos binarios (PDF/Word/imagem) por entidade (pedido do usuario).
/// Antes da R72 o unico upload do sistema era a Central XML (.xml apenas); funcionarios
/// (colaboradores), empresas (parceiros) e notas fiscais (pre-emissao) nao recebiam documentos.
/// 1) Round trip: criar (base64 + sha256) -&gt; listar -&gt; baixar (mesmos bytes) -&gt; exclusao macia.
/// 2) Isolamento por tenant e filtro por categoria/entidade.
/// 3) Validacao: whitelist de extensoes, limite de 8 MiB, arquivo vazio, base64 invalido.
/// 4) Integridade do contrato de banco (v2362): manifesto com hash real,
///    SOURCE/SOURCE-SHA256 nos scripts completos e scrpt_completo.sha256 coerente
///    (camada complementa a DatabaseGeneratorIntegrityTests, que cobre as entradas genericamente).
/// </summary>
public sealed class R72AnexosArquivosContratoTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static AnexosArquivosRepository Repo() => new(TestDatabase.ConnectionString);

    /// <summary>Advisory lock serializa a aplicacao entre classes paralelas; probe idempotente na tabela.</summary>
    private static async Task GarantirSchemaAsync()
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("SELECT pg_advisory_lock(hashtext('r72_anexos_arquivos_schema'));");
        try
        {
            var tem = await cn.ExecuteScalarAsync<bool>("select exists(select 1 from information_schema.tables where table_schema='plantaopro' and table_name='anexos_arquivos')");
            if (!tem)
            {
                var sql = Read("database/migrations/2026_10_v2362_r72_anexos_arquivos.sql");
                for (var tentativa = 1; ; tentativa++)
                {
                    try { await cn.ExecuteAsync(sql); break; }
                    catch (PostgresException ex) when (ex.SqlState == "40P01" && tentativa < 5) { await Task.Delay(150 * tentativa); }
                }
            }
        }
        finally { await cn.ExecuteAsync("SELECT pg_advisory_unlock(hashtext('r72_anexos_arquivos_schema'));"); }
    }

    private static Guid NovoTenant() => Guid.NewGuid();

    private static async Task LimparAsync(params Guid[] tenants)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("delete from plantaopro.anexos_arquivos where tenant_id = any(@ts)", new { ts = tenants });
    }

    private static AnexoArquivoCriarCommand Cmd(string nome, byte[] bytes, string categoria, Guid? ent = null, string? obs = null) =>
        new(categoria, ent, nome, "application/octet-stream", Convert.ToBase64String(bytes), bytes.Length, obs);

    // ---------- round trip ----------

    [Fact]
    public async Task Criar_Listar_Baixar_RetornaOsMesmosBytesEGravaSha256()
    {
        await GarantirSchemaAsync();
        var t = NovoTenant();
        try
        {
            var repo = Repo();
            var bytes = new byte[] { 1, 2, 3, 250, 65 };
            var id = await repo.CriarAsync(t, Guid.NewGuid(), Cmd("nota-fiscal.pdf", bytes, "funcionario", obs: "r72"));
            var lista = await repo.ListarAsync(t, "FUNCIONARIO", null);
            var unico = Assert.Single(lista);
            Assert.Equal(id, unico.Id);
            Assert.Equal("FUNCIONARIO", unico.Categoria);
            Assert.Equal("nota-fiscal.pdf", unico.NomeOriginal);
            Assert.Equal("pdf", unico.Extensao);
            Assert.Equal(5, unico.Tamanho);
            Assert.Equal("r72", unico.Observacao);

            var conteudo = await repo.ObterConteudoAsync(t, id);
            Assert.NotNull(conteudo);
            Assert.Equal(bytes, conteudo!.Bytes);

            var shaEsperado = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.OpenAsync();
            var shaGravado = await cn.ExecuteScalarAsync<string>("select sha256 from plantaopro.anexos_arquivos where id=@id", new { id });
            Assert.Equal(shaEsperado, shaGravado);
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task Categoria_NormalizaParaMaiusculasNaGravacao()
    {
        await GarantirSchemaAsync();
        var t = NovoTenant();
        try
        {
            var repo = Repo();
            var id = await repo.CriarAsync(t, Guid.NewGuid(), Cmd("x.docx", new byte[] { 9 }, "empresa"));
            var linha = await repo.ListarAsync(t, "EMPRESA", null);
            var unico = Assert.Single(linha);
            Assert.Equal(id, unico.Id);
            Assert.Equal("docx", unico.Extensao);
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task Tenant_NaoVeeNemRemovaAnexosDeOutroTenant()
    {
        await GarantirSchemaAsync();
        var tA = NovoTenant();
        var tB = NovoTenant();
        try
        {
            var repo = Repo();
            var id = await repo.CriarAsync(tA, Guid.NewGuid(), Cmd("sigilo.pdf", new byte[] { 7 }, "outro"));
            Assert.Empty(await repo.ListarAsync(tB, "OUTRO", null));
            Assert.Null(await repo.ObterConteudoAsync(tB, id));
            Assert.False(await repo.RemoverAsync(tB, id));
            // o dono continua vendo e removendo
            Assert.Single(await repo.ListarAsync(tA, "OUTRO", null));
            Assert.True(await repo.RemoverAsync(tA, id));
        }
        finally { await LimparAsync(tA, tB); }
    }

    [Fact]
    public async Task FiltroPorEntidadeId_IsolaORegistroDoAlvo()
    {
        await GarantirSchemaAsync();
        var t = NovoTenant();
        var entidade1 = Guid.NewGuid();
        var entidade2 = Guid.NewGuid();
        try
        {
            var repo = Repo();
            await repo.CriarAsync(t, Guid.NewGuid(), Cmd("a.pdf", new byte[] { 1 }, "NOTA_FISCAL", entidade1));
            await repo.CriarAsync(t, Guid.NewGuid(), Cmd("b.png", new byte[] { 2 }, "NOTA_FISCAL", entidade2));
            Assert.Empty(await repo.ListarAsync(t, "NOTA_FISCAL", Guid.NewGuid()));
            var porEntidade2 = await repo.ListarAsync(t, "NOTA_FISCAL", entidade2);
            var unico = Assert.Single(porEntidade2);
            Assert.Equal("b.png", unico.NomeOriginal);
            Assert.Equal(entidade2, unico.EntidadeId);
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task Remover_EhExclusaoMaciaERepetidaFalha()
    {
        await GarantirSchemaAsync();
        var t = NovoTenant();
        try
        {
            var repo = Repo();
            var id = await repo.CriarAsync(t, Guid.NewGuid(), Cmd("g.gif", new byte[] { 3 }, "outro"));
            Assert.True(await repo.RemoverAsync(t, id));
            Assert.False(await repo.RemoverAsync(t, id));
            Assert.Empty(await repo.ListarAsync(t, "OUTRO", null));
            await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
            await cn.OpenAsync();
            var status = await cn.ExecuteScalarAsync<string>("select reg_status from plantaopro.anexos_arquivos where id=@id", new { id });
            Assert.Equal("I", status);
        }
        finally { await LimparAsync(t); }
    }

    [Fact]
    public async Task ConteudoBase64Invalido_LancaFormatExceptionSemGravar()
    {
        await GarantirSchemaAsync();
        var t = NovoTenant();
        try
        {
            var repo = Repo();
            var cmd = Cmd("x.pdf", new byte[] { 1 }, "outro");
            var corrompido = cmd with { ConteudoBase64 = "###nao-e-base64###" };
            await Assert.ThrowsAsync<FormatException>(() => repo.CriarAsync(t, Guid.NewGuid(), corrompido));
            Assert.Empty(await repo.ListarAsync(t, "OUTRO", null));
        }
        finally { await LimparAsync(t); }
    }

    // ---------- validacao e constantes do contrato ----------

    [Fact]
    public void ValidarArquivo_RecebeWhitelistRejeitaDemaisFormatosETamanhos()
    {
        foreach (var nomeOk in new[] { "a.pdf", "b.doc", "c.DOCX", "d.jpg", "e.jpeg", "f.PNG", "g.webp", "h.gif", "i.xml" })
            Assert.True(AnexosArquivosRepository.ValidarArquivo(nomeOk, 10) is null, $"deveria aceitar {nomeOk}");

        Assert.Contains("Selecione", AnexosArquivosRepository.ValidarArquivo(null, 10));
        Assert.Contains("Selecione", AnexosArquivosRepository.ValidarArquivo("  ", 10));
        Assert.Contains("Formato nao aceito", AnexosArquivosRepository.ValidarArquivo("virus.exe", 10));
        Assert.Contains("Formato nao aceito", AnexosArquivosRepository.ValidarArquivo("sem-extensao", 10));
        Assert.Contains("vazio", AnexosArquivosRepository.ValidarArquivo("z.pdf", 0));
        Assert.Contains("8 MB", AnexosArquivosRepository.ValidarArquivo("z.pdf", AnexosArquivosRepository.TamanhoMaximoBytes + 1L));
        Assert.Null(AnexosArquivosRepository.ValidarArquivo("z.pdf", AnexosArquivosRepository.TamanhoMaximoBytes));
    }

    [Fact]
    public void ContratosDeCategoriaEEstendaoSaoCongelados()
    {
        Assert.Equal(
            new[] { "EMPRESA", "FUNCIONARIO", "NOTA_FISCAL", "OUTRO" },
            AnexosArquivosRepository.CategoriasPermitidas.OrderBy(c => c).ToArray());
        Assert.Equal(
            new[] { "doc", "docx", "gif", "jpeg", "jpg", "pdf", "png", "webp", "xml" },
            AnexosArquivosRepository.ExtensoesPermitidas.OrderBy(c => c).ToArray());
        Assert.Equal(8 * 1024 * 1024, AnexosArquivosRepository.TamanhoMaximoBytes);
    }

    // ---------- integridade do contrato de banco (v2362) ----------

    [Fact]
    public void MigrationV2362_NoManifestoComHashRealENosScriptsCompleto()
    {
        var migRel = "database/migrations/2026_10_v2362_r72_anexos_arquivos.sql";
        var raw = File.ReadAllBytes(Path.Combine(Root, migRel));
        var hash = Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant();
        var hashLf = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(Root, migRel)).Replace("\r\n", "\n")))).ToLowerInvariant();

        var manifest = File.ReadAllText(Path.Combine(Root, "database", "migration-manifest.json"), System.Text.Encoding.UTF8);
        Assert.Contains("2026_10_v2362_r72_anexos_arquivos", manifest);
        Assert.True(manifest.Contains(hash) || manifest.Contains(hashLf), "manifesto nao referencia o hash da migration v2362");
        // dependencia direta na migration anterior (R71c)
        Assert.Contains("2026_10_v2361_r71c_ai_provedor_mistral", manifest);

        var completo = File.ReadAllText(Path.Combine(Root, "database", "scrpt_completo.sql"), System.Text.Encoding.UTF8).Replace("\r\n", "\n");
        Assert.Contains($"-- SOURCE: {migRel}\n-- SOURCE-SHA256: {hash}", completo);
        var pgadmin = File.ReadAllText(Path.Combine(Root, "database", "pgadmin", "instalar_no_banco_atual.sql"), System.Text.Encoding.UTF8).Replace("\r\n", "\n");
        Assert.Contains($"-- SOURCE: {migRel}\n-- SOURCE-SHA256: {hash}", pgadmin);

        // corpo da tabela no script completo: coluna de conteudo em text (convencao base64 da casa)
        Assert.Contains("create table if not exists plantaopro.anexos_arquivos", completo);
        Assert.Contains("conteudo text not null default ''", completo);
    }

    [Fact]
    public void ScrptCompletoSha256_CorrespondeAoByteADosArquivosGerados()
    {
        var linha = File.ReadAllText(Path.Combine(Root, "database", "scrpt_completo.sha256"), System.Text.Encoding.UTF8).Trim();
        var registrada = linha.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
        var atual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(Root, "database", "scrpt_completo.sql")))).ToLowerInvariant();
        Assert.Equal(registrada, atual);
    }
}
