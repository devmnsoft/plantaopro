# R42 · r42z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R41 (`b9755d2`, suíte 1572).

## Escopo da rodada

Roadmap R42: "Consolidação de scanner-fatos permanentes (regras E4/E5/E6/E8 em um suíte dedicado)".

## Commits

| Commit | Conteúdo |
|---|---|
| `2ce85df` | r42b1: `R42ScannersPermanentesContratoTests` — suíte dedicado com 5 fatos (um por regra E4 contraste, E5 formulários ×2, E6 lookups nome-vs-id, E8 sem homologação), agregando as classes canônicas R26/R30/R31/R32/R33/R25 por **invocação direta dos métodos públicos** (zero cópia de lógica; as regras permanecem com um único dono) + r42a0 com o mapa, decisões D-R42-1/D-R42-2 e o protocolo das famílias flake |
| (docs) | este r42z |

## Regressão permanente executável em uma unidade

    dotnet test --filter "FullyQualifiedName~R42ScannersPermanentes"

Ganha dois efeitos: um ponto de entrada único para a regressão de UX/texto dos quatro epics e
menos paralelismo entre os scans pesados de arquivo na suíte integral (eles deixam de disputar
disco/CPU por 5 classes distintas além da execução agregada sequencial).

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Classes dependentes: 28/28 verde (R42 + as 5 canônicas + higiene).
- Suíte integral (base **1577** = 1572 + 5 fatos): primeira execução saiu **exit 1 por infra** —
  `MSB4236: o SDK 'Microsoft.NET.Sdk' especificado não pôde ser encontrado` na avaliação do projeto
  (não chegou a executar nenhum teste; o log tem apenas a falha do resolvedor). Reexecução imediata:
  **1577/1577, exit 0, zero flakes**. Glitch transitório de resolução de SDK registrado como tal.

## Estado de casa

- Apps PARADOS, portas livres. Rodada 42 fechada neste push.
- Fila: **R43 E0 design navegação/menus** — recon pronto sobre `_AppSidebar` (105 linhas): literal
  inglês "Command Center" fora do i18n (L43), itens de gestão institucional ausentes do menu
  (Minha Assinatura, Onboarding/Implantação) e reaproveitamento de ícones confusos (Coverage usado
  para Cobertura, Usuários e Administrativo360).
