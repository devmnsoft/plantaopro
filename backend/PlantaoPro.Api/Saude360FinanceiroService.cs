using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using PlantaoPro.Api.Data;
using PlantaoPro.Api.Models;
using System.Data;
using System.Text.Json;

namespace PlantaoPro.Api;

/// <summary>
/// B8 Saúde 360 — Fluxo financeiro canônico (clinica_* é a fonte de origem).
///
/// Responsabilidades por operação (decisão R4-B8):
///   - conta a receber: escrita por ConsultaApplicationService (origem CONSULTA)
///     ou CRUD manual Saude360ClinicalService (origem MANUAL/DEMO);
///   - recebimento + baixa: BaixarRecebimentoAsync (este serviço) — única
///     jornada que atualiza conta, caixa, lançamentos e histórico;
///   - estorno: EstornarRecebimentoAsync (este serviço) — reverte conta, caixa
///     (se aberta) e grava clinica_estornos + campos estorno_* no recebimento;
///   - caixa: FecharCaixaAsync (este serviço) — saldo computado e
///     clinica_fechamentos_caixa; sem contagem física nesta versão.
/// As camadas v115/v116 continuam sendo módulos de consolidação com origem
/// própria e não recebem escritas desta jornada (sem módulos paralelos).
///
/// Valores operacionais canônicos da conta: valor_total/valor_pendente/
/// valor_pago. Em finalizações de consulta estes são gravados iguais ao
/// valor líquido bruto-desconto+coparticipacao (ver ConsultaApplicationService).
///
/// Concorrência/idempotência: todas as ações rodam em transação única com
/// lock de linha (FOR UPDATE) no registro alvo, predicados de status esperado
/// e, na baixa, predicado do valor pendente esperado — corrida perde com 409
/// e rollback completo (nada é gravado em dobro).
/// </summary>
public sealed class Saude360FinanceiroService
{
    private readonly string connectionString;
    private readonly ICurrentUserService user;
    private readonly IAuditService audit;
    private readonly ILogger<Saude360FinanceiroService> logger;

    public Saude360FinanceiroService(IConfiguration cfg, ICurrentUserService user, IAuditService audit, ILogger<Saude360FinanceiroService> logger)
    {
        connectionString = cfg.GetConnectionString("Default") ?? throw new InvalidOperationException("ConnectionStrings:Default não configurada.");
        this.user = user;
        this.audit = audit;
        this.logger = logger;
    }

    private Guid? Tenant => user.ClienteId ?? user.TenantId;
    private Guid? Uid => user.UserId;
    private static CommandDefinition Cmd(string sql, object? args, IDbTransaction tx, CancellationToken ct) => new(sql, args, tx, cancellationToken: ct);

    public sealed record BaixaResultado(Guid ContaReceberId, Guid ReceivingId, Guid? CaixaId, bool CaixaVinculado, bool ContaQuitada, string Detalhes);
    public sealed record EstornoResultado(Guid ReceivingId, bool ContaRevertida, bool CaixaRevertida, string Detalhes);
    public sealed record FechamentoCaixaResultado(Guid CaixaId, decimal SaldoFinal, bool JaviaFechado);

    private sealed record ContaLockRow(Guid Id, string Status, decimal Pendente, decimal Pago, Guid? PacienteId);
    private sealed record ReceivingLockRow(Guid Id, string Status, decimal Valor, Guid? ContaId, Guid? CaixaId);
    private sealed record CaixaLockRow(Guid Id, string Status, decimal SaldoInicial, decimal TotalEntradas, decimal TotalSaidas, decimal SaldoFinal);

