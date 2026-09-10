# PlantãoPro v2.15.6 — sessões, contexto e Central de Segurança

Base auditada: `04fd9b2`. A v2.15.5 já continha a fronteira de leitura de permissões por tenant; ela foi preservada e reutilizada. A v2.15.4 permanece documental e não foi tratada como evidência de entrega clínica.

## Matriz de fechamento

| Funcionalidade | Código existente | Defeito confirmado | Correção v2.15.6 | Teste/evidência | Estado final |
|---|---|---|---|---|---|
| Validação de sessão | `AuthenticationSessionService` consultava sessão, usuário e cliente | Contexto do JWT não era comparado ao contexto persistido; cliente suspenso era confundido com usuário bloqueado | Comparação exata de tenant/cliente e regra explícita para `SUSPENSO`; revogação continua negando a próxima chamada | `V2156SessionSecurityTests` | Fechado no servidor |
| Usuário / perfis | Persistência de usuário e fronteira v2.15.5 | Perfis de detalhe/criação/edição/cópia e matriz devolviam sucesso fixo | DTOs tipados, validação de escopo, transações reais, bloqueio de perfil global/base e revogação das sessões afetadas | Compatibilidade C# 10 + revisão SQL | Fechado na API |
| Troca obrigatória de senha | Claim e coluna já existiam | Endpoint não persistia | Atualiza `senha_alteracao_obrigatoria`, revoga sessões e audita | Teste de revogação do estado de sessão | Fechado na API |
| Sessões | Tabela e validação canônicas já existiam | Lista vazia e revogação simulada | Consulta paginada sem token, identificação da sessão atual e revogação persistente/auditada | Teste de sessão revogada | Fechado na API |
| Tentativas e auditoria | Registros já eram gravados no login e no serviço central | Endpoints sempre retornavam listas vazias | Leituras reais, paginadas e limitadas pelo tenant | Revisão de consultas e geração SQL | Fechado na API |
| SQL consolidado | Gerador oficial e manifesto existentes | Artefatos versionados não correspondiam aos bytes LF das fontes, quebrando o gate determinístico | Regeneração exclusivamente por `generate-scrpt-completo.py`, incluindo checksums derivados | Duas gerações consecutivas + `git diff` | Fechado |

## Validação e limitações desta execução

- O container do agente não possui o SDK .NET (`dotnet: command not found`). Isso não indica indisponibilidade dos runners: o workflow continua configurado para instalar .NET 10 e compilar com C# 10.
- O run solicitado não pôde ser baixado: o `gh` presente não está autenticado e a página do repositório respondeu 401. Os artefatos versionados não contêm falhas TRX enumeráveis; portanto nenhuma causa foi inventada.
- E2E navegada e screenshots da aplicação real não foram produzidos, pois não foi possível compilar/iniciar a aplicação neste container. Não há declaração de homologação.

## Backlog ordenado

1. **Operação/financeiro:** executar CI com PostgreSQL descartável, builds Debug/Release, TRX e replay triplo; publicar evidência nova.
2. **Jornada clínica existente:** executar E2E real de duas abas e formulários antigos em todos os comandos clínicos, além de indisponibilidade da API e timeout.
3. **Expansão:** somente após os gates anteriores, evoluir novas jornadas e módulos; esta rodada não cria gateway, preço ou arquitetura paralela de permissões.
