# Baseline de prontidão comercial v2.17.2

Data da rodada: 2026-09-21. Classificação geral: **parcial / bloqueado por ambiente**. Este relatório não aprova produção.

## 1. Diagnóstico inicial

- O branch de trabalho iniciou limpo, sem alterações locais do usuário.
- A solution oficial, os projetos API/Web/testes, os manifests canônicos, scripts e artefatos foram inspecionados.
- O código contém autenticação JWT e cookie, validação de sessão revogada, isolamento por tenant, catálogo de módulos, Super Admin, jornadas operacionais e Saúde 360. A presença em código é evidência estática, não homologação funcional.
- `ApiRouteStartupValidator.Validate(app)` permanece no startup e o catálogo estático informa zero conflitos.
- O artefato de autenticação anterior tinha somente `not-executed`; ele agora discrimina cada cenário e o bloqueio real, sem converter inspeção estática em sucesso de runtime.

## 2. Correções realizadas

- A evidência de autenticação foi normalizada em JSON estruturado, com ambiente, cenários API/Web, status individual, próximo comando e indicador explícito de que produção não está aprovada.
- A baseline desta rodada passou a separar resultado executado, cobertura estática e requisito ainda não verificado.

## 3. Funcionalidades avançadas encontradas

- Super Admin global, gestão de clientes/tenants, permissões e revogação administrativa de sessões.
- Módulos contratáveis, guardas de assinatura e limites operacionais.
- Agenda, recepção/check-in, painel de chamada, triagem, consulta, prontuário longitudinal, prescrição e financeiro clínico.
- Operação de plantões, escalas, cobertura, conferência, fechamento e financeiro médico.

Essas funcionalidades permanecem **parciais** nesta rodada, pois não houve execução de aplicação e banco.

## 4. Arquivos alterados

- `artifacts/auth-e2e.json` — matriz honesta de execução de autenticação.
- `artifacts/analysis/v2172-commercial-readiness-baseline.md` — relatório desta rodada.

## 5. Migrations e seeds

Nenhuma migration ou seed foi criada ou alterada. Os manifests existentes passaram na validação estática. Instalação limpa e upgrade não foram executados porque `psql` e Docker não estão instalados.

## 6. Comandos executados

- `git status --short --branch`, `git log -5 --oneline --decorate` e inventários com `find`/`rg`.
- `dotnet --info`, `dotnet restore backend/PlantaoPro.sln`, `dotnet build backend/PlantaoPro.sln -c Release --no-restore` e `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Release --no-build`.
- Todos os nove validadores Python requeridos no gate técnico.
- Inspeção de disponibilidade de `dotnet`, `docker`, `psql`, `node`, `npm` e `python`.

## 7. Evidências de sucesso

Passaram com código de saída zero:

- unicidade de controllers;
- compatibilidade C# 10;
- manifests do banco;
- segurança do repositório;
- feedback de UI;
- experiência de formulários;
- regressão de layout;
- UX operacional, incluindo o contrato de fechamento;
- UI SaaS e onboarding.

## 8. Falhas remanescentes

- Build e testes .NET não executaram.
- Swagger não foi iniciado e validado em runtime.
- Login API/Web, JWT, cookie, logout e sessão revogada não foram executados.
- Isolamento multi-tenant, Super Admin, módulos e Saúde 360 não foram exercitados ponta a ponta.
- Instalação limpa, upgrade e verificação de constraints PostgreSQL não foram executados.
- QA visual em navegador não foi executado; portanto não há screenshot novo nesta rodada.

## 9. Bloqueios de ambiente

- `dotnet`: comando ausente (exit 127).
- `docker`: comando ausente.
- `psql`: comando ausente.
- Sem API/Web executáveis, o navegador autenticado e o smoke visual não podem produzir evidência válida.

## 10. Riscos

- Regressões de compilação podem existir apesar dos validadores estáticos.
- Divergências entre schema canônico e PostgreSQL real só serão detectadas na instalação/atualização.
- Regras de tenant, sessão e módulos dependem de dados e transações reais e ainda podem falhar em runtime.
- Uma aprovação comercial baseada apenas na amplitude do código e em testes contratuais seria falso positivo.

## 11. Pendências

1. Disponibilizar SDK .NET 10 compatível, Docker e PostgreSQL/`psql`.
2. Executar restore, build e suíte completa sem `--no-build`.
3. Executar instalação limpa e upgrade canônicos.
4. Subir API/Web, validar Swagger e executar smoke autenticado com todos os perfis.
5. Exercitar tenant suspenso, sessão revogada, módulo bloqueado e limites do plano.
6. Executar jornada Saúde 360 e QA visual responsivo, arquivando screenshots e resultados.

