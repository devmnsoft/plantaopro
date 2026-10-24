namespace PlantaoPro.Web.Models;

// R103 (integracoes - WhatsApp/e-mail com opt-in): modelos somente leitura do outbox de
// mensagens externas renderizado na tela de preferencias de notificacoes.
public class MensagemOutboxItemViewModel
{
    public string Canal { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string Evento { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CriadoEm { get; set; } = string.Empty;
}

public class MensageriaResumoViewModel
{
    public long Total { get; set; }
    public long Enviadas { get; set; }
    public long BloqueadasSemConsentimento { get; set; }
    public long SemDestino { get; set; }
}

public class MensageriaPreferenciasViewModel
{
    public List<MensagemOutboxItemViewModel> Itens { get; set; } = new();
    public MensageriaResumoViewModel Resumo { get; set; } = new();
}
