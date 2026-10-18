# R29 · r29a0 — E1 linguagem simples, demais módulos (inventário file:line + decisões)

Data: 2026-10-18 · Sessão: `opencode` · Base: R28 (`cf9659d`, suíte esperada 1544).

## Método

Grep global `pela API|da API|BFF|Model.Endpoint` + jargões adicionais em `Views/**` fora de Adm360
(portal e Adm360 fechados em R27/R28). Restantes = comentários Razor/HTML ou atributos de componente
(`data-lookup-endpoint`), whitelisted. Resultado: **19 pontos em 9 arquivos** ("billing" em inglês é jargão
— itens 9/15/16/17).

## Classificação REESCREVER

| # | Local | Texto atual → novo |
|---|---|---|
| 1 | `AdminSaas\Index.cshtml` L93 | "Nenhum módulo retornado pela API." → "Nenhum módulo encontrado." |
| 2 | `Lgpd\Eventos.cshtml` L3 | "…registrados em <code>lgpd_eventos_privacidade</code> e podem ser consultados pela API <code>/api/lgpd/eventos</code>." → "Eventos de privacidade ficam registrados no sistema e podem ser consultados nesta área." (retira nome de tabela + endpoint crú — E1+E6) |
| 3 | `Escalas\Index.cshtml` L54 + L71 (3 ocorrências) | "Não informado(a) pela API" → "Não informado(a)" |
| 4 | `Configuracoes\Index.cshtml` L20 | "O perfil não foi retornado pela API." → "Seu perfil ainda não foi encontrado." |
| 5 | `Financeiro\Index.cshtml` L41 | "Somente valores realizados retornados pela API" → "Somente valores realizados" |
| 6 | idem L102 | "Não informado pela API" → "Não informado" |
| 7 | `Pagamentos\Index.cshtml` L13 | "…também só aparecem quando retornados pela API." → "…também só aparecem quando existem registros." |
| 8 | `Observabilidade\Erros.cshtml` L4 | "Últimas falhas registradas pela API/Web." → "Últimas falhas registradas pelo sistema." |
| 9 | `MinhaAssinatura\Index.cshtml` L5 | "Consulte somente informações contratuais devolvidas pela API para a organização autenticada." → "Consulte somente as informações do contrato da sua organização." + eyebrow "Contrato e billing" → "Contrato e cobranças" ("billing" é jargão em inglês) |
| 10 | idem L21 | "Situação retornada pela API" → "Situação atual" |
| 11 | idem L33 | "O histórico não foi informado pela API para esta assinatura." → "Ainda não há cobranças registradas para esta assinatura." |
| 12 | idem L38 | "Nenhum módulo foi informado pela API." → "Nenhum módulo habilitado no momento." |
| 13 | idem L39 | "Não informado pela API." → "Não informado." |
| 14 | `Comercial\PrevisaoReceita.cshtml` L3 | "A previsão consulta oportunidades e propostas em andamento pela API <code>/api/comercial/previsao-receita</code>." → "A previsão considera as oportunidades e propostas em andamento do seu funil comercial." (endpoint crú — E1+E6) |
| 15 | `MinhaAssinatura\Index.cshtml` L23 | "Data informada pelo billing" → "Data combinada no contrato." |
| 16 | idem L32 | eyebrow "Billing" → "Cobranças"; "Eventos efetivamente retornados pelo serviço de cobrança." → "Somente eventos registrados na cobrança." |
| 17 | `AdminSaas\Index.cshtml` L148 | "Revise ações críticas, bloqueios, billing e contexto assistido." → "…bloqueios, cobranças e contexto assistido." |

## Whitelist (código/comentário — manter)

- `Administrativo360\FluxoCaixa.cshtml` L252, `DashboardsPremium\Perfil.cshtml` L29, `Home\Dashboard.cshtml` L62:
  comentários HTML/Razor de rodada ("emitidas/retornadas pela API", "escopo tenant da API").
- `Shared\_AutocompleteField.cshtml` L6 / `_LookupSelect.cshtml` L6: atributo `data-lookup-endpoint="@Model.Endpoint"`
  — mecanismo do lookup por nome (epic E6), código, não texto.
- IDs/atributos `#billing`, `data-billing-*`, `billing-title` em Consultas/FaturamentoClinico/Clientes/MinhaAssinatura
  — identificadores de código, não texto.
- `CommercialDemoWeb\Landing.cshtml` L43 ("comercial, billing e Customer Success") — zona de marketing comercial
  (whitelist desde R25).

## Fato permanente (r29b1)

`R29LinguagemSimplesDemaisModulosContratoTests`: anti-regressão dos literais antigos nos 8 arquivos
(inclui regra estrutural `DoesNotContain("pela API")` arquivo-a-arquivo; Perfil.cshtml fora do escopo
deste fato por causa do comentário whitelist) + presença dos textos novos.

## Plano de fatias R29

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r29b1 | Os 19 pontos (9 views) | 2 fatos |
| r29z | Consolidado + suíte + push | — |

Fechado o E1 parte de *jargão técnico renderizado* em TODAS as zonas; o que restar no E1 (frases longas /
instruções de uso por página) casa com o epic E3 (R35–R37) para tratar junto com as instruções.
