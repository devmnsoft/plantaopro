using Xunit;

namespace PlantaoPro.Tests;

/// <summary>
/// R42 (roadmap r24a1: "Consolidação de scanner-fatos permanentes (regras E4/E5/E6/E8 em um suíte
/// dedicado)"): um único ponto de entrada que executa o conjunto permanente de contratos dos epics
/// E4 (contraste), E5 (formulários), E6 (nome-vs-id) e E8 (textos sem homologação). Os scans pesados
/// de arquivo eram executados em paralelo por 5 classes distintas na suíte integral — o que sobrecarrega
/// disco/CPU justamente sob carga — e agora têm também um ponto de execução unificado em uma única
/// classe sequencial, para rodar a regressão completa dessas regras em uma unidade:
///     dotnet test --filter "FullyQualifiedName~R42ScannersPermanentes"
/// As classes originais continuam intactas como fonte canônica de cada regra: o agregador invoca seus
/// métodos públicos diretamente, SEM copiar nenhuma linha de lógica (não há dois donos do contrato).
/// Plano: docs/evidencias/2026-10-18-rodada-42/r42a0-consolidacao-scanners-permanentes.md.
/// </summary>
public class R42ScannersPermanentesContratoTests
{
    [Fact]
    public void E4_ContrasteZonasEscuras()
    {
        var s = new R26ContrasteZonasEscurasContratoTests();
        s.Scanner_DeContraste_TemControlesPositivoENegativo();
        s.NenhumaView_ExibeTextoMutedSobreZonaEscura();
    }

    [Fact]
    public void E5_FormulariosCadastros_TetoEPatternDoFormato()
    {
        var s = new R30ValidacaoFormulariosCadastrosContratoTests();
        s.Cadastros_ModaisRapidos_TemosTetoEPatternDoFormato();
        s.Cadastros_ProdutosDetalhe_SobretetosCorrigidosParaOTamanhoDaColuna();
    }

    [Fact]
    public void E5_FormulariosOperacionais_TetosEPisos()
    {
        var s = new R31ValidacaoFormulariosOperacionaisContratoTests();
        s.EstoqueOperacional_TetosDaColunaEmRecebimentoColetaEPedido();
        s.FinanceiroEOrçamento_TetosEPisosNosCamposOperacionais();
    }

    [Fact]
    public void E6_LookupsSemCampoDeIdLivre()
    {
        var orcamento = new R32LookupsCampoDeIdContratoTests();
        orcamento.OrcamentoEditar_GetCarregaLookupsComoONovo();
        orcamento.OrcamentoEditar_ViewUsaSelectsENaoMaisInputDeIdLivre();

        var reserva = new R33E6LookupsReservaEPedidoContratoTests();
        reserva.ValeNovo_ReservaAtivaEscolhidaPorNome();
        reserva.DocumentoXml_VinculoEscolhePedidoELocalPorNome();
    }

    [Fact]
    public void E8_TextosSemHomologacao()
    {
        var s = new R25TextosSemHomologacaoContratoTests();
        s.BannerDoAmbienteDeExemplo_UsoTextoSimplesNasQuatroCulturas();
        s.RotasDeCompatibilidade_112_NaoAnunciamHomologacaoAoUsuario();
        s.LiteraisDeDemoRemovidos_NaoAparemEmServicosENenhumaView();
        s.BoletosEEstadosVazios_LinguagemSimplesSemDemo();
        s.AmbienteFiscalNF_e_WhitelistDeDominio_Permanecem();
    }
}
