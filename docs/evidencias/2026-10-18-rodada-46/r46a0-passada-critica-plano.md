# R46 · r46a0 — Passada crítica geral: regressão de UX + limpeza de pendências acumuladas

Data: 2026-10-18 · Sessão: `opencode` · Base: R45 (`0ea6bb6`, suíte 1585).

## Pendências acumuladas (inventário da passada)

| # | Pendência | Origem | Tratamento nesta rodada |
|---|---|---|---|
| 1 | Ícones duplicados no menu lateral — Coverage ×3, User ×4, Report ×3, Finance ×2, Calendar/Consultation em pares da mesma seção | aberta em r43a0 | **FECHADA**: mapa único por item; 3 pares residuais conceituais declarados |
| 2 | Famílias flake sob carga (ProvisionamentoB5.P2/P3, R7OB.O1/O3, e agora R19.Dapper CentralClientes) | R38–R45 | protocolo imutável mantido (isolate 5× antes de atribuir); consolidação em snapshot único segue como candidata quando a carga diminuir |
| 3 | Uso-vs-limite do plano sem chip perto das ações de criação | proposta R42 | fila para E9 (após faturamento R47/cobrança) |
| 4 | Convite aceito/onboarding criam usuários fora do guard de criação manual | r40b1 | comportamento documentado e intencional (transações próprias); sem mudança |

## Correção b1: ícones únicos no menu

Regra adotada: **um ícone por item**, tirados do `AppIconRegistry`; pares residuais ficam
DECLARADOS no fato (dicionário `LimitePorIcone`), do tipo (a) família de significado com rótulo
de texto distinto — `Coverage` = Cobertura × Usuários (pessoas), `Doctor` = Jornada Clínica ×
Profissionais (médico), `Payment` = Pagamentos × Minha Assinatura (dinheiro) — ou (b) audiência
exclusiva (o Razor nunca exibe os dois juntos: um item guarda por `!isGlobalAdmin`, o outro só
renderiza para admin global) — `User` = Perfis (tenant) × Usuários e Acessos (global),
`WorkItem` = Implantação (tenant) × Assinaturas (global). Nenhum outro ícone pode repetir.

Trocas aplicadas (20): MinhaCentral→Home, Dashboard→Dashboard (era Report), CommandCenter→Recent,
Agenda→Schedule, Convites→Add, Escalas→History, Jornada→Doctor, Agendamentos→Prescription,
Triagem→Waiting, Visão Financeira→View, Caixa→Unlock, Admin360→More, Médicos→Doctor,
Hospitais→Hospital, SaasDashboard→Triage, Clientes→Comment, Planos→Document, Assinaturas→WorkItem,
Módulos→Filter, Usuários e Acessos→User, Perfis Globais→Audit, Observabilidade→Information,
Auditoria→Refresh.

Segurança extra: como o `AppIconTagHelper` faz bind TYPED (`AppIconKey`), nome inventado só
falharia em runtime — o fato novo também valida todo nome usado contra o enum (erro vira falha de
teste, não de produção).

## Decisões

- **D-R46-1**: os 5 pares residuais (3 conceituais + 2 de audiência exclusiva) são exceção
  DECLARADA (dicionário `LimitePorIcone` no fato), não buraco na regra — trocar de novo exigiria
  leitura visual em dispositivo e volta para a R47+ se aparecer sinal real.
- **D-R46-2**: nada de lógica/rota/módulo mudou nesta fatia — só apresentação. Gate multiset
  continua a trava de regressão de build.

## Gates e suíte

- Rebuild real → multiset 160/160 idêntico ao pristino, 0 erros.
- Classes: R46 + R43 (navegação) + R45 (responsividade) + R39 (tenant) + higiene.
- Suíte integral base **1587** = 1585 + 2 fatos R46.
