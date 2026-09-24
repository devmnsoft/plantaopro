using Dapper;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

public sealed class CirurgiaRepository : Adm360Repository, ICirurgiaRepository
{
    public CirurgiaRepository(string connectionString) : base(connectionString) { }

    private sealed class CirurgiaRow
    {
        public Guid Id { get; set; }
        public string Numero { get; set; } = string.Empty;
        public Guid HospitalId { get; set; }
        public string Hospital { get; set; } = string.Empty;
        public Guid? MedicoId { get; set; }
        public string? Medico { get; set; }
        public string Procedimento { get; set; } = string.Empty;
        public DateOnly DataPrevista { get; set; }
        public TimeOnly? HoraPrevista { get; set; }
        public Guid? OrcamentoId { get; set; }
        public string? OrcamentoNumero { get; set; }
        public int? OrcamentoRevisao { get; set; }
        public Guid? ResponsavelId { get; set; }
        public string? Responsavel { get; set; }
        public Guid LocalDestinoId { get; set; }
        public string LocalDestino { get; set; } = string.Empty;
        public string Situacao { get; set; } = string.Empty;
        public string? Observacoes { get; set; }
        public DateTimeOffset CriadoEm { get; set; }
    }

    public async Task<IReadOnlyList<CirurgiaResumo>> ListarAsync(
        Guid tenantId, string? busca, string? situacao, DateOnly? inicio, DateOnly? fim, CancellationToken ct)
    {
        await using var cn = Connection();
        var rows = await cn.QueryAsync<CirurgiaRow>(new CommandDefinition(@"
            SELECT c.id AS Id, c.numero AS Numero,
                   COALESCE(h.nome, hosp.nome, loc.nome, 'Hospital') AS Hospital,
                   COALESCE(m.nome, med.nome, '') AS Medico,
                   c.procedimento AS Procedimento,
                   c.data_prevista AS DataPrevista,
                   c.hora_prevista AS HoraPrevista,
                   o.numero AS OrcamentoNumero,
                   loc.nome AS LocalDestino,
                   c.situacao AS Situacao,
                   c.created_at AS CriadoEm
            FROM plantaopro.adm360_cirurgias c
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = c.hospital_id AND h.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = c.hospital_id AND hosp.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros m ON m.id = c.medico_id AND m.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.medicos med ON med.id = c.medico_id AND med.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.adm360_locais loc ON loc.id = c.local_destino_id AND loc.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.adm360_orcamentos o ON o.id = c.orcamento_id AND o.tenant_id = c.tenant_id
            WHERE c.tenant_id = @tenantId
              AND (@busca IS NULL OR c.numero ILIKE '%' || @busca || '%' OR c.procedimento ILIKE '%' || @busca || '%' OR h.nome ILIKE '%' || @busca || '%' OR hosp.nome ILIKE '%' || @busca || '%')
              AND (@situacao IS NULL OR c.situacao = @situacao)
              AND (@inicio IS NULL OR c.data_prevista >= @inicio)
              AND (@fim IS NULL OR c.data_prevista <= @fim)
            ORDER BY c.data_prevista DESC, c.created_at DESC",
            new { tenantId, busca, situacao, inicio, fim }, cancellationToken: ct));

        return rows.Select(r => new CirurgiaResumo(
            r.Id, r.Numero, r.Hospital, r.Medico, r.Procedimento, r.DataPrevista, r.HoraPrevista,
            r.OrcamentoNumero, r.LocalDestino, r.Situacao, r.CriadoEm)).ToList();
    }

