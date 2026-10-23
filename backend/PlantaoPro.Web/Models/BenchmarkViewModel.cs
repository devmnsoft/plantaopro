namespace PlantaoPro.Web.Models;

// R101 - benchmark "seus indicadores vs o segmento" por plano: espelho serializável da resposta
// da API (camelCase) consumida pelo BFF server-side de /Relatorios/Benchmark.
public sealed record BenchmarkIndicadorViewModel(
    string Codigo,
    string Rotulo,
    decimal? MeuValor,
    string Unidade,
    double? MedianaPlano,
    double? MediaPlano,
    int? PercentilMeu,
    int PeersNaAmostra,
    string Definicao);

public sealed record BenchmarkResultadoViewModel(
    string MesReferencia,
    string PlanoCodigo,
    string PlanoNome,
    int AmostraMinimaPeers,
    bool AmostraInsuficiente,
    bool TemDadosProprios,
    IReadOnlyList<BenchmarkIndicadorViewModel> Indicadores);

public sealed record BenchmarkPageViewModel(string? MesSolicitada, BenchmarkResultadoViewModel? Resultado, string? Erro);
