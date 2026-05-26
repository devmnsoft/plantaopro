using Dapper;
using Npgsql;
using PlantaoPro.Api.Models;

namespace PlantaoPro.Api.Data;

public static class PlantaoStatus
{
    public const string Rascunho = "rascunho";
    public const string Aberto = "aberto";
    public const string EmEscala = "em_escala";
    public const string Preenchido = "preenchido";
    public const string EmAndamento = "em_andamento";
    public const string Realizado = "realizado";
    public const string Cancelado = "cancelado";
    public const string Encerrado = "encerrado";
}

public sealed class PlantaoRegraService
{
    public ApiResponse<string> ValidarCriacao(CreatePlantaoRequest r)
    {
        if (r.DataFim <= r.DataInicio) return ApiResponse<string>.Fail("Data fim deve ser maior que data início.");
        if (r.Valor < 0) return ApiResponse<string>.Fail("Valor não pode ser negativo.");
        if (r.Vagas <= 0) return ApiResponse<string>.Fail("Vagas deve ser maior que zero.");
        return ApiResponse<string>.Ok("ok");
    }

    public ApiResponse<string> ValidarEdicao(string statusAtual, UpdatePlantaoRequest r)
    {
        if (statusAtual != PlantaoStatus.Rascunho && statusAtual != PlantaoStatus.Aberto)
            return ApiResponse<string>.Fail("Plantão não pode ser editado neste status.");
        if (r.DataFim <= r.DataInicio) return ApiResponse<string>.Fail("Data fim deve ser maior que data início.");
        if (r.Valor < 0) return ApiResponse<string>.Fail("Valor não pode ser negativo.");
        if (r.Vagas <= 0) return ApiResponse<string>.Fail("Vagas deve ser maior que zero.");
        return ApiResponse<string>.Ok("ok");
    }
}

public sealed class PlantaoTransicaoService
{
    public bool PodeTransicionar(string atual, string novo) => (atual, novo) switch
    {
        (PlantaoStatus.Rascunho, PlantaoStatus.Aberto) => true,
        (PlantaoStatus.Rascunho, PlantaoStatus.Cancelado) => true,
        (PlantaoStatus.Aberto, PlantaoStatus.EmEscala) => true,
        (PlantaoStatus.Aberto, PlantaoStatus.Preenchido) => true,
        (PlantaoStatus.Aberto, PlantaoStatus.Cancelado) => true,
        (PlantaoStatus.EmEscala, PlantaoStatus.Preenchido) => true,
        (PlantaoStatus.EmEscala, PlantaoStatus.Cancelado) => true,
        (PlantaoStatus.Preenchido, PlantaoStatus.EmAndamento) => true,
        (PlantaoStatus.Preenchido, PlantaoStatus.Realizado) => true,
        (PlantaoStatus.EmAndamento, PlantaoStatus.Realizado) => true,
        (PlantaoStatus.Realizado, PlantaoStatus.Encerrado) => true,
        _ => false
    };
}

public sealed class PlantaoHistoricoService
{
    public async Task RegistrarAsync(NpgsqlConnection cn, NpgsqlTransaction tx, Guid plantaoId, string? statusAnterior, string statusNovo, string? justificativa, Guid usuarioId)
    {
        await cn.ExecuteAsync(@"insert into plantaopro.historico_plantao
(id,plantao_id,status_anterior,status_novo,justificativa,usuario_id,reg_date)
values(gen_random_uuid(),@plantaoId,@statusAnterior,@statusNovo,@justificativa,@usuarioId,now())",
            new { plantaoId, statusAnterior, statusNovo, justificativa, usuarioId }, tx);
    }
}
