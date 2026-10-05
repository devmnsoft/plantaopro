# WP-B — Jornadas de Homologação Funcional (2026-10-05)

## Ambiente e método

| Item | Valor |
|---|---|
| Baseline | `c15456d` (local `main`; `origin/main` ainda `82b8c26`, sem push nesta rodada) |
| Stack dev | API `https://localhost:51977` · Web `https://localhost:52977` (`scripts/local/run-dev-start.ps1`) |
| Banco | `plantaopro_test` (PostgreSQL) · Tenant Santa Casa `d3f6584c-2c64-4e5a-9ea9-4e1428647502` · Tenant Clínica Modelo (outro tenant) |
| Usuários | Gestor `gestor@santacasa-demo.example` · Auditor (perfil somente leitura) · Clínica `admin.clinica@plantaopro.local` (ver `docs/usuarios-teste.md`) |
| Interação | Navegador real (OpenCode desktop, viewport ~890px); formulários via `requestSubmit()`, POSTs determinísticos via `fetch` + `FormData` + header `RequestVerificationToken`; resposta server-side lida de `#pp-toast-data` |
| Persistência | Toda mutação confirmada com consulta SQL direta ao banco (`psql`) após a ação |
| Suíte canônica | 839 testes — verde após o fix deste pacote (`suite-839-pos-fix-createdat.log` neste diretório) |
| Capturas | PNG reais do pane do navegador, salvas neste diretório |

Legenda do resultado: **PASS** = esperado observado e persistido · **PASS+FIX** = falha real encontrada na jornada e corrigida no próprio pacote.

## Matriz por módulo

### M1 — Autenticação, perfis e módulos

| # | Cenário | Esperado | Resultado | Evidência |
|---|---|---|---|---|
| M1.1 | Login gestor (credenciais válidas) | Sessão criada, redirecionamento para home | PASS | `m1-1-login-sucesso-gestor.png` |
| M1.2 | Login com senha inválida | Erro exibido junto ao campo, sem sessão | PASS | `m1-2-login-senha-invalida.png` |
| M1.3 | Auditor (somente leitura) faz POST de importação XML | Negado por permissão | PASS | `m1-3-auditor-post-importar-negado.png` |
| M1.4 | Tenant Clínica abre rota ADM360 | `AccessDenied?module=ADM360&reason=MODULO_NAO_CONTRATADO` | PASS | `m1-4-accessdenied-modulo-nao-contratado-clinica.png` |

### M2 — Cadastros master (parceiros, produtos, locais)

| # | Cenário | Esperado | Resultado | Evidência |
|---|---|---|---|---|
| M2.1 | Form parceiro válido (Fornecedor Jornada WPB, doc `12345678000195`) | Criado + toast de sucesso + linha no banco | PASS | `m2-1-form-parceiro-valido.png`, `m2-1-parceiro-criado-sucesso.png` (parceiro `d2ec5f24…`) |
| M2.2 | POST com nome inválido | Rejeitado com validação junto ao campo | PASS | `m2-2-post-nome-invalido-rejeitado.png`, `m2-2-resposta-servidor-nome-invalido.txt` |
| M2.3 | Parceiro com CNPJ duplicado | Rejeitado como duplicado | PASS | `m2-3-parceiro-cnpj-duplicado-rejeitado.png` (Dup2 `c4ae4c54…`, doc `98765432000110`, CNPJ próprio) |
| M2.4 | Dois POSTs de parceiro simultâneos (concorrência) | Exatamente um sucesso | PASS | `fetch` em paralelo; 1 sucesso + 1 rejeição no banco |
| M2.5 | Produto WPB-001 criado | Criado + toast de sucesso | PASS (com finding decimal) | `m2-5-produto-criado-sucesso.png` (produto `0d96cc88…`) |
| M2.7 | Local ALMOX-WPB (INTERNO) criado | Criado + toast de sucesso | PASS | `m2-7-local-criado-sucesso.png` (local `dd4d901c…`) |

### M3 — Documentos XML (NF-e) e cadeia de compras

