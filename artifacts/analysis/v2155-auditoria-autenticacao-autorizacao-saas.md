# v2.15.5 — auditoria real de autenticação e autorização SaaS

Data: 2026-09-10 (UTC)  
Base local: `35c0225` (`work`), árvore limpa antes da criação de `codex/auditoria-autorizacao-saas-v2155`.  
Stack confirmada por projeto: .NET 10, C# 10, ASP.NET Core MVC/API, Razor, Dapper/Npgsql e PostgreSQL. O SDK `dotnet` não está instalado neste contêiner; portanto, as alterações e os testes adicionados estão **revisáveis, mas não homologados em runtime**.

## Matriz de estado real

| Jornada | Evidência de código (não apenas documentação) | Estado | Risco/prioridade |
|---|---|---|---|
| Login Web → API → banco → cookie | `Views/Account/Login.cshtml` faz POST com antiforgery; `wwwroot/js/auth-login.js` valida antes do loading, impede duplo envio e restaura no timeout/pageshow; `AccountController.Login` chama `api/auth/login`, aceita somente `Url.IsLocalUrl`, cria cookie e mantém JWT apenas em claim/camada servidor e sessão; `AuthController.Login` chama `AuthService.LoginAsync`; `Data.cs` consulta PostgreSQL, BCrypt, cria `auth_sessoes`, claims, módulos e permissões. | **Implementada sem validação executável nesta rodada** | P0: executar credencial válida/inválida, API offline, timeout, expiração e back navigation. |
| Recuperação de senha | `AccountController.ForgotPassword` e `AuthController.forgot-password/reset-password` existem; resposta não revela cadastro. | **Implementada sem validação** | P1: fluxo de e-mail real não homologado. |
| Clientes e contexto global | `ClientesController` API exige Administrador Global. `ContextoRepository` lista tenant ativo, valida vínculo e grava `contexto_sessoes`, `contexto_trocas` e recentes em transação. | **Parcial** | P0: seleção ainda não renova cookie/JWT; gravação isolada não muda claims já emitidos. |
| Usuários do cliente | `SecurityAdministrationService.SalvarUsuarioAsync` valida tenant/perfis, impede papel global, persiste usuário e vínculos em transação, audita e revoga sessões na edição; Web `UsuariosController`/views consomem o fluxo. | **Implementada sem validação** | P0: PostgreSQL e E2E pendentes. |
| Perfis e matriz | `PerfisController` + `SelfServiceSaasService.SalvarPerfilAsync` possuem CRUD persistente; `PermissoesController.PersistirAsync` troca permissões em transação e audita. Endpoints homônimos sob `api/seguranca/perfis` ainda são placeholders. | **Parcial** | P1: remover/encaminhar contrato duplicado para não anunciar persistência falsa. |
| Decisão efetiva de acesso | `EffectivePermissionService.TestarAsync` verifica usuário, papel global, vínculo, tenant/cliente, módulo contratado, grants e denies. Nesta rodada passou a aceitar vínculo secundário somente quando ativo e vigente. | **Implementada, regressões preparadas** | P0: integração PostgreSQL pendente. |
| Módulos/assinatura | `SaasModuleCatalogService` lê catálogo e `tenant_modulos`, distingue contratado/habilitado e audita alteração global; controllers de planos/assinaturas existem. | **Parcial** | P1: não confundir catálogo/persistência com cobrança e pagamento integrados. |
| Plantões e escalas | Controllers/services e telas existem, com implementação moderna tenant-aware e legado coexistente. | **Parcial** | P1: queries legadas de operação precisam revisão sistemática. |
| Financeiro | Serviços de pagamento/fechamento usam tenant em diversos contratos, mas há implementações históricas paralelas. | **Parcial** | P2 nesta entrega; homologar somente no bloco posterior. |
| Saúde 360 | Agenda, recepção, triagem, consulta e serviços clínicos existem; a entrega anterior não foi compilada por ausência do SDK. | **Implementada sem validação / parcial** | P2 nesta entrega; dados clínicos exigem autorização excepcional rastreável. |
| Layout/menu | `_AppSidebar.cshtml` filtra destinos por módulo/permissão; layout apresenta contexto e guard Web bloqueia rota. Login já tem labels, senha acessível e estados. | **Implementada sem validação visual** | P1: Playwright desktop/mobile bloqueado sem aplicação executável. |

