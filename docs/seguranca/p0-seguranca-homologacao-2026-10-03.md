# Relatório de Segurança P0 — Homologação PlantãoPro

- **Data**: 2026-10-03 (UTC)
- **Baseline**: commit `35c4382` (branch `main` local, não pushada) + correções desta rodada de homologação (edits não commitados + migrações v2305/v2306/v2307)
- **Suíte de referência**: **794/794 green** (`evidencias-a360/wps3b-suite.log`, 8 s) — evolução da rodada: baseline 775/775 → pós-WP-S1 776/776 → pós-WP-S2 779/779 → pós-WP-S3 (camada IA, +15 fatos) 794/794, revalidado após o ajuste do catálogo do guard (Anexo B)

## 1. Método

Revisão estática arquivo a arquivo dos alvos P0 (autorização, sessão/CSRF, banco, entradas/arquivos, segredos) **complementada por evidência executada**: suíte de testes xUnit contra PostgreSQL 18 real, probes `psql` documentados em scripts (`evidencias-a360/_wps1_migcheck.ps1`, `_wps1_apply_migs.ps1`, `_wps1_apikeys.ps1`) e regeneração verificável do script consolidado (`scrpt_completo.sql`, sha256 `c6b86683f4ffbf75ef41aee7e17f12684e614d0d376acbe226ea101fba07388b`).

**Critério de classificação** (cada achado recebe exatamente um):

| Classe | Significado |
|---|---|
| **VULNERABILIDADE** | Caminho de exploração concreto confirmado no código (com condição de exploração descrita) |
| **LACUNA** | Proteção ausente ou incompleta numa superfície real; exploração depende de circunstância descrita |
| **HIPÓTESE** | Risco plausível que **não pôde ser verificado** porque o ambiente (produção/rede) não está acessível nesta homologação |
| **PROTEÇÃO EXISTENTE** | Controle conferido e confirmado funcionando (evidência citada) |

Nenhum segredo (senha, token, chave) é reproduzido neste documento — apenas caminhos de arquivo.

## 2. Resumo dos achados

| ID | Classe | Gravidade | Alvo | Estado |
|---|---|---|---|---|
| S-01 | Vulnerabilidade → corrigida | Alta | Acesso clínico por autoria/e-mail em vez de vínculo médico | Corrigido + suíte verde |
| S-02 | Vulnerabilidade → corrigida | Alta | CSRF ausente nos BFFs Operation/Agenda | Corrigido (atributo + token + wrapper global) |
| S-03 | Lacuna → corrigida | Alta | Bypass de imutabilidade Adm360 via GUC setável por qualquer usuário | Corrigida (migração v2305: papel restrito + helper) |
| S-04 | Lacuna → corrigida | Média | Uploads XML lidos integralmente antes do limite de tamanho | Corrigida (limite antes da leitura, Web + API) |
| S-05 | Lacuna → corrigida | Média | Testes podiam escrever no banco operacional (fallback silencioso) | Corrigida (resolver com fonte única + origem auditável) |
| S-06 | Lacuna → corrigida | Média | Condição de corrida no swap de plano principal + duplicidade histórica | Corrigida (transação atômica + índice parcial único, v2306) |
| S-07 | Lacuna → corrigida | Média | API pública `/api/public/v1/*` anônima sem nenhuma credencial | Corrigida (X-Api-Key validada contra hash persistido) |
| S-08 | Hipótese | Baixa-Média | Rede/TLS/`pg_hba` e privilégios do banco em produção | Pendente — sem acesso ao ambiente produtivo |
| S-09 | Hipótese | Baixa-Média | Separação de papéis de banco em produção (migrations vs operacional) | Documentada como risco de implantação (v2305 exige CREATEROLE) |
| S-10 | Proteção existente | — | Kestrel 128 KB, cookies HttpOnly/Lax/SameAsRequest/sliding, JWT + revogação dinâmica de sessão, guards, constraint de agenda, SQL parametrizado, `DtdProcessing.Ignore`, clamp de lookup | Verificado (seção 4) |
| S-11 | Observação | Baixa | Credenciais de dev versionadas (`TestDatabase.cs`, `launchSettings.json`) | Conhecidas e documentadas; não publicadas aqui |
| S-15 | Lacuna → corrigida | Média | Controllers fora do catálogo do guard SaaS (`Pendencias`, `AssistenteIa`) | Corrigido (Anexo B — WP-S3) |

## 3. Detalhes por achado

### S-01 — Acesso clínico por autoria/e-mail (VULNERABILIDADE → corrigida)

