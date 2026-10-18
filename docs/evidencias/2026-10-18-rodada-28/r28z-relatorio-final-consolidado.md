# R28 · r28z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R27 (`b47951b`, suíte 1542).

## Escopo da rodada

Epic **E1** da diretriz (r24a1), fatia zona Adm360 parte 1 (suprimentos/compras/fiscal): jargão técnico
renderizado em linguagem de usuário.

## Commits

| Commit | Conteúdo |
|---|---|
| `cf9659d` | r28b1: 13 pontos em 8 views Adm360 (OrcamentoNovo, ValeNovo, Cotacoes/Relacionamento, DocumentosXml/Detalhes+Importar, Gestao/Monitor, TituloDetalhes, Fiscal/Configurar+Nova) + `R28LinguagemSimplesAdm360ContratoTests` (2 fatos) + r28a0 |

## Destaques da reescrita

- **UUID/GUID saiu de todo label e placeholder** dos formulários operacionais ("UUID do Vendedor",
  "ID Interno UUID", "ID do Pedido de Compra (UUID)", "Identificador do documento de origem (GUID)" →
  "Código do vendedor", "Código interno", "Pedido de compra", "Código do documento de origem").
  O placeholder com formato `00000000-…` continua mostrando o formato esperado sem nomear a tecnologia.
- **"Fila e Transmissões Outbox"** (monitor de cotações) → "Envios para os fornecedores"; estado vazio
  "Nenhuma transmissão outbox registrada." → "Nenhum envio para os fornecedores registrado ainda."
  Regra estrutural no fato: a palavra `outbox` não pode voltar à view do monitor (case-insensitive).
- **Idempotência explicada em português**: "A operação é idempotente e auditada." →
  "Se você repetir a operação, nada é duplicado, e tudo fica registrado para auditoria."
- **Fiscal\Configurar**: saída o caminho de configuração cru (`Fiscal:Credenciais:<nome>` no pool da API) —
  agora "O valor do certificado fica em configuração separada, nunca no banco de dados nem nas telas."
- **SHA-256 mantido** no DocumentosXml/Importar (integridade útil ao auditor); só a redação mudou
  ("hash verificado" → "integridade verificada"). Decisão registrada no r28a0.
- **E6 registrado**: os campos ainda pedem o usuário colar o código; conversão para lookup por nome fica na
  fila do E6 (R32/R33) quando as view-models ganharem as opções.

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Suíte completa: **Total 1544** (= base 1542 + 2 fatos), 1 falha sob carga paralela —
  `SaasGovernancaB6Rodada4Tests.G1b_PrefisGlobais_SomenteFonteUnicaCarregaLiteralEmCodigo`,
  40P01 impasse detectado; **isolate 5× = 5/5 verde** (mesma família documentada dos flakes R24/R25 —
  agora G1b se soma à rota G1a/40P01/ProvisionamentoB5/R19DapperBindingGuard).
- Resíduo verificado: zero ocorrências de `UUID`/`(GUID)`/`outbox`/`por API`/`pool da API` nos 8 arquivos.

## Recon já iniciado para a R29 (E1 demais módulos)

`%TEMP%` + grep global: 16 pontos em 8 arquivos fora de Adm360/portal (AdminSaas, Lgpd, Escalas,
Configuracoes, Financeiro, Pagamentos, Observabilidade, MinhaAssinatura, Comercial) — já reescritos nesta
sessão; fato e suíte no próximo push (r29b1/r29z). Whitelists: comentários de rodada, `data-lookup-endpoint`.

## Estado de casa

- Apps PARADOS, portas livres. `origin/main` sincronizada neste push.
- Fila: **R29 = E1 demais módulos** (fatia aplicada, fechamento pendente); roadmap r24a1 segue
  (R30–R31 E1 resx/valores, R32–R33 E6, R34 E7, R35–R37 E3, R38–R39 E2, R40+ E9).
