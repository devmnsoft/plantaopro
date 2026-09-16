# Gestão por unidade e decisões — evidência funcional

**Baseline inspecionada:** `f7d74dc` (`work`), em 2026-09-16. O diretório estava limpo. A validação abaixo deriva do código e dos schemas reais; relatórios anteriores não foram usados como prova de execução.

## Escopo entregue

- A Central Operacional agora possui fila real de substituições, filtros por unidade, situação, data de envio e profissional, paginação, ordenação estável e retorno preservado ao detalhe.
- O detalhe separa pedido, registro afetado, impacto e decisão. Aprovar e rejeitar são comandos explícitos e exigem justificativa e versão.
- A API revalida cliente, permissão específica e vínculo com a unidade no momento do POST. A alteração usa transação serializável, bloqueio da linha, estado e versão esperados, quantidade afetada e histórico na mesma transação.
- Aprovar o pedido apenas autoriza a próxima etapa. O profissional original continua responsável até o fluxo canônico confirmar um substituto elegível; a efetivação existente permanece serializável, protegida por advisory lock e versão.
- A fila direciona confirmações ao módulo de Escalas e correções/ausências à Conferência de Execução, sem replicar estados em uma nova tabela.

## Matriz de integrações verificada no código

| Integração | Classificação | Evidência / limite |
|---|---|---|
| Central Operacional | Implementada e verificada estaticamente | Resumo existente e nova fila/detalhe MVC ligados a `api/substituicoes`. |
| Portal do Profissional | Implementada parcialmente | A abertura e consulta de substituições já usam `substituicoes_plantao`; não foi executado E2E por ausência do SDK/runtime. |
| Unidades e permissões | Implementada e verificada estaticamente | Cliente no SQL, `PermissionGuardService` e `TenantGuardService` reavaliados no POST. Visão agregada permanece restrita ao cliente do token. |
| Cobertura/substituição | Implementada e verificada estaticamente | Estado/versionamento condicional na decisão; efetivação canônica existente preservada. |
| Confirmações | Implementada parcialmente | Acesso ao fluxo real de Escalas; não unificado artificialmente na tabela de pedidos. |
| Correções de execução | Implementada e verificada estaticamente | Serviço existente possui comparação, versão da presença, transação e tratamento pós-apuração; fila oferece acesso à ação própria. |
| Notificações | Implementada parcialmente | O profissional consulta o estado persistido no portal. A entrega assíncrona não foi ampliada nesta mudança; situação de entrega não é apresentada como sucesso. |
| Auditoria/histórico | Implementada e verificada estaticamente | Aprovação, decisão e histórico são persistidos; histórico da substituição entra na transação da decisão. |

## Estados e transições

| Origem | Comando | Destino | Efeito operacional |
|---|---|---|---|
| `SOLICITADA` | Aprovar | `APROVADA` | Libera busca/convite; não troca a atribuição. |
| `SOLICITADA` | Rejeitar | `RECUSADA` | Encerra pedido, mantém atribuição e grava justificativa. |
| Qualquer estado diferente de `SOLICITADA` | Aprovar/rejeitar | Sem alteração (`409`) | Informa decisão prévia/avanço do fluxo. |
| `SOLICITADA` com versão divergente | Aprovar/rejeitar | Sem alteração (`409`) | Exige nova revisão. |
| `AGUARDANDO_APROVACAO` | Confirmar substituto no fluxo canônico | `CONFIRMADA` | Cria nova escala e encerra a original atomicamente, após elegibilidade e conflitos. |

## Evidência de consistência persistida

O `UPDATE` exige `status='SOLICITADA' and versao=@versaoEsperada`, incrementa a versão e valida exatamente uma linha afetada. A aprovação/rejeição e `substituicao_historico` usam a mesma transação serializável. Repetição após timeout retorna o estado já decidido em vez de reaplicar efeitos.

## Verificações

- **Aprovada:** `git diff --check`.
- **Bloqueada pelo ambiente:** restore, builds Debug/Release, testes .NET e E2E de navegador; o executável `dotnet` não está instalado (`command not found`).
- **Bloqueada pelo ambiente:** screenshots reais em 360/768/1440; sem aplicação compilável/navegador executável nesta sessão. A folha responsiva foi revisada estaticamente.
- **Não aplicável:** migration/instalação limpa/upgrade; a entrega reutiliza tabelas e migrations existentes e não altera SQL de schema.

## Pendências objetivas

1. Executar a suíte com PostgreSQL isolado quando .NET 10 estiver disponível.
2. Homologar visualmente 360, 768 e 1440 pixels e anexar screenshots reais.
3. Evoluir o outbox de notificação para decisões de substituição em rodada própria; não alegar entrega de e-mail até isso existir.
