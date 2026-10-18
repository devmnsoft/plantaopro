# R27 · r27a0 — E1 linguagem simples, zona portal (inventário file:line + decisões)

Data: 2026-10-18 · Sessão: `opencode` · Base: R26 (`e880524`, suíte 1540).

## Método

Scanner de jargão técnico (`%TEMP%\opencode\r27_scan_jargao2.ps1`) sobre as zonas do portal
(Plantoes, MinhaAgenda, Agendamentos, Convite, FaturamentoClinico, DashboardsPremium, Saude360, Bi,
OperacaoPremium, OperacaoInteligente, AssistenteIa): palavras `api/bff/endpoint/guid/scoping/payload/json/sql`
em texto de view. Resultado: 32 ocorrências em 8 arquivos; cada uma classificada abaixo.

## Classificação REESCREVER (texto visível ao usuário → linguagem simples)

| # | Local | Texto atual → novo |
|---|---|---|
| 1 | `Plantoes\Details.cshtml` L187 (title de botão) | "A API do plantão não fornece vínculo de fechamento." → "O fechamento será vinculado quando for gerado no sistema." |
| 2 | `Plantoes\_PlantaoForm.cshtml` L93 | "Ações críticas são validadas pela API e registradas para auditoria." → "As ações críticas são validadas e registradas para auditoria." |
| 3 | `Agendamentos\AgendaPremium.cshtml` L10 | remove `Fonte: <code>@Model.Endpoint</code>` — URL técnica crua na página (E1+E6); `Model.Endpoint` usado só neste ponto (verificado) |
| 4 | idem L53 | "Não informada pela API" → "Não informada" |
| 5 | idem L65 (title) | "O status retornado pela API não permite confirmar..." → "O status atual deste agendamento não permite a confirmação." |
| 6 | idem L72 (title) | "...status de espera retornado pela API." → "A triagem exige check-in ou status de espera." |
| 7 | idem L73 (title) | "...quando a API retornar o vínculo da consulta..." → "Este botão ficará habilitado quando a consulta estiver vinculada ao agendamento." |
| 8 | `FaturamentoClinico\Index.cshtml` L34 | "Os filtros usam somente status, competência e convênio fornecidos pela API." → "Você pode filtrar por status, competência e convênio." |
| 9 | idem L50 | "consultar novamente os dados retornados pela API." → "consultar novamente." |
| 10 | idem L70/74/76/100 | "Não informado(a) pela API" → "Não informado(a)" |
| 11 | idem L84 (title) | "A API não informou o identificador da consulta." → "Esta conta ainda não tem vínculo com a consulta de origem." |
| 12 | idem L87 (title) | "A API atual não oferece aprovação, glosa ou exportação desta conta." → "Aprovação, glosa e exportação ainda não estão disponíveis para esta conta." |
| 13 | idem L101 | "...histórico não são fornecidos por esta API." → "Detalhes completos (paciente, convênio, valores e histórico) estão disponíveis no financeiro clínico." |
| 14 | `DashboardsPremium\Perfil.cshtml` L9 | "KPIs carregados da API <code>/api/dashboards/...</code>, respeitando tenant, perfil, plano..." → "Indicadores do seu perfil, plano e módulos contratados." (E1 + bônus E2: retira "tenant" e "KPI" do texto principal) |
| 15 | idem L18 (empty state) | "A API não retornou KPIs para este contexto." → "Nenhum indicador foi encontrado para este contexto." (mantém o substring do contrato PosRc "Somente indicadores reais são exibidos.") |
| 16 | idem L40 | "Sem alertas retornados pela API." → "Sem alertas para este período." |
| 17 | `OperacaoPremium\Cobertura.cshtml` L13 (botão) | "Atualizar BFF" → "Atualizar cobertura" (arquitetura BFF visível ao usuário — o mais grave do scan) |
| 18 | idem L28 | "Quando a API retornar plantões sem cobertura, eles aparecerão aqui com score, impedimentos e ações reais." → "Plantões sem cobertura aparecerão aqui com pontuação, impedimentos e ações disponíveis." |

## Whitelist (código/infra, NÃO é texto de usuário — manter)

- `Plantoes\Details.cshtml` L11: `Guid.Empty` dentro de expressão C# Razor.
- `Saude360\Formulario.cshtml` L62-66 / `Modulo.cshtml` L28: propriedade `Endpoint = "/api/lookups/..."`
  do componente de busca (Razor) e classe `available-from-api` — código, não renderizado como texto.
- `Bi\Index.cshtml` L21: `fetch('/api/bi/resumo-executivo')` — JavaScript.
- `DashboardsPremium\Perfil.cshtml` L28-32: comentário Razor `@* *@` (drill-down R10, reg_status) — não renderiza.

## Fato permanente (r27b1)

`R27LinguagemSimplesPortalContratoTests`: marcadores anti-regressão — os literais antigos não reaparecem nos 6
arquivos reescritos (inclui regra estrutural `DoesNotContain("pela API")` nestes arquivos) e os textos novos
EXISTEM (contra remoção acidental). Jargão em C#/JS/Razor continua liberado.

## Plano de fatias R27

| Fatia | Conteúdo | Fatos |
|---|---|---|
| r27b1 | Os 18 pontos (6 views) | 2 fatos (anti-regressão + presença dos novos textos) |
| r27z | Consolidado + suíte (base 1540) + push | — |
