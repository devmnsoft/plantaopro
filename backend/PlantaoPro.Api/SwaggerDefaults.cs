using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PlantaoPro.Api;

public sealed class DefaultApiResponseOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses.TryAdd("400", BuildResponse("Requisição inválida", "Validação falhou para os dados enviados."));
        operation.Responses.TryAdd("401", BuildResponse("Não autenticado", "Token JWT ausente, inválido ou expirado."));
        operation.Responses.TryAdd("500", BuildResponse("Erro interno", "Erro inesperado no servidor."));
    }

    private static OpenApiResponse BuildResponse(string title, string message)
    {
        return new OpenApiResponse
        {
            Description = title,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Example = new OpenApiObject
                    {
                        ["success"] = new OpenApiBoolean(false),
                        ["message"] = new OpenApiString(message),
                        ["data"] = new OpenApiNull(),
                        ["errors"] = new OpenApiArray(),
                        ["statusCode"] = new OpenApiInteger(400),
                        ["timestamp"] = new OpenApiString(DateTime.UtcNow.ToString("O"))
                    }
                }
            }
        };
    }
}
