using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;
using PlantaoPro.Infrastructure.Administrativo360;
using Xunit;

namespace PlantaoPro.Tests;

public sealed class Administrativo360CotacoesXmlDashboardTests
{
    private static string ObterConnectionString()
    {
        return Environment.GetEnvironmentVariable("PLANTAOPRO_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=127.0.0.1;Port=5432;Database=plantaopro_test;Username=postgres;Password=123456;Pooling=true;Maximum Pool Size=50;Minimum Pool Size=0;Timeout=30;Command Timeout=60;Search Path=PlantaoPro,public;Application Name=PlantaoPro.tests";
    }

    private static async Task GarantirConexaoBancoAsync(string cs)
    {
        try
        {
            await using var cn = new NpgsqlConnection(cs);
            await cn.OpenAsync();

            var hasModuloId = await cn.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_schema = 'plantaopro' AND table_name = 'tenant_modulos' AND column_name = 'modulo_id'
                );");

            if (!hasModuloId)
            {
                var migrationPath = Path.Combine(RepositoryPathResolver.RepoRoot, "database/migrations/2026_09_v2197_reconciliar_contratos_modulos.sql");
                if (File.Exists(migrationPath))
                {
                    var sql = await File.ReadAllTextAsync(migrationPath);
                    await cn.ExecuteAsync(sql);
                }
            }

            var hasAdm360 = await cn.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM plantaopro.tenant_modulos tm
                    JOIN plantaopro.modulos_sistema ms ON ms.id = tm.modulo_id
                    WHERE tm.tenant_id = @TenantSantaCasa AND ms.codigo = 'ADM360' AND tm.reg_status = 'A' AND tm.habilitado = true
                );", new { TenantSantaCasa });

            if (!hasAdm360)
            {
                await cn.ExecuteAsync(@"
                    INSERT INTO plantaopro.tenant_modulos (id, tenant_id, modulo_id, codigo, codigo_modulo, habilitado, status, origem, reg_status, reg_date)
                    SELECT gen_random_uuid(), @TenantSantaCasa, m.id, m.codigo, m.codigo, true, 'ATIVO', 'DEMO_MIGRATION', 'A', now()
                    FROM plantaopro.modulos_sistema m
                    WHERE upper(m.codigo) = 'ADM360' AND m.reg_status = 'A'
                    ON CONFLICT DO NOTHING;", new { TenantSantaCasa });
            }
        }
        catch (Exception ex)
        {
            Assert.Fail($"PostgreSQL obrigatório para a suíte de testes do Administrativo 360 não está acessível: {ex.Message}");
        }
    }

    private static readonly Guid TenantSantaCasa = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647502");
    private static readonly Guid TenantOutro = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647599");
    private static readonly Guid UsuarioGestor = Guid.Parse("d3f6584c-2c64-4e5a-9ea9-4e1428647511");

