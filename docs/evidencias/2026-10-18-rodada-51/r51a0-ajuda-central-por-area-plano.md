# R51 · r51a0 — Ajuda central: "como usar" por área (extensão do E3)

Data: 2026-10-18 · Sessão: `opencode` · Base: R50 (`8e42619`, suíte 1602).

## Objetivo

Estender a Central de Ajuda existente (`AjudaController` — Index/Busca/Artigo/Modulo/Checklist/
PrimeirosPassos, filtrada por perfil de sessão) com **conteúdo "como usar" para cada área** que ainda
não tem manual, mantendo o padrão E3 (linguagem simples, passos curtos, link direto para a tela).

## Recon

- A central já existe com 6 tópicos e 13 artigos estáticos, filtrados pelo perfil primário da sessão
  (`SessionClaimsBuilder` → claim Role = papel de maior prioridade no `RoleCatalog`).
- **Gap principal**: a família administrador de tenant — `ADMINISTRADOR_CLIENTE` (prioridade 700, papel
  primário do gestor demo e dos onboards R38+) e `ADMINISTRADOR` (650) — **não vê nenhum tópico próprio**:
  só "Privacidade LGPD" (TODOS). São eles que operam Administrativo360 (vendas/compras/estoque/títulos/
  relatórios) e Minha Central (faturas vencidas do R48).
- Sem conteúdo TODOS para as rotinas transversais novas: fatura vencida (R48) e notificações (R49
  consolidou preferências).
- Rotas confirmadas nos controllers: `Administrativo360/{Orcamentos,PedidosCompra,Estoque,TitulosReceber,
  RelatoriosFinanceiros}`, `MinhaCentral/Index`, `Notificacoes/Preferencias`.

## Decisões

### D-R51-1 — família administrador-cliente compartilha o mesmo manual
`ADMINISTRADOR` e `ADMINISTRADOR_CLIENTE` são irmãos do catálogo (mesmo escopo Tenant, mesmo
`DefaultRoute /ClientePortal`; o onboarding atribui ambos). `NormalizarPerfil` passa a mapear
`ADMINISTRADOR` → `ADMINISTRADOR_CLIENTE` **dentro da ajuda** (só `AjudaController` é afetado): o manual
de operação é um só e o usuário nunca vê "nada" ao abrir a central.

### D-R51-2 — dois novos tópicos + sete novos artigos (padrão do helper `Artigo(...)`)
- Tópico `instituicao` (Perfil `ADMINISTRADOR_CLIENTE`, ícone buildings): "Operação da instituição".
  Artigos (5): criar orçamento; registrar pedido de compra; acompanhar estoque e inventário; controlar
  títulos a receber e a pagar; consultar relatórios financeiros.
- Tópico `central` (Perfil `TODOS`, ícone sino): "Minha Central e avisos". Artigos (2): resolver fatura
  vencida; configurar notificações.
- Copy no tom da casa (R50): frases curtas, verbo no imperativo/indicativo, sem jargão banido
  (fallback/token/endpoint/seed), sem exclamação, sem id/código cru.

## Contrato permanente — `R51AjudaContratoTests` (3 fatos, scanner de fonte sobre `AjudaController.cs`)

1. `TopicoInstituicao_CobrePerfilAdministradorCliente` — tópico `instituicao` com Perfil
   `ADMINISTRADOR_CLIENTE` e >= 5 artigos daquele perfil.
2. `TopicoTodos_CobreFaturaVencidaENotificacoes` — artigos `central-fatura` e `central-notificacoes`
   presentes, com texto de "fatura vencida" em linguagem simples.
3. `FamiliaAdministrador_CompartilhaManual` — a normalização `ADMINISTRADOR` → `ADMINISTRADOR_CLIENTE`
   existe em `NormalizarPerfil`.

## Gates

Build real + gate 160 chaves; dirigidas (R51 + R50 + R49 + higiene); suíte integral (esperado 1602+3=1605);
commits `web,tests (rodada 51 r51b1)` + `docs (rodada 51 r51z)`; push.
