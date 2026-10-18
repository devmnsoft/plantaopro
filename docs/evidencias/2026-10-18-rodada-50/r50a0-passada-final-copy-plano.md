# R50 · r50a0 — Passada final de copy (tom de voz único, glossário)

Data: 2026-10-18 · Sessão: `opencode` · Base: R49 (`f255d3f`, suíte 1599).

## Objetivo

Última passada sistemática de copy antes da Ajuda central (R51): eliminar jargão técnico em texto
visível, fixar o **glossário canônico** do produto e travar tudo em contrato permanente.

## Recon (scan Views + resx pt-BR)

Achados de jargão em texto visível (fora nomes de código/propriedades, que permanecem):

| Local | Texto atual | Veredicto |
|---|---|---|
| `AssistenteIa\Index.cshtml` (5 usos) | "Fallback de modelo", "Fallback — provedor (opcional)", "Modelo do fallback (opcional)", "provedor de fallback" | trocar por "reserva/padrão" |
| `WhiteLabel\Index.cshtml` / `Preview.cshtml` | KPI card `"Fallback"`, "usa o fallback PlantãoPro" | "Padrão" / "usa o padrão PlantãoPro" |
| `Convites\Index.cshtml` | `title="Reenvio não está contratado neste endpoint."` | "endpoint" é jargão; frase de plano |
| `Convites\Index.cshtml` | `<dd>@item.PlantaoId.ToString()[..8]</dd>` sob `<dt>Plantão</dt>` | fragmento de GUID cru visível (E6); redundante com a ação "Abrir plantão" |
| `UiTexts.resx` → `Auth.ResetTokenInvalid` | "Token inválido ou expirado." | "token" é jargão p/ usuário; fala-se do link |
| `UiTexts.resx` L3222 (guia RelFin) | "(fallback de nome)" | "(resolução alternativa de nome)" |

Fora de escopo/intencionalmente mantidos: nomes de propriedade/atributo em código (`name="FallbackProvedor"`,
`Endpoint = "/api/..."`, chaves `Nav.FallbackTenant`), a palavra "Dashboard" (termo de negócio
consolidado na navegação — mudança total p/ "Painel" vai para R51+ junto da revisão de menus), e as
traduções en/es/fr (sem jargão equivalente identificado nesta passada).

## Glossário canônico (decisão D-R50-1)

| Termo | Uso no produto | Alternativas banidas em texto visível |
|---|---|---|
| modelo de reserva / padrão | configuração de IA, logos, valores por defeito | "fallback" |
| link (de redefinição) / sessão | autenticação | "token" |
| plano (do cliente) | recursos contratados | "endpoint", "tier", "package" |
| fatura / assinatura | SaaS | "invoice", "billing" em frases |
| organização / profissional | tenant/perfil quando o claim não existe | "tenant" fora da zona super-admin (já travado em R39) |

## Correções (passada)

1. `AssistenteIa\Index.cshtml`: 5 textos → "Modelo de reserva" / "Provedor de reserva (opcional)" /
   "Modelo de reserva (opcional)" / placeholder "padrão vigente do provedor" / form-text sem "fallback".
2. `WhiteLabel\{Index,Preview}.cshtml`: KPI `"Fallback"`→`"Padrão"`; help "usa o padrão PlantãoPro".
3. `Convites\Index.cshtml`: title → "O reenvio de convites não está disponível no seu plano."; remover o
   `<dd>` com fragmento de GUID (meta duplicada da ação "Abrir plantão").
4. `UiTexts.resx` (base pt-BR): `Auth.ResetTokenInvalid` → "O link de redefinição é inválido ou expirou.";
   guia RelFin "(fallback de nome)" → "(resolução alternativa de nome)".
5. Contrato permanente `R50CopyContratoTests`:
   - fato 1: frases-jargão banidas ausentes em Views ("fallback de modelo", "modelo do fallback",
     "provedor de fallback", "como fallback", "usa o fallback", "neste endpoint", KPI "Fallback");
   - fato 2: nenhum `<value>` do `UiTexts.resx` (pt-BR) contém "fallback" ou "token" (case-insensitive);
   - fato 3: `Convites\Index.cshtml` não exibe mais fragmento de GUID (`PlantaoId.ToString()[..8]`).

## Gates

Build real + gate 160 chaves; classes dirigidas (R50 + higiene + R7DF6 653 keys + R39); suíte integral
(esperado 1599+3=1602); commits `web,tests (rodada 50 r50b1)` + `docs (rodada 50 r50z)`; push.