    public async Task<CirurgiaDetalhes?> ObterPorIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    {
        await using var cn = Connection();
        var r = await cn.QuerySingleOrDefaultAsync<CirurgiaRow>(new CommandDefinition(@"
            SELECT c.id AS Id, c.numero AS Numero, c.hospital_id AS HospitalId,
                   COALESCE(h.nome, hosp.nome, loc.nome, 'Hospital') AS Hospital,
                   c.medico_id AS MedicoId, COALESCE(m.nome, med.nome, '') AS Medico,
                   c.procedimento AS Procedimento,
                   c.data_prevista AS DataPrevista,
                   c.hora_prevista AS HoraPrevista,
                   c.orcamento_id AS OrcamentoId,
                   o.numero AS OrcamentoNumero,
                   c.orcamento_revisao AS OrcamentoRevisao,
                   c.responsavel_id AS ResponsavelId,
                   COALESCE(u.nome, '') AS Responsavel,
                   c.local_destino_id AS LocalDestinoId,
                   loc.nome AS LocalDestino,
                   c.situacao AS Situacao,
                   c.observacoes AS Observacoes,
                   c.created_at AS CriadoEm
            FROM plantaopro.adm360_cirurgias c
            LEFT JOIN plantaopro.adm360_parceiros h ON h.id = c.hospital_id AND h.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.hospitais hosp ON hosp.id = c.hospital_id AND hosp.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.adm360_parceiros m ON m.id = c.medico_id AND m.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.medicos med ON med.id = c.medico_id AND med.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.adm360_locais loc ON loc.id = c.local_destino_id AND loc.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.adm360_orcamentos o ON o.id = c.orcamento_id AND o.tenant_id = c.tenant_id
            LEFT JOIN plantaopro.usuarios u ON u.id = c.responsavel_id AND u.tenant_id = c.tenant_id
            WHERE c.id = @id AND c.tenant_id = @tenantId",
            new { id, tenantId }, cancellationToken: ct));

        if (r is null) return null;

        return new CirurgiaDetalhes(
            r.Id, r.Numero, r.HospitalId, r.Hospital, r.MedicoId, r.Medico, r.Procedimento,
            r.DataPrevista, r.HoraPrevista, r.OrcamentoId, r.OrcamentoNumero, r.OrcamentoRevisao,
            r.ResponsavelId, r.Responsavel, r.LocalDestinoId, r.LocalDestino, r.Situacao,
            r.Observacoes, r.CriadoEm);
    }

