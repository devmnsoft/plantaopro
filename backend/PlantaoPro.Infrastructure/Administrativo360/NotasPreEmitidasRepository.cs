using System.Text.RegularExpressions;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

/// <summary>
/// Pré-nota fiscal interna (MVP A29): criação com conferência de referência, substituição
/// de itens apenas em RASCUNHO e transições de situação com invariante de evidência
/// (status externo somente com chave/momento válidos — H19). Cada transição registra
/// evento imutável EMISSAO_FISCAL idempotente na trilha adm360_eventos.
/// </summary>
public sealed class NotasPreEmitidasRepository : Adm360Repository, INotasPreEmitidasRepository
{
    private readonly Adm360EventService _eventos;

    public NotasPreEmitidasRepository(string connectionString) : base(connectionString)
    {
        _eventos = new Adm360EventService(connectionString);
    }

    public async Task<IReadOnlyList<NotaPreEmitidaResumoDto>> ListarAsync(Guid tenantId, string? situacao = null, CancellationToken ct = default)
    {
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        var linhas = await cn.QueryAsync<LinhaResumo>(new CommandDefinition(@"
            select id, numero, origem_tipo as ""OrigemTipo"", origem_id as ""OrigemId"",
                   situacao as ""Situacao"", destinatario_nome as ""DestinatarioNome"",
                   destinatario_documento as ""DestinatarioDocumento"", valor_total as ""ValorTotal"",
                   chave_acesso_externa as ""ChaveAcessoExterna"", emitida_em as ""EmitidaEm"",
                   created_at as ""CreatedAt""
              from plantaopro.adm360_notas_pre_emitidas
             where tenant_id = @tenantId
               and (@situacao::varchar is null or situacao = @situacao)
             order by created_at desc, numero desc",
            new { tenantId, situacao },
            cancellationToken: ct));

        return linhas.Select(l => l.ToResumo()).ToList();
    }

    public async Task<NotaPreEmitidaDetalhesDto?> ObterAsync(Guid tenantId, Guid notaId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        var linha = await cn.QueryFirstOrDefaultAsync<LinhaDetalhes>(new CommandDefinition(@"
            select n.id, n.numero, n.origem_tipo as ""OrigemTipo"", n.origem_id as ""OrigemId"",
                   n.situacao as ""Situacao"", n.destinatario_nome as ""DestinatarioNome"",
                   n.destinatario_documento as ""DestinatarioDocumento"", n.valor_total as ""ValorTotal"",
                   n.chave_acesso_externa as ""ChaveAcessoExterna"", n.emitida_em as ""EmitidaEm"",
                   n.created_at as ""CreatedAt"", n.rejeicao_mensagem as ""RejeicaoMensagem"",
                   n.cancelada_em as ""CanceladaEm"", n.motivo_cancelamento as ""MotivoCancelamento""
              from plantaopro.adm360_notas_pre_emitidas n
             where n.tenant_id = @tenantId and n.id = @notaId",
            new { tenantId, notaId },
            cancellationToken: ct));

        if (linha is null) return null;

        var itens = (await cn.QueryAsync<LinhaItem>(new CommandDefinition(@"
            select id, descricao, quantidade, preco_unitario as ""PrecoUnitario"", total
              from plantaopro.adm360_nota_pre_emitida_itens
             where tenant_id = @tenantId and nota_id = @notaId
             order by descricao",
            new { tenantId, notaId },
            cancellationToken: ct))).Select(i => i.ToDto()).ToList();

        return new NotaPreEmitidaDetalhesDto(linha.ToResumo(), itens, linha.RejeicaoMensagem, linha.CanceladaEm, linha.MotivoCancelamento);
    }

