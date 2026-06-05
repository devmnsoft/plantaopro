# PlantãoPro — relatório de sanitização e avanço beta homologável (2026-06-05)

## Escopo

Este relatório registra a rodada de validação do PlantãoPro para beta homologável e operação assistida, com foco em:

- higiene do repositório após execução acidental de prompt de produto incorreto;
- estabilidade mínima de API, Web, autenticação, Swagger e health check;
- prontidão documental para homologação comercial, operação assistida, SaaS básico, mobile MVP, suporte e Customer Success;
- rastreabilidade das pendências que exigem ambiente com SDK .NET, banco PostgreSQL e usuários de demonstração.

## Evidências de sanitização

- Branch atual validada: `branch work`.
- Backup local solicitado criado antes de alterações: `backup/antes-beta-homologavel-plantaopro`.
- Histórico recente contém merge revertido de uma branch indevida; o conteúdo versionado atual foi varrido para impedir resíduos ativos do produto incorreto.
- Diretórios externos proibidos na raiz permanecem bloqueados por teste de contrato.
- Termos e artefatos do produto incorreto permanecem bloqueados por teste de contrato em arquivos versionáveis.
- Referências a React Native/Expo permitido são legítimas apenas para o app mobile do próprio PlantãoPro (`mobile/PlantaoPro.App`) e para os documentos de Sprint Zero mobile.

## Evidências técnicas revisadas

- `/api/health` agora retorna `ApiResponse<HealthDto>` tipado, mantendo payload padronizado para smoke tests, Swagger e observabilidade.
- Swagger permanece habilitado em ambiente de desenvolvimento.
- API Mobile MVP permanece agrupada com tag `Mobile` e protegida por JWT, exceto login.
- Operação Assistida mantém endpoints de clientes, detalhe, checklist, ocorrências, resolução e treinamentos.
- Layout global mantém `_ToastMessages` sem model de página e `_ConfirmModal` com `ConfirmModalViewModel` explícito.

## Checklist beta homologável desta rodada

| Área | Critério | Status |
| --- | --- | --- |
| Sanitização | Sem diretórios externos proibidos versionados | Validado por inspeção e teste de contrato |
| Sanitização | Sem termos do produto incorreto em arquivos versionáveis | Validado por inspeção e teste de contrato |
| API | `/api/health` com resposta padronizada | Ajustado nesta rodada |
| API | Swagger em desenvolvimento | Mantido |
| Mobile | Login anônimo e demais endpoints com JWT | Mantido |
| Mobile | Bloqueio por plano sem mobile com auditoria | Mantido |
| Web | Toast global desacoplado do model da página | Mantido |
| Web | Modal global de confirmação com model explícito | Mantido |
| Docs | Homologação, demo, deploy, SaaS, suporte, Customer Success e mobile | Mantido |

## Smoke test manual recomendado

1. Subir PostgreSQL com schema `plantaopro` atualizado.
2. Executar scripts incrementais em `backend/sql`.
3. Subir `backend/PlantaoPro.Api` em ambiente Development.
4. Abrir `/swagger`.
5. Abrir `/api/health` e confirmar `success=true`, `data.application=PlantaoPro.Api` e `data.status=Healthy`.
6. Subir `backend/PlantaoPro.Web` apontando para a API local.
7. Abrir `/Account/Login`.
8. Login como administrador global e validar redirecionamento para Dashboard.
9. Logout.
10. Login como médico e validar redirecionamento para Minha Agenda.
11. Validar Dashboard, Médicos, Plantões, Financeiro, Operação Assistida, Auditoria e Observabilidade sem erro de partial.
12. Validar 403/404 amigáveis.
13. Executar fluxo operacional médico ponta a ponta com os roteiros de homologação.
14. Executar faturamento SaaS básico.
15. Executar chamada mobile protegida sem token e confirmar 401.
16. Executar chamada mobile com plano sem mobile e confirmar 403 amigável, quando aplicável.

## Pendências controladas

- Build e testes automatizados dependem de SDK .NET disponível no ambiente executor.
- Teste manual completo depende de PostgreSQL, seeds de demonstração e credenciais válidas.
- Responsividade final deve ser homologada em navegador real com perfis administrador, coordenação, médico, financeiro e hospital.
- Validações de pagamento, fatura, suspensão e limites de plano devem ser reexecutadas em base limpa antes do go-live beta.

## Critério de aceite para operação assistida

O PlantãoPro pode seguir para operação assistida quando:

- build de API, Web e testes estiver verde em ambiente com SDK .NET;
- `/swagger` e `/api/health` responderem localmente;
- login/logout por cookie e JWT mobile funcionarem;
- fluxo operacional e SaaS básico forem executados sem exceção técnica para usuário;
- auditoria registrar ações críticas e acessos negados;
- relatórios/exportações respeitarem `cliente_id`;
- documentação de homologação e deploy for anexada ao PR de release beta.
