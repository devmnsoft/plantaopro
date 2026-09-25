namespace PlantaoPro.Web.Models;

// ==========================================
// COTAÇÕES
// ==========================================

public sealed record CotacaoResumoViewModel(
    Guid Id,
    string Provedor,
    string IdentificadorExterno,
    int RevisaoExterna,
    string HospitalNome,
    string? Procedimento,
    DateTime PrazoResposta,
    string StatusInterno,
    string StatusExterno,
    string Origem,
    Guid? OrcamentoId,
    int TotalItens,
    int ItensPendentes,
    DateTime CapturadaEm
);

public sealed record CotacaoItemDetalheViewModel(
    Guid Id,
    int NumeroItem,
    string? CodigoExterno,
    string DescricaoExterna,
    string? FabricanteExterno,
    string? ModeloExterno,
    decimal QuantidadeSolicitada,
    string UnidadeSolicitada,
    Guid? ProdutoId,
    string? ProdutoNome,
    string? UnidadeInterna,
    decimal FatorConversao,
    decimal QuantidadeConvertida,
    decimal PrecoUnitarioOfertado,
    decimal Desconto,
    decimal PrecoTotalOfertado,
    string? MaterialOfertado,
    string? JustificativaSubstituicao,
    string StatusRelacionamento,
    string? MotivoNaoAtendimento
);

public sealed record CotacaoAnexoViewModel(
    Guid Id,
    string NomeArquivo,
    long TamanhoBytes,
    string ContentType,
    string Sha256Hash,
    DateTime CriadoEm
);

public sealed record CotacaoDetalhesViewModel(
    Guid Id,
    Guid EstabelecimentoId,
    string EstabelecimentoNome,
    Guid PortalContaId,
    string Provedor,
    string IdentificadorExterno,
    int RevisaoExterna,
    Guid? HospitalId,
    string? HospitalNome,
    string? HospitalSolicitanteExterno,
    string? PacienteIniciais,
    string? Procedimento,
    DateOnly? DataPrevista,
    DateTime PrazoResposta,
    string FusoHorario,
    string StatusInterno,
    string StatusExterno,
    string Origem,
    Guid? OrcamentoId,
    string? OrcamentoNumero,
    DateTime CapturadaEm,
    IReadOnlyList<CotacaoItemDetalheViewModel> Itens,
    IReadOnlyList<CotacaoAnexoViewModel> Anexos
);

public sealed record CotacaoRespostaViewModel(
    Guid Id,
    Guid CotacaoId,
    string IdentificadorExterno,
    Guid OrcamentoId,
    int Revisao,
    string StatusTransmissao,
    string StatusComercialExterno,
    int Tentativas,
    DateTime ProximaTentativa,
    string? ProtocoloExterno,
    string? MensagemRetorno,
    DateTime CriadoEm,
    DateTime? EnviadoEm
);

public sealed record MapeamentoDeParaViewModel(
    Guid Id,
    Guid? PortalContaId,
    string Provedor,
    string TipoEntidade,
    string CodigoExterno,
    string DescricaoExterna,
    Guid? EntidadeInternaId,
    string EntidadeInternaDescricao,
    decimal FatorConversao,
    string Situacao,
    DateTime CriadoEm
);

public sealed record PortalContaViewModel(
    Guid Id,
    Guid EstabelecimentoId,
    string EstabelecimentoNome,
    string Provedor,
    string NomeConta,
    string? IdentificadorExterno,
    string? UsuarioAcesso,
    string Ambiente,
    string StatusIntegracao,
    string? MotivoBloqueio,
    DateTime? UltimaSincronizacao,
    bool Ativo
);

public sealed record EstabelecimentoViewModel(
    Guid Id,
    string Cnpj,
    string RazaoSocial,
    string? NomeFantasia,
    string? InscricaoEstadual,
    string? Cnae,
    string Ambiente,
    bool Ativo,
    DateTime CriadoEm
);

public sealed class CotacoesIndexViewModel
{
    public IReadOnlyList<CotacaoResumoViewModel> Cotacoes { get; init; } = Array.Empty<CotacaoResumoViewModel>();
    public string? Status { get; init; }
    public string? Provedor { get; init; }
    public string? Erro { get; init; }
}

public sealed class CotacaoDetalhesPageViewModel
{
    public CotacaoDetalhesViewModel Cotacao { get; init; } = default!;
    public IReadOnlyList<CotacaoRespostaViewModel> Respostas { get; init; } = Array.Empty<CotacaoRespostaViewModel>();
    public string? Erro { get; init; }
    public string? Sucesso { get; init; }
}

