# M2 — responsabilidades, isolamento e operações efetivas

Data da auditoria: 2026-09-23. Baseline: branch `work`, commit `63605af` (merge de M1; implementação `47399f2`). Este documento é evidência de engenharia, não homologação da RC1.

## Revalidação de M1

O merge atual contém o reparo incremental `345_v2069_reparar_base_notificacoes.sql`, fixture de upgrade, validação CI de banco e regressão do envio do login. A inspeção confirmou os contratos de login, sessão persistida, logout/revogação e contexto. A execução local ficou bloqueada porque a imagem não contém o SDK `dotnet`; portanto build, testes, instalação limpa/upgrade, API/Web e login real **não foram reconfirmados nesta máquina**. Os artefatos versionados anteriores não foram tratados como nova execução. O CI canônico continua em `.github/workflows/dotnet-ci.yml` e `.github/workflows/database-one-click.yml`; execução/homologação em `docs/manual_execucao.md` e `docs/HOMOLOGACAO_LOCAL.md`.

## Mapa executável de responsabilidades

| Domínio | Responsabilidade atual / classe | Consumidores e rotas | Persistência / regra | Contrato atual | Destino e classificação | Risco / validação |
|---|---|---|---|---|---|---|
| Identidade e acesso | `AuthService.LoginAsync`, antes junto de `Data.cs`; normalização e elegibilidade extraídas para `Domain.Identity` | Web/mobile; `POST /api/auth/login`, login mobile | `usuarios`, `medicos`, `clientes`, `tenants`, `usuarios_perfis`, `login_tentativas`; exatamente uma credencial, usuário e perfil ativos | `LoginRequest` → `ApiResponse<LoginResponse>` | política canônica em Domain; orquestração ainda deve migrar a Application e SQL a Infrastructure | PostgreSQL: credencial válida/inválida, ambiguidade, perfil revogado |
| Sessões | `AuthenticationSessionService` | middleware JWT, logout, refresh | `auth_sessoes`; sessão não revogada, usuário ativo, claims de tenant/cliente iguais à sessão | `ClaimsPrincipal` → validação/revogação | implementação canônica atual; mover interface à Application e Dapper à Infrastructure | logout real, expiração, claims antigas |
| Clientes e tenants | `ContextoService`/`ContextoRepository`; `UsuarioContextService` legado | `/api/contexto/*`, menus e serviços | `usuario_tenant_acessos`, `tenants`, `clientes`, `auth_sessoes`; alvo deve ser autorizado | requests/DTOs tipados de contexto | canônico para troca de contexto; eliminar gradualmente leituras legadas | tenant_id diferente de cliente_id, duas abas, suspensão |
| Catálogo/assinatura SaaS | `SaasCommercialController`, serviços SaaS | rotas `/api/saas/*` e Web comercial | planos, módulos, assinaturas, clientes | DTOs SaaS próprios | manter separado de contrato operacional/convênio; mapear para Application depois | cobrança, limites e compatibilidade |
| Plantões/escalas/convites | `Data.cs`, controllers operacionais e `MobileController` | API/Web/mobile | `plantoes`, `escalas`, convites, unidades | DTOs operacionais/mobile | fatias futuras; repetição só é equivalente após caracterização | propriedade de IDs e concorrência |
| Disponibilidade/substituições | serviços operacionais em `Data.cs`/controllers | API/Web/mobile | disponibilidade, solicitações/trocas | DTOs operacionais | futuro; não fundir convite com substituição | estados semelhantes têm significado distinto |
| Execução/conferência/pagamentos | `ExecutionConferenceService`, fechamentos e pagamentos | rotas de execução/financeiro | escalas, conferência, pagamentos | contratos de fechamento e pagamento | serviços existentes são destino proposto | transação e idempotência |
| Pacientes/agenda/recepção | `Saude360ClinicalService`, controllers clínicos | API/Web | pacientes, agendamentos, recepção | `Saude360Dtos`/`ReceptionDtos` | separar por caso de uso em Application | LGPD e isolamento por cliente |
| Triagem/consultas/prontuário | `Saude360ClinicalService`, `Clinical/*` | controllers clínicos | triagem, consultas, prontuário | contratos clínicos | `Clinical/ConsultaApplicationService` é direção canônica | autorização por paciente/unidade |
| Prescrições/documentos | serviços clínicos | controllers Saúde360 | prescrições/documentos | DTOs clínicos | futuro; compatibilidade legada necessária | assinatura, histórico e acesso |
| Convênios/autorizações | `V116ConvenioService` e serviços clínicos | rotas convênio | convênios/autorizações | contrato de convênio específico | manter distinto de assinatura SaaS e cobertura operacional | faturamento e vínculo cruzado |
| Caixa/recebimentos | `V116CaixaService`, financeiro | rotas financeiras | caixa, contas a receber | DTOs financeiros | destino existente | tenant e integridade contábil |
| Relatórios/integrações | `Fase6BiIntegracoesService` e controller | `/api/bi`, `/api/relatorios`, `/api/integracoes`, `/api/public/v1` | BI, `api_keys`, webhooks | DTOs Fase6 | operações reais preservadas; stubs agora indisponíveis | PostgreSQL, leitura após escrita, consumidores |

