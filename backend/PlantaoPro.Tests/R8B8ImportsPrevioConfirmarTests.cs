using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using PlantaoPro.Api;
using PlantaoPro.Api.Controllers;
using PlantaoPro.Api.Data;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using PlantaoPro.Tests.Infrastructure;
using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R8-B8 — Central de importações: fluxo padronizado selecionar→validar→PRÉVIA→CONFIRMAR→processar→resultado (matriz §24).
/// Antes: os dois importadores reais (XML NF-e/NFS-e Adm360 e CID CSV) validavam e GRAVAVAM direto —
/// as etapas de PRÉVIA e CONFIRMAR não existiam (varredura C2: busca "previa"/"preview" em import → nada).
/// Agora cada importador tem uma etapa de prévia que roda o MESMO parsing/validação SEM gravar (espelha por
/// leitura as decisões de gravação), e a etapa de confirmar/processar continua sendo o endpoint de escrita
/// existente (back-compat). Prova: prévia não persiste nada e reporta exatamente o que o confirmar faria.
/// </summary>
public sealed class R8B8ImportsPrevioConfirmarTests
{
    private static string Root => RepositoryPathResolver.ResolveRoot();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    private static IConfiguration Cfg() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Default"] = TestDatabase.ConnectionString })
        .Build();

    private static Saude360ClinicalService Cid(Guid tenant, Guid? userId = null)
    {
        // ConflitoHorarioService é usado apenas por fluxos de agendamento; o caminho de importação CID não o toca.
        var user = new FakeCurrentUser(tenant, userId);
        return new Saude360ClinicalService(Cfg(), user, new FakeAuditService(), NullLogger<Saude360ClinicalService>.Instance, null!);
    }

    private static async Task GarantirConexaoAsync(string cs)
    {
        try { await using var cn = new NpgsqlConnection(cs); await cn.OpenAsync(); }
        catch (Exception ex) { Assert.Fail($"PostgreSQL obrigatório para a suíte R8-B8 não está acessível: {ex.Message}"); }
    }

    // ---------- fixtures XML (padrão A3/B1: tenant compartilhado, chaves únicas por execução) ----------

    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid UsuarioGestor = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    /// <summary>CNPJ do estabelecimento ativo do tenant (autorizado para receber NF-e/NFC-e).</summary>
    private const string CnpjDestinoAutorizado = "46389044000130";

    private static string ChaveNfeUnica()
    {
        string D(int n) => string.Concat(Enumerable.Range(0, n).Select(_ => (char)Random.Shared.Next('0', '9')));
        return "442609" + D(14) + "55" + D(22); // 6 + 14 + 2 + 22 = 44
    }

    private static string CorpoInfNfe(string chaveAcesso, string? modIde = null, string? destCnpj = null,
        string xNomeEmitente = "Emitente Teste R8B8 LTDA")
    {
        var mod = modIde ?? chaveAcesso.Substring(20, 2);
        // destCnpj nulo -> destinatário autorizado (fluido importável); outra chave -> quarentena DESTINATARIO_NAO_AUTORIZADO.
        var cnpjDest = destCnpj ?? CnpjDestinoAutorizado;
        var dest = $@"<dest>
          <CNPJ>{cnpjDest}</CNPJ>
          <xNome>Destinatario Santa Casa SA</xNome>
          <enderDest><xLgr>Rua B</xLgr><xMun>Cidade</xMun><UF>MG</UF></enderDest>
        </dest>";
        return $@"<ide>
    <cUF>44</cUF>
    <mod>{mod}</mod>
    <serie>1</serie>
    <nNF>{chaveAcesso.Substring(25, 9)}</nNF>
    <dhEmi>2026-10-03T10:00:00-03:00</dhEmi>
  </ide>
  <emit>
    <CNPJ>11222333000181</CNPJ>
    <xNome>{xNomeEmitente}</xNome>
    <enderEmit><xLgr>Rua A</xLgr><xMun>Cidade</xMun><UF>MG</UF></enderEmit>
  </emit>
  {dest}
  <total>
    <ICMSTot>
      <vBC>100.00</vBC>
      <vNF>110.00</vNF>
      <vProd>100.00</vProd>
    </ICMSTot>
  </total>";
    }