public sealed class RelacionamentoMapeamentoViewModel
{
    public IReadOnlyList<MapeamentoDeParaViewModel> Mapeamentos { get; init; } = Array.Empty<MapeamentoDeParaViewModel>();
    public string? Provedor { get; init; }
    public string? TipoEntidade { get; init; }
    public string? Erro { get; init; }
}

public sealed class SalvarMapeamentoFormModel
{
    public Guid? PortalContaId { get; set; }
    public string Provedor { get; set; } = "OPMENEXO";
    public string TipoEntidade { get; set; } = "PRODUTO";
    public string CodigoExterno { get; set; } = string.Empty;
    public string DescricaoExterna { get; set; } = string.Empty;
    public Guid? EntidadeInternaId { get; set; }
    public string EntidadeInternaDescricao { get; set; } = string.Empty;
    public decimal FatorConversao { get; set; } = 1.0000m;
}

public sealed class RelacionarItemFormModel
{
    public Guid CotacaoId { get; set; }
    public Guid CotacaoItemId { get; set; }
    public Guid? ProdutoId { get; set; }
    public decimal FatorConversao { get; set; } = 1.0000m;
    public decimal PrecoUnitarioOfertado { get; set; }
    public decimal Desconto { get; set; }
    public string? MaterialOfertado { get; set; }
    public string? JustificativaSubstituicao { get; set; }
    public string StatusRelacionamento { get; set; } = "RELACIONADO";
    public string? MotivoNaoAtendimento { get; set; }
}

public sealed class PortalContasConfigViewModel
{
    public IReadOnlyList<PortalContaViewModel> Contas { get; init; } = Array.Empty<PortalContaViewModel>();
    public IReadOnlyList<EstabelecimentoViewModel> Estabelecimentos { get; init; } = Array.Empty<EstabelecimentoViewModel>();
    public string? Erro { get; init; }
}

public sealed class ConfigurarPortalContaFormModel
{
    public Guid EstabelecimentoId { get; set; }
    public string Provedor { get; set; } = "OPMENEXO";
    public string NomeConta { get; set; } = string.Empty;
    public string? IdentificadorExterno { get; set; }
    public string? UsuarioAcesso { get; set; }
    public string? SegredoReferencia { get; set; }
    public string Ambiente { get; set; } = "HOMOLOGACAO";
}

// ==========================================
// DOCUMENTOS XML RECEBIDOS (NF-E MOD 55)
// ==========================================

public sealed record DocumentoRecebidoResumoViewModel(
    Guid Id,
    string ChaveAcesso,
    string Numero,
    string Serie,
    string Modelo,
    DateTime DataEmissao,
    string EmitenteCnpj,
    string EmitenteNome,
    string DestinatarioCnpj,
    string DestinatarioNome,
    decimal ValorTotal,
    string TipoDocumento,
    string StatusManifestacao,
    string StatusConferencia,
    bool Quarentena,
    string? MotivoQuarentena,
    string Origem,
    DateTime CriadoEm,
    Guid? PedidoId,
    Guid? RecebimentoId,
    Guid? TituloPagarId
);

public sealed record DocumentoItemDetalheViewModel(
    Guid Id,
    int NumeroItem,
    string CodigoProdutoEmitente,
    string DescricaoProdutoEmitente,
    string? Ncm,
    string? Cfop,
    string UnidadeComercial,
    decimal QuantidadeComercial,
    decimal ValorUnitario,
    decimal ValorTotal,
    Guid? ProdutoId,
    string? ProdutoNome,
    string? UnidadeInterna,
    decimal FatorConversao,
    decimal QuantidadeConvertida,
    bool Conferido
);

public sealed record DocumentoEventoViewModel(
    Guid Id,
    string TipoEvento,
    int SequenciaEvento,
    string DescricaoEvento,
    DateTime DataEvento,
    string? Protocolo,
    string? Detalhes,
    string? RegistradoPorNome
);

public sealed record DocumentoRecebidoDetalhesViewModel(
    Guid Id,
    Guid EstabelecimentoId,
    string EstabelecimentoNome,
    string ChaveAcesso,
    string Numero,
    string Serie,
    string Modelo,
    DateTime DataEmissao,
    string EmitenteCnpj,
    string EmitenteNome,
    string DestinatarioCnpj,
    string DestinatarioNome,
    decimal ValorTotal,
    decimal ValorProdutos,
    string TipoDocumento,
    string StatusManifestacao,
    string StatusConferencia,
    Guid? PedidoId,
    string? PedidoNumero,
    Guid? RecebimentoId,
    Guid? TituloPagarId,
    string XmlConteudo,
    string XmlHash,
    string? Nsu,
    bool Quarentena,
    string? MotivoQuarentena,
    string Origem,
    DateTime CriadoEm,
    IReadOnlyList<DocumentoItemDetalheViewModel> Itens,
    IReadOnlyList<DocumentoEventoViewModel> Eventos
);