    public async Task<Guid> CriarAsync(Guid tenantId, Guid usuarioId, CriarCirurgiaCommand command, CancellationToken ct)
    {
        CirurgiaRegras.ValidarCriacao(command.Procedimento, command.DataPrevista);

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        // Valida hospital
        var hospValido = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (
                SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = @id AND tenant_id = @tenantId AND ativo
                UNION ALL
                SELECT 1 FROM plantaopro.hospitais WHERE id = @id AND tenant_id = @tenantId AND (status IS NULL OR status IN ('A', 'ATIVO'))
                UNION ALL
                SELECT 1 FROM plantaopro.adm360_locais WHERE id = @id AND tenant_id = @tenantId AND tipo = 'EXTERNO' AND ativo
            )", new { id = command.HospitalId, tenantId }, tx, cancellationToken: ct));
        if (!hospValido) throw new ArgumentException("Hospital informado é inválido ou inativo.");

        // Valida médico se fornecido
        if (command.MedicoId.HasValue)
        {
            var medValido = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
                SELECT EXISTS (
                    SELECT 1 FROM plantaopro.medicos WHERE id = @id AND tenant_id = @tenantId
                    UNION ALL
                    SELECT 1 FROM plantaopro.adm360_parceiros WHERE id = @id AND tenant_id = @tenantId AND ativo
                )", new { id = command.MedicoId.Value, tenantId }, tx, cancellationToken: ct));
            if (!medValido) throw new ArgumentException("Médico informado é inválido.");
        }

        // Valida local destino
        var localValido = await cn.ExecuteScalarAsync<bool>(new CommandDefinition(@"
            SELECT EXISTS (
                SELECT 1 FROM plantaopro.adm360_locais WHERE id = @id AND tenant_id = @tenantId AND ativo
            )", new { id = command.LocalDestinoId, tenantId }, tx, cancellationToken: ct));
        if (!localValido) throw new ArgumentException("Local de destino é inválido ou inativo.");

        // Valida orçamento se fornecido
        if (command.OrcamentoId.HasValue)
        {
            var orcRow = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
                SELECT situacao, revisao FROM plantaopro.adm360_orcamentos
                WHERE id = @id AND tenant_id = @tenantId",
                new { id = command.OrcamentoId.Value, tenantId }, tx, cancellationToken: ct));
            if (orcRow is null) throw new ArgumentException("Orçamento informado não existe para esta organização.");
            if (orcRow.situacao != "APROVADO")
                throw new InvalidOperationException($"Orçamento vinculado deve estar APROVADO. Situação atual: {orcRow.situacao}.");
        }

        var id = Guid.NewGuid();
        var seq = await cn.ExecuteScalarAsync<long>("SELECT nextval('plantaopro.adm360_cirurgia_numero')", transaction: tx);
        var numero = $"CIR-{seq:D6}";

        await cn.ExecuteAsync(new CommandDefinition(@"
            INSERT INTO plantaopro.adm360_cirurgias(
                id, tenant_id, numero, hospital_id, medico_id, procedimento,
                data_prevista, hora_prevista, orcamento_id, orcamento_revisao,
                responsavel_id, local_destino_id, situacao, observacoes, created_by
            ) VALUES(
                @id, @tenantId, @numero, @HospitalId, @MedicoId, @Procedimento,
                @DataPrevista, @HoraPrevista, @OrcamentoId, @OrcamentoRevisao,
                @ResponsavelId, @LocalDestinoId, 'AGENDADA', @Observacoes, @usuarioId
            )",
            new
            {
                id, tenantId, numero, command.HospitalId, command.MedicoId, command.Procedimento,
                command.DataPrevista,
                HoraPrevista = command.HoraPrevista.HasValue ? command.HoraPrevista.Value.ToTimeSpan() : (TimeSpan?)null,
                command.OrcamentoId, command.OrcamentoRevisao,
                command.ResponsavelId, command.LocalDestinoId, command.Observacoes, usuarioId
            }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task AtualizarAsync(Guid tenantId, Guid usuarioId, AtualizarCirurgiaCommand command, CancellationToken ct)
    {
        CirurgiaRegras.ValidarCriacao(command.Procedimento, command.DataPrevista);

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var atual = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT situacao, data_prevista FROM plantaopro.adm360_cirurgias
            WHERE id = @CirurgiaId AND tenant_id = @tenantId FOR UPDATE",
            new { command.CirurgiaId, tenantId }, tx, cancellationToken: ct));

        if (atual is null) throw new InvalidOperationException("Cirurgia não encontrada.");
        if (atual.situacao is "CANCELADA" or "REALIZADA")
            throw new InvalidOperationException($"Não é permitido alterar cirurgia na situação '{atual.situacao}'.");

        // Se data prevista foi alterada, valida se algum lote reservado para o orçamento vence antes
        if ((DateOnly)atual.data_prevista != command.DataPrevista)
        {
            var lotesVencidos = await cn.ExecuteScalarAsync<int>(new CommandDefinition(@"
                SELECT COUNT(*)
                FROM plantaopro.adm360_cirurgias c
                JOIN plantaopro.adm360_reservas r ON r.origem_id = c.orcamento_id AND r.origem_tipo = 'ORCAMENTO_CIRURGICO' AND r.situacao = 'ATIVA'
                JOIN plantaopro.adm360_lotes l ON l.id = r.lote_id
                WHERE c.id = @CirurgiaId AND c.tenant_id = @tenantId
                  AND l.validade IS NOT NULL AND l.validade < @NovaData",
                new { command.CirurgiaId, tenantId, NovaData = command.DataPrevista }, tx, cancellationToken: ct));

            if (lotesVencidos > 0)
                throw new InvalidOperationException("A nova data da cirurgia ultrapassa a validade de lotes já reservados para este procedimento.");
        }

        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_cirurgias
            SET hospital_id = @HospitalId, medico_id = @MedicoId, procedimento = @Procedimento,
                data_prevista = @DataPrevista, hora_prevista = @HoraPrevista, responsavel_id = @ResponsavelId,
                local_destino_id = @LocalDestinoId, observacoes = @Observacoes,
                versao = versao + 1, updated_at = now()
            WHERE id = @CirurgiaId AND tenant_id = @tenantId",
            new
            {
                command.HospitalId, command.MedicoId, command.Procedimento,
                command.DataPrevista,
                HoraPrevista = command.HoraPrevista.HasValue ? command.HoraPrevista.Value.ToTimeSpan() : (TimeSpan?)null,
                command.ResponsavelId,
                command.LocalDestinoId, command.Observacoes, command.CirurgiaId, tenantId
            }, tx, cancellationToken: ct));

