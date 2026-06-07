# PlantãoPro Beta Homologável — Fluxos Operacionais (2026-06-06)

## Escopo da rodada

Esta rodada consolida a preparação do PlantãoPro para homologação Beta com foco em estabilidade de Web/API, leitura padronizada de `ApiResponse<T>`, ausência de resíduos externos e evidências objetivas para QA.

## Branch e base local

- Branch criada: `codex/plantaopro-beta-fluxos-operacionais`.
- Backup local criado: `backup/antes-plantaopro-beta-fluxos-operacionais`.
- Observação real do ambiente: o repositório local não possuía remoto configurado nem branch `main` disponível; a branch foi criada a partir da branch local `work`, que continha o histórico mais recente entregue no workspace.

## Sanitização de resíduo externo

Comando executado antes das alterações:

```bash
rg -n "<padrões do produto externo indevido>" .
```

Resultado: sem ocorrências no workspace versionado analisado.

## Correções implementadas

### Web/API — desserialização e erros amigáveis

- `BaseWebController` agora reconhece explicitamente envelopes `ApiResponse<T>` antes de cair em formatos alternativos (`data`, `payload`, `result`, `items`).
- Envelopes com `success=false` retornados pela API mesmo com HTTP 2xx passam a ser tratados como erro funcional, com mensagem amigável e log contendo endpoint, status e `ResponseSample`.
- O tratamento existente para 403, 404 e 500 foi preservado via `BuildApiErrorMessage`.
- `ComunicacaoController` deixou de desserializar manualmente payloads com `JsonSerializer.Deserialize<ApiResponse<...>>` e passou a usar os métodos padronizados da base (`ReadApiResponse`, `ReadApiListResponseAsync`, `SendApiAsync` e `SendApiWithoutResponseAsync`).

### Testes mínimos de contrato

- Criado teste de contrato para garantir que a Web mantém a desserialização explícita de `ApiResponse<T>`.
- Criado teste de contrato para impedir regressão do `ComunicacaoController` para desserialização manual e sem logs padronizados.

## Endpoints e fluxos revisados estaticamente

A rodada manteve os contratos já existentes para:

- Escalas: `/api/escalas`, ações de confirmar, recusar, cancelar, substituir e marcar realizado.
- Financeiro: `/api/financeiro/pagamentos`, gerar, confirmar, cancelar e meus pagamentos.
- Notificações: listagem, não lidas, marcar lida e marcar todas lidas.
- Dashboard e Mobile: `/api/dashboard` e `/api/mobile/home`.
- Comunicação: conversas, detalhes, nova conversa, envio de mensagem e leitura.

## Validações executadas

- Varredura inicial de resíduos do produto externo indevido: sem ocorrências.
- Varredura de padrões proibidos em Web/API: executada; os resultados residuais principais são ocorrências esperadas/diagnósticas como `StatusCode(...)` em controllers API e criação explícita de ViewModels em controllers/views, sem alteração necessária nesta rodada.
- Build e testes não puderam ser executados no ambiente porque o binário `dotnet` não está instalado.

## Pendências reais

- Executar `dotnet clean`, `dotnet build` e `dotnet test` em ambiente com SDK .NET compatível com `net10.0`.
- Executar QA manual com banco PostgreSQL e usuários de demonstração: login admin, criação de hospital/especialidade/médico/plantão, publicação, aceite médico, confirmação de escala, realização, geração/confirmar pagamento, notificação, auditoria e dashboard.
- Validar Expo (`npm install` e `npx expo start`) em ambiente Node/Expo completo.

## Conclusão

A rodada fortalece a estabilidade da Web contra variações reais de payload da API e cria guarda de teste para impedir regressões na desserialização de `ApiResponse<T>`. O projeto permanece preparado para validação de build/QA em ambiente com SDK .NET disponível.
