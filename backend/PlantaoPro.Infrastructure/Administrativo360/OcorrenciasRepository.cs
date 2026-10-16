using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

// Writer de adm360_ocorrencias (spec A12) — primeira escrita em produção para a tabela
// criada em v2191 (antes só DDL + seeds demo).
//
// Bloqueio imediato (A12 L112: "Bloqueio imediato autorizado pode abranger saldo interno
// e externo"): ao ABRIR, pernas BLOQUEIO_OCORRENCIA movem até `quantidade` de LIBERADO
// para BLOQUEADO no lote (locais internos e externos com saldo; local da ocorrência em
// primeiro lugar). Ao RESOLVER, pernas LIBERACAO_OCORRENCIA invertem; TRATAR e ENCERRAR
// não mexem em saldo. "Registro de ocorrência não apaga estoque": soma física por
// (lote, local) invariante no ciclo completo (invariante coberta por teste).
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

    private sealed class LocalCandidatoRow
    {
        public Guid LocalId { get; set; }
    }

    private sealed class BlocoRegistradoRow
    {
        public Guid ProdutoId { get; set; }
        public Guid LoteId { get; set; }
        public Guid LocalId { get; set; }
        public decimal Quantidade { get; set; }
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

            // A12 L112 — bloqueio imediato: na mesma transação da abertura o saldo do lote
            // já reflete a não conformidade (lote bloqueado não reserva/expede — L296).
            await BloquearSaldoImediatoAsync(cn, tx, tenantId, usuarioId, id,
                c.ProdutoId, c.LoteId, c.LocalId, c.Quantidade, c.Tipo.Trim(), c.Descricao.Trim(), ct);
        }, ct);
        return id;
    }

    private async Task TransicionarAsync(Guid tenantId, Guid usuarioId, Guid ocorrenciaId, long versao, string destino, string? destinoItens, string? justificativa, CancellationToken ct)
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

            // RESOLVIDA inverte o bloqueio imediato (itens tratados voltam à circulação);
            // TRATAR e ENCERRAR são atos administrativos e não mexem em saldo.
            if (destino == OcorrenciasA12.Resolvida)
                await LiberarSaldoBloqueadoAsync(cn, tx, tenantId, usuarioId, ocorrenciaId, ct);
        }, ct);
    }

    public Task TratarAsync(Guid tenantId, Guid usuarioId, Guid ocorrenciaId, long versao, CancellationToken ct)
        => TransicionarAsync(tenantId, usuarioId, ocorrenciaId, versao, OcorrenciasA12.EmTratamento, null, null, ct);

    public Task ResolverAsync(Guid tenantId, Guid usuarioId, Guid ocorrenciaId, long versao, CancellationToken ct)
        => TransicionarAsync(tenantId, usuarioId, ocorrenciaId, versao, OcorrenciasA12.Resolvida, null, null, ct);

    public Task EncerrarAsync(Guid tenantId, Guid usuarioId, Guid ocorrenciaId, EncerrarOcorrenciaCommand c, CancellationToken ct)
        => TransicionarAsync(tenantId, usuarioId, ocorrenciaId, c.Versao, OcorrenciasA12.Encerrada, c.Destino, c.JustificativaEncerramento, ct);

    // ===== Bloqueio imediato de saldo (A12 L112 + regra global L296) =====

    // A12 L112 — bloqueio imediato ao abrir: cobre até `quantidade` do saldo LIBERADO do
    // lote em TODOS os locais ativos (internos e externos). Ordem determinística: local da
    // ocorrência primeiro; demais ordenados por (codigo, id). Saldo total menor que a
    // quantidade ⇒ bloqueio parcial — a abertura não é rejeitada (não conformidade pode
    // ser documental; a diferença fica visível na soma das pernas da ocorrência).
    private async Task BloquearSaldoImediatoAsync(
        NpgsqlConnection cn, NpgsqlTransaction tx, Guid tenantId, Guid usuarioId, Guid ocorrenciaId,
        Guid produtoId, Guid loteId, Guid localOcorrencia, decimal quantidade, string tipo, string descricao, CancellationToken ct)
    {
        var candidatos = (await cn.QueryAsync<LocalCandidatoRow>(new CommandDefinition(@"
            SELECT s.local_id AS LocalId
            FROM plantaopro.adm360_saldos s
            JOIN plantaopro.adm360_locais lo ON lo.id = s.local_id AND lo.tenant_id = s.tenant_id
            WHERE s.tenant_id = @tenantId AND s.produto_id = @produtoId AND s.lote_id = @loteId
              AND s.condicao = 'LIBERADO' AND s.disponivel > 0 AND lo.ativo
            ORDER BY CASE WHEN s.local_id = @localOcorrencia THEN 0 ELSE 1 END, lo.codigo, lo.id",
            new { tenantId, produtoId, loteId, localOcorrencia }, tx, cancellationToken: ct))).AsList();

        if (candidatos.Count == 0)
            return; // lote sem saldo LIBERADO: nada a bloquear (abertura segue sem efeito em estoque)

        var chaves = candidatos.Select(x => $"{tenantId}:{produtoId}:{loteId}:{x.LocalId}").ToArray();
        await BloquearChavesDeterministasAsync(cn, tx, chaves, ct);
        foreach (var cand in candidatos)
            await ValidarBloqueioInventarioAsync(cn, tx, tenantId, cand.LocalId, ct);

        var restante = quantidade;
        var motivo = $"Bloqueio imediato da ocorrencia {tipo} - {descricao} (ocorrencia {ocorrenciaId})";
        foreach (var cand in candidatos)
        {
            if (restante <= 0m)
                break;

            // Releitura sob lock (padrão de ReservarAsync): a perna é decidida com o saldo atual.
            var disponivel = await cn.ExecuteScalarAsync<decimal>(new CommandDefinition(@"
                SELECT COALESCE(disponivel, 0)
                FROM plantaopro.adm360_saldos
                WHERE tenant_id = @tenantId AND produto_id = @produtoId AND lote_id = @loteId
                  AND local_id = @localId AND condicao = 'LIBERADO'",
                new { tenantId, produtoId, loteId, localId = cand.LocalId }, tx, cancellationToken: ct));
            if (disponivel <= 0m)
                continue;

            var qtd = Math.Min(disponivel, restante);
            await EscreverPernasMudancaCondicaoAsync(cn, tx, tenantId, usuarioId, ocorrenciaId,
                produtoId, loteId, cand.LocalId, qtd, motivo, liberando: false, ct);
            restante -= qtd;
        }
    }

    // Inversão no RESOLVER: espelha exatamente o que foi registrado ao abrir (mesmos
    // lote/locale/quantidades, lidos dos movimentos — nada recalculado). Se nada foi
    // bloqueado (abertura sem saldo LIBERADO), não há efeito. Caveat documentado: se um
    // inventário ajustou a quantidade bloqueada entre abrir e resolver, a liberação
    // espelha o registrado (fluxo simétrico; divergência física é papel do inventário).
    private async Task LiberarSaldoBloqueadoAsync(
        NpgsqlConnection cn, NpgsqlTransaction tx, Guid tenantId, Guid usuarioId, Guid ocorrenciaId, CancellationToken ct)
    {
        var blocos = (await cn.QueryAsync<BlocoRegistradoRow>(new CommandDefinition(@"
            SELECT produto_id AS ProdutoId, lote_id AS LoteId, local_id AS LocalId,
                   sum(quantidade)::numeric(18,4) AS Quantidade
            FROM plantaopro.adm360_movimentos
            WHERE tenant_id = @tenantId AND origem_tipo = 'OCORRENCIA' AND origem_id = @ocorrenciaId
              AND tipo = 'BLOQUEIO_OCORRENCIA' AND condicao = 'BLOQUEADO'
            GROUP BY produto_id, lote_id, local_id
            HAVING sum(quantidade) > 0
            ORDER BY local_id",
            new { tenantId, ocorrenciaId }, tx, cancellationToken: ct))).AsList();

        if (blocos.Count == 0)
            return;

        var chaves = blocos.Select(x => $"{tenantId}:{x.ProdutoId}:{x.LoteId}:{x.LocalId}").ToArray();
        await BloquearChavesDeterministasAsync(cn, tx, chaves, ct);

        var motivo = $"Liberacao do bloqueio da ocorrencia (ocorrencia {ocorrenciaId})";
        foreach (var bloco in blocos)
        {
            await ValidarBloqueioInventarioAsync(cn, tx, tenantId, bloco.LocalId, ct);
            await EscreverPernasMudancaCondicaoAsync(cn, tx, tenantId, usuarioId, ocorrenciaId,
                bloco.ProdutoId, bloco.LoteId, bloco.LocalId, bloco.Quantidade, motivo, liberando: true, ct);
        }
    }

    // Idioma da casa (QualidadeRepository/Transferencia): mudança de condição = duas pernas
    // que somam zero na soma física (uma débito na condição de saída, uma crédito na de
    // entrada), sempre com origem na ocorrência (L296: motivo consultável) e chave
    // idempotência determinística por perna.
    private async Task EscreverPernasMudancaCondicaoAsync(
        NpgsqlConnection cn, NpgsqlTransaction tx, Guid tenantId, Guid usuarioId, Guid ocorrenciaId,
        Guid produtoId, Guid loteId, Guid localId, decimal qtd, string motivo, bool liberando, CancellationToken ct)
    {
        var tipoMovimento = liberando ? "LIBERACAO_OCORRENCIA" : "BLOQUEIO_OCORRENCIA";
        var prefixoChave = liberando ? "DEL" : "BLQ";
        var pernas = liberando
            ? new[] { ("BLOQUEADO", -qtd, "SAIDA_BLOQUEADO"), ("LIBERADO", qtd, "ENTRADA_LIBERADO") }
            : new[] { ("LIBERADO", -qtd, "SAIDA_LIBERADO"), ("BLOQUEADO", qtd, "ENTRADA_BLOQUEADO") };

        foreach (var (condicao, valor, perna) in pernas)
        {
            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_movimentos(
                    id, tenant_id, produto_id, lote_id, local_id, tipo, condicao, quantidade, motivo, origem_tipo, origem_id, idempotency_key, created_by
                ) VALUES(
                    gen_random_uuid(), @tenantId, @produtoId, @loteId, @localId, @tipo, @condicao, @qtd, @motivo, 'OCORRENCIA', @ocorrenciaId, @key, @usuarioId)",
                new
                {
                    tenantId,
                    produtoId,
                    loteId,
                    localId,
                    tipo = tipoMovimento,
                    condicao,
                    qtd = valor,
                    motivo,
                    ocorrenciaId,
                    key = $"{ocorrenciaId}:{prefixoChave}:{localId}:{perna}",
                    usuarioId
                }, tx, cancellationToken: ct));
        }
    }
}