- **Símbolo/rota**: `Saude360ClinicalService.ObterAsync` / `BuildListSql` — motor canônico das rotas clínicas de Saúde 360 (consultas, prescrições, repasses médicos, planos).
- **Arquivo**: `backend/PlantaoPro.Api/Saude360ClinicalService.cs`
- **Evidência pré-correção**: o filtro de escopo para profissionais incluía *autoria do registro* e *correspondência de e-mail* como regras amplas, além do vínculo médico — i.e., um médico cujo e-mail coincidisse com o de um registro em `plantaopro.medicos`, ou que fosse o autor de um registro, enxergava dados de profissional de quem não era vinculado.
- **Severidade**: Alta (vazamento intra-tenant de dados clínicos; UUID/campos de auditoria não substituem autorização).
- **Condição de exploração**: usuário logado com perfil clínico no tenant; e-mail ou autoria coincidentes com registros de outro profissional.
- **Impacto**: leitura não autorizada de prontuário/consultas/prescrições entre profissionais do mesmo tenant.
- **Correção**: escopo clínico exclusivamente pelo vínculo explícito `medicos.usuario_id`:
  - `ObterAsync`: L116–120 — subquery única `t.medico_id in (select m.id from plantaopro.medicos m where m.reg_status='A' and m.usuario_id=@uid and (@tenantId is null or m.cliente_id=@tenantId))`;
  - `BuildListSql` (listas consultas/prescrições/repassesMedicos): L582–583 — mesma regra.
  - Autoria (`created_by`) e e-mail saíram da regra de autorização.
- **Teste de regressão**: jornada clínica completa `Saude360JornadaSFixesRegressionTests` (+ contrato `FechamentoDemonstrativosV2169ContractTests`, que ancora `m.usuario_id=@uid` também no escopo de contestações de pagamento). Suíte verde.

### S-02 — CSRF ausente nos BFFs (VULNERABILIDADE → corrigida)

- **Símbolo/rota**: `OperationBffController` (todas as ações state-changing `bff/operacao/*`) e `AgendaBffController` (`bff/agenda/*`), além de fetches genéricos de outras telas que partilhavam o cookie de sessão.
- **Arquivos**: `backend/PlantaoPro.Web/Controllers/OperationBffController.cs` L11; `backend/PlantaoPro.Web/Controllers/AgendaBffController.cs` L14; `backend/PlantaoPro.Web/Views/Shared/_Layout.cshtml` L26 e L75+.
- **Evidência pré-correção**: nenhum dos dois controllers validava antiforgery; o site não expunha token para JS, então `fetch` POST sem token era aceito. O `SameSite=Lax` dos cookies reduzia o alcance (mitigação parcial, não controle dedicado).
- **Severidade**: Alta (execução de operações state-changing sob a sessão da vítima).
- **Condição de exploração**: vítima autenticada carrega página/controlador em domínio atacante; requisição cross-site submetida com o cookie.
- **Correção aplicada (defesa em camadas)**:
  1. `[AutoValidateAntiforgeryToken]` nos dois controllers BFF (qualquer POST/PUT/PATCH/DELETE sem token válido → 400);
  2. `<meta name="antiforgery-token">` no layout (L26), populado via `@inject IAntiforgery` + `GetAndStoreTokens(Context).RequestToken`;
  3. Wrapper global de `window.fetch` (L75+): para POST/PUT/PATCH/DELETE same-origin injeta o header `RequestVerificationToken` automaticamente; formulários continuam com field `__RequestVerificationToken`. Nenhuma navegação GET é afetada.
- **Teste de regressão**: compilação força o atributo em toda ação; suíte verde (776/776). Cobrança runtime pela jornada no navegador é coberta pelas capturas/roteiro (entrega 6, WP-S4). Nota honesta: o wrapper é aplicado em todo layout autenticado; rotas que renderizam fora do layout (login) só usam métodos sem estado.

### S-03 — Bypass de imutabilidade Adm360 via GUC (LACUNA → corrigida)

