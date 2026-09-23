# Auditoria de fechamento — 22/09/2026

## Rodada M1 — A01–A06 (atualização de 23/09/2026)

**Estado:** **M1 parcialmente executado — homologação pendente**. A análise partiu
do commit `6e9d9dd44d07cfde351cbfaee2377dca67b2cd41`, na branch local `work`, sem
alterações locais e sem remoto Git configurado. A evidência desta atualização é
vinculada ao commit que contém este relatório; os artefatos históricos não foram
tratados como resultado da árvore atual.

### Escopo congelado da RC1 e backlog posterior

A RC1 continua sendo um marco futuro e compreende: jornada operacional até o
pagamento; jornada clínica até o recebimento; administração SaaS, permissões,
módulos e cobrança manual; auditoria, relatórios essenciais e recuperação
operacional. Esta rodada não declara a RC1 concluída.

Ficam fora desta rodada e em backlog separado: mobile distribuído, gateway de
pagamento automático, push/WhatsApp, API pública comercial, webhooks externos,
assinatura digital e integrações avançadas.

### Matriz executiva

| Atividade | Problema e evidência | Arquivos envolvidos | Dependências | Critério de aceite | Status | Evidência vinculada ao commit |
| --- | --- | --- | --- | --- | --- | --- |
| A01 — escopo | RC1 não estava delimitada para este gate | este relatório; `backend/docs/execucao/00_PLANO_FECHAMENTO_PRODUCAO.md` | decisão de produto | escopo e backlog explícitos, sem declarar RC1 | aprovado | seção “Escopo congelado” no commit desta entrega |
| A02 — baseline | imagem atual não oferece `dotnet`, `psql` ou `docker` | solução, validadores e workflow | SDK .NET 10 e PostgreSQL 16 | restore, build, suíte e validadores aprovados no mesmo commit | bloqueado | comandos e classificação de evidência abaixo |
| A03 — banco | baseline marcava v1.31.0 como aplicada em fixture mínima; v2.07.0 fazia `ALTER` em `notifications` ausente | migration v2.06.9, manifests, gerador, fixture e teste de upgrade | PostgreSQL 16 descartável | clean install, upgrade, reaplicação, equivalência e dados reais preservados | bloqueado | correção estática aprovada; execução PostgreSQL pendente |
| A04 — login | teste falhava por sequência textual válida, apesar de a intenção ser comportamental | teste v2.16.4, `auth-login.js`, teste Playwright e workflow | .NET, Web e Chromium | POST/antiforgery, foco, envio único, loading, erro, timeout, pageshow e returnUrl validados | bloqueado | contrato frágil substituído por referência à prova navegada; execução pendente |
| A05 — identidade | contratos e seed existem, mas cinco logins/sessões não foram exercitados nesta imagem | relatório de autenticação, seed, Auth/API/Web | banco e runtimes | cinco perfis, senha errada, contexto, cookie, logout/revogação e recuperação | bloqueado | nenhuma credencial ou token foi registrado; runtime pendente |
| A06 — integração | workflow continha jobs que apenas escreviam JSON de sucesso | `.github/workflows/dotnet-ci.yml` | A02–A05 | API/Web, health, Swagger, login navegável e isolamento de dois tenants | bloqueado | jobs simulados removidos e regressão Playwright obrigatória adicionada; execução pendente |

### Causa raiz e compatibilidade do banco

`notifications` e suas tabelas relacionadas nasceram na v1.31.0. O caminho
`baseline` validava somente `usuarios`, `perfis` e `plantoes`, mas registrava como
aplicadas todas as migrations anteriores à v1.13 segundo a ordem histórica do
manifesto. Como a v1.31 aparece antes da v1.13 nessa ordem, uma fixture mínima
ficava com a migration registrada e sem as tabelas. A v2.07 então executava
`ALTER TABLE plantaopro.notifications` e falhava.

