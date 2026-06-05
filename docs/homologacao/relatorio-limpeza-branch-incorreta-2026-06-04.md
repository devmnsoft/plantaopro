# Relatório de limpeza da branch incorreta — 2026-06-04

## Objetivo
Confirmar que o PlantãoPro permaneceu isolado do prompt de outro produto e registrar critérios mínimos para retomar a evolução controlada da beta comercial.

## Confirmação de branch e histórico
- Branch de trabalho validada: `work`.
- Branch local de segurança criada antes da análise: `backup/antes-limpeza-branch-incorreta`.
- O histórico recente contém um merge indevido e, em seguida, um revert explícito registrado no commit `4f98119`.
- A branch local indevida não existe no checkout atual, portanto não foi feito merge novo e não houve tentativa de apagar branch remota.
- A comparação solicitada contra `main` não foi aplicável neste container porque não há referência local `main` nem a branch indevida disponível.

## Varredura de resíduos
Foi executada uma busca completa pelos termos proibidos associados ao produto incorreto, APIs/sites indevidos, domínio de negócio externo e portas indevidas.

Resultado: nenhum match após a remoção de um diretório local ignorado que continha dependências instaladas fora do padrão oficial `app móvel do PlantãoPro`.

> Observação: referências legítimas a aplicativo móvel multiplataforma permanecem apenas na documentação e no app mobile do PlantãoPro, pois a Sprint Zero mobile do próprio PlantãoPro usa `app móvel do PlantãoPro`.

## Arquivos ignorados e binários
- O diretório local ignorado fora do padrão oficial foi removido do workspace para evitar confusão com o app mobile legítimo.
- A verificação de arquivos rastreados não encontrou binários indevidos versionados nas extensões proibidas nem caminhos `bin/`, `obj/` ou `.vs/`.

## Estado técnico
- `git status` iniciou limpo antes das alterações desta rodada.
- As mudanças desta rodada restringem-se a reforço de confirmação UX em Operação Assistida, documentação de limpeza e testes de contrato de higiene.
- `dotnet clean`/`dotnet build` não puderam ser executados neste container porque o comando `dotnet` não está instalado. Devem ser repetidos no ambiente CI/homologação com SDK .NET disponível.

## Critérios para continuidade
- Não criar, restaurar ou mergear a branch indevida.
- Manter app mobile somente em `app móvel do PlantãoPro`.
- Considerar qualquer diretório raiz de APIs/sites que não façam parte da arquitetura PlantãoPro como bloqueador de PR.
- Repetir a varredura de termos proibidos antes de cada release candidate.
