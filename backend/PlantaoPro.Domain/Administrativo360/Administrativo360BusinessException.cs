namespace PlantaoPro.Domain.Administrativo360;

/// <summary>
/// Violação CONHECIDA de regra de negócio do módulo Administrativo 360. Lançada pelos
/// guards de domínio (PlantaoPro.Domain) e repositórios (PlantaoPro.Infrastructure) e
/// convertida em HTTP 400 com mensagem amigável pelo filtro da API
/// (Adm360BusinessExceptionFilter). Falhas técnicas (outros tipos de exceção, inclusive
/// InvalidOperationException genérica lançada por código não guardado) seguem para o
/// handler global: HTTP 500 + log estruturado com correlação.
/// Herda de InvalidOperationException para preservar catches legados por tipo base nos
/// controllers do módulo (ex.: Adm360CadastrosController).
/// </summary>
public sealed class Administrativo360BusinessException : InvalidOperationException
{
    public Administrativo360BusinessException(string message) : base(message) { }
}
