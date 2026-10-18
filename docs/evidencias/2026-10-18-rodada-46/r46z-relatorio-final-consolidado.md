# R46 · r46z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R45 (`f156149`, suíte 1585).

## Escopo da rodada

Roadmap R46: "Passada crítica geral" — regressão de UX + limpeza de pendências acumuladas.

## Commits

| Commit | Conteúdo |
|---|---|
| `39070b5` | r46b1: deduplicação de ícones no menu lateral — 20 trocas para mapa único por item, 5 pares residuais declarados (3 conceituais: Coverage/Doctor/Payment; 2 de audiência exclusiva: User/WorkItem) + contrato permanente de unicidade + validação de todo nome contra `AppIconKey` + r46a0 com inventário de pendências |
| (docs) | este r46z + r41z/r43z retroativos |

## Invenção da passada (o que estava acumulado e o que se fez)

| # | Pendência | Origem | Tratamento |
|---|---|---|---|
| 1 | Ícones duplicados no menu lateral | aberta em r43a0 | **FECHADA** nesta rodada (D-R46-1/2) |
| 2 | Famílias flake sob carga | R38–R45 | protocolo imutável mantido (isolate 5× antes de atribuir); nova família documentada em r45z (`R19DapperBindingGuardTests.Db_CentralClientes_…`); consolidação em snapshot único segue candidata quando a carga diminuir |
| 3 | Chip de uso-vs-limite perto das ações de criação | proposta R42 | fila para E9 (após faturamento R47 / cobrança) |
| 4 | Convite aceito/onboarding criam usuários fora do guard de criação manual | r40b1 | comportamento documentado e intencional; sem mudança |

## Decisões fixas

- **D-R46-1**: regra "um ícone por item" tirada do `AppIconRegistry`; pares residuais são exceção
  DECLARADA no fato (`LimitePorIcone`), não buraco: (a) família de significado com rótulo distinto —
  `Coverage` (Cobertura × Usuários), `Doctor` (Jornada × Profissionais), `Payment` (Pagamentos × Minha Assinatura);
  (b) audiência exclusiva (o Razor nunca exibe os dois juntos: um guarda por `!isGlobalAdmin`, o outro só
  renderiza para admin global) — `User` (Perfis tenant × Usuários e Acessos global), `WorkItem`
  (Implantação tenant × Assinaturas global).
- **D-R46-2**: nome de ícone inválido falharia só em runtime (bind tipado do `AppIconTagHelper`);
  o fato valida todo nome usado contra o enum → erro vira falha de teste.
- Só apresentação mudou — zero mudança de lógica/rota/módulo.

## Ajuste pós-verde (antes do commit b1)

- Primeiro run das classes: 1 falha — o fato acusava `User ×2` sem estar no dicionário. O par era real
  mas de AUDIÊNCIA EXCLUSIVA (Perfis tenant × Usuários e Acessos global, nunca exibidos juntos); `WorkItem`
  seguia o mesmo padrão. Ambos declarados no `LimitePorIcone` com a justificativa — 17/17 verde na sequência.

## Gates e suíte

- Rebuild real → multiset **160/160**, 0 erros, gate limpo.
- Classes dependentes: **17/17 verde** (R46 + R43 + R45 + R39 + higiene).
- Suíte integral (base **1587** = 1585 + 2 fatos): **1587/1587, exit 0, zero flakes**.

## Estado de casa

- Apps PARADOS, portas livres. Push confirmado: `8ce28b9..39070b5` (main sincronizada).
- Fila: **R47 E9 faturamento/cobrança** — recon pronto: a tela do cliente (`Minha Assinatura → Faturas`)
  é uma tabela estática sem ação de pagamento, enquanto o motor de cobrança (`faturas_saas` + sandbox
  checkout + webhook) já existe e opera do lado MNSOFT; o gap é o fluxo self-service do cliente.
