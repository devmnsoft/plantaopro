# v1.67 — execução do smoke visual

## Pré-requisitos

1. Instale o SDK .NET compatível, restaure e inicie `backend/PlantaoPro.Web` em um banco de homologação autorizado.
2. Instale as dependências Playwright do repositório.
3. Autentique cada perfil necessário e salve o storage state **fora do Git**. Não use usuário ou dado fictício.

## Execução

```bash
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 \
PLANTAOPRO_STORAGE_STATE=/caminho/seguro/auth-state.json \
node scripts/ui/visual-smoke.mjs
```

O script visita 20 rotas em `360x800`, `390x844`, `430x932`, `768x1024`, `1024x768`, `1366x768`, `1440x900` e `1920x1080`. As evidências reais são gravadas em `artifacts/ui-audit/screenshots/v167/`.

Uma execução sem storage state falha explicitamente nas rotas autenticadas. Falhas de HTTP, redirecionamento ao login, overflow, corte de cards, sobreposição do shell, tabelas sem adaptação, forms fora do contrato ou dialogs inacessíveis também encerram o comando com código diferente de zero.

## Bloqueio deste ambiente

Em 13/08/2026, `dotnet --info` retornou `command not found`. Não foram criadas imagens artificiais nem alegadas capturas autenticadas.
