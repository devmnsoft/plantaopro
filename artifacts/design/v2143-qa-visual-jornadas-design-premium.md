# PlantãoPro v2.14.3 — QA visual, jornadas e design premium

## Escopo e restrições reais

Rodada de QA estático realizada em 3 de setembro de 2026. O container não possui o SDK `dotnet`; por isso, em conformidade com a regra da rodada, nenhuma alteração foi feita em C#, banco, migrations, DTOs, services, controllers, projetos ou solução. O trabalho ficou restrito a Razor, CSS, JavaScript estático e esta documentação. Não foram adicionados mocks, dados fictícios, credenciais, secrets ou bypass de autenticação.

## Mapa de telas reais auditadas

O inventário das views Razor confirmou as seguintes áreas reais:

| Área solicitada | Superfícies encontradas | Diagnóstico da rodada |
|---|---|---|
| Login e recuperação | `Account/Login`, `ForgotPassword`, `ResetPassword`, `AccessDenied` | Login já tinha labels, validação, Caps Lock, recuperação, mensagens por motivo de bloqueio/sessão e recuperação do botão. Faltava tornar explícita a etapa de validação e atualizar a identidade da versão. |
| Dashboard | `Home/Dashboard`, `MinhaCentral/Index`, `MeuDia/Index`, dashboards por perfil | Estrutura premium existente; guia contextual global preservado. |
| Central Meu Dia | `MeuDia/Index`, `MinhaCentral/Index` e parciais de prioridades, resumo, kanban e atividades | Jornada e estados reais existentes; acesso permanece condicionado ao menu/guard do servidor. |
| Escalas e plantões | `Escalas/*`, `Plantoes/*`, `CentralEscala/*`, `OperacaoPremium/*` | Fluxo operacional, filtros, detalhe, estados vazios e cartões mobile existentes. O CSS v2.14.3 melhora foco, tabelas e quebra de conteúdo. |
| Médicos/profissionais | `Medicos/*`, `MinhaAgenda/*` | Cadastros, agenda, presença e pagamentos encontrados; sem solicitação manual de ID no fluxo auditado. |
| Hospitais/unidades | `Hospitais/*`, `HospitalArea/*` | Listagem, formulário e detalhe reais encontrados. |
| Especialidades | `Especialidades/*` | Listagem, formulário e detalhe reais encontrados. |
| Financeiro | `Financeiro/*`, `Pagamentos/*`, `ClinicaFinanceiro/*`, `FaturamentoSaas/*` | KPIs, filtros, status, detalhe, empty state e versão mobile existentes; acabamento de tabela e controles uniformizado. |
| Relatórios/BI | `Relatorios/*`, `Bi/*`, `Inteligencia/*` | Superfícies reais encontradas, protegidas por contexto/perfil. |
| Usuários | `Usuarios/*` e `Usuario/*` | Estado honesto sem dados fake e gestão por tenant encontrados. |
| Perfis e permissões | `Perfis/*`, `Permissoes/Matriz` | Matriz e revisão por perfil existentes; segregação declarada no guia. |
| Administração global | `AdminSaas/*`, `Clientes/*`, `Assinaturas/*`, `FeatureFlags/*`, `CustomerSuccess/*`, `Observabilidade/*` | Cockpit global real encontrado. Havia orientação local duplicada com o guia do layout; foi removida e substituída por guardrails de escopo. |
| Configurações | `Configuracoes/*`, `Parametrizacoes/*`, `WhiteLabel/*`, `Integracoes/*` | Telas de configuração, saúde, parametrização e integração encontradas. |
| Auditoria | `Auditoria/Index`, `Auditoria/Details`, `Observabilidade/Acessos` | Consulta e detalhe de eventos reais encontrados. |
| Erros e estados | `Shared/Error`, `_ErrorState*`, `_EmptyState`, `Account/AccessDenied` | Componentes reutilizáveis encontrados; mensagens inline e regiões `aria-live` preservadas. |

Além das áreas acima, o inventário encontrou módulos clínicos (pacientes, triagem, consultas, agendamentos, convênios), LGPD, comunicação, onboarding, implantação e operação assistida. Eles não foram funcionalmente alterados nesta rodada.

## Telas e componentes alterados

- **Login:** painel de progresso acessível durante a validação, mensagem recuperável quando a resposta demora e identificação v2.14.3.
- **Central Global MNSOFT:** orientação duplicada removida; guardrails visuais mostram escopo global, auditoria e necessidade de selecionar cliente.
- **Shell autenticado e de autenticação:** nova camada CSS v2.14.3 carregada nos dois layouts.
- **Telas internas beneficiadas pela camada global:** foco visível, bordas/elevação de cards e tabelas, erros por campo, quebra de textos extensos e ajustes mobile.

## Jornadas críticas revisadas