### Distinções canônicas

* Assinatura SaaS (plano/módulo/limite/cobrança), contrato operacional (unidade/cobertura/vigência/preço de plantão) e convênio (procedimento/autorização/faturamento) são conceitos distintos e não devem compartilhar formulário ou DTO universal.
* `LoginIdentity` é política pura canônica. `PasswordHashService` permanece o único algoritmo de senha. `AuthService` permanece adapter legado em transição; sua remoção antes da migração integral quebraria Web/mobile.
* `ContextoService` + `ContextoRepository` são a implementação canônica de seleção autorizada. Leitura de claims em `UsuarioContextService` é compatibilidade legada, não autorização suficiente por si só.
* Respostas vazias de consultas podem ser estado válido. Comandos sem persistência não podem retornar sucesso.

## Alterações desta rodada

### A08 — identidade/acesso

A classificação, normalização, fingerprint seguro e elegibilidade foram extraídos para `PlantaoPro.Domain/Identity/LoginIdentity.cs`, sem SQL/HTTP. API e mobile reutilizam o contrato, preservando e-mail/CPF/CNPJ já aceitos e `PasswordHashService`. A persistência/orquestração completa do `AuthService` ainda está concentrada em `Data.cs`; logo A08 está **parcial**, não concluída.

### A09 — isolamento

Os dashboards e recursos Fase6 deixaram de transformar ausência de cliente em `Guid.Empty`/filtro global. Operações de relatório, API keys e webhooks exigem cliente explícito; SQL operacional usa igualdade obrigatória. Revogação de API key confirma uma linha pertencente à organização antes de informar sucesso. A cobertura PostgreSQL de dois tenants não pôde ser executada neste ambiente; A09 está **parcial e bloqueada para conclusão**.

### A10 — operações sem efeito

| Endpoint | Decisão |
|---|---|
| dashboards, executar relatório, criar/listar/revogar API key, criar/listar webhook | preservado; há acesso real ao banco (revogação endurecida) |
| configurar widgets; salvar/excluir filtro; rotação/permissões de API key; alterar/ativar/desativar webhook; reenviar entrega | fora da fatia implementada: agora HTTP 501, sem alegar sucesso |
| exportar CSV | HTTP 501: o arquivo anterior continha somente mensagem, não dados |
| agendamento na API pública | HTTP 501: não gera mais GUID sem persistência |
| PDF | 501 já existente, preservado |
| indicadores/series/ranking/alertas e consultas públicas vazias | consultas/empty state preservados; exigem decisão de produto antes da RC1 |

Não houve alteração de schema ou migration.

## Matriz de aceite e evidência

| Teste | Estado | Evidência / pendência |
|---|---|---|
| T01–T02 | não executado | caracterização pura adicionada; login PostgreSQL requer SDK e banco |
| T03–T04 | parcial | ausência de cliente bloqueada em Fase6; contexto adulterado precisa integração |
| T05–T06 | bloqueado | obrigatório executar cenário PostgreSQL com tenant_id e cliente_id diferentes |
| T07–T08 | não executado | sessão valida status/perfil/tenant; exige regressão real |
| T09 | pendente | autorização de módulo transversal não foi comprovada nesta fatia |
| T10–T11 | não executado | seleção global/tenant precisa QA integrada e múltiplas abas |
| T12 | parcial | falsos sucessos bloqueados; revogação verifica efeito; releitura PostgreSQL pendente |
| T13 | parcial | rotas preservadas, mas respostas de stubs mudaram intencionalmente para 501 |
| T14 | não executado | nenhuma interface visual foi alterada; captura não se aplica |
| T15 | parcial | política pura extraída; AuthService/SQL ainda precisam migrar de API |

## Riscos, bloqueios e próximos passos

1. Instalar a versão de SDK fixada pelo repositório e executar CI local completo, banco limpo, upgrade/reaplicação e os E2E API/Web.
2. Criar testes PostgreSQL para login, sessão revogada, tenants distintos e cliente suspenso; não substituir por inspeção de strings.
3. Migrar repositórios de identidade/sessão/contexto para Infrastructure e casos de uso para Application em passos pequenos.
4. Ajustar interfaces somente após validar os consumidores dos endpoints agora 501; nenhuma captura foi produzida porque não houve mudança visual executável.
5. Validar autorização por módulo e propriedade para todo endpoint Fase6, inclusive consultas.

## Status do marco

* **A07:** mapa entregue; completo como diagnóstico, execução futura priorizada.
* **A08:** parcial.
* **A09:** parcial; isolamento integral não comprovado.
* **A10:** falsos sucessos conhecidos bloqueados; persistências preservadas ainda requerem integração.
* **M2:** **não concluído / não homologado**.
* **M3:** **não liberado**, pois os critérios de isolamento PostgreSQL e revalidação de M1/M2 não foram executados.
