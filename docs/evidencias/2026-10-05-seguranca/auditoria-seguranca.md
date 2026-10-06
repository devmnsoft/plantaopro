# Auditoria de segurança — homologação PlantãoPro (ADM360 + BFF Web)

- **Data**: 2026-10-05 (rodada de homologação) · **Baseline**: `67b4f91` (WP-C) sobre origem `82b8c26`
- **Escopo**: rotas por ID e IDOR, isolamento entre tenants, entidades relacionadas, downloads/exportações, inputs (CSRF/XSS/upload), logs e secrets, API pública, CORS/cookies.
- **Método**: duas varreduras exaustivas paralelas (API+Infra e Web/MVC) com verificação spot linha a linha; cada achado confirmado no código-fonte antes de classificar. Sem scanner externo — auditoria estática manual.

## Veredito geral

**Nenhum vazamento cross-tenant ativo encontrado.** 4 UPDATEs sem filtro de tenant no WHERE foram corrigidos nesta rodada (defense-in-depth: as linhas já eram validadas/travadas por id+tenant na mesma transação). Superfície anônima mínima e esperada. 2 POSTs sem antiforgery → 1 corrigido, 1 documentado (sem chamador).

---

## A. Correções aplicadas nesta rodada

| ID | Arquivo:linha | Evidência (antes) | Correção aplicada | Teste |
|---|---|---|---|---|
| SG-01 | `backend/PlantaoPro.Infrastructure/Administrativo360/ComprasRepository.cs:207` | `UPDATE plantaopro.adm360_pedido_itens SET quantidade_recebida = … WHERE id = @PedidoItemId` (sem tenant no WHERE) | Adicionado `AND tenant_id = @tenantId` (parâmetro já estava no escopo) | Suíte canônica 839/839 — fluxo de recebimento (M3) exercita esta query |
| SG-02 | `ComprasRepository.cs:216-221` | `UPDATE plantaopro.adm360_pedidos p SET situacao = CASE … WHERE id = @PedidoId` | Adicionado `AND p.tenant_id = @tenantId`; dict de parâmetros agora `{ c.PedidoId, tenantId }` | Idem — situação PARCIAL/RECEBIDO coberta pela suíte |
| SG-03 | `backend/PlantaoPro.Infrastructure/Administrativo360/QualidadeRepository.cs:134-136` | `UPDATE plantaopro.adm360_vale_eventos SET quantidade_decidida = … WHERE id = @RecebimentoItemId` | Adicionado `AND tenant_id = @tenantId` | Suíte — decisão de inspeção de retorno de consignação |
| SG-04 | `QualidadeRepository.cs:147-149` | `UPDATE plantaopro.adm360_recebimento_itens SET quantidade_decidida = … WHERE id = @RecebimentoItemId` | Adicionado `AND tenant_id = @tenantId` | Suíte — gate de recebimento A3/G5 (lote+movimento) |
| SG-05 | `backend/PlantaoPro.Web/Controllers/AccountController.cs:429` | `RefreshContext` ([HttpGet]+[HttpPost]) sem antiforgery — CSRF podia forçar re-assinatura da sessão | `[ValidateAntiForgeryToken]` no POST (GET inalterado; o filtro só valida verbos POST) | Suíte — nenhum chamador interno de RefreshContext existe hoje (grep em todo o projeto: apenas referências em `.cs`), zero regressão esperada e confirmada pelos testes de conta |

Notas de verificação pré-fix:
- Colunas `tenant_id` confirmadas nos schemas: `adm360_pedido_itens` (`database/migrations/2026_09_v2191_…sql:9`), `adm360_recebimento_itens` (v2191:12), `adm360_vale_eventos` (v2193:109), `adm360_pedidos` (FK v2191).
- UPDATEs/DELETEs de todas as demais tabelas `adm360_*` varridos (60+ ocorrências): todos os outros já filtram por `tenant_id` ou são batch de manutenção não exposto ao usuário (`Adm360TransmissaoRecovery.cs:24-31`, OK by design).
- Entidades relacionadas: o vínculo recebimento↔pedido-item valida o tenant dos dois lados antes de gravar (ex.: `ComprasRepository.cs:178-181` valida `adm360_locais` por `id AND tenant_id AND ativo`; FKs compostas `(tenant_id, …)` no schema impedem cruzamento).

