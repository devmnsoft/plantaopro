# R30 · r30z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R29 (`3d011fd`, suíte 1546).

## Escopo da rodada

Epic **E5** da diretriz (r24a1), parte 1: "validação em todos os formulários" — zona cadastros.

## Commits

| Commit | Conteúdo |
|---|---|
| `19c7bf6` | r30b1: 11 pontos — modais rápidos de Cadastros.cshtml (parceiro: nome 160 + documento CNPJ/CPF 20 com pattern; produto: sku 40/nome 180/unidade 12/min 0 no preço/EAN-GTIN 80; local: código 30/nome 120) + 2 sobretetos em Produtos.cshtml (sku 60→40, nome 200→180) + `R30ValidacaoFormulariosCadastrosContratoTests` (2 fatos, contrato de atributo) + r30a0 |

## Decisão D-R30-1 (regra de validação da casa)

- Validação nativa HTML5 (`required`/`maxlength`/`pattern`/`min`/`type=email`) — sem framework novo.
- **`maxlength` = tamanho exato da coluna** (fonte: information_schema do banco dev): teto maior deixa o
  usuário errar no banco (500); menor trunca dado legítimo.
- `pattern` só para formato objetivamente fechado (CNPJ/CPF `[0-9./ -]*`, EAN/GTIN `[0-9 -]*`).
- Sobreteto (maxlength > coluna) é BUG, não estilo.
- Servidor continua autoridade (BFF→API + TempData + `_AlertasAcao`); cliente é prevenção UX.

## Achado da auditoria

Os cadastros detalhados (Parceiros, Produtos, Locais, Cargos, Colaboradores, Departamentos) já eram válidos
— o buraco estava nos modais rápidos de Cadastros.cshtml (o caminho mais usado) e nos dois sobretetos do
detalhe de produto. 59 views/342 inputs auditados; números e mapa de colunas no r30a0 e nos dumps psql.

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Suíte completa: **Total 1548** (= base 1546 + 2 fatos), 1 falha sob carga —
  `R19DapperBindingGuardTests.Db_CentralClientes_SqlCompletaDaFonte_MaterializaItensETotais`
  (mesma race documentada desde R24; já era a falha da suíte R29) — **isolate 5× = 5/5 verde**.
  Este flake agora recaiu 2× seguidas sob carga: candidato prioritário a snapshot/CTE única quando o E9
  tocar a infra de testes (fila R40+).

## Estado de casa

- Apps PARADOS, portas livres. `origin/main` sincronizada neste push.
- Fila: R31 = E5 parte 2 (operacional — r31a0 já escrito nesta sessão com os 13 pontos mapeados); depois
  R32–R33 E6, R34 E7, R35–R37 E3, R38–R39 E2, R40+ E9.
