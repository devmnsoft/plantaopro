# R4-B6 — Governança SaaS: fonte única de escopo, vigência efetiva de contrato e TRIAL consistente

Data: 2026-10-06 · Branch: `main` · Base: `b3d057d` (R4-GATE) · Banco: `plantaopro_test`

## 1. Escopo e decisões

O item B6 pede: escopos bem definidos; fim do "SUPER_ADMIN por nome"; decisão de acesso
composta (sessão + usuário + vínculo + tenant + vigência + módulo/permissão + unidade/recurso);
upgrade/downgrade sem perda de dados; revogação valendo em sessão aberta; TRIAL consistente;
e o princípio **"contrato ausente ou expirado nunca é ilimitado"**.

Decisões adotadas nesta rodada:

1. **Perfis globais com fonte única.** Os quatro códigos
   (`ADMIN_GLOBAL`, `ADMINISTRADOR_GLOBAL`, `SUPER_ADMIN`, `SUPER_ADMINISTRADOR`) agora vivem
   apenas em `GlobalAdminProfiles`. Todo ponto de leitura/escrita usa a lista compilada
   (`SqlInList`) — nenhum `.cs` fora da fonte carrega o literal, e nenhuma view carrega alias
   `SUPER_ADMIN`. Classificação é case-insensitive e ignora espaços (`IsGlobalProfile`).
   Distinção documentada: `RoleCatalog.IsGlobal` classifica o *escopo do catálogo de papéis*
   (GLOBAL/HYBRID, inclui SUPORTE/COMERCIAL); `GlobalAdminProfiles` define quem administra o SaaS.
2. **Vigência por relógio, sem job.** O predicado canônico `ModuleContractVigencia.EffectivePredicate`
   é aplicado nos dois pontos de leitura (claims em `Data.LoadModulesAsync` e política efetiva em
   `EffectivePermissionService.TestarAsync`): contrato ausente não vale; `desativado_em` tem
   precedência sobre `status`; ATIVO futuro ainda não vale; AGENDADO com início vencido vale
   desde já (ativação preguiçosa); SUSPENSO/BLOQUEADO não valem. Não há expiração automática:
   se o estado físico mudou, claims antigos sobrevivem até revogação de sessão (S5) ou próximo login.
3. **TRIAL é um estado comercial, não um modo debug.** TRIAL vigente opera (uso/limites);
   TRIAL vencida bloqueia 403 com registro de bloqueio `ASSINATURA_TRIAL_VENCIDA` e mensagem
   citando o período experimental; ATIVA vencida continua registrando `ASSINATURA_VENCIDA`
   (âncora de string preservada, verificada por contrato em outra suíte).
4. **Uma assinatura ativa por cliente, mesmo sob concorrência.** Criação em transação com
   advisory lock por cliente; violação do índice parcial único vira 409 (não 500). Reativar
   uma SUSPENSA quando já existe ATIVA retorna 409 e deixa a original intacta.
5. **Desativar tenant/module revoga sessões na mesma operação** (motivo `MODULO_DESATIVADO`),
   preservando o id no upsert e auditando histórico — downgrade imediato sem perder dados.

## 2. Implementação

