# Rotas para validação v1.58.0

## Escopo do smoke

`/Account/Login`, `/AdminSaas/Index`, `/Home/Dashboard`, `/MinhaCentral`, `/MeuDia`, `/Agenda`, `/Plantoes`, `/Escalas`, `/Saude360`, `/Pacientes`, `/Agendamentos`, `/Triagem`, `/Consultas`, `/Pagamentos` e `/Configuracoes`.

## Situação neste ambiente

- **Análise estática:** executada pelo conjunto de scripts de regressão.
- **Runtime:** bloqueado porque o SDK .NET não está instalado.
- **Screenshots:** não geradas; dependem do servidor e de uma sessão autenticada válida.
- **Autenticação:** o smoke não contém credenciais. Use `PLANTAOPRO_STORAGE_STATE` para fornecer uma sessão Playwright criada localmente.

## Execução local

```bash
dotnet restore backend/PlantaoPro.sln
dotnet build backend/PlantaoPro.sln -c Release
dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release
dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj
```

Em outro terminal:

```bash
npm install --no-save playwright
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 \
PLANTAOPRO_STORAGE_STATE=/caminho/para/auth-state.json \
node scripts/ui/visual-smoke.mjs
```

O script verifica overflow horizontal, presença do shell, separação entre sidebar e conteúdo no desktop, posição do footer, dimensões dos cards, visibilidade de ações primárias e largura do body. Ele grava screenshots para todas as rotas nas larguras 360, 390, 430, 768, 1024, 1366 e 1920px.
