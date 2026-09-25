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

    public DocumentosXmlRepository(string connectionString, IComprasRepository? comprasRepo = null)
        : base(connectionString)
    {
        this.comprasRepo = comprasRepo ?? new ComprasRepository(connectionString);
    }

    private static string CalcularSha256(string conteudo)
    {
        var bytes = Encoding.UTF8.GetBytes(conteudo);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

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
              AND (@quarentena IS NULL OR quarentena = @quarentena)
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

    public async Task<Guid> ImportarXmlAsync(Guid tenantId, Guid usuarioId, ImportarXmlManualCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.XmlConteudo))
            throw new ArgumentException("Conteúdo do XML não pode ser vazio.");

        var xmlConteudo = command.XmlConteudo.Trim();
        var xmlHash = CalcularSha256(xmlConteudo);

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
        XNamespace ns = XNamespace.None;
        XElement? infNFe = null;

        // 1. Parsing seguro de XML: Proibir DTD e entidades externas para prevenir XXE
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                MaxCharactersInDocument = 10_000_000 // limite seguro de 10 MB
            };

            XDocument doc;
            using (var stringReader = new StringReader(xmlConteudo))
            using (var reader = XmlReader.Create(stringReader, settings))
            {
                doc = XDocument.Load(reader);
            }

            ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;
            infNFe = doc.Descendants(ns + "infNFe").FirstOrDefault()
                ?? doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "infNFe");

            if (infNFe is null)
            {
                quarentena = true;
                motivoQuarentena = "XML_MALFORMADO";
                chaveAcesso = $"MALF{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}".PadRight(44, '0').Substring(0, 44);
            }
            else
            {
                chaveAcesso = (string?)infNFe.Attribute("Id") ?? string.Empty;
                if (chaveAcesso.StartsWith("NFe", StringComparison.OrdinalIgnoreCase))
                    chaveAcesso = chaveAcesso.Substring(3);

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

                var ide = infNFe.Element(ns + "ide") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "ide");
                var emit = infNFe.Element(ns + "emit") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "emit");
                var dest = infNFe.Element(ns + "dest") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "dest");
                var total = infNFe.Element(ns + "total") ?? infNFe.Elements().FirstOrDefault(e => e.Name.LocalName == "total");
                var icmsTot = total?.Element(ns + "ICMSTot") ?? total?.Elements().FirstOrDefault(e => e.Name.LocalName == "ICMSTot");

                numero = (string?)ide?.Element(ns + "nNF") ?? "0";
                serie = (string?)ide?.Element(ns + "serie") ?? "1";
                modelo = (string?)ide?.Element(ns + "mod") ?? "55";
                var dhEmiStr = (string?)ide?.Element(ns + "dhEmi") ?? (string?)ide?.Element(ns + "dEmi");
                dataEmissao = DateTime.TryParse(dhEmiStr, out var dEmi) ? dEmi.ToUniversalTime() : DateTime.UtcNow;

                emitCnpj = LimparCnpj((string?)emit?.Element(ns + "CNPJ") ?? (string?)emit?.Element(ns + "CPF"));
                emitNome = (string?)emit?.Element(ns + "xNome") ?? "Emitente Não Identificado";

                destCnpj = LimparCnpj((string?)dest?.Element(ns + "CNPJ") ?? (string?)dest?.Element(ns + "CPF"));
                destNome = (string?)dest?.Element(ns + "xNome") ?? "Destinatário Não Identificado";

                var vNFStr = (string?)icmsTot?.Element(ns + "vNF") ?? "0";
                var vProdStr = (string?)icmsTot?.Element(ns + "vProd") ?? "0";
                decimal.TryParse(vNFStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out valorTotal);
                decimal.TryParse(vProdStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out valorProdutos);
            }
        }
        catch (Exception)
        {
            quarentena = true;
            motivoQuarentena = "XML_MALFORMADO";
            chaveAcesso = $"MALF{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}".PadRight(44, '0').Substring(0, 44);
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
                SELECT id, xml_hash, xml_conteudo FROM plantaopro.adm360_documentos_recebidos
                WHERE tenant_id = @tenantId AND chave_acesso = @chaveAcesso
                FOR UPDATE",
                new { tenantId, chaveAcesso }, tx, cancellationToken: ct));

            if (existente is not null)
            {
                var existenteHash = (string)existente.xml_hash;
                var existenteConteudo = ((string?)existente.xml_conteudo)?.Trim();

                if (existenteHash == xmlHash || existenteConteudo == xmlConteudo)
                {
                    // Mesmo arquivo: idempotente
                    documentoId = (Guid)existente.id;
                    return;
                }

                // Conteúdo divergente para mesma chave
                throw new InvalidOperationException($"Chave de acesso {chaveAcesso} já cadastrada no tenant com conteúdo XML divergente.");
            }

            documentoId = Guid.NewGuid();

            await c.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_documentos_recebidos(
                    id, tenant_id, estabelecimento_id, chave_acesso, numero, serie, modelo,
                    data_emissao, emitente_cnpj, emitente_nome, destinatario_cnpj, destinatario_nome,
                    valor_total, valor_produtos, tipo_documento, status_manifestacao, status_conferencia,
                    xml_conteudo, xml_hash, quarentena, motivo_quarentena, origem
                ) VALUES (
                    @documentoId, @tenantId, @estabelecimentoId, @chaveAcesso, @numero, @serie, @modelo,
                    @dataEmissao, @emitCnpj, @emitNome, @destCnpj, @destNome,
                    @valorTotal, @valorProdutos, 'NFE_COMPLETA', 'SEM_MANIFESTACAO',
                    (CASE WHEN @quarentena THEN 'DIVERGENTE' ELSE 'PENDENTE' END),
                    @xmlConteudo, @xmlHash, @quarentena, @motivoQuarentena, 'IMPORTACAO_MANUAL'
                )",
                new
                {
                    documentoId, tenantId, estabelecimentoId, chaveAcesso, numero, serie, modelo,
                    dataEmissao, emitCnpj, emitNome, destCnpj, destNome,
                    valorTotal, valorProdutos, quarentena, motivoQuarentena, xmlConteudo, xmlHash
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
        throw new InvalidOperationException("Não é possível manifestar à SEFAZ: Certificado Digital A1 ICP-Brasil não configurado para o estabelecimento.");
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
                throw new InvalidOperationException("Documento em quarentena não pode ser vinculado a recebimento físico.");

            if (doc.recebimento_id is not null)
                throw new InvalidOperationException("Este documento fiscal já está vinculado a um recebimento físico confirmado.");

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

                // Atualiza situação do pedido
                await cn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_pedidos
                    SET situacao = 'RECEBIDO_TOTAL'
                    WHERE id = @pedidoId AND tenant_id = @tenantId",
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
                throw new InvalidOperationException($"Respeito aos limites da SEFAZ: próxima consulta permitida em {proxima:dd/MM/yyyy HH:mm:ss} UTC (aguarde {Math.Ceiling(restante.TotalMinutes)} min).");
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

        throw new InvalidOperationException(erro);
    }
}