## B. Itens verificados — sem alteração necessária

| Item | Evidência (arquivo:linha) | Conclusão |
|---|---|---|
| **Autorização por rota** | Todos os controllers ADM360 da API têm `[Authorize]` com role/policy de classe (`Administrativo360Controller.cs:8`, `Adm360CaixaController.cs:7`, etc.); nenhum endpoint do módulo sem auth além de health/liveness | OK |
| **Leitura de tenant (claims)** | `EffectiveAccessAuthorization.cs:39-85` lê `tenant_id`; `SecurityAdministrationServices.cs:98-99` **falha fechado** (`TENANT_CONTEXT_REQUIRED`) quando ausente; linha do usuário usa `coalesce(u.tenant_id, u.cliente_id)` (v2193/SecurityAdministrationServices.cs:88) — mesmo critério do escopo de dados (`CurrentUserService`) | Divergência de readers é inerte: sem tenant, nega |
| **Lockout de login** | Aplicado na API: `backend/PlantaoPro.Api/Data.cs:276-278, 342-358, 372` (limiar → bloqueio N min; default **ligado**; `Program.cs:34` exige flag explícita para desligar em produção) | As chaves raiz `LoginLockoutEnabled`/`LockoutMinutes` em `appsettings.json` da Web são mortas (nenhum `.cs` referencia) — proteção real está na API |
| **Honeypot do demo comercial** | `CommercialDemoWebController.cs:49-53` rejeita quando `website` não vazio; o campo é honeypot: `Views/CommercialDemoWeb/Contato.cshtml:8` `<input name="website" class="pp-hp" tabindex="-1">` | Lógica correta (anti-spam), não condição invertida |
| **SegredoReferência (contas de portal)** | Escrita: `CotacoesRepository.cs:172-189`; **leitura** `ListarContasPortalAsync` (`CotacoesRepository.cs:133-141`) NÃO seleciona `segredo_referencia`; padrão PRG — erro vai por `TempData["Error"]`, form nunca re-renderiza com o valor | Segredo só trafega no POST (HTTPS) e fica no banco; não aparece em HTML |
| **Html.Raw (XSS)** | Único uso em views: `Views/Shared/_ToastMessages.cshtml:53`, alimentado por JSON emitido com `System.Text.Json` (escapa por padrão) | BAIXO |
| **Antiforgery cobertura** | 178 `[HttpPost]` × 176 `[ValidateAntiForgeryToken]` no pre-fix; pós-fix os únicos sem o atributo: `AccountController.cs:321` (Logout — GET+POST; as 4 ocorrências nas views são links GET, nenhum form POST) | Logout-CSVF documentado como BAIXO (auto-negativo: força logout) |
| **Superfície anônima** | Login/Logout/AccessDenied/Reset (AccountController), Error, PlanosPublicos+Cadastro (SaaS), CommercialDemo (landing/simulador/contato), `__test/*` (gateado por ambiente: `NotFound()` fora de Dev/Teste) | Mínima e esperada; nenhuma página funcional interna exposta |
| **Downloads/exportações** | `RelatoriosController.cs:34,53-60`: bytes via stream `File(…)` (sem escrita em disco → sem path traversal em FS); nome = `{kind.ToLowerInvariant()}-{ts}.csv`. Em `OperacionalAsync` o `kind` é hard-coded (`:20-22`); em `ExportarOperacional` vem de campo de form mas só altera qual endpoint da **própria** API é chamado (404 se inválido) e o nome do download | BAIXO/aceito |
| **Logs** | Amostras de resposta truncadas em 400 chars: `BaseWebController.cs:17` (`MaxLogContentLength`), `:464-467`; pontos de emissão `:161,168,177,279,292,306` — envelopes de erro da própria API (mensagens/erros validação, sem PII estruturada); logins logam classe do identificador, não o valor (`AccountController.cs:417` `ClassifyIdentifier`) | Aceito; máscara adicional se um dia proxy-amos corpo de terceiros |
| **Sessão/cookies** | Framework defaults (`Program.cs` AddSession); perfil de execução sempre HTTPS local; fallback legado `Request.Cookies["jwt"]` (`BaseWebController.cs:49`) nunca emitido nesta versão | Documentado; `SecurePolicy=Always` para deploy prod (backlog) |
| **CORS** | Restrito à origem da aplicação (BFF same-site único em execução) | OK no desenho atual |

