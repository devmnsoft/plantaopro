# PlantãoPro v2.13.3 — QA visual e design system nas telas reais

## Escopo e pré-validação

A rodada foi executada em `codex/v2133-qa-visual-design-system-telas-reais`. O repositório local não possui remoto configurado e o SDK .NET não está instalado (`dotnet: command not found`). Em respeito ao escopo seguro solicitado, não foram alterados C#, controllers, services, autenticação server-side, banco, migrations, projetos ou solução. Build, testes .NET, navegação autenticada e captura visual não puderam ser executados neste ambiente.

Foram inspecionadas as árvores `Views`, `Pages` e `wwwroot` do Web/API e pesquisadas referências aos módulos solicitados. A aplicação possui 396 views Razor e um design system incremental já consolidado até v2.13.2; esta rodada concentra o acabamento global das telas reais e corrige achados objetivos sem criar dados substitutos.

## Matriz de auditoria

| Tela / conjunto | Módulo | Layout | Problemas observados | Formulário / mensagens | Permissões e contexto | Responsividade / ajuda | Prioridade e resultado |
|---|---|---|---|---|---|---|---|
| Entrar | Acesso | `_AuthLayout` | Necessidade de confirmar POST real, recuperação de loading e versão visual | Labels e erros já associados; POST tradicional preservado; timeout devolve controle | Texto diferencia acesso institucional e global auditado | Shell responsivo; ajuda curta própria | P0 — preservado fluxo real, `novalidate` progressivo e versão v2.13.3 |
| Shell, sidebar e topbar | Global | `_Layout` | Ajuda global era genérica; contexto assistido sem ação de saída | Toasts, overlays e estados existentes | Sidebar já condiciona módulos por roles; tenant aparece no topo | Menu mobile existente; novo acabamento carregado em ambos os layouts | P0 — ajuda contextual por módulo e contexto reforçados |
| Banner de impersonação | Super Admin | `_Layout` | Banner sem botão explícito para encerrar | Estado persistente e auditável | Somente claim `impersonation=true`; usa Logout real, sem rota inventada | Sticky e adaptado ao mobile | P0 — ação “Encerrar acesso assistido” adicionada |
| Usuários | Gestão | `_Layout` | KPIs, identidade e tenant de demonstração estavam fixos | Não há fonte real conectada nesta action | Controller já restringe roles; conteúdo fake poderia induzir decisão | Estado informativo responsivo; ajuda global contextual | P0 — dados simulados removidos; estado seguro até integração real |
| Assinatura (criar/editar) | SaaS / financeiro | `_Layout` | Cliente e plano exigiam GUID manual | Validação existente; relacionamentos agora são selects com lookup real | Endpoints respeitam autenticação/contexto; sem fallback manual | Grid responsivo; ajuda global | P0 — seleção por nome e suporte a resposta paginada |
| Médicos | Operação | `_Layout` | Estrutura já premium; ações por ícone dependem de título | Formulários parciais e feedback existentes | Menu condicionado a roles operacionais/gestão | Tabela responsiva, empty state e introdução | P1 — coberto pela ajuda contextual de LGPD e impacto |
| Hospitais / especialidades | Operação | `_Layout` | Estrutura consistente; impacto de vínculos precisava orientação | Campos reais, datas e validações já existentes | Escopo do tenant indicado pelo shell | Tabela responsiva e estados vazios | P1 — ajuda contextual específica |
| Plantões / escalas | Operação médica | `_Layout` | Alta densidade de ações e risco operacional | Filtros com datas/selects; confirmação visual própria existente | Ações e menus condicionados a papéis | Cards móveis e tabela desktop | P1 — ajuda explica fluxo, substituição e fechamento |
| Financeiro / faturamento | Financeiro | `_Layout` | Necessidade de reforçar competência e impacto | Filtros e feedback existentes | Visível somente a perfis financeiros/gestão | Componentes responsivos existentes | P1 — ajuda específica sobre baixa, ajuste e auditoria |
| Relatórios / BI | Inteligência | `_Layout` | Exportação exige cuidado com dado pessoal | Filtros reais; estados de indisponibilidade existentes | Permissões e plano controlam acesso | Tabelas/cards responsivos | P1 — ajuda específica sobre filtro e exportação LGPD |
| Configurações | Administração | `_Layout` | Contexto da alteração não era explicado globalmente | Formulários com feedback existente | Menu apenas para gestão | Shell e ajuda responsivos | P1 — ajuda diferencia preferência pessoal/institucional |
| Auditoria | Governança | `_Layout` | Necessidade de reforçar sigilo e rastreabilidade | Filtros e detalhes existentes | Restrita a perfis autorizados | Tabela responsiva | P1 — ajuda específica de segurança |
| Nova solicitação da unidade | Portal hospital | `_Layout` | Botões submit sem tipo explícito | Campos visíveis, selects, datas/horas | Unidade vem do contexto autorizado | Grid Bootstrap responsivo | P1 — tipos de submit explicitados |

