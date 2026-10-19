# R58 — Ajuda central: perfis DIRETOR/RECEPCAO/PARCEIRO + tour guiado consumindo artigos

**Data:** 2026-10-19 · **Prévia desta rodada.** Gates executados no relatório final.

## Escopo (origem)

Fila r53b1 §7, **item 4**: "Ajuda central: artigos para `DIRETOR`/`RECEPCAO`/`PARCEIRO`; tour
guiado consumindo os artigos (botão já existe) (R51)". A pendência documentada no final da R51
confirma: perfis operacionais sem manuais próprios + botão "Fazer tour"
(`data-tour-start="help-center"`) existindo sem consumir os artigos como roteiro.

## Recon

- `AjudaController` mantém tópicos/artigos estáticos em C# (helper `Artigo(...)`), filtrados pelo
  perfil primário da sessão via `NormalizarPerfil`. Tópicos existentes: saas (ADM_GLOBAL),
  operacao (COORDENACAO), medico, financeiro, hospital, instituicao (ADM_CLIENTE, R51),
  central + privacidade (TODOS). **DIRETOR/RECEPCAO/PARCEIRO só viam os TODOS.**
- **DIRETOR (prioridade 600)**: `UserRoleExtensions.IsTenantAdmin` = ADMINISTRADOR ||
  ADMINISTRADOR_CLIENTE || DIRETOR; `RolesConstants.TenantAdmin` idêntico; mesmo `DefaultRoute
  /ClientePortal` (AccessScope L39, AccountController L245); acesso completo à família
  Administrativo360 (Administrativo360Controller, Adm360Gestao/Cotacoes/Fiscal/DocumentosXml,
  Billing, Configuracoes, FaturamentoSaas).
- **RECEPCAO (300)**: DefaultRoute `/CentralAtendimento`. Rotinas confirmadas nos controllers:
  `/Pacientes/Create` (Saude360WebControllers L332), `/Agendamentos/CheckIn` (L145),
  `/PainelChamada/Index` (L105). Copy-base já existe em `AssistenteContextualService` (linhas
  RECEPCAO). Políticas capability (`CENTRAL_ATENDIMENTO:VER`, `AGENDAMENTO:CHECKIN`,
  `PAINEL_CHAMADA:OPERAR`) — perfil RECEPCAO recebe os módulos pela v2339 (FeatureCatalogService).
- **PARCEIRO (200)**: DefaultRoute `/ParceiroPortal`. `ParceiroPortalController` (roles
  PARCEIRO+ADM_GLOBAL) com ações Index/Leads/Clientes/Propostas/Comissoes/Repasses/Materiais/
  Suporte; matriz ProfessionalSaas: ações VER/PROPOSTAS/COMISSOES.
- **Tour**: `wwwroot/js/v2145-guided-tour.js` — card genérico que coleta `[data-tour-step]`
  (formato `Título|Descrição`) em ordem de DOM, disparado por `[data-tour-start]`. O Index de
  Ajuda tem 3 passos fixos (busca, atalhos, manuais) e o botão "Fazer tour"; os artigos da
  seção "Recomendados para você" ainda não eram passos.

## Decisões

### D-R58-1 — DIRETOR compartilha o manual da família administrador de tenant
Extensão direta do D-R51-1 com evidência de código: DIRETOR é membro de `IsTenantAdmin`/
`RolesConstants.TenantAdmin`, mesmo escopo Tenant e mesmo `DefaultRoute /ClientePortal`.
`NormalizarPerfil` passa a mapear `DIRETOR` → `ADMINISTRADOR_CLIENTE` **dentro da ajuda**: o
diretor lê o manual "Operação da instituição" (5 artigos R51) em vez de ver só os TODOS. Sem
manual duplicado.

### D-R58-2 — tópico `recepcao` (Perfil RECEPCAO, ícone `bi-person-badge`)
"Atendimento na recepção" — 3 artigos: cadastrar paciente (Pacientes/Create); agendar e fazer
check-in (Agendamentos/CheckIn); operar sala de espera (PainelChamada/Index).

### D-R58-3 — tópico `parceiro` (Perfil PARCEIRO, ícone `bi-handshake`)
"Portal do parceiro" — 2 artigos: acompanhar propostas (ParceiroPortal/Propostas); acompanhar
comissões e repasses (ParceiroPortal/Comissoes, dúvida via Suporte).

### D-R58-4 — tour guiado consome os artigos do perfil
Cada âncora de artigo da seção "Recomendados para você" de `/Ajuda/Index` ganha
`data-tour-step="@artigo.Titulo|@artigo.Resumo"`: o roteiro do tour passa a ser 3 passos de UI
(busca, atalhos, manuais) + N artigos filtrados pelo perfil, em ordem de prioridade (Ordem).
**Sem mudança no JS** — o v2145 já lê `[data-tour-step]` (precedente: passos fixos com acentos
já funcionam). Se a busca estiver ativa, o roteiro reflete o filtro exibido (comportamento
derivado, aceito).

### D-R58-5 — copy no tom da casa (R50 + D-R57-1)
Frases curtas, imperativo/indicativo, sem exclamação, sem jargão banido (fallback/token/
endpoint/seed), sem id/código cru no texto exibível.

## Contrato novo — `R58AjudaPerfisContratoTests` (4 fatos, scanner de fonte)

1. `Diretor_CompartilhaManualDaFamiliaTenantAdmin` — o mapeamento DIRETOR →
   ADMINISTRADOR_CLIENTE existe em `NormalizarPerfil`.
2. `TopicoRecepcao_CobreRotinasDoGuiche` — slug `recepcao` + Perfil RECEPCAO; >= 3 artigos do
   perfil; blocos referenciam Pacientes, Agendamentos e PainelChamada.
3. `TopicoParceiro_CobrePropostasEComissoes` — slug `parceiro` + Perfil PARCEIRO; >= 2 artigos
   do perfil; referências a ParceiroPortal, Propostas e Comissoes.
4. `TourConsomeArtigosDoPerfil` — `Ajuda/Index.cshtml` renderiza `data-tour-step` com
   `@artigo.Titulo` no laço de `Model.Artigos`; os 3 passos fixos continuam presentes.

Suíte esperada: 1624 → **1628**.

## Gates (executados no relatório final)

1. Rebuild real `--no-incremental` → gate `r20b1_gate.ps1` (espera `Now=160 Base=160 DIFF=NENHUMA`).
2. Dirigidos R58 (4/4) + dirigidos R51 (não regressão).
3. Suíte integral (~1628).
4. Smoke canônica R52 contra binário final.
5. Mini-e2e R58: `/Ajuda/Index` renderiza 3 passos fixos + 1 passo por artigo do perfil demo
   (esperado 3 + 8 = 11 `data-tour-step`) e o botão do tour intacto.
6. Docscan dos docs desta rodada.
