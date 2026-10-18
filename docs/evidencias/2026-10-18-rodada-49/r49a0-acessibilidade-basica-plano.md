# R49 · r49a0 — Acessibilidade básica (aria/labels/foco) + contraste revisitado

Data: 2026-10-18 · Sessão: `opencode` · Base: R48 (`33c4c33`, suíte 1594).

## Recon — estado atual

| Camada | Estado |
|---|---|
| Layout | **OK**: `lang` nos 4 `<html>`; skip-link presente em `_Layout` e `_AuthLayout` (`Common.SkipLink`); `main#mainContent`; botão de overlay já tem `aria-label` |
| CSS fundação | **Existe** (`design-system/accessibility.css`): `.skip-link`, outline `:focus-visible` 3px `--pp-focus-ring` em a/button/input/select/textarea/[tabindex], `.visually-hidden-focusable`, `@media(prefers-reduced-motion:reduce)` |
| Tokens | Semântica com contraste anotado (`--pp-success/#087a4d`, `--pp-warning/#805000`, `--pp-danger/#b42336`, `--pp-info/#245ea8`); foco `>=4,5:1 via v2151 #1d6fe9` (comentário). **Suspeita real**: `--pp-brand-primary:#1f73f1` com texto branco = **~4,4:1** (abaixo de 4,5:1 p/ texto normal) — precisa de verificação computada, não da anotação |
| Markup | Auditoria byte-level sobre 484 cshtml: **inputs sem nome acessível: 126 (43 arquivos)** · **selects: ~92 (~45 arquivos)** · **textareas: ~22 (17 arquivos)** · **botões só-ícone sem aria-label: 12 (10 arquivos)**. Concentrados em `Administrativo360` (backoffice legado) + páginas antigas (Agenda, CommandCenter, Lgpd, Notificacoes, Pendencias, Permissoes, V114, MinhaCentral, OperacaoAssistida, CommercialDemoWeb) |

Regra de nome acessível usada na auditoria (e no contrato): todo `input` (exceto `hidden`),
`select` e `textarea` deve ter ao menos um de: `id`, `asp-for`, `aria-label`, `aria-labelledby`,
`placeholder` ou `title`. Botão cujo conteúdo visível é vazio (só `<i>`/`<svg>`) exige
`aria-label` ou `title`.

## Decisões

- **D-R49-1**: correção mecânica por `aria-label` (nome acessível sem mexer em layout/ids) com
  **dicionário curado pt-BR** por atributo `name` (status→"Status", situacao→"Situação",
  dataInicio→"Início do período", quantidade→"Quantidade", localId→"Local", produtoId→"Produto",
  …) aplicado por script PS determinístico (inserção logo após o `name="…"` na mesma tag).
  Queda de segurança: nome humanizado automático (camelCase→espaços) — nenhum campo fica sem nome.
  Textareas e os 12 botões só-ícone são curados caso a caso (contexto visível em cada view).
  Não inventar a palavra "tenant" em rótulo novo (uso "Instituição" quando aplicável).
- **D-R49-2**: contraste **computado**, não anotado — o contrato lê os hex dos pares críticos em
  `tokens.css` e calcula a razão WCAG em C#: text-primary×page-background, text-secondary×surface,
  text-soft×surface, text-muted×surface, text-inverse×brand-primary (≥4,5:1). Se o par
  brand-primary×branco falhar, ajusta-se o token primário para `#1d6fe9` (já citado como o valor
  v2151) mantendo hover/active mais escuros.
- **D-R49-3**: contrato permanente `R49AcessibilidadeContratoTests` (scanner de fonte, padrão de
  casa): (i) zero campos sem nome acessível, (ii) zero botões só-ícone sem aria-label/title,
  (iii) layouts têm lang+skip-link+main, (iv) razões de contraste ≥4,5:1 nos pares listados,
  (v) `accessibility.css` mantém focus-visible + reduced-motion. Whitelist vazia se a passada
  fechar tudo.
- **D-R49-4**: escopo "básico" explícito — nada de trap de foco em modais, aria-live em toasts,
  landmark audit completo nem remediação de tabelas densas; documentado como pendência das
  próximas passadas (R52 hardening pode estender).

## Plano de implementação (b1)

1. Script de correção: dicionário name→rótulo + fallback humanizado; inserção idempotente
   (não duplica se `aria-label` já existir); relatorio antes/depois (esperado: 126+~92+~22 → 0;
   botões 12→0 manualmente).
2. Ajuste de token (se D-R49-2 aprouver) + comentário documentando a razão computada.
3. Contrato (5 fatos acima).
4. Gates: rebuild real → multiset 160/160 → classes dirigidas (R49 + higiene + R39 + R48/R47) →
   suíte integral base **~1599** (1594 + 5 fatos).
5. Smoke Web: página legada corrigida (Administrativo360 Cadastros via sessão demo) — HTML renderizado
   contém os `aria-label`; sem novo artefato em banco (página de leitura/cadastro backoffice).
