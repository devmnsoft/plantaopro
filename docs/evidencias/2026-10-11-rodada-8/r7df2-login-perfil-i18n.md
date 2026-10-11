# R7-D-F2 — Login/perfil: mensagens de sessão/senha localizadas + padronização de chaves

Data: 2026-10-11 · Base: `5e42d59` (R7-D-F1). Fatia 2 do corte R7-D (escopo em
`r7d0-varredura-design-textos-i18n.md`, seção 6).

## Problema (achado R7-D0)

O fluxo de primeira/última impressão seguia com pt-BR hardcoded fora do mecanismo:

- `AccountController.cs` — 14 mensagens fixas em pt-BR (erros de login por status,
  falha de conexão/timeout, logout, "enviaremos instruções", sucesso/falha de reset de
  senha, refresh de contexto expirado/recusado);
- `Views/Account/ForgotPassword.cshtml` e `ResetPassword.cshtml` — título, eyebrow,
  headings, banners, rótulos e botões 100% literais;
- catálogo com nomes de chave em português (`Profile.Nome`, `Profile.Telefone`,
  `Profile.Notificacoes`) dentro de um padrão de grupos em inglês.

## Solução

| Peça | O que mudou | Evidência |
|---|---|---|
| `UiTexts.resx` (+ `.en/.es/.fr`) | **+34 chaves `Auth.*`** (sessão, erros de login, recuperação, reset, refresh de contexto) em paridade nos 4 idiomas; **renomeadas 3 chaves** `Profile.Nome→Profile.Name`, `Profile.Telefone→Profile.Phone`, `Profile.Notificacoes→Profile.Notifications` (valores intactos) | paridade coberta pelo teste R7-E1 + contrato novo (abaixo) |
| `AccountController.cs` | todas as 14 mensagens via `UiText.L("Auth.…")`; a mensagem com referência de correlação usa `string.Format(InvariantCulture, UiText.L("Auth.ErrorAuthFailedWithRef"), correlationId)` | contrato `AccountController_MensagensDeSessaoESenha_…` |
| `ForgotPassword.cshtml` / `ResetPassword.cshtml` | todos os textos via `UiText.L("Auth.Forgot…")`/`Auth.Reset…`; estrutura, validação e classes intactas | contrato `ViewsDeRecuperacaoESenha_…` |
| `Usuario/Index.cshtml` | 3 usos atualizados para os nomes padronizados | contrato `ChavesProfile_PadronizadasParaInglês_…` |
| Testes `R7DF2LoginPerfilI18nContratoTests.cs` (novo) | 3 fatos de contrato de fonte: uso das chaves + ausência dos 14+ literais no controller/views, renames presentes/nos 4 resx sem legados | `[números da execução]` |

## Decisões registradas

1. **Mensagens dinâmicas da API seguem passando direto** (`apiResult?.Message`): a API ainda
   não emite mensagens localizadas — registrar como dívida do lado API, fora deste corte.
2. **RefreshContext**: as duas variantes (expirado × recusa genérica) viram chaves
   separadas para permitir tradução honesta de cada causa.
3. **Renomear ≠ traduzir**: os valores pt-BR das chaves `Profile.*` não mudaram; mudou só o
   nome da chave (padrão inglês dos grupos). Usos mapeados antes do rename: 3 no
   `Usuario/Index.cshtml` e zero em testes/controllers (comprovado por grep pré-mudança).
4. `NoEligibleContext.cshtml` e `AccessDenied.cshtml` **foram de propósito fora deste corte**
   (texto de diagnóstico próprio, tratado pelas chaves `Login.Reason*` já existentes no
   caminho de query); seguem como oportunidade documentada no anexo R7-D0.
5. **Dois contratos do gate migraram de literal para chave** (mesma intenção, novo lugar da
   verdade): `V2156SessionSecurityTests.RefreshDeContexto_…` agora exige
   `UiText.L("Auth.ContextExpired")` no controller + texto público no master pt-BR; e
   `V2164CompilationAndJourneyContractTests.Login_…ConnectionErrors` exige
   `UiText.L("Auth.LoginConnection")` + valor público no resx, mantendo a vedação da frase
   técnica ("Verifique se o backend está em execução") em controller E resx.

## Validação

- Build Web (Razor) + Tests: **0 erros** nos dois (`build15.log`, `BUILDWEB15_EXIT:0`,
  `BUILDTESTS15_EXIT:0`).
- Testes alvo (R7DF1 + R7DF2 + R7E1 + V2150 + V2156 + V2164): **65/65 aprovados em 7 s**
  (`build15.log`) — inclui os 2 contratos do gate atualizados na decisão 5.
- Gate da rodada + suíte completa ×3: gate **52/52** · full1 **1260/1260** · full2
  **1260/1260** · full3 **1260/1260** — janela limpa, sem flake (`suite-final2-summary.txt`
  de 10:18 UTC-3, `v2-full*.log`).

**Estado honesto**: IMPLEMENTADO → TESTADO. AINDA NÃO HOMOLOGADO em host vivo e não LIBERADO.
Commit local desta fatia; push diferido por decisão da rodada.
