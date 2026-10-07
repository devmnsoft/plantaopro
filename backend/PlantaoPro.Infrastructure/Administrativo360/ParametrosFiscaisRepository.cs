using System.Text.Json;
using Dapper;
using Npgsql;
using PlantaoPro.Application.Administrativo360;
using PlantaoPro.Domain.Administrativo360;

namespace PlantaoPro.Infrastructure.Administrativo360;

/// <summary>
/// Parâmetros fiscais de emissão (uma linha por tenant — upsert idempotente).
/// O status é derivado deterministicamente do conjunto de campos (P2: pendência de
/// decisão comercial fica explícita, nunca "configurado" por omissão). A credencial
/// entra apenas como NOME de referência de segredo (A33) — nunca o valor.
/// </summary>
public sealed class ParametrosFiscaisRepository : Adm360Repository, IParametrosFiscaisRepository
{
    public ParametrosFiscaisRepository(string connectionString) : base(connectionString) { }

    public async Task<ParametrosFiscaisDto?> ObterAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var cn = Connection();
        await cn.OpenAsync(ct);

        var linha = await cn.QueryFirstOrDefaultAsync<LinhaParametros>(new CommandDefinition(@"
            select id, tenant_id as ""TenantId"", ltrim(uf) as uf, municipio,
                   regime_fiscal as ""RegimeFiscal"", operacao_fiscal as ""OperacaoFiscal"",
                   cfops::text as ""CfopsJson"", responsavel_id as ""ResponsavelId"",
                   responsavel_nome as ""ResponsavelNome"", ambiente, provedor,
                   certificado_referencia as ""CertificadoReferencia"", status, observacao,
                   created_at as ""CreatedAt"", updated_at as ""UpdatedAt""
              from plantaopro.adm360_parametros_fiscais
             where tenant_id = @tenantId",
            new { tenantId },
            cancellationToken: ct));

        return linha is null ? null : linha.ToDto();
    }

    public async Task<ParametrosFiscaisDto> SalvarAsync(Guid tenantId, Guid usuarioId, SalvarParametrosFiscaisCommand comando, CancellationToken ct = default)
    {
        ParametrosFiscaisRegras.ValidarCampos(
            comando.Uf, comando.Municipio, comando.RegimeFiscal, comando.OperacaoFiscal,
            comando.Cfops, comando.Ambiente, comando.Provedor, comando.CertificadoReferencia);

        var completo = ParametrosFiscaisRegras.ConjuntoCompleto(
            comando.Uf, comando.Municipio, comando.RegimeFiscal, comando.OperacaoFiscal,
            comando.Provedor, comando.Ambiente);
        var status = ParametrosFiscaisRegras.DerivarStatus(completo, comando.BloqueioExterno);

        var cfopsJson = comando.Cfops is { Count: > 0 }
            ? JsonSerializer.Serialize(comando.Cfops)
            : "{}";

        await ExecutarComRetrySerializableAsync(async (cn, tx) =>
        {
            var tenantExiste = await cn.ExecuteScalarAsync<int>(new CommandDefinition(
                "select count(*) from plantaopro.tenants where id = @tenantId",
                new { tenantId }, tx, cancellationToken: ct));
            if (tenantExiste == 0)
                throw new KeyNotFoundException("Tenant não encontrado para cadastro dos parâmetros fiscais.");

            await cn.ExecuteAsync(new CommandDefinition(@"
                insert into plantaopro.adm360_parametros_fiscais(
                    id, tenant_id, uf, municipio, regime_fiscal, operacao_fiscal, cfops,
                    responsavel_id, responsavel_nome, ambiente, provedor, certificado_referencia,
                    status, observacao, created_by, updated_by, created_at, updated_at
                ) values(
                    @id, @tenantId, @uf, @municipio, @regimeFiscal, @operacaoFiscal, cast(@cfopsJson as jsonb),
                    @responsavelId, @responsavelNome, @ambiente, @provedor, @certificadoReferencia,
                    @status, @observacao, @usuarioId, @usuarioId, now(), now()
                )
                on conflict (tenant_id) do update set
                    uf = excluded.uf,
                    municipio = excluded.municipio,
                    regime_fiscal = excluded.regime_fiscal,
                    operacao_fiscal = excluded.operacao_fiscal,
                    cfops = excluded.cfops,
                    responsavel_id = excluded.responsavel_id,
                    responsavel_nome = excluded.responsavel_nome,
                    ambiente = excluded.ambiente,
                    provedor = excluded.provedor,
                    certificado_referencia = excluded.certificado_referencia,
                    status = excluded.status,
                    observacao = excluded.observacao,
                    updated_by = @usuarioId,
                    updated_at = now()",
                new
                {
                    id = Guid.NewGuid(),
                    tenantId,
                    uf = string.IsNullOrWhiteSpace(comando.Uf) ? null : comando.Uf.Trim().ToUpperInvariant(),
                    municipio = comando.Municipio,
                    regimeFiscal = comando.RegimeFiscal?.ToUpperInvariant(),
                    operacaoFiscal = comando.OperacaoFiscal?.ToUpperInvariant(),
                    cfopsJson,
                    responsavelId = comando.ResponsavelId,
                    responsavelNome = comando.ResponsavelNome,
                    ambiente = comando.Ambiente.ToUpperInvariant(),
                    provedor = comando.Provedor?.ToUpperInvariant(),
                    certificadoReferencia = string.IsNullOrWhiteSpace(comando.CertificadoReferencia) ? null : comando.CertificadoReferencia.Trim(),
                    status,
                    observacao = comando.Observacao,
                    usuarioId
                },
                tx,
                cancellationToken: ct));
        }, ct);

        return (await ObterAsync(tenantId, ct))!;
    }

    private sealed record LinhaParametros(
        Guid Id,
        Guid TenantId,
        string? Uf,
        string? Municipio,
        string? RegimeFiscal,
        string? OperacaoFiscal,
        string CfopsJson,
        Guid? ResponsavelId,
        string? ResponsavelNome,
        string Ambiente,
        string? Provedor,
        string? CertificadoReferencia,
        string Status,
        string? Observacao,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    )
    {
        public ParametrosFiscaisDto ToDto()
        {
            Dictionary<string, string> cfops = new(StringComparer.OrdinalIgnoreCase);
            using var doc = JsonDocument.Parse(CfopsJson ?? "{}");
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                foreach (var prop in doc.RootElement.EnumerateObject())
                    cfops[prop.Name] = prop.Value.GetString() ?? string.Empty;

            return new ParametrosFiscaisDto(
                Id, TenantId, Uf, Municipio, RegimeFiscal, OperacaoFiscal,
                cfops, ResponsavelId, ResponsavelNome, Ambiente, Provedor,
                CertificadoReferencia, Status, Observacao, CreatedAt, UpdatedAt);
        }
    }
}
