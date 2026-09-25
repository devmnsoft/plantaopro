namespace PlantaoPro.Application.Administrativo360;

// 1. Estabelecimentos e Capacidades
public sealed record EstabelecimentoDto(
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

public sealed record CriarEstabelecimentoCommand(
    string Cnpj,
    string RazaoSocial,
    string? NomeFantasia,
    string? InscricaoEstadual,
    string? Cnae,
    string Ambiente = "HOMOLOGACAO"
);

public sealed record CapacidadeContratadaDto(
    Guid Id,
    string Capacidade,
    bool Habilitado,
    DateTime AtivadoEm,
    string ConfiguracoesJson
);

public sealed record HabilitarCapacidadeCommand(
    string Capacidade,
    bool Habilitado,
    string? ConfiguracoesJson = null
);

// 2. Contas de Portal de Cotação
public sealed record PortalContaDto(
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

public sealed record ConfigurarPortalContaCommand(
    Guid EstabelecimentoId,
    string Provedor,
    string NomeConta,
    string? IdentificadorExterno,
    string? UsuarioAcesso,
    string? SegredoReferencia,
    string Ambiente = "HOMOLOGACAO"
);

// 3. De/Para de Mapeamentos
public sealed record MapeamentoDeParaDto(
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

public sealed record SalvarMapeamentoDeParaCommand(
    Guid? PortalContaId,
    string Provedor,
    string TipoEntidade,
    string CodigoExterno,
    string DescricaoExterna,
    Guid? EntidadeInternaId,
    string EntidadeInternaDescricao,
    decimal FatorConversao = 1.0000m
);

// 4. Cotações e Itens
public sealed record CotacaoResumoDto(
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

public sealed record CotacaoItemDetalheDto(
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

public sealed record CotacaoAnexoDto(
    Guid Id,
    string NomeArquivo,
    long TamanhoBytes,
    string ContentType,
    string Sha256Hash,
    DateTime CriadoEm
);

public sealed record CotacaoDetalhesDto(
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
    IReadOnlyList<CotacaoItemDetalheDto> Itens,
    IReadOnlyList<CotacaoAnexoDto> Anexos
);

public sealed record CapturarCotacaoCommand(
    Guid EstabelecimentoId,
    Guid PortalContaId,
    string Provedor,
    string IdentificadorExterno,
    int RevisaoExterna,
    string? HospitalSolicitante,
    string? PacienteIniciais,
    string? Procedimento,
    DateOnly? DataPrevista,
    DateTime PrazoResposta,
    string Origem,
    string PayloadOriginal,
    IReadOnlyList<CapturarCotacaoItemCommand> Itens,
    IReadOnlyList<CapturarCotacaoAnexoCommand>? Anexos = null,
    string? IdempotencyKey = null
);

public sealed record CapturarCotacaoItemCommand(
    int NumeroItem,
    string? CodigoExterno,
    string DescricaoExterna,
    string? FabricanteExterno,
    string? ModeloExterno,
    decimal QuantidadeSolicitada,
    string UnidadeSolicitada
);

public sealed record CapturarCotacaoAnexoCommand(
    string NomeArquivo,
    long TamanhoBytes,
    string ContentType,
    string Sha256Hash,
    byte[]? Conteudo
);

public sealed record RelacionarItemCotacaoCommand(
    Guid CotacaoItemId,
    Guid? ProdutoId,
    decimal FatorConversao,
    decimal PrecoUnitarioOfertado,
    decimal Desconto,
    string? MaterialOfertado,
    string? JustificativaSubstituicao,
    string StatusRelacionamento,
    string? MotivoNaoAtendimento
);

public sealed record GerarOrcamentoDaCotacaoCommand(
    Guid CotacaoId,
    string? IdempotencyKey = null
);

public sealed record AprovarRespostaCotacaoCommand(
    Guid CotacaoId,
    string? IdempotencyKey = null
);

public sealed record TransmitirRespostaCommand(
    Guid RespostaId
);

// 5. Outbox de Respostas
public sealed record CotacaoRespostaDto(
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

// 6. Central de XML Recebidos (NF-e mod 55)
public sealed record DocumentoRecebidoResumoDto(
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

public sealed record DocumentoItemDetalheDto(
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

public sealed record DocumentoEventoDto(
    Guid Id,
    string TipoEvento,
    int SequenciaEvento,
    string DescricaoEvento,
    DateTime DataEvento,
    string? Protocolo,
    string? Detalhes,
    string? RegistradoPorNome
);

public sealed record DocumentoRecebidoDetalhesDto(
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
    IReadOnlyList<DocumentoItemDetalheDto> Itens,
    IReadOnlyList<DocumentoEventoDto> Eventos
);

public sealed record ImportarXmlManualCommand(
    string XmlConteudo,
    string? NomeArquivo = null
);

public sealed record ManifestarDocumentoCommand(
    Guid DocumentoId,
    string TipoManifestacao,
    string? Justificativa = null
);

public sealed record VincularDocumentoRecebimentoCommand(
    Guid DocumentoId,
    Guid PedidoId,
    Guid LocalId,
    string? IdempotencyKey = null
);

public sealed record DfeSincronizacaoDto(
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

public sealed record ExecutarSincronizacaoDfeCommand(
    Guid EstabelecimentoId
);

// 7. Dashboard Gerencial e Relatórios
public sealed record CotacoesIndicadoresDto(
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

public sealed record XmlIndicadoresDto(
    int TotalRecebidos,
    int Completos,
    int Resumos,
    int Quarentenados,
    int PendentesConferencia,
    int Vinculados
);

public sealed record IntegracoesIndicadoresDto(
    int ContasAtivas,
    int ConexoesBloqueadas,
    int FalhasUltimas24h,
    DateTime? UltimaSincronizacaoGeral
);

public sealed record DashboardAdm360Dto(
    CotacoesIndicadoresDto Cotacoes,
    XmlIndicadoresDto DocumentosXml,
    IntegracoesIndicadoresDto Integracoes,
    DateTime GeradoEm
);

public sealed record FiltroDashboardDto(
    Guid? EstabelecimentoId = null,
    string? Provedor = null,
    DateOnly? DataInicio = null,
    DateOnly? DataFim = null,
    string? Situacao = null
);

// 8. Interfaces de Repositório e Conectores
public interface ICotacoesRepository
{
    Task<IReadOnlyList<EstabelecimentoDto>> ListarEstabelecimentosAsync(Guid tenantId, CancellationToken ct = default);
    Task<Guid> CriarEstabelecimentoAsync(Guid tenantId, Guid usuarioId, CriarEstabelecimentoCommand command, CancellationToken ct = default);
    Task<IReadOnlyList<CapacidadeContratadaDto>> ListarCapacidadesAsync(Guid tenantId, CancellationToken ct = default);
    Task HabilitarCapacidadeAsync(Guid tenantId, Guid usuarioId, HabilitarCapacidadeCommand command, CancellationToken ct = default);

    Task<IReadOnlyList<PortalContaDto>> ListarContasPortalAsync(Guid tenantId, CancellationToken ct = default);
    Task<Guid> ConfigurarContaPortalAsync(Guid tenantId, Guid usuarioId, ConfigurarPortalContaCommand command, CancellationToken ct = default);

    Task<IReadOnlyList<MapeamentoDeParaDto>> ListarMapeamentosAsync(Guid tenantId, string? provedor = null, string? tipoEntidade = null, CancellationToken ct = default);
    Task<Guid> SalvarMapeamentoAsync(Guid tenantId, Guid usuarioId, SalvarMapeamentoDeParaCommand command, CancellationToken ct = default);

    Task<IReadOnlyList<CotacaoResumoDto>> ListarCotacoesAsync(Guid tenantId, string? status = null, string? provedor = null, CancellationToken ct = default);
    Task<CotacaoDetalhesDto?> ObterCotacaoPorIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<Guid> CapturarCotacaoAsync(Guid tenantId, Guid usuarioId, CapturarCotacaoCommand command, CancellationToken ct = default);
    Task RelacionarItemAsync(Guid tenantId, Guid usuarioId, RelacionarItemCotacaoCommand command, CancellationToken ct = default);
    Task<Guid> GerarOrcamentoCirurgicoAsync(Guid tenantId, Guid usuarioId, GerarOrcamentoDaCotacaoCommand command, CancellationToken ct = default);
    Task<Guid> AprovarRespostaAsync(Guid tenantId, Guid usuarioId, AprovarRespostaCotacaoCommand command, CancellationToken ct = default);
    Task<CotacaoRespostaDto?> ObterRespostaPorIdAsync(Guid tenantId, Guid respostaId, CancellationToken ct = default);
    Task<IReadOnlyList<CotacaoRespostaDto>> ListarRespostasAsync(Guid tenantId, string? status = null, CancellationToken ct = default);
    Task TransmitirRespostaAsync(Guid tenantId, Guid usuarioId, TransmitirRespostaCommand command, CancellationToken ct = default);
    Task<(byte[]? Bytes, string Nome, string ContentType)?> ObterAnexoAsync(Guid tenantId, Guid anexoId, CancellationToken ct = default);
}

public interface IDocumentosXmlRepository
{
    Task<IReadOnlyList<DocumentoRecebidoResumoDto>> ListarDocumentosAsync(Guid tenantId, string? status = null, bool? quarentena = null, CancellationToken ct = default);
    Task<DocumentoRecebidoDetalhesDto?> ObterDocumentoPorIdAsync(Guid tenantId, Guid id, CancellationToken ct = default);
    Task<Guid> ImportarXmlAsync(Guid tenantId, Guid usuarioId, ImportarXmlManualCommand command, CancellationToken ct = default);
    Task ManifestarDocumentoAsync(Guid tenantId, Guid usuarioId, ManifestarDocumentoCommand command, CancellationToken ct = default);
    Task VincularRecebimentoAsync(Guid tenantId, Guid usuarioId, VincularDocumentoRecebimentoCommand command, CancellationToken ct = default);

    Task<IReadOnlyList<DfeSincronizacaoDto>> ListarSincronizacoesAsync(Guid tenantId, CancellationToken ct = default);
    Task ExecutarSincronizacaoDfeAsync(Guid tenantId, Guid usuarioId, ExecutarSincronizacaoDfeCommand command, CancellationToken ct = default);
}

public interface IGestaoDashboardRepository
{
    Task<DashboardAdm360Dto> ObterDashboardAsync(Guid tenantId, FiltroDashboardDto filtro, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListarCapacidadesAtivasAsync(Guid tenantId, CancellationToken ct = default);
}

public interface IPortalCotacaoConnector
{
    string Provedor { get; }
    Task<PortalConexaoStatusResult> TestarConexaoAsync(PortalContaDto conta, CancellationToken ct = default);
    Task<IReadOnlyList<CapturarCotacaoCommand>> SincronizarCotacoesNovasAsync(PortalContaDto conta, CancellationToken ct = default);
    Task<EnvioRespostaPortalResult> TransmitirPropostaAsync(PortalContaDto conta, CotacaoRespostaDto resposta, CotacaoDetalhesDto cotacao, CancellationToken ct = default);
}

public sealed record PortalConexaoStatusResult(bool Conectado, string Status, string? Mensagem, DateTime VerificadoEm);
public sealed record EnvioRespostaPortalResult(bool Sucesso, string StatusTransmissao, string? Protocolo, string Mensagem);
