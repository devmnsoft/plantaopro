# R8-B13 — Menu `_AppSidebar`: `Usuarios.Index` ×2 e `Perfis.Index` ×2 (duplicação visível p/ super-admin) reconciliado por exclusão mútua de guardas (§29 item 5)

- **Rodada:** 8 (ciclo de conclusão integrada, master §1–§30) — fatia "Menu `_AppSidebar` com `Usuarios.Index` ×2 e `Perfis.Index` ×2" (item 5 da fila §29, ordem quebrado→alto impacto: … → B11 → B12 → **B13**)
- **Data:** 2026-10-13
- **Status:** IMPLEMENTADO + TESTADO (aceite por testes executados). HOMOLOGADO/LIBERADO seguem a janela viva de homologação (`r6a1b` §7), como o restante da rodada.
- **Regra de push:** respeitada — commit local em `main`, push mantido pendente com o resto da rodada.

## 1. Achado (fonte canônica)

- Matriz `r8a0-auditoria-inicial-matriz-estado.md` §29 item 5 (linha 231): **"Menu `_AppSidebar` com `Usuarios.Index` ×2 e `Perfis.Index` ×2 (:75/:76 vs :89/:90) — duplicação visível para super-admin."** Varredura `r8a0-varredura-indicadores-design-redundancias.md` (C5).
- **Estrutura (leitura do `_AppSidebar.cshtml`):**
  - `canManage` INCLUI `AdministradorGlobal`; `isGlobalAdmin` É `AdministradorGlobal` → **`isGlobalAdmin ⊆ canManage`**. Logo toda seção **"MNSOFT"** (`@if (isGlobalAdmin)`) é SEMPRE acompanhada pela seção **"Cliente"** (`@if (canManage)`).
  - Os dois pares duplicados são links `<a asp-action="Index">` **SEM parâmetro de rota** → destino idêntico:
    - Seção "Cliente" (:76/:77): rótulos `Nav.Usuarios`/`Nav.Perfis`, guardados por `Can("USUARIOS")`/`Can("PERFIS")`.
    - Seção "MNSOFT" (:90/:91): rótulos `Nav.UsuariosEAcessos`/`Nav.PerfisGlobais`, **incondicionais**.
- **Sem ação distinta de escopo global:** verificado nos controllers Web — `UsuariosController` (Index/Create/Edit/Save/AlterarStatus/Perfis[redirect]) e `PerfisController` (Index/Create/Edit/Details/Permissoes/Inativar) **não têm ação "globais"**. O escopo global é resolvido DENTRO da ação pelo usuário autenticado (claims), não por rota. → as 2 cópias por rota eram **redundância visível** (e ainda marcavam 2 itens "active" ao mesmo tempo em /Usuarios e /Perfis).

## 2. Decisão e mudança (sem regra inventada, sem deleção antecipada)

- **Reconciliar por EXCLUSÃO MÚTUA de guardas**, não remover rota/view. As cópias da seção **"Cliente"** só renderizam quando `&& !isGlobalAdmin`; as da seção **"MNSOFT"** permanecem (incondicionais dentro de `@if (isGlobalAdmin)`).
  - **Super-admin** (`isGlobalAdmin`): cópias "Cliente" ocultas → vê os rótulos globais ("Usuários e acessos"/"Perfis globais") na seção MNSOFT.
  - **Admin não-global** (`!isGlobalAdmin` + canManage): mantém "Usuários"/"Perfis" na seção "Cliente" (se tiver as permissões de módulo); não vê a seção MNSOFT.
  - **Resultado:** NO MÁXIMO 1 link por rota para QUALQUER usuário; a rota segue alcançável em todos os casos (um super-admin sem a permissão de módulo ainda chega via item incondicional da MNSOFT). O duplo-highlight "active" some junto com a duplicação.
- **Por que ocultar as cópias "Cliente" para o global (e não o inverso):** a seção MNSOFT é toda plataforma/global; manter "Usuários e acessos"/"Perfis globais" ali preserva a intenção "acesso mgmt global sempre disponível p/ o global admin" (o item era incondicional na seção) e mantém os rótulos semanticamente certos para esse perfil.

## 3. Alterações

| Arquivo | Mudança |
| --- | --- |
| `backend/PlantaoPro.Web/Views/Shared/_AppSidebar.cshtml` | Cópias "Cliente" de `Usuarios`/`Perfis` (:76/:77) passam de `@if (Can("USUARIOS"/"PERFIS"))` para `@if (Can("…") && !isGlobalAdmin)`. Sem rota nova/removida; sem view removida. |
| `backend/PlantaoPro.Tests/R8B13SidebarNavDedupContratoTests.cs` | **Novo**: 2 fatos — (1) inventário de rotas repetidas fixo `{MinhaCentral/Index×2 [intencional: logo], Usuarios/Index×2, Perfis/Index×2}` (pega nova duplicação sem reconciliação); (2) prova da exclusão mútua (as seções coexistem + as cópias "Cliente" carregam `&& !isGlobalAdmin`). |
| `docs/evidencias/2026-10-11-rodada-8/r8a0-auditoria-inicial-matriz-estado.md` | §29 item 5 (linha 231) → **R8-B13**; resumo "Estado" (§29) refletindo itens 1+2+3+4+5. |

