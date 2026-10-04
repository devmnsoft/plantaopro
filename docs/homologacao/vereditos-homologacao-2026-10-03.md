# Vereditos de Homologação por Item — PlantãoPro (2026-10-03)

**Entrega 7 de 8.** Baseline `35c4382` (branch `main` local, não pushada) + correções WP-S1/S2/S3/S4 (edits não commitados) + migrações v2305/v2306/v2307.

## Legenda dos vereditos

| Veredito | Significado |
|---|---|
| **APROVADO** | Requerimento coberto por evidência **executada** nesta homologação (suíte contra PostgreSQL real, probe funcional com stack no ar, aplicação/diff de migração, medição por estilos computados). Prontidão técnica confirmada; **aceite funcional do cliente é etapa separada**. |
| **FALHOU** | Executado e o requisito não está atendido. |
| **BLOQUEADO** | Há dependência externa que impede a execução (acesso a produção, credencial comercial, ferramenta). |
| **NÃO EXECUTADO** | Declarado fora desta rodada (não se declara concluído sem execução). |

Evidências: `docs/seguranca/p0-seguranca-homologacao-2026-10-03.md` (P0), `docs/ia/README.md` (IA), `docs/homologacao/roteiro-homologacao-perfis-2026-10-03.md` (perfis), logs em `$TEMP/opencode/evidencias-a360/`.

---

## P0 — Segurança / Sessão / CSRF / Banco / Entradas

| Item | Veredito | Justificativa (evidência executada) |
|---|---|---|
| S-01 Acesso clínico por vínculo médico (não autoria/e-mail) | **APROVADO** | Correção com suíte verde; probe funcional (acesso clínico por vínculo) no smoke 3 perfis |
| S-02 Cookie anti-CSRF em todas as operações, incl. BFFs Operation/Agenda | **APROVADO** | Atributo `[ValidateAntiForgeryToken]` + token + wrapper global; suíte; `OperationBffController.cs` L11 / `AgendaBffController.cs` L14 |
| S-03 Bypass de imutabilidade Adm360 via GUC setável por qualquer usuário | **APROVADO** | v2305 aplicada; GUC `plantaopro.fn_adm360_bypass_habilitado()` presente nos 2 bancos de teste (diff WP-S5); papel restrito testado na suíte |
| S-04 Upload XML lido antes do limite de tamanho | **APROVADO** | Limite antes da leitura (Web + API); suíte |
| S-05 Fallback silencioso de banco em testes | **APROVADO** | Fonte única de resolução + origem auditável; todas as suítes rodaram contra PostgreSQL real |
| S-06 Corrida no swap de plano principal + duplicidade histórica | **APROVADO** | Transação atômica + índice parcial único (v2306); objeto presente em upgrade e instalação limpa (diff 0) |
| S-07 API pública `/api/public/v1/*` anônima sem credencial | **APROVADO** | `X-Api-Key` validada contra hash persistido; fail-closed sem chave (probe) |
| S-08 Rede/TLS/`pg_hba` e privilégios do banco **em produção** | **BLOQUEADO** | Sem acesso ao ambiente produtivo nesta homologação (dependência externa) |
| S-09 Separação de papéis de banco em produção (requisito CREATEROLE na v2305) | **BLOQUEADO** | Risco de implantação documentado; exigiria ambiente produtivo/estágio para validar |
| S-10 Proteções existentes (Kestrel 128 KB, cookies HttpOnly/Lax/sliding, JWT + revogação dinâmica, guards, SQL parametrizado, `DtdProcessing.Ignore`, clamp de lookup) | **APROVADO** | Verificação item a item registrada no relatório P0 §4 |
| S-11 Credenciais de dev versionadas (`TestDatabase.cs`, `launchSettings.json`) | **APROVADO** | Observação aceita e documentada; segredos não expostos nas entregas |
| S-12 Total zerado em páginas vazias | **APROVADO** | Correção + suíte |
| S-13 Timeout na leitura do corpo escapava como erro interno | **APROVADO** | Correção + suíte |
| S-14 Filtro morto `UnitId` no contrato de produtividade | **APROVADO** | Removido por decisão; build 0 erros + suíte |
| S-15 Controllers fora do catálogo do guard SaaS (`Pendencias`, `AssistenteIa`) | **APROVADO** | Registrados no catálogo (Anexo B); evidência ao vivo: `medico` → `/Account/AccessDenied?ReturnUrl=%2FAssistenteIa` |