- **Símbolo/objeto**: triggers `fn_adm360_evento_imutavel` / `fn_adm360_vale_evento_progresso` sobre `plantaopro.adm360_eventos` — protegem imutabilidade do histórico Adm360.
- **Arquivos**: `database/migrations/2026_10_v2305_administrativo360_imutabilidade_por_papel.sql`; funções recriadas em `plantaopro`.
- **Evidência pré-correção**: o bypass exigia apenas `plantao.bypass_imutabilidade_adm360='on'`. Probe empírico em PG 18 provou que **qualquer usuário não-superuser** pode `SET` esse GUC (session-local) e passar nos triggers. `GRANT/REVOKE SET ON PARAMETER ... FROM PUBLIC` foi testado e **não** bloqueia o `SET` — ou seja, privilégio de parâmetro não resolvia.
- **Severidade**: Alta (quem se conecta ao schema pode alterar/apagar eventos imutáveis do histórico financeiro).
- **Condição de exploração**: qualquer role com `CONNECT` + permissões de escrita nas tabelas; `SET plantao.bypass_imutabilidade_adm360='on'` na própria sessão.
- **Impacto**: quebra da trilha auditável do Administrativo 360 (fechamento, estornos, pagamentos).
- **Correção**: v2305 cria o papel restrito `plantaopro_maintenance` (`NOLOGIN NOINHERIT NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS`) e o helper `plantaopro.fn_adm360_bypass_habilitado()` — o bypass só vale se o GUC estiver `'on'` **E** o `session_user` for superuser **ou** membro do papel de manutenção; triggers recriadas com as mesmas mensagens de erro de antes (SqlState `P0001`, texto "imut…") para não quebrar contratos existentes.
- **Evidência executada** (`_wps1_apply_migs.ps1`): papel existe com `canlogin=0 super=0`; helper retorna `f` com GUC off (mesmo para superuser) e `t` com GUC on em sessão superuser.
- **Teste de regressão**: `Administrativo360EventosImutabilidadeTests` (mantém assert de `P0001` + mensagem); suíte verde com os objetos novos ativos no banco.
- **Resíduo**: em dev/teste os testes conectam como superuser local, então o bypass continua funcional por conveniência de ambiente; em produção o papel de manutenção deve ser concedido apenas ao job/service que precisa (ver S-09).

### S-04 — Upload XML processado antes do limite (LACUNA → corrigida)

- **Símbolo/rota**: upload de documentos fiscais — Web `Adm360DocumentosXmlWebController` (formulário multipart + campo de conteúdo) e API `Adm360DocumentosXmlController` (`POST api/adm360/documentos-xml`).
- **Arquivos**: `backend/PlantaoPro.Web/Controllers/Adm360DocumentosXmlWebController.cs` L71–74, L94; `backend/PlantaoPro.Api/Controllers/Adm360DocumentosXmlController.cs` L76–80.
- **Evidência pré-correção**: o conteúdo era lido/parseado integralmente e só depois validado — payload grande consumia memória/CPU antes do reject.
- **Severidade**: Média (DoS local por upload; sem exposição a payload binário arbitrário).
- **Condição de exploração**: usuário autenticado envia XML gigante (vários MB+) em form ou body JSON.
- **Correção**: limite verificado **antes** de ler/parsear: arquivo ≤ 2 MiB; string ≤ 2.000.000 de caracteres (mesmas constantes espelhadas em Web e API). Parser mantém `DtdProcessing.Ignore` (`DocumentosXmlRepository.cs` L240–246) — proteção contra entidades externas mantida.
- **Teste de regressão**: `Administrativo360DocumentosXmlB1Tests` (green). **Limitação declarada**: não há fixtures reais ABRASF no repositório (só 4 XML sintéticos em `backend/PlantaoPro.Tests/Fixtures/Administrativo360Xml/`); a validação de NF-e/NFC-e/NFS-e ABRASF reais fica registrada como limitação, sem modificar fixtures para caber no parser.

### S-05 — Fallback de banco operacional em testes (LACUNA → corrigida)

- **Símbolo**: `TestDatabase.ConnectionString` — fonte única de connection string de todos os testes de integração.
- **Arquivo**: `backend/PlantaoPro.Tests/Infrastructure/TestDatabase.cs` L28–38.
- **Evidência pré-correção**: o resolver caía na conexão operacional da aplicação (`PLANTAOPRO_CONNECTION_STRING` / `ConnectionStrings__Default`) quando a variável de teste não estava definida — risco de a suíte escrever no banco que a aplicação usa em execução.
- **Severidade**: Média (corrupção/mistura de dados operacionais em CI/dev; não é vazamento externo).
- **Correção**: apenas `PLANTAOPRO_TEST_CONNECTION` ou padrão local `plantaopro_test` (explicitamente descartável); conexões operacionais são **deliberadamente ignoradas**; a origem usada é exposta em `TestDatabase.Origem` para evidência de execução. Sem fallback silencioso: banco fora do ar = erro de conexão explícito.
- **Teste de regressão**: `TestDatabaseResolutionTests` (3 fatos: precedência da variável, ignorância das operacionais, padrão local).

### S-06 — Condição de corrida no plano principal (LACUNA → corrigida)

