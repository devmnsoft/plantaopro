# Roteiro de Homologação por Perfil — PlantãoPro (2026-10-03)

**Entrega 6 de 8** da consolidação de homologação sobre o baseline `82b8c26` (que contém WP-S1/S2/S3/S4 e migrações v2305/v2306/v2307).

- **Ambiente executado**: stack dev local — API `https://localhost:51977`, Web `https://localhost:52977`, PostgreSQL local (schema `plantaopro`), build com 0 erros (`artifacts/runtime-logs/dev/build-20261003-221535.log`).
- **Credenciais**: ver `docs/usuarios-teste.md` (não repetir senhas neste documento).
- **Perfis cobertos**: administrador da clínica (`admin.clinica`), gestor de suprimentos do tenant Santa Casa (`gestor@santacasa-demo.example`), médico (`medico@plantaopro.local`).
- **Foco desta rodada (P2 IA + P5 design)**: telas onde o assistente IA aparece (`/Pendencias`, `/AssistenteIa`, `Administrativo360/Cotacoes/Detalhes/{id}`), bloco de ajuda "Como usar esta tela", acessibilidade (contraste/teclado/foco) e responsividade (1440/768/390).

## 1. Métodos de evidência

| Método | Como | O que prova |
|---|---|---|
| Navegação + interação real | Browser real autenticado por perfil; cliques em `[data-ai-run]`, `[data-ai-test-btn]`, "Gerar análise" | Estados reais da máquina de estados IA renderizados pelo servidor |
| Contraste WCAG | Estilos **computados** (cor efetiva × fundo efetivo) com fórmula de luminância WCAG 2.x | Razões medidas (≥ 4,5 texto normal; ≥ 3,0 texto grande) |
| Teclado/foco | Pressão real de **Tab** (ativa `:focus-visible`) + leitura de outline computado | Indicador de foco visível nos controles novos |
| Responsivo | Injeção de **iframe same-origin** (768 / 390 / 1440 px) sobre a página: as media queries internas seguem o viewport do próprio iframe. Em 1440 px (janela nativa < 1440), iframe `width:1440` + `zoom` para caber na janela — o `zoom` não altera media queries internas | Medidas reais de layout nas três larguras |

**Limitação declarada (capturas)**: a ferramenta de browser exige janela desktop **visível**; durante toda a sessão ela não estava em primeiro plano (erro `Screenshot needs a visible tab`, reiterado após `tabs.focus`). As capturas PNG por módulo/perfil ficam **PENDENTES** (item 7 abaixo); todo o resto foi evidenciado por DOM/estilos computados e estados reais, não por inferência.

---

## 2. Perfil A — Administrador da clínica (`admin.clinica`)

### A1 — Central de Pendências com Assistente IA — `GET /Pendencias`
**Passos manuais**: login → *Central de Pendências* → abrir "Como usar esta tela" → clicar "Gerar resumo".
**Esperado**: bloco de ajuda presente; painel IA opcional/não bloqueante; estado determinístico conforme dados reais; link rodapé "Configurar assistente (administradores)".

**Executado ✅ (evidência DOM)**:
- Bloco `details.screen-guide-card` presente, incluindo item novo sobre o Assistente IA.
- Clique em `[data-ai-run]` → estado **VAZIO**: *"Não há pendências no período. Nada para resumir."* (usuário sem pendências — vazinho ≠ falha; comportamento correto pós-checagem de configuração).
- Contraste medido no painel: kicker `rgb(37,99,235)`/branco **5,17**; título **15,63**; nota `rgb(82,107,120)`/branco **5,63**; todos ≥ 4,5 (AA; títulos AAA).
- Contraste do ajuda: summary **8,05**, texto **6,57** (AA+/AAA).
- Foco: botão IA recebe anel (`box-shadow rgba(31,115,241,.22)`); `summary` do **sem anel** foi corrigido nesta rodada — após rebuild, Tab real → `:focus-visible` = true, `outline 2.4px solid rgb(31,115,241)`.

### A2 — Configuração do Assistente IA — `GET /AssistenteIa`
**Passos manuais**: menu *Configurações → Assistente IA* → inspecionar cards → clicar "Testar conexão".
**Esperado**: 2 jornadas canônicas; aviso quando `Ai:EncryptionKey` ausente; teste sem chave → `NAO_CONFIGURADO` com mensagem acionável.