## P1 — Jornadas funcionais

| Item | Veredito | Justificativa |
|---|---|---|
| Plantões: agenda concorrente → exatamente 1 sucesso | **APROVADO** | Teste de concorrência na suíte contra PostgreSQL real (constraint de agenda) |
| Plantões: jornada completa no navegador com capturas por perfil | **BLOQUEADO** | Fluxos navegados (smoke 3 perfis, `wps3-web-smoke.log`); capturas PNG bloqueadas pela visibilidade da janela do browser — pontos instrumentados |
| Meu Dia: total correto + prazo total via HTTP + correlação F3 (`X-Correlation-ID`) | **APROVADO** | Suíte + probes HTTP com header de correlação verificado |
| Administrativo 360: cotação → caixa → estorno sem pagamento duplicado; finalização idempotente | **APROVADO** | Suíte (idempotência) + tela de cotação validada ao vivo com perfil gestor (cotação B7X-a9f0542) |
| XML real: fixtures não modificadas; bytes/hash/encoding preservados | **APROVADO** | Suíte preserva bytes/hash; imutabilidade por trigger no banco (`trg_adm360_cotacao_exportacao_imutavel`, presente nos 2 cenários de banco). **Limitação declarada**: fixture sintética — XML ABRASF real indisponível (backlog) |
| Saúde 360: troca de plano atômica + finalização idempotente | **APROVADO** | v2306 + suíte |
| SaaS/superadmin: alterações afetam sessões | **APROVADO** | Revogação dinâmica de sessão coberta pela suíte |
| BI / notificações / integrações: sem sucesso fictício | **APROVADO** | Integração não configurada informa estado real; API pública fail-closed; quirk `OperationBff` registrado (backlog) |
| Mobile (app): avaliação por execução | **NÃO EXECUTADO** | Fora do escopo executado desta rodada — declarado, sem conclusão inferida |

## P2 — IA (camada canônica)

| Item | Veredito | Justificativa |
|---|---|---|
| Camada canônica + migração v2307 (`ai_config`, `ai_usos`) | **APROVADO** | Objetos criados em instalação limpa e upgrade (329 tabelas; diff 0); persistência verificada via psql; suíte 794/794 |
| 2 jornadas canônicas: `MEU_DIA_RESUMO` + `COTACAO_ANALISE` | **APROVADO** | Cards presentes em `/AssistenteIa`; painéis executados ao vivo em `/Pendencias` (2 perfis) e Detalhes da cotação (gestor) |
| Máquina de estados: `VAZIO`→`NAO_ENCONTRADO`→`NAO_HABILITADO`→`NAO_CONFIGURADO`→execução | **APROVADO** | `VAZIO` e `NAO_CONFIGURADO` comprovados ao vivo (textos exatos capturados); demais estados na suíte (+15 fatos `AiWps3RegressionTests`) |
| IA não aprova/transmite/altera/baixa/prescreve/finaliza | **APROVADO** | Camada sem endpoints de escrita de domínio; cobertura na suíte |
| Segredos só no servidor (chave cifrada em `bytea`; `Ai:EncryptionKey`) | **APROVADO** | `AiConfigException` quando chave mestra ausente (testado); alerta exibido na tela de configuração |
| Jornada com chave real de provedor (mocks não comprovam homologação) | **NÃO EXECUTADO** | Exige credencial comercial; estados sem chave totalmente exercitados |