- **Símbolo/objeto**: `Saude360ClinicalService.CriarAsync` (troca de plano principal do paciente) + tabela `plantaopro.plano_saude_pacientes`.
- **Arquivos**: `backend/PlantaoPro.Api/Saude360ClinicalService.cs` L158–183; `database/migrations/2026_10_v2306_saude360_plano_principal_unico.sql`.
- **Evidência pré-correção**: a troca (despromover atual + promover novo) corria em duas escritas sem transação atômica — duas operações concorrentes podiam deixar 2 (ou 0) planos principais; além disso, dados históricos do banco local continham duplicidades (a dedup da v2306 promoveu/demoveu 2 linhas no primeiro apply; segundo apply foi idempotente, `UPDATE 0`).
- **Severidade**: Média (integridade clínica/financeira do vínculo paciente×plano).
- **Correção**: (1) swap atômico em uma única transação — UPDATE com lock de linha + INSERT; conflito (exclusão/unique) → rollback + HTTP 409 "plano principal … alterado em outra operação"; (2) índice parcial único `ux_plano_saude_pacientes_principal` sobre `(paciente_id)` WHERE `principal=TRUE AND reg_status='A' AND paciente_id IS NOT NULL`, com dedup one-shot na mesma migração. Confere-se que `BuildUpdate` não traz `principal` no body (sem 2º vetor de mass assignment).
- **Teste de regressão**: jornada Saúde 360 (`Saude360JornadaSFixesRegressionTests`) + suíte verde com o índice ativo.

### S-07 — API pública anônima sem credencial (LACUNA → corrigida)

- **Símbolo/rota**: `Fase6BiIntegracoesController` — `GET/POST api/public/v1/*` (webhooks/eventos, `{recurso}`, `{recurso}/{id}`, agendamentos, contas a receber, autorizações de convênios).
- **Arquivos**: `backend/PlantaoPro.Api/Controllers/Fase6BiIntegracoesController.cs` L55–87 (gate `ChavePublicaValida`); `backend/PlantaoPro.Api/Fase6BiIntegracoesServices.cs` L112 (`ValidarChavePublicaAsync`).
- **Evidência pré-correção**: seis rotas `[AllowAnonymous]` sem qualquer credencial — consumível por qualquer pessoa com a URL.
- **Severidade**: Média (hoje os payloads são vazios/não sensíveis — impacto baixo neste momento; o risco é estrutural quando a API expuser dados reais).
- **Correção**: todo `/api/public/v1/*` agora exige header `X-Api-Key` validado contra o hash SHA-256 persistido em `plantaopro.api_keys` — status `ATIVA` e `expira_em` não passado; o vínculo de tenant vem da própria chave (rota anônima, sem sessão); registro best-effort de `ultimo_uso_em`; sem chave/chave inválida/expirada → `401` JSON.
- **Pendências declaradas** (backlog, até haver dados reais): escopo por recurso (coluna `escopos` já existe na criação de chaves) e rate limiting.
- **Teste de regressão**: novo `Fase6ApiPublicaChaveTests` (2 fatos: ausente/branca/desconhecida/revogada → nega; ATIVA valida + devolve tenant + registra uso; expirada → nega).

### S-08 — Rede/TLS/`pg_hba` do banco em produção (HIPÓTESE)

- **Por quê não é concluída nesta homologação**: o ambiente acessível é o PostgreSQL 18 local em `127.0.0.1:5432`; o banco produtivo (topologia, rede, TLS, `pg_hba.conf`) não foi alcançável, então **não se declara "protegido" sem verificação real**.
- **Checklist a executar quando houver acesso**: `pg_hba.conf` (scram-sha-256, reject por padrão, hosts limitados), TLS obrigatório nas conexões remotas, firewall/rede entre app e banco, dump/backup criptografados.
- **Grau**: Baixa-Média (depende do ambiente real).

### S-09 — Papéis de banco em produção e requisito CREATEROLE (HIPÓTESE/risco de implantação)

- **Fato verificado**: em dev o app e os testes conectam como superuser local (`postgres`) — aceitável apenas para ambiente descartável.
- **Requisito novo introduzido por v2305**: criar `plantaopro_maintenance` exige superuser ou CREATEROLE no usuário de migração; **nenhuma migração anterior tem precedente de `CREATE ROLE`** — risco explícito de instalação em produção onde o migrator é um papel comum.
- **Plano recomendado (não executado, sem ambiente produtivo)**: usuário de migrations separado (com CREATEROLE durante o deploy) do usuário operacional (sem superuser, sem login interativo); bypass concedido ao operacional somente se o fluxo de manutenção assim exigir; avaliar RLS **como defesa extra**, desde que nenhuma política incompleta crie falsa sensação (uma política parcial que falhe filtrar vaza).
- **Grau**: Baixa-Média.

### S-11 — Segredos versionados (observação)

- Conexão local com senha em `backend/PlantaoPro.Tests/Infrastructure/TestDatabase.cs` (padrão dev, documentada acima) e `launchSettings.json` (fora do scan do contrato de segredos — que cobre `appsettings*.json` e `.cs/.cshtml` por padrões tipo `eyJ…`).
- Senhas de seed dos usuários de teste estão persistidas **hash bcrypt** no banco, não em texto claro no código.
- Chaves de provedores de IA ainda não existem (camada entra em WP-S3); quando existirem, seguirão a regra `Ai__Providers__{Groq,Gemini,DeepSeek}__ApiKey` somente em env/user-secrets — nunca em código.
- **Política**: segredos expostos no desenvolvimento devem ser rotacionados ao saírem do ambiente descartável; nenhum valor é publicado neste relatório.

