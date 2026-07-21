# Diagnóstico real v1.18.4

## Ambiente local

Comandos obrigatórios executados antes das alterações:

- `git status --short`
- `git log -10 --oneline`
- `dotnet --info`
- `dotnet restore backend/PlantaoPro.sln`
- `dotnet build backend/PlantaoPro.sln -c Release`
- `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build --logger "trx;LogFileName=tests.trx" --logger "console;verbosity=detailed"`

Resultado local: a imagem de execução não possui `dotnet` no `PATH`, portanto restore/build/test não puderam ser executados localmente. A evidência registrada no terminal foi `/bin/bash: dotnet: command not found` para `dotnet --info`, `restore`, `build` e `test`.

## Causa raiz de banco

A instalação limpa por migrations falhava porque `database/migrations/2026_plantao_pro_self_service_white_label.sql` altera `plantaopro.planos` antes de a cadeia de instalação garantir a existência da tabela. O risco equivalente foi tratado consolidando a instalação nova em `database/scrpt_completo.sql`, onde `planos` é criado antes de clientes, tenants, assinaturas, white label e sementes referenciais.

## Causa raiz dos testes/artefatos

O workflow executava `dotnet test` e copiava o TRX apenas após sucesso. Quando os testes falhavam, o job encerrava antes de preservar `tests.trx`. O pipeline agora usa `continue-on-error` somente no passo de teste, coleta os TRX em `if: always()` e falha explicitamente em seguida quando o resultado do teste não for sucesso.

## Falha SQL confirmada

- Arquivo: `database/migrations/2026_plantao_pro_self_service_white_label.sql`.
- SQLSTATE esperado do PostgreSQL para relação inexistente: `42P01`.
- Objeto inexistente: `plantaopro.planos`.
- Dependência ausente: tabela `planos` antes de `ALTER TABLE`, seeds, joins e referências SaaS.

## Warnings

A validação local de warnings C# está bloqueada pela ausência do SDK .NET. O CI permanece responsável por registrar `artifacts/build.log` e `artifacts/tests.trx`.
