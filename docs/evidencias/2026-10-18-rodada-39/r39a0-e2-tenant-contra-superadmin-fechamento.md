# R39 · r39a0 — E2 parte 2: tenant visível só para super-admin (fechamento) + D-R24-3 revisada

Data: 2026-10-18 · Sessão: `opencode` · Base: R38 (`d6fede2`, suíte 1565).

## Situação herdada da R38

As telas de uso institucional já não renderizam a palavra "tenant" (b1/b2), e o inventário final de
70 ocorrências restantes ficou classificado como código não renderizado, zonas `isGlobal`, nota de
analista e marketing. O que falta neste passo: **travar por contrato** o que resta, e **revisitar
D-R24-3** (backoffice SaaS).

## Revisita D-R24-3 (verificada nos controllers nesta rodada)

| Zona | Papéis autorizados | "tenant" é legítimo? |
|---|---|---|
| `AdminSaasController` (backoffice MNSOFT) | `AdministradorGlobal, Suporte, Auditor` | **SIM** — público 100% plataforma; admin da instituição nem entra |
| `Clientes/*`, `CommandCenter`, `MeuDia` (ramo global), `Modulos\Tenant`, `Fase2Operational` | roles de plataforma / ramo `isGlobal` | **SIM** — linguagem de operador sobre múltiplos clientes |
| `Usuarios`, `WhiteLabel`, `Perfis`, `Permissoes`, `Seguranca` | aceitam `AdministradorCliente` | **NÃO** — já reescritos na R38 (b1/b2) |
| Demais telas institucionais | perfis operacionais | **NÃO** — limpas |

**Decisão D-R39-1**: D-R24-3 **confirmada sem mudança de texto** — o backoffice é por construção a
zona super-admin e sua linguagem ("tenants", "multi-tenant") é informação para quem opera vários
clientes; o risco era o oposto (palavra vazar para fora), já tratado e agora travado.

## Contrato permanente (fato novo)

`R39TenantSoParaSuperAdminContratoTests`:
1. **AdminSaas fechado**: o atributo `[Authorize]` imediatamente acima de `class AdminSaasController`
   contém `AdministradorGlobal` e **não** contém `AdministradorCliente` — se alguém abrir o
   backoffice para o admin da instituição, a rodada falha.
2. **Instituições livres da palavra**: todo `.cshtml` fora de `AdminSaas/` e fora de uma whitelist de
   25 arquivos (os keeps classificados na R38) **não pode** conter "tenant"/"tenants" em nenhuma
   linha — introduzir o termo numa tela institucional nova ou futura falha a suíte.

## Escopo intencionalmente fino

Não há mudança de UI nesta rodada: o trabalho de reescrita coube à R38 e o modelo de acesso foi
auditado no r38a1 (D-R38-1/2). R39 é o fechamento contratual do epic **E2**. Depois: **R40 E9**
(planos e limites — recon já iniciado: limites aplicados hoje só na troca de plano).