**Executado ✅ (evidência DOM)**:
- Cards **MEU_DIA_RESUMO** e **COTACAO_ANALISE** presentes; guia "Como usar esta tela" com 4 itens.
- Alerta exibido ao carregar: *"O servidor ainda não tem a chave mestra de criptografia configurada (Ai:EncryptionKey)…"* (auto-dismiss após alguns segundos — evidência já capturada logo após o load).
- "Testar conexão" (`[data-ai-test-btn]`) → **"Nenhuma chave configurada para este provedor (nem do cliente, nem do servidor)."** (= `NAO_CONFIGURADO`); spinner de "Processando…" removido após a resposta (sem resíduo visível).
- Contraste: kicker do card **5,76**, título **14,91**, labels **14,55** (AA+).
- Responsivo 390: 3 cards empilhados, card com 324 px, sem estouro.

---

## 3. Perfil B — Gestor de suprimentos, tenant Santa Casa (`gestor@santacasa-demo.example`)

### B1 — Cotação (Detalhes) — `GET /Administrativo360/Cotacoes/Detalhes/14268164-ee46-4e6a-8186-bcd00afebe7d`
Cotação real **B7X-a9f0542** (status interno `PRONTA_PARA_ENVIO`).
**Passos manuais**: login como gestor → *Administrativo 360 → Cotações* → abrir B7X-a9f0542 → bloco "Como usar esta tela" → no painel IA clicar "Gerar análise".
**Esperado**: código legível preservado no título; guia de uso nova (ciclo/status/IA/documentos); painel IA escopado à cotação; "Gerar análise" sem chave → `NAO_CONFIGURADO` do provedor do tenant.

**Executado ✅ (evidência DOM)**:
- Título: **"Cotação B7X-a9f0542 — Administrativo 360"** (identificador externo legível mantido).
- Guia nova presente com os 4 itens (Ciclo da cotação / Status interno / Assistente IA / Documentos XML) — texto íntegro confirmado por `textContent`.
- Painel IA com `data-ai-url="/Administrativo360/Cotacoes/14268164…/AnaliseIa"`; clique → *"Fonte: groq · Nenhuma chave configurada para groq. Defina a chave do cliente ou a chave global do servidor."* (= `NAO_CONFIGURADO`, resolução de chave no escopo do tenant) + link "Configurar assistente (administradores)".
- **Bug de contraste encontrado e corrigido aqui**: `.text-warning` (amarelo Bootstrap `#ffc107`) sobre superfícies claras medía **1,63:1** em 4 pontos do header (link do breadcrumb "Cotações", ícone, rótulo `OPMENEXO`, valor `R$ 120,00`). Correção: contra-escopo `body.pp-a360 main .text-warning { color: var(--pp-color-warning,#a15c00) !important }` em `administrativo360-tema-claro.css` → re-medido **4,86–5,19:1** (AA) nos 4 pontos.
- Header: `text-light` já converte corretamente para tinta primária (**15,12:1**) — sem bug.
- Foco: Tab real até o `summary` da guia → `:focus-visible` com anel da marca.
- Responsivo 390: painel padding **13,6px** (query ≤414 nova), título **14,4px**, guia em 1 coluna, âmbar aplicado.

---

## 4. Perfil C — Médico (`medico@plantaopro.local`)

### C1 — Configuração do Assistente IA — `GET /AssistenteIa`
**Esperado**: negado por papel (configuração é de administradores).
**Executado ✅**: redirect para `/Account/AccessDenied?ReturnUrl=%2FAssistenteIa`, título **"Acesso negado"**.

### C2 — Central de Pendências — `GET /Pendencias`
**Esperado**: leitura permitida; painel IA disponível para uso (não para configuração).
**Executado ✅**: painel IA presente; clique → estado **VAZIO** correto; guia "Como usar" presente.

**Nota de estado de tenant (pré-existente)**: o landing do médico cai em `AccessDenied?module=MEDICO_AREA&reason=MODULO_NAO_CONTRATADO` (módulo `MEDICO_AREA` não contratado no tenant Clínica Modelo). Não é defeito do guard — é configuração do tenant — registrado para o backlog/comercial.

---

## 5. Acessibilidade transversal (valores medidos)

