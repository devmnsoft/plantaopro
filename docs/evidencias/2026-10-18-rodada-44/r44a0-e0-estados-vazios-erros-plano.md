# R44 · r44a0 — E0 design: estados vazios, carregamento e erros (com E1 revisitada)

Data: 2026-10-18 · Sessão: `opencode` · Base: R43 (`ed80cfe`, suíte 1580).

## Recon dos estados existentes (o que já estava bom)

| Superfície | Estado | Veredito |
|---|---|---|
| `Pendencias\Index` | erro com `role="alert"` + `_EmptyState` "Nenhuma ação necessária" | Bom — mantém |
| `Saude360\Modulo` (jornada clínica toda) | vazio via `_EmptyState` com CTA da ação primária, alerta de erro, KPI "indisponível" com motivo, strings ×4 culturas | Bom — mantém |
| `Shared\Error.cshtml` | "Falha recuperável", instrução honesta sobre reenvio de dados, dois caminhos de saída | Bom — mantém |
| Carregamento | estados server-rendered; listas JS usam padrões existentes do design system (feedback.css) | Fora do alvo desta fatia |

## O gap real corrigido

**Quatro telas de detalhe exibiam JSON cru** quando o registro carregava: `Notificacoes\Details`,
`Hospitais\Details`, `Medicos\Details`, `Especialidades\Details` — cada uma um `pre` com
`JsonSerializer.Serialize(Model?.Data)` (contraste com o resto do produto, onde o detalhe é
sempre campos rotulados). O fluxo `RenderDetails` (`WebListControllerExtensions`) devolve
placeholder apenas em 404/erro; com sucesso o dump ia direto para a tela.

Correção: telas humanas idênticas em estrutura — título próprio, `dl` de campos com rótulos em
português simples, **"Não informado"** quando o dado ausente é legítimo (e-mail, telefone…),
estado claro de indisponível, alerta de erro com a mensagem do serviço e **botão de volta** para a
lista. Nenhum endpoint mudou; só a renderização.

## Decisão

- **D-R44-1**: o campo "Registro" do profissional renderiza `CRM/UF`; unidade sem nome fantasia
  mostra a razão social; valores ausentes nunca aparecem como vazio ou `null`.
- **Contrato permanente**: novo fato trava que NENHUMA view do Web use mais dump JSON
  (`JsonSerializer.Serialize(Model?.Data)` em .cshtml) — regra do E0/E1, não da rodada.

## Gates e suíte

- Rebuild real → multiset 160/160 idêntico ao pristino, 0 erros.
- Classes: R44 + R39 (scan de tenant nas views) + R34/R7DF6 (resx) + higiene.
- Suíte integral base **1583** = 1580 + 3 fatos R44.