A nova migration aditiva v2.06.9 restaura o agregado **completo** anterior à
v2.07 (`notifications`, destinatários, leituras e preferências), antes dos
`ALTER`s. Ela usa somente `CREATE TABLE IF NOT EXISTS`, preserva dados, não muda
checksums históricos e também consta no manifesto de instalação. O consolidado
foi regenerado pelo gerador canônico. A fixture passou a conter usuário, perfil,
vínculo e plantão com valores determinísticos, e o teste confere esses registros
de domínio depois de duas aplicações do upgrade; a tabela de marcadores deixou
de ser a única prova de preservação. Seeds de demonstração continuam opt-in por
`APPLY_DEMO_SEEDS=1`; não são executados automaticamente em produção.

### Login: defeito versus teste

O JavaScript já recuperava o estado em validação inválida, erro devolvido na
view e evento `pageshow`; chamar `resetSubmission()` antes de mostrar o resumo é
o comportamento correto. A falha era uma asserção negativa acoplada exatamente
a essa ordem textual. A proteção não foi simplesmente apagada: o contrato agora
aponta para cenários Playwright que exercitam POST por clique e Enter, duplo
clique, validação cancelada e recuperação pelo histórico. O workflow passou a
executar essa prova contra a Web iniciada. Antiforgery e `returnUrl` local
continuam sob responsabilidade do controller/ASP.NET, sem autenticação no JS.

### Classificação das evidências

- **Comportamental:** testes de regras .NET e o Playwright de submissão; nesta
  imagem ficaram pendentes por ausência de runtime.
- **Integração:** PostgreSQL descartável, API/Web, Swagger, sessão e tenants;
  pendentes nesta imagem.
- **Textual:** validadores Python e contratos que inspecionam arquivos; úteis
  para estrutura, nunca promovidos a prova de funcionamento completo.
- **Visual:** navegação/screenshot em browser real; não executada porque a Web
  não pôde iniciar. Não houve mudança visual perceptível nesta rodada.

### Separação lógica controlada

Não foi feita uma refatoração ampla de `Data.cs` durante uma correção de gate.
O mapa para A07–A08 permanece: Domain concentra normalização/invariantes;
Application deve orquestrar verificação de credenciais, elegibilidade e contexto;
Infrastructure deve conter consultas Dapper, sessão e auditoria; API mantém
binding/autorização/respostas; Web mantém apresentação, cookie e cliente HTTP.
Os contratos públicos, rotas, tenant, autorização e auditoria foram preservados.

### Itens que ainda impedem M1

Faltam executar no mesmo commit: build e suíte .NET; instalação limpa, upgrade,
reaplicação, equivalência e consultas de preservação em PostgreSQL; API e Web;
health e Swagger; cinco contas e destinos; senha incorreta; logout e revogação;
isolamento entre dois tenants (incluindo ausência de contexto, global explícito e
cliente suspenso); e o teste navegável sem loading permanente. A próxima
atividade liberada é repetir A02–A06 em runner com .NET 10, PostgreSQL 16 e
Chromium; A07–A08 só deve avançar após esse gate.

## Resumo executivo

Esta rodada iniciou pela base existente, sem abrir um novo módulo. A inspeção estática e os nove validadores do repositório passaram. A auditoria encontrou uma regra financeira permissiva: qualquer estado de autorização de convênio que não fosse explicitamente negado, cancelado ou expirado liberava faturamento. Isso incluía autorização ausente, pendente e estados futuros desconhecidos. A regra passou a falhar fechada e liberar somente `APROVADA`.

O ambiente desta rodada não possui o executável `dotnet`. Por isso, build, testes .NET, inicialização da aplicação, login, Swagger, PostgreSQL e QA navegada não foram comprovados. **Esta entrega não declara o produto pronto para produção.**

## Classificação por área auditada

