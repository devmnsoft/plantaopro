# WP-A1 — Contrato JSON do BFF, guard de tenant bloqueado e landing do médico

- **Data**: 2026-10-04 (rodada 2) · **Baseline**: `82b8c26` · **Commit**: `207eeab`
- **Escopo**: consumidores de API em `/bff/*` recebem sempre status HTTP adequado + envelope JSON; cliente bloqueado barra inclusive o BFF; médico sem módulo contratado cai no Meu Dia após o login.

## 1. Contrato entregue (comportamento verificado em integração)

Caminho | Situação | Resposta observada
---|---|---
`GET/POST /bff/*` | sem sessão | `401` `{"status":401,"reason":"SESSAO_EXPIRADA",...}` — sem chamada à API operacional, sem HTML
`/bff/*` | sessão válida | encaminhado à API com `Authorization: Bearer <jwt da sessão>`; corpo e status do upstream preservados (JSON)
`/bff/*` | JWT expirado/revogado pela API (`401`) | envelope JSON repassando o motivo — nunca redirect silencioso para HTML
`/bff/*` | upstream responde `3xx` | mapeado para JSON com `reason` de redirecionamento (sem corpo 3xx cru)
`/bff/*` | upstream responde HTML com `>=400` | convertido em envelope JSON (consumidor não recebe página de erro)
`/bff/*` | POST | antiforgery validado no servidor; método/corpo preservados até a API
`/bff/*` | cliente bloqueado (`cliente_status != ATIVO`) | `403` `{"status":403,"reason":"CLIENTE_BLOQUEADO",...}` — **status HTTP 403** e **nenhuma chamada à API** (verificado por contagem no stub)
Páginas renderizadas | módulo não contratado | `302` `Account/AccessDenied?module=...&reason=MODULO_NAO_CONTRATADO` (contrato de navegação mantido)
Login de médico | sem `MEDICO_AREA` contratado | `302 /MeuDia` (módulo core) — nunca AccessDenied logo após autenticar
Login de médico | com `MEDICO_AREA` contratado | `302 /MedicoArea/Index`

Detalhamento técnico: `BffContracts` (envelope/razões/mapeamento), `OperationBffController` + `AgendaBffController` (proxy com `SocketsHttpHandler{AllowAutoRedirect=false}`), `SaasRouteGuardFilter` (bloqueio de tenant antes de qualquer outro tratamento; exceções `Account/Ajuda/MinhaAssinatura/Lgpd`).

## 2. Causas raiz identificadas e corrigidas

1. **.NET 10: `SignInAsync` não atualiza `HttpContext.User` dentro da mesma requisição.**
   Sonda instrumentando o fluxo de login mostrou, logo após o sign-in: principal local com 2 claims de módulo + `access_catalog_version=v2149`, mas `HttpContext.User` ainda anônimo (0 módulos). Consequência: `IsModuleEnabled` caía no ramo legado pré-v2149 ("tudo habilitado salvo BI_AVANCADO") e o fallback de landing nunca disparava. Correção: atribuição explícita `HttpContext.User = principal;` após cada `SignInAsync` (`AccountController.LoginAsync` e RefreshContext) — idempotente em qualquer ambiente.
2. **Guard devolvia HTTP 200 para cliente bloqueado em endpoint JSON** (`JsonResult` sem `StatusCode`). Envelope trazia `"status":403`, mas o status da linha era 200. Correção: `StatusCode = 403` na resposta.
3. **Infraestrutura de teste** (era ela, não o produto, que causava parte das falhas da rodada anterior):
   - `StubApiHandler.Reset()` trocava apenas o responder e acumulava `Requests` entre testes;
   - o kit de helpers chamava `Reset()` *depois* de instalar o responder customizado, apagando-o;
   - antiforgery: token gerado anônimo não valida em request autenticada (uid diferente) — `TestSigninController` agora emite o token no contexto já autenticado e devolve no body JSON;
   - query vazia (`?jwt=`) não faz bind para parâmetro com padrão no .NET 10 — adicionado flag explícito `semJwt=true` para sessões autenticadas sem JWT.
4. **Roteamento (comportamento pré-existente do baseline, documentado)**: URL sem ação (`/MedicoArea`, `/Treinamento`, `/Renovacoes`, `/HospitalArea`) dá 404 porque a rota convencional usa `{controller=Account}/{action=Login}` — ação padrão `Login` não existe nesses controllers. `Agenda` funciona por rota literal `[HttpGet("/agenda")]`. URLs canônicas geradas pela aplicação são `/Controller/Index`. → Pendência registrada para o Bloco B: conferir se links de menu usam a forma canônica.

## 3. Testes

- 15 novos testes de integração Web (série `WpA1*`, coleção serializada `web-bff`): contrato BFF (sem sessão, sessão válida com Bearer, token revogado, redirects do upstream, HTML→JSON, POST com antiforgery, método preservado, cliente bloqueado não chama a API...) + landing/guarda (medico c/sem área contratada, permissão negada com motivo).
- Instrumentação temporária usada no diagnóstico (`/__test/dump`, sondas de sessão) permaneceu apenas o endpoint de dump (Testing/Development); sondas de sessão removidas antes do commit.
- Suíte completa: **830/830 aprovados, 0 falhas** — ver `wpa1-suite-830-saida.log`.

## 4. Limites e pendências

- `TestSigninController` responde apenas em Development/Testing (fora disso, 404) — não faz parte da superfície de produção.
- 404 das URLs "raiz" de controllers sem rota literal (item 2.4) permanece como comportamento do baseline; decisão sobre normalização fica para o Bloco B/design.
- Persistência de screenshots no browser segue item incerto da rodada (janela invisível); a ser retestado no Bloco B.
