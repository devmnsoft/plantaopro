# R5-D11/D12 — Jornadas Saúde 360 e Identidade (white-label) homologadas ao vivo + matriz final da jornada

Data: 2026-10-09. Ambiente: hosts locais (API 51976 / Web 52976) → banco `plantaopro_test`.
Probes: `d11_saude.ps1`, `d12_identidade.ps1` (HTTP real + psql). Sem escrita manual em banco nesta
rodada — toda persistência veio de rotas do produto.

## 1. D11 — Jornada Saúde 360

1. **Habilitação fina** — `POST api/modulos/eb240058-…/habilitar-tenant` (PACIENTES, superadmin) →
   habilitação `ccccc226-bc87-497f-bfb8-58ca2a1def3e`; o guard do `/Pacientes` saiu de
   `MODULO_NAO_CONTRATADO` para exigir apenas permissão.
2. **Permissões do perfil base** — edição runtime rejeita perfil `base_sistema=true`
   ("protegido pelo sistema"), então o mecanismo canônico é seed: migration **v2335**
   (`2026_10_v2335_d11_perfil_admin_modulos_finos_saude.sql`, aditiva/idempotente, precedente
   v2334) concede o conjunto completo de PACIENTES+CONSULTAS a **todos** os perfis
   `ADMINISTRADOR_CLIENTE` (global + por tenant) — `INSERT 0 124`. Re-login necessário (claims
   computados no login). `/Pacientes/Index` → **200**.
3. **Paciente real pelo BFF** — `GET /Pacientes/Create` → `POST /Pacientes/Save` →
   `POST api/pacientes`: a primeira tentativa provou a validação real do kernel (**400 "CPF
   inválido"** para CPF de enfeite); com CPF de teste algoritmico válido (`529.982.247-25`) a
   criação passou e a linha persistiu em `plantaopro.pacientes` (`Maria Teresa Andrade`,
   `4ddc830b-…a224`). Duplicidade de CPF no mesmo escopo responde **409** (observado ao repetir).
4. **Triagem real pela API** — `POST api/triagens` (JWT gestor; gate de papel atende): triagem
   `b659b72d-…17db`, risco `AMARELO`, vinculada ao paciente persistido. A página Web
   `/Triagem/Index` continua honestamente bloqueada: `AccessDenied?module=TRIAGEM&reason=MODULO_NAO_CONTRATADO`
   (catálogo não tem a linha TRIAGEM — ver E13).
5. **Correção do avaliador da jornada** (defeito encontrado): os critérios
   `PRIMEIRO_PACIENTE_CADASTRADO`/`PRIMEIRA_TRIAGEM_REALIZADA` liam apenas
   `tenant_id=@tenantId`, mas o kernel clínico (`Saude360ClinicalService`) escopa por
   `cliente_id` (coluna onde grava; `tenant_id` fica null). Uma etapa concluída por rota real
   seria negada para sempre. `OnboardingJornadaService.AvaliarCriterioAsync` passa a aceitar as
   duas chaves (`tenant_id=@tenantId or cliente_id=@clienteId`) — `clienteId` é o mesmo já
   resolvido do tenant e usado por `DADOS_EMPRESA_COMPLETOS`.

## 2. D12 — Identidade (white-label) pela rota self-service

- `PUT api/white-label/tenant/{tenantId}` (JWT gestor): validador real exigiu HEX e contraste
  WCAG AA (4.5:1) — payload `Santa Casa Digital` / `#0B5394` sobre `#FFFFFF` salvo e persistido em
  `tenant_white_label` (`on conflict (tenant_id)` idempotente).
- **Tenancy provada**: mesmo PUT para tenant de outro cliente → **403** (`ObterAtualAsync` recusa
  fora do escopo do usuário).
- Página Web `/WhiteLabel` para o gestor → `AccessDenied?module=WHITE_LABEL&reason=MODULO_NAO_CONTRATADO`
  (gating honesto; a entrega do passo é a rota de API do papel, como no convite→aceite).
- `POST /Onboarding/Reavaliar` → `ONB_IDENTIDADE` `CONCLUIDO/AUTOMATICO`
  ("Identidade visual personalizada salva").

## 3. Matriz final medida (checklist do tenant demo, via psql)

| Etapa | Status | Origem | Evidência persistida |
|---|---|---|---|
| ONB_EMPRESA_DADOS | CONCLUIDO | AUTOMATICO | Dados completos no cadastro |
| ONB_EQUIPE_CONVIDAR | CONCLUIDO | AUTOMATICO | Convite registrado (uso único, expiração) |
| ONB_EQUIPE_ACESSO | CONCLUIDO | AUTOMATICO | Convidado usou o token |
| ONB_IDENTIDADE | CONCLUIDO | AUTOMATICO | Identidade personalizada salva |
| ONB_PL_CRIAR / PUBLICAR / ESCALA | CONCLUIDO ×3 | AUTOMATICO | Grade/escala reais do tenant |
| ONB_SD_PACIENTE | CONCLUIDO | AUTOMATICO | Paciente cadastrado no tenant |
| ONB_SD_TRIAGEM | CONCLUIDO | AUTOMATICO | Triagem registrada no fluxo assistido |
| ONB_AD_OPERACAO | CONCLUIDO | AUTOMATICO | Operação administrativa lançada |
| ONB_SD_UNIDADE | **PENDENTE** | - | Nenhuma unidade cadastrada — **sem rota de escrita no produto** (E13) |
| ONB_REVISAO | **PENDENTE** | - | Gate honesto: "Faltam 1 etapa(s) obrigatoria(s): Cadastrar a unidade de atendimento" |

**10/12 CONCLUIDO por dados reais**; as duas pendências são conclusões derivadas de dados, não cliques.

## 4. Defeitos de produto registrados (backlog E13 — não maquiados aqui)

1. **Sem rota de escrita para `clinica_unidades_atendimento`** — o catálogo C7/C8 materializa
   `ONB_SD_UNIDADE`, mas nenhum controller (API ou BFF) insere nessa tabela (a lookup "unidades" é
   da tabela legada de plantões e é somente leitura). O passo obrigatório é injogável em
   self-service até existir a rota (ou remover o passo do catálogo).
2. **Granularidade do catálogo Saúde 360** — `TRIAGEM`, `AGENDAMENTOS`, `CLINICA_DASHBOARD` não
   existem em `modulos_sistema`; páginas correspondentes ficam em `MODULO_NAO_CONTRATADO` mesmo
   com contrato SAUDE360 ativo e permissão concedida.
3. **Kernel clínico grava escopo em `cliente_id` e deixa `tenant_id` NULL** em `pacientes`/`triagens`
   — padronizar a coluna canônica (o avaliador agora aceita as duas, solução de compatibilidade).
4. **Edição runtime de permissões é bloqueada para perfis `base_sistema`** — grants finos só por
   migração (v2334/v2335); decisão de produto sobre customização por tenant permanece aberta.

## 5. Build & suíte

- Migration v2335 aplicada em `plantaopro_test` (não registrada em `schema_migrations` — registrar
  no upgrade formal; banco principal `plantaopro` continua aguardando a sequência completa).
- Com os hosts derrubados: build limpo e **suíte completa 1171/1171 aprovados** após a correção do
  avaliador (uma falha isolada de `B2_AprovarUpgrade_DestinoInativo_409SemMudarNada` no 1º run foi
  flakiness de banco compartilhado em paralelismo — verde isolado e verde no run completo seguinte).
