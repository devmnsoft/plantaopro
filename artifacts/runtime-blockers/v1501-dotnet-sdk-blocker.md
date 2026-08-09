# PlantãoPro v1.50.1 — bloqueio do SDK .NET

Data da verificação: 2026-08-09 (UTC)

## Resultado local

O comando solicitado falhou porque o executável não existe no ambiente:

```bash
dotnet --info
# /bin/bash: dotnet: command not found
# exit code: 127
```

As buscas `which dotnet`, `find / -name dotnet -type f`, `/usr/share/dotnet` e `~/.dotnet` não localizaram uma instalação existente.

## Fontes oficiais testadas

Ambas as fontes foram consultadas uma única vez com `curl -sSIL --max-time 20`. O proxy recusou o túnel antes que o download pudesse começar.

| Fonte | Comando | Status observado | Resultado |
| --- | --- | --- | --- |
| `https://dot.net/v1/dotnet-install.sh` | `curl -sSIL --max-time 20 https://dot.net/v1/dotnet-install.sh` | Resposta do proxy: HTTP 403; código reportado pelo curl: `000`; exit code 56 | `CONNECT tunnel failed, response 403` |
| `https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb` | `curl -sSIL --max-time 20 https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb` | Resposta do proxy: HTTP 403; código reportado pelo curl: `000`; exit code 56 | `CONNECT tunnel failed, response 403` |

O sistema base identificado é Ubuntu 24.04.4 LTS. Nenhuma repetição de download nem instalação parcial foi realizada após a confirmação do bloqueio.

## Impacto e desbloqueio necessário

Sem um SDK .NET compatível não é possível executar restore, build, testes, a aplicação Web ou a auditoria visual real. Para continuar, é necessário disponibilizar o SDK .NET no contêiner (e expor `dotnet` no `PATH`) ou liberar no proxy uma fonte oficial apropriada para instalação.
