# Rodada 21 — Relatório final consolidado (r21z)

- **Data:** 2026-10-18 · **Base de entrada:** `64287c3` (r20z, ponta de `origin/main`) · **Ponta local desta rodada:** commit deste arquivo · **Diretriz:** "continuar pelas próximas 10 sprints (R19→R28) e avançar também o design do sistema" — push ao fim de cada rodada segue autorização genérica registrada em r19z.

## O que a rodada fechou

| Item | Severidade | Estado | Prova |
|---|---|---|---|
| **F-R21-A** — 3 entradas da whitelist `AcoesSemFormEstatico` eram JS-driven sem evidência versionada | — | Verificadas e documentadas com `file:line` | `AtivarCargo` (`Cargos.cshtml` L256, `formImp.action = inativar ? ... : '/Administrativo360/AtivarCargo'` — mesmo form/parâmetros do estático `InativarCargo`), `AtivarDepartamento` (`Departamentos.cshtml` L231, idêntico), `AnaliseIa` (`Cotacoes/Detalhes.cshtml` L572, `data-ai-url` POST via fetch); evidência gravada nos comentários dos sets + fato permanente exige a URL dinâmica continuar existindo |
| **F-R21-B** — 4 entradas eram **fantasmas**: zero pontos de chamada no Web, mas cadeia BFF→API completa (gap funcional) | P3 | Classificados e registrados | `OrcamentoRejeitar`/`OrcamentoCancelar`/`OrcamentoCancelarReserva`/`ValeCancelar`: busca case-sensitive em todo `PlantaoPro.Web` só retorna as definições + a whitelist; rotas Api vivas confirmadas (`Adm360OrcamentosController` L82/L91/L126, `Adm360ValesController` L143) + resx presentes; fato permanente exige zero ocorrência em qualquer `.cshtml` (botão ligado ⇒ falha ⇒ saída do set obrigatória) |
| **F-R21-C** — whitelist era de confiança (só comentário; sem antivacuidade em nenhum dos sentidos) | — | Instaurado: **whitelist auto-verificada** (D1) | fato `whitelist_acoes_sem_form_estatico_e_auto_verificada` em `R16ScanFormBindingFonteTests`: JS-driven ⇒ ≥1 literal de URL dinâmico em quotes em `Views/**` (piso ≥3); fantasma ⇒ zero ocorrências case-sensitive (piso ≥4); rows obsoletas (entrada que deixou de ser HttpPost c/ escalares implícitos) ⇒ falha; scan principal inalterado (invariante R20: 79/203/76 intacto no mesmo build) |

**Design (diretriz de avançar o design):** o catálogo `AcoesSemFormEstatico` virou contrato de fonte versionado no padrão da casa (ADR ou guarda de fonte — aqui, guarda de fonte; a regra do mapa `docs/arquitetura.md` aceita os dois). Padrão reaproveitável para futuros catálogos de exceção: dividir por classe de risco, gravar evidência `file:line` por entrada e auto-verificar cada entrada em vez de confiar no comentário.

## Gates finais

- Build `dotnet build --no-incremental -c Release`: **0 erros**; multiset de avisos **idêntico** ao pristino (160 entradas, chave `arquivo|mensagem`, `Compare-Object` sem divergência).
- Suíte final: **1523/1523** (base 1522 + 1 fato novo). Dois flakes em corridas completas sob carga paralela antes do verde final — ambos da família conhecida 40P01 (um capturado: `R7OBOnboardingPreferenciasIdiomaTests.O4`, o outro passou na corrida seguinte) — tratados pelo protocolo de casa: isolate 5/5 verde.

## Commits da rodada

| Commit | Conteúdo |
|---|---|
| `1fbdb78` | r21a0 auditoria + classificação das 7 entradas (F-R21-A/B/C) e decisões D1–D3 |
| `93ff024` | r21b1 D1 implementada: 2 sets versionados + fato auto-verificador + `LerTodasAsViews` (484 views) |
| (este) | r21z consolidado |

## Observações abertas (não bloqueiam; seguem na fila)

1. **Fantasmas P3 (fila R22)**: os 4 Adm360 classificados nesta rodada + fantasma legado `Pendencias?module=OCORRENCIAS` (herdado da R19). Insumo de decisão já registrado (D2): a cadeia BFF→API está completa — o caminho mais barato é **ligar os botões** nas views de detalhe (`OrcamentoDetalhes`/`OrcamentoReserva`/`ValeDetalhes`) e recuperar as funções (rejeitar/cancelar orçamento, cancelar reserva, cancelar vale), não remover a cadeia.
2. **Sessão portal ADMINISTRADOR_CLIENTE (gestor)** — herdada da R19, continua reproduzível; segue bloqueando smoke de UI das páginas Adm360 para o tenant Santa Casa.
3. **Espelho aguardando a Api (R8-B27)**: allowlist versionada no contrato BFF (obsoletos quando a Api emitir) — estado inalterado.

## Fila R22 (plano das 10 sprints)

1. **Fantasmas P3**: decisão ligar × remover para os 4 Adm360 (insumo D2 favorece ligar) + limpeza do fantasma legado `Pendencias?module=OCORRENCIAS`.
2. Padrões de casa mantidos: commits `<secoes> (rodada 22 r22xx)` sem acentos, gate de casa antes de cada commit, evidência versionada em `docs/evidencias/<data>-rodada-22/`, push ao fim da rodada (credencial local wincred fixada na R20 evita o hang do GCM).
