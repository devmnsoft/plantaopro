using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class DocumentosXmlRepository : Adm360Repository, IDocumentosXmlRepository
{
    private readonly IComprasRepository comprasRepo;
    private readonly Adm360EventService eventos;

    public DocumentosXmlRepository(string connectionString, IComprasRepository? comprasRepo = null, Adm360EventService? eventos = null)
        : base(connectionString)
    {
        this.comprasRepo = comprasRepo ?? new ComprasRepository(connectionString);
        this.eventos = eventos ?? new Adm360EventService(connectionString);
    }

    private static string CalcularSha256(string conteudo) => CalcularSha256(Encoding.UTF8.GetBytes(conteudo));

    // B1: hash sempre calculado sobre os BYTES exatos (hex minúsculo), fonte da verdade para ETag/download.
    private static string CalcularSha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    // B1: nomes locais que identificam a família NFS-e (varia por prefeitura; escopo do parse).
    private static readonly HashSet<string> NfseRaizLocalNames = new(StringComparer.Ordinal)
        { "infNFS", "nfs", "proNFS", "retProNFS", "procNFS", "ideNFS", "infServico" };

    private static XElement? DescByLocalName(XElement? scope, params string[] localNames)
        => scope is null ? null : scope.Descendants().FirstOrDefault(e => localNames.Contains(e.Name.LocalName));

    private static string LimparCnpj(string? doc) => string.IsNullOrWhiteSpace(doc) ? string.Empty : Regex.Replace(doc, @"\D", "");

    public async Task<IReadOnlyList<DocumentoRecebidoResumoDto>> ListarDocumentosAsync(Guid tenantId, string? status = null, bool? quarentena = null, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var sql = @"
            SELECT id, chave_acesso, numero, serie, modelo, data_emissao,
                   emitente_cnpj, emitente_nome, destinatario_cnpj, destinatario_nome,
                   valor_total, tipo_documento, status_manifestacao, status_conferencia,
                   quarentena, motivo_quarentena, origem, created_at,
                   pedido_id, recebimento_id, titulo_pagar_id, nome_arquivo
            FROM plantaopro.adm360_documentos_recebidos
            WHERE tenant_id = @tenantId
              AND (@status IS NULL OR status_conferencia = @status)
              AND (@quarentena::boolean IS NULL OR quarentena = @quarentena::boolean)
            ORDER BY data_emissao DESC, created_at DESC";

        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(sql, new { tenantId, status, quarentena }, cancellationToken: ct));

        return rows.Select(r => new DocumentoRecebidoResumoDto(
            (Guid)r.id,
            (string)r.chave_acesso,
            (string)r.numero,
            (string)r.serie,
            (string)r.modelo,
            (DateTime)r.data_emissao,
            (string)r.emitente_cnpj,
            (string)r.emitente_nome,
            (string)r.destinatario_cnpj,
            (string)r.destinatario_nome,
            (decimal)r.valor_total,
            (string)r.tipo_documento,
            (string)r.status_manifestacao,
            (string)r.status_conferencia,
            (bool)r.quarentena,
            (string?)r.motivo_quarentena,
            (string)r.origem,
            (DateTime)r.created_at,
            r.pedido_id is not null ? (Guid?)r.pedido_id : null,
            r.recebimento_id is not null ? (Guid?)r.recebimento_id : null,
            r.titulo_pagar_id is not null ? (Guid?)r.titulo_pagar_id : null,
            (string?)r.nome_arquivo
        )).ToList();
    }

    public async Task<DocumentoRecebidoDetalhesDto?> ObterDocumentoPorIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var d = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT d.id, d.estabelecimento_id, e.razao_social AS estabelecimento_nome,
                   d.chave_acesso, d.numero, d.serie, d.modelo, d.data_emissao,
                   d.emitente_cnpj, d.emitente_nome, d.destinatario_cnpj, d.destinatario_nome,
                   d.valor_total, d.valor_produtos, d.tipo_documento, d.status_manifestacao,
                   d.status_conferencia, d.pedido_id, p.numero AS pedido_numero,
                   d.recebimento_id, d.titulo_pagar_id, d.xml_conteudo, d.xml_hash,
                   d.nsu, d.quarentena, d.motivo_quarentena, d.origem, d.created_at, d.nome_arquivo
            FROM plantaopro.adm360_documentos_recebidos d
            JOIN plantaopro.adm360_estabelecimentos e ON e.id = d.estabelecimento_id AND e.tenant_id = d.tenant_id
            LEFT JOIN plantaopro.adm360_pedidos p ON p.id = d.pedido_id AND p.tenant_id = d.tenant_id
            WHERE d.id = @id AND d.tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (d is null) return null;

        var itensRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT i.id, i.numero_item, i.codigo_produto_emitente, i.descricao_produto_emitente,
                   i.ncm, i.cfop, i.unidade_comercial, i.quantidade_comercial, i.valor_unitario,
                   i.valor_total, i.produto_id, pr.nome AS produto_nome, i.unidade_interna,
                   i.fator_conversao, i.quantidade_convertida, i.conferido
            FROM plantaopro.adm360_documento_itens i
            LEFT JOIN plantaopro.adm360_produtos pr ON pr.id = i.produto_id AND pr.tenant_id = i.tenant_id
            WHERE i.documento_id = @id AND i.tenant_id = @tenantId
            ORDER BY i.numero_item",
            new { id, tenantId }, cancellationToken: ct));

        var eventosRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT ev.id, ev.tipo_evento, ev.sequencia_evento, ev.descricao_evento,
                   ev.data_evento, ev.protocolo, ev.detalhes, u.nome AS registrado_por_nome
            FROM plantaopro.adm360_documento_eventos ev
            LEFT JOIN plantaopro.usuarios u ON u.id = ev.registrado_por
            WHERE ev.documento_id = @id AND ev.tenant_id = @tenantId
            ORDER BY ev.sequencia_evento, ev.data_evento",
            new { id, tenantId }, cancellationToken: ct));

        var itens = itensRows.Select(i => new DocumentoItemDetalheDto(
            (Guid)i.id,
            (int)i.numero_item,
            (string)i.codigo_produto_emitente,
            (string)i.descricao_produto_emitente,
            (string?)i.ncm,
            (string?)i.cfop,
            (string)i.unidade_comercial,
            (decimal)i.quantidade_comercial,
            (decimal)i.valor_unitario,
            (decimal)i.valor_total,
            i.produto_id is not null ? (Guid?)i.produto_id : null,
            (string?)i.produto_nome,
            (string?)i.unidade_interna,
            (decimal)i.fator_conversao,
            (decimal)i.quantidade_convertida,
            (bool)i.conferido
        )).ToList();

        var eventos = eventosRows.Select(e => new DocumentoEventoDto(
            (Guid)e.id,
            (string)e.tipo_evento,
            (int)e.sequencia_evento,
            (string)e.descricao_evento,
            (DateTime)e.data_evento,
            (string?)e.protocolo,
            (string?)e.detalhes,
            (string?)e.registrado_por_nome
        )).ToList();

        return new DocumentoRecebidoDetalhesDto(
            (Guid)d.id,
            (Guid)d.estabelecimento_id,
            (string)d.estabelecimento_nome,
            (string)d.chave_acesso,
            (string)d.numero,
            (string)d.serie,
            (string)d.modelo,
            (DateTime)d.data_emissao,
            (string)d.emitente_cnpj,
            (string)d.emitente_nome,
            (string)d.destinatario_cnpj,
            (string)d.destinatario_nome,
            (decimal)d.valor_total,
            (decimal)d.valor_produtos,
            (string)d.tipo_documento,
            (string)d.status_manifestacao,
            (string)d.status_conferencia,
            d.pedido_id is not null ? (Guid?)d.pedido_id : null,
            (string?)d.pedido_numero,
            d.recebimento_id is not null ? (Guid?)d.recebimento_id : null,
            d.titulo_pagar_id is not null ? (Guid?)d.titulo_pagar_id : null,
            (string)d.xml_conteudo,
            (string)d.xml_hash,
            (string?)d.nsu,
            (bool)d.quarentena,
            (string?)d.motivo_quarentena,
            (string)d.origem,
            (DateTime)d.created_at,
            itens,
            eventos,
            (string?)d.nome_arquivo
        );
    }

    // B1: bytes originais preservados — fonte da verdade para o download (ETag = SHA-256 dos bytes).
    public async Task<(byte[] Bytes, string Hash, string ChaveAcesso)?> ObterXmlBytesAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var d = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT xml_bytes, xml_hash, chave_acesso
            FROM plantaopro.adm360_documentos_recebidos
            WHERE id = @id AND tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (d is null) return null;

        return (((byte[])d.xml_bytes), (string)d.xml_hash, (string)d.chave_acesso);
    }

    public async Task<ImportarXmlResultadoDto> ImportarXmlAsync(Guid tenantId, Guid usuarioId, ImportarXmlManualCommand command, CancellationToken ct = default)
    {
        // B1: os bytes originais do arquivo são a fonte da verdade (hash, duplicidade e download).
        // Quando o chamador não envia bytes (API JSON clássica), deriva-se UTF-8 do texto.
        byte[] xmlBytes;
        if (command.XmlBytes is { Length: > 0 })
        {
            xmlBytes = command.XmlBytes;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(command.XmlConteudo))
                throw new ArgumentException("Conteúdo do XML não pode ser vazio.");
            xmlBytes = Encoding.UTF8.GetBytes(command.XmlConteudo);
        }

        var xmlHash = CalcularSha256(xmlBytes);
        // A3/G6: nome do arquivo original — conceito explícito separado do documento fiscal (chave de acesso).
        var nomeArquivo = string.IsNullOrWhiteSpace(command.NomeArquivo) ? null : command.NomeArquivo.Trim();

        // A3/G1: o XmlReader lê os BYTES diretamente (BOM e a declaração <?xml encoding="..."?> são
        // respeitados nativamente); o encoding detectado é usado só para o texto de exibição (xml_conteudo).
        // Hash/duplicidade/download continuam sobre os bytes exatos.
        var textoXml = DecodificarTextoXml(xmlBytes, DetectarEncodingXml(xmlBytes));

        XDocument? doc = null;
        // B1: DtdProcessing.Ignore aceita o DOCTYPE presente em arquivos reais da SEFAZ sem buscá-lo/validá-lo
        // (DTD proibida no parse); entidades externas desabilitadas (XXE).
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Ignore,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            MaxCharactersInDocument = 10_000_000 // limite seguro de 10 MB
        };
        try
        {
            using var ms = new MemoryStream(xmlBytes);
            using var reader = XmlReader.Create(ms, settings);
            doc = XDocument.Load(reader);
        }
        catch (Exception)
        {
            doc = null; // arquivo ilegível -> unidade única em quarentena XML_MALFORMADO
        }

        // A3/G2: um arquivo pode conter VÁRIOS documentos — todas as unidades <infNFe> (NF-e/NFC-e),
        // senão as raízes ABRASF (prescrição eletrônica), senão as raízes EXTERNAS NFS-e.
        // Prioridade: infNFe > ABRASF > NFS-e.
        var unidades = new List<UnidadeXml>();
        if (doc?.Root is null)
        {
            unidades.Add(UnidadeMalFormada(xmlHash));
        }
        else
        {
            foreach (var el in DescobrirUnidades(doc))
                unidades.Add(el.Name.LocalName == "infNFe"
                    ? ExtrairUnidadeNfe(el, xmlHash)
                    : AbrsfLocalNames.Contains(el.Name.LocalName)
                        ? ExtrairUnidadeAbrsf(el, xmlHash)
                        : ExtrairUnidadeNfse(el, xmlHash));
            if (unidades.Count == 0)
                unidades.Add(UnidadeMalFormada(xmlHash)); // raiz não reconhecida por nenhuma família
        }

        // Verifica estabelecimentos autorizados do Tenant (uma vez para o arquivo inteiro)
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        var estabelecimentos = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT id, cnpj, razao_social FROM plantaopro.adm360_estabelecimentos
            WHERE tenant_id = @tenantId AND ativo = true",
            new { tenantId }, cancellationToken: ct))).ToList();

        var cnpjsAutorizados = estabelecimentos.Select(e => LimparCnpj((string)e.cnpj)).ToHashSet();
        var primeiroEstab = estabelecimentos.FirstOrDefault();

        var resultados = new List<ImportarXmlDocumentoResultado>(unidades.Count);
        int importados = 0, emQuarentena = 0, duplicadosIgnorados = 0, falhas = 0;
        // G2: duplicidade DENTRO do mesmo arquivo — a 2ª ocorrência da mesma chave de acesso não volta
        // ao banco (torna-se duplicado idempotente referenciando a primeira ocorrência).
        var vistosNoArquivo = new Dictionary<string, Guid?>(StringComparer.Ordinal);

        foreach (var u in unidades)
        {
            var un = u;
            // G4: ABRASF é documento CLÍNICO (não fiscal) -> destinatário NÃO é validado contra o tenant.
            if (!un.Quarentena && un.VerificarDestinatario && !cnpjsAutorizados.Contains(un.DestCnpj))
                un = un with { Quarentena = true, MotivoQuarentena = "DESTINATARIO_NAO_AUTORIZADO" };

            var estabMatch = (!un.Quarentena && un.VerificarDestinatario)
                ? estabelecimentos.FirstOrDefault(e => LimparCnpj((string)e.cnpj) == un.DestCnpj)
                : null;
            var estabelecimentoId = estabMatch?.id != null ? (Guid)estabMatch.id : ((Guid?)primeiroEstab?.id ?? Guid.Empty);

            if (vistosNoArquivo.TryGetValue(un.ChaveAcesso, out var idDaPrimeira))
            {
                resultados.Add(new ImportarXmlDocumentoResultado(idDaPrimeira, un.ChaveAcesso, true, un.Quarentena, un.MotivoQuarentena, null));
                duplicadosIgnorados++;
                continue;
            }

            ImportarXmlDocumentoResultado r;
            try
            {
                r = await ImportarUnidadeAsync(tenantId, usuarioId, un, xmlBytes, textoXml, xmlHash, nomeArquivo, estabelecimentoId, ct);
            }
            catch (Exception ex)
            {
                r = new ImportarXmlDocumentoResultado(null, un.ChaveAcesso, false, un.Quarentena, un.MotivoQuarentena, ex.Message);
                falhas++;
            }

            vistosNoArquivo[un.ChaveAcesso] = r.DocumentoId;
            resultados.Add(r);
            if (r.DuplicadoIdempotente) duplicadosIgnorados++;
            else if (r.DocumentoId.HasValue)
            {
                if (un.Quarentena) emQuarentena++; else importados++;
            }
        }

        return new ImportarXmlResultadoDto(unidades.Count, importados, emQuarentena, duplicadosIgnorados, falhas, resultados);
    }

    // A3: uma unidade = um documento fiscal dentro do arquivo (infNFe, raiz ABRASF ou raiz externa NFS-e).
    private sealed record UnidadeXml(
        string TipoDocumento,
        string ChaveAcesso,
        string Numero,
        string Serie,
        string Modelo,
        DateTime DataEmissao,
        string EmitCnpj,
        string EmitNome,
        string DestCnpj,
        string DestNome,
        decimal ValorTotal,
        decimal ValorProdutos,
        bool VerificarDestinatario,
        XElement? InfNFe,
        bool Quarentena = false,
        string? MotivoQuarentena = null);

    // A3/G1: encoding do XML detectado por BOM e, na ausência, pela declaração <?xml ... encoding="..."?>.
    private static Encoding DetectarEncodingXml(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) return Encoding.UTF8;
        if (bytes.Length >= 2 && ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF))) return Encoding.Unicode;
        if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0x00 && bytes[3] == 0xFF) return Encoding.UTF32;

        var lim = Math.Min(bytes.Length, 2048);
        var cabeca = Encoding.Latin1.GetString(bytes, 0, lim);
        var m = Regex.Match(cabeca, @"encoding\s*=\s*[\""']([A-Za-z0-9._\-]+)[\""']", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            try { return Encoding.GetEncoding(m.Groups[1].Value); }
            catch (ArgumentException) { /* codepage não registrada no runtime -> padrão */ }
        }
        return Encoding.UTF8;
    }

    // Texto de exibição (xml_conteudo): encoding detectado com fallback UTF-8 -> Latin-1 (nunca falha).
    // TrimStart(U+FEFF): Trim() sozinho NÃO remove o BOM (não é IsWhiteSpace).
    private static string DecodificarTextoXml(byte[] bytes, Encoding preferido)
    {
        foreach (var enc in new[] { preferido, Encoding.UTF8, Encoding.Latin1 })
        {
            try { return enc.GetString(bytes).TrimStart('\uFEFF').Trim(); }
            catch (ArgumentException) { /* bytes inválidos para este encoding -> próximo fallback */ }
        }
        return Encoding.Latin1.GetString(bytes).TrimStart('\uFEFF').Trim();
    }

    // A3/G4: famílias reconhecidas de prescrição eletrônica ABRASF.
    private static readonly HashSet<string> AbrsfLocalNames = new(StringComparer.Ordinal) { "ABRASF", "pRes" };

    // A3/G2: identifica as unidades de documento do arquivo (prioridade infNFe > ABRASF > NFS-e externa).
    private static IReadOnlyList<XElement> DescobrirUnidades(XDocument doc)
    {
        var infNfes = doc.Descendants().Where(e => e.Name.LocalName == "infNFe").ToList();
        if (infNfes.Count > 0) return infNfes;

        // A família é procurada na raiz e nos descendentes (XDocument.Descendants() JÁ inclui a raiz uma única vez).
        var todos = doc.Descendants().ToList();
        var abrExternos = todos.Where(e => AbrsfLocalNames.Contains(e.Name.LocalName))
                               .Where(e => !e.Ancestors().Any(a => AbrsfLocalNames.Contains(a.Name.LocalName))).ToList();
        if (abrExternos.Count > 0) return abrExternos;

        var nfseExternos = todos.Where(e => NfseRaizLocalNames.Contains(e.Name.LocalName))
                                .Where(e => !e.Ancestors().Any(a => NfseRaizLocalNames.Contains(a.Name.LocalName))).ToList();
        return nfseExternos;
    }

    // Arquivo ilegível ou raiz não reconhecida: unidade única em quarentena XML_MALFORMADO com chave
    // sintética DETERMINÍSTICA por conteúdo (reimportar o mesmo arquivo corrompido é idempotente).
    private static UnidadeXml UnidadeMalFormada(string xmlHash) => new(
        "NFE_COMPLETA",
        "MALF" + xmlHash.Substring(0, 33),
        "0", "1", "55", DateTime.UtcNow,
        "00000000000000", "Emitente Não Identificado", "00000000000000", "Destinatário Não Identificado",
        0m, 0m, true, null,
        true, "XML_MALFORMADO");

    // A3/G3: NF-e/NFC-e (<infNFe>). Taxonomia de quarentena:
    //   DOCUMENTO_INCOMPLETO  -> seção obrigatória ausente (ide/emit/dest/total) OU chave ausente/<44 dígitos;
    //   MODELO_NAO_SUPORTADO  -> mod da chave fora de 55/65 OU <ide mod> divergente da chave.
    private static UnidadeXml ExtrairUnidadeNfe(XElement infNFe, string xmlHash)
    {
        var ns = infNFe.GetDefaultNamespace();
        XElement? Elem(string nome) =>
            infNFe.Element(ns + nome) ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == nome);
        string? Val(XElement? escopo, string nome) =>
            escopo is null ? null
            : (escopo.Element(ns + nome) ?? escopo.Elements().FirstOrDefault(e => e.Name.LocalName == nome))?.Value;

        // O atributo Id pode vir prefixado com NFe/NFCe (convenção comum em exportadores/portais).
        var chaveBruta = (string?)infNFe.Attribute("Id") ?? string.Empty;
        if (chaveBruta.StartsWith("NFCe", StringComparison.OrdinalIgnoreCase)) chaveBruta = chaveBruta.Substring(4);
        else if (chaveBruta.StartsWith("NFe", StringComparison.OrdinalIgnoreCase)) chaveBruta = chaveBruta.Substring(3);
        var chaveAcesso = Regex.Replace(chaveBruta, @"\D", "");

        var ide = Elem("ide");
        var emit = Elem("emit");
        var dest = Elem("dest");
        var total = Elem("total");
        var icmsTot = total is null ? null
            : (total.Element(ns + "ICMSTot") ?? total.Elements().FirstOrDefault(e => e.Name.LocalName == "ICMSTot"));

        var numero = (string?)Val(ide, "nNF") ?? "0";
        var serie = (string?)Val(ide, "serie") ?? "1";
        var modeloXml = (string?)Val(ide, "mod") ?? string.Empty;
        var dhEmiStr = (string?)Val(ide, "dhEmi") ?? (string?)Val(ide, "dEmi");
        var dataEmissao = DateTimeOffset.TryParse(dhEmiStr, out var dOff) ? dOff.UtcDateTime
            : DateTime.TryParse(dhEmiStr, out var dEmi) ? dEmi.ToUniversalTime()
            : DateTime.UtcNow;

        var emitCnpj = LimparCnpj((string?)Val(emit, "CNPJ") ?? (string?)Val(emit, "CPF"));
        var emitNome = (string?)Val(emit, "xNome") ?? "Emitente Não Identificado";
        if (string.IsNullOrEmpty(emitCnpj)) emitCnpj = "00000000000000";
        var destCnpj = LimparCnpj((string?)Val(dest, "CNPJ") ?? (string?)Val(dest, "CPF"));
        var destNome = (string?)Val(dest, "xNome") ?? "Destinatário Não Identificado";
        if (string.IsNullOrEmpty(destCnpj)) destCnpj = "00000000000000";

        decimal.TryParse((string?)Val(icmsTot, "vNF") ?? "0", System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var valorTotal);
        decimal.TryParse((string?)Val(icmsTot, "vProd") ?? "0", System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var valorProdutos);

        // G3: classificação da quarentena (reaproveita a regra canônica do Domain).
        string? motivo = null;
        if (ide is null || emit is null || dest is null || total is null)
        {
            motivo = "DOCUMENTO_INCOMPLETO";
        }
        else
        {
            try
            {
                XmlDocumentoRegras.ValidarChaveAcesso(chaveAcesso);
            }
            catch (Administrativo360BusinessException)
            {
                motivo = "MODELO_NAO_SUPORTADO"; // formato ok, mas mod da chave fora de 55/65
            }
            catch (ArgumentException)
            {
                motivo = "DOCUMENTO_INCOMPLETO"; // chave ausente/curta/com caracteres não numéricos
            }

            if (motivo is null && modeloXml.Length > 0 && modeloXml != chaveAcesso.Substring(20, 2))
                motivo = "MODELO_NAO_SUPORTADO"; // <ide mod> divergente do mod da chave
        }

        string chaveFinal, tipoFinal, modeloFinal;
        if (motivo is null)
        {
            var modChave = chaveAcesso.Substring(20, 2);
            tipoFinal = modChave == "65" ? "NFC_E" : "NFE_COMPLETA";
            modeloFinal = modChave;
            chaveFinal = chaveAcesso;
        }
        else
        {
            tipoFinal = "NFE_COMPLETA";
            modeloFinal = "55";
            // Preserva a chave quando couber na coluna (valor diagnóstico); sintética determinística só sem chave utilizável.
            chaveFinal = chaveAcesso.Length is > 0 and <= 60 ? chaveAcesso : "INC" + xmlHash.Substring(0, 33);
        }

        return new UnidadeXml(tipoFinal, chaveFinal, numero, serie, modeloFinal, dataEmissao,
            emitCnpj, emitNome, destCnpj, destNome, valorTotal, valorProdutos, true, infNFe,
            motivo is not null, motivo);
    }

    // A3/G4: prescrição eletrônica ABRASF — documento CLÍNICO, não fiscal:
    // sem valor monetário (totais zero), sem itens padronizados e o DESTINATÁRIO NÃO É VALIDADO contra o tenant.
    // Extração tolerante por nomes locais conforme layout oficial de prescrição eletrônica ABRASF.
    private static UnidadeXml ExtrairUnidadeAbrsf(XElement raiz, string xmlHash)
    {
        // Chave = dígitos do atributo Id (ABRASF/pRes); sem ele, chave sintética determinística por conteúdo.
        var idBruto = (string?)raiz.Attribute("Id")
            ?? (string?)DescByLocalName(raiz, "pRes")?.Attribute("Id");
        var idDigits = Regex.Replace(idBruto ?? string.Empty, @"\D", "");
        var chaveAcesso = idDigits.Length > 0 ? idDigits : "ABR" + xmlHash.Substring(0, 33);
        if (chaveAcesso.Length > 60) chaveAcesso = chaveAcesso.Substring(0, 60);

        var nNumeroBruto = (string?)DescByLocalName(raiz, "nNumero", "numPrescricao", "numero") ?? string.Empty;
        var numero = Regex.Replace(nNumeroBruto, @"\D", "");

        var dhEmiStr = (string?)DescByLocalName(raiz, "dhEmi", "dataHoraEmissao", "dataEmissao");
        var dataEmissao = DateTimeOffset.TryParse(dhEmiStr, out var dOff) ? dOff.UtcDateTime
            : DateTime.TryParse(dhEmiStr, out var dEmi) ? dEmi.ToUniversalTime()
            : DateTime.UtcNow;

        var emit = DescByLocalName(raiz, "emit");
        var dest = DescByLocalName(raiz, "dest", "tomador", "paciente", "pessoa");
        var emitCnpj = LimparCnpj((string?)DescByLocalName(emit, "CNPJ", "CPF"));
        if (string.IsNullOrEmpty(emitCnpj)) emitCnpj = "00000000000000";
        var emitNome = (string?)DescByLocalName(emit, "xNome") ?? "Emitente Não Identificado";
        var destCnpj = LimparCnpj((string?)DescByLocalName(dest, "CNPJ", "CPF"));
        if (string.IsNullOrEmpty(destCnpj)) destCnpj = "00000000000000";
        var destNome = (string?)DescByLocalName(dest, "xNome") ?? "Destinatário Não Identificado";

        return new UnidadeXml("ABRASF", chaveAcesso, numero.Length > 0 ? numero : "0", "1", "ABR",
            dataEmissao, emitCnpj, emitNome, destCnpj, destNome, 0m, 0m, false, null);
    }

    // A3: NFS-e (modelo 67) — identificação por elementos característicos (layout varia por prefeitura).
    // Documentado: NFS-e não tem quebra de itens, portanto valor_produtos = valor_total (total dos serviços).
    private static UnidadeXml ExtrairUnidadeNfse(XElement nfseScope, string xmlHash)
    {
        var idAttrEl = nfseScope.Descendants().FirstOrDefault(e => e.Attribute("Id") != null);
        var idDigits = Regex.Replace((string?)idAttrEl?.Attribute("Id") ?? string.Empty, @"\D", "");
        var numeroBruto = (string?)DescByLocalName(nfseScope, "numNFS", "nNumero", "nNumeroNFS", "nServico") ?? string.Empty;
        var numeroDigits = Regex.Replace(numeroBruto, @"\D", "");

        string chaveAcesso;
        if (idDigits.Length > 0) chaveAcesso = idDigits;
        else if (numeroDigits.Length > 0) chaveAcesso = numeroDigits;
        else chaveAcesso = "NFSE" + xmlHash.Substring(0, 36); // sintética determinística por conteúdo
        if (chaveAcesso.Length > 60) chaveAcesso = chaveAcesso.Substring(0, 60);

        var ideNfse = DescByLocalName(nfseScope, "ideNFS", "ide");
        var dhEmiNfseStr = (string?)DescByLocalName(ideNfse ?? nfseScope, "dhEmi", "dataHoraEmissao", "dData", "dIni");
        var dataEmissao = DateTimeOffset.TryParse(dhEmiNfseStr, out var dOffNfse) ? dOffNfse.UtcDateTime
            : DateTime.TryParse(dhEmiNfseStr, out var dEmiNfse) ? dEmiNfse.ToUniversalTime()
            : DateTime.UtcNow;

        var emitNfse = DescByLocalName(nfseScope, "emit");
        // Destinatário varia por prefeitura: dest/toma/tomador/dst
        var destNfse = DescByLocalName(nfseScope, "dest", "toma", "tomador", "dst");
        var emitCnpj = LimparCnpj((string?)DescByLocalName(emitNfse, "CNPJ", "CPF"));
        if (string.IsNullOrEmpty(emitCnpj)) emitCnpj = "00000000000000";
        var emitNome = (string?)DescByLocalName(emitNfse, "xNome") ?? "Emitente Não Identificado";
        var destCnpj = LimparCnpj((string?)DescByLocalName(destNfse, "CNPJ", "CPF"));
        if (string.IsNullOrEmpty(destCnpj)) destCnpj = "00000000000000";
        var destNome = (string?)DescByLocalName(destNfse, "xNome") ?? "Destinatário Não Identificado";

        var valoresEl = DescByLocalName(nfseScope, "valores");
        var valorStr = (string?)DescByLocalName(valoresEl, "vServ", "vTotal", "vNF") ?? "0";
        decimal.TryParse(valorStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var valorTotal);

        return new UnidadeXml("NFS_E", chaveAcesso,
            numeroDigits.Length > 0 ? numeroDigits : (idDigits.Length > 0 ? idDigits : "0"),
            "1", "67", dataEmissao, emitCnpj, emitNome, destCnpj, destNome, valorTotal, valorTotal, true, null);
    }

    // A3/G2: grava UMA unidade de documento (transação por unidade; dedup por chave com FOR UPDATE —
    // mesmo arquivo é idempotente por hash/bytes/texto; conteúdo divergente levanta erro de negócio).
    private async Task<ImportarXmlDocumentoResultado> ImportarUnidadeAsync(
        Guid tenantId, Guid usuarioId, UnidadeXml u,
        byte[] xmlBytes, string textoXml, string xmlHash, string? nomeArquivo,
        Guid estabelecimentoId, CancellationToken ct)
    {
        Guid? documentoId = null;
        bool duplicadoIdempotente = false;

        await ExecutarComRetrySerializableAsync(async (c, tx) =>
        {
            // WS-A3: advisory lock determinístico serializa importações concorrentes da MESMA
            // chave de acesso (vencedor único + contrato de idempotência explícito).
            await BloquearChavesDeterministasAsync(c, tx, new[] { $"adm360:docxml:{tenantId:N}:{u.ChaveAcesso}" }, ct);

            // Checagem de duplicidade pela chave de acesso
            var existente = await c.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, xml_hash, xml_bytes, xml_conteudo FROM plantaopro.adm360_documentos_recebidos
                WHERE tenant_id = @tenantId AND chave_acesso = @chaveAcesso
                FOR UPDATE",
                new { tenantId, chaveAcesso = u.ChaveAcesso }, tx, cancellationToken: ct));

            if (existente is not null)
            {
                var existenteHash = (string)existente.xml_hash;
                var existenteBytes = existente.xml_bytes is byte[] b ? b : Array.Empty<byte>();
                var existenteConteudo = ((string?)existente.xml_conteudo)?.Trim();

                // B1: idempotência do MESMO ARQUIVO — compara hash dos bytes, bytes exatos ou texto
                // normalizado (equivalente quando a entrada nova deriva de UTF-8 sem BOM original).
                if (existenteHash == xmlHash
                    || existenteBytes.AsSpan().SequenceEqual(xmlBytes.AsSpan())
                    || existenteConteudo == textoXml)
                {
                    documentoId = (Guid)existente.id;
                    duplicadoIdempotente = true;
                    return;
                }

                throw new Administrativo360BusinessException($"Chave de acesso {u.ChaveAcesso} já cadastrada no tenant com conteúdo XML divergente.");
            }

            // WS-A3: pré-validação da identidade fiscal (tenant+emitente+modelo+numero+serie)
            // fora de quarentena (espelha o índice partial; documentos em quarentena trazem
            // identificadores extraídos sem fiabilidade e não bloqueiam importação).
            if (!u.Quarentena)
            {
                var chaveConflito = await c.ExecuteScalarAsync<string>(new CommandDefinition(@"
                    SELECT chave_acesso FROM plantaopro.adm360_documentos_recebidos
                    WHERE tenant_id = @tenantId AND emitente_cnpj = @emitCnpj
                      AND modelo = @modelo AND numero = @numero AND serie = @serie
                      AND quarentena = false
                    LIMIT 1",
                    new { tenantId, emitCnpj = u.EmitCnpj, modelo = u.Modelo, numero = u.Numero, serie = u.Serie },
                    tx, cancellationToken: ct));
                if (chaveConflito is not null)
                    throw new Administrativo360BusinessException(
                        $"Identidade fiscal (emitente {u.EmitCnpj}, modelo {u.Modelo}, número {u.Numero}, série {u.Serie}) já registrada nesta organização pelo documento {chaveConflito}. Revise as duplicidades antes de importar este arquivo.");
            }

            documentoId = Guid.NewGuid();

            try
            {
                var inseriu = await c.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_documentos_recebidos(
                        id, tenant_id, estabelecimento_id, chave_acesso, numero, serie, modelo,
                        data_emissao, emitente_cnpj, emitente_nome, destinatario_cnpj, destinatario_nome,
                        valor_total, valor_produtos, tipo_documento, status_manifestacao, status_conferencia,
                        xml_conteudo, xml_bytes, xml_hash, quarentena, motivo_quarentena, origem, nome_arquivo
                    ) VALUES (
                        @documentoId, @tenantId, @estabelecimentoId, @chaveAcesso, @numero, @serie, @modelo,
                        @dataEmissao, @emitCnpj, @emitNome, @destCnpj, @destNome,
                        @valorTotal, @valorProdutos, @tipoDocumento, 'SEM_MANIFESTACAO',
                        (CASE WHEN @quarentena THEN 'DIVERGENTE' ELSE 'PENDENTE' END),
                        @xmlConteudo, @xmlBytes, @xmlHash, @quarentena, @motivoQuarentena, 'IMPORTACAO_MANUAL', @nomeArquivo
                    )
                    ON CONFLICT (tenant_id, chave_acesso) DO NOTHING",
                    new
                    {
                        documentoId, tenantId, estabelecimentoId, chaveAcesso = u.ChaveAcesso,
                        numero = u.Numero, serie = u.Serie, modelo = u.Modelo,
                        dataEmissao = u.DataEmissao, emitCnpj = u.EmitCnpj, emitNome = u.EmitNome,
                        destCnpj = u.DestCnpj, destNome = u.DestNome,
                        valorTotal = u.ValorTotal, valorProdutos = u.ValorProdutos, tipoDocumento = u.TipoDocumento,
                        xmlConteudo = textoXml, xmlBytes, xmlHash,
                        quarentena = u.Quarentena, motivoQuarentena = u.Quarentena ? u.MotivoQuarentena : null,
                        nomeArquivo
                    }, tx, cancellationToken: ct));

                if (inseriu == 0)
                {
                    // Corrida residual: outra transação commitou esta chave entre a pré-checagem
                    // e o INSERT. Relê o estado visível e segue a MESMA regra de deduplicação.
                    var vencedor = await c.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                        SELECT id, xml_hash, xml_bytes, xml_conteudo
                        FROM plantaopro.adm360_documentos_recebidos
                        WHERE tenant_id = @tenantId AND chave_acesso = @chaveAcesso",
                        new { tenantId, chaveAcesso = u.ChaveAcesso }, tx, cancellationToken: ct));
                    if (vencedor is null)
                        throw new InvalidOperationException($"Chave de acesso {u.ChaveAcesso} foi registrada por outra transação mas não é visível nesta leitura.");

                    var vencedorHash = (string)vencedor.xml_hash;
                    var vencedorBytes = vencedor.xml_bytes is byte[] vb ? vb : Array.Empty<byte>();
                    var vencedorConteudo = ((string?)vencedor.xml_conteudo)?.Trim();
                    if (vencedorHash == xmlHash
                        || vencedorBytes.AsSpan().SequenceEqual(xmlBytes.AsSpan())
                        || vencedorConteudo == textoXml)
                    {
                        documentoId = (Guid)vencedor.id;
                        duplicadoIdempotente = true;
                        return;
                    }

                    throw new Administrativo360BusinessException($"Chave de acesso {u.ChaveAcesso} já cadastrada no tenant com conteúdo XML divergente.");
                }
            }
            catch (NpgsqlException ex) when (ex.SqlState == "23505")
            {
                // Importação concorrente de outra chave com a MESMA identidade fiscal (índice único):
                // o conflito de chave_acesso é absorvido pelo ON CONFLICT DO NOTHING acima, então
                // o 23505 que chega até aqui só pode vir de ux_adm360_docrec_tenant_fiscal.
                throw new Administrativo360BusinessException(
                    $"Identidade fiscal (emitente {u.EmitCnpj}, modelo {u.Modelo}, número {u.Numero}, série {u.Serie}) registrada concorrentemente por outro documento nesta organização. Chave do arquivo importado: {u.ChaveAcesso}. Revise as duplicidades e reimporte. Detalhe do banco: {ex.Message}");
            }

            // Itens <det> se documento NF-e/NFC-e válido (A3: ABRASF/NFS-e não trazem itens padronizados)
            if (!u.Quarentena && u.InfNFe is not null)
            {
                var infNFe = u.InfNFe;
                var ns = infNFe.GetDefaultNamespace();
                var dets = infNFe.Elements(ns + "det")
                    .Concat(infNFe.Elements().Where(e => e.Name.LocalName == "det"))
                    .Distinct().ToList();
                int nItem = 1;
                foreach (var det in dets)
                {
                    var prod = det.Element(ns + "prod") ?? det.Elements().FirstOrDefault(e => e.Name.LocalName == "prod");
                    var cProd = (string?)prod?.Element(ns + "cProd") ?? $"ITEM-{nItem}";
                    var xProd = (string?)prod?.Element(ns + "xProd") ?? "Produto";
                    var ncm = (string?)prod?.Element(ns + "NCM");
                    var cfop = (string?)prod?.Element(ns + "CFOP");
                    var uCom = (string?)prod?.Element(ns + "uCom") ?? "UN";
                    var qComStr = (string?)prod?.Element(ns + "qCom") ?? "1";
                    var vUnComStr = (string?)prod?.Element(ns + "vUnCom") ?? "0";
                    var vProdItemStr = (string?)prod?.Element(ns + "vProd") ?? "0";

                    decimal.TryParse(qComStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var qCom);
                    decimal.TryParse(vUnComStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vUnCom);
                    decimal.TryParse(vProdItemStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var vProdItem);

                    // Mapeamento De/Para do produto
                    var dePara = await c.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                        SELECT entidade_interna_id, fator_conversao
                        FROM plantaopro.adm360_mapeamentos_de_para
                        WHERE tenant_id = @tenantId AND tipo_entidade = 'PRODUTO'
                          AND (codigo_externo = @cProd OR lower(descricao_externa) = lower(@xProd))
                          AND situacao = 'ATIVO' LIMIT 1",
                        new { tenantId, cProd, xProd }, tx, cancellationToken: ct));

                    Guid? prodInternoId = dePara?.entidade_interna_id is not null ? (Guid?)dePara.entidade_interna_id : null;
                    decimal fator = dePara?.fator_conversao is not null ? (decimal)dePara.fator_conversao : 1.0000m;
                    decimal qConvertida = CotacaoRegras.CalcularQuantidadeConvertida(qCom, fator);

                    var itemId = Guid.NewGuid();
                    await c.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO plantaopro.adm360_documento_itens(
                            id, tenant_id, documento_id, numero_item, codigo_produto_emitente,
                            descricao_produto_emitente, ncm, cfop, unidade_comercial, quantidade_comercial,
                            valor_unitario, valor_total, produto_id, fator_conversao, quantidade_convertida, conferido
                        ) VALUES (
                            @itemId, @tenantId, @documentoId, @nItem, @cProd,
                            @xProd, @ncm, @cfop, @uCom, @qCom,
                            @vUnCom, @vProdItem, @prodInternoId, @fator, @qConvertida, false
                        )",
                        new
                        {
                            itemId, tenantId, documentoId, nItem, cProd,
                            xProd, ncm, cfop, uCom, qCom,
                            vUnCom, vProdItem, prodInternoId, fator, qConvertida
                        }, tx, cancellationToken: ct));
                }
            }

            // Evento de entrada (WS-A3: sequência calculada MAX+1, nunca fixa).
            var evId = Guid.NewGuid();
            var seqEvento = await ProximaSequenciaEventoDocumentoAsync(c, tx, tenantId, documentoId.Value, ct);
            await c.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_documento_eventos(
                    id, tenant_id, documento_id, tipo_evento, sequencia_evento,
                    descricao_evento, data_evento, detalhes, registrado_por
                ) VALUES (
                    @evId, @tenantId, @documentoId, 'IMPORTACAO_MANUAL', @seqEvento,
                    'Documento fiscal importado manualmente para o módulo Administrativo 360', now(),
                    @detalhes, @usuarioId
                )",
                new
                {
                    evId, tenantId, documentoId, seqEvento,
                    detalhes = u.Quarentena ? $"Quarentena: {u.MotivoQuarentena}" : "Arquivo XML validado e persistido com sucesso.",
                    usuarioId
                }, tx, cancellationToken: ct));

            // P4: evento DECLARACAO_MANUAL — registro imutável da declaração do documento, na mesma transação
            await eventos.RegistrarAsync(c, tx, tenantId, Adm360TipoEvento.DeclaracaoManual, "DOCUMENTO_XML", documentoId.Value, usuarioId,
                "Declaração manual de documento fiscal (importação XML)",
                new { chave_acesso = u.ChaveAcesso, quarentena = u.Quarentena, motivo_quarentena = u.Quarentena ? u.MotivoQuarentena : null },
                $"declaracao:documento:{documentoId:N}", ct);
        }, ct);

        return new ImportarXmlDocumentoResultado(documentoId, u.ChaveAcesso, duplicadoIdempotente, u.Quarentena, u.Quarentena ? u.MotivoQuarentena : null, null);
    }

    // A3/G5: conferência autorizada — habilita o uso do documento no gate de recebimento de estoque
    // (ComprasRepository.ReceberAsync). Idempotente: CONFERIDO/VINCULADO são aceitos em silêncio.
    public async Task ConferirDocumentoAsync(Guid tenantId, Guid usuarioId, ConferirDocumentoCommand command, CancellationToken ct = default)
    {
        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var doc = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, chave_acesso, quarentena, status_conferencia
                FROM plantaopro.adm360_documentos_recebidos
                WHERE id = @DocumentoId AND tenant_id = @tenantId
                FOR UPDATE",
                new { command.DocumentoId, tenantId }, tx, cancellationToken: ct));

            if (doc is null)
                throw new KeyNotFoundException("Documento fiscal não encontrado.");

            if ((bool)doc.quarentena)
                throw new Administrativo360BusinessException("Documento em quarentena não pode ser conferido.");

            var statusAnterior = (string)doc.status_conferencia;
            if (statusAnterior == "CONFERIDO" || statusAnterior == "VINCULADO")
                return; // já aprovado anteriormente (idempotente)

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_documentos_recebidos
                SET status_conferencia = 'CONFERIDO', updated_at = now()
                WHERE id = @DocumentoId AND tenant_id = @tenantId",
                new { command.DocumentoId, tenantId }, tx, cancellationToken: ct));

            // WS-A3: sequência calculada MAX+1 (documento já está com FOR UPDATE acima).
            var evId = Guid.NewGuid();
            var seqEvento = await ProximaSequenciaEventoDocumentoAsync(cn, tx, tenantId, command.DocumentoId, ct);
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_documento_eventos(
                    id, tenant_id, documento_id, tipo_evento, sequencia_evento,
                    descricao_evento, data_evento, detalhes, registrado_por
                ) VALUES (
                    @evId, @tenantId, @DocumentoId, 'CONFIRMACAO_CONFERENCIA', @seqEvento,
                    'Documento conferido manualmente pelo gestor (conferência autorizada)', now(),
                    'Conferência autorizada habilita o uso deste documento no fluxo de recebimento físico/estoque.', @usuarioId
                )",
                new { evId, tenantId, command.DocumentoId, seqEvento, usuarioId }, tx, cancellationToken: ct));

            await eventos.RegistrarAsync(cn, tx, tenantId, Adm360TipoEvento.ConfirmacaoConferencia, "DOCUMENTO_XML", command.DocumentoId, usuarioId,
                "Confirmação de conferência autorizada de documento fiscal recebido",
                new { chave_acesso = (string)doc.chave_acesso, status_anterior = statusAnterior, status_novo = "CONFERIDO" },
                $"conferencia:documento:{command.DocumentoId:N}", ct);
        }, ct);
    }


    public async Task ManifestarDocumentoAsync(Guid tenantId, Guid usuarioId, ManifestarDocumentoCommand command, CancellationToken ct = default)
    {
        var doc = await ObterDocumentoPorIdAsync(tenantId, command.DocumentoId, ct);
        if (doc is null)
            throw new KeyNotFoundException("Documento fiscal não encontrado.");

        // Regra de aceite 15 e requisito de manifesto:
        // Ação oficial exige credencial real/certificado configurado. Sem certificado, permanece indisponível com motivo real.
        throw new Administrativo360BusinessException("Não é possível manifestar à SEFAZ: Certificado Digital A1 ICP-Brasil não configurado para o estabelecimento.");
    }

    public async Task VincularRecebimentoAsync(Guid tenantId, Guid usuarioId, VincularDocumentoRecebimentoCommand command, CancellationToken ct = default)
    {
        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var doc = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, chave_acesso, numero, valor_total, quarentena, status_conferencia,
                       recebimento_id, pedido_id, titulo_pagar_id
                FROM plantaopro.adm360_documentos_recebidos
                WHERE id = @DocumentoId AND tenant_id = @tenantId
                FOR UPDATE",
                new { command.DocumentoId, tenantId }, tx, cancellationToken: ct));

            if (doc is null)
                throw new KeyNotFoundException("Documento fiscal não encontrado.");

            if ((bool)doc.quarentena)
                throw new Administrativo360BusinessException("Documento em quarentena não pode ser vinculado a recebimento físico.");

            if (doc.recebimento_id is not null)
                throw new Administrativo360BusinessException("Este documento fiscal já está vinculado a um recebimento físico confirmado.");

            var pedido = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, numero, fornecedor_id, situacao
                FROM plantaopro.adm360_pedidos
                WHERE id = @PedidoId AND tenant_id = @tenantId
                FOR UPDATE",
                new { command.PedidoId, tenantId }, tx, cancellationToken: ct));

            if (pedido is null)
                throw new KeyNotFoundException("Pedido de compra não encontrado.");

            // Verifica se o recebimento com esta chave/documento já existe
            var recebimentoExistente = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id FROM plantaopro.adm360_recebimentos
                WHERE tenant_id = @tenantId AND (documento = @chave OR pedido_id = @pedidoId)
                LIMIT 1",
                new { tenantId, chave = (string)doc.chave_acesso, pedidoId = command.PedidoId }, tx, cancellationToken: ct));

            Guid recebimentoId;
            if (recebimentoExistente is not null)
            {
                recebimentoId = (Guid)recebimentoExistente.id;
            }
            else
            {
                // Cria recebimento físico e gera o contas a pagar automaticamente (atômico)
                recebimentoId = Guid.NewGuid();
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_recebimentos(
                        id, tenant_id, pedido_id, documento, idempotency_key, created_at, confirmado_em, created_by
                    ) VALUES (
                        @recebimentoId, @tenantId, @pedidoId, @documento, @idemp, now(), now(), @usuarioId
                    )",
                    new
                    {
                        recebimentoId, tenantId, pedidoId = command.PedidoId,
                        documento = (string)doc.numero, idemp = command.IdempotencyKey ?? $"REC-NFE-{doc.chave_acesso}",
                        usuarioId
                    }, tx, cancellationToken: ct));

                // Atualiza situação do pedido com a MESMA regra canônica de ComprasRepository.RegistrarRecebimentoAsync:
                // CHECK de adm360_pedidos só aceita RASCUNHO|APROVADO|PARCIAL|RECEBIDO|CANCELADO (RECEBIDO_TOTAL inexistia).
                // Recebimento parcial mantém PARCIAL até as quantidades de todos os itens estarem recebidas.
                await cn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_pedidos p
                    SET situacao = CASE WHEN EXISTS(SELECT 1 FROM plantaopro.adm360_pedido_itens i WHERE i.pedido_id = p.id AND i.quantidade_recebida < i.quantidade) THEN 'PARCIAL' ELSE 'RECEBIDO' END,
                        versao = p.versao + 1
                    WHERE p.id = @pedidoId AND p.tenant_id = @tenantId",
                    new { pedidoId = command.PedidoId, tenantId }, tx, cancellationToken: ct));
            }

            // Busca título a pagar gerado ou existente para o pedido/fornecedor
            var tituloPagarId = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition(@"
                SELECT id FROM plantaopro.adm360_titulos_pagar
                WHERE tenant_id = @tenantId AND origem_tipo = 'RECEBIMENTO_COMPRA' AND origem_id = @recebimentoId
                LIMIT 1",
                new { tenantId, recebimentoId }, tx, cancellationToken: ct));

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_documentos_recebidos
                SET pedido_id = @PedidoId,
                    recebimento_id = @recebimentoId,
                    titulo_pagar_id = @tituloPagarId,
                    status_conferencia = 'VINCULADO',
                    updated_at = now()
                WHERE id = @DocumentoId AND tenant_id = @tenantId",
                new { command.DocumentoId, tenantId, command.PedidoId, recebimentoId, tituloPagarId }, tx, cancellationToken: ct));

            // Registra evento de vinculação (WS-A3: sequência calculada MAX+1).
            var evId = Guid.NewGuid();
            var seqVinculo = await ProximaSequenciaEventoDocumentoAsync(cn, tx, tenantId, command.DocumentoId, ct);
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_documento_eventos(
                    id, tenant_id, documento_id, tipo_evento, sequencia_evento,
                    descricao_evento, data_evento, detalhes, registrado_por
                ) VALUES (
                    @evId, @tenantId, @DocumentoId, 'VINCULACAO_RECEBIMENTO', @seqVinculo,
                    'Documento conferido e vinculado com sucesso ao Pedido e Recebimento Físico', now(),
                    'Vínculo aprovado sem duplicar contas a pagar nem criar estoque fantasma.', @usuarioId
                )",
                new { evId, tenantId, command.DocumentoId, seqVinculo, usuarioId }, tx, cancellationToken: ct));
        }, ct);
    }

    public async Task<IReadOnlyList<DfeSincronizacaoDto>> ListarSincronizacoesAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var sql = @"
            SELECT s.id, s.estabelecimento_id, e.razao_social AS estabelecimento_nome,
                   s.cnpj, s.ambiente, s.provedor, s.ultimo_nsu, s.max_nsu,
                   s.data_consulta, s.proxima_consulta_permitida, s.status, s.mensagem_erro
            FROM plantaopro.adm360_dfe_sincronizacoes s
            JOIN plantaopro.adm360_estabelecimentos e ON e.id = s.estabelecimento_id AND e.tenant_id = s.tenant_id
            WHERE s.tenant_id = @tenantId
            ORDER BY s.cnpj";

        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(sql, new { tenantId }, cancellationToken: ct));

        return rows.Select(r => new DfeSincronizacaoDto(
            (Guid)r.id,
            (Guid)r.estabelecimento_id,
            (string)r.estabelecimento_nome,
            (string)r.cnpj,
            (string)r.ambiente,
            (string)r.provedor,
            (string)r.ultimo_nsu,
            (string)r.max_nsu,
            r.data_consulta is not null ? (DateTime?)r.data_consulta : null,
            (DateTime)r.proxima_consulta_permitida,
            (string)r.status,
            (string?)r.mensagem_erro
        )).ToList();
    }

    public async Task ExecutarSincronizacaoDfeAsync(Guid tenantId, Guid usuarioId, ExecutarSincronizacaoDfeCommand command, CancellationToken ct = default)
    {
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        var estab = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT id, cnpj, razao_social, ambiente FROM plantaopro.adm360_estabelecimentos
            WHERE id = @EstabelecimentoId AND tenant_id = @tenantId",
            new { command.EstabelecimentoId, tenantId }, cancellationToken: ct));

        if (estab is null)
            throw new KeyNotFoundException("Estabelecimento não encontrado.");

        var sync = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT id, data_consulta, proxima_consulta_permitida, status
            FROM plantaopro.adm360_dfe_sincronizacoes
            WHERE estabelecimento_id = @EstabelecimentoId AND tenant_id = @tenantId",
            new { command.EstabelecimentoId, tenantId }, cancellationToken: ct));

        if (sync is not null)
        {
            DateTime proxima = (DateTime)sync.proxima_consulta_permitida;
            if (DateTime.UtcNow < proxima)
            {
                var restante = proxima - DateTime.UtcNow;
                throw new Administrativo360BusinessException($"Respeito aos limites da SEFAZ: próxima consulta permitida em {proxima:dd/MM/yyyy HH:mm:ss} UTC (aguarde {Math.Ceiling(restante.TotalMinutes)} min).");
            }
        }

        // Sem certificado A1 real configurado: registra tentativa com o motivo real sem sucesso fictício
        var erro = "Certificado Digital A1 ICP-Brasil não configurado para o CNPJ. Comunicação real com a SEFAZ bloqueada.";

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_dfe_sincronizacoes(
                id, tenant_id, estabelecimento_id, cnpj, ambiente,
                data_consulta, proxima_consulta_permitida, status, mensagem_erro
            ) VALUES (
                gen_random_uuid(), @tenantId, @estabId, @cnpj, @ambiente,
                now(), now() + interval '1 hour', 'ERRO', @erro
            ) ON CONFLICT (tenant_id, estabelecimento_id, ambiente) DO UPDATE
            SET data_consulta = now(),
                proxima_consulta_permitida = now() + interval '1 hour',
                status = 'ERRO',
                mensagem_erro = @erro",
            new { tenantId, estabId = command.EstabelecimentoId, cnpj = (string)estab.cnpj, ambiente = (string)estab.ambiente, erro }, cancellationToken: ct));

        throw new Administrativo360BusinessException(erro);
    }
}
