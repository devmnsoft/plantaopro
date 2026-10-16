using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

// Writer de adm360_ocorrencias (spec A12) — primeira escrita em produção para a tabela
// criada em v2191 (antes só DDL + seeds demo). Nenhuma transição toca adm360_movimentos:
// "Registro de ocorrência não apaga estoque" (invariante coberta por teste).
public sealed class OcorrenciasRepository : Adm360Repository, IOcorrenciasRepository
{
    private sealed class EstadoRow
    {
        public string Situacao { get; set; } = string.Empty;
        public long Versao { get; set; }
    }

    private sealed class OcorrenciaRow
    {
        public Guid Id { get; set; }
        public string Produto { get; set; } = string.Empty;
        public string Lote { get; set; } = string.Empty;
        public string Local { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public string Situacao { get; set; } = string.Empty;
        public Guid ResponsavelId { get; set; }
        public DateOnly? Prazo { get; set; }
        public string? Destino { get; set; }
        public string? JustificativaEncerramento { get; set; }
        public long Versao { get; set; }
        public DateTime CriadoEm { get; set; }
    }

    public OcorrenciasRepository(string connectionString) : base(connectionString) { }

    public async Task<IReadOnlyList<OcorrenciaResumo>> ListarAsync(Guid tenantId, string? situacao, CancellationToken ct)
    {
        await using var cn = Connection();
        // POCO com setters (padrão da casa — ver InspecaoExistenteRow em QualidadeRepository):
        // Dapper mapeia por propriedade, sem dependência de construtor posicional.
        var rows = (await cn.QueryAsync<OcorrenciaRow>(new CommandDefinition(@"
            SELECT o.id AS Id, p.nome AS Produto, l.codigo AS Lote, lo.nome AS Local,
                   o.tipo AS Tipo, o.descricao AS Descricao, o.quantidade AS Quantidade,
                   o.situacao AS Situacao, o.responsavel_id AS ResponsavelId, o.prazo AS Prazo,
                   o.destino AS Destino, o.justificativa_encerramento AS JustificativaEncerramento,
                   o.versao AS Versao, o.created_at AS CriadoEm
            FROM plantaopro.adm360_ocorrencias o
            JOIN plantaopro.adm360_produtos p ON p.id = o.produto_id AND p.tenant_id = o.tenant_id
            JOIN plantaopro.adm360_lotes l ON l.id = o.lote_id AND l.tenant_id = o.tenant_id
            JOIN plantaopro.adm360_locais lo ON lo.id = o.local_id AND lo.tenant_id = o.tenant_id
            WHERE o.tenant_id = @tenantId
              AND (@situacao IS NULL OR o.situacao = @situacao)
            ORDER BY o.created_at DESC",
            new { tenantId, situacao }, cancellationToken: ct))).AsList();
        return rows
            .Select(r => new OcorrenciaResumo(r.Id, r.Produto, r.Lote, r.Local, r.Tipo, r.Descricao, r.Quantidade,
                r.Situacao, r.ResponsavelId == Guid.Empty ? null : r.ResponsavelId, r.Prazo, r.Destino,
                r.JustificativaEncerramento, r.Versao, (DateTimeOffset)r.CriadoEm))
            .ToArray();
    }

    public async Task<Guid> AbrirAsync(Guid tenantId, Guid usuarioId, CriarOcorrenciaCommand c, CancellationToken ct)
    {
        var id = Guid.Empty;
        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            OcorrenciasA12.ValidarCriacao(c.Tipo, c.Descricao, c.Quantidade);

            if (!await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
                    "select exists(select 1 from plantaopro.adm360_produtos where id=@id and tenant_id=@tenantId)",
                    new { id = c.ProdutoId, tenantId }, tx, cancellationToken: ct)))
                throw new Administrativo360BusinessException("Produto não encontrado nesta operação.");

