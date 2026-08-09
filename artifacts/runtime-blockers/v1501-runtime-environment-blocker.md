# PlantãoPro v1.50.1 — bloqueio do ambiente de runtime

Data da verificação: 2026-08-09 (UTC)

## Identificação do checkout

- Branch atual: `work`.
- Commit atual: `2d5133a3a9ada57e28cf59689ee2f76d2a52c759`.
- Remote atual: `origin`, configurado como `https://github.com/devmnsoft/plantaopro.git` para fetch e push.
- Fetch do remote: falhou com `CONNECT tunnel failed, response 403`.
- Branch `main`: indisponível no clone local e não obtida do remote.

## SDK .NET

- `dotnet --info`: falhou com exit code 127 (`dotnet: command not found`).
- `which dotnet`: nenhum resultado.
- `find / -name dotnet -type f 2>/dev/null | head -20`: nenhum resultado.
- `/usr/share/dotnet`: inexistente.
- `~/.dotnet`: inexistente.
- Tentativa na fonte `https://dot.net/v1/dotnet-install.sh`: proxy respondeu HTTP 403 (`curl` exit 56).
- Tentativa na fonte `https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb`: proxy respondeu HTTP 403 (`curl` exit 56).
- Instalação: não executada, pois nenhuma fonte pôde ser alcançada.

## Validações não executadas

Por ausência do SDK, não foram executados:

```bash
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release
dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj
```

Consequentemente, não houve validação de login, navegação, assets, layout, endpoints ou screenshots. Nenhum CSS, Razor, código de runtime ou redesign foi alterado.

## Impacto

O template não pode ser diagnosticado de forma confiável nem redesenhado sem build e runtime. A branch de trabalho desejada também não pode ser baseada em uma `main` atualizada enquanto o fetch permanecer bloqueado.

## Necessário para continuar

1. Liberar acesso HTTPS ao GitHub para obter a branch `main`, ou fornecer localmente um checkout atualizado dela.
2. Disponibilizar no ambiente um SDK .NET compatível com a solução, ou liberar uma fonte oficial de instalação.
3. Reexecutar restore, build e testes até aprovação.
4. Iniciar a aplicação Web, validar autenticação apenas com bootstrap/credencial local documentada e percorrer as rotas solicitadas.
5. Somente depois gerar screenshots e o diagnóstico visual de runtime; o redesign deve permanecer bloqueado até essa validação.
