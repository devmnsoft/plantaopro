# R58 — Ajuda central DIRETOR/RECEPCAO/PARCEIRO + tour guiado consumindo artigos · Relatório final consolidado

**Data:** 2026-10-19 · **Status: FECHADA e pushada** · **Prévia:** `r58a0-ajuda-perfis-e-tour-plano.md`

## Item da fila

Fila r53b1 §7, **item 4**: "Ajuda central: artigos para `DIRETOR`/`RECEPCAO`/`PARCEIRO`; tour
guiado consumindo os artigos (botão já existe) (R51)" — pendência documentada no relatório final
da R51.

## Decisões

- **D-R58-1** — `NormalizarPerfil` (AjudaController) mapeia **DIRETOR → ADMINISTRADOR_CLIENTE**
  dentro da ajuda: extensão do D-R51-1 com evidência de código (`UserRoleExtensions.IsTenantAdmin`
  e `RolesConstants.TenantAdmin` incluem DIRETOR; mesmo escopo Tenant; mesmo `DefaultRoute
  /ClientePortal`; acesso completo à família Administrativo360). O diretor lê o manual "Operação
  da instituição" (5 artigos R51) em vez de só os TODOS; sem manual duplicado.
- **D-R58-2** — Tópico `recepcao` (Perfil RECEPCAO, ícone `bi-person-badge`) "Atendimento na
  recepção" + 3 artigos: cadastrar paciente (`/Pacientes/Create`), agendar e fazer check-in
  (`/Agendamentos/CheckIn`), operar sala de espera (`/PainelChamada/Index`) — rotas confirmadas
  em `Saude360WebControllers.cs`; copy-base do `AssistenteContextualService`.
- **D-R58-3** — Tópico `parceiro` (Perfil PARCEIRO, ícone `bi-handshake`) "Portal do parceiro"
  + 2 artigos: acompanhar propostas (`/ParceiroPortal/Propostas`), acompanhar comissões e
  repasses (`/ParceiroPortal/Comissoes`, divergências via Suporte) — rotas confirmadas em
  `ParceiroPortalController`.
- **D-R58-4** — Tour guiado consome os artigos: cada âncora de artigo da seção "Recomendados
  para você" de `/Ajuda/Index` ganhou `data-tour-step="@artigo.Titulo|@artigo.Resumo"`. O roteiro
  do tour v2145 agora é 3 passos fixos de UI (busca, atalhos, manuais) + N artigos filtrados pelo
  perfil em ordem de prioridade. **Sem mudança no JS** (o v2145 já lê `[data-tour-step]`).
- **D-R58-5** — Copy no tom da casa (R50 + D-R57-1): frases curtas, imperativo/indicativo, sem
  exclamação, sem jargão banido (fallback/token/endpoint/seed), sem id/código cru.

## Mudanças versionadas

1. `AjudaController.cs` — mapeamento DIRETOR (2 linhas com comentário D-R58-1) + 2 tópicos +
   5 artigos nos helpers existentes.
2. `Views/Ajuda/Index.cshtml` — atributo `data-tour-step` nas âncoras de artigo (1 linha).
3. **Novo** `R58AjudaPerfisContratoTests.cs` — 4 fatos (scanner de fonte): normalização do
   diretor; tópico recepcao + >= 3 artigos + rotas Pacientes/Agendamentos/PainelChamada; tópico
   parceiro + >= 2 artigos + rotas ParceiroPortal Propostas/Comissoes; Index renderiza
   `data-tour-step` com o título do artigo no laço e os 3 passos fixos permanecem.

## Gates executados

| Gate | Resultado |
|---|---|
| Rebuild real `--no-incremental` | exit 0 · gate **Now=160 Base=160 DIFF=NENHUMA · erros=0** |
| Dirigidos R58 + R51 (não regressão) | **7/7** (4 novos + 3 da R51) |
| Suíte integral | **1628/1628** · exit 0 · 35 s (era 1624 pós-R57) |
| Smoke canônica R52 (binário final) | **`R52_E2E_OK FALHAS=0`** · /Ajuda/Index=98ms · MÉDIA_PADRAO=184ms (n=5) · log em `r58_smoke_e2e_log.txt` |
| Mini-e2e R58 `r58_ajuda_e2e.ps1` | **`R58_E2E_OK FALHAS=0`**: botão do tour OK; 3 passos fixos OK; artigos renderizados n=8; roteiro consome os 8 artigos; **total de passos = 11 (3+8)**; passo de exemplo "Como criar orçamento" presente · saída em `r58_ajuda_e2e_saida.txt` (UTF-8 verificado por char code via ripgrep) |
| Teardown | `PORTAS_LIVRES=True` (exit 1 dos shells background = processo morto pelo Stop-Process, padrão da casa) |

## Incidentes / notas

- O mini-e2e contém acentos em asserts (`Busca rápida|`, `Como criar orçamento`) — regravado
  com BOM antes da execução (armadilha PS5.1 documentada na R56); a saída versionada foi
  revalidada por grep UTF-8 porque a verificação inline pelo shell mostrou `False` por corrupção
  do canal da string de comando (falso negativo — o arquivo estava íntegro).
- Perfil demo (`ADMINISTRADOR_CLIENTE`) não exibe os tópicos recepcao/parceiro: o e2e cobre o
  mecanismo (roteiro = 3+N) e o passo do manual da instituição; o conteúdo dos dois perfis novos
  fica travado pelos fatos 2 e 3 do contrato R58 (scanner de fonte).
- A busca ativa em `/Ajuda/Index?busca=` reflete o filtro no roteiro (comportamento derivado,
  aceito e documentado no plano).

## Suíte-base da casa

**1628** (era 1624 pós-R57; +4 do novo contrato R58).

## Próximo item da fila r53b1 §7

Item 5: **acessibilidade avançada** (trap de foco em modais, `aria-live` em toasts, landmarks,
`th scope` em tabelas densas, teclado em widgets custom — R49/D-R49-4). Depois: item 6
(inventário de GUIDs/fragmentos) e seguintes.