## 4. Proteções existentes verificadas (PROTEÇÃO EXISTENTE)

Controles conferidos e confirmados com evidência (arquivo:linha):

| Controle | Evidência |
|---|---|
| Limite total de headers HTTP 128 KB (Web e API — evita 431 com JWT de catálogo de permissões ~41 KB) | `backend/PlantaoPro.Web/Program.cs` L10; `backend/PlantaoPro.Api/Program.cs` L30 (comente a justificativa F5) |
| Cookie de sessão: `HttpOnly`, `SecurePolicy=SameAsRequest`, `SameSite=Lax`, sliding expiration 8 h | `backend/PlantaoPro.Web/Program.cs` L42–47 |
| Validação JWT completa (issuer/audience/lifetime/assinatura) + **revogação dinâmica**: `OnTokenValidated` consulta `IAuthenticationSessionService.ValidateAsync` a cada request (sessão revogada/bloqueada cai na hora) | `backend/PlantaoPro.Api/Program.cs` L107–127 |
| Autorização por policy/role no Web + guard global de rota SaaS (`SaasRouteGuardFilter` registrado globalmente) | `backend/PlantaoPro.Web/Program.cs` L12–15, L50–54 |
| SQL parametrizado (Dapper) em toda a camada; identificadores dinâmicos vêm de allowlist fixa de `tableKey` (não de input livre) | Camadas de repositório (p.ex. `Saude360ClinicalService.BuildListSql`); contrato `FechamentoDemonstrativosV2169ContractTests` ancora ausência de `update plantaopro.pagamentos set valor` fora do fluxo |
| Parser XML com `DtdProcessing.Ignore` (entidades externas) | `backend/PlantaoPro.Infrastructure/.../DocumentosXmlRepository.cs` L240–246 |
| Conflito de agenda em concorrência: contagem transacional + constraint de exclusão no banco → exatamente 1 sucesso, o outro recebe 409 | Jornada S de Saúde 360 (suíte) |
| Guards de máquina de estados nas transições clínicas (motor canônico `AcaoAsync`) | `Saude360ClinicalService.AcaoAsync` |
| BFF operation bloqueia segmentos com `..` no identificador de rota | `OperationBffController` |
| Lookup clínico limitado por allowlist + clamp de paginação 1–100 + escopo por tenant | `Saude360ClinicalService` (listas) |
| Trilha de auditoria: logs de permissão/aceitos-negados escritos por serviço dedicado | `Administrativo360ErrosNegocioTipadosTests` (asserts sobre `permissao_logs` e `acessos_negados_log`) |
| Contrato anti-regressão de segredos/UI: scan de `appsettings*.json` + `.cs/.cshtml` por padrões de segredo/token embutido, `href="#"`, `alert(`, `@model dynamic` | Suíte de contract tests (passou com os novos arquivos desta rodada) |

## 5. O que fica declarado (honestidade de homologação)

1. **Protegido por prova executada**: S-01…S-07 (correções + testes verdes) e a seção 4.
2. **Não verificado por falta de ambiente** (não declarado como protegido): S-08 e S-09 — dependem de acesso ao banco/topologia de produção.
3. **Limitações funcionais declaradas** (não são de segurança mas afetam o veredito): fixtures ABRASF reais ausentes (S-04); mobile avaliado estaticamente (WP seguinte); IA sem chave real retorna estado explícito "não configurado" (WP-S3 — semântica validada por probe funcional real, Anexo B; integração com provedor real fica NAO EXECUTADA até haver chave).
4. **Riscos residuais conhecidos**: JWT simétrico local; cookies sem `Secure` forçado em HTTP puro (mitigado por `SameAsRequest` + proxy em produção); API pública sem escopo/rate limit até ter dados reais (S-07).

## 6. Regressões desta rodada (evidência executada)