        await tx.CommitAsync(ct);
    }

    public async Task CancelarAsync(Guid tenantId, Guid usuarioId, Guid cirurgiaId, string motivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Motivo do cancelamento é obrigatório.");

        await using var cn = Connection();
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var atual = await cn.QuerySingleOrDefaultAsync<dynamic>(new CommandDefinition(@"
            SELECT situacao, orcamento_id FROM plantaopro.adm360_cirurgias
            WHERE id = @cirurgiaId AND tenant_id = @tenantId FOR UPDATE",
            new { cirurgiaId, tenantId }, tx, cancellationToken: ct));

        if (atual is null) throw new InvalidOperationException("Cirurgia não encontrada.");
        if (atual.situacao == "CANCELADA") return; // Idempotente
        if (atual.situacao == "REALIZADA") throw new InvalidOperationException("Cirurgia já realizada não pode ser cancelada.");

        // Atualiza cirurgia
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_cirurgias
            SET situacao = 'CANCELADA',
                observacoes = COALESCE(observacoes || E'\n', '') || 'Cancelamento: ' || @motivo,
                versao = versao + 1, updated_at = now()
            WHERE id = @cirurgiaId AND tenant_id = @tenantId",
            new { cirurgiaId, tenantId, motivo }, tx, cancellationToken: ct));

        // Cancela vales ainda em preparação (RASCUNHO, EM_SEPARACAO, PRONTO_PARA_EXPEDICAO)
        await cn.ExecuteAsync(new CommandDefinition(@"
            UPDATE plantaopro.adm360_vales
            SET situacao = 'CANCELADO',
                observacoes = COALESCE(observacoes || E'\n', '') || 'Cirurgia cancelada: ' || @motivo,
                versao = versao + 1, updated_at = now()
            WHERE cirurgia_id = @cirurgiaId AND tenant_id = @tenantId
              AND situacao IN ('RASCUNHO', 'EM_SEPARACAO', 'PRONTO_PARA_EXPEDICAO')",
            new { cirurgiaId, tenantId, motivo }, tx, cancellationToken: ct));

        // Libera reservas ainda não expedidas
        if (atual.orcamento_id is not null)
        {
            Guid orcId = atual.orcamento_id;
            await cn.ExecuteAsync(new CommandDefinition(@"
                UPDATE plantaopro.adm360_reservas
                SET situacao = 'CANCELADA'
                WHERE tenant_id = @tenantId AND origem_tipo = 'ORCAMENTO_CIRURGICO' AND origem_id = @orcId
                  AND situacao = 'ATIVA'
                  AND id NOT IN (
                      SELECT vi.reserva_id
                      FROM plantaopro.adm360_vales v
                      JOIN plantaopro.adm360_vale_itens vi ON vi.vale_id = v.id
                      WHERE v.tenant_id = @tenantId AND v.cirurgia_id = @cirurgiaId
                        AND v.situacao IN ('EXPEDIDO', 'RETORNO_PARCIAL')
                        AND vi.reserva_id IS NOT NULL
                  )",
                new { tenantId, orcId, cirurgiaId }, tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
    }
}
