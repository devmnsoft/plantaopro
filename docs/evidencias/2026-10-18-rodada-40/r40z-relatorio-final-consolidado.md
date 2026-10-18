# R40 · r40z — Relatório final consolidado

Data: 2026-10-18 · Sessão: `opencode` · Base anterior: R39 (`9cf98f2`, suíte 1567).

## Escopo da rodada

Epic **E9** (evolução SaaS) — rodada 1: planos e limites por plano. Recon produziu o mapa de
cobertura dos guards (r40a0) e revelou um buraco real: o limite de usuários do plano não era
aplicado em lugar nenhum.

## Commits

| Commit | Conteúdo |
|---|---|
| `045b980` | r40b1: `POST api/seguranca/usuarios` aplica `PodeCadastrarUsuarioAsync` antes de salvar e registra `RegistrarUsoAsync("USUARIOS", 1)` no sucesso (mesmo padrão de médicos/hospitais; sem contexto de cliente o guard não roda) + "Usuário não encontrado no **tenant** permitido." → "…no escopo de acesso." nas 8 ocorrências (Seguranca ×2, Permissoes ×2, SecurityAdministrationServices ×4, Web UsuariosController ×1) com o pin do `V2155TenantPermissionBoundaryTests` ajustado no mesmo commit + fato `R40PlanosLimitesCoberturaContratoTests` (guard antes do save + ausência da frase antiga em todo .cs do backend) + r40a0 com o mapa completo de guards (médicos ✓, hospitais ✓, plantões na publicação ✓, convites no envio ✓, usuários ✗→✓) |
| (docs) | este r40z |

## Decisões de escopo (documentadas no r40a0)

- Convites aceitos e o provisionamento de onboarding criam usuários por transações próprias e
  consomem o limite mensal de convites / capacidade inicial — entrar no guard de criação manual
  exigiria política produto (convite aceito conta como usuário?) para o cliente definir.
- Downgrade continua barrando queda abaixo do uso atual (comportamento verificado, mantido).

## Gates e suíte

- Rebuild real → multiset **160/160 idêntico ao pristino**, 0 erros.
- Classes dependentes: 22/22 verde (R40 + V2155 + SaasOperacaoSerial + higiene).
- Suíte integral (base **1569** = 1567 + 2 fatos): **1569/1569, exit 0, zero flakes**.

## Estado de casa

- Apps PARADOS, portas livres. Rodada 40 fechada neste push.
- Fila: **R41 E9 onboarding + primeira experiência** — recon pronto: as descrições canônicas dos
  critérios da jornada exibem "tenant" em 4 pontos (visíveis ao admin da instituição), e a
  primeira tela do admin novo (MeuDia) **não aponta para o checklist de implantação**; ambos entram
  no r41b1 (banner "continue seu checklist" no MeuDia para admin da instituição).
