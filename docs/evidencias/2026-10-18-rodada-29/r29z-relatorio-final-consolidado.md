# R29 · r29z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R28 (`eb00499`, suíte 1544).

## Escopo da rodada

Epic **E1** da diretriz (r24a1), fatia "demais módulos" — **fecha a varredura de jargão técnico
renderizado em TODAS as zonas da UI** (portal R27, Adm360 R28, demais módulos R29).

## Commits

| Commit | Conteúdo |
|---|---|
| `2c70d2c` | r29b1: 19 pontos em 9 views (AdminSaas, Lgpd/Eventos, Escalas/Index, Configuracoes/Index, Financeiro/Index, Pagamentos/Index, Observabilidade/Erros, MinhaAssinatura/Index, Comercial/PrevisaoReceita) + `R29LinguagemSimplesDemaisModulosContratoTests` (2 fatos) + r29a0 |

## Destaques da reescrita

- Padrão "**X pela API**" extinto de toda a UI renderizada (restam só comentários Razor/HTML de rodada —
  whitelist versionada no r29a0 e nos fatos).
- **Endpoints crus e nome de tabela** saíram do texto: Lgpd ("lgpd_eventos_privacidade" + /api/lgpd/eventos →
  "registrados no sistema, consultados nesta área"), Comercial (endpoint da previsão → "do seu funil comercial").
- **"billing" em inglês** em exibição → "Cobranças" (MinhaAssinatura eyebrow + hero; AdminSaas card de auditoria).
  Identificadores de código `id="billing-title"`/`data-billing-*` seguem whitelist (não são texto).
- MinhaAssinatura ganhou texto de dono: "Consulte somente as informações do contrato da sua organização."

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Suíte completa: **Total 1546** (= base 1544 + 2 fatos), 1 falha sob carga paralela —
  `R19DapperBindingGuardTests.Db_CentralClientes_SqlCompletaDaFonte_MaterializaItensETotais`
  (race entre selects fora de transação, documentada desde R24); **isolate 5× = 5/5 verde**.
- Fato com 1 ajuste de literal (marcador positivo com "somente" minúsculo — corrigido antes do commit;
  classe R29 2/2 verde no build final).

## Recon já iniciado para a R30 (E5 formulários parte 1 — auditoria + validação dos cadastros)

Scan de validação em 59 views com `<form` (`%TEMP%\opencode\r30_scan_formas.ps1`): 342 inputs,
apenas 164 `required`, 43 `maxlength`, 3 `pattern`; handler JS de submit em ~6 views. Tamanhos de coluna
levantados no banco (`plantaopro`): parceiro nome 160/documento 20; produto sku 40/nome 180/unidade 12/
codigo_barras 80/anvisa_numero 60; lote codigo 80; local codigo 30/nome 120/tipo 10; cargo codigo 30/nome 120;
colaborador cpf char(11)/email 180/matricula 30/nome 160; departamento codigo 30/nome 120. Regra D-R30-1
(nativa: required + maxlength = tamanho da coluna + pattern/min; sem framework novo) no r30a0.

## Estado de casa

- Apps PARADOS, portas livres. `origin/main` sincronizada neste push.
- Fila: **R30 = E5 parte 1** (cadastros); roadmap r24a1 segue (R31 E5 operações, R32–R33 E6, R34 E7,
  R35–R37 E3, R38–R39 E2, R40+ E9).
