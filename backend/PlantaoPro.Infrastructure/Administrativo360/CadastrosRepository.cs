using Dapper;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class CadastrosRepository : Adm360Repository, ICadastrosRepository
{
    public CadastrosRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<Parceiro360>> ListarParceirosAsync(Guid tenantId, string? busca, bool? fornecedor, CancellationToken ct)
    {
        await using var cn = Connection();
        return (await cn.QueryAsync<Parceiro360>(new CommandDefinition(@"
            SELECT id, nome, documento, fornecedor, ativo, created_at AS CriadoEm
            FROM plantaopro.adm360_parceiros
            WHERE tenant_id = @tenantId
              AND (@busca IS NULL OR nome ILIKE '%' || @busca || '%' OR documento ILIKE '%' || @busca || '%')
              AND (@fornecedor IS NULL OR fornecedor = @fornecedor)
            ORDER BY nome",
            new { tenantId, busca, fornecedor }, cancellationToken: ct))).AsList();
    }

    public async Task<Guid> SalvarParceiroAsync(Guid tenantId, Guid? id, string nome, string? documento, bool fornecedor, bool ativo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome do parceiro é obrigatório.");

        await using var cn = Connection();
        await cn.OpenAsync(ct);

        var normNome = nome.Trim();
        var normDoc = string.IsNullOrWhiteSpace(documento) ? null : documento.Trim();

        // 1. Criação de nova entidade
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            var novoId = Guid.NewGuid();

            // Validação de duplicidade de documento no mesmo tenant
            if (!string.IsNullOrEmpty(normDoc))
            {
                var docExiste = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                    SELECT EXISTS (
                        SELECT 1 FROM plantaopro.adm360_parceiros
                        WHERE tenant_id = @tenantId AND documento = @normDoc
                    )", new { tenantId, normDoc }, cancellationToken: ct));

                if (docExiste)
                    throw new InvalidOperationException("Já existe um parceiro cadastrado com este documento neste cliente.");
            }

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, documento, fornecedor, ativo, created_at)
                VALUES (@novoId, @tenantId, @normNome, @normDoc, @fornecedor, @ativo, now())",
                new { novoId, tenantId, normNome, normDoc, fornecedor, ativo }, cancellationToken: ct));

            return novoId;
        }

        // 2. Atualização de entidade existente - restrita estritamente ao tenant
        var targetId = id.Value;

        var existe = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (
                SELECT 1 FROM plantaopro.adm360_parceiros
                WHERE id = @targetId AND tenant_id = @tenantId
            )", new { targetId, tenantId }, cancellationToken: ct));

        if (!existe)
            throw new KeyNotFoundException("Parceiro não encontrado ou não pertence a este cliente.");

        // Validação de duplicidade com outro ID no mesmo tenant
        if (!string.IsNullOrEmpty(normDoc))
        {
            var docDuplicado = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                SELECT EXISTS (
                    SELECT 1 FROM plantaopro.adm360_parceiros
                    WHERE tenant_id = @tenantId AND documento = @normDoc AND id <> @targetId
                )", new { tenantId, normDoc, targetId }, cancellationToken: ct));

            if (docDuplicado)
                throw new InvalidOperationException("Já existe outro parceiro cadastrado com este documento neste cliente.");
        }

        var affected = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_parceiros
            SET nome = @normNome,
                documento = @normDoc,
                fornecedor = @fornecedor,
                ativo = @ativo
            WHERE id = @targetId AND tenant_id = @tenantId",
            new { targetId, tenantId, normNome, normDoc, fornecedor, ativo }, cancellationToken: ct));

        if (affected == 0)
            throw new KeyNotFoundException("Parceiro não encontrado ou não pertence a este cliente.");

        return targetId;
    }

    public async Task AlternarStatusParceiroAsync(Guid tenantId, Guid id, bool ativo, CancellationToken ct)
    {
        await using var cn = Connection();
        var affected = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_parceiros
            SET ativo = @ativo
            WHERE id = @id AND tenant_id = @tenantId",
            new { tenantId, id, ativo }, cancellationToken: ct));

        if (affected == 0)
            throw new KeyNotFoundException("Parceiro não encontrado ou não pertence a este cliente.");
    }

    public async Task<IReadOnlyList<Produto360>> ListarProdutosAsync(Guid tenantId, string? busca, bool apenasAtivos, CancellationToken ct)
    {
        await using var cn = Connection();
        return (await cn.QueryAsync<Produto360>(new CommandDefinition(@"
            SELECT id, sku, nome, unidade, codigo_barras AS CodigoBarras,
                   controla_lote AS ControlaLote, exige_inspecao AS ExigeInspecao,
                   COALESCE(preco_custo, 0) AS PrecoCusto, ativo
            FROM plantaopro.adm360_produtos
            WHERE tenant_id = @tenantId
              AND (@apenasAtivos = false OR ativo = true)
              AND (@busca IS NULL OR nome ILIKE '%' || @busca || '%' OR sku ILIKE '%' || @busca || '%' OR codigo_barras ILIKE '%' || @busca || '%')
            ORDER BY nome",
            new { tenantId, busca, apenasAtivos }, cancellationToken: ct))).AsList();
    }

    public async Task<Guid> SalvarProdutoAsync(Guid tenantId, Guid? id, string sku, string nome, string unidade, string? codigoBarras, bool controlaLote, bool exigeInspecao, decimal precoCusto, bool ativo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("SKU é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome do produto é obrigatório.");
        if (precoCusto < 0) throw new ArgumentOutOfRangeException(nameof(precoCusto), "O preço de custo não pode ser negativo.");

        var normSku = sku.Trim().ToUpperInvariant();
        var normNome = nome.Trim();
        var normUnidade = string.IsNullOrWhiteSpace(unidade) ? "UN" : unidade.Trim().ToUpperInvariant();
        var normBarras = string.IsNullOrWhiteSpace(codigoBarras) ? null : codigoBarras.Trim();

        await using var cn = Connection();
        await cn.OpenAsync(ct);

        // 1. Criação de novo produto
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            var novoId = Guid.NewGuid();

            var skuExiste = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                SELECT EXISTS (
                    SELECT 1 FROM plantaopro.adm360_produtos
                    WHERE tenant_id = @tenantId AND upper(sku) = @normSku
                )", new { tenantId, normSku }, cancellationToken: ct));

            if (skuExiste)
                throw new InvalidOperationException("Já existe um produto com este SKU neste cliente.");

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_produtos (id, tenant_id, sku, nome, unidade, codigo_barras, controla_lote, exige_inspecao, preco_custo, ativo, created_at)
                VALUES (@novoId, @tenantId, @normSku, @normNome, @normUnidade, @normBarras, @controlaLote, @exigeInspecao, @precoCusto, @ativo, now())",
                new { novoId, tenantId, normSku, normNome, normUnidade, normBarras, controlaLote, exigeInspecao, precoCusto, ativo }, cancellationToken: ct));

            return novoId;
        }

        // 2. Atualização de produto existente
        var targetId = id.Value;

        var atual = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT unidade, controla_lote, exige_inspecao
            FROM plantaopro.adm360_produtos
            WHERE id = @targetId AND tenant_id = @tenantId",
            new { targetId, tenantId }, cancellationToken: ct));

        if (atual is null)
            throw new KeyNotFoundException("Produto não encontrado ou não pertence a este cliente.");

        // Validação de impacto: não permitir alterar unidade de medida, controla_lote ou exige_inspecao se já houver movimentações
        bool unidadeMudou = !string.Equals((string)atual.unidade, normUnidade, StringComparison.OrdinalIgnoreCase);
        bool loteMudou = (bool)atual.controla_lote != controlaLote;
        bool inspecaoMudou = (bool)atual.exige_inspecao != exigeInspecao;

        if (unidadeMudou || loteMudou || inspecaoMudou)
        {
            var temMovimentacao = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                SELECT EXISTS (
                    SELECT 1 FROM plantaopro.adm360_movimentos
                    WHERE produto_id = @targetId AND tenant_id = @tenantId
                )", new { targetId, tenantId }, cancellationToken: ct));

            if (temMovimentacao)
                throw new InvalidOperationException("Não é permitido alterar unidade de medida, controle de lote ou inspeção de produtos com movimentação de estoque já registrada.");
        }

        // Validação de duplicidade de SKU para outro ID
        var skuDuplicado = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (
                SELECT 1 FROM plantaopro.adm360_produtos
                WHERE tenant_id = @tenantId AND upper(sku) = @normSku AND id <> @targetId
            )", new { tenantId, normSku, targetId }, cancellationToken: ct));

        if (skuDuplicado)
            throw new InvalidOperationException("Já existe outro produto com este SKU neste cliente.");

        var affected = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_produtos
            SET sku = @normSku,
                nome = @normNome,
                unidade = @normUnidade,
                codigo_barras = @normBarras,
                controla_lote = @controlaLote,
                exige_inspecao = @exigeInspecao,
                preco_custo = @precoCusto,
                ativo = @ativo
            WHERE id = @targetId AND tenant_id = @tenantId",
            new { targetId, tenantId, normSku, normNome, normUnidade, normBarras, controlaLote, exigeInspecao, precoCusto, ativo }, cancellationToken: ct));

        if (affected == 0)
            throw new KeyNotFoundException("Produto não encontrado ou não pertence a este cliente.");

        return targetId;
    }

    public async Task AlternarStatusProdutoAsync(Guid tenantId, Guid id, bool ativo, CancellationToken ct)
    {
        await using var cn = Connection();
        var affected = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_produtos
            SET ativo = @ativo
            WHERE id = @id AND tenant_id = @tenantId",
            new { tenantId, id, ativo }, cancellationToken: ct));

        if (affected == 0)
            throw new KeyNotFoundException("Produto não encontrado ou não pertence a este cliente.");
    }

    public async Task<IReadOnlyList<Local360>> ListarLocaisAsync(Guid tenantId, string? tipo, bool apenasAtivos, CancellationToken ct)
    {
        await using var cn = Connection();
        return (await cn.QueryAsync<Local360>(new CommandDefinition(@"
            SELECT id, codigo, nome, tipo, ativo
            FROM plantaopro.adm360_locais
            WHERE tenant_id = @tenantId
              AND (@apenasAtivos = false OR ativo = true)
              AND (@tipo IS NULL OR tipo = @tipo)
            ORDER BY nome",
            new { tenantId, tipo, apenasAtivos }, cancellationToken: ct))).AsList();
    }

    public async Task<Guid> SalvarLocalAsync(Guid tenantId, Guid? id, string codigo, string nome, string tipo, bool ativo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("Código do local é obrigatório.");
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome do local é obrigatório.");

        var normCodigo = codigo.Trim().ToUpperInvariant();
        var normNome = nome.Trim();
        var normTipo = (tipo ?? "").Trim().ToUpperInvariant();

        if (normTipo != "INTERNO" && normTipo != "EXTERNO")
            throw new ArgumentException("Tipo de local inválido. O tipo deve ser estritamente 'INTERNO' ou 'EXTERNO'.");

        await using var cn = Connection();
        await cn.OpenAsync(ct);

        // 1. Criação de novo local
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            var novoId = Guid.NewGuid();

            var codigoExiste = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                SELECT EXISTS (
                    SELECT 1 FROM plantaopro.adm360_locais
                    WHERE tenant_id = @tenantId AND upper(codigo) = @normCodigo
                )", new { tenantId, normCodigo }, cancellationToken: ct));

            if (codigoExiste)
                throw new InvalidOperationException("Já existe um local de estoque com este código neste cliente.");

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_locais (id, tenant_id, codigo, nome, tipo, ativo, created_at)
                VALUES (@novoId, @tenantId, @normCodigo, @normNome, @normTipo, @ativo, now())",
                new { novoId, tenantId, normCodigo, normNome, normTipo, ativo }, cancellationToken: ct));

            return novoId;
        }

        // 2. Atualização de local existente
        var targetId = id.Value;

        var existe = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (
                SELECT 1 FROM plantaopro.adm360_locais
                WHERE id = @targetId AND tenant_id = @tenantId
            )", new { targetId, tenantId }, cancellationToken: ct));

        if (!existe)
            throw new KeyNotFoundException("Local de estoque não encontrado ou não pertence a este cliente.");

        // Validação de duplicidade de código para outro ID
        var codigoDuplicado = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (
                SELECT 1 FROM plantaopro.adm360_locais
                WHERE tenant_id = @tenantId AND upper(codigo) = @normCodigo AND id <> @targetId
            )", new { tenantId, normCodigo, targetId }, cancellationToken: ct));

        if (codigoDuplicado)
            throw new InvalidOperationException("Já existe outro local de estoque com este código neste cliente.");

        var affected = await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_locais
            SET codigo = @normCodigo,
                nome = @normNome,
                tipo = @normTipo,
                ativo = @ativo
            WHERE id = @targetId AND tenant_id = @tenantId",
            new { targetId, tenantId, normCodigo, normNome, normTipo, ativo }, cancellationToken: ct));

        if (affected == 0)
            throw new KeyNotFoundException("Local de estoque não encontrado ou não pertence a este cliente.");

        return targetId;
    }

    public async Task<IReadOnlyList<Lote360>> ListarLotesAsync(Guid tenantId, Guid? produtoId, CancellationToken ct)
    {
        await using var cn = Connection();
        return (await cn.QueryAsync<Lote360>(new CommandDefinition(@"
            SELECT l.id, l.produto_id AS ProdutoId, p.nome AS ProdutoNome, l.codigo, l.validade, l.fabricacao
            FROM plantaopro.adm360_lotes l
            JOIN plantaopro.adm360_produtos p ON p.id = l.produto_id AND p.tenant_id = l.tenant_id
            WHERE l.tenant_id = @tenantId
              AND (@produtoId IS NULL OR l.produto_id = @produtoId)
            ORDER BY l.validade NULLS LAST, l.codigo",
            new { tenantId, produtoId }, cancellationToken: ct))).AsList();
    }

    public async Task<Lookups360Bundle> ObterLookupsAsync(Guid tenantId, CancellationToken ct)
    {
        await using var cn = Connection();
        var parceiros = (await cn.QueryAsync<Parceiro360>(new CommandDefinition(@"
            SELECT id, nome, documento, fornecedor, ativo, created_at AS CriadoEm
            FROM plantaopro.adm360_parceiros
            WHERE tenant_id = @tenantId AND ativo = true
            ORDER BY nome",
            new { tenantId }, cancellationToken: ct))).AsList();

        var produtos = (await cn.QueryAsync<Produto360>(new CommandDefinition(@"
            SELECT id, sku, nome, unidade, codigo_barras AS CodigoBarras,
                   controla_lote AS ControlaLote, exige_inspecao AS ExigeInspecao,
                   COALESCE(preco_custo, 0) AS PrecoCusto, ativo
            FROM plantaopro.adm360_produtos
            WHERE tenant_id = @tenantId AND ativo = true
            ORDER BY nome",
            new { tenantId }, cancellationToken: ct))).AsList();

        var locais = (await cn.QueryAsync<Local360>(new CommandDefinition(@"
            SELECT id, codigo, nome, tipo, ativo
            FROM plantaopro.adm360_locais
            WHERE tenant_id = @tenantId AND ativo = true
            ORDER BY nome",
            new { tenantId }, cancellationToken: ct))).AsList();

        var lotes = (await cn.QueryAsync<Lote360>(new CommandDefinition(@"
            SELECT l.id, l.produto_id AS ProdutoId, p.nome AS ProdutoNome, l.codigo, l.validade, l.fabricacao
            FROM plantaopro.adm360_lotes l
            JOIN plantaopro.adm360_produtos p ON p.id = l.produto_id AND p.tenant_id = l.tenant_id
            WHERE l.tenant_id = @tenantId
            ORDER BY l.validade NULLS LAST, l.codigo",
            new { tenantId }, cancellationToken: ct))).AsList();

        // Carregar médicos canônicos de plantaopro.medicos vinculados ao tenant ou globais ativos
        var medicos = (await cn.QueryAsync<Medico360>(new CommandDefinition(@"
            SELECT m.id,
                   coalesce(nullif(m.nome, ''), u.nome, 'Médico') AS Nome,
                   coalesce(nullif(m.codigo, ''), m.cpf, '') AS Documento,
                   (coalesce(m.status, 'ATIVO') = 'ATIVO' AND coalesce(m.reg_status, 'A') = 'A') AS Ativo
            FROM plantaopro.medicos m
            LEFT JOIN plantaopro.usuarios u ON u.id = m.usuario_id
            WHERE (m.tenant_id = @tenantId OR m.tenant_id IS NULL)
              AND coalesce(m.reg_status, 'A') = 'A'
              AND coalesce(m.status, 'ATIVO') = 'ATIVO'
            ORDER BY Nome",
            new { tenantId }, cancellationToken: ct))).AsList();

        var fornecedores = parceiros.Where(p => p.Fornecedor).ToList();
        var hospitais = parceiros.Where(p => !p.Fornecedor).ToList();
        var pagadores = parceiros.Where(p => !p.Fornecedor).ToList();

        return new Lookups360Bundle(
            Parceiros: parceiros,
            Produtos: produtos,
            Locais: locais,
            Lotes: lotes,
            Medicos: medicos,
            Hospitais: hospitais,
            Pagadores: pagadores,
            Fornecedores: fornecedores);
    }
}
