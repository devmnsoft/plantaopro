# Smoke visual v1.66 — instruções

Nenhuma imagem foi gerada neste ambiente: o comando `dotnet --info` falhou porque o SDK não está instalado. Não há evidência artificial.

## Execução local

1. Instale o SDK definido pelo projeto e restaure: `dotnet restore backend/PlantaoPro.sln`.
2. Inicie a aplicação em uma configuração real: `dotnet run --project backend/PlantaoPro.Web/PlantaoPro.Web.csproj`.
3. Instale a dependência Playwright usada pelo repositório e seus browsers, se necessário.
4. Faça login com uma conta real e salve o storage state Playwright em um arquivo fora do Git.
5. Execute:

```bash
PLANTAOPRO_BASE_URL=http://127.0.0.1:5000 \
PLANTAOPRO_STORAGE_STATE=/caminho/seguro/storage-state.json \
node scripts/ui/visual-smoke.mjs
```

As capturas serão gravadas em `artifacts/ui-audit/screenshots/v166/`. O smoke percorre 19 rotas em 360×800, 390×844, 430×932, 768×1024, 1024×768, 1366×768 e 1920×1080. Ele falha em overflow horizontal crítico, shell/containers ausentes, sobreposição da topbar/sidebar, cards sem dimensão, tabelas sem resposta mobile, overlays no fluxo, CTA invisível ou contrato específico ausente.
