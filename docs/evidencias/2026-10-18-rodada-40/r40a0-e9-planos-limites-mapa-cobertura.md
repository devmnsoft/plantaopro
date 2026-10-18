# R40 · r40a0 — E9 (rodada 1): planos e limites por plano — mapa de cobertura e correção do buraco real

Data: 2026-10-18 · Sessão: `opencode` · Base: R39 (`9cf98f2`, suíte 1567). Epic **E2 FECHADO** na R39.

## Mapa de cobertura dos limites de plano (verificado nos controllers nesta rodada)

| Limite | Verificação | Onde |
|---|---|---|
| Médicos na criação | `PodeCadastrarMedicoAsync` antes do create + uso registrado | `MedicosController.Create` ✓ |
| Hospitais na criação | `PodeCadastrarHospitalAsync` antes do create + uso registrado | `HospitaisController.Create` ✓ |
| Plantões/mês | `PodePublicarPlantao` na publicação (rascunho não consome) | `PlantoesController` (L261+) ✓ |
| Convites/mês | `PodeEnviarConviteAsync` no envio do convite + uso registrado | `PlantoesController` (L233+) ✓ |
| **Usuários** | **definia `PodeCadastrarUsuarioAsync` mas NENHUM endpoint chamava** | `POST api/seguranca/usuarios` ✗ → corrigido em r40b1 |

Mecanismo (invariado): o guard exige assinatura ATIVA ou TRIAL vigente, compara usado×limite
(limite 0 = sem teto), registra `cliente_bloqueios` + alerta diário ao bloquear e avisa uso alto
(`RegistrarAlertaUsoAltoAsync`) ao permitir. Área institucional "Minha Assinatura" já expõe plano,
uso, solicitações de upgrade/downgrade/cancelamento e faturas.

## Correções (r40b1)

1. **Limite de usuários passa a valer na criação**: `SegurancaController.CriarUsuario` agora aplica
   `PodeCadastrarUsuarioAsync(clienteId)` antes de salvar e registra `RegistrarUsoAsync("USUARIOS", 1)`
   no sucesso — mesmo padrão de médicos/hospitais. Sem contexto de cliente (global admin puro) o
   guard não roda, idem no padrão existente.
2. **"tenant" sai da mensagem de 404** que chegava à tela: "Usuário não encontrado no tenant
   permitido." → "…no escopo de acesso.", nas 8 ocorrências (Seguranca ×2, Permissoes ×2,
   SecurityAdministrationServices ×4, Web UsuariosController ×1) com o pin do
   `V2155TenantPermissionBoundaryTests` ajustado no mesmo commit.

## Escopo intencional (documentado para auditoria)

- Convites aceitos e o provisionamento de onboarding criam usuários por transações próprias
  (SelfService/ProvisionamentoB5) e consomem o limite mensal de convites / capacidade inicial —
  fora do guard de criação manual; entrar no escopo do limite exigiria política produto
  (convite aceito conta como usuário?) — decisão para cliente quando houver demanda.
- Downgrade continua barrando queda abaixo do uso atual (SolicitacoesPlanosService + SelfService),
  comportamento verificado e mantido.