    private static string NfeXml(string chaveAcesso, string? modIde = null, string? destCnpj = null)
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<infNFe Id=""NFe{chaveAcesso}"" versao=""4.00"">
{CorpoInfNfe(chaveAcesso, modIde, destCnpj)}
</infNFe>";
    }

    /// <summary>Purge escopado por chave de acesso (não afeta seeds de outros testes).</summary>
    private static async Task PurgePorChavesAsync(string cs, params string[] chaves)
    {
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        await cn.ExecuteAsync("SELECT set_config('plantao.bypass_imutabilidade_adm360', 'on', true);", transaction: tx);
        await cn.ExecuteAsync(@"
            DELETE FROM plantaopro.adm360_documento_eventos
            WHERE documento_id IN (SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE chave_acesso = ANY(@chaves));
            DELETE FROM plantaopro.adm360_documento_itens
            WHERE documento_id IN (SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE chave_acesso = ANY(@chaves));
            DELETE FROM plantaopro.adm360_documentos_recebidos
            WHERE chave_acesso = ANY(@chaves);", new { chaves }, tx);
        await tx.CommitAsync();
    }

    private static async Task SeedClienteAsync(NpgsqlConnection cn, Guid t) =>
        await cn.ExecuteScalarAsync<Guid>("insert into plantaopro.clientes(id,tenant_id,nome,status,reg_status) values(@t,@t,'R8B8 Clinica','ATIVO','A') returning id", new { t });

    private static async Task LimparCidAsync(Guid t, string versao)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync("delete from plantaopro.cid_importacoes where cliente_id=@t", new { t });
        await cn.ExecuteAsync("delete from plantaopro.cid_tabela where versao=@v", new { v = versao });
        await cn.ExecuteAsync("delete from plantaopro.clientes where id=@t or tenant_id=@t", new { t });
    }

    // =========================================================================
    // 1) XML — prévia NÃO grava e mostra o que seria importado
    // =========================================================================
    [Fact]
    public async Task Xml_Previa_NaoGrava_MasMostraImportacao()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirConexaoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);
        try
        {
            var repo = new DocumentosXmlRepository(cs);
            var previa = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(chave)) { NomeArquivo = "r8b8-previa.xml" },
                CancellationToken.None, previa: true);

            Assert.Equal(1, previa.TotalUnidades);
            Assert.Equal(1, previa.Importados);
            Assert.Equal(0, previa.EmQuarentena);
            Assert.Equal(0, previa.Falhas);
            Assert.False(previa.Documentos[0].Quarentena);

            // Nada foi persistido pela prévia.
            var gravadas = await QueryCountDocAsync(chave);
            Assert.True(gravadas == 0, $"Prévia não deve gravar; obtido {gravadas} documento(s) para a chave.");
        }
        finally { await PurgePorChavesAsync(cs, chave); }
    }

    [Fact]
    public async Task Xml_Previa_Quarentena_Destinatario_NaoGrava()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirConexaoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);
        try
        {
            var repo = new DocumentosXmlRepository(cs);
            // destinatário presente porém fora dos autorizados -> quarentena (mesma regra da gravação).
            var previa = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(NfeXml(chave, destCnpj: "99999999000199")) { NomeArquivo = "r8b8-previa-quar.xml" },
                CancellationToken.None, previa: true);

            Assert.Equal(1, previa.EmQuarentena);
            Assert.Equal(0, previa.Importados);
            Assert.True(previa.Documentos[0].Quarentena);
            Assert.Contains("DESTINATARIO_NAO_AUTORIZADO", previa.Documentos[0].MotivoQuarentena);

            var gravadas = await QueryCountDocAsync(chave);
            Assert.True(gravadas == 0, $"Prévia em quarentena não deve gravar; obtido {gravadas}.");
        }
        finally { await PurgePorChavesAsync(cs, chave); }
    }

    // =========================================================================
    // 2) XML — jornada prévia → confirmar: a prévia prevê 1, o confirmar grava exatamente 1
    // =========================================================================
    [Fact]
    public async Task Xml_Jornada_PreviaEntaoConfirmar_GravaUmaUnicaVez()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirConexaoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);
        try
        {
            var repo = new DocumentosXmlRepository(cs);
            var xml = NfeXml(chave);

            var previa = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xml) { NomeArquivo = "r8b8-jornada.xml" },
                CancellationToken.None, previa: true);
            Assert.Equal(1, previa.Importados);
            Assert.True((await QueryCountDocAsync(chave)) == 0, "Antes do confirmar nada deveria estar gravado.");

            // Confirmar = mesma chamada sem a flag previa (endpoint importar-manual).
            var confirmar = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xml) { NomeArquivo = "r8b8-jornada.xml" },
                CancellationToken.None);
            Assert.Equal(1, confirmar.Importados);
            Assert.NotNull(confirmar.Documentos[0].DocumentoId);
            Assert.True((await QueryCountDocAsync(chave)) == 1, "Confirmar deve gravar exatamente 1 unidade.");
        }
        finally { await PurgePorChavesAsync(cs, chave); }
    }

    // =========================================================================
    // 3) XML — prévia após confirmar identifica duplicidade cross-request (idempotente), sem regravar
    // =========================================================================
    [Fact]
    public async Task Xml_Previa_AposConfirmar_IdentificaDuplicadoCrossRequest()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirConexaoAsync(cs);
        var chave = ChaveNfeUnica();
        await PurgePorChavesAsync(cs, chave);
        try
        {
            var repo = new DocumentosXmlRepository(cs);
            var xml = NfeXml(chave);

            var primeira = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xml) { NomeArquivo = "r8b8-dup.xml" }, CancellationToken.None);
            Assert.Equal(1, primeira.Importados);
            Assert.True((await QueryCountDocAsync(chave)) == 1, "Primeira importação deve gravar 1.");

            // Prévia da MESMA carga já presente: deve tratar como duplicado idempotente (sem gravar).
            var previa = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor,
                new ImportarXmlManualCommand(xml) { NomeArquivo = "r8b8-dup.xml" }, CancellationToken.None, previa: true);
            Assert.Equal(0, previa.Importados);
            Assert.True(previa.DuplicadosIgnorados >= 1, "Prévia deve sinalizar duplicidade cross-request.");
            Assert.True(previa.Documentos[0].DuplicadoIdempotente, "Unidade já importada deve vir como duplicado idempotente na prévia.");
            Assert.True((await QueryCountDocAsync(chave)) == 1, "Prévia de duplicata não deve criar nova linha.");
        }
        finally { await PurgePorChavesAsync(cs, chave); }
    }

    // =========================================================================
    // 4) CID — prévia NÃO grava e conta inseridos/atualizados/erros
    // =========================================================================
    [Fact]
    public async Task Cid_Previa_NaoGrava_MasContaInseridosAtualizadosEErros()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirConexaoAsync(cs);
        var t = Guid.NewGuid();
        var versao = "R8B8-" + Guid.NewGuid().ToString("N")[..6];
        var sfx = Guid.NewGuid().ToString("N")[..6];
        var codeExistente = $"R8B8{sfx}X";
        var codeNovo1 = $"R8B8{sfx}A";
        var codeNovo2 = $"R8B8{sfx}B";
        var codeErro = $"R8B8{sfx}E";
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await SeedClienteAsync(cn, t);
        try
        {
            // Estabelece UM código já existente via confirmar (1 linha gravada), isolado por versao única.
            var seed = new CidImportacaoRequest { Csv = $"codigo;descricao\n{codeExistente};Codigo preexistente", ArquivoNome = "seed.csv", Fonte = "R8B8", Versao = versao };
            var s1 = await Cid(t).ImportarCidCsvAsync(seed);
            Assert.True(s1.Success, s1.Message);

            // Prévia do CSV completo: 2 novos + 1 existente + 1 com descrição vazia (erro).
            var csv = $"codigo;descricao\n{codeNovo1};Novo um\n{codeNovo2};Novo dois\n{codeExistente};Preexistente alterado\n{codeErro};";
            var prev = await Cid(t).PreverImportacaoCidCsvAsync(new CidImportacaoRequest { Csv = csv, ArquivoNome = "full.csv", Fonte = "R8B8", Versao = versao });
            Assert.True(prev.Success, prev.Message);
            var d = (dynamic)prev.Data!;
            Assert.Equal(4, (int)d.TotalLinhas);
            Assert.Equal(2, (int)d.Inseridos);
            Assert.Equal(1, (int)d.Atualizados);
            Assert.Equal(1, (int)d.Erros);
            Assert.True((bool)d.previa);

            // A prévia não gravou os dois novos nem a linha de resumo.
            var novos = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.cid_tabela where versao=@v and upper(codigo)=any(@codes)",
                new { v = versao, codes = new[] { codeNovo1.ToUpperInvariant(), codeNovo2.ToUpperInvariant() } });
            Assert.True(novos == 0, $"Prévia não deve gravar novas linhas CID; obtido {novos}.");
            var resumos = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.cid_importacoes where cliente_id=@t and arquivo_nome='full.csv'", new { t });
            Assert.True(resumos == 0, "Prévia não deve gravar a linha de resumo da importação.");
            var existenteAinda = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.cid_tabela where versao=@v and upper(codigo)=@c", new { v = versao, c = codeExistente.ToUpperInvariant() });
            Assert.True(existenteAinda == 1, "Prévia não deve alterar o código já existente.");
        }
        finally { await LimparCidAsync(t, versao); }
    }

    // =========================================================================
    // 5) CID — jornada prévia → confirmar: mesmos contadores, e só o confirmar grava
    // =========================================================================
    [Fact]
    public async Task Cid_Jornada_PreviaEConfirmar_MesmosContadoresEGrava()
    {
        var cs = TestDatabase.ConnectionString;
        await GarantirConexaoAsync(cs);
        var t = Guid.NewGuid();
        var versao = "R8B8-" + Guid.NewGuid().ToString("N")[..6];
        var sfx = Guid.NewGuid().ToString("N")[..6];
        var codeNovo1 = $"R8B8{sfx}A";
        var codeNovo2 = $"R8B8{sfx}B";
        var codeErro = $"R8B8{sfx}E";
        await using var cn = new NpgsqlConnection(cs);
        await cn.OpenAsync();
        await SeedClienteAsync(cn, t);
        try
        {
            var csv = $"codigo;descricao\n{codeNovo1};Novo um\n{codeNovo2};Novo dois\n{codeErro};";

            var prev = await Cid(t).PreverImportacaoCidCsvAsync(new CidImportacaoRequest { Csv = csv, ArquivoNome = "jornada.csv", Fonte = "R8B8", Versao = versao });
            Assert.True(prev.Success, prev.Message);
            var dp = (dynamic)prev.Data!;
            Assert.Equal(3, (int)dp.TotalLinhas);
            Assert.Equal(2, (int)dp.Inseridos);
            Assert.Equal(0, (int)dp.Atualizados);
            Assert.Equal(1, (int)dp.Erros);
            Assert.True((await CidTabelaNovosAsync(cn, versao, codeNovo1, codeNovo2)) == 0, "Prévia não deve gravar.");

            var conf = await Cid(t).ImportarCidCsvAsync(new CidImportacaoRequest { Csv = csv, ArquivoNome = "jornada.csv", Fonte = "R8B8", Versao = versao });
            Assert.True(conf.Success, conf.Message);
            var dc = (dynamic)conf.Data!;
            Assert.Equal(2, (int)dc.Inseridos); // mesmos contadores previstos pela prévia
            Assert.Equal(1, (int)dc.Erros);
            Assert.True((await CidTabelaNovosAsync(cn, versao, codeNovo1, codeNovo2)) == 2, "Confirmar deve gravar as linhas previstas.");
            var resumo = await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.cid_importacoes where cliente_id=@t and arquivo_nome='jornada.csv'", new { t });
            Assert.True(resumo == 1, "Confirmar deve gravar exatamente 1 linha de resumo.");
        }
        finally { await LimparCidAsync(t, versao); }
    }

    // =========================================================================
    // 6) contrato: rotas + serviços de prévia coerentes
    // =========================================================================
    [Fact]
    public void Contrato_RotasEServicosDePreviaoCoerentes()
    {
        var xmlCtrl = Read("backend/PlantaoPro.Api/Controllers/Adm360DocumentosXmlController.cs");
        Assert.Contains("importar-preview", xmlCtrl);
        Assert.Contains("previa: true", xmlCtrl);

        var cidCtrl = Read("backend/PlantaoPro.Api/Controllers/Saude360ClinicalControllers.cs");
        Assert.Contains("prever-csv", cidCtrl);

        var repo = Read("backend/PlantaoPro.Infrastructure/Administrativo360/DocumentosXmlRepository.cs");
        Assert.Contains("AvaliarUnidadePrevioAsync", repo);
        Assert.Contains("bool previa", repo);

        var svc = Read("backend/PlantaoPro.Api/Saude360ClinicalService.cs");
        Assert.Contains("PreverImportacaoCidCsvAsync", svc);
        Assert.Contains("select exists(select 1 from plantaopro.cid_tabela", svc, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<int> QueryCountDocAsync(string chave)
    {
        await using var cn = new NpgsqlConnection(TestDatabase.ConnectionString);
        await cn.OpenAsync();
        return await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.adm360_documentos_recebidos where tenant_id=@t and chave_acesso=@c",
            new { t = TenantSantaCasa, c = chave });
    }

    private static async Task<int> CidTabelaNovosAsync(NpgsqlConnection cn, string versao, params string[] codes)
        => await cn.ExecuteScalarAsync<int>("select count(1) from plantaopro.cid_tabela where versao=@v and upper(codigo)=any(@codes)",
            new { v = versao, codes = codes.Select(c => c.ToUpperInvariant()).ToArray() });

    // ---------- fakes ----------

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly Guid _userId;
        private readonly Guid? _tenantId;
        public FakeCurrentUser(Guid? tenantId = null, Guid? userId = null)
        {
            _tenantId = tenantId;
            _userId = userId ?? Guid.Empty;
        }
        public bool GlobalAdmin => false;
        public Guid? UserId => _userId == Guid.Empty ? null : _userId;
        public Guid? TenantId => _tenantId;
        public Guid? ClienteId => _tenantId;
        public Guid? SessionId => null;
        public IReadOnlyCollection<string> Roles => Array.Empty<string>();
        public bool IsAuthenticated() => true;
        public bool IsGlobalAdmin() => false;
        public bool IsTenantAdmin() => true;
        public bool IsPartner() => false;
        public bool IsDoctor() => false;
        public bool HasRole(string role) => false;
    }

    private sealed class FakeAuditService : IAuditService
    {
        public Task RegistrarAsync(Guid? usuarioId, Guid? clienteId, string entidade, Guid? entidadeId, string acao, object? detalhes, bool sucesso, string? ipOrigem, string? perfil, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task LogAsync(Guid? userId, string acao, string entidade, Guid? registroId, string descricao, string? valorAnterior = null, string? valorNovo = null, string? ip = null, string? userAgent = null)
            => Task.CompletedTask;
    }
}