| # | Cenário | Esperado | Resultado | Evidência |
|---|---|---|---|---|
| M3.1 | Importar `nfe-valida.xml` (chave de 44 dígitos) | NFE_COMPLETA, importado com sucesso | PASS | `m3-1-nfe-valida-importada.png` (doc `33caa57b…`) |
| M3.2 | Detalhes da NF-e parseada | Emitente, CNPJ, chave, destinatário, valor corretos | PASS | `m3-2-detalhes-nfe-parseada.png` |
| M3.3 | Conferência com destinatário autorizado | Conferência confirmada, sem quarentena | PASS | `m3-3-conferencia-autorizada.png` |
| M3.4 | NF-e com destinatário fora do tenant | Quarentena `DESTINATARIO_NAO_AUTORIZADO`, DIVERGENTE, hash persistido | PASS | `m3-4-quarentena-dest-fora.png`; SQL: `quarentena=t`, `xml_hash` = sha256 do arquivo (doc `bba4b9bf…`, chave `…0002`) |
| M3.5 | XML malformado | Quarentena `XML_MALFORMADO`, chave sintética `MALF…`, DIVERGENTE | PASS | `m3-5-quarentena-malformada.png`; SQL: doc `09dea27c…`, chave `MALF1be6…` |
| M3.6 | Reimportação do mesmo arquivo (bytes idênticos) | Duplicado ignorado, contagem da chave = 1 | PASS | Toast "1 duplicado(s) ignorado(s)"; SQL: count da chave = 1 (sem captura; prova por banco) |
| M3.7 | Cadeia completa de compras (ver abaixo) | Pedido → vinculação → confirmação → inspeção, tudo persistido | **PASS+FIX** | `m3-7-detalhes-vinculado.png`, `m3-7-eventos-rastreabilidade.png`, `m3-8-inspecao-fila.png` + SQL a cada etapa |
| M3.8 | Fila de inspeção após decisão | Item resolvido sai da fila | PASS | `m3-8-inspecao-fila.png` (fila com 276 itens demo pendentes; item WPB 2/2 decidido não aparece) |

Detalhamento M3.7 (todas as etapas confirmadas via toast + SQL):

1. **Pedido PC-32** criado pela UI (fornecedor WPB, produto WPB-001, 2 × R$ 55,00) e aprovado → "Valores congelados". Pedido `2a0453c4…`, item `caf5e30f…`.
2. **VincularRecebimento** (documento M3.1 + PC-32 + ALMOX-WPB) → documento `VINCULADO`; evento `VINCULACAO_RECEBIMENTO`; recebimento de vinculação `REC-NFE-<chave>` sem itens. **FALHA ENCONTRADA AQUI ANTES DO FIX**: o INSERT usava coluna inexistente `criado_em` (schema tem `created_at`) — toda vinculação nova quebrava em runtime. Corrigido em `backend/PlantaoPro.Infrastructure/Administrativo360/DocumentosXmlRepository.cs`.
3. **ConfirmarRecebimento** (qtd 2, lote `LOT-WPB-01`, validade 2028-10-05) → recebimento físico com item `QUARENTENA`, movimento de entrada em quarentena (+2), título a pagar **PAG-000582** (R$ 110,00, aprovado) e pedido → `RECEBIDO` (2/2). Recebimento `a2626870…`, lote `320116bb…`, item de recebimento `695e7164…`.
4. **DecidirInspecao** (aprovada 2, reprovada 0) → inspeção registrada (idempotente por chave); pernas de estoque: saída de quarentena −2 / entrada liberado +2.
5. **Rastreabilidade**: os 3 eventos ordenados (importação, conferência, vinculação) visíveis na tela — `m3-7-eventos-rastreabilidade.png`.

### M4 — Concorrência e isolamento multi-tenant

| # | Cenário | Esperado | Resultado | Evidência |
|---|---|---|---|---|
| M4.1 | Dois `VincularRecebimento` simultâneos para o MESMO documento (doc `a3600000…050` + pedido `a3610000…008` + ALMOX-WPB) | Exatamente um sucesso; o outro negado pela regra de negócio | PASS | Toasts: 1 "Documento vinculado ao recebimento de compra com sucesso." + 1 "Este documento fiscal já está vinculado a um recebimento físico confirmado."; SQL: documento `VINCULADO`, exatamente 1 evento de vinculação |
| M4.2 | Tenant Clínica abre por ID o documento do Santa Casa (`33caa57b…`) | Sem vazamento entre tenants | PASS | `AccessDenied?module=ADM360&reason=MODULO_NAO_CONTRATADO` (`m4-2-outro-tenant-nao-contratado.png`) + verificação por código: todas as consultas por ID no repositório somam `AND tenant_id = @tenantId` (ex.: `DocumentosXmlRepository.cs:95`) |