            if (!await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
                    "select exists(select 1 from plantaopro.adm360_lotes where id=@id and tenant_id=@tenantId and produto_id=@produto)",
                    new { id = c.LoteId, produto = c.ProdutoId, tenantId }, tx, cancellationToken: ct)))
                throw new Administrativo360BusinessException("Lote não encontrado para o produto selecionado.");

            if (!await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
                    "select exists(select 1 from plantaopro.adm360_locais where id=@id and tenant_id=@tenantId)",
                    new { id = c.LocalId, tenantId }, tx, cancellationToken: ct)))
                throw new Administrativo360BusinessException("Local não encontrado nesta operação.");

            if (c.RecebimentoItemId is Guid ri
                && !await cn.ExecuteScalarAsync<bool>(new CommandDefinition(
                    "select exists(select 1 from plantaopro.adm360_recebimento_itens where id=@id and tenant_id=@tenantId)",
                    new { id = ri, tenantId }, tx, cancellationToken: ct)))
                throw new Administrativo360BusinessException("Item de recebimento não encontrado nesta operação.");

            id = await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_ocorrencias(
                    id, tenant_id, recebimento_item_id, lote_id, produto_id, local_id,
                    tipo, descricao, quantidade, situacao, responsavel_id, prazo, versao, created_at
                ) VALUES(
                    gen_random_uuid(), @tenantId, @ri::uuid, @lote, @produto, @local,
                    @tipo, @descricao, @quantidade, 'ABERTA', COALESCE(@responsavel, @usuario), @prazo::date, 1, now()
                ) RETURNING id",
                new
                {
                    tenantId,
                    ri = c.RecebimentoItemId,
                    lote = c.LoteId,
                    produto = c.ProdutoId,
                    local = c.LocalId,
                    tipo = c.Tipo.Trim(),
                    descricao = c.Descricao.Trim(),
                    c.Quantidade,
                    responsavel = c.ResponsavelId,
                    usuario = usuarioId,
                    prazo = c.Prazo
                }, tx, cancellationToken: ct));
        }, ct);
        return id;
    }

    private async Task TransicionarAsync(Guid tenantId, Guid ocorrenciaId, long versao, string destino, string? destinoItens, string? justificativa, CancellationToken ct)
    {
        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            if (destino == OcorrenciasA12.Encerrada)
                OcorrenciasA12.ValidarEncerramento(destinoItens, justificativa);

            var row = await cn.QuerySingleOrDefaultAsync<EstadoRow>(new CommandDefinition(@"
                SELECT situacao AS Situacao, versao AS Versao
                FROM plantaopro.adm360_ocorrencias
                WHERE id = @id AND tenant_id = @tenantId
                FOR UPDATE",
                new { id = ocorrenciaId, tenantId }, tx, cancellationToken: ct));
            if (row is null)
                throw new Administrativo360BusinessException("Ocorrência não encontrada.");
            if (!OcorrenciasA12.PodeTransicionar(row.Situacao, destino))
                throw new Administrativo360BusinessException($"Transição inválida: ocorrência em {row.Situacao} não pode ir para {destino}.");

            var changed = await cn.ExecuteAsync(new CommandDefinition(
                destino == OcorrenciasA12.Encerrada
                    ? @"UPDATE plantaopro.adm360_ocorrencias
                        SET situacao = @destino, versao = versao + 1,
                            destino = @destinoItens, justificativa_encerramento = @justificativa
                        WHERE id = @id AND tenant_id = @tenantId AND versao = @versao"
                    : @"UPDATE plantaopro.adm360_ocorrencias
                        SET situacao = @destino, versao = versao + 1
                        WHERE id = @id AND tenant_id = @tenantId AND versao = @versao",
                new { id = ocorrenciaId, tenantId, versao, destino, destinoItens, justificativa },
                tx, cancellationToken: ct));
            if (changed == 0)
                throw new Administrativo360BusinessException("A ocorrência foi alterada por outro usuário. Atualize antes de repetir.");
        }, ct);
    }

    public Task TratarAsync(Guid tenantId, Guid usuarioId, Guid ocorrenciaId, long versao, CancellationToken ct)
        => TransicionarAsync(tenantId, ocorrenciaId, versao, OcorrenciasA12.EmTratamento, null, null, ct);

    public Task ResolverAsync(Guid tenantId, Guid usuarioId, Guid ocorrenciaId, long versao, CancellationToken ct)
        => TransicionarAsync(tenantId, ocorrenciaId, versao, OcorrenciasA12.Resolvida, null, null, ct);

    public Task EncerrarAsync(Guid tenantId, Guid usuarioId, Guid ocorrenciaId, EncerrarOcorrenciaCommand c, CancellationToken ct)
        => TransicionarAsync(tenantId, ocorrenciaId, c.Versao, OcorrenciasA12.Encerrada, c.Destino, c.JustificativaEncerramento, ct);
}
