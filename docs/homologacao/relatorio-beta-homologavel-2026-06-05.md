# Relatório de evolução — Beta Homologável PlantãoPro (2026-06-05)

## Escopo da rodada

Este relatório registra a consolidação da versão Beta Homologável do PlantãoPro para operação assistida com cliente real, demonstração comercial, validação técnica, validação operacional e preparação de deploy controlado.

## Higiene do repositório

Comandos executados antes de qualquer alteração:

```bash
git branch --show-current
git branch --all
git status --short
git diff --stat
git log --oneline --decorate -10
git branch backup/antes-beta-homologavel-plantaopro
rg -n "<termos-proibidos-do-produto-externo>" .
```

Resultado:

- Branch atual: `work`.
- Nenhum merge foi feito a partir da branch incorreta de outro produto.
- Foi criada a branch local de backup `backup/antes-beta-homologavel-plantaopro`.
- O diretório local ignorado do app externo foi removido do workspace de release, mantendo apenas o app oficial do PlantãoPro em `mobile/PlantaoPro.App`.
- A varredura de termos encontrou apenas falsos positivos de documentação/mobile oficial do PlantãoPro; não foram identificados módulos, APIs, domínios ou portas do produto incorreto em código versionado do PlantãoPro.

## Estabilização técnica executada

Comandos de inspeção executados:

```bash
git status
git diff --stat
git diff --cached --stat
git ls-files | grep -E "(\.dll|\.exe|\.pdb|\.cache|\.zip|\.apk|\.aab|\.db|\.sqlite)$"
git ls-files | grep -E "(^|/)(bin|obj|\.vs)/"
rg -n "@page|asp-page|@model dynamic|item\.ToString\(|href=\"#\"|ADD CONSTRAINT IF NOT EXISTS|alert\(|confirm\(|Type\(\)|Message\(\)|= \[\]|return \[\]|new string\(\)" backend/PlantaoPro.Web backend/PlantaoPro.Api
rg -n "sealed class .*Detalhe.*:|StatusCode\s*\(|class AccountController|class HomeController|new .*ViewModel\(\)|<partial name=|PartialAsync\(" backend/PlantaoPro.Web backend/PlantaoPro.Api
```

Resultado:

- Não há binários, pacotes, APK/AAB, SQLite/DB, `bin/`, `obj/` ou `.vs/` versionados.
- A varredura obrigatória não apontou uso de `@page`, `asp-page`, `@model dynamic`, `href="#"`, `alert()`, `confirm()`, `new string()`, collection expressions ou `ADD CONSTRAINT IF NOT EXISTS` no código da API/Web.
- As ocorrências de `<partial name=...>` e `PartialAsync(...)` são usos esperados de MVC/Razor com model explícito ou partial sem model.
- As ocorrências de `StatusCode(...)` são chamadas legítimas de `ControllerBase.StatusCode(...)`, não método local ocultando o framework.

## Build, testes e limitação do ambiente

Tentativa executada:

```bash
dotnet clean backend/PlantaoPro.Api/PlantaoPro.Api.csproj
dotnet clean backend/PlantaoPro.Web/PlantaoPro.Web.csproj
dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj
dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj
```

Resultado no container atual:

- `dotnet: command not found`.
- A validação de build/testes deve ser repetida no ambiente de homologação ou CI com SDK .NET instalado.
- Os testes de contrato foram reforçados para validar higiene do repositório e presença dos documentos obrigatórios da Beta Homologável.

## Validação funcional esperada em homologação

Executar o roteiro manual final descrito em `docs/homologacao/checklist-beta-comercial.md`, cobrindo:

1. API em `/swagger` e `/api/health`.
2. Web em `/Account/Login`.
3. Login/logout de ADMINISTRADOR_GLOBAL e MEDICO.
4. Dashboard, Minha Agenda, Operação Assistida, Auditoria e Observabilidade.
5. Fluxo operacional: cliente, hospital, especialidade, médico, plantão, publicação, solicitação, escala, realização, pagamento, notificação e auditoria.
6. Fluxo SaaS: cliente, plano, assinatura, uso, fatura, pagamento, inadimplência, suspensão e reativação.
7. API Mobile MVP com JWT, paginação, payload leve, 401 sem token e 403 amigável quando o plano não permitir mobile.
8. Exportações CSV com auditoria e respeito a `cliente_id`.
9. 403/404 amigáveis e responsividade mobile-first.

## Pendências controladas para operação assistida

- Reexecutar `dotnet build` e `dotnet test` no CI/homologação com SDK .NET.
- Registrar evidências de tela do teste manual final com usuário, horário e resultado esperado.
- Validar carga/performance com massa real do cliente piloto.
- Manter publicação em lojas, push notification real e integrações externas fora do escopo da Beta Homologável.

## Critério de aceite para promover a Beta

A Beta pode seguir para operação assistida quando:

- O CI confirmar build verde.
- O roteiro manual final for concluído sem exceção técnica.
- Não houver ocorrência crítica aberta.
- Segurança multiempresa estiver validada por perfil.
- Operação Assistida, fluxo operacional médico e faturamento SaaS básico tiverem evidências aprovadas.
