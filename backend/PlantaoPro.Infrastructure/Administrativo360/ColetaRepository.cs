using Dapper;
using PlantaoPro.Application.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class ColetaRepository : Adm360Repository, IColetaRepository
{
    private sealed class LeituraExistenteRow
    {
        public Guid TarefaId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string? Lote { get; set; }
        public decimal Quantidade { get; set; }
    }

    private sealed class ProdutoRow
    {
        public Guid Id { get; set; }
        public bool ControlaLote { get; set; }
        public string Sku { get; set; } = string.Empty;
    }

    public ColetaRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<TarefaColeta>> TarefasAsync(Guid tenantId, Guid usuarioId, CancellationToken ct)
    {
        await using var cn = Connection();
        return (await cn.QueryAsync<TarefaColeta>(new CommandDefinition(@"
            SELECT id, tipo, descricao, situacao
            FROM plantaopro.adm360_tarefas_coleta
            WHERE tenant_id = @tenantId
              AND (atribuida_a IS NULL OR atribuida_a = @usuarioId)
              AND situacao = 'ABERTA'
            ORDER BY created_at",
            new { tenantId, usuarioId }, cancellationToken: ct))).AsList();
    }

    public async Task RegistrarAsync(Guid tenantId, Guid usuarioId, RegistrarLeituraCommand c, CancellationToken ct)
    {
        if (c.Quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser positiva.");
        if (string.IsNullOrWhiteSpace(c.Codigo))
            throw new ArgumentException("Código do produto é obrigatório.");

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        // Validação de idempotência por scan_id
        var existente = await cn.QuerySingleOrDefaultAsync<LeituraExistenteRow>(new CommandDefinition(@"
            SELECT tarefa_id AS TarefaId, codigo AS Codigo, lote AS Lote, quantidade AS Quantidade
            FROM plantaopro.adm360_leituras
            WHERE tenant_id = @tenantId AND scan_id = @ScanId",
            new { tenantId, c.ScanId }, tx, cancellationToken: ct));

        if (existente is not null)
        {
            if (existente.TarefaId == c.TarefaId &&
                string.Equals(existente.Codigo, c.Codigo, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existente.Lote ?? string.Empty, c.Lote ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
                existente.Quantidade == c.Quantidade)
            {
                await tx.CommitAsync(ct);
                return; // Reenvio idempotente com mesmo conteúdo
            }

            throw new InvalidOperationException("Conflito: scan_id já registrado com conteúdo divergente.");
        }

        // Validação de autorização e situação da tarefa
        var tarefaValida = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS(
                SELECT 1 FROM plantaopro.adm360_tarefas_coleta
                WHERE id = @TarefaId
                  AND tenant_id = @tenantId
                  AND situacao = 'ABERTA'
                  AND (atribuida_a IS NULL OR atribuida_a = @usuarioId)
            )",
            new { c.TarefaId, tenantId, usuarioId }, tx, cancellationToken: ct));

        if (!tarefaValida)
            throw new UnauthorizedAccessException("Tarefa inexistente, não autorizada ou já encerrada.");

        // Identificação do produto por SKU ou código de barras
        var produto = await cn.QuerySingleOrDefaultAsync<ProdutoRow>(new CommandDefinition(@"
            SELECT id AS Id, controla_lote AS ControlaLote, sku AS Sku
            FROM plantaopro.adm360_produtos
            WHERE tenant_id = @tenantId
              AND ativo
              AND (sku = @codigo OR codigo_barras = @codigo)",
            new { tenantId, codigo = c.Codigo.Trim() }, tx, cancellationToken: ct));

        if (produto is null || produto.Id == Guid.Empty)
            throw new ArgumentException($"Produto não identificado para o código '{c.Codigo}'. Resolução necessária pelo usuário.");

        // Se o produto controla lote e o lote foi informado, validar pertencimento ao produto
        if (produto.ControlaLote && !string.IsNullOrWhiteSpace(c.Lote))
        {
            var loteValido = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                SELECT EXISTS(
                    SELECT 1 FROM plantaopro.adm360_lotes
                    WHERE tenant_id = @tenantId
                      AND produto_id = @produtoId
                      AND codigo = @lote
                )",
                new { tenantId, produtoId = produto.Id, lote = c.Lote.Trim() }, tx, cancellationToken: ct));

            if (!loteValido)
                throw new ArgumentException($"Lote '{c.Lote}' não pertence ao produto identificado ({produto.Sku}).");
        }

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_leituras(
                id, tenant_id, tarefa_id, scan_id, codigo, lote, quantidade, created_by
            ) VALUES(
                gen_random_uuid(), @tenantId, @TarefaId, @ScanId, @Codigo, @Lote, @Quantidade, @usuarioId
            )",
            new { tenantId, c.TarefaId, c.ScanId, Codigo = c.Codigo.Trim(), Lote = c.Lote?.Trim(), c.Quantidade, usuarioId }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }
}
