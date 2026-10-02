using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Dapper;
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
                   pedido_id, recebimento_id, titulo_pagar_id
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
            r.titulo_pagar_id is not null ? (Guid?)r.titulo_pagar_id : null
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
                   d.nsu, d.quarentena, d.motivo_quarentena, d.origem, d.created_at
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
            eventos
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

    public async Task<Guid> ImportarXmlAsync(Guid tenantId, Guid usuarioId, ImportarXmlManualCommand command, CancellationToken ct = default)
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
        // Decode UTF-8 preserva U+FEFF se houver BOM; Trim() sozinho NAO remove U+FEFF (nao e IsWhiteSpace).
        // Sem o TrimStart o XmlReader falha ("Dados no nivel raiz invalidos") -> quarentena falsa XML_MALFORMADO.
        var textoXml = Encoding.UTF8.GetString(xmlBytes).TrimStart('\uFEFF').Trim();

        string chaveAcesso = string.Empty;
        string numero = "0";
        string serie = "1";
        string modelo = "55";
        DateTime dataEmissao = DateTime.UtcNow;
        string emitCnpj = "00000000000000";
        string emitNome = "Emitente Não Identificado";
        string destCnpj = "00000000000000";
        string destNome = "Destinatário Não Identificado";
        decimal valorTotal = 0;
        decimal valorProdutos = 0;
        bool quarentena = false;
        string? motivoQuarentena = null;
        string tipoDocumento = "NFE_COMPLETA";
        XNamespace ns = XNamespace.None;
        XElement? infNFe = null;
        XElement? nfseScope = null;

        void AtribuirChaveMalFormada() =>
            chaveAcesso = $"MALF{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}".PadRight(44, '0').Substring(0, 44);

        // 1. Parsing seguro de XML — B1: DtdProcessing.Ignore aceita o DOCTYPE presente em arquivos
        //    reais da SEFAZ sem buscá-lo/validá-lo (DTD proibida no parse); entidades externas desabilitadas (XXE).
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                MaxCharactersInDocument = 10_000_000 // limite seguro de 10 MB
            };

            XDocument doc;
            using (var stringReader = new StringReader(textoXml))
            using (var reader = XmlReader.Create(stringReader, settings))
            {
                doc = XDocument.Load(reader);
            }

            ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            infNFe = doc.Descendants(ns + "infNFe").FirstOrDefault()
                ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "infNFe");
            nfseScope = infNFe is null
                ? doc.Descendants().FirstOrDefault(e => NfseRaizLocalNames.Contains(e.Name.LocalName))
                : null;

            if (infNFe is null && nfseScope is null)
            {
                quarentena = true;
                motivoQuarentena = "XML_MALFORMADO";
                AtribuirChaveMalFormada();
            }
            else if (infNFe is not null)
            {
                // Família NF-e/NFC-e (modelos 55/65): identificada pelo elemento <infNFe>.
                chaveAcesso = (string?)infNFe.Attribute("Id") ?? string.Empty;
                if (chaveAcesso.StartsWith("NFCe", StringComparison.OrdinalIgnoreCase))
                    chaveAcesso = chaveAcesso.Substring(4);
                else if (chaveAcesso.StartsWith("NFe", StringComparison.OrdinalIgnoreCase))
                    chaveAcesso = chaveAcesso.Substring(3);

                var ide = infNFe.Element(ns + "ide") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "ide");
                var emit = infNFe.Element(ns + "emit") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "emit");
                var dest = infNFe.Element(ns + "dest") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "dest");
                var total = infNFe.Element(ns + "total") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "total");
                var icmsTot = total?.Element(ns + "ICMSTot") ?? total?.Elements().FirstOrDefault(e => e.Name.LocalName == "ICMSTot");

                numero = (string?)ide?.Element(ns + "nNF") ?? "0";
                serie = (string?)ide?.Element(ns + "serie") ?? "1";
                var modeloXml = (string?)ide?.Element(ns + "mod") ?? string.Empty;
                modelo = string.IsNullOrEmpty(modeloXml) ? "55" : modeloXml;
                var dhEmiStr = (string?)ide?.Element(ns + "dhEmi") ?? (string?)ide?.Element(ns + "dEmi");
                dataEmissao = DateTimeOffset.TryParse(dhEmiStr, out var dOff) ? dOff.UtcDateTime
                    : DateTime.TryParse(dhEmiStr, out var dEmi) ? dEmi.ToUniversalTime()
                    : DateTime.UtcNow;

                emitCnpj = LimparCnpj((string?)emit?.Element(ns + "CNPJ") ?? (string?)emit?.Element(ns + "CPF"));
                emitNome = (string?)emit?.Element(ns + "xNome") ?? "Emitente Não Identificado";

                destCnpj = LimparCnpj((string?)dest?.Element(ns + "CNPJ") ?? (string?)dest?.Element(ns + "CPF"));
                destNome = (string?)dest?.Element(ns + "xNome") ?? "Destinatário Não Identificado";

                var vNFStr = (string?)icmsTot?.Element(ns + "vNF") ?? "0";
                var vProdStr = (string?)icmsTot?.Element(ns + "vProd") ?? "0";
                decimal.TryParse(vNFStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out valorTotal);
                decimal.TryParse(vProdStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out valorProdutos);

                try
                {
                    XmlDocumentoRegras.ValidarChaveAcesso(chaveAcesso);
                }
                catch (Exception)
                {
                    quarentena = true;
                    motivoQuarentena = "CHAVE_OU_MODELO_INVALIDO";
                    if (chaveAcesso.Length < 44)
                        chaveAcesso = (chaveAcesso + new string('0', 44)).Substring(0, 44);
                }

                if (!quarentena && chaveAcesso.Length >= 22)
                {
                    var modChave = chaveAcesso.Substring(20, 2);
                    if (modeloXml.Length > 0 && modeloXml != modChave)
                    {
                        // Modelo da chave não bate com o modelo declarado em <ide>
                        quarentena = true;
                        motivoQuarentena = "CHAVE_OU_MODELO_INVALIDO";
                    }
                    else
                    {
                        tipoDocumento = modChave == "65" ? "NFC_E" : "NFE_COMPLETA";
                        modelo = modChave;
                    }
                }

                if (chaveAcesso.Length > 60)
                    chaveAcesso = chaveAcesso.Substring(0, 60);
            }
            else
            {
                // Família NFS-e (modelo 67): identificação por elementos característicos
                // (layout varia por prefeitura). Documentado: NFS-e não tem quebra de itens,
                // portanto valor_produtos = valor_total (total dos serviços).
                tipoDocumento = "NFS_E";
                modelo = "67";

                var idAttrEl = nfseScope!.Descendants().FirstOrDefault(e => e.Attribute("Id") != null);
                var idDigits = Regex.Replace((string?)idAttrEl?.Attribute("Id") ?? string.Empty, @"\D", "");
                var numeroBruto = (string?)DescByLocalName(nfseScope, "numNFS", "nNumero", "nNumeroNFS", "nServico") ?? string.Empty;
                var numeroDigits = Regex.Replace(numeroBruto, @"\D", "");

                if (idDigits.Length > 0)
                    chaveAcesso = idDigits;
                else if (numeroDigits.Length > 0)
                    chaveAcesso = numeroDigits;
                else
                    chaveAcesso = "NFSE" + xmlHash.Substring(0, 36); // sintética determinística por conteúdo

                numero = numeroDigits.Length > 0 ? numeroDigits : (idDigits.Length > 0 ? idDigits : "0");
                serie = "1";
                if (chaveAcesso.Length > 60)
                    chaveAcesso = chaveAcesso.Substring(0, 60);

                var ideNfse = DescByLocalName(nfseScope, "ideNFS", "ide");
                var dhEmiNfseStr = (string?)DescByLocalName(ideNfse ?? nfseScope, "dhEmi", "dataHoraEmissao", "dData", "dIni");
                dataEmissao = DateTimeOffset.TryParse(dhEmiNfseStr, out var dOffNfse) ? dOffNfse.UtcDateTime
                    : DateTime.TryParse(dhEmiNfseStr, out var dEmiNfse) ? dEmiNfse.ToUniversalTime()
                    : DateTime.UtcNow;

                var emitNfse = DescByLocalName(nfseScope, "emit");
                // Destinatário varia por prefeitura: dest/toma/tomador/dst
                var destNfse = DescByLocalName(nfseScope, "dest", "toma", "tomador", "dst");
                emitCnpj = LimparCnpj((string?)DescByLocalName(emitNfse, "CNPJ", "CPF"));
                emitNome = (string?)DescByLocalName(emitNfse, "xNome") ?? "Emitente Não Identificado";
                destCnpj = LimparCnpj((string?)DescByLocalName(destNfse, "CNPJ", "CPF"));
                destNome = (string?)DescByLocalName(destNfse, "xNome") ?? "Destinatário Não Identificado";

                var valoresEl = DescByLocalName(nfseScope, "valores");
                var valorStr = (string?)DescByLocalName(valoresEl, "vServ", "vTotal", "vNF") ?? "0";
                decimal.TryParse(valorStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out valorTotal);
                valorProdutos = valorTotal;
            }
        }
        catch (Exception)
        {
            quarentena = true;
            motivoQuarentena = "XML_MALFORMADO";
            AtribuirChaveMalFormada();
        }

        // 3. Verifica estabelecimentos autorizados do Tenant
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        var estabelecimentos = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT id, cnpj, razao_social FROM plantaopro.adm360_estabelecimentos
            WHERE tenant_id = @tenantId AND ativo = true",
            new { tenantId }, cancellationToken: ct))).ToList();

        var cnpjsAutorizados = estabelecimentos.Select(e => LimparCnpj((string)e.cnpj)).ToHashSet();

        Guid estabelecimentoId = Guid.Empty;

        if (!quarentena)
        {
            if (!cnpjsAutorizados.Contains(destCnpj))
            {
                quarentena = true;
                motivoQuarentena = "DESTINATARIO_NAO_AUTORIZADO";
                estabelecimentoId = estabelecimentos.FirstOrDefault()?.id != null ? (Guid)estabelecimentos.First().id : Guid.Empty;
            }
            else
            {
                var estabMatch = estabelecimentos.FirstOrDefault(e => LimparCnpj((string)e.cnpj) == destCnpj);
                estabelecimentoId = estabMatch?.id != null ? (Guid)estabMatch.id : (Guid)estabelecimentos.First().id;
            }
        }
        else
        {
            estabelecimentoId = estabelecimentos.FirstOrDefault()?.id != null ? (Guid)estabelecimentos.First().id : Guid.Empty;
        }

        Guid documentoId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (c, tx) =>
        {
            // Checa duplicidade por chave de acesso
            var existente = await c.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, xml_hash, xml_bytes, xml_conteudo FROM plantaopro.adm360_documentos_recebidos
                WHERE tenant_id = @tenantId AND chave_acesso = @chaveAcesso
                FOR UPDATE",
                new { tenantId, chaveAcesso }, tx, cancellationToken: ct));

            if (existente is not null)
            {
                var existenteHash = (string)existente.xml_hash;
                var existenteBytes = existente.xml_bytes is byte[] b ? b : Array.Empty<byte>();
                var existenteConteudo = ((string?)existente.xml_conteudo)?.Trim();

                // B1: mesmo arquivo é idempotente — compara hash dos bytes, bytes exatos ou texto
                // normalizado (equivale quando a nova entrada não tem BOM/bytes originais).
                if (existenteHash == xmlHash
                    || existenteBytes.AsSpan().SequenceEqual(xmlBytes.AsSpan())
                    || existenteConteudo == textoXml)
                {
                    // Mesmo arquivo: idempotente
                    documentoId = (Guid)existente.id;
                    return;
                }

                // Conteúdo divergente para mesma chave
                throw new Administrativo360BusinessException($"Chave de acesso {chaveAcesso} já cadastrada no tenant com conteúdo XML divergente.");
            }

            documentoId = Guid.NewGuid();

            await c.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_documentos_recebidos(
                    id, tenant_id, estabelecimento_id, chave_acesso, numero, serie, modelo,
                    data_emissao, emitente_cnpj, emitente_nome, destinatario_cnpj, destinatario_nome,
                    valor_total, valor_produtos, tipo_documento, status_manifestacao, status_conferencia,
                    xml_conteudo, xml_bytes, xml_hash, quarentena, motivo_quarentena, origem
                ) VALUES (
                    @documentoId, @tenantId, @estabelecimentoId, @chaveAcesso, @numero, @serie, @modelo,
                    @dataEmissao, @emitCnpj, @emitNome, @destCnpj, @destNome,
                    @valorTotal, @valorProdutos, @tipoDocumento, 'SEM_MANIFESTACAO',
                    (CASE WHEN @quarentena THEN 'DIVERGENTE' ELSE 'PENDENTE' END),
                    @xmlConteudo, @xmlBytes, @xmlHash, @quarentena, @motivoQuarentena, 'IMPORTACAO_MANUAL'
                )",
                new
                {
                    documentoId, tenantId, estabelecimentoId, chaveAcesso, numero, serie, modelo,
                    dataEmissao, emitCnpj, emitNome, destCnpj, destNome,
                    valorTotal, valorProdutos, tipoDocumento,
                    xmlConteudo = textoXml, xmlBytes, xmlHash,
                    quarentena, motivoQuarentena
                }, tx, cancellationToken: ct));

            // Extrai itens <det> se documento válido
            if (!quarentena && infNFe != null)
            {
                var dets = infNFe.Elements(ns + "det").Concat(infNFe.Elements().Where(e => e.Name.LocalName == "det")).Distinct().ToList();
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

                    // Busca De/Para de produto
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

            // Evento de recepção
            var evId = Guid.NewGuid();
            await c.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_documento_eventos(
                    id, tenant_id, documento_id, tipo_evento, sequencia_evento,
                    descricao_evento, data_evento, detalhes, registrado_por
                ) VALUES (
                    @evId, @tenantId, @documentoId, 'IMPORTACAO_MANUAL', 1,
                    'Documento importado manualmente no módulo Administrativo 360', now(),
                    @detalhes, @usuarioId
                )",
                new
                {
                    evId, tenantId, documentoId,
                    detalhes = quarentena ? $"Quarentena: {motivoQuarentena}" : "Arquivo XML validado e gravado com sucesso.",
                    usuarioId
                }, tx, cancellationToken: ct));

            // P4: evento DECLARACAO_MANUAL — declaração manual do documento fiscal registrada imutavelmente na mesma transação
            await eventos.RegistrarAsync(c, tx, tenantId, Adm360TipoEvento.DeclaracaoManual, "DOCUMENTO_XML", documentoId, usuarioId,
                "Declaração manual de documento fiscal (importação XML)",
                new { chave_acesso = chaveAcesso, quarentena, motivo_quarentena = quarentena ? motivoQuarentena : null },
                $"declaracao:documento:{documentoId:N}", ct);
        }, ct);

        return documentoId;
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
                        id, tenant_id, pedido_id, documento, idempotency_key, criado_em, confirmado_em, created_by
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

            // Registra evento de vinculação
            var evId = Guid.NewGuid();
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_documento_eventos(
                    id, tenant_id, documento_id, tipo_evento, sequencia_evento,
                    descricao_evento, data_evento, detalhes, registrado_por
                ) VALUES (
                    @evId, @tenantId, @DocumentoId, 'VINCULACAO_RECEBIMENTO', 2,
                    'Documento conferido e vinculado com sucesso ao Pedido e Recebimento Físico', now(),
                    'Vínculo aprovado sem duplicar contas a pagar nem criar estoque fantasma.', @usuarioId
                )",
                new { evId, tenantId, command.DocumentoId, usuarioId }, tx, cancellationToken: ct));
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
