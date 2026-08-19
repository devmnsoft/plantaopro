# Matriz de módulos e regras

| Módulo | Regra | Permissão | Plano | Entidade/tabela | API | BFF/Web | Governança/auditoria | Estado |
|---|---|---|---|---|---|---|---|---|
| Autenticação | JWT forte; usuário ativo; não registrar senha/token | autenticado | base | `usuarios`, `perfis`, `usuarios_perfis`, `login_tentativas` | controllers/Auth existentes | Login existente | `IAuditService` | configuração Development corrigida; smoke pendente |
| System Health | resposta sanitizada; configuração ausente explícita | administradores na Web | base | consulta `select 1` | `api/health`, `api/health/db`, `api/health/auth` | `/SystemHealth` → `Configuracoes/Saude` | logs sanitizados | rota e parsing corrigidos; banco pendente |
| Organização/tenant | isolamento obrigatório | políticas Tenant | SaaS | tabelas `tenant_id`/`cliente_id` existentes | controllers existentes | telas existentes | serviços de auditoria | inventário detalhado pendente |
| Usuários/perfis | admin tenant não eleva acesso global | administração | SaaS | identidade existente | controllers existentes | configurações/segurança | auditoria | smoke pendente |
| Plano/entitlement | bloqueio não retorna 500 | guard services | conforme assinatura | assinatura/uso existentes | APIs SaaS existentes | páginas SaaS existentes | usage/auditoria | não validado |
| Operação clínica | respeitar vínculo e dados sensíveis | políticas clínicas | conforme contrato | schema `plantaopro` | APIs clínicas existentes | telas clínicas existentes | auditoria LGPD | fora da P0 atual |
| Notificações | tenant, destino real, mensagem e leitura | autenticado | base | fonte canônica SQL ausente | APIs/realtime existentes | topbar existente | evento operacional | não validado |
| Database tooling | sem DROP/apagar dados; migrations rastreadas | operador | n/a | `schema_migrations` | n/a | CLI Database | execução registrada | install bloqueado por script ausente |
