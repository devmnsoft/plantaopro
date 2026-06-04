# Relatório de estabilização PlantãoPro — 2026-06-04

## Escopo

Rodada executada para proteger o PlantãoPro após a execução indevida de um prompt de outro produto em branch separada. O foco foi confirmar higiene de branch, ausência de resíduos indevidos, estabilidade técnica mínima e prontidão para Beta Comercial Controlada.

## Resultado da higiene Git

- Branch de trabalho confirmada: `work`.
- Backup local de segurança criado antes da limpeza.
- A branch incorreta informada no prompt não estava disponível localmente para novo merge ou comparação direta.
- O histórico recente mostra que a branch incorreta foi revertida por commit de reversão do merge incorreto e depois estabilizada por PRs posteriores.
- Nenhum merge adicional da branch incorreta foi realizado nesta rodada.

## Resultado da varredura de resíduos

A varredura de termos do produto incorreto não encontrou referências de domínio proibidas versionadas. As ocorrências remanescentes são legítimas do PlantãoPro:

- Referências a React Native/Expo pertencem ao app mobile oficial em `mobile/PlantaoPro.App` e à documentação da Sprint Zero mobile do PlantãoPro.
- Referências a exportação são do módulo de auditoria/relatórios do PlantãoPro, não de outro produto.
- Um diretório raiz de app externo, local e ignorado pelo Git, continha `node_modules` no workspace e foi removido; ele não estava versionado.

## Validações técnicas executadas

- `git status`, `git diff --stat` e `git diff --cached --stat` revisados antes das alterações.
- `git ls-files` não retornou binários ou diretórios `bin`, `obj` e `.vs` versionados.
- Varreduras Razor/C#/SQL executadas para padrões proibidos. As ocorrências de `StatusCode(...)` encontradas são chamadas legítimas de `ControllerBase.StatusCode`, não métodos locais sombreando a API.
- `dotnet clean`/`dotnet build` não puderam ser executados neste container porque o SDK .NET não está instalado (`dotnet: command not found`). Devem ser repetidos no CI ou ambiente de homologação com SDK.

## Funcionalidades cobertas pela documentação de homologação

- Operação Assistida com checklist, ocorrências críticas, treinamentos, toast, modal e auditoria.
- Fluxo médico ponta a ponta: plantão, solicitação, escala, realização, pagamento e notificação.
- Fluxo SaaS básico: plano, assinatura, fatura, inadimplência, suspensão e reativação.
- API Mobile MVP com JWT, paginação, payload leve, 401 sem token e 403 para plano sem mobile.
- Segurança multiempresa, auditoria, observabilidade, relatórios, exportações, suporte e Customer Success.

## Pendências de homologação em ambiente completo

- Reexecutar `dotnet clean`, `dotnet build` e `dotnet test` no CI/ambiente com SDK .NET.
- Subir API/Web com PostgreSQL de homologação, abrir `/swagger`, `/api/health`, `/Account/Login`, Dashboard, 403 e 404 amigáveis.
- Executar o roteiro manual final da Beta Controlada do checklist de homologação.