| Área | Classificação nesta rodada | Evidência/limite |
| --- | --- | --- |
| Controllers API | funcional (estático) | Validador de unicidade aprovou 133 controllers; runtime não verificado. |
| Controllers Web | funcional (estático) | Validadores de rotas/layout aprovados; runtime não verificado. |
| Services e repositories | parcial | Inspeção dirigida aos fluxos clínico-financeiros; cobertura integral exige testes .NET e banco. |
| DTOs e ViewModels | funcional (estático) | Compatibilidade C# e contratos estáticos aprovados; materialização Dapper não executada. |
| Razor Views e partials | funcional (estático) | Validadores de feedback, formulário, layout, UX operacional e SaaS aprovados. |
| Menus e dashboards | parcial | Contratos estáticos aprovados; links e dados por perfil não foram navegados. |
| Autenticação e login | não verificado por bloqueio de ambiente | Aplicação não iniciou sem SDK .NET. |
| Autorização, perfis e tenant isolation | parcial | Atributos e filtros foram inspecionados; cross-tenant runtime não executado. |
| Plano/módulo SaaS | parcial | Validador SaaS aprovou; bloqueio real de API não foi exercitado. |
| Auditoria, logs e LGPD | parcial | Security check aprovou; persistência e consulta de auditoria não foram exercitadas. |
| Plantões, escalas e convites | parcial | Contratos existentes identificados; fluxo ponta a ponta não executado. |
| Financeiro médico | parcial | Contratos existentes identificados; pagamento real não executado. |
| Saúde 360 clínico | parcial | Rotas e regras existentes inspecionadas; jornada real não executada. |
| Financeiro clínico e convênios | parcial, corrigido | Autorização para faturamento agora é *fail closed*; integração runtime pendente. |
| Migrations e manifests SQL | funcional (estático) | Manifest validator aprovou; instalação limpa/upgrade PostgreSQL não executados. |
| Scripts de homologação e `artifacts/` | funcional (estático) | Inventário presente; script local bloqueia corretamente na ausência do SDK. |
| Template visual e responsividade | funcional (estático) | Validadores aprovados; screenshot/QA em navegador não executados porque a aplicação não iniciou. |

## Relatório final

1. **O que estava quebrado:** a autorização de faturamento aceitava `null`, vazio, `PENDENTE` e qualquer estado desconhecido.
2. **O que foi corrigido:** a regra agora aceita exclusivamente `APROVADA`, ignorando caixa e permitindo diferenças de maiúsculas/minúsculas.
3. **O que foi fechado funcionalmente:** o contrato determinístico de elegibilidade de faturamento por convênio e seus casos de borda.
4. **Template:** nenhuma alteração visual foi feita; os cinco validadores visuais/operacionais passaram.
5. **Regra implementada:** autorização ausente, pendente, negada, cancelada, expirada ou desconhecida bloqueia faturamento.
6. **Arquivos principais:** `ClinicalCommercialRules.cs`, `ClinicalCommercialRulesTests.cs` e este relatório.
7. **Migrations:** nenhuma criada ou alterada; a correção não muda persistência.
8. **Testes:** nove validadores Python aprovados; testes .NET bloqueados pela ausência do SDK.
9. **Build:** não verificado por bloqueio de ambiente (`dotnet: command not found`).
10. **Validadores:** unicidade, C# 10, manifests, segurança, feedback, formulários, layout, UX operacional e SaaS aprovados.
11. **Tenant/permissão:** filtros e atributos existentes foram inspecionados, mas não há evidência runtime nova nesta rodada.
12. **Fluxos homologados:** somente validações estáticas; nenhum fluxo ponta a ponta foi declarado homologado.
13. **Pendências reais:** executar build/test, banco limpo e upgrade, login por perfil, Swagger, smoke autenticado e roteiro manual de 28 passos em ambiente integrado.
14. **Riscos restantes:** regras estáticas podem não estar conectadas a todos os caminhos de persistência; concorrência e integrações externas precisam de prova runtime.
15. **Próximo prompt recomendado:** "Em ambiente com .NET e PostgreSQL, execute a homologação bloqueante completa; corrija primeiro qualquer falha de build/test, depois comprove autorização de convênio, isolamento cross-tenant e os fluxos Plantão → Escala → Pagamento e Paciente → Agendamento → Consulta → Recebimento com evidências reproduzíveis."