**Contraste — todas as amostras novas/revisadas:**

| Local | Par de cores | Razão | Veredito |
|---|---|---|---|
| IA kicker (painel) | `#2563eb` / branco | 5,17 | AA |
| IA título (painel) | `#10243e` / branco | 15,63 | AAA |
| IA nota/meta | `rgb(82,107,120)` / branco | 5,63 | AA |
| Guia: summary | `rgb(18,83,99)` / canvas | 8,05 | AAA |
| Guia: texto | `rgb(64,90,128)` / canvas | 6,57 | AAA |
| Card IA config: kicker/título/labels | várias / claro | 5,76 / 14,91 / 14,55 | AA+ |
| A360 `text-warning` (pós-correção) | `#a15c00` / branco·canvas | 4,86–5,19 | AA |
| A360 header `text-light` (conversão do tema) | `rgb(16,33,58)` / canvas | 15,12 | AAA |

**Desvio declarado (decisão de identidade)**: links da marca `#1f73f1` sobre canvas `#f4f8fb` medem **4,11:1** — levemente abaixo de AA para texto pequeno. Mantido por ser a cor primária fixada pela identidade P5; compensado por sublinhado/contorno em contexto de navegação. Registrado aqui para transparência (não silenciar).

**Teclado/foco**: Tab percorre o documento; `summary` da guia e botões IA com indicador visível confirmado por `:focus-visible` real (após as correções desta rodada).

**Responsividade (medidas reais via iframe same-origin):**

| Largura | Painel IA (padding) | Título | Head do painel | Guia (colunas) | KPI Pendências |
|---|---|---|---|---|---|
| 1440 (zoom p/ janela) | 16px 17,6px (desktop) | 15,2px | linha | 2 | 5 colunas |
| nativa (~785) | 16px 17,6px (desktop) | — | — | — | — |
| 768 | 16px 17,6px | 15,2px | coluna | 2 | — |
| 390 | **13,6px** (≤414 nova) | **14,4px** | coluna | **1** | — |

---

## 6. Regressão automatizada (esta rodada)

| Run | Resultado | Log (`$TEMP/opencode/evidencias-a360/`) |
|---|---|---|
| Suíte completa após edits WP-S4 (1º) | 793/794 — 1 falha transitória `Npgsql 23503` (FK `adm360_parceiros` × `adm360_orcamentos`) no teste de cadastros A360 | `wps4-suite.log` |
| Mesmo teste isolado | **1/1 verde** (888 ms) | `wps4-test-repro.log` |
| Suíte completa (re-run) | **794/794 verdes** (11 s) | `wps4-suite2.log` |

A falha única é flake de estado/ordenação (não reproduz isolado; suíte inteira passa no re-run) — sem relação com os edits (CSS/views).

## 7. Pendências declaradas (não se declara concluído sem execução)

1. **Capturas PNG** por módulo/perfil (`docs/homologacao/capturas-2026-10-03/`) — bloqueadas pela visibilidade da janela do browser; todos os pontos acima já estão instrumentados e prontos para capturar assim que a janela estiver visível (mesmas URL/perfis).
2. **Jornada de IA com dados reais**: `MEU_DIA_RESUMO` exibiu estado VAZIO (o usuário do probe não tinha pendências no período); gerar resumo com lista não vazia requer seed de pendências.
3. **IA com chave real de provedor** (NÃO EXECUTADA — exige credencial comercial; mocks/suíte cobrem os demais estados).
4. **Mobile (app)**: avaliação por execução segue pendente (fora do escopo desta rodada).

## 8. Correções aplicadas nesta rodada (WP-S4) — referências

- `wwwroot/css/design-system/v2146-pending-notifications.css`: `summary:focus-visible` com outline da marca (acessibilidade).
- `wwwroot/css/pages/administrativo360-tema-claro.css`: contra-escopo `.text-warning` → `--pp-color-warning (#a15c00)` sob `body.pp-a360 main`.
- `wwwroot/css/design-system/assistente-ia.css`: media query `@media (max-width: 414px)` (padding/títulos do painel).
- `Views/Pendencias/Index.cshtml`, `Views/Administrativo360/Cotacoes/Detalhes.cshtml`, `Views/AssistenteIa/Index.cshtml`: blocos de ajuda "Como usar esta tela".
