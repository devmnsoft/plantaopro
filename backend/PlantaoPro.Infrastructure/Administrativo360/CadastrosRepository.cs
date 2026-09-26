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
        var targetId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_parceiros (id, tenant_id, nome, documento, fornecedor, ativo)
            VALUES (@targetId, @tenantId, @nome, @documento, @fornecedor, @ativo)
            ON CONFLICT (id) DO UPDATE
            SET nome = EXCLUDED.nome,
                documento = EXCLUDED.documento,
                fornecedor = EXCLUDED.fornecedor,
                ativo = EXCLUDED.ativo",
            new { targetId, tenantId, nome = nome.Trim(), documento = documento?.Trim(), fornecedor, ativo }, cancellationToken: ct));

        return targetId;
    }

    public async Task AlternarStatusParceiroAsync(Guid tenantId, Guid id, bool ativo, CancellationToken ct)
    {
        await using var cn = Connection();
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_parceiros
            SET ativo = @ativo
            WHERE id = @id AND tenant_id = @tenantId",
            new { tenantId, id, ativo }, cancellationToken: ct));
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
        if (string.IsNullOrWhiteSpace(unidade)) unidade = "UN";

        await using var cn = Connection();
        var targetId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_produtos (id, tenant_id, sku, nome, unidade, codigo_barras, controla_lote, exige_inspecao, preco_custo, ativo)
            VALUES (@targetId, @tenantId, @sku, @nome, @unidade, @codigoBarras, @controlaLote, @exigeInspecao, @precoCusto, @ativo)
            ON CONFLICT (id) DO UPDATE
            SET sku = EXCLUDED.sku,
                nome = EXCLUDED.nome,
                unidade = EXCLUDED.unidade,
                codigo_barras = EXCLUDED.codigo_barras,
                controla_lote = EXCLUDED.controla_lote,
                exige_inspecao = EXCLUDED.exige_inspecao,
                preco_custo = EXCLUDED.preco_custo,
                ativo = EXCLUDED.ativo",
            new { targetId, tenantId, sku = sku.Trim(), nome = nome.Trim(), unidade = unidade.Trim(), codigoBarras = codigoBarras?.Trim(), controlaLote, exigeInspecao, precoCusto, ativo }, cancellationToken: ct));

        return targetId;
    }

    public async Task AlternarStatusProdutoAsync(Guid tenantId, Guid id, bool ativo, CancellationToken ct)
    {
        await using var cn = Connection();
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_produtos
            SET ativo = @ativo
            WHERE id = @id AND tenant_id = @tenantId",
            new { tenantId, id, ativo }, cancellationToken: ct));
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
        if (tipo != "INTERNO" && tipo != "EXTERNO") tipo = "INTERNO";

        await using var cn = Connection();
        var targetId = id.HasValue && id.Value != Guid.Empty ? id.Value : Guid.NewGuid();

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_locais (id, tenant_id, codigo, nome, tipo, ativo)
            VALUES (@targetId, @tenantId, @codigo, @nome, @tipo, @ativo)
            ON CONFLICT (id) DO UPDATE
            SET codigo = EXCLUDED.codigo,
                nome = EXCLUDED.nome,
                tipo = EXCLUDED.tipo,
                ativo = EXCLUDED.ativo",
            new { targetId, tenantId, codigo = codigo.Trim(), nome = nome.Trim(), tipo, ativo }, cancellationToken: ct));

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

        return new Lookups360Bundle(parceiros, produtos, locais, lotes);
    }
}
