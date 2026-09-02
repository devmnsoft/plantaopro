# PlantãoPro v2.13.2 — design system premium global

## Escopo e método

A rodada auditou estaticamente as Views, partials compartilhadas e ativos públicos de `PlantaoPro.Web`, além das ocorrências de autenticação e mensagens em `PlantaoPro.Api`. O SDK .NET não está instalado neste ambiente; por isso, conforme a restrição da rodada, nenhuma implementação C#, autenticação server-side, banco ou migration foi alterada.

## Matriz de auditoria visual

| Página / módulo | Layout | Formulário / mensagens | Ajuda | Responsividade | Contexto e prioridade | Diagnóstico / evolução |
|---|---|---|---|---|---|---|
| Account/Login | `_AuthLayout` | Login, validação por campo e resumo | Segurança no próprio card | Sim | Público, P0 | Composição já era resiliente; copy comercial, assinatura v2.13.2 e acabamento global foram consolidados sem alterar POST, antiforgery ou contrato de e-mail. |
| Home/Dashboard, MeuDia e MinhaCentral | `_Layout` | Filtros e estados operacionais | `_ScreenGuide` global | Sim | Perfil/tenant, P0 | Hierarquia, canvas, largura de leitura, cards e foco recebem tokens v2.13.2. |
| Shell: sidebar/topbar/footer | `_Layout` | Toasts, drawers e confirmação próprios | Drawer + guia global | Sim | Perfil/tenant, P0 | Topbar translúcida sóbria, conteúdo fluido e badge de contexto acessível. |
| Usuários / Perfis / Permissões | `_Layout` | CRUD e validação MVC | `_ScreenGuide` global | Sim | Permissões, P1 | Labels, controles, erro, botões e tabelas normalizados globalmente. |
| Clientes / tenants / Admin SaaS | `_Layout` | Filtros e ações administrativas | `_ScreenGuide` global | Sim | Super Admin, P1 | Contexto global ganha ícone de escudo e aviso explícito de acesso auditado. |
| Médicos / Especialidades | `_Layout` | `_MedicoForm` e `_EspecialidadeForm` | `_ScreenGuide` global | Sim | Tenant, P1 | Inputs, selects, obrigatoriedade e validação padronizados sem tocar relacionamentos. |
| Hospitais / unidades | `_Layout` | `_HospitalForm` | `_ScreenGuide` global | Sim | Tenant, P1 | Mesmo padrão premium e foco WCAG; vínculo existente preservado. |
| Escalas / Plantões | `_Layout` | Filtros, detalhes e substituição | `_ScreenGuide` global | Sim | Perfil/tenant, P1 | Tabelas, status, ações e leitura mobile refinados. |
| Financeiro / Pagamentos | `_Layout` | Filtros e detalhes | `_ScreenGuide` global | Sim | Perfil/tenant, P2 | Hierarquia e semântica visual global, sem modificar valores ou regras. |
| Relatórios / BI | `_Layout` | Filtros/exportação existente | `_ScreenGuide` global | Sim | Perfil/tenant, P2 | Superfícies, tabelas e toolbar consistentes. |
| Configurações / LGPD / Auditoria | `_Layout` | Configurações e estados existentes | `_ScreenGuide` global | Sim | Permissão elevada, P2 | Contraste e contexto melhorados; políticas e auditoria intactas. |

## Componentes e tokens evoluídos

O stylesheet `v2132-premium-global.css` é a camada final, leve e sem dependências. Ele consolida cores clínicas sóbrias, superfícies, tipografia, bordas, raios, sombra sutil, foco, movimento reduzido e alto contraste. Também normaliza cards, botões, inputs, selects, validação, tabelas, badges, alertas, empty states, toolbars e ajuda contextual. A camada foi incluída tanto no layout autenticado quanto no layout de autenticação.

## Login