## Padrão aplicado

- Foi criado o acabamento `v2133-real-screens.css`, carregado no shell autenticado e no layout de acesso.
- A ajuda “Como usar esta tela” agora adapta finalidade, público, ação principal, cuidado e impacto aos módulos críticos. Também explicita se a sessão está no contexto global MNSOFT ou restrita à instituição.
- O banner de acesso assistido é persistente, legível, responsivo e possui saída explícita usando o fluxo real de logout.
- O estado de usuários não apresenta mais métricas, nomes, e-mails ou tenants estáticos. Sem uma fonte real na action atual, a tela comunica a indisponibilidade e conduz à gestão real de perfis.
- Campos de relacionamento de assinatura deixaram de aceitar identificadores técnicos. Cliente e plano usam o componente de lookup; o loader reconhece respostas diretas e paginadas e exibe nomes de cliente/plano.
- Login mantém `POST` real, antiforgery, prevenção de submissão duplicada, recuperação após 15 segundos, erro acessível, indicador de Caps Lock e retorno correto pelo evento `pageshow`. O rótulo continua “E-mail” porque o contrato atual inspecionado não comprova suporte a CPF/CNPJ; não foi feita promessa visual incompatível com o backend.
- O Web deve ser acessado na URL configurada para o projeto `PlantaoPro.Web`; não foi possível confirmar host/porta de execução sem SDK e sem iniciar o servidor.

## Regras visuais por perfil

- **Super Admin MNSOFT:** recebe indicação “Super Admin · acesso auditado”, contexto global e orientação para confirmar o tenant antes de agir. A impersonação exibe banner persistente com expiração e saída.
- **Administrador do cliente:** vê menus de gestão permitidos e orientação restrita à instituição vinculada à sessão.
- **Usuário operacional:** recebe apenas módulos contemplados pelas roles já aplicadas no sidebar; a ajuda contextual não sugere ações administrativas.
- **Todos os perfis:** ações ausentes ou desabilitadas não são substituídas por links falsos; nenhum dado visual é criado para simular integração.

## Formulários e mensagens

- Labels visíveis, resumos de erro, mensagens por campo e feedback de envio existentes foram preservados.
- O login recebeu validação progressiva compatível com o gate estático e mantém recuperação de estado em demora de rede.
- A assinatura usa dropdown de relacionamento em vez de GUID manual, preservando os nomes de campo esperados no POST.
- A solicitação hospitalar recebeu `type="submit"` explícito nos dois comandos.
- Toasts e confirmação por modal permanecem como componentes globais; não foram introduzidos `alert()`, `confirm()` ou `href="#"`.

## Comandos executados e resultados

- `git status --short --branch`: executado; árvore inicialmente limpa na branch `work`.
- `git remote -v || true`: executado; nenhum remoto configurado.
- `dotnet --info || true` e `dotnet --list-sdks || true`: `dotnet: command not found`.
- `find backend/PlantaoPro.Web backend/PlantaoPro.Api ... | sort`: executado; inventário de views/assets produzido.
- `rg -n "Login|Dashboard|...|Auditoria" ...`: executado; módulos localizados.
- `python3 scripts/check-premium-ui.py`: aprovado.
- `python3 scripts/check-layout-regression.py`: aprovado.
- `python3 scripts/check-feedback-ui.py`: aprovado.
- `python3 scripts/check-ui-assets.py`: aprovado.
- `python3 scripts/check-form-experience.py`: apontou inicialmente `novalidate` ausente no login e dois botões sem tipo na solicitação hospitalar; ambos foram corrigidos e o gate foi reexecutado na validação final.
- Busca ampla da fase 9: 72 ocorrências históricas, majoritariamente parsing C#, estilos dinâmicos necessários para barras/white-label e código fora do escopo permitido sem SDK. Nenhuma expressão proibida permanece nos arquivos tocados.

## Limitações

Sem SDK .NET não foi possível restaurar, compilar, executar testes do backend, subir a aplicação, autenticar Super Admin/tenant ou produzir screenshot confiável das páginas renderizadas. Nenhum ajuste server-side foi tentado para contornar essa limitação. A ausência de remoto configurado também impede publicar diretamente no GitHub a partir deste checkout; o commit e a solicitação de PR são registrados pelos mecanismos disponíveis no ambiente.