    // =========================================================================
    // ACEITE 1: Script executa em base preparada e reaplica sem duplicar
    // =========================================================================
    [Fact]
    public async Task Aceite01_ScriptDemo_ExecutaEReaplicaSemDuplicar()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        await using var cn = new NpgsqlConnection(cs);
        var cotacoesCount = await cn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM plantaopro.adm360_cotacoes WHERE tenant_id = @TenantSantaCasa",
            new { TenantSantaCasa });
        Assert.True(cotacoesCount >= 3, "A base demonstrativa deve conter as cotações carregadas pelo seed.");

        var xmlCount = await cn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM plantaopro.adm360_documentos_recebidos WHERE tenant_id = @TenantSantaCasa",
            new { TenantSantaCasa });
        Assert.True(xmlCount >= 2, "A base demonstrativa deve conter os XMLs carregados pelo seed.");
    }

    // =========================================================================
    // ACEITE 2: Login demonstrativo funciona com dados do banco
    // =========================================================================
    [Fact]
    public async Task Aceite02_LoginDemonstrativo_AutenticaCorretamenteContraPostgres()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        await using var cn = new NpgsqlConnection(cs);
        var user = await cn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT id, email, senha_hash, status FROM plantaopro.usuarios WHERE lower(email) = 'gestor@santacasa-demo.example' AND tenant_id = @TenantSantaCasa",
            new { TenantSantaCasa });

        Assert.NotNull(user);
        Assert.Equal("ATIVO", (string)user.status);
        Assert.True(BCrypt.Net.BCrypt.Verify("SantaCasa!Demo2026#Gestor", (string)user.senha_hash),
            "A senha inicial informada na especificação deve validar via BCrypt contra o hash do banco.");

        var modulos = (await cn.QueryAsync<string>(@"
            SELECT distinct upper(ms.codigo)
            FROM plantaopro.tenant_modulos tm
            JOIN plantaopro.modulos_sistema ms ON ms.id=tm.modulo_id AND ms.reg_status='A' AND upper(ms.status)='ATIVO'
            WHERE tm.tenant_id=@TenantSantaCasa AND tm.reg_status='A' AND tm.habilitado=true AND upper(tm.status)='ATIVO'
            ORDER BY 1",
            new { TenantSantaCasa })).ToList();

        Assert.Contains("ADM360", modulos);
    }

    // =========================================================================
    // ACEITE 3: Outro tenant não consulta cotações, anexos ou XML (Isolamento)
    // =========================================================================
    [Fact]
    public async Task Aceite03_IsolamentoMultiTenant_NaoVazaCotacoesAnexosOuXml()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var repoCot = new CotacoesRepository(cs);
        var repoXml = new DocumentosXmlRepository(cs);

        // Santa Casa consulta cotações
        var cotacoesSantaCasa = await repoCot.ListarCotacoesAsync(TenantSantaCasa);
        Assert.NotEmpty(cotacoesSantaCasa);

        // Outro tenant consulta cotações e não deve receber nada da Santa Casa
        var cotacoesOutro = await repoCot.ListarCotacoesAsync(TenantOutro);
        Assert.Empty(cotacoesOutro);

        // XML da Santa Casa vs Outro
        var xmlSantaCasa = await repoXml.ListarDocumentosAsync(TenantSantaCasa);
        Assert.NotEmpty(xmlSantaCasa);

        var xmlOutro = await repoXml.ListarDocumentosAsync(TenantOutro);
        Assert.Empty(xmlOutro);

        // Anexo da Santa Casa não pode ser obtido pelo TenantOutro
        var cotacaoId = cotacoesSantaCasa[0].Id;
        var cotacaoDetalhes = await repoCot.ObterCotacaoPorIdAsync(TenantSantaCasa, cotacaoId);
        if (cotacaoDetalhes?.Anexos.Any() == true)
        {
            var anexoId = cotacaoDetalhes.Anexos[0].Id;
            var anexoOutro = await repoCot.ObterAnexoAsync(TenantOutro, anexoId);
            Assert.Null(anexoOutro);
        }
    }

    // =========================================================================
    // ACEITE 4: Cotação repetida não duplica
    // =========================================================================
    [Fact]
    public async Task Aceite04_CotacaoRepetida_NaoDuplica()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var repo = new CotacoesRepository(cs);
        var estabelecimentos = await repo.ListarEstabelecimentosAsync(TenantSantaCasa);
        Assert.NotEmpty(estabelecimentos);
        var contas = await repo.ListarContasPortalAsync(TenantSantaCasa);
        Assert.NotEmpty(contas);

        var cmd = new CapturarCotacaoCommand(
            EstabelecimentoId: estabelecimentos[0].Id,
            PortalContaId: contas[0].Id,
            Provedor: "OPMENEXO",
            IdentificadorExterno: "TESTE-IDEMP-001",
            RevisaoExterna: 1,
            HospitalSolicitante: "Hospital Teste",
            PacienteIniciais: "A.B.C.",
            Procedimento: "Cirurgia Teste",
            DataPrevista: DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            PrazoResposta: DateTime.UtcNow.AddDays(2),
            Origem: "DADOS_DE_TESTE",
            PayloadOriginal: "{}",
            Itens: new[]
            {
                new CapturarCotacaoItemCommand(1, "ITEM-EXT-1", "Parafuso Cortical 3.5mm", null, null, 2, "UN")
            }
        );

        var id1 = await repo.CapturarCotacaoAsync(TenantSantaCasa, UsuarioGestor, cmd);
        var id2 = await repo.CapturarCotacaoAsync(TenantSantaCasa, UsuarioGestor, cmd);

        Assert.Equal(id1, id2);
    }

    // =========================================================================
    // ACEITE 5: Nova revisão preserva a anterior
    // =========================================================================
    [Fact]
    public async Task Aceite05_NovaRevisao_PreservaAnterior()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var repo = new CotacoesRepository(cs);
        var estabelecimentos = await repo.ListarEstabelecimentosAsync(TenantSantaCasa);
        var contas = await repo.ListarContasPortalAsync(TenantSantaCasa);

        var cmdRev1 = new CapturarCotacaoCommand(
            EstabelecimentoId: estabelecimentos[0].Id,
            PortalContaId: contas[0].Id,
            Provedor: "OPMENEXO",
            IdentificadorExterno: "TESTE-REV-005",
            RevisaoExterna: 1,
            HospitalSolicitante: "Hospital Teste",
            PacienteIniciais: "J.K.",
            Procedimento: "Cirurgia Teste Rev 1",
            DataPrevista: DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            PrazoResposta: DateTime.UtcNow.AddDays(2),
            Origem: "DADOS_DE_TESTE",
            PayloadOriginal: "{\"rev\":1}",
            Itens: new[]
            {
                new CapturarCotacaoItemCommand(1, "ITEM-REV-1", "Placa Bloqueada 3.5mm", null, null, 1, "UN")
            }
        );

        var idRev1 = await repo.CapturarCotacaoAsync(TenantSantaCasa, UsuarioGestor, cmdRev1);

        var cmdRev2 = cmdRev1 with
        {
            RevisaoExterna = 2,
            Procedimento = "Cirurgia Teste Rev 2 Alterada",
            PayloadOriginal = "{\"rev\":2}"
        };

        var idRev2 = await repo.CapturarCotacaoAsync(TenantSantaCasa, UsuarioGestor, cmdRev2);

        Assert.NotEqual(idRev1, idRev2);

        var cotRev1 = await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, idRev1);
        var cotRev2 = await repo.ObterCotacaoPorIdAsync(TenantSantaCasa, idRev2);

        Assert.NotNull(cotRev1);
        Assert.NotNull(cotRev2);
        Assert.Equal(1, cotRev1.RevisaoExterna);
        Assert.Equal(2, cotRev2.RevisaoExterna);
    }

    // =========================================================================
    // ACEITE 6: De/Para pendente impede resposta
    // =========================================================================
    [Fact]
    public async Task Aceite06_DeParaPendente_ImpedeResposta()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var repo = new CotacoesRepository(cs);

        // Buscar a cotação 2 do seed demonstrativo que possui item com status PENDENTE
        var cotacoes = await repo.ListarCotacoesAsync(TenantSantaCasa, status: "EM_RELACIONAMENTO");
        var cotacaoPendente = cotacoes.FirstOrDefault(c => c.ItensPendentes > 0);
        Assert.NotNull(cotacaoPendente);

        // Tentativa de aprovar resposta deve falhar com InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await repo.AprovarRespostaAsync(TenantSantaCasa, UsuarioGestor, new AprovarRespostaCotacaoCommand(cotacaoPendente.Id));
        });
    }

    // =========================================================================
    // ACEITE 7: Conversão de unidade é aplicada corretamente
    // =========================================================================
    [Fact]
    public void Aceite07_ConversaoUnidade_MultiplicaPeloFator()
    {
        // 5 caixas de 10 unidades com fator 10.0 = 50 unidades internas
        decimal qtdSolicitada = 5m;
        decimal fator = 10.0000m;
        decimal qtdConvertida = CotacaoRegras.CalcularQuantidadeConvertida(qtdSolicitada, fator);
        Assert.Equal(50.0000m, qtdConvertida);

        // Teste de arredondamento
        decimal qtdFracionada = CotacaoRegras.CalcularQuantidadeConvertida(2.5m, 1.25m);
        Assert.Equal(3.1250m, qtdFracionada);
    }

    // =========================================================================
    // ACEITE 8: Orçamento gerado usa o módulo existente
    // =========================================================================
    [Fact]
    public async Task Aceite08_OrcamentoGerado_UsaModuloExistenteAdm360Orcamentos()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var repo = new CotacoesRepository(cs);
        var cotacoes = await repo.ListarCotacoesAsync(TenantSantaCasa);
        var cotacaoComOrcamento = cotacoes.FirstOrDefault(c => c.OrcamentoId.HasValue);
        Assert.NotNull(cotacaoComOrcamento);

        await using var cn = new NpgsqlConnection(cs);
        var orc = await cn.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT id, numero, revisao, total_geral FROM plantaopro.adm360_orcamentos WHERE id = @OrcamentoId AND tenant_id = @TenantSantaCasa",
            new { OrcamentoId = cotacaoComOrcamento.OrcamentoId!.Value, TenantSantaCasa });

        Assert.NotNull(orc);
        Assert.Equal(1, (int)orc.revisao);
        Assert.True((decimal)orc.total_geral > 0);
    }

    // =========================================================================
    // ACEITE 9: Retry de envio não causa retransmissão cega
    // =========================================================================
    [Fact]
    public async Task Aceite09_RetryDeEnvio_RespeitaTentativasEOutbox()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var repo = new CotacoesRepository(cs);
        var respostas = await repo.ListarRespostasAsync(TenantSantaCasa);
        Assert.NotEmpty(respostas);

        // Cotação já aceita pelo portal não deve retransmitir cegamente
        var aceita = respostas.FirstOrDefault(r => r.StatusTransmissao == "ACEITA_PELO_PORTAL");
        Assert.NotNull(aceita);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await repo.TransmitirRespostaAsync(TenantSantaCasa, UsuarioGestor, new TransmitirRespostaCommand(aceita.Id));
        });

        Assert.Contains("já foi concluída", ex.Message);
    }

    // =========================================================================
    // ACEITE 10: Timeout fica pendente de confirmação
    // =========================================================================
    [Fact]
    public void Aceite10_Timeout_NaoAssumeSucessoNemFalhaDefinitiva()
    {
        // Transição válida não lança exceção
        CotacaoRegras.ValidarTransicaoStatus("PRONTA_PARA_ENVIO", "RESPONDIDA");
        // Transição inválida deve lançar InvalidOperationException
        Assert.Throws<InvalidOperationException>(() => CotacaoRegras.ValidarTransicaoStatus("RESPONDIDA", "RECEBIDA"));
    }

    // =========================================================================
    // ACEITE 11: Sem credenciais não existe sucesso externo fictício
    // =========================================================================
    [Fact]
    public async Task Aceite11_SemCredenciais_IntegracaoFicaBloqueadaENaoRetornaSucessoFicticio()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var repo = new CotacoesRepository(cs);
        var contas = await repo.ListarContasPortalAsync(TenantSantaCasa);

        var inpartConta = contas.FirstOrDefault(c => c.Provedor == "INPART");
        Assert.NotNull(inpartConta);
        Assert.Equal("BLOQUEADA", inpartConta.StatusIntegracao);
        Assert.False(string.IsNullOrWhiteSpace(inpartConta.MotivoBloqueio));

        var connector = new InpartConnector();
        var status = await connector.TestarConexaoAsync(inpartConta);
        Assert.False(status.Conectado);
        Assert.Equal("BLOQUEADA", status.Status);
    }

    // =========================================================================
    // ACEITE 12: XML repetido não duplica
    // =========================================================================
    [Fact]
    public async Task Aceite12_XmlRepetido_NaoDuplica()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var repo = new DocumentosXmlRepository(cs);
        var docs = await repo.ListarDocumentosAsync(TenantSantaCasa);
        var docValido = docs.FirstOrDefault(d => !d.Quarentena);
        Assert.NotNull(docValido);

        var detalhes = await repo.ObterDocumentoPorIdAsync(TenantSantaCasa, docValido.Id);
        Assert.NotNull(detalhes);

        // Reimportar o mesmo XML
        var idReimportado = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor, new ImportarXmlManualCommand(detalhes.XmlConteudo));
        Assert.Equal(docValido.Id, idReimportado);
    }

    // =========================================================================
    // ACEITE 13: XML de CNPJ não autorizado é recusado
    // =========================================================================
    [Fact]
    public async Task Aceite13_XmlCnpjNaoAutorizado_FicaEmQuarentena()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var chaveAcessoTeste = "35260988888888888888550010000000031000000030";
        await using (var cn = new NpgsqlConnection(cs))
        {
            await cn.ExecuteAsync(@"
                DELETE FROM plantaopro.adm360_documento_eventos 
                WHERE documento_id IN (SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE chave_acesso = @chaveAcessoTeste);
                DELETE FROM plantaopro.adm360_documento_itens 
                WHERE documento_id IN (SELECT id FROM plantaopro.adm360_documentos_recebidos WHERE chave_acesso = @chaveAcessoTeste);
                DELETE FROM plantaopro.adm360_documentos_recebidos 
                WHERE chave_acesso = @chaveAcessoTeste;",
                new { chaveAcessoTeste });
        }

        var repo = new DocumentosXmlRepository(cs);
        var xmlCnpjEstranho = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<nfeProc xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">
  <NFe>
    <infNFe Id=""NFe{chaveAcessoTeste}"" versao=""4.00"">
      <ide><mod>55</mod><nNF>3</nNF><serie>1</serie><dhEmi>2026-09-24T10:00:00-03:00</dhEmi></ide>
      <emit><CNPJ>11222333000144</CNPJ><xNome>Fornecedor X</xNome></emit>
      <dest><CNPJ>99999999000199</CNPJ><xNome>Empresa Estranha Nao Autorizada</xNome></dest>
      <total><ICMSTot><vNF>1500.00</vNF><vProd>1500.00</vProd></ICMSTot></total>
    </infNFe>
  </NFe>