- Mantidos `Account/Login`, POST tradicional, antiforgery, validação MVC, senha mascarada, mostrar/ocultar senha, Caps Lock e recuperação.
- Mantido o watchdog de 15 segundos que reabilita o botão se uma navegação for interrompida; `pageshow` também restaura o estado.
- O identificador continua como **E-mail**, pois o contrato atual observado usa `Email`; CPF/CNPJ não foi prometido visualmente sem suporte confirmado no backend.
- Atualizados headline auxiliar, mensagem de segurança, assinatura MNSOFT e versão visual.
- Nenhuma credencial é exibida. Em demo, o layout informa explicitamente que credenciais não são mostradas.

## AppShell e contexto de acesso

O shell preserva a filtragem existente de menu/permissões. O contexto atual agora possui rótulo acessível. Super Administrador recebe escudo e texto “Super Admin · acesso auditado”; usuários de cliente continuam vendo apenas a instituição fornecida pelas claims. Quando a claim de impersonação existe, o banner informa que o acesso assistido é auditável, identifica o contexto e mostra sua expiração. Não foi inventado um endpoint para encerrar impersonação, pois nenhum fluxo correspondente foi localizado na camada Web auditada.

## Formulários, tabelas e mensagens

Formulários MVC existentes recebem labels mais fortes, área clicável confortável, bordas, foco visível, ajuda e erros contrastantes. O resumo de validação ganha marcador semântico lateral. Tabelas recebem cabeçalho legível, hover discreto e contêiner mobile. Toasts, banners, drawers, modal de confirmação, estados vazios e validações existentes continuam sendo os canais próprios; nenhum `alert()` ou `confirm()` foi introduzido.

## “Como usar esta tela”

O `_ScreenGuide` permanece renderizado globalmente nas páginas autenticadas e explica objetivo, público, ação, cuidado e próximo passo. O login mantém a ajuda específica “Como acessar com segurança”. O drawer contextual segue disponível no shell.

## Responsividade e acessibilidade

A camada cobre desktop, notebook, tablet e mobile com conteúdo fluido, ações empilháveis e tabelas roláveis. Usa `clamp`, `text-wrap`, alvos mínimos, foco de 3 px, mensagens com regiões ARIA já existentes, suporte a `prefers-reduced-motion` e `forced-colors`. A validação visual em navegador não pôde ser executada porque o runtime .NET não existe no container.

## QA manual recomendado

1. Abrir login em desktop e mobile.
2. Testar erro de login e foco no resumo.
3. Testar loading e recuperação automática após 15 segundos.
4. Entrar como Super Admin e conferir o contexto global auditado.
5. Ativar acesso assistido e conferir banner/expiração.
6. Entrar como usuário de cliente e conferir instituição/menu limitado.
7. Abrir usuários, médicos, hospitais, escalas e financeiro.
8. Conferir sucesso, erro, atenção, toast e confirmação própria.
9. Conferir “Como usar esta tela”.
10. Navegar apenas por teclado, zoom 125% e viewport mobile.

## Comandos e resultados

- `git status --short --branch`, `git remote -v`: executados; checkout local iniciou limpo e sem remote configurado.
- `dotnet --info`, `dotnet --list-sdks`: indisponíveis (`dotnet: command not found`).
- Inventário com `find ... | sort` e buscas com `rg`: executados.
- Restore/build/testes .NET: não executáveis pela ausência do SDK.
- Validações estáticas, scripts disponíveis, busca proibida e `git diff --check`: resultados registrados na validação final desta rodada.

## Limitações restantes

- Build Debug/Release, testes .NET e screenshot dependem da instalação do SDK/runtime compatível.
- A matriz cobre as jornadas prioritárias e os padrões globais; páginas muito especializadas devem receber revisão funcional com dados reais por perfil.
- CPF/CNPJ no login só deve ser habilitado quando o contrato de autenticação aceitar esses identificadores.
- Um botão de saída de acesso assistido depende de rota server-side segura e auditada; não foi simulado.
