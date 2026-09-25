using System.Text.Json;
using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class CotacoesRepository : Adm360Repository, ICotacoesRepository
{
    private readonly IEnumerable<IPortalCotacaoConnector> connectors;

    public CotacoesRepository(string connectionString, IEnumerable<IPortalCotacaoConnector>? connectors = null)
        : base(connectionString)
    {
        this.connectors = connectors ?? new IPortalCotacaoConnector[] { new OpmenexoConnector(), new InpartConnector(), new ImportacaoManualConnector() };
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

    public async Task<IReadOnlyList<CotacaoResumoDto>> ListarCotacoesAsync(Guid tenantId, string? status = null, string? provedor = null, CancellationToken ct = default)
    {
        await using var cn = Connection();
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
        var c = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT c.id, c.estabelecimento_id, e.razao_social AS estabelecimento_nome,
                   c.portal_conta_id, c.provedor, c.identificador_externo, c.revisao_externa,
                   c.hospital_id, p.nome AS hospital_nome, c.hospital_solicitante_externo,
                   c.paciente_iniciais, c.procedimento, c.data_prevista, c.prazo_resposta,
                   c.fuso_horario, c.status_interno, c.status_externo, c.origem,
                   c.orcamento_id, o.numero AS orcamento_numero, c.capturada_em
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
            anexos
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
                // Idempotente: orçamento já vinculado
                orcamentoId = (Guid)cotacao.orcamento_id;
                return;
            }

            var itens = (await cn.QueryAsync<dynamic>(new CommandDefinition(@"
                SELECT id, numero_item, produto_id, quantidade_convertida, preco_unitario_ofertado,
                       desconto, preco_total_ofertado, status_relacionamento, motivo_nao_atendimento
                FROM plantaopro.adm360_cotacao_itens
                WHERE cotacao_id = @CotacaoId AND tenant_id = @tenantId",
                new { command.CotacaoId, tenantId }, tx, cancellationToken: ct))).ToList();

            var pendentes = itens.Where(i => (string)i.status_relacionamento == "PENDENTE").ToList();
            if (pendentes.Count > 0)
                throw new InvalidOperationException($"Não é possível gerar orçamento: existem {pendentes.Count} item(ns) pendente(s) de relacionamento de produto.");

            var itensAtendidos = itens.Where(i => (string)i.status_relacionamento == "RELACIONADO").ToList();
            if (itensAtendidos.Count == 0)
                throw new InvalidOperationException("Não há itens com produtos relacionados para compor o orçamento cirúrgico.");

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
            var numeroOrcamento = $"ORC-COT-{(string)cotacao.identificador_externo}-R{(int)cotacao.revisao_externa}";
            var dataPrev = cotacao.data_prevista is not null ? ToDateOnly(cotacao.data_prevista) : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));
            var validade = dataPrev.AddDays(30);

            decimal totalProdutos = itensAtendidos.Sum(i => (decimal)i.preco_total_ofertado);

            await cn.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO plantaopro.adm360_orcamentos(
                    id, tenant_id, numero, revisao, hospital_id, procedimento,
                    responsavel_financeiro_id, data_prevista, validade, situacao,
                    total_produtos, desconto_geral, total_geral, observacoes, created_by
                ) VALUES (
                    @orcamentoId, @tenantId, @numeroOrcamento, 1, @hospitalId, @procedimento,
                    @hospitalId, @dataPrev, @validade, 'RASCUNHO',
                    @totalProdutos, 0, @totalProdutos, 'Gerado automaticamente a partir de Cotação Externa', @usuarioId
                ) ON CONFLICT (tenant_id, id) DO NOTHING",
                new
                {
                    orcamentoId, tenantId, numeroOrcamento, hospitalId,
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

            // Vincula o orçamento à cotação e avança status
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_cotacoes
                SET orcamento_id = @orcamentoId,
                    status_interno = 'AGUARDANDO_APROVACAO',
                    updated_at = now()
                WHERE id = @CotacaoId AND tenant_id = @tenantId",
                new { orcamentoId, command.CotacaoId, tenantId }, tx, cancellationToken: ct));
        }, ct);

        return orcamentoId;
    }

    public async Task<Guid> AprovarRespostaAsync(Guid tenantId, Guid usuarioId, AprovarRespostaCotacaoCommand command, CancellationToken ct = default)
    {
        Guid respostaId = Guid.Empty;

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
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
            respostaId = Guid.NewGuid();

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
                   r.created_at, r.enviado_em
            FROM plantaopro.adm360_cotacao_respostas r
            JOIN plantaopro.adm360_cotacoes c ON c.id = r.cotacao_id AND c.tenant_id = r.tenant_id
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
            r.enviado_em is not null ? (DateTime?)r.enviado_em : null
        );
    }

    public async Task<IReadOnlyList<CotacaoRespostaDto>> ListarRespostasAsync(Guid tenantId, string? status = null, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var sql = @"
            SELECT r.id, r.cotacao_id, c.identificador_externo, r.orcamento_id, r.revisao,
                   r.status_transmissao, r.status_comercial_externo, r.tentativas,
                   r.proxima_tentativa, r.protocolo_externo, r.mensagem_retorno,
                   r.created_at, r.enviado_em
            FROM plantaopro.adm360_cotacao_respostas r
            JOIN plantaopro.adm360_cotacoes c ON c.id = r.cotacao_id AND c.tenant_id = r.tenant_id
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
            r.enviado_em is not null ? (DateTime?)r.enviado_em : null
        )).ToList();
    }

    public async Task TransmitirRespostaAsync(Guid tenantId, Guid usuarioId, TransmitirRespostaCommand command, CancellationToken ct = default)
    {
        var resp = await ObterRespostaPorIdAsync(tenantId, command.RespostaId, ct);
        if (resp is null)
            throw new KeyNotFoundException("Registro de resposta na fila não encontrado.");

        if (resp.StatusTransmissao == "ACEITA_PELO_PORTAL")
            throw new InvalidOperationException($"A transmissão desta proposta já foi concluída e aceita pelo portal com protocolo '{resp.ProtocoloExterno}'. Não é permitida retransmissão.");

        var cotacao = await ObterCotacaoPorIdAsync(tenantId, resp.CotacaoId, ct);
        if (cotacao is null)
            throw new KeyNotFoundException("Cotação vinculada não encontrada.");

        var contas = await ListarContasPortalAsync(tenantId, ct);
        var conta = contas.FirstOrDefault(c => c.Id == cotacao.PortalContaId)
            ?? new PortalContaDto(Guid.Empty, cotacao.EstabelecimentoId, cotacao.EstabelecimentoNome, cotacao.Provedor, "Canal Direto", null, null, "HOMOLOGACAO", "NAO_CONFIGURADA", "Conta não encontrada", null, true);

        var conector = ObterConector(cotacao.Provedor);

        // Marca tentativa na fila
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_cotacao_respostas
            SET status_transmissao = 'ENVIANDO', tentativas = tentativas + 1, updated_at = now()
            WHERE id = @id AND tenant_id = @tenantId",
            new { id = command.RespostaId, tenantId }, cancellationToken: ct));

        // Transmissão via conector
        var resultado = await conector.TransmitirPropostaAsync(conta, resp, cotacao, ct);

        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_cotacao_respostas
            SET status_transmissao = @status,
                protocolo_externo = @protocolo,
                mensagem_retorno = @mensagem,
                enviado_em = (CASE WHEN @sucesso THEN now() ELSE enviado_em END),
                updated_at = now()
            WHERE id = @id AND tenant_id = @tenantId",
            new
            {
                id = command.RespostaId, tenantId,
                status = resultado.StatusTransmissao,
                protocolo = resultado.Protocolo,
                mensagem = resultado.Mensagem,
                sucesso = resultado.Sucesso
            }, cancellationToken: ct));

        if (resultado.Sucesso && resultado.StatusTransmissao == "ACEITA_PELO_PORTAL")
        {
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_cotacoes
                SET status_interno = 'RESPONDIDA', updated_at = now()
                WHERE id = @cotacaoId AND tenant_id = @tenantId",
                new { cotacaoId = resp.CotacaoId, tenantId }, cancellationToken: ct));
        }
    }

    public async Task<(byte[]? Bytes, string Nome, string ContentType)?> ObterAnexoAsync(Guid tenantId, Guid anexoId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        var a = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT nome_arquivo, content_type, conteudo
            FROM plantaopro.adm360_cotacao_anexos
            WHERE id = @anexoId AND tenant_id = @tenantId",
            new { anexoId, tenantId }, cancellationToken: ct));

        if (a is null) return null;

        return ((byte[]?)a.conteudo, (string)a.nome_arquivo, (string)a.content_type);
    }
}