## Defeito comprovado e correção

**Defeito P0:** `GET /api/permissoes/usuario/{usuarioId}` e os dois endpoints `testar-acesso` aceitavam IDs de usuário e tenant fornecidos pelo cliente e chamavam diretamente o cálculo de permissões. Um administrador local podia consultar o catálogo efetivo de um usuário fora de seu tenant; em `api/seguranca/testar-acesso`, o tenant malicioso ainda era encaminhado ao cálculo depois de uma validação feita com o tenant local.

**Correção:** `SecurityAdministrationService.UsuarioPertenceAoEscopoAsync` agora centraliza a fronteira: administradores locais sempre usam o claim do tenant; globais podem informar contexto; o alvo precisa ter vínculo principal ou `usuario_tenant_acessos` ativo, iniciado e não expirado. `TestarPermissaoNoEscopoAsync` mantém validação e cálculo com o mesmo tenant efetivo. Ambos os controllers retornam 404 genérico antes de materializar permissões de um alvo fora do escopo.

**Múltiplos vínculos:** `EffectivePermissionService.TestarAsync` deixou de rejeitar automaticamente qualquer tenant diferente do cadastro principal. Ele aceita o tenant selecionado apenas quando há vínculo secundário ativo/vigente, valida `tenants` e o cliente associado, e continua negando ID arbitrário com `CROSS_TENANT_DENIED`.

## Suspeitas separadas de defeitos

- **Comprovado:** endpoints placeholder em `SegurancaController` retornam sucesso sem persistência para perfil, sessão e auditoria.
- **Comprovado:** seleção de contexto persiste no banco, mas não emite nova identidade; o cookie continua com claims anteriores.
- **Suspeita a validar:** coexistência de `tenant_id` e `cliente_id` pode causar joins incorretos em serviços legados. Não foi declarada falha sem fixture PostgreSQL.
- **Suspeita a validar:** DTOs dinâmicos/tuplas Dapper podem variar conforme aliases e tipos do PostgreSQL. Os DTOs críticos novos usam propriedades públicas, mas falta materialização real nesta rodada.
- **Suspeita a validar:** rotas comerciais duplicadas (`api/planos`, `api/public`) podem conflitar por verbo/template; executar o verificador e inicialização da API assim que houver SDK.

## Dependências e próximos blocos

1. **P0 agora:** instalar SDK .NET 10; restore/build Debug+Release; testes unitários e PostgreSQL; E2E de login e administração com dois tenants; renovar identidade ao trocar contexto; substituir placeholders da Central de Segurança.
2. **P1 operação e financeiro:** consolidar escopo de plantões/escalas e depois fechamento/repasses, sem misturar com esta entrega.
3. **P2 clínico em ordem:** pacientes/agenda/check-in/painel → triagem/consulta → prescrição → financeiro clínico/convênios. Exigir autorização clínica específica, justificativa e auditoria durável.

## Evidências de validação desta rodada

- `dotnet --info`: falhou com `dotnet: command not found`; nenhum resultado de build/teste foi inventado.
- `dotnet restore backend/PlantaoPro.sln`, builds Debug/Release e `dotnet test backend/PlantaoPro.Tests/PlantaoPro.Tests.csproj -c Debug --no-build`: falharam com exit code 127 pela mesma ausência do executável.
- `psql` e `docker`: indisponíveis; a integração Dapper/PostgreSQL e a concorrência não foram executadas.
- Verificações locais aprovadas: compatibilidade C# 10/Razor, segurança do repositório, unicidade de controllers, contrato de rotas v1.52, 11 testes Python e `git diff --check`.
- Testes adicionados: 3 regressões de contrato em `V2155TenantPermissionBoundaryTests` para vínculo vigente, escopo antes do cálculo e rejeição do tenant enviado por administrador local. Quantidade executada: **0**, devido à ausência do SDK.
- Migrations: nenhuma executada e nenhuma alteração de framework/segurança foi feita para contornar o ambiente.
