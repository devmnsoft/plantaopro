# QA final funcional — Release Candidate PlantãoPro

## Resultado executivo

- Build local: Bloqueado por ambiente sem SDK .NET (`dotnet` ausente).
- CI: Pronto via GitHub Actions para restore, build Release e testes.
- Banco: Parcial; ordem documentada em `docs/operacao/ordem-migrations-seeds.md`, execução real PostgreSQL não foi possível neste container.
- Web/API: Parcial por bloqueio de SDK/runtime local; rotas e configurações revisadas por inspeção.
- Mobile médico: Parcial; navegação MVP implementada e `npm install` executado com sucesso após manter dependências Expo já compatíveis do projeto.

## Saúde 360

| Fluxo | Status | Evidência/observação |
| --- | --- | --- |
| Pacientes | Parcial | CRUD e regras existentes foram mantidos; não houve execução Web/API por falta de SDK. Validar CPF duplicado por tenant em homologação. |
| Agendamentos | Parcial | Fluxo documentado; conflito, confirmação, check-in, cancelamento e reagendamento exigem QA com banco real. |
| Painel de chamada | Parcial | Validar modo TV e exposição mínima de dados em ambiente com API no ar. |
| Triagem | Parcial | Validar vínculo paciente/agendamento, sinais vitais, risco e histórico com massa demo. |
| Consultas | Parcial | Validar RBAC clínico e auditoria de visualização/finalização/impressão em runtime. |
| CID | Parcial | Busca/cadastro/importação CSV precisam de teste PostgreSQL real. |
| Prescrição | Parcial | Validar finalização/cancelamento/impressão e ausência de logs sensíveis em runtime. |
| Financeiro Clínica | Parcial | Validar caixa, recebimento, estorno e segregação de conteúdo clínico. |
| Convênios | Parcial | Cadastro/plano/autorização presentes no escopo; glosas/faturamento seguem fluxo parcial. |
| Planos de Saúde | Parcial | Validar vínculo, principalidade, carteirinha e validade com banco real. |

## Plantões, escalas e financeiro médico

| Etapa | Status | Observação |
| --- | --- | --- |
| Criar/publicar plantão | Parcial | Requer execução API/Web com SDK .NET. |
| Médico visualizar/solicitar/aceitar convite | Parcial | Mobile possui navegação; ação real depende de endpoint homologado. |
| Confirmar escala/reduzir vaga | Parcial | Validar regra de vaga nunca negativa em teste integrado. |
| Marcar realizado/gerar pagamento | Parcial | Validar pagamento duplicado proibido em banco real. |
| Contestar/resolver/confirmar pagamento | Parcial | Validar auditoria e notificação em runtime. |

## QA manual executado nesta rodada

- Higiene Git inicial, arquivos versionados sensíveis e varredura de padrões proibidos em Web/API.
- Sanitização de appsettings com placeholders.
- Criação de CI .NET.
- Navegação mobile MVP por inspeção estática.

## Atualização homologação real 2026-07-07

- **Implementado:** CI .NET 10 preview, docker-compose PostgreSQL, scripts únicos de banco, endpoints `/`, `/api/health` e `/api/health/db`, roteiro QA executável e checklist demo.
- **Bloqueado por ambiente:** build/test locais porque `dotnet` não está instalado no container; validação Docker/psql depende de daemon e cliente PostgreSQL; Metro/Expo exige sessão interativa.
- **Classificação:** Release Candidate parcial até GitHub Actions e QA real confirmarem build, testes, banco e fluxos ponta a ponta.

## Rodada 2026-07-07 — QA funcional mínimo

Classificação: **Bloqueado por ambiente** até que .NET 10 SDK preview, Docker e psql estejam disponíveis no executor.

### Fluxo Plantões

1. Login admin/coordenação — não executado: `dotnet` ausente.
2. Criar plantão — não executado: Web/API indisponíveis.
3. Publicar plantão — não executado: Web/API indisponíveis.
4. Médico visualizar/aceitar convite/solicitar — não executado: Web/API indisponíveis.
5. Confirmar escala — não executado: Web/API indisponíveis.
6. Marcar realizada — não executado: Web/API indisponíveis.
7. Gerar pagamento — não executado: Web/API indisponíveis.
8. Confirmar pagamento — não executado: Web/API indisponíveis.
9. Ver auditoria/notificação — não executado: Web/API indisponíveis.

### Fluxo Saúde 360

Paciente, agendamento, confirmação, check-in, painel, triagem, consulta, CID, prescrição, financeiro clínica e convênio/plano permanecem pendentes de execução real por ausência do SDK .NET e banco aplicado.

### Fluxo Segurança

As validações de isolamento por médico, recepção, financeiro, tenant e módulo bloqueado permanecem pendentes de smoke funcional real. Não houve evidência de aprovação, portanto o produto não foi promovido a Homologável nesta rodada.

## Evolução operação inteligente e demo comercial premium — 2026-07-07
- Evoluído dashboard executivo por perfil: Admin Global, Administrador Cliente, Coordenação, Médico, Financeiro e Saúde 360.
- Criado cockpit Operação Inteligente com pendências por prioridade, perfil responsável, CTA seguro e recomendações determinísticas sem IA externa.
- Criada jornada Primeiros Passos por perfil para implantação do tenant e operação diária.
- Agenda clínica recebeu visão comercial por cards para Calendário, AgendaDia, AgendaMedico e CheckIn.
- Relatórios gerenciais priorizam filtros, cards, LGPD e exportação futura bloqueada até auditoria.
- Demo premium documentada com usuários por perfil e seed idempotente `database/seeds/2026_demo_comercial_premium.sql`.
- Mobile médico mantém telas mínimas, fallback amigável, uso de `EXPO_PUBLIC_API_BASE_URL` e sem log de token.
- Classificação: Evolução funcional parcial no ambiente atual quando SDK .NET ou Docker não estiverem disponíveis; Demo premium navegável para apresentação.

## Evidência adicional — 2026-07-08

- `dotnet --info`, `restore`, `build` e `test` não puderam ser executados porque o SDK .NET não está instalado no container (`dotnet: command not found`).
- A varredura de controller confirma correção esperada quando executada após a alteração: apenas `Saude360WebControllers.cs` deve conter `class AgendamentosController`.
- Classificação: evolução funcional implementada, pendente de validação em ambiente com SDK .NET, Docker e PostgreSQL.