| Evidência | Resultado |
|---|---|
| Baseline `35c4382` | 775/775 green (19 s) — `evidencias-a360/baseline-suite.log` |
| Pós-código (WP-S1) | 774/774 green (15 s) — `evidencias-a360/wps1-suite.log` (delta explicado: rewrite `TestDatabaseResolutionTests` 4→3 fatos) |
| Pós-migrações ativas no banco + chave da API pública | **776/776 green (11 s)** — `evidencias-a360/wps1-suite-final2.log` |
| Pós-WP-S2 (Meu Dia / Central de Ações) | **779/779 green (11 s)** — `evidencias-a360/wps2-suite.log` (delta: +1 out-of-range no scoping, +2 classificação de leitura do corpo) |
| v2305 aplicada (rol + helper) | `canlogin=0 super=0`; helper `f` com GUC off / `t` com GUC on + superuser — `_wps1_apply_migs.ps1` |
| v2306 aplicada (dedup + índice) | 1º apply `UPDATE 2` (duplicidades reais removidas); 2º apply idempotente `UPDATE 0` |
| Cleanup de probe | papel `guc_probe` removido (exigia REVOKE de privilégio no parâmetro GUC — registrado como comportamento empírico de PG 18) |
| Script consolidado (pós-v2305/v2306) | `scrpt_completo.sql` regenerado — sha256 `c6b86683…388b` |
| v2307 aplicada (camada IA) | cria `ai_config` + `ai_usos`; reaplicação idempotente — `evidencias-a360/v2307-apply.log`; manifest v2.21.6 (ordem 76) |
| Pós-WP-S3 (camada IA, +15 fatos) | **794/794 green** — `evidencias-a360/wps3-suite.log` |
| Pós-adjustment do catálogo do guard (S-15) | **794/794 green (8 s)** — `evidencias-a360/wps3b-suite.log` |
| Smoke funcional camada IA (stack no ar, 18 passos, 3 perfis) | cadeia completa verde — `evidencias-a360/wps3-web-smoke.log` (Anexo B) |
| Script consolidado (pós-v2307) | `scrpt_completo.sql` regenerado — sha256 `b925e687…deff1` |

---

## Anexo A - WP-S2 (Meu Dia / Central de Acoes) - achados e correcoes

### S-12 - Pagina sem registros declarava total zero em vez do total real (corrigido)
- **Simbolo/rota**: `ProductivityActionServices.ListAsync` - API `GET api/produtividade` e `GET api/produtividade/meu-dia` (Web `/Pendencias`, `/MeuDia`).
- **Evidencia**: o `Total` saia de `count(*) over()::int as TotalRows` da consulta da pagina. Com a pagina solicitada fora do intervalo nao ha nenhuma linha, e o fallback do codigo (`rows.FirstOrDefault()?.TotalRows ?? 0`) zeria o total -> a UI afirmava "0 registro(s)" enquanto os filtros tinham itens.
- **Gravidade**: media (integridade de dados exibidos; nao e vazamento).
- **Condição de exploracao**: qualquer filtro com itens que quebre a ultima pagina disponivel (ex.: `page=99&pageSize=2`).
- **Correcao**: o CTE filtrado virou const compartilhada `FilteredCte` (`backend/PlantaoPro.Api/ProductivityActionServices.cs` L126) entre a consulta paginada (L140) e a contagem dedicada `countSql` (L145); quando `rows.Count == 0`, o servico executa `ExecuteScalarAsync<int>(countSql)` sobre o MESMO CTE (L154-160) e reporta o total real.
- **Teste de regressao**: `ProductivityActionScopingTests.PaginaForaDoIntervalo_MostraListaVaziaMasMantemTotalRealDoFiltro` (pagina fora do intervalo repete o total/pagina total da pagina 1; tenant vazio continua legitimamente zero).
- **UI**: `backend/PlantaoPro.Web/Views/Pendencias/Index.cshtml` L49 - painel "Esta pagina nao tem registros" com total real + link "Ir para a ultima pagina" (estado vazio classico mantido para total 0).

### S-13 - Timeout durante a leitura do corpo escapava como erro interno (corrigido)
- **Simbolo/rota**: `ProductivityWebService.GetAsync` (BFF Web -> API), usado por `/MeuDia` e `/Pendencias`.
- **Evidencia**: somente o `client.SendAsync` estava dentro do try/catch classificatorio; o `ReadFromJsonAsync` do corpo acontecia depois do bloco de catches, de modo que `OperationCanceledException` disparada pelo `HttpClient.Timeout` durante a leitura do corpo escapava para fora do metodo -> 500.
- **Gravidade**: baixa/media (classificacao de falha; mascara degradacao real do BFF->API como erro interno).
- **Condição de exploracao**: API lenta na transferencia do corpo (headers ok, corpo demorado) com `PlantaoProApi:LoginTimeoutSeconds` excedido.
- **Correcao**: um unico try cobre envio E leitura integral do corpo (`backend/PlantaoPro.Web/Services/ProductivityWebService.cs` L43 em diante; comentario L49); os catches externos classificam por ordem: cancelamento do chamador -> `CANCELADO` (L81-85), timeout -> `TIMEOUT` (L87-92), transporte -> `TRANSPORTE` (L93-97). `JsonException` segue virando `RESPOSTA_INVALIDA`.
- **Teste de regressao**: `MeuDiaWps2RegressionTests.TimeoutDuranteLeituraDoCorpo_ClassificaComoTimeoutEmVezDeFalhar` (handler fake devolve headers imediatamente e falha na primeira leitura do corpo com `TaskCanceledException` - o que o transporte real faz quando o tempo limite do cliente vence no meio da resposta -> `ErrorKind == "TIMEOUT"` e mensagem amigavel) + `MeuDiaWps2RegressionTests.CancelamentoDoChamadorDuranteLeituraDoCorpo_ClassificaComoCancelado` (corpo pendente + cancelamento do chamador -> `ErrorKind == "CANCELADO"`).