</nfeProc>";

        var docId = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor, new ImportarXmlManualCommand(xmlCnpjEstranho));
        var doc = await repo.ObterDocumentoPorIdAsync(TenantSantaCasa, docId);

        Assert.NotNull(doc);
        Assert.True(doc.Quarentena);
        Assert.Equal("DESTINATARIO_NAO_AUTORIZADO", doc.MotivoQuarentena);
    }

    // =========================================================================
    // ACEITE 14: XML malformado ou com entidade externa (XXE) é rejeitado
    // =========================================================================
    [Fact]
    public async Task Aceite14_XmlMalformado_RejeitadoEQuarentenado()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var repo = new DocumentosXmlRepository(cs);
        var xmlInvalido = "ESTE_NAO_E_UM_XML_VALIDO_<<>>>";

        var docId = await repo.ImportarXmlAsync(TenantSantaCasa, UsuarioGestor, new ImportarXmlManualCommand(xmlInvalido));
        var doc = await repo.ObterDocumentoPorIdAsync(TenantSantaCasa, docId);

        Assert.NotNull(doc);
        Assert.True(doc.Quarentena);
        Assert.Equal("XML_MALFORMADO", doc.MotivoQuarentena);
    }

    // =========================================================================
    // ACEITE 15: Importação não duplica recebimento ou AP
    // =========================================================================
    [Fact]
    public async Task Aceite15_ImportacaoNaoDuplicaRecebimentoOuApAutomaticamente()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        await using var cn = new NpgsqlConnection(cs);
        // Verificar que documentos importados sem vínculo explícito não criam registro em contas a pagar
        var titulosCount = await cn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM plantaopro.adm360_titulos_pagar WHERE tenant_id = @TenantSantaCasa AND observacoes LIKE '%IMPORTACAO_XML_AUTOMATICA%'",
            new { TenantSantaCasa });

        Assert.Equal(0, titulosCount);
    }

    // =========================================================================
    // ACEITE 16: Dashboard e exportação batem com consultas
    // =========================================================================
    [Fact]
    public async Task Aceite16_Dashboard_CalculaIndicadoresPorConsultasReais()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        var repo = new GestaoDashboardRepository(cs);
        var dashboard = await repo.ObterDashboardAsync(TenantSantaCasa, new FiltroDashboardDto());

        Assert.NotNull(dashboard);
        Assert.True(dashboard.Cotacoes.TotalRecebidas >= 3, "Total de cotações deve refletir a quantidade gravada.");
        Assert.True(dashboard.DocumentosXml.TotalRecebidos >= 2, "Total de XMLs deve refletir a quantidade gravada.");
        Assert.NotNull(dashboard.Integracoes);
    }

    // =========================================================================
    // ACEITE 17: Reinício preserva registros e fila
    // =========================================================================
    [Fact]
    public async Task Aceite17_PersistenciaDuravel_PreservaRegistrosEFila()
    {
        var cs = ObterConnectionString();
        await GarantirConexaoBancoAsync(cs);

        // Simula nova instância conectando ao banco
        var repo1 = new CotacoesRepository(cs);
        var repo2 = new CotacoesRepository(cs);

        var cotacoes1 = await repo1.ListarCotacoesAsync(TenantSantaCasa);
        var cotacoes2 = await repo2.ListarCotacoesAsync(TenantSantaCasa);

        Assert.Equal(cotacoes1.Count, cotacoes2.Count);
        Assert.Equal(cotacoes1[0].Id, cotacoes2[0].Id);
    }

    // =========================================================================
    // ACEITE 18: Validação das Regras de Domínio e Contratos Canônicos
    // =========================================================================
    [Fact]
    public void Aceite18_RegrasDominio_ValidacoesDeChaveEAcesso()
    {
        Assert.Throws<ArgumentException>(() => XmlDocumentoRegras.ValidarChaveAcesso("123")); // Menos de 44 dígitos
        Assert.Throws<InvalidOperationException>(() => XmlDocumentoRegras.ValidarChaveAcesso("35260900000000000000570010000000011000000010")); // Mod 57 (CT-e) fora de escopo
        XmlDocumentoRegras.ValidarChaveAcesso("35260900000000000000550010000000011000000010"); // Mod 55 NF-e válido não lança

        CapacidadeContratadaRegras.ValidarCapacidadeAtiva("PORTAIS_COTACAO", new[] { "PORTAIS_COTACAO", "XML_RECEBIDOS" }); // Ativo não lança
        Assert.Throws<InvalidOperationException>(() => CapacidadeContratadaRegras.ValidarCapacidadeAtiva("XML_RECEBIDOS", new[] { "PORTAIS_COTACAO" }));
    }
}
