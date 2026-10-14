# R8-B26 — Layout: menu/shell Administrativo 360 ficando "abaixo/atrás" do conteúdo

**Data:** 2026-10-14 · **Rodada:** 8 · **Bloco:** B26
**Solicitação do usuário:** "melhorar o design do sistema" — o menu do sistema (sobretudo o Administrativo 360) aparecia *abaixo/atrás* dos cards e tabelas em vez de ficar como navegação lateral fixa.

## 1. Classificação
- **Tipo:** defeito existente + comprovado (reproduzido ao vivo e medido), corrigido.
- **Escopo:** geometria do app-shell (`[rail][conteúdo]`) na faixa de largura **~992–1199 px**. Ajuste puramente estrutural/visual; nenhuma regra clínica/comercial inventada.

## 2. Reprodução (ao vivo)
- Subi API (`http://localhost:51976`) + Web (`http://localhost:52976`) em Development (ambos HTTP; sem TLS).
- Login do admin do tenant de homologação: `admin.clinica@plantaopro.local` (tenant `8b0c8e74-a81b-4ea2-b499-94755a1ca001`).
- O tenant não tinha ADM360 contratado → grant reversível (ver §6).
- Busquei o HTML autenticado de `/Administrativo360/Gestao/Dashboard`, fiz os assets absolutos e renderizei em aba do navegador (**viewport 1000×700**).

### Medição ANTES (viewport 1000 px)
| Elemento | valor medido | interpretação |
|---|---|---|
| `.pp-app-shell` | grid · colunas `240px 744.8px` · **linhas `700px 9504px`** | shell virou 2 LINHAS (empilhado) |
| `#appSidebar` | x=0 y=0 w=280 h=700 · `position:sticky` · `grid-column:auto` | rail 100 vh na linha 1 |
| `.pp-main-shell` | **x=0 y=700 w=240 · `grid-column:1`** | conteúdo empurrado pra **linha 2** (abaixo do menu), banda ~240 px |
| `.adm360-nav-wrapper` | **w=157** | sub-nav apertado |

Sintoma exato reportado: o menu lateral (100 vh, sticky) empurrava todo o conteúdo um viewport inteiro pra baixo e, ao rolar, ficava sobre/ao lado dos cards e tabelas.

## 3. Causa raiz
`backend/PlantaoPro.Web/wwwroot/css/design-system/v2144-responsive-safe-actions.css` — o bloco `@media (max-width: 74.99rem)` (~1199 px) forçava:
```css
.pp-app-shell  { grid-template-columns: 1fr; }  /* colapsa p/ 1 coluna */
.pp-main-shell { grid-column: 1; }             /* conteúdo para a coluna do rail */
```
…enquanto o sidebar segue **visível** (o modo mobile/off-canvas só entra em ≤~991 px via `clinical`/`responsive`). Na faixa **992–1199 px**, isso empilhava o conteúdo abaixo do menu e comprimia tudo à esquerda. Era contraditório com a própria linha seguinte do bloco (que mantinha `.pp-sidebar` com largura de rail visível).

## 4. Correção
Removi as duas declarações que colapsavam o shell naquele bloco (mantive as regras úteis: largura do sidebar + esconder badges do topbar). O shell volta a ficar em **2 colunas `[rail][conteúdo]`**; o `grid-column:2` do `.pp-main-shell` volta a vir de `layout.css` sem override; o modo mobile segue por off-canvas (caminho independente).

**Contenção da mudança:** só afeta a faixa **991,98–1199,84 px** (de quebrada → correta).
- ≥1200 px: o bloco `max-width:74.99rem` não se aplica → comportamento **inalterado** (já era 2 colunas).
- ≤991,98 px: `display:block` + off-canvas dominam; as regras removidas eram de grid (irrelevantes ali) → **inalterado**.

## 5. Evidência executada
- **Renderização ao vivo (medição numérica) — DEPOIS da correção**, re-fetch do HTML com o CSS atualizado (novo hash `?v=` força re-download; sem nenhum estilo injetado), mesmo viewport 1000 px:
  | Elemento | valor medido |
  |---|---|
  | `.pp-main-shell` | **x=240 y=0 w=745 · `grid-column:2`** |
  | `.pp-topbar` | x=240 y=0 w=745 |
  | `#appSidebar` | x=0 y=0 w=280 (rail à esquerda) |
  | `.adm360-nav-wrapper` | **w=661** (largura plena da área de conteúdo) |
  | `documentElement.scrollWidth` | 985 = clientWidth (**sem estouro horizontal**) |
  Prévia idêntica confirmada por CSS injetado (mesma regra) antes de aplicar no arquivo.
