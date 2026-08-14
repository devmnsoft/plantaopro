# Homologação de build e runtime — v1.74.0

Data: 2026-08-14 (UTC).

## Comandos e resultados

| Comando | Resultado |
|---|---|
| `dotnet --info` | **BLOQUEADO** — `dotnet: command not found` (exit 127). |
| `dotnet restore backend/PlantaoPro.sln` | **NÃO EXECUTADO** — SDK .NET ausente (exit 127). |
| `dotnet build backend/PlantaoPro.sln -c Release` | **NÃO EXECUTADO** — SDK .NET ausente (exit 127). |
| `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release` | **NÃO EXECUTADO** — SDK .NET ausente (exit 127). |

Build, testes e runtime **não são declarados aprovados**. O ambiente não contém o executável `dotnet`.

## Verificações e correções estáticas

- Scanner automatizado: 130 nomes de controller únicos.
- Scanner manual: nenhuma classe `*Controller` repetida em arquivos distintos.
- O contrato de contas clínicas foi ampliado sem inventar campos ausentes; a interface identifica explicitamente dados não fornecidos.
- Consultas ganharam navegação por rotas MVC reais para faturamento e financeiro clínico.

## Pendências de runtime

1. Instalar o SDK compatível com os `TargetFrameworks` da solução.
2. Configurar banco, secrets e URL da API conforme a documentação do repositório.
3. Executar restore, build e test acima.
4. Iniciar API e Web, autenticar um usuário com tenant e permissões clínicas/financeiras.
5. Salvar o estado Playwright e executar o smoke v1.74.0.

## Visual Studio / Windows

1. Instale Visual Studio 2022 com **ASP.NET e desenvolvimento Web** e o SDK indicado pelos projetos.
2. Abra `backend/PlantaoPro.sln` e restaure os pacotes NuGet.
3. Selecione `Release` e use **Build > Rebuild Solution**.
4. Rode `PlantaoPro.Tests` pelo Test Explorer.
5. Defina API e Web como projetos de inicialização, aplique a configuração local e valide as rotas descritas na matriz v1.74.
