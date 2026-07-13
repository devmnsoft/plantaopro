namespace PlantaoPro.Api.Models;

public record OperacaoPendenciaDto(string Titulo,string Motivo,string Prioridade,string PerfilResponsavel,string Cta,string UrlOrigem,DateTime? Prazo,string Status,string Modulo,string TenantId,string ClienteId);
public record OperacaoRecomendacaoDto(string Severidade,string PerfilAlvo,string Modulo,string Entidade,string EntidadeId,string Mensagem,string AcaoSugerida,string UrlAcao,DateTime CreatedAt,string TenantId,string ClienteId);
public record OperacaoInteligenteResumoDto(IEnumerable<OperacaoPendenciaDto> Pendencias,IEnumerable<OperacaoRecomendacaoDto> Recomendacoes,IEnumerable<DashboardChartItem> DistribuicaoPorPrioridade,string MensagemGuia,string ProximoPasso);
