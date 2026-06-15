namespace PlantaoPro.Web.Services;

public static class ApiErrorPresenter
{
    public static string ToFriendlyMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return "Não foi possível concluir a operação. Tente novamente.";
        if (message.Contains("42P01", StringComparison.OrdinalIgnoreCase) || message.Contains("relation", StringComparison.OrdinalIgnoreCase)) return "A base clínica ainda não foi inicializada. Execute as migrations do Saúde 360.";
        if (message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase)) return "Não foi possível conectar à API. Verifique se o backend está em execução.";
        if (message.Contains("Registro não encontrado", StringComparison.OrdinalIgnoreCase)) return "Não encontramos esse registro. Ele pode ter sido removido ou você pode não ter permissão.";
        if (message.Contains("Tenant", StringComparison.OrdinalIgnoreCase)) return "Não foi possível identificar sua clínica. Saia e entre novamente.";
        return message;
    }
}
