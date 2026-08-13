# v1.64 — execução do smoke visual

## Pré-requisitos

1. Instale o SDK .NET compatível com a solução, Node.js e as dependências do Playwright.
2. Restaure e execute a aplicação com configuração real autorizada (sem seed/mock criado para screenshots).
3. Entre com um usuário de homologação autorizado e exporte o storage state do Playwright para um arquivo fora do Git.

## Execução

```bash
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release
dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj
```

Em outro terminal:

```bash
npm install --no-save playwright
npx playwright install chromium
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 \
PLANTAOPRO_STORAGE_STATE=/caminho/seguro/storage-state.json \
node scripts/ui/visual-smoke.mjs
```

Para validar apenas uma combinação durante diagnóstico:

```bash
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 \
PLANTAOPRO_STORAGE_STATE=/caminho/seguro/storage-state.json \
PLANTAOPRO_VIEWPORTS=390x844 \
node scripts/ui/visual-smoke.mjs
```

## Evidências e leitura do resultado

As capturas reais são gravadas em `artifacts/ui-audit/screenshots/v164/`. A execução completa visita 19 rotas em 7 viewports. Qualquer HTTP inválido, redirecionamento inesperado ao login, overflow, colisão do shell, card sem dimensão, tabela sem contenção, hero desproporcional, drawer atrás da sidebar ou toast atrás da navegação encerra com status diferente de zero.

Revise visualmente todas as imagens: um gate geométrico não avalia sozinho hierarquia, contraste, copy, truncamento significativo ou qualidade percebida. Não versione storage state, credenciais nem screenshots com dados pessoais.

## Estado no contêiner Codex

Em 12/08/2026, `dotnet --info` retornou `command not found`. Por isso o smoke não foi executado e o diretório de screenshots não foi preenchido com evidência artificial.