| Ponto | Arquivo | Mudança |
|---|---|---|
| S1 | `backend/PlantaoPro.CrossCutting/GlobalAdminProfiles.cs` | **novo** — fonte única: 4 códigos distintos, `SqlInList` compilada, `IsGlobalProfile` |
| S1 | `backend/PlantaoPro.Api/SecurityAdministrationServices.cs`, `AuthenticationSessionServices.cs`, `Data.cs` | 6 consultas SQL migradas para `GlobalAdminProfiles.SqlInList` (validação de sessão, teste de permissão efetiva, perfis atribuíveis, proteção do último super admin) |
| S1 | `backend/PlantaoPro.Web/Views/Ajuda/Index.cshtml`, `Views/Ajuda/PrimeirosPassos.cshtml` | alias `SUPER_ADMIN` substituído por `GlobalAdminProfiles.IsGlobalProfile` |
| S2 | `backend/PlantaoPro.CrossCutting/ModuleContractVigencia.cs` | **novo** — `EffectivePredicate` (constante SQL única da vigência) |
| S2 | `backend/PlantaoPro.Api/Data.cs` (`LoadModulesAsync`), `SecurityAdministrationServices.cs` (`TestarAsync`) | aplicam a constante; cópias inline eliminadas |
| S2 | `backend/PlantaoPro.Tests/V2197ModuleContractReconciliationTests.cs` | âncora atualizada: fact 1 ancora a fonte única e veda cópias inline (o B6 mudou a fonte; o teste acompanha a decisão, não a string antiga) |
| S3 | `backend/PlantaoPro.Api/TenantServices.cs` (`AssinaturaGuardService.ObterUsoPlano`) | TRIAL vencida → `ASSINATURA_TRIAL_VENCIDA` + 403 + registro de bloqueio; estados aceitos `ATIVA \|\| TRIAL` |
| S3 | `backend/PlantaoPro.Api/Models.cs` (`UsoPlanoDto`) | novo campo `DataTrialFim` |
| S4 | `backend/PlantaoPro.Api/Controllers/SaasCommercialController.cs` | `Criar(AssinaturaComercialRequest)` em transação c/ advisory lock e 23505→409; `AlterarStatus` reescrita; `Reativar` pública com guarda de conflito |
| S5 | `backend/PlantaoPro.Api/SaasCoreServices.cs` (`ToggleTenantAsync`) | desativação revoga `auth_sessoes` na mesma tx (motivo `MODULO_DESATIVADO`); upsert preserva id; histórico em `tenant_modulos_historico` |
| — | `backend/PlantaoPro.Tests/SaasGovernancaB6Rodada4Tests.cs` | **novo** — 8 facts G1–G6 com fixture auto-limpante (seed por método + purge em `Dispose`; GUIDs isolados `b6000001-*`) |
| — | `database/migrations/2026_10_v2312_*.sql`, `2026_10_v2313_*.sql`, `database/migration-manifest.json` | migrações aditivas + entries (veja seção 3) |

## 3. Defeitos pré-existentes fechados nesta rodada (schema e compatibilidade)

### 3.1 v2312 — colunas comerciais ausentes em `plantaopro.assinaturas`

`data_inicio date`, `observacoes text`, `motivo_cancelamento text`, `data_cancelamento timestamptz`
eram lidas/escritas pelos endpoints `/api/assinaturas` (Criar/AlterarStatus) e pelo SELECT de
`ObterAssinaturaAtual`, mas nenhuma migração anterior as criava → erro 42703 em tempo de execução
sem cobertura de teste viva. Migração aditiva idempotente
(`ADD COLUMN IF NOT EXISTS`, todas nulláveis, sem tocar linhas existentes).
Aplicada em `plantaopro_test` ✓. **Aplicação em `plantaopro` (prod local) pendente para a janela
A2/F3** — o banco local de produção estava sem essas colunas.

### 3.2 v2313 — `plantaopro.planos.permite_relatorios` ausente

Ausente tanto em `plantaopro_test` quanto no `plantaopro` local (verificado por consulta ao
catálogo em 2026-10-06: count=0 nos dois). Causava 42703 em `ObterUsoPlano` e nos endpoints
comerciais de plano (listar/criar/editar também escrevem a coluna). Causa raiz na linhagem de
upgrade: a coluna existe só no CREATE TABLE da linhagem nova (no-op para tabela existente) e a
linhagem antiga só tinha `ADD COLUMN IF NOT EXISTS permite_api`. Migração aditiva idempotente
com `default false` (flag fechada em bancos existentes, mesmo padrão das colunas vizinhas).
Aplicada em `plantaopro_test` ✓; **janela A2/F3 para `plantaopro`**.

### 3.3 Npgsql 10: colunas `date` retornam `DateOnly` e o Dapper não converte para `DateTime`

Após fechar o 42703, o G3 ainda falhava com o catch-all "Não foi possível validar uso do plano
no momento.". Diagnóstico com logger capturador expôs a exceção real:

```
System.Data.DataException: Error parsing column 5 (DataFim=05/11/2026)
---> System.InvalidCastException: Object must implement IConvertible.
```

E a sonda direta confirmou: `ExecuteScalar<object>` numa coluna `date` retorna
**`System.DateOnly`** (Npgsql 10.0.2). `DateOnly` não implementa `IConvertible`, então o Dapper
(2.1.35) falha ao converter para propriedade `DateTime`. Defeito latente app-wide: o caminho
só quebra quando há linha real para mapear — nenhuma das 970 rodadas anteriores exercitava esse
mapeamento com dados.

