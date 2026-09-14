# PlantãoPro v2.16.1 — estabilização clínica e design

## Base, execução e limites da evidência

- Commit inicial registrado: `4e3b260` (merge da v2.16.0), sem alterações locais. Branch: `codex/v2161-estabilizacao-clinica-design`.
- Foram lidos os workflows `dotnet-ci.yml` e `database-one-click.yml`, os relatórios v2.15.9/v2.16.0, os manifests e os scripts canônicos.
- Os logs dos runs `34836800339` e `34836800404` não estavam acessíveis neste executor: `gh auth status` informou ausência de autenticação e a API do GitHub respondeu HTTP 403. Portanto este relatório não inventa a linha do “primeiro erro”. Os resultados por job citados na solicitação foram usados como inventário, e as causas abaixo foram comprovadas no código/SQL presente no commit inicial.
- O executor não contém `dotnet`, `psql` ou Docker. Download do SDK recebeu HTTP 403 e o repositório APT não oferece os pacotes. Assim, build, PostgreSQL integrado, E2E autenticado e screenshot da aplicação real permanecem gates obrigatórios de CI/homologação, não resultados alegados por este documento.

## Matriz curta de falhas

| Sintoma observado | Causa comprovada | Arquivo | Correção | Teste/gate |
|---|---|---|---|---|
| Criação/replay/instalação interrompem na etapa v2.16.0 | Um único `CREATE TRIGGER` combinava `INSERT` com `UPDATE OF triagem_id`; os eventos precisam de triggers separados. É uma causa raiz independente. | `database/schema/380_v2160_triagem_consulta_jornada.sql` | Triggers distintos para INSERT e UPDATE, função segura para `OLD` e limpeza do nome legado. | Instalação limpa, replay 3x e teste de INSERT/UPDATE no PostgreSQL. |
| Snapshot poderia aceitar triagem de outro paciente no mesmo tenant | A seleção verificava apenas `triagem_id`, `cliente_id` e ativo. | migration v2.16.0 | Exige também `paciente_id`; vínculo incompatível falha com identificadores e o snapshot só é substituído quando o vínculo de triagem muda. | Testes de tenant/paciente divergentes, INSERT, UPDATE e preservação após finalização. |
| Upgrade com duplicidade falha apenas no `CREATE UNIQUE INDEX`, sem diagnóstico operacional | Não havia preflight com os IDs conflitantes. Apagar/mesclar automaticamente seria perda de evidência. | migration v2.16.0 | Preflight bloqueante lista tenant, agendamento e atendimentos; exige correção auditável antes do retry, sem DELETE ou cast. | Fixture com duplicidades e confirmação de preservação. |
| Triagem não propagava a identidade operacional | `triagens` não possuía `atendimento_id`/`unidade_id` na evolução v2.16.0. | migration v2.16.0 | Colunas e backfill determinístico por tenant/agendamento/atendimento ativo. | Upgrade com dado anterior e jornada ponta a ponta. |
| Constraint podia ser confundida com objeto homônimo de outro schema | Busca em `pg_constraint` usava somente `conname`. | migration v2.15.9 | Busca limitada por `conrelid = 'plantaopro.agendamentos'::regclass`. | Fixture com constraint homônima fora do schema. |
| Consulta podia ser marcada finalizada mesmo se atendimento/agendamento relacionado afetasse zero linhas | O retorno do comando múltiplo era ignorado; estado incompatível virava sucesso parcial. Causa comportamental independente da instalação. | `ConsultaApplicationService.cs` | Atualizações separadas, correspondência de tenant/paciente/unidade e rollback com 409 quando a cardinalidade obrigatória não é 1. Walk-in continua sem exigir agendamento. | Teste PostgreSQL transacional e repetição/conflito. |
| Tela não distinguia claramente pendência local de gravação confirmada | Um único texto temporário não mantinha o horário confirmado, e salvar/finalizar tinham peso visual próximo. | workspace médico | Estado persistente com última confirmação, cores por carregamento/pendência/erro/conflito, maior área de texto, foco visível e ações “Salvar rascunho”/“Revisar e finalizar” distintas. | Navegador real desktop/mobile, teclado e console. |

Falhas posteriores à primeira interrupção de banco (runtime, autenticação e segurança dependentes do runtime) são **cascata** até que a instalação e o build concluam. Falhas de compilação, criação one-click e o defeito transacional da finalização são causas independentes e devem ser reavaliadas separadamente.

## Banco e cadeia de atualização

- A migration v2.16.0 registrada não poderia ter sido aplicada com sucesso pelo caminho canônico transacional: a inserção em `schema_migrations` ocorre somente antes do `COMMIT`. Sua correção foi feita na própria etapa inalcançável e os checksums dos manifests/fontes foram atualizados; não há checksum aplicado sendo silenciosamente substituído.
- O SQL consolidado, o script do pgAdmin, hashes e relatórios derivados foram regenerados pelo mecanismo oficial `python3 scripts/generate-scrpt-completo.py`.
- `tstzrange` permanece coerente com `data_inicio`/`data_fim` `timestamptz`; `btree_gist` permanece exigida. Nenhum cast ou constraint foi removido.
- A validação de inconsistência é deliberadamente bloqueante e explicativa. Nenhum `IF EXISTS` foi acrescentado para esconder tabela obrigatória e nenhum dado é removido.

## Jornada e interface

- A finalização da consulta agora preserva atomicidade quando atendimento ou agendamento já mudou de estado/contexto. A consulta, atendimento e agendamento precisam compartilhar tenant, paciente e unidade.
- O snapshot valida tenant e paciente e registra a triagem efetivamente associada. INSERT e troca explícita de `triagem_id` são eventos separados; alterações comuns posteriores não sobrescrevem a evidência.
- A tela médica mantém o contexto do paciente visível, não usa `localStorage`, preserva os campos em erro/409 e só muda “última gravação confirmada” após resposta de sucesso.
- O manual médico e o de triagem agora explicam rascunho, confirmação, conflito, alertas de conferência e finalização.
- Prescrição e novas integrações não foram ampliadas nesta rodada.

## Comandos e resultados locais

- `python3 scripts/check-csharp10-compatibility.py` — passou.
- `python3 -m unittest discover -s scripts/tests -v` — 11 testes passaram na execução inicial.
- `python3 scripts/generate-scrpt-completo.py` — passou e regenerou artefatos/hashes.
- `python3 scripts/validate-scrpt-completo.py` — passou, cobertura de objetos 100%.
- `dotnet restore/build/test` — não executado: SDK ausente e download bloqueado por HTTP 403.
- PostgreSQL clean/replay/upgrade/equivalência — não executado: `psql`/Docker ausentes.
- E2E e screenshots reais — não executados, pois API/Web não podem ser compiladas neste executor. Não foram usadas imagens antigas ou mockups como evidência.

## Gates obrigatórios antes de homologar

Executar no CI: restore; builds Debug e Release; xUnit; clean install; replay; legacy upgrade com duplicidades e sobreposições; equivalência de schemas; testes PostgreSQL de snapshot INSERT/UPDATE, ausência de triagem, vínculo divergente, concorrência, repetição e rollback; E2E autenticado de recepção/enfermagem/médico; revogação/troca de contexto; desktop/mobile/teclado/console e screenshots da aplicação real. Um PR desta rodada não equivale a homologação nem autoriza merge automático.
