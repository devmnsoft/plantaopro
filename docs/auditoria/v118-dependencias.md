# v1.18 — Relatório de dependências

Data UTC: 2026-07-21.

## Decisões aplicadas

- Mantido `TargetFramework` `net10.0` e `LangVersion` `10.0` nos projetos existentes.
- Unificada a versão de `Npgsql` entre API e Infrastructure para `10.0.2`, evitando mix entre `10.0.2` e `8.0.9`.
- Atualizado `Microsoft.AspNetCore.Authentication.JwtBearer` de `6.0.36` para `10.0.2`, compatível com a aplicação `net10.0` configurada no repositório.
- Removido `Swashbuckle.AspNetCore` do projeto Web porque o Web MVC não expõe o contrato OpenAPI; Swagger permanece no projeto API.

## Validação local

Os comandos `dotnet list`, `dotnet build` e `dotnet format` não puderam ser executados localmente porque o SDK `dotnet` não está instalado no contêiner (`dotnet: command not found`). A validação completa fica bloqueada pelo ambiente local e deve ocorrer no GitHub Actions com `actions/setup-dotnet@v4`.