Correção cirúrgica (sem mudar contratos de DTO/JSON): cast `::timestamp` nos 4 SELECTs do
caminho SaaS comercial, onde o tipo retornado volta a ser `DateTime`:

- `TenantServices.cs` — `ObterUsoPlano` (`data_fim`, `data_trial_fim`) e
  `ObterAssinaturaAtual` (`data_inicio`, `data_fim`, `data_trial_fim`);
- `SaasCommercialController.cs` — List e Detalhar de assinaturas (`data_inicio`, `data_fim`).

`date::timestamp` é determinístico (meia-noite da própria data, sem conversão de fuso — o fuso
do servidor é irrelevante para o cast).

**Raio-x do impacto (backlog):** o schema tem outras colunas `date` que podem ser mapeadas para
`DateTime` em outros módulos (ex.: `adm360_caixa_fechamentos.*`, `adm360_titulos_pagar.*`,
`adm360_vendas.competencia`, `assinatura_uso.competencia`). A auditoria completa fica no backlog
(seção 5); a suíte verde ×3 confirma que nenhum outro caminho cobregado regressou.

## 4. Evidência de execução

### 4.1 Matriz cenário × resultado (suíte viva contra `plantaopro_test`)

| Teste | Cenário | Resultado esperado | Observado |
|---|---|---|---|
| `G1a_PrefisGlobais_FonteUnica_CodigosDistinctos_E_ClassificacaoConsistente` | 4 códigos distintos; `SqlInList` consistente; classificação insensitive a caso/espaços; não-globais (`MEDICO`, `SUPERADMIN`, vazio/null) negados | 10 asserções | ✅ passa |
| `G1b_PrefisGlobais_SomenteFonteUnicaCarregaLiteralEmCodigo` | nenhum `.cs` fora da fonte carrega `'SUPER_ADMINISTRADOR'`; nenhuma view `.cshtml` carrega `SUPER_ADMIN`; as 2 views migradas usam `IsGlobalProfile` | varredura limpa sobre toda a árvore backend+web | ✅ passa |
| `G2_VigenciaContrato_ModuloNaoCore_DecidePorContratoEfetivo` | módulo não-core decide só pelo contrato efetivo: sem contrato / desativado / AGENDADO futuro → `MODULE_NOT_CONTRACTED`; AGENDADO dentro do prazo / ATIVO vigente → `ALLOWED` | 5 cenários corretos (ausente ≠ ilimitado) | ✅ passa |
| `G3_Trial_ValidoOpera_E_VencidoBloqueiaComRegistro` | TRIAL vigente opera (status `TRIAL`, pode cadastrar médico); TRIAL vencida → 403 + mensagem com "experimental" + bloqueio `ASSINATURA_TRIAL_VENCIDA`; ATIVA vencida → 403 + `ASSINATURA_VENCIDA` | estados e registros exatos | ✅ passa |
| `G4a_CorridaCriacao_ExatamenteUmaAssinaturaAtivaPorCliente` | duas `Criar` concorrentes: exatamente uma 200, a outra 400/409; no final **uma** linha ativa por cliente | sem duplicidade sob race | ✅ passa |
| `G4b_ReativarComConflito_Retorna409_E_AtivaOriginalIntacta` | reativar SUSPENSA com ATIVA existente → 409; ATIVA original intacta; a SUSPENSA permanece SUSPENSA | conflito explícito, sem efeito colateral | ✅ passa |
| `G5_RevogacaoAdministrativa_SessaoAbertaViraInvalidaComMotivo` | sessão aberta válida do próprio usuário torna-se inválida após revogação administrativa, com motivo `REVOGACAO_ADMINISTRATIVA` persistido | revogação vale em sessão aberta | ✅ passa |
| `G6_DowngradeImediato_DesativaContrato_E_RevogaSessaoAberta` | desativar módulo → contrato `BLOQUEADO` (`desativado_em` preenchido, `habilitado=false`) + sessão do tenant revogada (`MODULO_DESATIVADO`) + histórico ≥ 1; re-habilitar restaura `ATIVO`/`habilitado` | downgrade imediato sem perda, reversível | ✅ passa |

