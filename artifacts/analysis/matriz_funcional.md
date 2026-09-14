# Matriz Funcional - Auditoria v2.15.0

## Resumo do Ambiente
- **Backend:** .NET 10, C# 10
- **Banco de Dados:** PostgreSQL com Dapper
- **Frontend:** MVC Views, jQuery/JS nativo
- **Testes:** Unitários presentes, sem testes end-to-end Playwright disponíveis no momento (`npx playwright test` = No tests found).

## Matriz por Funcionalidade

| Funcionalidade | Status | Arquivos / Componentes | Observações / Prioridades |
|---|---|---|---|
| **Login e Autenticação** | Implementado sem validação completa | `AuthController.cs`, `AccountController.cs`, `Data.cs`, `auth-login.js` | Funcionalidade desenvolvida, porém erros de API Timeout / CORS ou Base URL ausentes impedem uso real. Requer ajuste de loading button no Web form. (P0) |
| **Sessão / Token** | Parcial | `AuthenticationSessionServices.cs` | Sessões sendo registradas, mecanismo JWT seguro; a revalidação web precisa comprovar expiração do cookie vs sessão. (P0) |
| **Clientes (Tenants)** | Implementado sem validação | `ClientesController.cs` | Listagem e gerenciamento básico. Dapper e API estão filtrando por Tenant corretamente, porém validações de UI requerem revisão. (P1) |
| **Usuários** | Implementado sem validação | `UsuariosController.cs` | Vinculação com ClienteId / TenantId existente. Necessário separar a gestão de perfis pelo administrador do cliente. (P1) |
| **Perfis e Permissões** | Parcial | `PermissoesController.cs`, `AccessServices.cs` | Roles mapeadas (`roleCatalog`), claims configuradas, mas UI e controle restrito de edição de super admin precisam validação. (P0) |
| **Módulos / Assinaturas** | Parcial | `SaasDashboardController.cs`, `AssinaturasController.cs` | Controle visual de módulos "disponíveis/indisponíveis" depende do `SaasRouteGuardFilter`. (P1) |
| **Plantões** | Implementado sem validação | `PlantoesController.cs` | Módulo base do SaaS. Precisa validar o filtro por tenant nas buscas Dapper. (P2) |
| **Escalas** | Implementado sem validação | `EscalasController.cs` | (P2) |
| **Financeiro** | Implementado sem validação | `FinanceiroController.cs` | (P2) |
| **Saúde 360** | Implementado sem validação | `Saude360ClinicalControllers.cs` | (P2) |

## Defeitos e Suspeitas Comprovadas
1. **Defeito P0:** Falha na tentativa de login Web vs API (Timeout/BadRequest). A causa-raiz provável é a configuração da BaseUrl do HttpClient e o tratamento da submissão do formulário (`auth-login.js` timer).
2. **Suspeita P1:** Filtragem do Tenant nas consultas legadas. Foram encontrados indícios de correções (ver commits recentes e `coalesce(u.tenant_id, u.cliente_id)`), mas é necessário testar com acesso de cliente vs super admin.
3. **Suspeita P1:** Autenticação expõe JWT para a sessão (`HttpContext.Session.SetString("jwt", login.Token)`), mas não para o JS. Isso é um risco mediano e está contido no lado servidor (preservado de forma segura no Session/Cookie).

## Plano de Correção e Execução
1. Atualizar o `AccountController.cs` e `Program.cs` para validar `PlantaoProApi` e BaseUrls.
2. Refinar a visualização de `Login.cshtml` para desativar botão corretamente, mantendo submit natural.
3. Consolidar `SaaS Authorization` removendo dependências perigosas de troca de tenant para usuários sem permissão.
4. Testes com tenants reais na API.