- **Nota de ferramenta:** `browser.screenshot` falhou por o desktop-window da caixa não estar composited/visível; a evidência executada é a medição de layout acima (`getBoundingClientRect` + computed style), antes/depois.
- **Higiene:** o doc `r8b25-admin360-gestao-dashboard-definicoes.md` trazia, **auto-referentemente**, o termo legado proibido ao explicar o gotcha → falha determinística do `RepositoryHygieneContractTests`. Re-word dos 3 trechos (sinônimos "fiel"/"aderente"); `grep -ri "fideli*"` no repo = **0 ocorrências**; classe de higiene **8/8**.
- **Compilação:** o ajuste é CSS estático (não compilado pelo MSBuild); sem mudança de assembly nem de lógica.
- **Full suite v2** (gate phaseA + full×3 `--no-build`):
  - **Primeira execução após o ajuste** — Total de testes **1388** nas 3 passes, com 4 falhas determinísticas + 1 transitória, **todas diagnosticadas e eliminadas**:
    - As 4 falhas determinísticas (`JwtConfigurationContractTests` ×2 · `HomologacaoRuntimeRealContractTests.MenusControllers…` · `HomologacaoCrudsAcoesJornadasContractTests.Repositorio_NaoDeveConterPadroesProibidos…`) partiam da mesma causa: ao subir os servidores ao vivo, o app **persistiu a connection string resolvida (com a credencial do banco)** nos `appsettings.json` versionados, que deviam permanecer vazios (credencial via env `ConnectionStrings__Default` / user-secrets). **Reverti os 2 arquivos** → testes-alvo **7/7**.
    - A transitória (`ProvisionamentoB5Tests.P3_CorridaMesmoCnpj_…`) falhou em 1 das 3 passes por pressão de memória → **isolate 5/5**.
  - **Execução definitiva (após o revert):** gate phaseA **EXIT 0** + full×3 **EXIT 0**, **Total de testes 1388 em cada pass, zero falhas**.

## 6. Mudança de dados (reversível — banco de TESTE `plantaopro_test`)
Para reproduzir/visualizar, contratei ADM360 ao tenant de homologação (função canônica `modulos_efetivos()` passa a incluí-lo; o guard Web resolve via `api/auth/effective-modules` a cada request):
```sql
insert into plantaopro.tenant_modulos
  (id, tenant_id, codigo, nome, status, dados, criado_em, modulo_id, codigo_modulo, habilitado, origem, reg_status, reg_date, metadata, cliente_id)
values
  (gen_random_uuid(), '8b0c8e74-a81b-4ea2-b499-94755a1ca001',
   'ADM360', 'Administrativo 360', 'ATIVO', '{"habilitado": true}'::jsonb, now(),
   'a3600000-0000-4000-8000-000000000099', 'ADM360', true, 'LEGADO', 'A', now(), '{}'::jsonb,
   '8b0c8e74-a81b-4ea2-b499-94755a1ca002');
```
Reverter:
```sql
delete from plantaopro.tenant_modulos
where tenant_id='8b0c8e74-a81b-4ea2-b499-94755a1ca001' and codigo_modulo='ADM360';
```

## 7. Status
- **IMPLEMENTADO + TESTADO** (suíte verde). Não HOMOLOGADO/LIBERADO (o portão §7 vivo ainda é externo); mas o ajuste é verificável por renderização e está documentado aqui com medições.
- **Arquivos tocados:** `v2144-responsive-safe-actions.css` (produção/estilo) + `r8b25-admin360-gestao-dashboard-definicoes.md` (re-word de higiene). Nenhum assembly de produção alterado.
- **Gotcha ambiental registrado:** rodar os servidores (Web/API) ao vivo persiste a connection string efetiva dentro dos `appsettings.json` versionados; **reverter esses 2 arquivos** sempre que o app foi subido antes de commitar (a suíte roda com `--no-build` e lê os arquivos-fonte em disco, sem reescrevê-los).