public sealed record DfeSincronizacaoViewModel(
    Guid Id,
    Guid EstabelecimentoId,
    string EstabelecimentoNome,
    string Cnpj,
    string Ambiente,
    string Provedor,
    string UltimoNsu,
    string MaxNsu,
    DateTime? DataConsulta,
    DateTime ProximaConsultaPermitida,
    string Status,
    string? MensagemErro
);

public sealed class DocumentosXmlIndexViewModel
{
    public IReadOnlyList<DocumentoRecebidoResumoViewModel> Documentos { get; init; } = Array.Empty<DocumentoRecebidoResumoViewModel>();
    public string? Status { get; init; }
    public bool? Quarentena { get; init; }
    public string? Erro { get; init; }
}

public sealed class DocumentoXmlDetalhesPageViewModel
{
    public DocumentoRecebidoDetalhesViewModel Documento { get; init; } = default!;
    public string? Erro { get; init; }
    public string? Sucesso { get; init; }
}

public sealed class ImportarXmlManualFormModel
{
    public string XmlConteudo { get; set; } = string.Empty;
    public string? NomeArquivo { get; set; }
}

public sealed class VincularRecebimentoFormModel
{
    public Guid DocumentoId { get; set; }
    public Guid PedidoId { get; set; }
    public Guid LocalId { get; set; }
}

public sealed class SincronizacaoDfeViewModel
{
    public IReadOnlyList<DfeSincronizacaoViewModel> Sincronizacoes { get; init; } = Array.Empty<DfeSincronizacaoViewModel>();
    public IReadOnlyList<EstabelecimentoViewModel> Estabelecimentos { get; init; } = Array.Empty<EstabelecimentoViewModel>();
    public string? Erro { get; init; }
}

public sealed class EstabelecimentosPageViewModel
{
    public IReadOnlyList<EstabelecimentoViewModel> Estabelecimentos { get; init; } = Array.Empty<EstabelecimentoViewModel>();
    public string? Erro { get; init; }
}

public sealed class CriarEstabelecimentoFormModel
{
    public string Cnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Cnae { get; set; }
    public string Ambiente { get; set; } = "HOMOLOGACAO";
}

// ==========================================
// GESTÃO, DASHBOARD E MONITOR
// ==========================================

public sealed record CotacoesIndicadoresViewModel(
    int TotalRecebidas,
    int AguardandoRelacionamento,
    int ProximasDoPrazo,
    int OrcamentosGerados,
    int RespostasAceitas,
    int PropostasVencedoras,
    int PropostasPerdidas,
    decimal TaxaSucessoPercentual,
    double TempoMedioRespostaHoras,
    int FalhasIntegracao
);

public sealed record XmlIndicadoresViewModel(
    int TotalRecebidos,
    int Completos,
    int Resumos,
    int Quarentenados,
    int PendentesConferencia,
    int Vinculados
);

public sealed record IntegracoesIndicadoresViewModel(
    int ContasAtivas,
    int ConexoesBloqueadas,
    int FalhasUltimas24h,
    DateTime? UltimaSincronizacaoGeral
);

public sealed record DashboardAdm360ViewModel(
    CotacoesIndicadoresViewModel Cotacoes,
    XmlIndicadoresViewModel DocumentosXml,
    IntegracoesIndicadoresViewModel Integracoes,
    DateTime GeradoEm
);

public sealed class DashboardGestaoViewModel
{
    public DashboardAdm360ViewModel Dashboard { get; init; } = default!;
    public Guid? EstabelecimentoId { get; init; }
    public string? Provedor { get; init; }
    public DateOnly? DataInicio { get; init; }
    public DateOnly? DataFim { get; init; }
    public string? Situacao { get; init; }
    public IReadOnlyList<string> CapacidadesAtivas { get; init; } = Array.Empty<string>();
    public string? Erro { get; init; }
}

public sealed class MonitorIntegracoesViewModel
{
    public IReadOnlyList<PortalContaViewModel> ContasPortal { get; init; } = Array.Empty<PortalContaViewModel>();
    public IReadOnlyList<DfeSincronizacaoViewModel> SincronizacoesDfe { get; init; } = Array.Empty<DfeSincronizacaoViewModel>();
    public IReadOnlyList<CotacaoRespostaViewModel> RespostasRecentes { get; init; } = Array.Empty<CotacaoRespostaViewModel>();
    public string? Erro { get; init; }
}
