# R5-D9 — Homologação viva da jornada de onboarding + acesso a módulos contratados

Data: 2026-10-09. Ambiente: hosts locais (API 51976 / Web 52976) apontando para o banco
`plantaopro_test` (user-secrets Api e Web). Sem browser automation: toda prova foi feita por
scripts PowerShell (`d9_run_iniciar.ps1`, `d9_final.ps1`, `d9_pages3.ps1`) contra HTTP real, com
verificação persistida via psql.

## 1. Fixture de demonstracao montado por rotas reais (nada inserido "na mao" exceto reparo abaixo)

- Assinatura canônica **cancelada e recriada** pelas rotas administrativas reais (histórico CANCELADA preservado):
  - `0cc4eb63-4bce-4482-93a6-0c8e7eba1be3` ATIVA, plano DEMO_LOCAL (R$ 0,00), vigência 2026-10-09 → 2027-10-09,
    **`tenant_id` preenchido** (era o defeito do item 3.2).
  - Anterior `1559b3e7-…` CANCELADA via `POST api/assinaturas/{id}/cancelar`.
- **Reparo de fixture do banco de teste** (única escrita direta em banco, documentada): o seed legado do tenant
  demo não ligava o cliente ao tenant — `update tenants set cliente_id='d3f6584c-…7501' where id='d3f6584c-…7502'
  and cliente_id is null`. Sem isso nenhuma rota real conseguia resolver o vínculo.
- `POST api/modulos/{id}/habilitar-tenant` (superadmin JWT): PLANTOES (`07ea2a74-…d070`) e SAUDE360
  (`066c1581-…008e`), origem CONTRATO. ADM360 já era efetivo via linha LEGADO/ATIVO (predicado de vigência
  `ModuleContractVigencia.EffectivePredicate`), sem necessidade de habilitar. Lista efetiva conferida via psql.

## 2. Jornada materializada avaliada de ponta a ponta (HTTP real)

- Gestor demo `gestor@santacasa-demo.example` (tenant `…7502`, cliente `…7501`) logado pelo fluxo form
  real do Web; `POST /Onboarding/Iniciar` → **302 → /Onboarding/Index**; checklist renderizado com os
  **12 passos do contrato** (probe DOM pelos títulos exatos do catálogo + consulta SQL):

| Grupo | Códigos materializados | Situação (derivada de dados) |
|---|---|---|
| Núcleo | ONB_EMPRESA_DADOS, ONB_EQUIPE_CONVIDAR, ONB_EQUIPE_ACESSO, ONB_IDENTIDADE | pendentes — a avaliação aponta a falta real ("Falta no cadastro: e-mail corporativo") |
| PLANTOES | ONB_PL_CRIAR, ONB_PL_PUBLICAR, ONB_PL_ESCALA | CONCLUIDO (dados reais de plantão existem no tenant demo) |
| SAUDE360 | ONB_SD_UNIDADE, ONB_SD_PACIENTE, ONB_SD_TRIAGEM | PENDENTE (sem dados clínicos reais — honesto) |
| ADM360 | ONB_AD_OPERACAO | CONCLUIDO (operação administrativa persistida) |
| Geral | ONB_REVISAO | PENDENTE (gate: todas as obrigatórias atendidas) |

- `POST /Onboarding/Reavaliar` → 302 com mensagem de sucesso e **sem falso erro** (ver defeito 3.4).
- Smoke de acesso com os novos claims CONTRATO (re-login): `/Plantoes/Index` **200**; `/MinhaAssinatura/Index`
  **200**; `/Pacientes/Index` e `/Triagem/Index` → honesto `AccessDenied?module=PACIENTES|TRIAGEM&reason=MODULO_NAO_CONTRATADO`.

## 3. Defeitos encontrados e corrigidos nesta homologação

1. **ONBOARDING invisível para o gestor (guard eterno em MODULO_NAO_CONTRATADO)** — o módulo lógico
   `ONBOARDING` do guard não existe em `modulos_sistema`, então nem `habilitar-tenant` resolveria. Corrigido
   adicionando `"ONBOARDING"` às listas de administração/core nas 4 famílias de acesso: Web
   `PermissionService.TenantAdministrationModules` + `ModuleAccessService.CoreModules` (`AccessServices.cs`),
   API `SecurityAccessServices.TenantAdministrationModules` e `EffectivePermissionService.CoreModules`.
   Perfis comuns continuam `PERMISSAO_NEGADA` (asscrito em teste).