### S-14 - Filtro morto `UnitId` no contrato de produtividade (removido por decisa)
- **Evidencia**: `ProductivityQuery.UnitId` existia no contrato (modelo API, viewmodel Web, `BuildQuery`, URLs da view `Pendencias`), mas nenhum ramo de `DerivedSql` consumia o parametro: nao ha coluna canonica de unidade nas origens das acoes (dois conceitos distintos: `plantaopro.unidades` dos work items Operacao 360 x `hospitais` dos plantoes) e a UI nunca ofereceu o seletor. Parametro sempre verdadeiro = contrato enganoso.
- **Decisao**: removido de `ProductivityActionModels.cs` (API), `ProductivityViewModels.cs` (Web), `ProductivityWebService.BuildQuery` e `Views/Pendencias/Index.cshtml`. Os `UnitId` com coluna real (UnitPortal, MinhaCentral, WorkItems) foram mantidos.
- **Gravidade**: informativa (correcao contratual; sem impacto de seguranca direta).
- **Regressao**: compilacao limpa + suíte de scoping/paginacao sem alteracao de ancoras.

### Suíte pós-WP-S2
| Evidencia | Resultado |
|---|---|
| Suíte completa `-c Debug` | **779/779 green (11 s)** - `evidencias-a360/wps2-suite.log` (baseline 775 -> 779: +1 fact de pagina fora do intervalo no scoping, +2 fatos de classificacao de leitura do corpo) |

---

## Anexo B - WP-S3 (Camada IA canonica + catalogo do guard) - achados e evidencia

### S-15 - Controllers ausentes do catalogo do guard SaaS (lacuna -> corrigida)
- **Simbolo/rota**: `SaasRouteGuardFilter.ControllerModules` — filtro de ação global fail-closed que mapeia controller autenticado → módulo SaaS. Rotas afetadas: `/Pendencias*` (lacuna pré-existente, bloqueava o Meu Dia do WP-S2) e `/AssistenteIa*` (superfície nova desta rodada).
- **Arquivo**: `backend/PlantaoPro.Web/Services/Security/SaasRouteGuardFilter.cs` (dicionário L10–L71).
- **Evidência**: controller autenticado SEM entrada no dicionário recebe 302 para `AccessDenied?reason=CATALOGO_NAO_CONFIGURADO` antes da ação. Sem as entradas, `/Pendencias` e `/AssistenteIa` eram inacessíveis para QUALQUER usuário logado (o motivo "módulo não contratado" mascarava um bug de configuração); o log Web registrou repetidamente `fail: ... Controller autenticado sem módulo no catálogo SaaS.`.
- **Gravidade**: média (disponibilidade fail-closed — sem vazamento de dados, mas duas telas 100% fora de serviço e jornada de homologação bloqueada).
- **Condição de exploração**: qualquer usuário autenticado navegando para `/Pendencias` ou `/AssistenteIa` (URL direta ou menu).
- **Correção**: entradas registradas no catálogo — `["AssistenteIa"] = "CONFIGURACOES"` (L29) e `["Pendencias"] = "MEU_DIA"` (L45). Racional: MEU_DIA é módulo comum (mesmo escopo de `MeuDia`, todo usuário autenticado); CONFIGURACOES é módulo de administração do tenant E o controller continua exigindo `[Authorize(Roles = "ADMINISTRADOR_GLOBAL,ADMINISTRADOR,ADMINISTRADOR_CLIENTE")]` (`AssistenteIaController.cs` L16), que falha ANTES do guard para perfis clínicos — médico segue recebendo `AccessDenied?ReturnUrl=/AssistenteIa` (verificado no probe, passo 17).
- **Regressão**: `V2153CockpitSaasContractTests` (contrato do guard, ancoras por token) segue verde; suíte **794/794** pós-adjustment (`wps3b-suite.log`); evidência funcional nos passos 2/4/17/18 abaixo.

