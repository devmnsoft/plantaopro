Repositório: https://github.com/devmnsoft/plantaopro
Branch base: main
PRs já merged e fora de escopo: #496, #499, #500.

Não mexer em login, BCrypt, JWT de produção nem publicar senha na tela de /Account/Login.

Contas Development (já persistidas no PostgreSQL — reutilizar, não recriar e-mail):
- superadmin@mnsoft.example / MnSoft!Demo2026#Admin → ADMINISTRADOR_GLOBAL → Command Center de plataforma
- gestor@santacasa-demo.example / SantaCasa!Demo2026#Gestor → ADMINISTRADOR_CLIENTE → Meu Dia da Santa Casa
- medico@santacasa-demo.example / Medico!Demo2026#Acesso → MEDICO → Meu Dia pessoal
Tenant: santa-casa-demonstracao / cliente SANTA_CASA_DEMONSTRACAO
Unidade já seedada: UNIDADE_DEMO
Módulos já contratados: ESCALAS, EXECUCAO, CONFERENCIA
IDs estáveis das contas: d3f6584c-2c64-4e5a-9ea9-4e1428647510 (super), …7511 (gestor), …7512 (médica), cliente …7501, tenant …7502, unidade …7504.

Problema atual
Depois do login o casco existe (Meu Dia, Command Center, Central de Ações), mas a operação da Santa Casa parece vazia ou com dados descontextualizados. Não há jornada viva de ponta a ponta pronta para demonstração comercial. O prospect não enxerga o ciclo de valor: montar escala -> médico visualizar / fazer check-in -> gestor acompanhar execução -> conferência fechar o plantão.

Requisitos de negócio da demonstração comercial
Garantir que os 3 usuários tenham jornadas ricas, realistas e demonstráveis no mesmo banco que eles já logam (sem criar scripts que rodam em banco diferente do que a aplicação usa):

1. Gestor da Santa Casa (gestor@santacasa-demo.example):
   - Meu Dia com módulos contratados destacados: Escalas, Execução, Conferência.
   - Command Center operacional: 4 plantões na janela [ontem, hoje, amanhã, +3 dias].
     - 1 plantão descoberto precisando de cobertura (+3 dias)
     - 1 plantão com convite pendente para cobertura (amanhã)
     - 1 plantão em execução com check-in realizado hoje
     - 1 plantão elegível a conferência (ontem)
   - Indicadores operacionais reais na tela (hoje, descobertos, confirmações pendentes, profissionais ativos no tenant).
   - Atalhos de decisão funcionando para Escalas, Execução e Conferência.

2. Médica (medico@santacasa-demo.example):
   - Meu Dia pessoal com sua própria agenda:
     - Plantão de ontem realizado (histórico)
     - Plantão de hoje em andamento com check-in registrado
     - Próximo plantão atribuído para amanhã
   - Não deve ver CTA de "Montar escala" (é médica, não gestora). Deve ver "Minha agenda", "Meu plantão atual", "Meus registros".
   - Módulos acessíveis conforme seus perfis, sem erro 403.

3. Superadmin (superadmin@mnsoft.example):
   - Command Center de plataforma com visão global de risco, clientes e saúde da operação.
   - Não misturar dados da Santa Casa como se fossem globais sem filtro de tenant.

4. Isolamento multi-tenant:
   - Toda consulta de plantões, escalas e conferências deve respeitar tenant_id / cliente_id.
   - O médico só vê seus plantões; o gestor só vê a Santa Casa; o superadmin vê a saúde da plataforma.

Entregáveis
- Script de seed demonstrável em database/seeds/development/ (idempotente, usando os IDs canônicos).
- Ajustes de backend (API / queries / DTOs) necessários para alimentar os cards e listas com dados reais da Santa Casa.
- Ajustes de frontend (Views Razor / ViewModels) para garantir que Meu Dia e Command Center renderizem as jornadas sem empty states falsos.
- Documentação funcional da demonstração em artifacts/functional/operacao-demo-santacasa.md com o roteiro passo a passo do que demonstrar com cada usuário.
- Cópia deste prompt em artifacts/prompts/prompt-operacao-demo-santacasa.md.
