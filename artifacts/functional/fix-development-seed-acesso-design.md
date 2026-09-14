# Correção do DevelopmentSeed, acesso demonstrativo e design

## Linha de base e causa-raiz

A execução começou no commit `ea7e8bd` e a árvore de trabalho estava limpa. As alterações anteriores do fluxo demonstrativo foram preservadas.

O erro de compilação tinha uma única causa-raiz no bloco SQL de `DevelopmentSeed.cs`: documentos JSON usavam `\"` dentro de uma string verbatim (`@"..."`). Em C# 10, a barra não escapa aspas nesse tipo de literal; a primeira aspa encerrava a string e fazia o compilador interpretar `insert`, `into`, `values` e `conflict` como C#, gerando inclusive o diagnóstico enganoso sobre nove argumentos de `CommandDefinition`.

A correção mantém C# 10, separa texto SQL, parâmetros e `CommandDefinition`, e usa os nomes `commandText`, `parameters`, `transaction` e `cancellationToken` compatíveis com Dapper 2.1.35. Os objetos JSON são serializados por `System.Text.Json`, enviados como parâmetros e convertidos com `cast(@parametro as jsonb)` pelo PostgreSQL.

## Segurança e repetibilidade

O provisionamento continua sendo um comando administrativo explícito. Ele exige simultaneamente ambiente `Development`, `DemoSeed:Enabled=true` e correspondência exata entre `DemoSeed:DevelopmentDatabase` e `current_database()`. Uma transação com `pg_advisory_xact_lock` serializa concorrentes.

Os IDs estáveis e `ON CONFLICT (id) DO NOTHING` preservam dados em reexecuções. Depois das inserções, uma consulta valida IDs, códigos e contexto dos oito registros estruturais; colisões cancelam toda a transação em vez de esconder cadastro parcial. Perfis e usuários preexistentes também têm identidade e contexto conferidos. O provisionamento normal preserva hashes; somente `--reset-demo-passwords` cria novos hashes BCrypt e revoga sessões das duas identidades demonstrativas.

## Provisionar o banco descartável

O schema canônico deve ter sido instalado previamente. A partir da raiz do repositório:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=plantaopro_dev;Username=plantaopro_app;Password=LOCAL_ONLY'
export DemoSeed__Enabled=true
export DemoSeed__DevelopmentDatabase=plantaopro_dev
dotnet run --project backend/PlantaoPro.Api -- --provision-demo
```

As senhas iniciais abaixo são usadas apenas quando a identidade ainda não existe:

| Perfil | Login | Senha inicial |
|---|---|---|
| Super administrador | `superadmin@mnsoft.example` | `MnSoft!Demo2026#Admin` |
| Gestor da Santa Casa Demonstração | `gestor@santacasa-demo.example` | `SantaCasa!Demo2026#Gestor` |

Para redefinir localmente somente essas contas e invalidar suas sessões:

```bash
dotnet run --project backend/PlantaoPro.Api -- --reset-demo-passwords
```

Os domínios `.example` não recebem e-mail. Portanto, recuperação por e-mail não é considerada funcional para essas contas; a redefinição local explícita acima é o procedimento suportado.

## Iniciar e navegar

Em dois terminais:

```bash
dotnet run --project backend/PlantaoPro.Api --launch-profile http
dotnet run --project backend/PlantaoPro.Web --launch-profile http
```

* **Web real:** `http://localhost:52976/Account/Login`
* **Swagger da API:** `http://localhost:51976/swagger` (documentação da API, não a interface Web)

O login público preservado possui antiforgery, autocomplete, validação por campo, alternância de visibilidade da senha, prevenção de envio duplicado e recuperação de estado após demora/erro. O aviso de demonstração só aparece por configuração e nenhuma senha é exibida na página. Após autenticação, o redirecionamento e os painéis existentes continuam usando perfil, tenant, módulos e permissões persistidos, sem atalho por e-mail ou senha mestra.

## Verificações e limitações desta execução

* `git diff --check`: aprovado.
* Inspeção do schema canônico: os conflitos usados pelo seed apontam para chaves primárias por `id`; usuário, perfil e vínculo usam ainda os índices únicos canônicos.
* `dotnet build backend/PlantaoPro.sln --no-restore`: não executado, pois o contêiner não possui `dotnet`.
* Instalação local do SDK com `curl -fsSL https://dot.net/v1/dotnet-install.sh`: bloqueada com HTTP 403 pelo ambiente.
* Testes PostgreSQL e login E2E: não executados, pois o contêiner não possui SDK, `psql`, Docker nem Podman.
* Telas alteradas nesta correção: nenhuma; o refinamento já existente do login foi preservado. Por isso não foi produzida captura nova sem uma aplicação executável.

As contas **não são declaradas funcionais nesta execução**: elas precisam ser provisionadas e os dois logins devem passar pelo fluxo Web/API contra PostgreSQL em CI ou em uma estação com .NET 10 e banco descartável.
