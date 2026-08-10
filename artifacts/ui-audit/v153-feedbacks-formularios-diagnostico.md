# Diagnóstico v1.53 — feedbacks e formulários

## Método
Auditoria estrutural dos componentes compartilhados, login e jornadas com formulários. A rodada priorizou mudanças transversais que alcançam telas reais pelo layout e melhorias diretas em Login, Plantões, Pacientes e Agendamentos.

| Jornada | Leitura | Formulário/validação | Ícone/feedback/confirmação | Mobile | Correção aplicada |
|---|---|---|---|---|---|
| Login / Forgot / Reset | Hierarquia do login competia com o formulário | Erro usava alerta Bootstrap; estados pouco coesos | Segurança e Caps Lock existiam, sem sistema visual comum | Card perdia foco em telas estreitas | Workspace de autenticação responsivo, resumo semântico, campos maiores, loading e suporte global de formulários |
| Plantões | Conteúdo útil, mas alertas interrompiam a leitura | Validação e AJAX tinham apresentações distintas | Confirmação existente, ícones legados | Rodapé e ações podiam comprimir | Formulário semântico, painéis de erro/aviso e feedback AJAX consolidado |
| Escalas | Formulários pontuais em cards Bootstrap | Resumo nem sempre disponível | Confirmações já declarativas em ações críticas | Ações estreitas | Camada global humaniza campos, foco inválido, loading e diálogos responsivos; migração específica permanece incremental |
| Convites | Tabela e ações densas | Poucos formulários longos | Feedback dependia de toast simples | Ações podem quebrar linha | Toast rico global e tipografia/touch targets reutilizáveis |
| Pagamentos | Alta densidade financeira | Ações sensíveis sem linguagem uniforme | Risco precisa continuar contextual por operação | Tabelas exigem overflow | Modal/toast semântico global; textos específicos continuam responsabilidade de cada ação |
| Pacientes | Campos em grade sem agrupamento visual | Erros sem padrão acessível | LGPD sem painel contextual | Ações comprimidas | Card de formulário, resumo assertivo, validação inline e rodapé adaptável |
| Agendamentos | Grade funcional, pouco destacada | Datas dependiam do servidor | Feedback simples | Grade responsiva existente | Card, resumo acessível, estado não salvo e ações mobile |
| Triagem | Alta carga clínica | Sinais vitais exigem regras do domínio | Alertas de risco devem permanecer baseados em dados reais | Densidade alta | Componentes de risco/erro/loading disponíveis globalmente; sem inventar classificação clínica |
| Consultas | Fluxo longo | Finalização requer validação do servidor | Confirmação crítica já usa atributos declarativos em partes do produto | Rodapé longo | Estado ocupado, alterações não salvas e modal acessível transversais |
| Configurações | Cards heterogêneos | Formulários curtos e dispersos | Mudanças sensíveis pedem confirmação | Cards empilham | Banners e confirmação padronizados disponíveis no layout |
| Relatórios | Filtros e resultados competem | Filtros variam | Processamento pouco explícito | Tabela larga | Estados loading/update e tabela legível disponíveis para adoção incremental |

## Componentes compartilhados auditados
`_AuthLayout`, `_ConfirmModal`, `_ToastMessages`, `_ToastRegion`, `_OverlayPortal`, `_StatusBadge`, `_EmptyState`, `_ValidationScriptsPartial`, `_AppSidebar`, `_AppTopbar`, `_WorkspaceHeader`, `plantaopro-toast.js`, `plantaopro-ui.js`, `form-experience.js`, `auth-login.js`, `forms.css`, `feedback.css`, `overlays.css` e `v151-product-experience.css`.

## Decisões
- Preservar endpoints e dados reais: nenhum mock ou sucesso artificial foi introduzido.
- Adotar progressivamente sem quebrar Razor legado: JavaScript adiciona semântica, mas as telas prioritárias alteradas recebem classes explícitas.
- Manter validações clínicas/financeiras no servidor; a interface explica e foca erros sem duplicar regras críticas.
