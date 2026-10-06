using Microsoft.AspNetCore.Mvc.ModelBinding;
using PlantaoPro.CrossCutting.Localization;

namespace PlantaoPro.Web.Models;

/// <summary>
/// Aplica o contrato monetario/cultural central (<see cref="ValorHumano"/>) a TODO decimal
/// ligado por formulario, route ou query string na aplicacao Web. Substitui o binder padrao
/// do MVC (cultura invariante), que lia "12,50" como 1250 (bug M2.5 do gate financeiro).
/// Falhas vao para o ModelState com mensagem humana, sem termos tecnicos ("Parameter 'x'").
/// </summary>
public sealed class ValorHumanoModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        Type? tipo = context?.Metadata?.ModelType;
        return tipo == typeof(decimal) || tipo == typeof(decimal?) ? new ValorHumanoModelBinder() : null;
    }
}

public sealed class ValorHumanoModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string nome = context.ModelName;
        var bruto = context.ValueProvider.GetValue(nome);
        string? texto = bruto.Values.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(texto))
        {
            if (context.ModelMetadata.ModelType == typeof(decimal?))
            {
                context.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            context.ModelState.TryAddModelError(nome, "Informe um valor numerico.");
            return Task.CompletedTask;
        }

        if (ValorHumano.TentarConverter(texto, out decimal valor, out string erro))
            context.Result = ModelBindingResult.Success(valor);
        else
            context.ModelState.TryAddModelError(nome, erro);

        return Task.CompletedTask;
    }
}