Nota metodológica: a fixture semeia por método e purga no `Dispose` (transações determinísticas),
então o banco compartilhado não acumula estado entre execuções da suíte; GUIDs fixos isolados
(`b6000001-*`) evitam colisão com outras classes que rodam em paralelo.

### 4.2 Suíte canônica integral

Três execuções consecutivas de `dotnet test backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj
--nologo -v q` contra `plantaopro_test`:

| Rodada | Resultado | Duração |
|---|---|---|
| 1 | **978/978 aprovado**, 0 falhas | ~11 s |
| 2 | **978/978 aprovado**, 0 falhas | ~11 s |
| 3 | **978/978 aprovado**, 0 falhas | ~11 s |

Baseline no GATE: 970/970. Diferença **+8** = exatamente os novos facts B6.

### 4.3 Diagnóstico de identidade pós-suíte

Consulta idêntica à do tool de upgrade (`MISSING_ADMIN_ROLE_LINK`) voltou ao baseline histórico:
count=1 (apenas o usuário legado "Teste WP-A1", defeito de dado já catalogado — não de schema).
Os usuários/tenants/clientes B6 foram purgados; nenhum resíduo novo deixado pelo fixture.

## 5. Classificação e limitações

**Classificação B6:**

| Item | Classificação |
|---|---|
| Governança (S1–S5) + migrações v2312/v2313 + correção DateOnly | **APROVADO** — evidência de execução completa (8/8 B6 + 978×3), pronto tecnicamente |
| Aceite funcional do fluxo comercial MNSOFT (IIS real, clientes reais, operacionais) | pendente de homologação — separado desta rodada, por critério do GATE |
| Aplicação de v2312/v2313 no `plantaopro` (prod local) | **PENDENTE JANELA A2/F3** (banco local verificado sem ambas as colunas em 2026-10-06) |

**Limitações honestas (backlog):**

1. **Auditoria DateOnly completa (Npgsql 10).** Esta rodada cobre só o caminho SaaS comercial.
   Colunas `date` de outros módulos (adm360_*, assinatura_uso, faturas com `data_pagamento`)
   precisam da mesma revisão antes de depender delas em homologação viva. Prioridade alta para
   os módulos do bloco B7/B8/fiscal, que tocam essas tabelas.
2. **Assimetria AGENDADO na decisão de solicitações.** `ModuleContractingService.DecideAsync`
   (:94) valida dependências com regra mais estrita que `EffectivePredicate`: só conta contrato
   `habilitado=true` E `status='ATIVO'` (AGENDADO não libera dependência — intenção declarada em
   comentário inline). Como o predicado efetivo trata AGENDADO com início vencido como válido,
   um módulo cujo contrato ficou fisicamente `AGENDADO` (ativação preguiçosa) **passe o controle
   de acesso mas não libere aprovação de dependências novas**. Harmonizar ou documentar formalmente.
3. **EXPIRADO e renovação.** A máquina de status comercial testada cobre ATIVA/SUSPENSA/CANCELADA/
   TRIAL; exibição de estado EXPIRADO e renovação via upsert da mesma assinatura não têm teste
   vivo nesta rodada.
4. **ClientesAtivos sem datas.** Endpoint lista clientes ativos sem expor vigência — útil, mas
   insuficiente para decisão comercial; enriquecer com `data_fim`/trial.
5. **Flake intermittent catalogado.** `Administrativo360DocumentosXmlB1Tests.Divergente_MesmaChave_OutroConteudo_FalhaComMensagemDeErro`
   falhou **uma vez** sob carga paralela (esperava 1 falha de divergência, recebeu 0), passou em
   execução isolada e nas 3 rodadas integrais seguintes. Não há evidência de causa no B6
   (passa sempre depois dele); manter observação — se repetir, investigar contaminação de estado
   entre classes paralelas no repositório de documentos XML.
6. **Sem promessa de ausência absoluta de bugs.** A suíte cobre os cenários acima contra
   `plantaopro_test`; caminhos sem teste vivo (item 1) continuam sujeitos a defeito latente do
   mesmo padrão.

**Fora do escopo B6 (estado geral da entrega, sem mudança nesta rodada):** P2/IA segue
BLOQUEADO por credencial real (sem chave, homologação externa não é declarada); emissão fiscal
segue BLOQUEADA por decisão comercial. Push do repositório continua condicionado a decisão
explícita.