2. **`tenant_id` órfão em `POST api/assinaturas` (Criar)** — o admin criava assinatura sem vínculo com o
   tenant, deixando todo o gate de contrato cego. Agora resolve `tenants.cliente_id` do cliente e persiste
   `tenant_id` no insert (fica null honestamente se não houver vínculo; o kernel B5 já gravava certo).
   Arquivo: `SaasCommercialController.cs`.
3. **Rotas raiz do OnboardingController** — sem template de controller, `[HttpPost("Iniciar")]` etc. mapeavam
   para `/Iniciar` na raiz. Adicionado `[Route("Onboarding")]`; ao fazer isso, ações `[HttpGet]` sem template
   **saem do match convencional (404 em /Onboarding/Index)** — comportamento MVC confirmado empiricamente.
   Corrigido no padrão do app (`ConvitesEquipeController`): templates explícitos em todas as ações
   (`Index` = `""`+`"Index"`, `NovoCliente`, `Sucesso`).
4. **"Formato inesperado" falso após POST bem-sucedido** — `PostJornadaAsync` tipava o payload como `string`,
   mas a jornada devolve `data` em lista; um POST 200 legítimo disparava banner de erro. Passa a ler
   `JsonElement` (aceita lista ou objeto sem inventar contrato).
5. **Perfil ADMINISTRADOR_CLIENTE com seed incompleto de permissões** — grants legados de PLANTOES tinham
   5/31 ações (sem `PLANTOES.VER`) e SAUDE360 tinha zero, então `/Plantoes` caía em `PERMISSAO_NEGADA` mesmo
   com contrato ATIVO (o guard exige permissão ∧ módulo). Migration **v2334**
   (`2026_10_v2334_d9_perfil_admin_modulos_jornada.sql`, aditiva/idempotente) segue o precedente do suite
   ADM360 (semeado completo com 39 ações): concede o conjunto canônico de PLANTOES+SAUDE360 ao perfil global
   ADMINISTRADOR_CLIENTE — 57 grants inseridos; o gate de contrato continua obrigatório e o cliente pode
   revogar via override (`usuario_permissoes_especiais`).

## 4. Evidências pós-correção (medidas, não afirmadas)

- `LOGIN=302`; `GET /Onboarding/Index=200`; banners de erro ausentes; `REAVALIAR_STATUS=302` + mensagem
  `Checklist reavaliado…` presente.
- `ETAPA_NUCLEO/PLANTOES/SAUDE360/ADM360/REVISAO=True` (títulos exatos do catálogo presentes no HTML).
- SQL: 12 linhas `ONB_*` em `tenant_onboarding_checklist` do onboarding `f524628d-…` (master `EM_ANDAMENTO`),
  status conforme tabela da seção 2.
- `PAGE_/Plantoes/Index=200`, `PAGE_/MinhaAssinatura/Index=200`, `/Pacientes|/Triagem=302→AccessDenied(MODULO_NAO_CONTRATADO)`.
- **Suíte completa: 1171/1171 aprovados** (inclui os 2 novos `OnboardingD9GestorAcessoTests`: gestor sem claim
  ONBOARDING renderiza o checklist; perfil MEDICO negado com motivo `PERMISSAO_NEGADA`).

## 5. Pendências/observações registradas (sem maquiagem)

- v2334 aplicada **somente** em `plantaopro_test`; banco principal `plantaopro` aguarda upgrade externo pela
  sequência completa de migrations (v2323→v2334). Assim como v2330–v2333, a aplicação manual desta vez não foi
  registrada em `schema_migrations` (a tabela não traz as execuções manuais das anteriores no banco de teste) —
  registrar no upgrade formal.
- **E13 (backlog): granularidade do catálogo Saúde 360** — o contrato `SAUDE360` não libera controllers dos
  códigos finos (`PACIENTES`, `TRIAGEM`, `CONSULTAS`…); ou `habilitar-tenant` expande submódulos, ou o guard
  passa a mapear esses controllers para `SAUDE360`. Decisão de produto pendente.
- Etapa de IA do onboarding continua sem código de módulo canônico (herança C7).
- A rota legível `/Onboarding/Index` agora é atributal; links antigos para `/Iniciar` na raiz deixaram de existir.
