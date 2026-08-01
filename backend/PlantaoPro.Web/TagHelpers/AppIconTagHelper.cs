using Microsoft.AspNetCore.Razor.TagHelpers;
using PlantaoPro.Web.DesignSystem;

namespace PlantaoPro.Web.TagHelpers;

[HtmlTargetElement("app-icon")]
public sealed class AppIconTagHelper : TagHelper
{
    [HtmlAttributeName("name")]
    public AppIconKey Name { get; set; } = AppIconKey.Unknown;

    [HtmlAttributeName("size")]
    public int Size { get; set; } = 20;

    [HtmlAttributeName("label")]
    public string? Label { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var icon = AppIconRegistry.Resolve(Name);
        var size = Math.Clamp(Size, 12, 64);
        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "pp-icon");
        output.Attributes.SetAttribute("width", size);
        output.Attributes.SetAttribute("height", size);
        output.Attributes.SetAttribute("focusable", "false");
        output.Attributes.SetAttribute("role", string.IsNullOrWhiteSpace(Label) ? "presentation" : "img");
        if (string.IsNullOrWhiteSpace(Label)) output.Attributes.SetAttribute("aria-hidden", "true");
        else output.Attributes.SetAttribute("aria-label", Label);
        output.Content.SetHtmlContent($"<use href=\"/assets/icons/sprite.svg#{icon.SymbolId}\"></use>");
    }
}
