# Telas refatoradas na v1.56.0

## Saúde 360 e módulos médicos

`Views/Saude360/Modulo.cshtml` passou de uma combinação dominante de hero/cards Bootstrap para a composição clínica do produto: `pp-page`, `pp-page-hero`, KPIs, tabela operacional e banner LGPD. Pacientes, Agendamentos, Triagem e rotas clínicas que reutilizam esse módulo recebem a mesma hierarquia sem duplicação de CSS.

`Views/Agendamentos/AgendaPremium.cshtml` recebeu hero clínico, barra de filtros, resumo de status, grid de cards e tipos explícitos nos botões de ação. Dados continuam vindos do view model; o empty state é mantido quando a API não retorna itens.

## Operação

`Views/Escalas/Index.cshtml` agora apresenta empty state compartilhado, contêiner de dados com quantidade real, linguagem de presença/pagamento e ações bloqueadas semanticamente tipadas. A interface não afirma conflito, presença ou pagamento que o DTO não fornece.

Plantões foi auditada e já continha KPIs calculados da página, status de cobertura, especialidade, período, valor, ações, tabela desktop e cards mobile; não houve reescrita cosmética desnecessária.

## SaaS

`Views/Onboarding/Index.cshtml` foi migrada de card Bootstrap isolado para hero, CTA, explicação e stepper de cinco etapas. O formulário de novo cliente, Admin SaaS e Planos já tinham composição `pp-*`; foram mantidos e incluídos/fortalecidos nos gates.

## Regressão automatizada

- Layout: valida footer no shell, `pp-content` flexível, composição das views críticas, botões tipados e links não-placeholder.
- Formulários: onboarding entrou no conjunto crítico, além de login, recuperação, pacientes, agendamentos e plantões.
- SaaS: a landing de onboarding passou a exigir hero, stepper e seção do design system.
- Feedback: continua varrendo arquivos alterados contra APIs nativas, `href="#"` e botões sem tipo.

## Pendências honestas

Sem o SDK .NET não houve runtime, login autenticado nem screenshots. Timeline de Escalas, etapa atual da jornada e ações persistidas da Agenda exigem dados/contratos de backend antes de qualquer UI que não seja fictícia.
