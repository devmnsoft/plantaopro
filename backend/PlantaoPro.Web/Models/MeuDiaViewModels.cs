namespace PlantaoPro.Web.Models;

public sealed record MeuDiaIndicadorViewModel(string Chave, string Titulo, decimal Valor, string Icone, string Severidade);
public sealed record MeuDiaItemViewModel(Guid Id, string Tipo, string Titulo, string Descricao, string Prioridade, DateTime? Prazo, string Responsavel, string Status, string Icone, string Controller, string Action, bool PodeConcluir, bool PodeAdiar, Guid? TenantId = null, Guid? ClienteId = null);
public sealed record MeuDiaViewModel(IEnumerable<MeuDiaIndicadorViewModel> Indicadores, IEnumerable<MeuDiaItemViewModel> Pendencias, IEnumerable<MeuDiaItemViewModel> Agenda, IEnumerable<MeuDiaItemViewModel> Alertas, IEnumerable<MeuDiaItemViewModel> AcoesRapidas, string Saudacao, string Contexto, string? ErrorMessage = null)
{
    public bool HasItems => Pendencias.Any() || Agenda.Any() || Alertas.Any() || AcoesRapidas.Any();
    public static MeuDiaViewModel Empty(string saudacao, string contexto, string? erro = null) => new(Array.Empty<MeuDiaIndicadorViewModel>(), Array.Empty<MeuDiaItemViewModel>(), Array.Empty<MeuDiaItemViewModel>(), Array.Empty<MeuDiaItemViewModel>(), Array.Empty<MeuDiaItemViewModel>(), saudacao, contexto, erro);
}
