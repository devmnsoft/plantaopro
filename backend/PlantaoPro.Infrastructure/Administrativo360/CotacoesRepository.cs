using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class CotacoesRepository : Adm360Repository, ICotacoesRepository
{
    private readonly IEnumerable<IPortalCotacaoConnector> connectors;
    private readonly Adm360EventService eventos;

    public CotacoesRepository(string connectionString, IEnumerable<IPortalCotacaoConnector>? connectors = null, Adm360EventService? eventos = null)
        : base(connectionString)
    {
        this.connectors = connectors ?? new IPortalCotacaoConnector[] { new OpmenexoConnector(), new InpartConnector(), new ImportacaoManualConnector() };
        this.eventos = eventos ?? new Adm360EventService(connectionString);
    }

    private static DateOnly ToDateOnly(object val) => val switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        _ => DateOnly.Parse(val.ToString()!)
    };

    private IPortalCotacaoConnector ObterConector(string provedor)
    {
        var con = connectors.FirstOrDefault(c => string.Equals(c.Provedor, provedor, StringComparison.OrdinalIgnoreCase));
        return con ?? new ImportacaoManualConnector();
    }

    public async Task<IReadOnlyList<EstabelecimentoDto>> ListarEstabelecimentosAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT id, cnpj, razao_social, nome_fantasia, inscricao_estadual, cnae, ambiente, ativo, created_at
            FROM plantaopro.adm360_estabelecimentos
            WHERE tenant_id = @tenantId
            ORDER BY razao_social",
            new { tenantId }, cancellationToken: ct));

        return rows.Select(r => new EstabelecimentoDto(
            (Guid)r.id,
            (string)r.cnpj,
            (string)r.razao_social,
            (string?)r.nome_fantasia,
            (string?)r.inscricao_estadual,
            (string?)r.cnae,
            (string)r.ambiente,
            (bool)r.ativo,
            (DateTime)r.created_at
        )).ToList();
    }

    public async Task<Guid> CriarEstabelecimentoAsync(Guid tenantId, Guid usuarioId, CriarEstabelecimentoCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Cnpj) || string.IsNullOrWhiteSpace(command.RazaoSocial))
            throw new ArgumentException("CNPJ e Razão Social são obrigatórios.");

        var cnpjLimpo = System.Text.RegularExpressions.Regex.Replace(command.Cnpj, @"\D", "");
        var id = Guid.NewGuid();

        await using var cn = Connection();
        await cn.OpenAsync(ct);

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_estabelecimentos(
                id, tenant_id, cnpj, razao_social, nome_fantasia, inscricao_estadual, cnae, ambiente, ativo
            ) VALUES (
                @id, @tenantId, @cnpjLimpo, @razaoSocial, @nomeFantasia, @inscricaoEstadual, @cnae, @ambiente, true
            ) ON CONFLICT (tenant_id, cnpj) DO UPDATE
            SET razao_social = EXCLUDED.razao_social,
                nome_fantasia = EXCLUDED.nome_fantasia,
                inscricao_estadual = EXCLUDED.inscricao_estadual,
                ambiente = EXCLUDED.ambiente,
                ativo = true",
            new
            {
                id, tenantId, cnpjLimpo, razaoSocial = command.RazaoSocial,
                nomeFantasia = command.NomeFantasia, inscricaoEstadual = command.InscricaoEstadual,
                cnae = command.Cnae, ambiente = command.Ambiente
            }, cancellationToken: ct));

        return id;
    }

    public async Task<IReadOnlyList<CapacidadeContratadaDto>> ListarCapacidadesAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT id, capacidade, habilitado, ativado_em, configuracoes::text AS config_json
            FROM plantaopro.adm360_capacidades_contratadas
            WHERE tenant_id = @tenantId",
            new { tenantId }, cancellationToken: ct));

        return rows.Select(r => new CapacidadeContratadaDto(
            (Guid)r.id,
            (string)r.capacidade,
            (bool)r.habilitado,
            (DateTime)r.ativado_em,
            (string)(r.config_json ?? "{}")
        )).ToList();
    }

    public async Task HabilitarCapacidadeAsync(Guid tenantId, Guid usuarioId, HabilitarCapacidadeCommand command, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_capacidades_contratadas(
                id, tenant_id, capacidade, habilitado, configuracoes
            ) VALUES (
                @id, @tenantId, @capacidade, @habilitado, COALESCE(@config::jsonb, '{}'::jsonb)
            ) ON CONFLICT (tenant_id, capacidade) DO UPDATE
            SET habilitado = EXCLUDED.habilitado,
                configuracoes = COALESCE(@config::jsonb, plantaopro.adm360_capacidades_contratadas.configuracoes)",
            new
            {
                id, tenantId, capacidade = command.Capacidade.ToUpperInvariant(),
                habilitado = command.Habilitado, config = command.ConfiguracoesJson
            }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<PortalContaDto>> ListarContasPortalAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT c.id, c.estabelecimento_id, e.razao_social AS estabelecimento_nome,
                   c.provedor, c.nome_conta, c.identificador_externo, c.usuario_acesso,
                   c.ambiente, c.status_integracao, c.motivo_bloqueio, c.ultima_sincronizacao, c.ativo
            FROM plantaopro.adm360_portal_contas c
            JOIN plantaopro.adm360_estabelecimentos e ON e.id = c.estabelecimento_id AND e.tenant_id = c.tenant_id
            WHERE c.tenant_id = @tenantId
            ORDER BY c.provedor, c.nome_conta",
            new { tenantId }, cancellationToken: ct));

        return rows.Select(r => new PortalContaDto(
            (Guid)r.id,
            (Guid)r.estabelecimento_id,
            (string)r.estabelecimento_nome,
            (string)r.provedor,
            (string)r.nome_conta,
            (string?)r.identificador_externo,
            (string?)r.usuario_acesso,
            (string)r.ambiente,
            (string)r.status_integracao,
            (string?)r.motivo_bloqueio,
            r.ultima_sincronizacao is not null ? (DateTime?)r.ultima_sincronizacao : null,
            (bool)r.ativo
        )).ToList();
    }

    public async Task<Guid> ConfigurarContaPortalAsync(Guid tenantId, Guid usuarioId, ConfigurarPortalContaCommand command, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var prov = command.Provedor.ToUpperInvariant();
        var status = prov == "IMPORTACAO_MANUAL" ? "CONFIGURADA" : (string.IsNullOrWhiteSpace(command.UsuarioAcesso) ? "NAO_CONFIGURADA" : "CONFIGURADA");
        var motivo = status == "NAO_CONFIGURADA" ? "Credenciais de API não configuradas." : null;

        await using var cn = Connection();
        await cn.OpenAsync(ct);

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_portal_contas(
                id, tenant_id, estabelecimento_id, provedor, nome_conta, identificador_externo,
                usuario_acesso, segredo_referencia, ambiente, status_integracao, motivo_bloqueio, ativo
            ) VALUES (
                @id, @tenantId, @estabelecimentoId, @prov, @nomeConta, @identificadorExterno,
                @usuarioAcesso, @segredoReferencia, @ambiente, @status, @motivo, true
            ) ON CONFLICT (tenant_id, provedor, identificador_externo) DO UPDATE
            SET estabelecimento_id = EXCLUDED.estabelecimento_id,
                nome_conta = EXCLUDED.nome_conta,
                usuario_acesso = EXCLUDED.usuario_acesso,
                segredo_referencia = COALESCE(EXCLUDED.segredo_referencia, plantaopro.adm360_portal_contas.segredo_referencia),
                ambiente = EXCLUDED.ambiente,
                status_integracao = EXCLUDED.status_integracao,
                motivo_bloqueio = EXCLUDED.motivo_bloqueio,
                ativo = true",
            new
            {
                id, tenantId, estabelecimentoId = command.EstabelecimentoId, prov,
                nomeConta = command.NomeConta, identificadorExterno = command.IdentificadorExterno ?? "DEFAULT",
                usuarioAcesso = command.UsuarioAcesso, segredoReferencia = command.SegredoReferencia,
                ambiente = command.Ambiente, status, motivo
            }, cancellationToken: ct));

        return id;
    }

    public async Task<IReadOnlyList<MapeamentoDeParaDto>> ListarMapeamentosAsync(Guid tenantId, string? provedor = null, string? tipoEntidade = null, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var sql = @"
            SELECT id, portal_conta_id, provedor, tipo_entidade, codigo_externo, descricao_externa,
                   entidade_interna_id, entidade_interna_descricao, fator_conversao, situacao, created_at
            FROM plantaopro.adm360_mapeamentos_de_para
            WHERE tenant_id = @tenantId
              AND (@provedor IS NULL OR provedor = @provedor)
              AND (@tipoEntidade IS NULL OR tipo_entidade = @tipoEntidade)
            ORDER BY tipo_entidade, descricao_externa";

        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(sql, new { tenantId, provedor, tipoEntidade }, cancellationToken: ct));

        return rows.Select(r => new MapeamentoDeParaDto(
            (Guid)r.id,
            r.portal_conta_id is not null ? (Guid?)r.portal_conta_id : null,
            (string)r.provedor,
            (string)r.tipo_entidade,
            (string)r.codigo_externo,
            (string)r.descricao_externa,
            r.entidade_interna_id is not null ? (Guid?)r.entidade_interna_id : null,
            (string)r.entidade_interna_descricao,
            (decimal)r.fator_conversao,
            (string)r.situacao,
            (DateTime)r.created_at
        )).ToList();
    }

    public async Task<Guid> SalvarMapeamentoAsync(Guid tenantId, Guid usuarioId, SalvarMapeamentoDeParaCommand command, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_mapeamentos_de_para(
                id, tenant_id, portal_conta_id, provedor, tipo_entidade, codigo_externo,
                descricao_externa, entidade_interna_id, entidade_interna_descricao,
                fator_conversao, situacao, criado_por
            ) VALUES (
                @id, @tenantId, @portalContaId, @provedor, @tipoEntidade, @codigoExterno,
                @descricaoExterna, @entidadeInternaId, @entidadeInternaDescricao,
                @fatorConversao, 'ATIVO', @usuarioId
            ) ON CONFLICT (tenant_id, provedor, tipo_entidade, codigo_externo) DO UPDATE
            SET descricao_externa = EXCLUDED.descricao_externa,
                entidade_interna_id = EXCLUDED.entidade_interna_id,
                entidade_interna_descricao = EXCLUDED.entidade_interna_descricao,
                fator_conversao = EXCLUDED.fator_conversao,
                situacao = 'ATIVO',
                updated_at = now()",
            new
            {
                id, tenantId, portalContaId = command.PortalContaId, provedor = command.Provedor.ToUpperInvariant(),
                tipoEntidade = command.TipoEntidade.ToUpperInvariant(), codigoExterno = command.CodigoExterno,
                descricaoExterna = command.DescricaoExterna, entidadeInternaId = command.EntidadeInternaId,
                entidadeInternaDescricao = command.EntidadeInternaDescricao, fatorConversao = command.FatorConversao,
                usuarioId
            }, cancellationToken: ct));

        return id;
    }

    // B7: expiração lazy por prazo excedido. Marca EXPIRADA em leitura (autocommit) ou
    // dentro da transação do fluxo de escrita (escopo de linha quando o id é conhecido),
    // preservando os estados terminais existentes. Depois do UPDATE a mesma conexão vê o
    // estado novo e as regras de negócio agem sobre ele de forma idempotente.
    private static async Task MarcarExpiradasPrazoExcedidoAsync(NpgsqlConnection cn, NpgsqlTransaction? tx, Guid tenantId, Guid? cotacaoId, CancellationToken ct)
    {
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_cotacoes
               SET status_interno = 'EXPIRADA', updated_at = now()
             WHERE tenant_id = @tenantId
               AND (@cotacaoId IS NULL OR id = @cotacaoId)
               AND prazo_resposta < now()
               AND status_interno NOT IN ('RESPONDIDA', 'CANCELADA', 'EXPIRADA')",
            new { tenantId, cotacaoId }, tx, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<CotacaoResumoDto>> ListarCotacoesAsync(Guid tenantId, string? status = null, string? provedor = null, CancellationToken ct = default)
    {
        await using var cn = Connection();
        await MarcarExpiradasPrazoExcedidoAsync(cn, null, tenantId, null, ct);
        var sql = @"
            SELECT c.id, c.provedor, c.identificador_externo, c.revisao_externa,
                   COALESCE(p.nome, c.hospital_solicitante_externo, 'Hospital não informado') AS hospital_nome,
                   c.procedimento, c.prazo_resposta, c.status_interno, c.status_externo, c.origem,
                   c.orcamento_id, c.capturada_em,
                   (SELECT COUNT(*) FROM plantaopro.adm360_cotacao_itens i WHERE i.cotacao_id = c.id AND i.tenant_id = c.tenant_id) AS total_itens,
                   (SELECT COUNT(*) FROM plantaopro.adm360_cotacao_itens i WHERE i.cotacao_id = c.id AND i.tenant_id = c.tenant_id AND i.status_relacionamento = 'PENDENTE') AS itens_pendentes
            FROM plantaopro.adm360_cotacoes c
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = c.hospital_id AND p.tenant_id = c.tenant_id
            WHERE c.tenant_id = @tenantId
              AND (@status IS NULL OR c.status_interno = @status)
              AND (@provedor IS NULL OR c.provedor = @provedor)
            ORDER BY c.prazo_resposta ASC, c.capturada_em DESC";

        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(sql, new { tenantId, status, provedor }, cancellationToken: ct));

        return rows.Select(r => new CotacaoResumoDto(
            (Guid)r.id,
            (string)r.provedor,
            (string)r.identificador_externo,
            (int)r.revisao_externa,
            (string)r.hospital_nome,
            (string?)r.procedimento,
            (DateTime)r.prazo_resposta,
            (string)r.status_interno,
            (string)r.status_externo,
            (string)r.origem,
            r.orcamento_id is not null ? (Guid?)r.orcamento_id : null,
            (int)r.total_itens,
            (int)r.itens_pendentes,
            (DateTime)r.capturada_em
        )).ToList();
    }

    public async Task<CotacaoDetalhesDto?> ObterCotacaoPorIdAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        await using var cn = Connection();
        await MarcarExpiradasPrazoExcedidoAsync(cn, null, tenantId, id, ct);
        var c = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT c.id, c.estabelecimento_id, e.razao_social AS estabelecimento_nome,
                   c.portal_conta_id, c.provedor, c.identificador_externo, c.revisao_externa,
                   c.hospital_id, p.nome AS hospital_nome, c.hospital_solicitante_externo,
                   c.paciente_iniciais, c.procedimento, c.data_prevista, c.prazo_resposta,
                   c.fuso_horario, c.status_interno, c.status_externo, c.origem,
                   c.orcamento_id, o.numero AS orcamento_numero, c.capturada_em,
                   c.cancelado_em, c.motivo_cancelamento
            FROM plantaopro.adm360_cotacoes c
            JOIN plantaopro.adm360_estabelecimentos e ON e.id = c.estabelecimento_id AND e.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros p ON p.id = c.hospital_id AND p.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.adm360_orcamentos o ON o.id = c.orcamento_id AND o.tenant_id = c.tenant_id
            WHERE c.id = @id AND c.tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (c is null) return null;

        var itensRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT i.id, i.numero_item, i.codigo_externo, i.descricao_externa,
                   i.fabricante_externo, i.modelo_externo, i.quantidade_solicitada, i.unidade_solicitada,
                   i.produto_id, pr.nome AS produto_nome, i.unidade_interna, i.fator_conversao,
                   i.quantidade_convertida, i.preco_unitario_ofertado, i.desconto, i.preco_total_ofertado,
                   i.material_ofertado, i.justificativa_substituicao, i.status_relacionamento, i.motivo_nao_atendimento
            FROM plantaopro.adm360_cotacao_itens i
            LEFT JOIN plantaopro.adm360_produtos pr ON pr.id = i.produto_id AND pr.tenant_id = i.tenant_id
            WHERE i.cotacao_id = @id AND i.tenant_id = @tenantId
            ORDER BY i.numero_item",
            new { id, tenantId }, cancellationToken: ct));

        var anexosRows = await cn.QueryAsync<dynamic>(new CommandDefinition(@"
            SELECT id, nome_arquivo, tamanho_bytes, content_type, sha256_hash, created_at
            FROM plantaopro.adm360_cotacao_anexos
            WHERE cotacao_id = @id AND tenant_id = @tenantId
            ORDER BY created_at",
            new { id, tenantId }, cancellationToken: ct));

        var itens = itensRows.Select(i => new CotacaoItemDetalheDto(
            (Guid)i.id,
            (int)i.numero_item,
            (string?)i.codigo_externo,
            (string)i.descricao_externa,
            (string?)i.fabricante_externo,
            (string?)i.modelo_externo,
            (decimal)i.quantidade_solicitada,
            (string)i.unidade_solicitada,
            i.produto_id is not null ? (Guid?)i.produto_id : null,
            (string?)i.produto_nome,
            (string?)i.unidade_interna,
            (decimal)i.fator_conversao,
            (decimal)i.quantidade_convertida,
            (decimal)i.preco_unitario_ofertado,
            (decimal)i.desconto,
            (decimal)i.preco_total_ofertado,
            (string?)i.material_ofertado,
            (string?)i.justificativa_substituicao,
            (string)i.status_relacionamento,
            (string?)i.motivo_nao_atendimento
        )).ToList();

        var anexos = anexosRows.Select(a => new CotacaoAnexoDto(
            (Guid)a.id,
            (string)a.nome_arquivo,
            (long)a.tamanho_bytes,
            (string)a.content_type,
            (string)a.sha256_hash,
            (DateTime)a.created_at
        )).ToList();

        return new CotacaoDetalhesDto(
            (Guid)c.id,
            (Guid)c.estabelecimento_id,
            (string)c.estabelecimento_nome,
            (Guid)c.portal_conta_id,
            (string)c.provedor,
            (string)c.identificador_externo,
            (int)c.revisao_externa,
            c.hospital_id is not null ? (Guid?)c.hospital_id : null,
            (string?)c.hospital_nome,
            (string?)c.hospital_solicitante_externo,
            (string?)c.paciente_iniciais,
            (string?)c.procedimento,
            c.data_prevista is not null ? ToDateOnly(c.data_prevista) : null,
            (DateTime)c.prazo_resposta,
            (string)c.fuso_horario,
            (string)c.status_interno,
            (string)c.status_externo,
            (string)c.origem,
            c.orcamento_id is not null ? (Guid?)c.orcamento_id : null,
            (string?)c.orcamento_numero,
            (DateTime)c.capturada_em,
            itens,
            anexos,
            c.cancelado_em is not null ? (DateTime?)c.cancelado_em : null,
            (string?)c.motivo_cancelamento
        );
    }

    public async Task<Guid> CapturarCotacaoAsync(Guid tenantId, Guid usuarioId, CapturarCotacaoCommand command, CancellationToken ct = default)
    {
        Guid cotacaoId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            // Busca se já existe cotacao com essa mesma conta, identificador e revisão
            var existente = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT id, revisao_externa, status_interno
                FROM plantaopro.adm360_cotacoes
                WHERE tenant_id = @tenantId
                  AND portal_conta_id = @portalContaId
                  AND identificador_externo = @identificadorExterno
                  AND revisao_externa = @revisaoExterna
                FOR UPDATE",
                new { tenantId, command.PortalContaId, command.IdentificadorExterno, command.RevisaoExterna }, tx, cancellationToken: ct));

            if (existente is not null)
            {
                // Mesma revisão: atualiza dados sem duplicar
                cotacaoId = (Guid)existente.id;
                await cn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_cotacoes
                    SET hospital_solicitante_externo = @hospital,
                        paciente_iniciais = @paciente,
                        procedimento = @procedimento,
                        data_prevista = @dataPrevista,
                        prazo_resposta = @prazoResposta,
                        payload_original = @payload,
                        updated_at = now()
                    WHERE id = @cotacaoId AND tenant_id = @tenantId",
                    new
                    {
                        cotacaoId, tenantId, hospital = command.HospitalSolicitante,
                        paciente = command.PacienteIniciais, procedimento = command.Procedimento,
                        dataPrevista = command.DataPrevista, prazoResposta = command.PrazoResposta,
                        payload = command.PayloadOriginal
                    }, tx, cancellationToken: ct));
                return;
            }

            // Nova cotação ou nova revisão
            cotacaoId = Guid.NewGuid();

            // Tenta mapear hospital automaticamente se houver correspondência no De/Para
            Guid? hospitalId = null;
            if (!string.IsNullOrWhiteSpace(command.HospitalSolicitante))
            {
                hospitalId = await cn.ExecuteScalarAsync<Guid?>(new CommandDefinition(@"
                    SELECT entidade_interna_id FROM plantaopro.adm360_mapeamentos_de_para
                    WHERE tenant_id = @tenantId AND provedor = @provedor AND tipo_entidade = 'HOSPITAL'
                      AND lower(descricao_externa) = lower(@hospital) AND situacao = 'ATIVO' LIMIT 1",
                    new { tenantId, provedor = command.Provedor.ToUpperInvariant(), hospital = command.HospitalSolicitante }, tx, cancellationToken: ct));
            }

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_cotacoes(
                    id, tenant_id, estabelecimento_id, portal_conta_id, provedor,
                    identificador_externo, revisao_externa, hospital_id, hospital_solicitante_externo,
                    paciente_iniciais, procedimento, data_prevista, prazo_resposta,
                    status_interno, status_externo, origem, payload_original, idempotency_key
                ) VALUES (
                    @cotacaoId, @tenantId, @estabelecimentoId, @portalContaId, @provedor,
                    @identificadorExterno, @revisaoExterna, @hospitalId, @hospitalSolicitante,
                    @pacienteIniciais, @procedimento, @dataPrevista, @prazoResposta,
                    'RECEBIDA', 'ABERTA', @origem, @payloadOriginal, @idempotencyKey
                )",
                new
                {
                    cotacaoId, tenantId, command.EstabelecimentoId, command.PortalContaId,
                    provedor = command.Provedor.ToUpperInvariant(), command.IdentificadorExterno,
                    command.RevisaoExterna, hospitalId, hospitalSolicitante = command.HospitalSolicitante,
                    pacienteIniciais = command.PacienteIniciais, procedimento = command.Procedimento,
                    dataPrevista = command.DataPrevista, prazoResposta = command.PrazoResposta,
                    origem = command.Origem, payloadOriginal = command.PayloadOriginal,
                    idempotencyKey = command.IdempotencyKey ?? $"COT:{tenantId:N}:{command.PortalContaId:N}:{command.IdentificadorExterno}:{command.RevisaoExterna}"
                }, tx, cancellationToken: ct));

            // Itens da cotação
            foreach (var item in command.Itens)
            {
                var itemId = Guid.NewGuid();

                // Busca se há De/Para para o produto
                Guid? produtoId = null;
                string? unidadeInterna = null;
                decimal fatorConversao = 1.0000m;
                string statusRelacionamento = "PENDENTE";

                if (!string.IsNullOrWhiteSpace(item.CodigoExterno))
                {
                    var mapeado = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                        SELECT entidade_interna_id, fator_conversao
                        FROM plantaopro.adm360_mapeamentos_de_para
                        WHERE tenant_id = @tenantId AND provedor = @provedor AND tipo_entidade = 'PRODUTO'
                          AND codigo_externo = @codigo AND situacao = 'ATIVO' LIMIT 1",
                        new { tenantId, provedor = command.Provedor.ToUpperInvariant(), codigo = item.CodigoExterno }, tx, cancellationToken: ct));

                    if (mapeado is not null && mapeado.entidade_interna_id is not null)
                    {
                        produtoId = (Guid)mapeado.entidade_interna_id;
                        fatorConversao = (decimal)mapeado.fator_conversao;
                        statusRelacionamento = "RELACIONADO";
                    }
                }

                decimal qtdConvertida = CotacaoRegras.CalcularQuantidadeConvertida(item.QuantidadeSolicitada, fatorConversao);

                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_cotacao_itens(
                        id, tenant_id, cotacao_id, numero_item, codigo_externo, descricao_externa,
                        fabricante_externo, modelo_externo, quantidade_solicitada, unidade_solicitada,
                        produto_id, unidade_interna, fator_conversao, quantidade_convertida, status_relacionamento
                    ) VALUES (
                        @itemId, @tenantId, @cotacaoId, @numeroItem, @codigoExterno, @descricaoExterna,
                        @fabricanteExterno, @modeloExterno, @quantidadeSolicitada, @unidadeSolicitada,
                        @produtoId, @unidadeInterna, @fatorConversao, @qtdConvertida, @statusRelacionamento
                    )",
                    new
                    {
                        itemId, tenantId, cotacaoId, numeroItem = item.NumeroItem,
                        codigoExterno = item.CodigoExterno, descricaoExterna = item.DescricaoExterna,
                        fabricanteExterno = item.FabricanteExterno, modeloExterno = item.ModeloExterno,
                        quantidadeSolicitada = item.QuantidadeSolicitada, unidadeSolicitada = item.UnidadeSolicitada,
                        produtoId, unidadeInterna, fatorConversao, qtdConvertida, statusRelacionamento
                    }, tx, cancellationToken: ct));
            }

            // Anexos
            if (command.Anexos is not null)
            {
                var resumoAnexos = new List<object>();
                foreach (var a in command.Anexos)
                {
                    var anexoId = Guid.NewGuid();
                    await cn.ExecuteAsync(new CommandDefinition(@"
                        INSERT INTO plantaopro.adm360_cotacao_anexos(
                            id, tenant_id, cotacao_id, nome_arquivo, tamanho_bytes, content_type, sha256_hash, conteudo
                        ) VALUES (
                            @anexoId, @tenantId, @cotacaoId, @nomeArquivo, @tamanhoBytes, @contentType, @sha256Hash, @conteudo
                        )",
                        new
                        {
                            anexoId, tenantId, cotacaoId, nomeArquivo = a.NomeArquivo,
                            tamanhoBytes = a.TamanhoBytes, contentType = a.ContentType,
                            sha256Hash = a.Sha256Hash, conteudo = a.Conteudo
                        }, tx, cancellationToken: ct));
                    resumoAnexos.Add(new { nome_arquivo = a.NomeArquivo, tamanho_bytes = a.TamanhoBytes, sha256_hash = a.Sha256Hash });
                }

                // P4: evento ARQUIVO — arquivo(s) recebido(s) com a captura (registro imutável na mesma transação)
                if (resumoAnexos.Count > 0)
                {
                    await eventos.RegistrarAsync(cn, tx, tenantId, Adm360TipoEvento.Arquivo, "COTACAO_ANEXO", cotacaoId, usuarioId,
                        $"Anexo(s) recebido(s) com a captura da cotação ({resumoAnexos.Count} arquivo(s))",
                        new { total_anexos = resumoAnexos.Count, anexos = resumoAnexos },
                        $"arquivo:captura:{tenantId:N}:{cotacaoId:N}", ct);
                }
            }
        }, ct);

        return cotacaoId;
    }

    public async Task RelacionarItemAsync(Guid tenantId, Guid usuarioId, RelacionarItemCotacaoCommand command, CancellationToken ct = default)
    {
        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var item = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT i.id, i.cotacao_id, i.quantidade_solicitada, c.status_interno
                FROM plantaopro.adm360_cotacao_itens i
                JOIN plantaopro.adm360_cotacoes c ON c.id = i.cotacao_id AND c.tenant_id = i.tenant_id
                WHERE i.id = @CotacaoItemId AND i.tenant_id = @tenantId
                FOR UPDATE",
                new { command.CotacaoItemId, tenantId }, tx, cancellationToken: ct));

            if (item is null)
                throw new KeyNotFoundException("Item de cotação não encontrado.");

            decimal fator = command.FatorConversao <= 0 ? 1.0000m : command.FatorConversao;
            decimal qtdConvertida = CotacaoRegras.CalcularQuantidadeConvertida((decimal)item.quantidade_solicitada, fator);
            decimal totalOfertado = Math.Max(0m, (command.PrecoUnitarioOfertado * qtdConvertida) - command.Desconto);

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_cotacao_itens
                SET produto_id = @ProdutoId,
                    fator_conversao = @fator,
                    quantidade_convertida = @qtdConvertida,
                    preco_unitario_ofertado = @PrecoUnitarioOfertado,
                    desconto = @Desconto,
                    preco_total_ofertado = @totalOfertado,
                    material_ofertado = @MaterialOfertado,
                    justificativa_substituicao = @JustificativaSubstituicao,
                    status_relacionamento = @StatusRelacionamento,
                    motivo_nao_atendimento = @MotivoNaoAtendimento
                WHERE id = @CotacaoItemId AND tenant_id = @tenantId",
                new
                {
                    command.CotacaoItemId, tenantId, command.ProdutoId, fator, qtdConvertida,
                    command.PrecoUnitarioOfertado, command.Desconto, totalOfertado,
                    command.MaterialOfertado, command.JustificativaSubstituicao,
                    command.StatusRelacionamento, command.MotivoNaoAtendimento
                }, tx, cancellationToken: ct));

            // Atualiza status da cotação para EM_RELACIONAMENTO se estava em RECEBIDA
            var statusAtual = (string)item.status_interno;
            if (statusAtual == "RECEBIDA")
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_cotacoes
                    SET status_interno = 'EM_RELACIONAMENTO', updated_at = now()
                    WHERE id = @cotacaoId AND tenant_id = @tenantId",
                    new { cotacaoId = (Guid)item.cotacao_id, tenantId }, tx, cancellationToken: ct));
            }
        }, ct);
    }

    public async Task<Guid> GerarOrcamentoCirurgicoAsync(Guid tenantId, Guid usuarioId, GerarOrcamentoDaCotacaoCommand command, CancellationToken ct = default)
    {
        Guid orcamentoId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            await MarcarExpiradasPrazoExcedidoAsync(cn, tx, tenantId, command.CotacaoId, ct);

            var cotacao = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT c.id, c.orcamento_id, c.hospital_id, c.procedimento, c.data_prevista,
                       c.status_interno, c.identificador_externo, c.revisao_externa
                FROM plantaopro.adm360_cotacoes c
                WHERE c.id = @CotacaoId AND c.tenant_id = @tenantId
                FOR UPDATE",
                new { command.CotacaoId, tenantId }, tx, cancellationToken: ct));

            if (cotacao is null)
                throw new KeyNotFoundException("Cotação não encontrada.");

            if (cotacao.orcamento_id is not null && (Guid)cotacao.orcamento_id != Guid.Empty)
            {
                if (!command.Revisar)
                {
                    // Idempotente: orçamento já vinculado
                    orcamentoId = (Guid)cotacao.orcamento_id;
                    return;
                }
                // B7: revisão de orçamento existente — só a partir de fases que ainda permitem ajuste.
                // O orçamento antigo permanece como snapshot histórico (itens não são apagados).
                var faseAtual = (string)cotacao.status_interno;
                if (faseAtual is not ("AGUARDANDO_APROVACAO" or "EM_ORCAMENTO"))
                    throw new Administrativo360BusinessException(
                        $"Revisão de orçamento somente é permitida nas fases AGUARDANDO_APROVACAO ou EM_ORCAMENTO (fase atual: {faseAtual}).");
            }

            var itens = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
                SELECT id, numero_item, produto_id, quantidade_convertida, preco_unitario_ofertado,
                       desconto, preco_total_ofertado, status_relacionamento, motivo_nao_atendimento
                FROM plantaopro.adm360_cotacao_itens
                WHERE cotacao_id = @CotacaoId AND tenant_id = @tenantId",
                new { command.CotacaoId, tenantId }, tx, cancellationToken: ct))).ToList();

            var pendentes = itens.Where(i => (string)i.status_relacionamento == "PENDENTE").ToList();
            if (pendentes.Count > 0)
                throw new Administrativo360BusinessException($"Não é possível gerar orçamento: existem {pendentes.Count} item(ns) pendente(s) de relacionamento de produto.");

            var itensAtendidos = itens.Where(i => (string)i.status_relacionamento == "RELACIONADO").ToList();
            if (itensAtendidos.Count == 0)
                throw new Administrativo360BusinessException("Não há itens com produtos relacionados para compor o orçamento cirúrgico.");

            // Se hospital_id for nulo, busca primeiro parceiro cadastrado como hospital/cliente
            Guid hospitalId = cotacao.hospital_id is not null
                ? (Guid)cotacao.hospital_id
                : await cn.ExecuteScalarAsync<Guid>(new CommandDefinition(@"
                    SELECT id FROM plantaopro.adm360_parceiros WHERE tenant_id = @tenantId LIMIT 1",
                    new { tenantId }, tx, cancellationToken: ct));

            if (hospitalId == Guid.Empty)
            {
                hospitalId = Guid.NewGuid();
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_parceiros(id, tenant_id, nome, ativo)
                    VALUES(@hospitalId, @tenantId, 'Hospital Solicitante', true)",
                    new { hospitalId, tenantId }, tx, cancellationToken: ct));
            }

            orcamentoId = Guid.NewGuid();
            int novaRevisao = 1;
            string numeroOrcamento;
            if (command.Revisar)
            {
                // B7: nova revisão — sufixo -V{n} evita colisão com o número do orçamento anterior
                var revisaoAnterior = await cn.ExecuteScalarAsync<int>(new CommandDefinition(@"
                    SELECT COALESCE(MAX(revisao), 0) FROM plantaopro.adm360_orcamentos
                    WHERE id = @orcId AND tenant_id = @tenantId",
                    new { orcId = (Guid)cotacao.orcamento_id, tenantId }, tx, cancellationToken: ct));
                novaRevisao = revisaoAnterior + 1;
                numeroOrcamento = $"ORC-COT-{(string)cotacao.identificador_externo}-R{(int)cotacao.revisao_externa}-V{novaRevisao}";
            }
            else
            {
                numeroOrcamento = $"ORC-COT-{(string)cotacao.identificador_externo}-R{(int)cotacao.revisao_externa}";
            }
            var dataPrev = cotacao.data_prevista is not null ? ToDateOnly(cotacao.data_prevista) : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
            var validade = dataPrev.AddDays(30);

            decimal totalProdutos = itensAtendidos.Sum(i => (decimal)i.preco_total_ofertado);

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_orcamentos(
                    id, tenant_id, numero, revisao, hospital_id, procedimento,
                    responsavel_financeiro_id, data_prevista, validade, situacao,
                    total_produtos, desconto_geral, total_geral, observacoes, created_by
                ) VALUES (
                    @orcamentoId, @tenantId, @numeroOrcamento, @novaRevisao, @hospitalId, @procedimento,
                    @hospitalId, @dataPrev, @validade, 'RASCUNHO',
                    @totalProdutos, 0, @totalProdutos, 'Gerado automaticamente a partir de Cotação Externa', @usuarioId
                ) ON CONFLICT (tenant_id, id) DO NOTHING",
                new
                {
                    orcamentoId, tenantId, numeroOrcamento, novaRevisao, hospitalId,
                    procedimento = (string)(cotacao.procedimento ?? "Procedimento Cirúrgico"),
                    dataPrev, validade, totalProdutos, usuarioId
                }, tx, cancellationToken: ct));

            foreach (var it in itensAtendidos)
            {
                var orcItemId = Guid.NewGuid();
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_orcamento_itens(
                        id, tenant_id, orcamento_id, produto_id, quantidade,
                        preco_unitario, desconto, total
                    ) VALUES (
                        @orcItemId, @tenantId, @orcamentoId, @produtoId, @quantidade,
                        @precoUnitario, @desconto, @total
                    )",
                    new
                    {
                        orcItemId, tenantId, orcamentoId, produtoId = (Guid)it.produto_id,
                        quantidade = (decimal)it.quantidade_convertida,
                        precoUnitario = (decimal)it.preco_unitario_ofertado,
                        desconto = (decimal)it.desconto,
                        total = (decimal)it.preco_total_ofertado
                    }, tx, cancellationToken: ct));
            }

            // Vincula o orçamento à cotação e avança status.
            // B7: com revisão, a cotação volta a EM_ORCAMENTO para ajuste; a aprovação só então
            // prossegue pela transição EM_ORCAMENTO -> PRONTA_PARA_ENVIO do domínio.
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_cotacoes
                SET orcamento_id = @orcamentoId,
                    status_interno = @statusNovo,
                    updated_at = now()
                WHERE id = @CotacaoId AND tenant_id = @tenantId",
                new { orcamentoId, command.CotacaoId, tenantId, statusNovo = command.Revisar ? "EM_ORCAMENTO" : "AGUARDANDO_APROVACAO" }, tx, cancellationToken: ct));
        }, ct);

        return orcamentoId;
    }

    public async Task<Guid> AprovarRespostaAsync(Guid tenantId, Guid usuarioId, AprovarRespostaCotacaoCommand command, CancellationToken ct = default)
    {
        Guid respostaId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            await MarcarExpiradasPrazoExcedidoAsync(cn, tx, tenantId, command.CotacaoId, ct);

            var cotacao = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT c.id, c.orcamento_id, c.prazo_resposta, c.status_interno,
                       c.identificador_externo, c.revisao_externa
                FROM plantaopro.adm360_cotacoes c
                WHERE c.id = @CotacaoId AND c.tenant_id = @tenantId
                FOR UPDATE",
                new { command.CotacaoId, tenantId }, tx, cancellationToken: ct));

            if (cotacao is null)
                throw new KeyNotFoundException("Cotação não encontrada.");

            CotacaoRegras.ValidarPrazoResposta((DateTime)cotacao.prazo_resposta, DateTime.UtcNow);

            // B7: guarda de transição de status (furo antigo: o fluxo só validava prazo e pendências).
            // Cancelada/expirada são terminais e bloqueiam; PRONTA_PARA_ENVIO atual é idempotente.
            CotacaoRegras.ValidarTransicaoStatus((string)cotacao.status_interno, "PRONTA_PARA_ENVIO");

            var itens = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
                SELECT numero_item, status_relacionamento, motivo_nao_atendimento, produto_id,
                       quantidade_convertida, preco_unitario_ofertado, preco_total_ofertado,
                       material_ofertado, justificativa_substituicao
                FROM plantaopro.adm360_cotacao_itens
                WHERE cotacao_id = @CotacaoId AND tenant_id = @tenantId",
                new { command.CotacaoId, tenantId }, tx, cancellationToken: ct))).ToList();

            var itensValidacao = itens.Select(i => (
                NumeroItem: (int)i.numero_item,
                StatusRelacionamento: (string)i.status_relacionamento,
                MotivoNaoAtendimento: (string?)i.motivo_nao_atendimento,
                ProdutoId: i.produto_id is not null ? (Guid?)i.produto_id : null
            )).ToList();

            CotacaoRegras.ValidarPendenciasParaEnvio(itensValidacao, cotacao.orcamento_id is not null ? (Guid?)cotacao.orcamento_id : null);

            // B7 anti-duplicidade: no máximo UMA resposta não resolvida por cotação. Se a linha já
            // existe (ex.: após estorno), ela é reutilizada com novo snapshot em vez de inserir
            // duplicada. ENVIANDO indica transmissão em voo; ACEITA_PELO_PORTAL exige estorno antes.
            // Ordem de lock preservada: cotação (acima) -> resposta (aqui). Backstop no banco:
            // índice único parcial ux_adm360_resposta_cotacao_nao_resolvida (v2314).
            var existente = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT r.id, r.status_transmissao
                  FROM plantaopro.adm360_cotacao_respostas r
                 WHERE r.cotacao_id = @CotacaoId AND r.tenant_id = @tenantId
                 ORDER BY r.created_at ASC
                 LIMIT 1
                 FOR UPDATE",
                new { command.CotacaoId, tenantId }, tx, cancellationToken: ct));

            if (existente is not null)
            {
                var statusTransmissaoExistente = (string)existente.status_transmissao;
                if (statusTransmissaoExistente == "ENVIANDO")
                    throw new Administrativo360BusinessException(
                        "Já existe transmissão em andamento para esta resposta. Aguarde o retorno do portal ou a conciliação de envios interrompidos antes de tentar novamente.");
                if (statusTransmissaoExistente == "ACEITA_PELO_PORTAL")
                    throw new Administrativo360BusinessException(
                        "A proposta desta cotação já foi aceita pelo portal. Estorne a resposta antes de gerar uma nova aprovação.");
            }

            // Snapshot imutável da resposta
            var snapshotObj = new
            {
                CotacaoId = (Guid)cotacao.id,
                IdentificadorExterno = (string)cotacao.identificador_externo,
                Revisao = (int)cotacao.revisao_externa,
                AprovadoPor = usuarioId,
                AprovadoEm = DateTime.UtcNow,
                Itens = itens.Select(i => new
                {
                    i.numero_item,
                    i.produto_id,
                    i.quantidade_convertida,
                    i.preco_unitario_ofertado,
                    i.preco_total_ofertado,
                    i.material_ofertado,
                    i.justificativa_substituicao,
                    i.status_relacionamento
                })
            };

            var snapshotJson = JsonSerializer.Serialize(snapshotObj);
            bool novaResposta = existente is null;
            if (!novaResposta) respostaId = (Guid)existente.id; else respostaId = Guid.NewGuid();

            if (novaResposta)
            {
                await cn.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_cotacao_respostas(
                        id, tenant_id, cotacao_id, orcamento_id, revisao,
                        snapshot_proposta, status_transmissao, status_comercial_externo,
                        tentativas, proxima_tentativa, aprovado_por, aprovado_em
                    ) VALUES (
                        @respostaId, @tenantId, @CotacaoId, @orcamentoId, @revisao,
                        @snapshotJson::jsonb, 'NA_FILA', 'AGUARDANDO_DECISAO',
                        0, now(), @usuarioId, now()
                    )",
                    new
                    {
                        respostaId, tenantId, command.CotacaoId, orcamentoId = (Guid)cotacao.orcamento_id,
                        revisao = (int)cotacao.revisao_externa, snapshotJson, usuarioId
                    }, tx, cancellationToken: ct));
            }
            else
            {
                // B7 reuso da fila: reconstrói o snapshot com os itens atuais (inclui orçamento de
                // revisão, quando houve) e devolve a resposta à fila. Histórico preservado:
                // protocolo_externo, mensagem_retorno e tentativas não são tocados.
                await cn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_cotacao_respostas
                    SET orcamento_id = @orcamentoId,
                        snapshot_proposta = @snapshotJson::jsonb,
                        status_transmissao = 'NA_FILA',
                        status_comercial_externo = 'AGUARDANDO_DECISAO',
                        revisao = @revisao,
                        proxima_tentativa = now(),
                        aprovado_por = @usuarioId,
                        aprovado_em = now(),
                        updated_at = now()
                    WHERE id = @respostaId AND tenant_id = @tenantId",
                    new
                    {
                        orcamentoId = (Guid)cotacao.orcamento_id,
                        revisao = (int)cotacao.revisao_externa, snapshotJson, usuarioId,
                        respostaId, tenantId
                    }, tx, cancellationToken: ct));
            }

            // P4: evento APROVACAO — decisão de aprovação registrada imutavelmente na mesma transação.
            // B7: chave de idempotência apenas na primeira criação da resposta; o reuso registra
            // o próprio fato sem chave para não ser descartado pelo ON CONFLICT DO NOTHING.
            await eventos.RegistrarAsync(cn, tx, tenantId, Adm360TipoEvento.Aprovacao, "COTACAO_RESPOSTA", respostaId, usuarioId,
                novaResposta ? "Proposta aprovada para envio ao portal" : "Proposta re-aprovada apos estorno (reuso da resposta existente)",
                new { cotacao_id = (Guid)cotacao.id, orcamento_id = (Guid)cotacao.orcamento_id, revisao = (int)cotacao.revisao_externa, reuso = !novaResposta },
                novaResposta ? $"aprovacao:resposta:{respostaId:N}" : null, ct);

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_cotacoes
                SET status_interno = 'PRONTA_PARA_ENVIO', updated_at = now()
                WHERE id = @CotacaoId AND tenant_id = @tenantId",
                new { command.CotacaoId, tenantId }, tx, cancellationToken: ct));
        }, ct);

        return respostaId;
    }

    public async Task<CotacaoRespostaDto?> ObterRespostaPorIdAsync(Guid tenantId, Guid respostaId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var r = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT r.id, r.cotacao_id, c.identificador_externo, r.orcamento_id, r.revisao,
                   r.status_transmissao, r.status_comercial_externo, r.tentativas,
                   r.proxima_tentativa, r.protocolo_externo, r.mensagem_retorno,
                   r.created_at, r.enviado_em,
                   x.id AS exportacao_id, x.nome_arquivo AS exportacao_nome_arquivo, x.sha256_hash AS exportacao_sha256_hash
            FROM plantaopro.adm360_cotacao_respostas r
            JOIN plantaopro.adm360_cotacoes c ON c.id = r.cotacao_id AND c.tenant_id = r.tenant_id
            LEFT JOIN plantaopro.adm360_cotacao_exportacoes x ON x.resposta_id = r.id AND x.tenant_id = r.tenant_id
            WHERE r.id = @respostaId AND r.tenant_id = @tenantId",
            new { respostaId, tenantId }, cancellationToken: ct));

        if (r is null) return null;

        return new CotacaoRespostaDto(
            (Guid)r.id,
            (Guid)r.cotacao_id,
            (string)r.identificador_externo,
            (Guid)r.orcamento_id,
            (int)r.revisao,
            (string)r.status_transmissao,
            (string)r.status_comercial_externo,
            (int)r.tentativas,
            (DateTime)r.proxima_tentativa,
            (string?)r.protocolo_externo,
            (string?)r.mensagem_retorno,
            (DateTime)r.created_at,
            r.enviado_em is not null ? (DateTime?)r.enviado_em : null,
            (Guid?)r.exportacao_id,
            (string?)r.exportacao_nome_arquivo,
            (string?)r.exportacao_sha256_hash
        );
    }

    public async Task<IReadOnlyList<CotacaoRespostaDto>> ListarRespostasAsync(Guid tenantId, string? status = null, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var sql = @"
            SELECT r.id, r.cotacao_id, c.identificador_externo, r.orcamento_id, r.revisao,
                   r.status_transmissao, r.status_comercial_externo, r.tentativas,
                   r.proxima_tentativa, r.protocolo_externo, r.mensagem_retorno,
                   r.created_at, r.enviado_em,
                   x.id AS exportacao_id, x.nome_arquivo AS exportacao_nome_arquivo, x.sha256_hash AS exportacao_sha256_hash
            FROM plantaopro.adm360_cotacao_respostas r
            JOIN plantaopro.adm360_cotacoes c ON c.id = r.cotacao_id AND c.tenant_id = r.tenant_id
            LEFT JOIN plantaopro.adm360_cotacao_exportacoes x ON x.resposta_id = r.id AND x.tenant_id = r.tenant_id
            WHERE r.tenant_id = @tenantId
              AND (@status IS NULL OR r.status_transmissao = @status)
            ORDER BY r.created_at DESC";

        var rows = await cn.QueryAsync<dynamic>(new CommandDefinition(sql, new { tenantId, status }, cancellationToken: ct));

        return rows.Select(r => new CotacaoRespostaDto(
            (Guid)r.id,
            (Guid)r.cotacao_id,
            (string)r.identificador_externo,
            (Guid)r.orcamento_id,
            (int)r.revisao,
            (string)r.status_transmissao,
            (string)r.status_comercial_externo,
            (int)r.tentativas,
            (DateTime)r.proxima_tentativa,
            (string?)r.protocolo_externo,
            (string?)r.mensagem_retorno,
            (DateTime)r.created_at,
            r.enviado_em is not null ? (DateTime?)r.enviado_em : null,
            (Guid?)r.exportacao_id,
            (string?)r.exportacao_nome_arquivo,
            (string?)r.exportacao_sha256_hash
        )).ToList();
    }

    public async Task TransmitirRespostaAsync(Guid tenantId, Guid usuarioId, TransmitirRespostaCommand command, CancellationToken ct = default)
    {
        var resp = await ObterRespostaPorIdAsync(tenantId, command.RespostaId, ct);
        if (resp is null)
            throw new KeyNotFoundException("Registro de resposta na fila não encontrado.");

        var cotacao = await ObterCotacaoPorIdAsync(tenantId, resp.CotacaoId, ct);
        if (cotacao is null)
            throw new KeyNotFoundException("Cotação vinculada não encontrada.");

        var contas = await ListarContasPortalAsync(tenantId, ct);
        var conta = contas.FirstOrDefault(c => c.Id == cotacao.PortalContaId)
            ?? new PortalContaDto(Guid.Empty, cotacao.EstabelecimentoId, cotacao.EstabelecimentoNome, cotacao.Provedor, "Canal Direto", null, null, "HOMOLOGACAO", "NAO_CONFIGURADA", "Conta não encontrada", null, true);

        var conector = ObterConector(cotacao.Provedor);

        // B5: etapa 1 — transação CURTA de marcação com lock de linha (SELECT ... FOR UPDATE).
        // Guardas de máquina de estados avaliam o estado REAL sob lock, então duas transmissões
        // simultâneas nunca ambas avançam: a segunda lê ENVIANDO e é rejeitada com mensagem clara.
        // A chamada externa do conector acontece FORA de qualquer transação de banco.
        var attemptId = Guid.NewGuid();
        var novaTentativa = resp.Tentativas + 1;
        await using (var cnMarca = Connection())
        {
            await cnMarca.OpenAsync(ct);
            await using var txMarca = await cnMarca.BeginTransactionAsync(ct);
            try
            {
                // B7: ordem de lock — cotação ANTES da resposta. Todos os fluxos de escrita
                // (aprovar, cancelar, estornar e transmitir) seguem a mesma ordem para evitar
                // deadlock. A expiração lazy do prazo entra no mesmo escopo de linha.
                await MarcarExpiradasPrazoExcedidoAsync(cnMarca, txMarca, tenantId, resp.CotacaoId, ct);

                var statusCotacao = await cnMarca.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                    SELECT c.status_interno, c.prazo_resposta::timestamp AS prazo_resposta
                      FROM plantaopro.adm360_cotacoes c
                     WHERE c.id = @cotacaoId AND c.tenant_id = @tenantId
                     FOR UPDATE",
                    new { cotacaoId = resp.CotacaoId, tenantId }, txMarca, cancellationToken: ct));

                if (statusCotacao is null)
                    throw new KeyNotFoundException("Cotação vinculada não encontrada.");

                if ((string)statusCotacao.status_interno == "CANCELADA")
                    throw new Administrativo360BusinessException("A cotação vinculada foi cancelada internamente e não pode ser transmitida.");

                // Prazo excedido (estado EXPIRADA ou ainda não marcado): mensagem ancorada do domínio.
                CotacaoRegras.ValidarPrazoResposta((DateTime)statusCotacao.prazo_resposta, DateTime.UtcNow);

                var locked = await cnMarca.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                    SELECT r.status_transmissao, r.tentativas, r.protocolo_externo
                      FROM plantaopro.adm360_cotacao_respostas r
                     WHERE r.id = @respostaId AND r.tenant_id = @tenantId
                     FOR UPDATE",
                    new { respostaId = command.RespostaId, tenantId }, txMarca, cancellationToken: ct));

                if (locked is null)
                    throw new KeyNotFoundException("Registro de resposta na fila não encontrado.");

                var statusAtual = (string)locked.status_transmissao;
                if (statusAtual == "ACEITA_PELO_PORTAL")
                    throw new Administrativo360BusinessException($"A transmissão desta proposta já foi concluída e aceita pelo portal com protocolo '{(string?)locked.protocolo_externo}'. Não é permitida retransmissão.");

                CotacaoRegras.ValidarRetransmissaoPermitida(statusAtual, command.ConfirmarRetransmissaoDeDesconhecido);

                novaTentativa = (int)locked.tentativas + 1;

                // Tentativa PERSISTIDA: uma linha por envio (UNIQUE tenant+resposta+tentativa).
                // É a evidência usada por reconciliação, auditoria e bloqueio de retransmissão cega.
                await cnMarca.ExecuteAsync(new CommandDefinition(@"
                    INSERT INTO plantaopro.adm360_cotacao_envios(
                        id, tenant_id, resposta_id, tentativa, canal, status_transmissao, iniciado_em, created_by
                    ) VALUES (
                        @attemptId, @tenantId, @respostaId, @tentativa, @canal, 'INICIADA', now(), @usuarioId
                    )",
                    new { attemptId, tenantId, respostaId = command.RespostaId, tentativa = novaTentativa, canal = cotacao.Provedor, usuarioId }, txMarca, cancellationToken: ct));

                await cnMarca.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_cotacao_respostas
                    SET status_transmissao = 'ENVIANDO', tentativas = @novaTentativa, updated_at = now()
                    WHERE id = @respostaId AND tenant_id = @tenantId",
                    new { novaTentativa, respostaId = command.RespostaId, tenantId }, txMarca, cancellationToken: ct));

                await txMarca.CommitAsync(ct);
            }
            catch
            {
                try { await txMarca.RollbackAsync(ct); } catch { /* rollback best-effort */ }
                throw;
            }
        }

        // B5: etapa 2 — chamada EXTERNA fora de transação, com prazo próprio de comunicação.
        // Timeout ou exceção do conector viram estado honesto RESULTADO_DESCONHECIDO (o portal
        // pode ter recebido); cancelamento externo propaga (envio fica p/ conciliação).
        EnvioRespostaPortalResult resultado;
        try
        {
            using var ctsLigado = CancellationTokenSource.CreateLinkedTokenSource(ct);
            ctsLigado.CancelAfter(TimeSpan.FromSeconds(30));
            resultado = await conector.TransmitirPropostaAsync(conta, resp, cotacao, ctsLigado.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            resultado = new EnvioRespostaPortalResult(false, "RESULTADO_DESCONHECIDO", null,
                "Tempo limite de comunicação com o portal excedido (30 s). O retorno do envio não pôde ser confirmado.");
        }
        catch (Exception ex)
        {
            resultado = new EnvioRespostaPortalResult(false, "RESULTADO_DESCONHECIDO", null,
                "Falha técnica na transmissão (exceção do conector): " + ex.Message);
        }

        // B5: etapa 3 — transação CURTA de finalização: estado final + fechamento da tentativa
        // persistida + arquivo imutável do canal manual + evento + status da cotação.
        // O guard 'status_transmissao = ENVIANDO' impede sobrescrever estado conciliado.
        await using (var cnFinal = Connection())
        {
            await cnFinal.OpenAsync(ct);
            await using var txFinal = await cnFinal.BeginTransactionAsync(ct);
            try
            {
                // B7 finalização honesta: a resposta só é atualizada quando ainda está ENVIANDO
                // (guard na WHERE). Se outro escritor (estorno, recovery de boot/cíclico ou
                // cancelamento) mudou o estado durante a chamada externa, as linhas afetadas
                // são 0 e NENHUM campo da resposta é sobrescrito.
                var linhasResposta = await cnFinal.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_cotacao_respostas
                    SET status_transmissao = @status,
                        protocolo_externo = @protocolo,
                        mensagem_retorno = @mensagem,
                        enviado_em = (CASE WHEN @sucesso THEN now() ELSE enviado_em END),
                        updated_at = now()
                    WHERE id = @id AND tenant_id = @tenantId AND status_transmissao = 'ENVIANDO'",
                    new
                    {
                        id = command.RespostaId, tenantId,
                        status = resultado.StatusTransmissao,
                        protocolo = resultado.Protocolo,
                        mensagem = resultado.Mensagem,
                        sucesso = resultado.Sucesso
                    }, txFinal, cancellationToken: ct));

                string? estadoConciliado = null;
                if (linhasResposta == 0)
                {
                    estadoConciliado = await cnFinal.QuerySingleOrDefaultAsync<string?>(new CommandDefinition(@"
                        SELECT status_transmissao FROM plantaopro.adm360_cotacao_respostas
                        WHERE id = @id AND tenant_id = @tenantId",
                        new { id = command.RespostaId, tenantId }, txFinal, cancellationToken: ct));
                }

                var mensagemEnvio = resultado.Mensagem ?? "(sem mensagem do portal)";
                if (linhasResposta == 0)
                {
                    // Registro da tentativa conciliado com o estado real: o status do envio
                    // permanece o resultado devolvido pelo conector (CHECK da tabela), e a
                    // divergência fica documentada na própria linha de auditoria.
                    mensagemEnvio = $"{mensagemEnvio} | Finalizacao honesta (B7): o estado da resposta foi conciliado para '{estadoConciliado}' durante a transmissao; nenhum campo da resposta foi sobrescrito.";
                }

                await cnFinal.ExecuteAsync(new CommandDefinition(@"
                    UPDATE plantaopro.adm360_cotacao_envios
                    SET status_transmissao = @status,
                        protocolo_externo = @protocolo,
                        mensagem = left(@mensagem, 2000),
                        finalizado_em = now()
                    WHERE id = @attemptId AND tenant_id = @tenantId",
                    new { status = resultado.StatusTransmissao, protocolo = resultado.Protocolo, mensagem = mensagemEnvio, attemptId, tenantId }, txFinal, cancellationToken: ct));

                // Canal manual: gera e registra o arquivo REAL da proposta aprovada (imutável;
                // a primeira geração vence — retransmissões preservam o arquivo original).
                // P1 (homologação): exportar ≠ respondida — este passo apenas persiste o artefato;
                // a cotação mantém o estado anterior até haver aceite real do destinatário.
                if (resultado.StatusTransmissao == "EXPORTADA_MANUALMENTE")
                {
                    await RegistrarExportacaoManualAsync(cnFinal, txFinal, tenantId, usuarioId, command.RespostaId, ct);
                }

                // P1 (homologação): apenas o ACEITE confirmado pelo portal externo avança a
                // cotação para RESPONDIDA. A exportação manual não representa resposta do
                // destinatário — separar preparado/baixado/manual de aceito/rejeitado/desconhecido.
                // B7: o flip só ocorre quando a resposta efetivamente estava ENVIANDO e foi
                // finalizada por este envio (linhasResposta > 0); se conciliada por outro
                // escritor, a cotação não é tocada.
                if (linhasResposta > 0 && resultado.Sucesso && resultado.StatusTransmissao == "ACEITA_PELO_PORTAL")
                {
                    await cnFinal.ExecuteAsync(new CommandDefinition(@"
                        UPDATE plantaopro.adm360_cotacoes
                        SET status_interno = 'RESPONDIDA', updated_at = now()
                        WHERE id = @cotacaoId AND tenant_id = @tenantId",
                        new { cotacaoId = resp.CotacaoId, tenantId }, txFinal, cancellationToken: ct));
                }

                // P4/B5: evento RETORNO_EXTERNO — o retorno da transmissão é sempre registrado (um por tentativa)
                await eventos.RegistrarAsync(cnFinal, txFinal, tenantId, Adm360TipoEvento.RetornoExterno, "COTACAO_RESPOSTA", command.RespostaId, usuarioId,
                    $"Retorno da transmissão da resposta: {resultado.StatusTransmissao}",
                    new { status_final = resultado.StatusTransmissao, protocolo_externo = resultado.Protocolo, mensagem_retorno = resultado.Mensagem, estado_resposta_conciliado = estadoConciliado },
                    null, ct);

                await txFinal.CommitAsync(ct);
            }
            catch
            {
                try { await txFinal.RollbackAsync(ct); } catch { /* rollback best-effort */ }
                throw;
            }
        }
    }

    private async Task RegistrarExportacaoManualAsync(NpgsqlConnection cn, NpgsqlTransaction? tx, Guid tenantId, Guid? usuarioId, Guid respostaId, CancellationToken ct)
    {
        var meta = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT r.cotacao_id, r.snapshot_proposta::text AS snapshot_text,
                   r.aprovado_por, r.aprovado_em,
                   c.identificador_externo, c.revisao_externa
            FROM plantaopro.adm360_cotacao_respostas r
            JOIN plantaopro.adm360_cotacoes c ON c.id = r.cotacao_id AND c.tenant_id = r.tenant_id
            WHERE r.id = @respostaId AND r.tenant_id = @tenantId",
            new { respostaId, tenantId }, tx, cancellationToken: ct));

        if (meta is null) return;

        System.Text.Json.Nodes.JsonNode? snapshotNode;
        try
        {
            snapshotNode = System.Text.Json.Nodes.JsonNode.Parse((string)meta.snapshot_text);
        }
        catch (JsonException)
        {
            snapshotNode = null;
        }

        var documento = new
        {
            tipo_documento = "PROPOSTA_APROVADA_EXPORTACAO_MANUAL",
            cotacao = new { identificador_externo = (string)meta.identificador_externo, revisao_externa = (int)meta.revisao_externa },
            aprovacao = new
            {
                aprovado_por = meta.aprovado_por is null ? null : ((Guid)meta.aprovado_por).ToString(),
                aprovado_em = meta.aprovado_em is null ? null : ((DateTime)meta.aprovado_em).ToString("O")
            },
            gerado_em_utc = DateTime.UtcNow.ToString("O"),
            proposta_aprovada = snapshotNode
        };

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(documento, new JsonSerializerOptions { WriteIndented = true }));
        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var nomeArquivo = $"COT_{meta.identificador_externo}_R{(int)meta.revisao_externa}_PROPOSTA_EXPORTADA.json";

        var exportacaoId = Guid.NewGuid();

        // Imutabilidade de aplicação: UNIQUE(tenant_id, resposta_id) + DO NOTHING =>
        // retransmissões nunca substituem o arquivo da primeira geração.
        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_cotacao_exportacoes(
                id, tenant_id, cotacao_id, resposta_id, nome_arquivo, tamanho_bytes, content_type, sha256_hash, conteudo
            ) VALUES (
                @id, @tenantId, @cotacaoId, @respostaId, @nomeArquivo, @tamanhoBytes, 'application/json', @sha256, @conteudo
            )
            ON CONFLICT (tenant_id, resposta_id) DO NOTHING",
            new
            {
                id = exportacaoId, tenantId, cotacaoId = (Guid)meta.cotacao_id, respostaId,
                nomeArquivo, tamanhoBytes = bytes.Length, sha256, conteudo = bytes
            }, tx, cancellationToken: ct));

        // P4: evento ARQUIVO — arquivo real da exportação (idempotente por resposta: a primeira geração vence)
        await eventos.RegistrarAsync(cn, tx, tenantId, Adm360TipoEvento.Arquivo, "EXPORTACAO", exportacaoId, usuarioId,
            $"Arquivo de exportação gerado: {nomeArquivo}",
            new { nome_arquivo = nomeArquivo, tamanho_bytes = bytes.Length, sha256_hash = sha256 },
            $"arquivo:exportacao:{respostaId:N}", ct);
    }

    public async Task<(byte[]? Bytes, string Nome, string ContentType, string Sha256Hash)?> ObterAnexoAsync(Guid tenantId, Guid anexoId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var a = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT nome_arquivo, content_type, sha256_hash, conteudo
            FROM plantaopro.adm360_cotacao_anexos
            WHERE id = @anexoId AND tenant_id = @tenantId",
            new { anexoId, tenantId }, cancellationToken: ct));

        if (a is null) return null;

        return ((byte[]?)a.conteudo, (string)a.nome_arquivo, (string)a.content_type, (string)a.sha256_hash);
    }

    public async Task<CotacaoExportacaoArquivoDto?> ObterExportacaoPorRespostaAsync(Guid tenantId, Guid respostaId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var row = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT x.id, x.tenant_id, x.cotacao_id, x.resposta_id, x.nome_arquivo, x.tamanho_bytes,
                   x.content_type, x.sha256_hash, x.gerado_em, x.conteudo
            FROM plantaopro.adm360_cotacao_exportacoes x
            JOIN plantaopro.adm360_cotacao_respostas r ON r.id = x.resposta_id
            WHERE x.resposta_id = @respostaId AND x.tenant_id = @tenantId AND r.tenant_id = @tenantId",
            new { respostaId, tenantId }, cancellationToken: ct));

        if (row is null) return null;

        return new CotacaoExportacaoArquivoDto(
            (Guid)row.id,
            (Guid)row.tenant_id,
            (Guid)row.cotacao_id,
            (Guid)row.resposta_id,
            (string)row.nome_arquivo,
            (int)row.tamanho_bytes,
            (string)row.content_type,
            (string)row.sha256_hash,
            (DateTime)row.gerado_em,
            (byte[])row.conteudo);
    }

    // B7: cancelamento auditável da cotação — estado terminal com momento e motivo registrados.
    // Idempotente: cotação já CANCELADA não altera nada nem duplica evento (chave estável).
    // Bloqueia quando existe resposta ENVIANDO (transmissão em voo: a finalização honesta
    // ou a reconciliação precisam fechar a tentativa antes).
    public async Task CancelarCotacaoAsync(Guid tenantId, Guid usuarioId, CancelarCotacaoCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Motivo))
            throw new Administrativo360BusinessException("O motivo do cancelamento da cotação é obrigatório.");

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            await MarcarExpiradasPrazoExcedidoAsync(cn, tx, tenantId, command.CotacaoId, ct);

            var cotacao = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT c.id, c.status_interno
                  FROM plantaopro.adm360_cotacoes c
                 WHERE c.id = @CotacaoId AND c.tenant_id = @tenantId
                 FOR UPDATE",
                new { command.CotacaoId, tenantId }, tx, cancellationToken: ct));

            if (cotacao is null)
                throw new KeyNotFoundException("Cotação não encontrada.");

            var statusAtual = (string)cotacao.status_interno;

            // Idempotência: cancelamento já registrado
            if (statusAtual == "CANCELADA") return;

            var enviando = await cn.ExecuteScalarAsync<int>(new CommandDefinition(@"
                SELECT count(*) FROM plantaopro.adm360_cotacao_respostas r
                WHERE r.cotacao_id = @CotacaoId AND r.tenant_id = @tenantId AND r.status_transmissao = 'ENVIANDO'",
                new { command.CotacaoId, tenantId }, tx, cancellationToken: ct));

            if (enviando > 0)
                throw new Administrativo360BusinessException(
                    "Não é possível cancelar a cotação com transmissão em andamento (resposta ENVIANDO). Aguarde o retorno do portal ou a conciliação de envios interrompidos antes de tentar novamente.");

            CotacaoRegras.ValidarTransicaoStatus(statusAtual, "CANCELADA");

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_cotacoes
                SET status_interno = 'CANCELADA',
                    cancelado_em = now(),
                    motivo_cancelamento = @motivo,
                    updated_at = now()
                WHERE id = @CotacaoId AND tenant_id = @tenantId",
                new { command.CotacaoId, tenantId, motivo = command.Motivo }, tx, cancellationToken: ct));

            // P4/B7: evento CANCELAMENTO — chave estável por cotação: re-cancelamentos
            // (idempotência) não duplicam o fato no log imutável.
            await eventos.RegistrarAsync(cn, tx, tenantId, Adm360TipoEvento.Cancelamento, "COTACAO", (Guid)cotacao.id, usuarioId,
                "Cotação cancelada",
                new { status_anterior = statusAtual, motivo = command.Motivo },
                $"cancelamento:cotacao:{cotacao.id:N}", ct);
        }, ct);
    }

    // B7: estorno de resposta de cotação — devolve a fila para reprocessamento sem apagar
    // histórico: protocolo anterior e mensagens persistidas são preservados e a justificativa
    // é anexada ao fim de mensagem_retorno. Se o aceite já tinha avançado a cotação para
    // RESPONDIDA, o estorno a devolve a PRONTA_PARA_ENVIO (novo ciclo de transmissão).
    // ENVIANDO é bloqueado: a I/O externa em voo deve ser finalizada/reconciliada primeiro.
    public async Task EstornarRespostaAsync(Guid tenantId, Guid usuarioId, EstornarRespostaCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Justificativa))
            throw new Administrativo360BusinessException("A justificativa do estorno é obrigatória.");

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            // Ordem de lock: cotação ANTES da resposta (mesma ordem de todos os fluxos de escrita)
            var cotacao = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT c.id
                  FROM plantaopro.adm360_cotacoes c
                 WHERE c.id = (SELECT cotacao_id FROM plantaopro.adm360_cotacao_respostas
                                WHERE id = @respostaId AND tenant_id = @tenantId)
                   AND c.tenant_id = @tenantId
                 FOR UPDATE",
                new { respostaId = command.RespostaId, tenantId }, tx, cancellationToken: ct));

            if (cotacao is null)
                throw new KeyNotFoundException("Registro de resposta ou sua cotação vinculada não encontrados.");

            var cotacaoId = (Guid)cotacao.id;

            var resposta = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT r.id, r.status_transmissao, r.mensagem_retorno
                  FROM plantaopro.adm360_cotacao_respostas r
                 WHERE r.id = @respostaId AND r.tenant_id = @tenantId
                 FOR UPDATE",
                new { respostaId = command.RespostaId, tenantId }, tx, cancellationToken: ct));

            if (resposta is null)
                throw new KeyNotFoundException("Registro de resposta na fila não encontrado.");

            var statusAtual = (string)resposta.status_transmissao;

            if (statusAtual == "ENVIANDO")
                throw new Administrativo360BusinessException(
                    "Esta resposta está com transmissão em andamento e não pode ser estornada. Aguarde o retorno do portal ou a conciliação de envios interrompidos antes de tentar novamente.");

            var mensagemNov = ((string?)resposta.mensagem_retorno ?? "") + " | Estorno (B7): " + command.Justificativa;

            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_cotacao_respostas
                SET status_transmissao = 'NA_FILA',
                    status_comercial_externo = 'AGUARDANDO_DECISAO',
                    proxima_tentativa = now(),
                    mensagem_retorno = left(@mensagemNov, 2000),
                    updated_at = now()
                WHERE id = @respostaId AND tenant_id = @tenantId",
                new { mensagemNov, respostaId = command.RespostaId, tenantId }, tx, cancellationToken: ct));

            // Aceite já aplicado à cotação é anulado: volta à espera de novo envio.
            // Cotas em CANCELADA/EXPIRADA/PRONTA_PARA_ENVIO não são tocadas.
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_cotacoes
                SET status_interno = 'PRONTA_PARA_ENVIO', updated_at = now()
                WHERE id = @cotacaoId AND tenant_id = @tenantId
                  AND status_interno = 'RESPONDIDA'",
                new { cotacaoId, tenantId }, tx, cancellationToken: ct));

            // P4/B7: evento ESTORNO — sem chave de idempotência: cada estorno é um fato
            // distinto (registrado_em diferenciando conteúdo idêntico).
            await eventos.RegistrarAsync(cn, tx, tenantId, Adm360TipoEvento.Estorno, "COTACAO_RESPOSTA", (Guid)resposta.id, usuarioId,
                "Resposta estornada e devolvida à fila",
                new { cotacao_id = cotacaoId, status_anterior = statusAtual, justificativa = command.Justificativa, registrado_em = DateTime.UtcNow.ToString("O") },
                null, ct);
        }, ct);
    }
}