## 12. Próximo passo recomendado

Em uma estação de homologação com os pré-requisitos instalados e segredos efêmeros fora do Git, executar `bash scripts/local/run-homologacao-linux.sh`; em seguida executar o smoke autenticado e o smoke visual. Atualizar `artifacts/auth-e2e.json` para `passed` somente com respostas, claims, cookies, revogação e isolamento realmente observados.

## 13. Classificação dos itens obrigatórios auditados

| Item | Classificação | Evidência desta rodada |
|---|---|---|
| `README.md` e instruções operacionais | pronto com evidência | Fonte canônica e avisos de não produção inspecionados. |
| `artifacts/auth-e2e.json` | pronto como registro de bloqueio | Registra tentativa, exit code, pré-requisitos ausentes e todos os cenários ainda não executados. |
| `artifacts/api-route-conflicts.json` | pronto com evidência estática | Catálogo informa zero conflitos; Swagger runtime continua não verificado. |
| API startup/JWT/sessão | parcial | Configuração e validação de sessão existem; sem processo .NET executável. |
| Web cookie/login/logout | parcial | Implementação e contratos existem; navegador autenticado não iniciado. |
| Perfis obrigatórios | não verificado em runtime | Os sete perfis foram mantidos individualmente como `not-executed` no artefato. |
| Banco, manifests e seeds | parcial | Manifest validado estaticamente; instalação limpa e upgrade bloqueados. |
| Linux/Windows homologação | bloqueado por ambiente | Script Linux retornou 127; PowerShell não se aplica ao executor Linux. |
| Controllers, testes e views | parcial | Inventário e contratos estáticos presentes; build/test indisponíveis. |
| Menu e rotas funcionais | parcial | Menu usa guards de perfil/módulo; navegação HTTP não executada. |
| Super Admin e onboarding | parcial | Controllers/views/validadores encontrados; jornada real não executada. |
| Saúde 360 | parcial | Agenda, triagem, consulta e financeiro encontrados; ponta a ponta não executado. |

## 14. Matriz auditada do menu comercial

Esta matriz registra somente o que foi localizado no menu atual; “funcional” abaixo significa **rota MVC implementada e coberta estaticamente**, não aprovação runtime.

| Módulo | Controller/view principal | Rota MVC | Restrição observada | Status |
|---|---|---|---|---|
| Dashboard | `Home` / `Dashboard` | `/Home/Dashboard` | usuário autenticado | parcial |
| Plantões | `Plantoes` / `Index` | `/Plantoes` | permissão `PLANTOES` | parcial |
| Escalas | `Escalas` / `Index` | `/Escalas` | permissão `ESCALAS` | parcial |
| Médicos | `Medicos` / `Index` | `/Medicos` | gestão + `MEDICOS` | parcial |
| Hospitais | `Hospitais` / `Index` | `/Hospitais` | gestão + `HOSPITAIS` | parcial |
| Financeiro | `Financeiro` / `Index` | `/Financeiro` | perfil financeiro/módulo contratado | parcial |
| Notificações | `Notificacoes` / `Index` | `/Notificacoes` | usuário autenticado | parcial |
| Agenda | `Agenda` / `Index` | `/Agenda` | permissão `AGENDA` | parcial |
| Triagem | `Triagem` / `Index` | `/Triagem` | `SAUDE360_TRIAGEM` | parcial |
| Consulta | `Consultas` / `Index` | `/Consultas` | `SAUDE360_CONSULTAS` | parcial |
| Relatórios | `Relatorios` / `Index` | `/Relatorios` | gestão + `RELATORIOS` | parcial |
| Administração SaaS | `SaasDashboard` / `Index` | `/SaasDashboard` | `ADMINISTRADOR_GLOBAL` | parcial |
| Configurações | `Configuracoes` / `Index` | `/Configuracoes` | gestão + `CONFIGURACOES` | parcial |
| Auditoria | `Auditoria` / `Index` | `/Auditoria` | `ADMINISTRADOR_GLOBAL` | parcial |

Itens solicitados que não aparecem como links independentes no menu auditado — Especialidades, Operação Inteligente, Painel de Chamada, Prescrição e Convênios — permanecem corretamente **não promovidos como validados** nesta rodada. Eles devem ser adicionados apenas após confirmar controller, action, autorização, contratação e resposta HTTP sem 404/500 em homologação real.
