# Prompt operacional — acesso local e workspace PlantãoPro

Use este prompt em implementações seguintes. Não inventar login hardcoded na API ou na Web de produção.

## Objetivo
Garantir que um ambiente Development consiga autenticar com três identidades persistidas no PostgreSQL, com BCrypt, perfis canônicos e jornadas distintas após o login.

## Identidades estabelecidas (somente Development)
| Papel | E-mail | Senha | Perfil | Destino |
|---|---|---|---|---|
| Super administrador MNSOFT | `superadmin@mnsoft.example` | `MnSoft!Demo2026#Admin` | `ADMINISTRADOR_GLOBAL` | Command Center / clientes SaaS |
| Gestor fictício da Santa Casa | `gestor@santacasa-demo.example` | `SantaCasa!Demo2026#Gestor` | `ADMINISTRADOR_CLIENTE` | Meu Dia operacional |
| Médica fictícia da Santa Casa | `medico@santacasa-demo.example` | `Medico!Demo2026#Acesso` | `MEDICO` | Agenda pessoal / plantões |

A tela de login **não** publica senhas. Credenciais vivem em `appsettings.Development.json`, no seed SQL e neste documento interno.

## Regras de implementação
1. Autenticação continua 100% PostgreSQL + `BCrypt.Net.BCrypt.Verify`. Sem comparação de senha em texto puro.
2. Produção não herda `DemoSeed:Enabled`. `appsettings.json` de produção permanece sem senhas.
3. Auto-provisionamento só em `IsDevelopment()` + `DemoSeed:Enabled` + `DemoSeed:AutoProvisionIfEmpty`.
4. Se o connection string legado apontar para o banco `postgres` e `Database:AllowLegacyPostgresDatabase=true`, o seed pode gravar ali desde que `plantaopro.usuarios` exista.
5. Se já houver outro `ADMINISTRADOR_GLOBAL`, não criar segundo superadmin; ainda assim provisionar gestor e médico.
6. Reset de senha demo revoga sessões em `plantaopro.auth_sessoes`.
7. Query de login exige `clientes.reg_status`, `nome_fantasia`, `razao_social` e tabela `medicos` — o seed cria essas colunas se faltarem.
8. Template autenticado: superadmin vê plataforma; gestor vê Escalas/Execução/Conferência; médico vê agenda pessoal.
9. JWT de Development precisa de chave com ≥ 32 caracteres (`Jwt__Key` por user-secrets ou env). Sem isso a API nem sobe.

## Como aplicar agora
1. Subir API e Web em Development.
2. Na primeira subida da API, o auto-provision grava/reescreve as três contas.
3. Alternativa manual: executar `database/seeds/development/121_acesso_demo_local.sql` no mesmo banco da connection string.
4. Entrar em `/Account/Login` com um dos e-mails acima.

## Fora de escopo
Não misturar tenants. Não exibir senha na UI. Não habilitar lockout desligado em Production.