## Findings

| # | Finding | Severidade | Estado |
|---|---|---|---|
| 1 | **Coluna inexistente `criado_em`** no INSERT de `adm360_recebimentos` em `VincularRecebimentoAsync` — toda vinculação nova falhava em runtime (schema usa `created_at`). | Bloqueante | **Corrigido neste pacote** (regressão coberta pela suíte; jornada M3.7 refeita no build novo) |
| 2 | Decimal pt-BR: ponto lido como milhar no custo do produto ("12,50" digitado vira 1250) — valor ×100 no banco. | Alto | Backlog (regras de parse/format) |
| 3 | Mensagem de validação vaza `(Parameter 'precoCusto')` para o usuário. | Médio | Backlog |
| 4 | Sem UNIQUE constraint em `chave_acesso`; dedup só por string exata em aplicação (M3.6 passa por isso). | Médio | Backlog (risco em escrita concorrente) |
| 5 | Dois eventos com `sequencia_evento=2` convivem no mesmo documento (`CONFIRMACAO_CONFERENCIA` e `VINCULACAO_RECEBIMENTO`) — gerador de sequência desalinhado. | Médio | Backlog (audit trail) |
| 6 | Duas linhas de recebimento por documento: placeholder `REC-NFE-<chave>` (vinculação, sem itens) vs `REC-<guid>` (confirmação, com itens); `documento.recebimento_id` aponta para o placeholder. | Médio | Backlog (decisão de design do vínculo) |
| 7 | JSON cru de `#pp-toast-data` fica visível como texto na página (escape/aplicação do toast). | Baixo | Backlog (UX) |
| 8 | Label duplicado no modal de parceiro. | Baixo | Backlog (UX) |
| 9 | Viewport estreito: blocos genéricos ("Página", "Jornada da instituição") ocupam a dobra inicial e empurram o conteúdo da Detalhes para baixo — exige rolagem. | Baixo | Backlog (layout; avaliado no WP-C) |
| 10 | Cookie de sessão expira no restart do servidor (sliding expiration) — documentado para o roteiro de homologação. | Informativo | Registrado |
| 11 | Teste flaky na suíte: `Administrativo360CotacoesXmlDashboardTests.Aceite12_XmlRepetido_NaoDuplica` falhou 1× na execução sequencial e passou isolado e em rerun completa (839/839). Suspeita de ordem/paralelismo xUnit, não de produto. | Informativo | Monitorar |

## Linhas persistidas (rastreamento)

- Parceiros: `d2ec5f24…` (Fornecedor Jornada WPB) · `c4ae4c54…` (Dup2)
- Produto WPB-001: `0d96cc88…` · Local ALMOX-WPB: `dd4d901c…`
- Documentos XML: `33caa57b…` (NF-e válida, VINCULADO) · `b0ace792…` / `0e9bee29…` (INCOMPLETO) · `bba4b9bf…` (quarentena dest. fora) · `09dea27c…` (malformado) · `a3600000…050` (M4.1, demo CONFERIDO → VINCULADO)
- Pedido PC-32: `2a0453c4…` (RECEBIDO) + item `caf5e30f…` (2/2)
- Recebimentos: `9b5bcaf9…` (vinculação, sem itens) · `a2626870…` (confirmação, item `695e7164…` QUARENTENA 2/2 decidida)
- Lote `320116bb…` (LOT-WPB-01) · Título PAG-000582 (R$ 110,00)

## Prontidão

- **Prontidão técnica**: suíte 839/839 verde no build com o fix; gate anterior fechado (`c15456d`).
- **Homologação funcional**: módulos M1–M4 desta matriz executados com persistência real e capturados; pendências listadas nos findings (nenhuma bloqueante aberta).
- **Liberação de produção**: ainda depende dos próximos blocos (WP-C design, auditoria de segurança por ID/rotas, evidências consolidadas e instruções de homologação).