    public async Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, CriarNotaPreEmitidaCommand comando, CancellationToken ct = default)
    {
        NotaPreEmitidaRegras.ValidarOrigem(comando.OrigemTipo, comando.OrigemId);
        ValidarItens(comando.Itens, out var itensNormalizados, out var valorTotal);

        if (string.IsNullOrWhiteSpace(comando.DestinatarioNome))
            throw new Administrativo360BusinessException("O destinatário da pré-nota é obrigatório.");

        Guid novoId = Guid.NewGuid();
        string novoNumero = string.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            // A29: conferência de referência — a origem declarada deve existir no tenant.
            if (comando.OrigemId is not null)
            {
                var tabelaOrigem = TabelaDeOrigem(comando.OrigemTipo);
                var origemExiste = await cn.ExecuteScalarAsync<int>(new CommandDefinition(
                    $"select count(*) from {tabelaOrigem} where tenant_id = @tenantId and id = @origemId",
                    new { tenantId, origemId = comando.OrigemId.Value }, tx, cancellationToken: ct));
                if (origemExiste == 0)
                    throw new KeyNotFoundException(
                        $"Documento de origem '{comando.OrigemTipo.ToUpperInvariant()}' não encontrado para este cliente.");
            }

            var criado = await cn.QueryFirstAsync<LinhaResumo>(new CommandDefinition(@"
                insert into plantaopro.adm360_notas_pre_emitidas(
                    id, tenant_id, numero, origem_tipo, origem_id, situacao, destinatario_nome,
                    destinatario_documento, itens_total, valor_total, created_by, created_at
                ) values(
                    @id, @tenantId, 'NPE-' || lpad(nextval('plantaopro.adm360_nota_pre_numero')::text, 8, '0'),
                    upper(@origemTipo), @origemId, 'RASCUNHO', @destinatarioNome, @destinatarioDocumento,
                    @itensTotal, @valorTotal, @usuarioId, now()
                )
                returning id, numero, origem_tipo as ""OrigemTipo"", origem_id as ""OrigemId"",
                          situacao as ""Situacao"", destinatario_nome as ""DestinatarioNome"",
                          destinatario_documento as ""DestinatarioDocumento"", valor_total as ""ValorTotal"",
                          chave_acesso_externa as ""ChaveAcessoExterna"", emitida_em as ""EmitidaEm"",
                          created_at as ""CreatedAt""",
                new
                {
                    id = novoId,
                    tenantId,
                    origemTipo = comando.OrigemTipo,
                    origemId = comando.OrigemId,
                    destinatarioNome = comando.DestinatarioNome.Trim(),
                    destinatarioDocumento = comando.DestinatarioDocumento,
                    itensTotal = itensNormalizados.Count,
                    valorTotal,
                    usuarioId
                }, tx, cancellationToken: ct));

            novoNumero = criado.Numero;
            await InserirItensAsync(cn, tx, tenantId, novoId, itensNormalizados, ct);

            await _eventos.RegistrarAsync(
                cn, tx, tenantId, Adm360TipoEvento.EmissaoFiscal, "nota_pre_emitida", novoId, usuarioId,
                $"Pré-nota {novoNumero} criada (origem {comando.OrigemTipo.ToUpperInvariant()}, {itensNormalizados.Count} itens).",
                new { origem_tipo = comando.OrigemTipo.ToUpperInvariant(), origem_id = comando.OrigemId, itens_total = itensNormalizados.Count, valor_total = valorTotal },
                idempotencyKey: $"fiscal-nota-pre-criacao:{novoId}", ct);
        }, ct);

        return novoId;
    }

    public async Task AtualizarItensAsync(Guid tenantId, Guid usuarioId, AtualizarNotaPreEmitidaItensCommand comando, CancellationToken ct = default)
    {
        ValidarItens(comando.Itens, out var itensNormalizados, out var valorTotal);

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var atual = await cn.QueryFirstOrDefaultAsync<string>(new CommandDefinition(@"
                select situacao from plantaopro.adm360_notas_pre_emitidas
                 where tenant_id = @tenantId and id = @notaId for update",
                new { tenantId, notaId = comando.NotaId }, tx, cancellationToken: ct));

            if (atual is null)
                throw new KeyNotFoundException("Pré-nota não encontrada.");

            if (!atual.Equals(NotaPreEmitidaSituacoes.Rascunho, StringComparison.OrdinalIgnoreCase))
                throw new Administrativo360BusinessException(
                    "Os itens da pré-nota só podem ser alterados enquanto ela estiver em RASCUNHO.");

            await cn.ExecuteAsync(new CommandDefinition(
                "delete from plantaopro.adm360_nota_pre_emitida_itens where tenant_id = @tenantId and nota_id = @notaId",
                new { tenantId, notaId = comando.NotaId }, tx, cancellationToken: ct));

            await InserirItensAsync(cn, tx, tenantId, comando.NotaId, itensNormalizados, ct);

            await cn.ExecuteAsync(new CommandDefinition(@"
                update plantaopro.adm360_notas_pre_emitidas
                   set itens_total = @itensTotal,
                       valor_total = @valorTotal,
                       updated_at = now(),
                       updated_by = @usuarioId,
                       versao = versao + 1
                 where tenant_id = @tenantId and id = @notaId",
                new { tenantId, notaId = comando.NotaId, itensTotal = itensNormalizados.Count, valorTotal, usuarioId },
                tx, cancellationToken: ct));
        }, ct);
    }

    public async Task TransicionarAsync(Guid tenantId, Guid usuarioId, TransicionarNotaPreEmitidaCommand comando, CancellationToken ct = default)
    {
        var alvo = comando.NovaSituacao.ToUpperInvariant();
        NotaPreEmitidaRegras.ValidarSituacao(alvo);

        // Evidência validada ANTES de qualquer escrita (H19: sem evidência, sem status externo).
        NotaPreEmitidaRegras.ValidarEvidenciaParaSituacao(alvo, comando.ChaveAcessoExterna, comando.EmitidaEm, comando.RejeicaoMensagem);
        DateTime? canceladaEm = null;
        if (alvo == NotaPreEmitidaSituacoes.Cancelada)
        {
            canceladaEm = comando.CanceladaEm ?? DateTime.UtcNow;
            NotaPreEmitidaRegras.ValidarCancelamento(canceladaEm, comando.MotivoCancelamento);
        }

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var nota = await cn.QueryFirstOrDefaultAsync<LinhaTransicao>(new CommandDefinition(@"
                select id, numero, situacao as ""Situacao"", idempotency_key as ""IdempotencyKey"", versao
                  from plantaopro.adm360_notas_pre_emitidas
                 where tenant_id = @tenantId and id = @notaId
                 for update",
                new { tenantId, notaId = comando.NotaId }, tx, cancellationToken: ct));

            if (nota is null)
                throw new KeyNotFoundException("Pré-nota não encontrada.");

            // Idempotência de situação: repetir a mesma transição não altera nada nem duplica evento.
            if (nota.Situacao.Equals(alvo, StringComparison.OrdinalIgnoreCase))
                return;

            NotaPreEmitidaRegras.ValidarTransicao(nota.Situacao, alvo);

            if (!string.IsNullOrWhiteSpace(comando.IdempotencyKey))
            {
                var chaveExistente = (string?)nota.IdempotencyKey;
                if (chaveExistente is not null && !string.Equals(chaveExistente, comando.IdempotencyKey.Trim(), StringComparison.Ordinal))
                    throw new Administrativo360BusinessException(
                        "Esta pré-nota já foi processada com outra chave de idempotência; use uma nova pré-nota.");
            }

            var versaoNova = nota.Versao + 1;
            await cn.ExecuteAsync(new CommandDefinition(@"
                update plantaopro.adm360_notas_pre_emitidas
                   set situacao = @alvo,
                       -- campos externos: preenchidos só pelo estado-alvo; limpos ao voltar para estados internos
                       chave_acesso_externa = case when @alvo = 'AUTORIZADA' then @chave else null end,
                       emitida_em = case when @alvo in ('AUTORIZADA','REJEITADA') then @emitidaEm::timestamptz else null end,
                       rejeicao_mensagem = case when @alvo = 'REJEITADA' then @rejeicao else null end,
                       protocolo_externo = case when @alvo in ('AUTORIZADA','REJEITADA','PENDENTE_CONFIRMACAO')
                                               then coalesce(@protocolo, protocolo_externo)
                                           when @alvo = 'CANCELADA' then protocolo_externo
                                          else null end,
                       cancelada_em = case when @alvo = 'CANCELADA' then @canceladaEm::timestamptz else null end,
                       motivo_cancelamento = case when @alvo = 'CANCELADA' then @motivo else null end,
                       idempotency_key = case when @chaveIdempotencia is not null and (idempotency_key is null or idempotency_key = @chaveIdempotencia)
                                              then @chaveIdempotencia else idempotency_key end,
                       versao = @versaoNova,
                       updated_at = now(),
                       updated_by = @usuarioId
                 where tenant_id = @tenantId and id = @notaId",
                new
                {
                    tenantId,
                    notaId = comando.NotaId,
                    alvo,
                    chave = comando.ChaveAcessoExterna,
                    emitidaEm = comando.EmitidaEm,
                    rejeicao = comando.RejeicaoMensagem,
                    protocolo = comando.ProtocoloExterno,
                    canceladaEm,
                    motivo = comando.MotivoCancelamento,
                    chaveIdempotencia = string.IsNullOrWhiteSpace(comando.IdempotencyKey) ? null : comando.IdempotencyKey.Trim(),
                    versaoNova,
                    usuarioId
                }, tx, cancellationToken: ct));

            await _eventos.RegistrarAsync(
                cn, tx, tenantId, Adm360TipoEvento.EmissaoFiscal, "nota_pre_emitida", comando.NotaId, usuarioId,
                $"Pré-nota {nota.Numero}: {nota.Situacao} -> {alvo}.",
                new
                {
                    situacao_anterior = nota.Situacao,
                    nova_situacao = alvo,
                    chave_acesso_externa = comando.ChaveAcessoExterna,
                    protocolo_externo = comando.ProtocoloExterno,
                    versao = versaoNova
                },
                idempotencyKey: $"fiscal-nota-pre:{comando.NotaId}:v{versaoNova}:{alvo}", ct);

            static bool novaEhExterna(string s) => s is "AUTORIZADA" or "REJEITADA";
        }, ct);
    }

    private static string TabelaDeOrigem(string origemTipo) =>
        origemTipo.ToUpperInvariant() switch
        {
            "VENDA" => "plantaopro.adm360_vendas",
            "ORCAMENTO" => "plantaopro.adm360_orcamentos",
            "COTACAO" => "plantaopro.adm360_cotacoes",
            _ => throw new ArgumentException($"Tipo de origem inválido: '{origemTipo}'.")
        };

    private static void ValidarItens(
        IReadOnlyList<NotaPreEmitidaItemCommand>? itens,
        out List<(string Descricao, decimal Quantidade, decimal PrecoUnitario, decimal Total)> normalizados,
        out decimal valorTotal)
    {
        if (itens is null || itens.Count == 0)
            throw new Administrativo360BusinessException("A pré-nota precisa de ao menos um item.");

        valorTotal = 0m;
        normalizados = new List<(string, decimal, decimal, decimal)>(itens.Count);
        foreach (var item in itens)
        {
            if (string.IsNullOrWhiteSpace(item.Descricao))
                throw new Administrativo360BusinessException("Todo item da pré-nota exige descrição.");
            if (item.Quantidade <= 0)
                throw new Administrativo360BusinessException("A quantidade do item deve ser maior que zero.");
            if (item.PrecoUnitario < 0)
                throw new Administrativo360BusinessException("O preço unitário do item não pode ser negativo.");

            var total = Math.Round(item.Quantidade * item.PrecoUnitario, 4, MidpointRounding.AwayFromZero);
            valorTotal += total;
            normalizados.Add((item.Descricao.Trim(), item.Quantidade, item.PrecoUnitario, total));
        }
        valorTotal = Math.Round(valorTotal, 4, MidpointRounding.AwayFromZero);
    }

    private static async Task InserirItensAsync(
        NpgsqlConnection cn, NpgsqlTransaction tx, Guid tenantId, Guid notaId,
        List<(string Descricao, decimal Quantidade, decimal PrecoUnitario, decimal Total)> itens, CancellationToken ct)
    {
        foreach (var (descricao, quantidade, preco, total) in itens)
        {
            await cn.ExecuteAsync(new CommandDefinition(@"
                insert into plantaopro.adm360_nota_pre_emitida_itens(
                    id, tenant_id, nota_id, descricao, quantidade, preco_unitario, total
                ) values(@id, @tenantId, @notaId, @descricao, @quantidade, @precoUnitario, @total)",
                new
                {
                    id = Guid.NewGuid(),
                    tenantId,
                    notaId,
                    descricao,
                    quantidade,
                    precoUnitario = preco,
                    total
                }, tx, cancellationToken: ct));
        }
    }

    private sealed record LinhaResumo(
        Guid Id,
        string Numero,
        string OrigemTipo,
        Guid? OrigemId,
        string Situacao,
        string DestinatarioNome,
        string? DestinatarioDocumento,
        decimal ValorTotal,
        string? ChaveAcessoExterna,
        DateTime? EmitidaEm,
        DateTime CreatedAt
    )
    {
        public NotaPreEmitidaResumoDto ToResumo() => new(
            Id, Numero, OrigemTipo, OrigemId, Situacao, DestinatarioNome,
            DestinatarioDocumento, ValorTotal, ChaveAcessoExterna, EmitidaEm, CreatedAt);
    }

    private sealed record LinhaDetalhes(
        Guid Id,
        string Numero,
        string OrigemTipo,
        Guid? OrigemId,
        string Situacao,
        string DestinatarioNome,
        string? DestinatarioDocumento,
        decimal ValorTotal,
        string? ChaveAcessoExterna,
        DateTime? EmitidaEm,
        DateTime CreatedAt,
        string? RejeicaoMensagem,
        DateTime? CanceladaEm,
        string? MotivoCancelamento
    )
    {
        public NotaPreEmitidaResumoDto ToResumo() => new(
            Id, Numero, OrigemTipo, OrigemId, Situacao, DestinatarioNome,
            DestinatarioDocumento, ValorTotal, ChaveAcessoExterna, EmitidaEm, CreatedAt);
    }

    private sealed record LinhaItem(
        Guid Id,
        string Descricao,
        decimal Quantidade,
        decimal PrecoUnitario,
        decimal Total
    )
    {
        public NotaPreEmitidaItemDto ToDto() => new(Id, Descricao, Quantidade, PrecoUnitario, Total);
    }

    private sealed record LinhaTransicao(
        Guid Id,
        string Numero,
        string Situacao,
        string? IdempotencyKey,
        long Versao
    );
}