1. **Login → dashboard:** o botão informa “Verificando acesso…”, a região viva descreve a validação de conta/contexto e o POST normal continua responsável pela sessão e destino.
2. **Login → sessão expirada → novo login:** `reason=expired`/`session-expired` produz mensagem humana; os campos continuam operáveis e o foco vai para erros retornados.
3. **Falha ou conexão lenta no login:** após 15 segundos o botão volta ao estado utilizável, os dados digitados são mantidos e a tela instrui uma nova tentativa. Eventos offline também liberam a ação e exibem erro recuperável.
4. **Super Admin → selecionar cliente → visualizar contexto:** o cockpit mantém CTA de seleção, banner global e agora exibe três guardrails persistentes antes dos atalhos.
5. **Super Admin → bloquear/desbloquear cliente:** ação continua dependente de dados e autorização reais; o padrão de confirmação acessível e trilha de auditoria existente foi preservado, sem criar endpoint ou bypass.
6. **Cliente → usuários/perfis:** as telas continuam limitadas ao tenant e o guia global explica audiência, contexto, ação e impacto. O estado de usuários não inventa registros.
7. **Gestor → escala/plantão:** filtros com labels, seletores de data, status, jornada operacional, detalhe e cartões mobile foram verificados; ações indisponíveis permanecem honestamente desabilitadas.
8. **Financeiro → pagamento:** filtros, KPIs calculados dos registros recebidos, estados vazios honestos, status e detalhe foram verificados; nenhum valor fictício foi introduzido.
9. **Usuário comum → módulo permitido:** menus e telas continuam dependentes das permissões existentes. A rodada não alterou guards, sessão, tenant ou autorização de backend.

## Login premium

A tela mantém “E-mail, CPF ou CNPJ”, senha, aviso de Caps Lock, revelar/ocultar senha, recuperação de senha, validação por campo, erros do servidor e mensagens específicas para cliente bloqueado, usuário bloqueado, sessão expirada, contexto ausente e acesso negado. O estado novo narra a validação sem prometer sucesso. Em demora, o controle é devolvido ao usuário; em retorno pelo histórico (`pageshow`) o botão também é normalizado. O foco por teclado ganhou contraste consistente e `prefers-reduced-motion` é respeitado.

## Super Administrador e isolamento

O cockpit declara explicitamente “Visão global MNSOFT”, oferece seleção de cliente e acesso a clientes/tenants, usuários, perfis, cobranças, auditoria, módulos e registros operacionais. Os novos chips são apenas orientação visual: não elevam privilégio nem alteram contexto. O banner de acesso assistido e sua saída continuam fornecidos pelo shell existente. Cliente comum não recebe visão global por esta mudança; a autorização continua no servidor e o menu continua baseado em perfil/contexto.

## Formulários e sistema visual

Nos formulários tocados diretamente (login), há labels visíveis, ajuda, mensagens por campo, resumo de erro, ação primária, recuperação secundária, estado ocupado e estado recuperável. Não há relacionamento ou dado de CPF/CNPJ a ser cadastrado no login, portanto não foi aplicada máscara destrutiva ao identificador híbrido.

A camada v2.14.3 uniformiza foco, hover de campos, erro por campo, elevação moderada, cabeçalhos de tabela, wrapping e layouts mobile. O shell já injeta exatamente um bloco global **Como usar esta tela** nas telas autenticadas; o cockpit tinha um segundo bloco local, agora removido. O login, fora do shell autenticado, mantém seu próprio bloco discreto.

## Problemas encontrados e tratamento

- `dotnet` ausente: impediu build, testes e execução local; backend ficou intocado.
- Feedback “Processando...” existe no design system para ações gerais, mas os fluxos Ajax possuem `finally` que restaura botões; o login usa controle próprio e timeout de recuperação.
- Orientação duplicada no cockpit global: corrigida.
- Conteúdo longo em tabelas e títulos poderia estourar no mobile: corrigido na camada v2.14.3.
- Muitos módulos e versões históricas de CSS coexistem. A rodada adicionou uma camada final pequena e isolada, sem reescrever estilos antigos de alto risco.
- Não foi possível capturar screenshot real sem runtime .NET/aplicação inicializável; não foi criado mock visual para contornar essa limitação.

## Comandos executados e resultados

### Diagnóstico

- `pwd` — sucesso; `/workspace/plantaopro`.
- `git status --short --branch` — sucesso; branch inicial limpa.
- `git remote -v || true` — executado; nenhum remote configurado no container.
- `dotnet --info || true` e `dotnet --list-sdks || true` — `dotnet: command not found`.
- buscas `rg` solicitadas — executadas; identificaram os estados “Processando...” controlados em JavaScript e mapearam superfícies de layout, login, dashboard, Super Admin, tenant, perfil e permissão.

### Validação

Os resultados finais estão registrados no histórico da tarefa. `git diff --check`, as buscas por padrões proibidos e por secrets foram executadas. Os comandos .NET não foram executados porque o binário não existe, conforme a restrição explícita da rodada.
