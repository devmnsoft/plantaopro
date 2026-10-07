# Rodada 5 — Baseline de diagnóstico (2026-10-07)

Estado inicial do ciclo "PLANTÃOPro — Jornada SaaS", antes de qualquer mudança da rodada.

## 1. Ponto de partida

| Item | Estado verificado |
|---|---|
| Git | `HEAD = main = origin/main = 0ad2617` (fim da Rodada 4 — `R4-J`), workspace limpo |
| Web dev | no ar (pid 48900; https://localhost:52977 / http 52976) |
| API dev | parada (subida sob demanda, banco `plantaopro_test`) |
| Banco | Postgres 18; `plantaopro_test` (dev/teste) e `plantaopro` (só leitura); migrations aplicadas até `v2321` (fiscal parâmetros/pré-emissão) |
| Contas demo | `docs/usuarios-teste.md` — @plantaopro.local (superadmin, admin.clinica, medico, recepcao, financeiro) e @santacasa-demo.example (gestor, medico, consulta) |

## 2. A1 — Inventário de funcionalidades (resumo)

Inventário integral em [`d5a1-inventario-funcionalidades.md`](d5a1-inventario-funcionalidades.md)
(~825 ações em ~125 controladores web; auditoria read-only sobre o HEAD).

Contagens por categoria:

| Categoria | Ações |
|---|---:|
| FUNCIONAL_VERIFICADA (pipeline real + teste em banco real) | 249 |
| IMPLEMENTADA_SEM_HOMOLOGACAO (pipeline real, homologação pendente) | 211 |
| SOMENTE_APRESENTACAO (estático / fetch morto / empty-state honesto) | ≈ 302 |
| PARCIAL | 13 |
| BLOQUEADA_INTEGRACAO | 1 |

Descobertas que orientam os blocos B/C desta rodada:

- **Eixo assinatura do cliente é fachada**: `MinhaAssinatura/Faturas|Uso|Limites|Upgrade|Downgrade|
  Cancelamento` hardcoded ou com form sem handler; os endpoints self-service reais
  (`SelfServiceSaasController` L389–437 → `SelfServiceServices.CriarSolicitacao*`) existem sem UI ligada.
- **Cadastro público (`/Cadastro/Confirmar`) dá sucesso sem provisionar**: o POST não chama a API;
  `FinalizarCadastroAsync` (SelfServiceServices L216+) está desconectado do fluxo web.
- **Sem proxy `/api/*` no Web**: `fetch('/api/…')` feita no navegador (BI, Operações) é morta; o padrão
  funcional do app é BFF server-side.
- **Dados voláteis**: `CommercialDemoService`, `B2BCommercialOpsServices` e Piloto-checklist vivem em
  dicionários em memória (perdidos no restart IIS).
- Os módulos operacionais do tenant (Saúde 360, Administrativo360, plantões/escalas/financeiro/agenda,
  jornada, portal de módulos) têm pipeline real Web→API→PostgreSQL.

## 3. A2 — Fiscal e segurança (estado verificado no banco `plantaopro_test`)

Cadeia canônica v2149 (confirmada por leitura + query):

- Login (`Data.cs`) monta roles a partir de `usuarios_perfis` × `perfis` filtrados pelo escopo do tenant.
- Claims de permissão = union(grants permitidos dos perfis, `usuario_permissoes_especiais`) − negações,
  normalizadas: `upper(replace(coalesce(p.codigo, ms.codigo||'.'||ac.codigo), ':', '.'))`.
- Módulos = `tenant_modulos` ATIVO ∩ vigência do contrato (`ModuleContractVigencia.EffectivePredicate`).
- Web `HasPermission` (AccessServices L103–122): CommonModules → sempre; módulos de administração de
  tenant × IsTenantAdmin → bypass; senão exige claim do módulo **e** claim `{MOD}.{AÇÃO}` ou `{MOD}.*`.

Lacunas encontradas (alvo do incremento `R5-A2`):

1. **`ADMINISTRADOR_CLIENTE` sem os verbos genéricos ADM360**: faltam `CRIAR, EDITAR, CONFIGURAR, REABRIR,
   CONFIRMAR, CANCELAR, CONFERIR` (verificados nos grants do perfil; o ADMINISTRADOR global tem os 27
   verbos genéricos SO_ADMIN_*). Consequência: admin do cliente bloqueado nas escritas fiscais mapeadas
   por nome próprio (ex.: `SalvarConfiguracao` cai no fallback "VER" no guard).
2. **Guard resolve ação por nome genérico** (`ResolvePermissionAction` em `SaasRouteGuardFilter.cs` L196):
   nomes próprios fiscais (`SalvarNota`, `MarcarPronta`, `Emitir`, `CancelarNota`…) caem em VER → POSTs
   liberados por quem só lê, ou bloqueados sem motivo correto. Auditor continua somente-leitura por sorte,
   não por regra explícita.
3. **ENVIANDO sem comprovação**: `Adm360FiscalWebController.Emitir` transita para ENVIANDO mesmo sem
   conector de transmissão registrado (nenhum provedor externo integrado — P1).
4. **Self-service provisiona admin sem nenhum grant ADM360**: o seed de `GarantirPerfilAdminClienteAsync`
   (SelfServiceServices L550–559) não inclui códigos ADM360 — novo cliente sequer abre a leitura fiscal.
5. Credenciais fiscais lidas pelo **host Web** (`Fiscal:Credenciais:{ref}`) — migram para a API junto do
   serviço fiscal centralizado (retifica delta F2 da Rodada 4, aceitável nesta rodada).

Anotações de contexto (não impedem a rodada):

- Demo tenant Santa Casa tem contrato apenas de `ADM360`, `AUDITORIA`, `ESCALAS` — **sem PLANTOES**:
  gestor fica bloqueado em `/Plantoes` apesar de ter claims `PLANTOES.*` (matriz de aceite usa conta/
  tenant adequados por módulo).
- Perfis `CONSULTA_CLIENTE` e `GESTOR_OPERACIONAL` têm zero grants ADM360 (GET fiscal já bloqueado hoje —
  comportamento mantido e documentado).
- Tabela `tenant_modulos` usa `ativado_em`/`desativado_em` (não tem `data_inicio`/`data_fim`).

## 4. B4/B5/C7 — Comercial, provisionamento e onboarding (estado verificado)

- **TRIAL existe** como estado de assinatura (R4-B6): `TenantServices.ObterUsoPlanoAsync` bloqueia TRIAL
  vencida (`ASSINATURA_TRIAL_VENCIDA`); TRIAL vale para limites enquanto não vence; default comercial B2B
  é `PILOTO_TRIAL`. Porém o **self-service cria `assinaturas` diretamente `ATIVA` (+1 mês, dia_vencimento =
  dia UTC atual)** — decisão de TRIAL vs ATIVA no self-service é pendência comercial (B4).
- **Fluxo self-service (transação única, tudo-ou-nada)**: validações (CNPJ único; e-mail único GLOBAL entre
  tenants — quirk documentado) → solicitação FINALIZADO → `tenants` → `clientes` → `assinaturas` → admin +
  vínculo → seed de perfil (sem ADM360, ver A2-4) → white label padrão → onboarding com **11 etapas
  obrigatórias hardcoded** → LGPD → pagamento inicial ABERTO +7 dias crédito manual.
- **Sem gravação em `tenant_modulos` no cadastro**: novo tenant nasce com módulo core apenas; o único
  caminho real de contratar módulo é o portal (`MinhaAssinatura/Modulos*` → `solicitacoes_modulos` →
  `tenant_modulos`), já funcional ponta a ponta.
- Pagamento inicial segue manual (ABERTO, 7 dias, crédito manual) — alinhado à contratação B2B; não há
  provedor de cobrança integrado (sem stripe/pagarme/pix; webhooks existentes são BI outbound c/ HMAC).

## 5. Ambiente e convenções de verificação

- Ciclo build/restart Web: matar 52976/52977 → `dotnet build PlantaoPro.Web.csproj -c Debug` → rodar em
  background apontando `ConnectionStrings__Default` para `plantaopro_test`; Razor dessincronizado ⇒
  `--no-incremental`; API dev roda com `ConnectionStrings__Default` (e, a partir do A2,
  `Fiscal__Credenciais__{ref}`).
- psql: `'C:\Program Files\PostgreSQL\18\bin\psql.exe'`, `PGPASSWORD=123456`, schema `plantaopro`,
  1 query/comando, `ilike` ASCII, datas com cast `::text`.
- PowerShell 5.1 (sem `&&`); LangVersion 10; console corrompe acentos (artefatos em ASCII no terminal).

## 6. Plano da rodada (blocos)

| Bloco | Escopo | Status nesta data |
|---|---|---|
| A1 | Inventário + operações viram comandos reais | inventário pronto (este commit) |
| A2 | Fiscal honesto: autorização por ação, transmissor por conector, credenciais na API | design fechado, implementação próxima (`R5-A2`) |
| A3 | Defeitos visuais pela causa (z-index/overflow/ordem CSS), tokens | pendente |
| GATE A | auth/sessão, tenant/perfil, comandos críticos sem sucesso fictício, navegação, testes | pendente |
| B4 | Matriz comercial canônica (plano→módulos→capacidades→limites→adicionais) | pendente |
| B5 | Provisionamento idempotente self-service + admin, convites com expiração/uso único | pendente |
| B6 | Telas comerciais viram operações reais; cobrança implementada a partir dos requisitos | pendente |
| C7/C8 | Onboarding adaptado ao contratado; jornadas de ativação por módulo com progresso persistido | pendente |
| D9–D12 | Homologação de ADM360 (fiscal até transmissão real c/ conector), Plantões, Saúde 360, IA contextual | pendente |
| E13/E14 | Navegação gerada do catálogo efetivo; operação/suporte IIS (logs correlacionados, jobs, backup) | pendente |
| Entrega | 12 itens + roteiro de aceite (matriz obrigatória) + backlog priorizado | pendente |