## C. Riscos residuais e recomendações (backlog pós-MVP)

| ID | Severidade | Item | Recomendação |
|---|---|---|---|
| R-1 | Média-baixa | **Idempotency keys geradas no servidor/cliente efêmero**: `Adm360OrcamentosController.cs:76` gera `Guid.NewGuid()` quando header ausente (replay ≠ mesma key); proxies Web `Administrativo360Controller.Orcamentos.cs:301` e `…EstoqueValorizacao.cs:78` enviam key nova por clique | Manter key gerada pelo cliente (storage/hidden field) por ação em andamento; mitigação atual: guards de estado (transições de situação) + diálogos `data-confirm` (WP-C) |
| R-2 | Média-baixa | **GUC de imutabilidade**: gatilhos `adm360_*` respeitam `plantao.bypass_imutabilidade_adm360` (v2305); em dev/CI a conexão de superuser `postgres` efetiva bypass | Conectar produção com role dedicada (sem superuser); registrar no runbook de deploy |
| R-3 | Baixa | **Chaves mortas** `LoginLockoutEnabled`/`LockoutMinutes` na `appsettings.json` da Web podem induzir proteção que não existe na camada certa | Remover ou mover sob `Authentication:`; lockout real é da API (ver §B) |
| R-4 | Baixa | **Logout POST sem antiforgery** (sem chamador hoje) | Adicionar token quando um form POST existir, ou remover o overload POST |
| R-5 | Baixa | **SegredoReferência em claro no banco** (`adm360_portal_contas.segredo_referencia`) | Criptografia at rest (coluna encryted) |
| R-6 | Baixa | **FecharCaixa (TOCTOU)**: extrato do cofre calculado fora da transação single-statement do INSERT (`CaixaRepository.FecharCaixaAsync`) | Consolidar no mesmo snapshot/lock |
| R-7 | Baixa | **`/bff/*` (rotas JSON) não passam pelo guard de módulo da Web** (`SaasRouteGuardFilter.cs:149-152`) — permissão delegada à API (design documentado) | Revisar na refatoração de rotas |
| R-8 | Baixa | **Session `SecurePolicy` não forçado a Always** | Definir no pipeline de produção |
| R-9 | Info | **Suíte flaky sob paralelismo**: `A3/G5 gate de recebimento` falhou 1× em execução completa e passou isolado e na re-execução (mesma linhagem do `Aceite12_XmlRepetido_NaoDuplica` já conhecido) | Isolar coleção de banco em execução serial se reaparecer |

## D. Testes

- Suíte canônica `dotnet test backend\PlantaoPro.Tests\PlantaoPro.Tests.csproj --nologo -v q` **após** as correções SG-01..05: **839/839 aprovados** (55-59 s). Uma única falha transitória no `A3/G5` na primeira execução (ver R-9); re-execução e execução isolada verdes.
- Stack reiniciada no build com as correções: API `https://localhost:51977/api/health` Healthy; Web `https://localhost:52977/Account/Login` 200 + liveness pós-init OK (2026-10-05 21:49).
- Exercícios funcionais do recebimento/inspeção (M3) da jornada WP-B cobrem os UPDATEs corrigidos com dados reais de `plantaopro_test`.

## E. Conclusão

A superfície do módulo Administrativo 360 está **aptas para homologação funcional** no plano de segurança: autenticação/autorização completas por rota, isolamento de tenant consistente (com as 4 lacunas de defense-in-depth fechadas), CSRF coberto onde há estado mutável em UI, XSS contido, superfície anônima mínima. Itens de §C são melhorias pós-MVP sem impacto conhecido em confidencialidade/integridade atual.
