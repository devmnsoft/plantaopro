namespace PlantaoPro.Web.Models;

public sealed class EmptyStateViewModel
{
    public string Icon { get; set; } = "bi-inbox";
    public string Title { get; set; } = "Nenhum registro encontrado";
    public string Description { get; set; } = "Quando houver dados disponíveis, eles aparecerão aqui.";
    public string? PrimaryActionText { get; set; }
    public string? PrimaryController { get; set; }
    public string? PrimaryAction { get; set; }
    public string? SecondaryActionText { get; set; }
    public string? SecondaryController { get; set; }
    public string? SecondaryAction { get; set; }
}