## 4. Evidência (executada)

- **Build sln completa:** EXIT 0 (0 erros; apenas avisos pré-existentes) — compila o `_AppSidebar.cshtml` editado (Razor) + o teste.
- **Alvo:** `R8B13SidebarNavDedupContratoTests` = **2/2 aprovados** (`--filter FullyQualifiedName~R8B13`).
- **Gate phaseA v2 (pipeline):** **52/52, EXIT=0** (nas 2 execuções do pipeline).
- **Full suite v2:** obtido um full **limpo 1351/1351 EXIT=0** (= 1349 baseline B12 + 2 novos). As demais execuções registraram falhas unitárias isoladas **em classes do catálogo ① de flakes conhecidos** (`ProvisionamentoB5Tests`, `SaasGovernancaB6Rodada4Tests`) — **método diferente a cada run** (assinatura clássica de flake de banco sob pressão de memória): `P1_Finalizar_CriaMundoTodo_E_AdminAutentica`, `G3_Trial_ValidoOpera_E_VencidoBloqueiaComRegistro`, `P2_EmailDuplicado_400_SemResiduo`, `G4b_ReativarComConflito_Retorna409_E_AtivaOriginalIntacta`. **Todos aprovados 5/5 em isolamento** (`isolate-r8b13{,b}.ps1`, memória normal). Um `full1` da 1ª execução `EXIT=-1` (crash/OOM, RAM livre < 1 GB no início — 0,98 GB e 0,49 GB nas duas). **Nenhuma falha atribuível à esta fatia** — o full limpo 1351/1351 já cobre qualquer teste que asserte conteúdo da sidebar.
- **Verificação byte-level:** `_AppSidebar.cshtml` :76/:77 carregam `&& !isGlobalAdmin`; :90/:91 (MNSOFT) seguem incondicionais dentro de `@if (isGlobalAdmin)`; `git status` = sidebar + teste novo (+ matriz/doc).

## 5. Dívida registrada (fora do escopo desta fatia)

1. **Vistas globais de fato distintas?** Hoje "Perfis globais"/"Usuários e acessos" e "Perfis"/"Usuários" apontam para o MESMO `Index` (escopo resolvido por claim no controller). Se produto quiser **vistas separadas** (ex.: lista global de perfis vs. do tenant), cria-se a ação/rota distinta e o link da MNSOFT passa a apontar para ela — fora desta reconciliação de menu.
2. **Outros menus de navegação** (command palette / Ctrl K) podem ter lista própria de rotas — fora do escopo do item 5 (foco `_AppSidebar`). Monitorar se houver a mesma duplicação lá.
3. **Main DB v2339 / homologação viva:** invariado — janela segue em `r6a1b` §7 (+ lote v2340–v2351 + IIS).

## 6. Fila seguinte (R8)

Com §29 item 5 entregue (menu reconciliado + contrato), o próximo item da fila §29 = **item 6: Comissão calculada/apropriada em 4 locais (baixa, geração AP, relatórios, UI) — validar canonicidade da fórmula.** Alternativas: janela viva de homologação (`r6a1b` §7), ou o push da rodada (pendente).

## 7. Gotchas aprendidos (reutilizáveis)

- **Nome de tupla em chave de dicionário C#:** uma `(string,string)` usada como chave de `Dictionary` perde os nomes dos elementos (vira `ValueTuple<string,string>`); use `.Item1/.Item2` ou destruturize `foreach (var ((a, b), v) in dict)` — não `.Controller`/`.Action` (CS1061).
- **`$` em PowerShell inline via wrapper:** o harness remove `$` de `-Command` inline → usar arquivo `.ps1` ou comandos sem `$` (ex.: `Get-CimInstance Win32_OperatingSystem | Select-Object FreePhysicalMemory`).
- **Flakes de banco sob pressão de RAM:** com < ~1 GB livre, o full suite pode cair em `EXIT=-1` (OOM) e fazer classes flakies (catálogo ①) falharem em **métodos diferentes a cada run**; isolá-las (5×) sob memória normal é a prova de que não é regressão.
- **`isGlobalAdmin ⊆ canManage`:** quem vê a seção global SEMPRE vê a de cliente também — para deduplicar um item presente nas duas, exclua a cópia de uma delas **para o subconjunto** (aqui: `&& !isGlobalAdmin` nas cópias de cliente); não remover as duas.