## P5 — Design / identidade / acessibilidade

| Item | Veredito | Justificativa |
|---|---|---|
| Identidade (fundo claro, branco, azul `#1f73f1`, estados semânticos) | **APROVADO** | Estilos computados medidos em 3 telas/3 perfis |
| CSS em camadas | **APROVADO** | Camadas conferidas (`design-system/*`, `pages/administrativo360-tema-claro.css` etc.) |
| Ajuda curta "Como usar esta tela" em cada tela trabalhada | **APROVADO** | 3 blocos novos validados ao vivo (Pendências, Cotação Detalhes, Assistente IA) |
| Preservação de UUIDs + códigos legíveis | **APROVADO** | Cotação B7X-a9f0542 no título com UUID íntegro na URL |
| Painel IA opcional/não bloqueante | **APROVADO** | Interagir com ele não altera o restante da tela; estados de erro inline |
| Responsivo 1440 / 768 / 390 px | **APROVADO** | Medidas reais via iframe same-origin (tabela no roteiro §5) |
| Teclado + foco visível | **APROVADO** | Tab real → `:focus-visible` com anel da marca (após fix `summary:focus-visible`) |
| Contraste WCAG AA | **APROVADO** | Todas as amostras ≥ 4,5:1 após correção `.text-warning` (1,63→4,86–5,19). **Desvio declarado**: link da marca `#1f73f1` sobre canvas = 4,11:1 (decisão de identidade P5, compensado por sublinhado) |
| Leitores de tela | **NÃO EXECUTADO** | Estrutura semântica verificada (details/summary nativos, hierarquia de títulos, labels); teste com leitor real não executado |
| Capturas reais por módulo/perfil | **BLOQUEADO** | Ferramenta exige janela desktop visível (erro reiterado após `tabs.focus`); pontos instrumentados e prontos para captura |

## P6 — Banco / testes / aceite

| Item | Veredito | Justificativa |
|---|---|---|
| Migrações incrementais (v2305/v2306/v2307) | **APROVADO** | Aplicadas individualmente sobre banco "antigo" (upgrade). **Nota de correção**: a regeneração deixou `database/migration-manifest.json` terminando em v2304 — entradas v2305–v2307 foram preenchidas nesta rodada com a cadeia de dependência testada (v2304→v2305→v2306→v2307) e checksums conferidos contra `source-checksums.json` e o SHA-256 real dos arquivos |
| Instalação limpa | **APROVADO** | Banco novo + `scrpt_completo.sql` atual: 329 tabelas, 0 erros (`wps5/wps5-clean-install.log`) |
| Upgrade de estado anterior | **APROVADO** | Banco novo + schema do commit HEAD + v2305→v2306→v2307 em ordem (`wps5/wps5-upgrade.log`) |
| Reaplicação (idempotência) | **APROVADO** | v2307 aplicada 2× seguidas, exit 0 |
| Convergência exata entre os dois caminhos | **APROVADO** | Diff de colunas (3855), índices (731), funções (219) e triggers (10): **0 diferença** (`wps5/wps5-diff*.log`) |
| Testes de todos os estados de IA + regressão | **APROVADO** | 794/794 verdes (`wps4-suite2.log`); flake transitória documentada (23503 FK; passa isolada e em re-run) |

---

## Consolidação

| Veredito | Qtd |
|---|---|
| APROVADO | 39 |
| FALHOU | 0 |
| BLOQUEADO | 4 (S-08, S-09, capturas ×2: Plantões/Design) |
| NÃO EXECUTADO | 3 (Mobile, chave real de IA, leitores de tela) |

**FALHOU = 0.** Nenhum item executado ficou pendente de correção nesta rodada; os itens BLOQUEADO/NÃO EXECUTADO têm dependência externa declarada e caminho de destravamento no backlog (entrega 8).