    // ------------------------------------------------------------------
    // Baixa de recebimento: recebe, quita (total ou parcial), vincula caixa.
    // ------------------------------------------------------------------
    public async Task<ApiResponse<BaixaResultado>> BaixarRecebimentoAsync(Saude360CreateRequest request, CancellationToken ct)
    {
        if (Tenant is not Guid tenant) return ApiResponse<BaixaResultado>.Fail("Selecione uma organização para operar o financeiro.", 403);
        if (request.ContaReceberId is null || request.Valor.GetValueOrDefault() <= 0 || string.IsNullOrWhiteSpace(request.FormaPagamento) || request.DataPagamento is null)
            return ApiResponse<BaixaResultado>.Fail("Recebimento exige conta, valor positivo, forma de pagamento e data de pagamento.", 400);

        var contaId = request.ContaReceberId.Value;
        var valor = request.Valor.Value;
        var forma = request.FormaPagamento.Trim();
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var conta = await cn.QuerySingleOrDefaultAsync<ContaLockRow>(Cmd(@"
            select id, status, coalesce(valor_pendente,0) Pendente, coalesce(valor_pago,0) Pago, paciente_id PacienteId
            from plantaopro.clinica_contas_receber
            where id=@contaId and reg_status='A' and cliente_id=@tenant
            for update", new { contaId, tenant }, tx, ct));
        if (conta is null) { await tx.RollbackAsync(ct); return ApiResponse<BaixaResultado>.Fail("Conta a receber não encontrada.", 404); }
        if (conta.Status == "RECEBIDO" || conta.Pendente <= 0) { await tx.RollbackAsync(ct); return ApiResponse<BaixaResultado>.Fail("A conta já está quitada. Se houve erro, use o estorno.", 409); }
        if (conta.Status == "CANCELADA") { await tx.RollbackAsync(ct); return ApiResponse<BaixaResultado>.Fail("Conta cancelada não aceita recebimento.", 409); }
        if (valor > conta.Pendente) { await tx.RollbackAsync(ct); return ApiResponse<BaixaResultado>.Fail($"O valor do recebimento excede o saldo pendente da conta ({conta.Pendente.ToString("0.00")}).", 400); }

        Guid? caixaId;
        if (request.CaixaId is not null)
        {
            var caixaEscolhida = await cn.QuerySingleOrDefaultAsync<Guid?>(Cmd(
                "select id from plantaopro.clinica_caixa where id=@caixaId and reg_status='A' and cliente_id=@tenant and status='ABERTO'",
                new { caixaId = request.CaixaId.Value, tenant }, tx, ct));
            if (caixaEscolhida is null) { await tx.RollbackAsync(ct); return ApiResponse<BaixaResultado>.Fail("O caixa informado não está aberto para receber lançamentos.", 409); }
            caixaId = caixaEscolhida.Value;
        }
        else
        {
            // Sem caixa explícito: tenta o caixa aberto mais recente do tenant.
            caixaId = await cn.ExecuteScalarAsync<Guid?>(Cmd(@"
                select id from plantaopro.clinica_caixa
                where reg_status='A' and cliente_id=@tenant and status='ABERTO'
                order by coalesce(aberto_em, data_abertura) desc, id
                limit 1", new { tenant }, tx, ct));
        }

        var recId = Guid.NewGuid();
        await cn.ExecuteAsync(Cmd(@"
            insert into plantaopro.clinica_recebimentos(id,cliente_id,conta_receber_id,paciente_id,caixa_id,valor,forma_pagamento,data_recebimento,comprovante,observacoes,status,created_by,created_at,reg_date,reg_status)
            values(@recId,@tenant,@contaId,@pacienteId,@caixaId,@valor,@forma,@data,@comprovante,@obs,'CONFIRMADO',@uid,now(),now(),'A')",
            new { recId, tenant, contaId, pacienteId = conta.PacienteId, caixaId, valor, forma, data = request.DataPagamento.Value, comprovante = request.DocumentoAlternativo ?? "", obs = request.Observacoes ?? "", uid = Uid }, tx, ct));

        var atualizado = await cn.ExecuteAsync(Cmd(@"
            update plantaopro.clinica_contas_receber
            set    valor_pago = valor_pago + @valor,
                   valor_pendente = valor_pendente - @valor,
                   status = case when valor_pendente - @valor <= 0 then 'RECEBIDO' else status end,
                   updated_by = @uid, updated_at = now(), reg_update = now()
            where  id = @contaId and reg_status = 'A' and cliente_id = @tenant and valor_pendente = @pendenteEsperado",
            new { valor, uid = Uid, contaId, tenant, pendenteEsperado = conta.Pendente }, tx, ct));
        if (atualizado != 1) { await tx.RollbackAsync(ct); return ApiResponse<BaixaResultado>.Fail("Conflito: a conta foi alterada por outra ação simultânea. Recarregue e tente novamente.", 409); }

        var quitada = conta.Pendente - valor <= 0;
        if (caixaId is not null)
        {
            await cn.ExecuteAsync(Cmd(@"
                update plantaopro.clinica_caixa
                set total_entradas = total_entradas + @valor,
                    saldo_final = saldo_inicial + total_entradas + @valor - total_saidas,
                    updated_by = @uid, reg_update = now()
                where id = @caixaId and status = 'ABERTO'", new { valor, uid = Uid, caixaId }, tx, ct));
            await cn.ExecuteAsync(Cmd(@"
                insert into plantaopro.clinica_lancamentos(id,cliente_id,caixa_id,tipo,descricao,valor,status,created_by,created_at,reg_date,reg_status)
                values(gen_random_uuid(),@tenant,@caixaId,'ENTRADA',@descricao,@valor,'CONFIRMADO',@uid,now(),now(),'A')",
                new { tenant, caixaId, descricao = $"Baixa de conta {contaId:N} — R$ {valor:0.00} ({forma})", valor, uid = Uid }, tx, ct));
        }
        await HistoricoAsync(cn, tx, ct, "clinica_recebimentos", recId, "BAIXA_RECEBIMENTO", new { contaReceberId = contaId, valor, formaPagamento = forma, caixaId, quitada });
        await AuditarAsync("clinica_recebimentos", recId, "SAUDE360_BAIXA_RECEBIMENTO", new { contaReceberId = contaId, valor, formaPagamento = forma, caixaId });
        await tx.CommitAsync(ct);

        var detalhes = caixaId is not null
            ? $"Baixa registrada no caixa {caixaId:N}."
            : "Nenhum caixa aberto disponível para vinculação: o recebimento foi baixado na conta, mas o saldo do caixa não foi movimentado.";
        return ApiResponse<BaixaResultado>.Ok(new BaixaResultado(contaId, recId, caixaId, caixaId is not null, quitada, detalhes), quitada ? "Recebimento baixado e conta quitada." : "Recebimento baixado (pagamento parcial).");
    }

    // ------------------------------------------------------------------
    // Estorno: reverte recebimento CONFIRMADO + conta + caixa (se aberta).
    // ------------------------------------------------------------------
    public async Task<ApiResponse<EstornoResultado>> EstornarRecebimentoAsync(Saude360ActionRequest request, CancellationToken ct)
    {
        if (Tenant is not Guid tenant) return ApiResponse<EstornoResultado>.Fail("Selecione uma organização para operar o financeiro.", 403);
        if (request.Id is null) return ApiResponse<EstornoResultado>.Fail("Informe o recebimento a estornar.", 400);
        var motivo = !string.IsNullOrWhiteSpace(request.Motivo) ? request.Motivo : request.Justificativa ?? "";
        if (string.IsNullOrWhiteSpace(motivo)) return ApiResponse<EstornoResultado>.Fail("A ação exige motivo ou justificativa.", 400);

        var recId = request.Id.Value;
        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var rec = await cn.QuerySingleOrDefaultAsync<ReceivingLockRow>(Cmd(@"
            select id, status, coalesce(valor,0) Valor, conta_receber_id ContaId, caixa_id CaixaId
            from plantaopro.clinica_recebimentos
            where id=@recId and reg_status='A' and cliente_id=@tenant
            for update", new { recId, tenant }, tx, ct));
        if (rec is null) { await tx.RollbackAsync(ct); return ApiResponse<EstornoResultado>.Fail("Recebimento não encontrado.", 404); }
        if (rec.Status != "CONFIRMADO") { await tx.RollbackAsync(ct); return ApiResponse<EstornoResultado>.Fail("O recebimento já foi estornado ou está em outro estado.", 409); }

        var atualizado = await cn.ExecuteAsync(Cmd(@"
            update plantaopro.clinica_recebimentos
            set status='ESTORNADO', estornado_em=now(), estornado_por=@uid, justificativa_estorno=left(@motivo,2000),
                updated_by=@uid, updated_at=now(), reg_update=now()
            where id=@recId and reg_status='A' and cliente_id=@tenant and status='CONFIRMADO'",
            new { uid = Uid, motivo = motivo.Trim(), recId, tenant }, tx, ct));
        if (atualizado != 1) { await tx.RollbackAsync(ct); return ApiResponse<EstornoResultado>.Fail("Conflito: o recebimento foi alterado por outra ação simultânea.", 409); }

        await cn.ExecuteAsync(Cmd(@"
            insert into plantaopro.clinica_estornos(id,cliente_id,recebimento_id,motivo,valor,status,reg_date,reg_status)
            values(gen_random_uuid(),@tenant,@recId,@motivo,@valor,'ESTORNADO',now(),'A')",
            new { tenant, recId, motivo = motivo.Trim(), valor = rec.Valor }, tx, ct));

        var contaRevertida = false;
        if (rec.ContaId is not null)
        {
            var contaExiste = await cn.QuerySingleOrDefaultAsync<bool>(Cmd(
                "select exists(select 1 from plantaopro.clinica_contas_receber where id=@contaId and reg_status='A' and cliente_id=@tenant for update)",
                new { contaId = rec.ContaId.Value, tenant }, tx, ct));
            if (contaExiste)
            {
                // RECEBIDO volta a ABERTA (ou VENCIDA se o vencimento já passou);
                // contas ainda abertas mantêm o status — a baixa parcial é exposta pelos valores.
                await cn.ExecuteAsync(Cmd(@"
                    update plantaopro.clinica_contas_receber
                    set valor_pago = greatest(valor_pago - @valor, 0),
                        valor_pendente = valor_pendente + @valor,
                        status = case when status = 'RECEBIDO' then case when vencimento < current_date then 'VENCIDA' else 'ABERTA' end else status end,
                        updated_by=@uid, updated_at=now(), reg_update=now()
                    where id=@contaId and reg_status='A' and cliente_id=@tenant",
                    new { valor = rec.Valor, uid = Uid, contaId = rec.ContaId.Value, tenant }, tx, ct));
                contaRevertida = true;
            }
        }

        var caixaRevertida = false;
        if (rec.CaixaId is not null)
        {
            var caixaAberta = await cn.ExecuteScalarAsync<int>(Cmd(
                "select count(*) from plantaopro.clinica_caixa where id=@caixaId and reg_status='A' and cliente_id=@tenant and status='ABERTO'",
                new { caixaId = rec.CaixaId.Value, tenant }, tx, ct));
            if (caixaAberta > 0)
            {
                await cn.ExecuteAsync(Cmd(@"
                    update plantaopro.clinica_caixa
                    set total_entradas = greatest(total_entradas - @valor, 0),
                        saldo_final = saldo_inicial + greatest(total_entradas - @valor, 0) - total_saidas,
                        updated_by=@uid, reg_update=now()
                    where id=@caixaId and status='ABERTO'",
                    new { valor = rec.Valor, uid = Uid, caixaId = rec.CaixaId.Value }, tx, ct));
                await cn.ExecuteAsync(Cmd(@"
                    insert into plantaopro.clinica_lancamentos(id,cliente_id,caixa_id,tipo,descricao,valor,status,created_by,created_at,reg_date,reg_status)
                    values(gen_random_uuid(),@tenant,@caixaId,'SAIDA',@descricao,@valor,'CONFIRMADO',@uid,now(),now(),'A')",
                    new { tenant, caixaId = rec.CaixaId.Value, descricao = $"Estorno de recebimento {recId:N} — R$ {rec.Valor:0.00}", valor = rec.Valor, uid = Uid }, tx, ct));
                caixaRevertida = true;
            }
        }
        await HistoricoAsync(cn, tx, ct, "clinica_recebimentos", recId, "ESTORNO", new { contaReceberId = rec.ContaId, valor = rec.Valor, caixaId = rec.CaixaId, motivo = motivo.Trim(), contaRevertida, caixaRevertida });
        await AuditarAsync("clinica_recebimentos", recId, "SAUDE360_ESTORNO_RECEBIMENTO", new { contaReceberId = rec.ContaId, valor = rec.Valor, motivo = motivo.Trim() });
        await tx.CommitAsync(ct);

        var extras = string.Concat(
            rec.CaixaId is not null && !caixaRevertida ? " Caixa já fechado: o total não foi reaberto; concilie manualmente." : null,
            rec.ContaId is not null && !contaRevertida ? " Conta inexistente/inativa: apenas o recebimento foi estornado." : null);
        return ApiResponse<EstornoResultado>.Ok(new EstornoResultado(recId, contaRevertida, caixaRevertida, extras.Trim()), "Recebimento estornado e baixa revertida.");
    }

    // ------------------------------------------------------------------
    // Fechamento de caixa: congela o saldo computado (sem contagem física).
    // ------------------------------------------------------------------
    public async Task<ApiResponse<FechamentoCaixaResultado>> FecharCaixaAsync(Saude360ActionRequest request, CancellationToken ct)
    {
        if (Tenant is not Guid tenant) return ApiResponse<FechamentoCaixaResultado>.Fail("Selecione uma organização para operar o financeiro.", 403);
        var caixaId = request.CaixaId ?? request.Id;
        if (caixaId is null) return ApiResponse<FechamentoCaixaResultado>.Fail("Informe o caixa a fechar.", 400);

        await using var cn = new NpgsqlConnection(connectionString);
        await cn.OpenAsync(ct);
        await using var tx = await cn.BeginTransactionAsync(ct);

        var caixa = await cn.QuerySingleOrDefaultAsync<CaixaLockRow>(Cmd(@"
            select id, status,
                   coalesce(saldo_inicial,0) SaldoInicial, coalesce(total_entradas,0) TotalEntradas,
                   coalesce(total_saidas,0) TotalSaidas, coalesce(saldo_final,0) SaldoFinal
            from plantaopro.clinica_caixa
            where id=@caixaId and reg_status='A' and cliente_id=@tenant
            for update", new { caixaId = caixaId.Value, tenant }, tx, ct));
        if (caixa is null) { await tx.RollbackAsync(ct); return ApiResponse<FechamentoCaixaResultado>.Fail("Caixa não encontrado.", 404); }
        if (caixa.Status == "FECHADO")
        {
            await tx.RollbackAsync(ct);
            return ApiResponse<FechamentoCaixaResultado>.Ok(new FechamentoCaixaResultado(caixaId.Value, caixa.SaldoFinal, true), "O caixa já estava fechado.");
        }

        var saldo = Math.Round(caixa.SaldoInicial + caixa.TotalEntradas - caixa.TotalSaidas, 2);
        var atualizado = await cn.ExecuteAsync(Cmd(@"
            update plantaopro.clinica_caixa
            set status='FECHADO', fechado_em=now(), data_fechamento=now(), usuario_fechamento_id=@uid,
                saldo_final=@saldo, saldo_informado=@saldo, diferenca=0,
                updated_by=@uid, reg_update=now()
            where id=@caixaId and reg_status='A' and cliente_id=@tenant and status='ABERTO'",
            new { uid = Uid, saldo, caixaId = caixaId.Value, tenant }, tx, ct));
        if (atualizado != 1) { await tx.RollbackAsync(ct); return ApiResponse<FechamentoCaixaResultado>.Fail("Conflito: o caixa foi alterado por outra ação simultânea.", 409); }

        await cn.ExecuteAsync(Cmd(@"
            insert into plantaopro.clinica_fechamentos_caixa(id,cliente_id,caixa_id,valor_informado,diferenca,status,observacoes,created_by,reg_date,reg_status)
            values(gen_random_uuid(),@tenant,@caixaId,@saldo,0,'FECHADO','Fechamento registrado sem contagem física: valor_informado é o saldo computado pelos lançamentos.',@uid,now(),'A')",
            new { tenant, caixaId = caixaId.Value, saldo, uid = Uid }, tx, ct));
        await HistoricoAsync(cn, tx, ct, "clinica_caixa", caixaId.Value, "FECHAMENTO_CAIXA", new { saldoInicial = caixa.SaldoInicial, totalEntradas = caixa.TotalEntradas, totalSaidas = caixa.TotalSaidas, saldoFinal = saldo });
        await AuditarAsync("clinica_caixa", caixaId.Value, "SAUDE360_FECHAR_CAIXA", new { saldoFinal = saldo });
        await tx.CommitAsync(ct);
        return ApiResponse<FechamentoCaixaResultado>.Ok(new FechamentoCaixaResultado(caixaId.Value, saldo, false), $"Caixa fechado com saldo {saldo.ToString("0.00")} (sem contagem física).");
    }

    // ------------------------------------------------------------------
    private async Task HistoricoAsync(NpgsqlConnection cn, IDbTransaction tx, CancellationToken ct, string entidade, Guid entidadeId, string acao, object detalhes)
    {
        if (Tenant is not Guid tenant) return;
        await cn.ExecuteAsync(Cmd(@"
            insert into plantaopro.clinica_financeiro_historico(id,cliente_id,entidade,entidade_id,acao,detalhes,usuario_id,reg_date,reg_status)
            values(gen_random_uuid(),@tenant,@entidade,@entidadeId,@acao,@detalhes::jsonb,@uid,now(),'A')",
            new { tenant, entidade, entidadeId, acao, detalhes = JsonSerializer.Serialize(detalhes), uid = Uid }, tx, ct));
    }

    private async Task AuditarAsync(string table, Guid id, string action, object detalhes)
    {
        try { await audit.RegistrarAsync(Uid, Tenant, table, id, action, detalhes, true, null, string.Join(',', user.Roles)); }
        catch (Exception ex) { logger.LogWarning(ex, "Falha de auditoria financeira Saúde 360 para {Table}/{Id}", table, id); }
    }
}
