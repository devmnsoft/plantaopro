# v2.13.6 — Super Admin global e design premium

## Escopo e limitação de execução

A pré-validação confirmou que o SDK `dotnet` não está instalado no ambiente (`dotnet: command not found`). Conforme a regra da rodada, nenhuma controller, service, DTO, autenticação, banco ou migration foi alterada. Esta entrega limita-se à apresentação Razor, navegação, CSS e inspeção estática. A implementação server-side existente continua sendo a autoridade de autorização, isolamento e auditoria; os agregados globais permanecem explicitamente indisponíveis em vez de serem simulados.

## Evoluções de interface

- A Central administrativa passou a se identificar como **Central Global MNSOFT**, com indicador inequívoco de escopo global e aviso sobre auditoria.
- Foi incluída consulta progressiva de clientes por nome/CNPJ e status, enviada à rota real de Clientes, sem campo de ID manual.
- O painel contém oito slots executivos: clientes totais, ativos e bloqueados, usuários ativos, plantões ativos, pendências financeiras, eventos críticos e saúde geral. Enquanto não houver fonte global autorizada, cada slot mostra estado indisponível, nunca zero ou dado fictício.
- A navegação global ganhou acessos agrupados para Central Global, Clientes, Cobranças, Logs e saúde e Auditoria. Foi corrigido o link quebrado que apontava para `AdminSaas/Dashboard`, action inexistente, passando a usar `AdminSaas/Index`.
- Foram adicionados atalhos para registros operacionais e observabilidade. Todos apontam para rotas existentes e mantêm a autorização do servidor.
- O grid responsivo usa quatro, duas ou uma coluna conforme a largura, preservando áreas de toque e legibilidade.

## Super Admin e isolamento do tenant

A controller existente de Admin SaaS exige os papéis globais/suporte/auditor no servidor. A seção MNSOFT do menu é renderizada somente para `AdministradorGlobal`. Administradores de cliente continuam limitados ao bloco Gestão e não recebem os atalhos globais. A UI não cria claims, não presume tenant, não concede permissão e não transporta um `tenant_id` informado manualmente.

A seleção apresentada é uma consulta, não uma impersonação cosmética. Entrada e saída de contexto assistido devem continuar exclusivamente pelo fluxo server-side auditado existente; sem SDK para validar ou alterar esse fluxo, a tela não oferece um botão que possa sugerir uma operação insegura.

## Matriz de telas e permissões (inventário desta rodada)

| Tela | Rota | Controller/action | Módulo | Permissão observada | Menu | Super Admin | Tenant comum | `tenant_id` | Problema/decisão |
|---|---|---|---|---|---|---|---|---|---|
| Central Global | `/AdminSaas` | `AdminSaas/Index` | Governança | AdminGlobal, Suporte, Auditor | Sim, apenas global | Sim | Não | Não presume | Link anterior apontava para action inexistente; corrigido |
| Clientes | `/Clientes` | `Clientes/Index` | Clientes | Autorização da controller | Sim, global | Sim | Conforme servidor | Fonte da API | Busca encaminhada à rota real |
| Usuários | `/Usuarios` | `Usuarios/Index` | Segurança | Gestão + servidor | Sim, gestão | Sim | Admin do tenant | Fonte da API | UI não eleva privilégio |
| Perfis | `/Perfis` | `Perfis/Index` | Segurança | Gestão + servidor | Sim, gestão | Sim | Admin do tenant | Fonte da API | Sem alteração server-side |
| Permissões | `/Permissoes/Matriz` | `Permissoes/Matriz` | Segurança | Gestão + servidor | Sim, gestão | Sim | Admin autorizado | Fonte da API | Matriz efetiva não é inventada |
| Profissionais | `/Medicos` | `Medicos/Index` | Operação | Gestão + servidor | Sim | Sim | Conforme papel | Fonte da API | Atalho pela navegação existente |
| Unidades | `/Hospitais` | `Hospitais/Index` | Operação | Gestão + servidor | Sim | Sim | Conforme papel | Fonte da API | Atalho pela navegação existente |
| Escalas | `/Escalas` | `Escalas/Index` | Operação | Papel operacional | Sim | Conforme claims | Conforme papel | Fonte da API | Sem alteração de autorização |
| Plantões | `/Plantoes` | `Plantoes/Index` | Operação | Papel operacional | Sim | Conforme claims | Conforme papel | Fonte da API | Atalho global preserva policy |
| Financeiro | `/Financeiro` | `Financeiro/Index` | Financeiro | Papel financeiro | Sim | Conforme claims | Conforme papel | Fonte da API | Sem bypass visual |
| Cobranças | `/Billing` | `Billing/Index` | Billing | Autorização do servidor | Sim, global | Sim | Não exibido | Fonte da API | Novo atalho global |
| Relatórios | `/Relatorios` | `Relatorios/Index` | Relatórios | Gestão + servidor | Sim | Sim | Conforme papel | Fonte da API | Sem exportação nova |
| Auditoria | `/Auditoria` | `Auditoria/Index` | Governança | Autorização do servidor | Sim, global | Sim | Não exibido globalmente | Fonte da API | Novo destaque de acesso auditado |
| Logs e saúde | `/Observabilidade` | `Observabilidade/Index` | Observabilidade | Autorização do servidor | Sim, global | Sim | Não exibido globalmente | Fonte da API | Novo atalho global |
| Configurações | `/Configuracoes` | `Configuracoes/Index` | Configurações | Gestão + servidor | Sim | Sim | Admin autorizado | Fonte da API | Sem mudança de escopo |
| LGPD | `/Lgpd` | `Lgpd/Index` | Privacidade | Autorização do servidor | Sim | Sim | Conforme servidor | Fonte da API | Dados sensíveis não são agregados |

## Formulários, estados e mensagens

O único formulário tocado é o filtro global: possui labels visíveis, controles sem ID manual, busca semântica e submit normal. O estado sem integração usa “Fonte global indisponível”, distinguindo ausência de dado de valores zero. Não foram introduzidos diálogos nativos, confirmação insegura, mocks ou segredos. Operações críticas não foram adicionadas sem o modal/endpoint auditável necessário.

## Ajuda contextual

A Central mantém **Como usar esta tela**, explica finalidade, público, sequência de ações e cuidado com o contexto. O aviso global explicita exclusividade do Super Administrador MNSOFT e auditoria de ações sensíveis.

## Comandos executados e resultados

- `git status --short --branch`: executado; árvore inicialmente limpa na branch `work`.
- `git remote -v`: executado; nenhum remote configurado no clone.
- `dotnet --info` e `dotnet --list-sdks`: indisponíveis (`command not found`).
- Inventário com `rg` e `find`: executado sobre controllers, views, navegação, papéis e rotas.
- Builds, restore e testes .NET: não executáveis sem SDK.
- `git diff --check` e varredura de padrões proibidos: executados após as alterações.

## Limitações restantes

- Os KPIs globais, módulos por cliente, explorador unificado de registros e alternância de contexto exigem endpoints agregados, policies e trilhas de auditoria validadas no backend. Permanecem em estado honesto de indisponibilidade.
- Login, claims, isolamento das queries e testes de integração não puderam ser compilados ou executados neste ambiente. Nenhum desses componentes foi alterado.
- Não foi possível iniciar a aplicação nem capturar screenshot sem o runtime/SDK .NET.