### Observação - `OperationBff` também fora do catálogo (PRÉ-EXISTENTE, vai para o backlog — fora do escopo desta correção)
- O proxy BFF `bff/operacao/*` (consumido pelo JS do centro de notificações) não tem entrada em `ControllerModules`: o fetch segue o 302, recebe HTML de AccessDenied e falha silenciosamente ao parsear JSON. Não afeta as jornadas da camada IA (que POSTam direto em endpoints Web com CSRF). Recomendação: registrar `OperationBff` no catálogo (módulo comum) ou excluí-lo do guard como BFF de API, com teste de regressão do centro de notificações.

### Evidência executada - migração v2307 (camada IA, idempotente)
| Item | Resultado |
|---|---|
| Apply inicial | cria `plantaopro.ai_config` + `plantaopro.ai_usos` com CHECKs de tarefa/provedor/limites — `evidencias-a360/v2307-apply.log` |
| Reaplicação | idempotente (`IF NOT EXISTS`; sem erro, sem duplicidade) |
| Manifesto | `install-manifest.json` schemaVersion **v2.21.6**, ordem 76 |
| Sem FK de tenant | segue a convenção das tabelas operacionais (ex.: `tenant_modulos`) |

### Evidência executada - smoke funcional da camada IA (stack no ar, 3 perfis)
Probe `evidencias-a360/probe-wps3-web.ps1` contra `https://localhost:52977` (Web) → `https://localhost:51977` (API), com logins reais, cookies e tokens CSRF extraídos das páginas. Log oficial: `evidencias-a360/wps3-web-smoke.log`.

| # | Passo | Perfil | Resultado observado |
|---|---|---|---|
| 1 | Login admin | admin.clinica (Clínica Modelo) | 302 `/` |
| 2 | GET `/Pendencias` | admin | 200 — painel IA presente (CSS/JS/URL do resumo) |
| 3 | GET `/Pendencias/ResumoIa` (antes de habilitar) | admin | `VAZIO` — pré-checagem determinística precede o estado de configuração |
| 4 | GET `/AssistenteIa` | admin | 200 — 2 formulários de tarefa + aviso da chave mestra do servidor |
| 5 | POST `Salvar/MEU_DIA_RESUMO` (groq, sem chave) | admin | 302 + alerta de sucesso na página |
| 6 | GET `ResumoIa` (apos habilitar) | admin | `VAZIO` consistente (regra determinística independe de habilitação) |
| 7 | POST `TestarConexao` groq | admin | `NAO_CONFIGURADO` — "Nenhuma chave configurada para este provedor (nem do cliente, nem do servidor)." |
| 8 | Login gestor | gestor@santacasa (tenant Santa Casa, ADM360 ativo) | 302 `/` |
| 9 | GET `Detalhes` de cotação real | gestor | 200 — painel IA com URL da análise (gates ADM360 + escopo de tenant corretos) |
| 10 | POST `{id}/AnaliseIa` SEM token CSRF | gestor | 400 (antiforgery exigido) |
| 11 | POST `{id}/AnaliseIa` com CSRF (tarefa desabilitada) | gestor | `NAO_HABILITADO` |
| 12 | POST `{guid inexistente}/AnaliseIa` com CSRF | gestor | `NAO_ENCONTRADO` — "Registro não encontrado neste cliente." (validação do ID relacionado precede a execução) |
| 13 | GET `/AssistenteIa` | gestor | 200 |
| 14 | POST `Salvar/COTACAO_ANALISE` (groq, sem chave) | gestor | 302 + alerta de sucesso |
| 15 | POST `{id}/AnaliseIa` (habilitada, sem chave) | gestor | `NAO_CONFIGURADO` com `provedor=groq` |
| 16 | Login médico | medico (perfil clínico) | 302 `/` |
| 17 | GET `/AssistenteIa` | médico | `AccessDenied?ReturnUrl=/AssistenteIa` (gate de papéis antes do guard) |
| 18 | GET `/Pendencias/ResumoIa` | médico | 200 `VAZIO` (leitura permitida via módulo comum MEU_DIA) |

Persistência verificada por `psql` (`q-aiconfig.sql`): linhas `ai_config` dos dois tenants com `habilitada=t`, `provedor='groq'`, `api_key_cifrada IS NULL` e `chave_mascara` vazia, limites gravados (`cota_mensal_usos=50`, `timeout_s=20`) — nenhuma chave inteira exposta em campo algum; `ai_usos` sem novos registros da rodada (nenhuma chamada LLM ocorreu — todas as respostas saíram das classes determinísticas; a cota só conta sucesso).

### Suíte pós-WP-S3 e pós-adjustment do guard
| Evidência | Resultado |
|---|---|
| Suíte completa `-c Debug` (camada IA, +15 fatos em `AiWps3RegressionTests`) | **794/794 green** — `evidencias-a360/wps3-suite.log` |
| Suíte após ajuste do catálogo do guard (S-15) | **794/794 green (8 s)** — `evidencias-a360/wps3b-suite.log` |
