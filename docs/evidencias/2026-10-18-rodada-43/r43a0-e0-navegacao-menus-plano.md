# R43 · r43a0 — E0 design: navegação e menus (clareza, agrupamento, profundidade)

Data: 2026-10-18 · Sessão: `opencode` · Base: R42 (`6c5fe91`, suíte 1577).

## Recon do menu lateral (`Shared/_AppSidebar.cshtml`, 105 linhas)

O menu já é bom em estrutura: seções com título localizado (resx ×4), visibilidade por
papel/módulo/permissão (`canManage`/`Can(...)`/`isGlobalAdmin`) e estado ativo por controller.
As falhas reais encontradas:

| # | Problema | Impacto | Correção |
|---|---|---|---|
| A | Literal inglês **"Command Center"** no item de menu (L43) e no h1 da tela — fora do i18n; usuário de outra cultura via inglês no meio do menu | Clareza + consistência de idioma | Nova chave `Nav.CommandCenter` nas 4 culturas (pt "Central de operações"); menu e h1/title da tela usam a mesma chave — um rótulo, um dono |
| B | **Implantação** (`/Onboarding`) e **Minha Assinatura** não têm item no menu — o admin da conta só chega a elas por atalho contextual (ex.: o banner do Meu Dia, R41) ou por URL | Descobrimento/primeira experiência (sinergia direta com R41) | Dois itens na seção de gestão institucional, guardados por `Can("ONBOARDING")`/`Can("ASSINATURAS")` e `!isGlobalAdmin`; os módulos são core do admin da conta (`TenantAdministrationModules`, decisão R5-D9 — `HasPermission` retorna true para tenant-admin sem contratação separada) |
| C | Título da página de implantação: **"Onboarding SaaS"** nas 4 culturas | Jargão para usuário comum (E1) | pt "Implantação" / en "Setup" / es "Puesta en marcha" / fr "Mise en route"; o menu reaproveita a mesma chave (rota estável, rótulo único) |

## Decisões

- **D-R43-1**: o rótulo do item de Implantação reutiliza `Onboarding.PageTitle` em vez de criar uma
  segunda chave de navegação — página e menu nunca divergem.
- **D-R43-2**: os dois itens novos ficam visíveis só quando a permissão core concede acesso e nunca
  para admin global (que já tem "Assinaturas" na seção de plataforma). Diretores sem perfil de
  tenant-admin não veem os itens — coerente com o próprio `HasPermission`.
- **Fora desta fatia (documentado)**: deduplicação de ícones (Coverage é usado por Cobertura,
  Usuários e Administrativo360; Finance por Visão Financeira e Caixa). Ícone duplicado não impede
  identificação pelo texto; trocar 5 ícones exigiria passar pela leitura visual — fila para R46
  (passada crítica geral) junto com os estados vazios.

## Gates e suíte

- Rebuild real → multiset 160/160 idêntico ao pristino, 0 erros.
- Classes: R43 + R34 (jargão em resx) + R7DF6 (chaves Adm360) + i18n/higiene.
- Suíte integral base **1580** = 1577 + 3 fatos R43.
