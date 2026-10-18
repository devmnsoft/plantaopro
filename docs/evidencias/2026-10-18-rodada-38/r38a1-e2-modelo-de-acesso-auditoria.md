# R38 · r38a1 — E2 parte 2: auditoria do modelo de acesso (CPF/CNPJ + email/senha)

Data: 2026-10-18 · Sessão: `opencode`. Base da auditoria: código lido nesta sessão, sem alteração de
esquema nesta fatia.

## O que o sistema faz hoje (verificado no código)

| Dimensão | Estado atual | Onde |
|---|---|---|
| Instituição | provisionada por **CNPJ único** no cadastro self-service (B5); cliente = a conta do CNPJ | `Cadastro\Cadastro.cshtml` (campo CNPJ), `ProvisionamentoB5Tests` |
| Pessoa (identidade) | **e-mail globalmente único** + senha obrigatória; criação com senha temporária de troca forçada | `UsuarioEditorViewModel` (Email AddressAttribute, SenhaTemporaria), `Usuarios\Form.cshtml` |
| Vínculo pessoa↔instituição | usuário pode ter vínculo em mais de um cliente ("identidade global e outros vínculos" no modal de bloqueio); permissões por perfil **dentro** do cliente | `Usuarios\Index.cshtml` (msg do confirm), `Usuarios\Form.cshtml` (filtro de perfis por cliente) |
| Sessão | claims `cliente`/`tenant` delimitam tudo; contexto exibido na barra (`_ContextBar`) e no badge (`_TenantContextBadge`) | `Shared\_ContextBar.cshtml`, `Shared\_TenantContextBadge.cshtml` |
| Isolamento de dados | leituras e escritas cruzadas entre instituições falham silenciosamente (404/nulo) | fatos Q7 (`Administrativo360IsolamentoBidirecionalQ7Tests`), `AiWps3RegressionTests` (jornada cotação) |
| CPF | **não faz parte da identidade de acesso** — existe só como documento opcional de fornecedor/profissional (CRM ou CPF) | `CadastrosRepository.cs` (coluna Documento) |

## Interpretação da diretriz + decisão

Diretriz (r24a1): "acesso isolado por CPF/CNPJ+email/senha". O isolamento funcional já se dá pelo par
que o sistema possui de identificador fiscal: **a instituição entra e opera pelo CNPJ** (cadastro,
documentos fiscais, convites) e **a pessoa pelo email+senha** (identidade única, vínculo explícito).
O CPF individual não é campo do login; adicioná-lo seria mudança de esquema e de fluxo de
autenticação sem demanda do cliente registrada.

**Decisão D-R38-1**: manter a identidade de pessoa como **email+senha** (equivalência funcional para
isolamento por pessoa, já que e-mail é único e o vínculo por CNPJ é explícito e auditado); o CPF segue
somente como documento opcional em cadastros de pessoas. Se o cliente pedir login por CPF/CNPJ,
abrir epic próprio (migração + política de desambiguação de e-mail duplicado).

**Decisão D-R38-2**: a visibilidade da palavra/conceito "tenant" sai das telas de uso institucional
(feita nas fatias b1/b2 desta rodada); nas telas de operação da plataforma (zonas `isGlobal`:
`Clientes`, `CommandCenter`, `MeuDia` variante global, `AdminSaas`) "tenant"/"cliente" segue como
linguagem de operador — decisão original D-R24-3, reconfirmada.

## Inventário final de ocorrências (pós b1+b2)

Scan idêntico ao r38a0 reexecutado após as reescritas: **70 linhas** restam em 25 views fora de
AdminSaas (era 161). Classificação integral:
- **Código não renderizado** (variáveis JS/C#, `Model.Tenant*`, `asp-route-`, classes CSS
  `is-tenant`, nomes de chaves UiText, tags de partial): ~55 linhas — `Usuarios\Form`,
  `_ContextBar`, `_GuidedJourney`, `Modulos\Tenant`, `_AppSidebar`, `_UserMenu`, `_TenantContextBadge`,
  `_PageContextHeader`, `_AppTopbar`, `AssistenteIa`, `Account\Login`, `Saude360\Modulo`,
  `Fase2Operational` (valor), `Convite\Aceitar` (valor), `Clientes/*` (rotas/valores);
- **Zona super-admin / `isGlobal`** (texto só aparece para MNSOFT): `CommandCenter` ×4, `MeuDia` ×2,
  `Home\Dashboard` (card `IsAdmin`), `_GuidedJourney` ramo global, `_ScreenGuide` ramo global,
  `Gestao\Dashboard` (comentário HTML não renderizado);
- **Nota técnica de analista**: `DashboardsPremium\Perfil` ×2 (predicado do KPI) — keep documentado;
- **Marketing/público**: `CommercialDemoWeb\Landing` ("Multi-tenant e white label"),
  `Shared\SaasComercialPage` ×2 (tela de operação comercial).

Nenhuma linha restante renderiza a palavra para um usuário comum da instituição.
