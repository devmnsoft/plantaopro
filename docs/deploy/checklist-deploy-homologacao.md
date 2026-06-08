# Checklist de deploy em homologação — PlantãoPro

Use este roteiro para publicar uma versão homologável e reprodutível do PlantãoPro sem expor segredos no repositório.

## 1. Pré-requisitos

- [ ] Runtime/SDK .NET compatível com os projetos `net10.0` disponível no agente de build.
- [ ] PostgreSQL provisionado com banco dedicado de homologação.
- [ ] Schema `plantaopro` criado.
- [ ] Usuário de banco com menor privilégio necessário para migrations e execução.
- [ ] Domínios/URLs de API e Web definidos.
- [ ] Certificados TLS válidos configurados no proxy ou plataforma de hospedagem.

## 2. Configuração segura

- [ ] `ConnectionStrings:Default` configurada por secret manager, variável de ambiente ou arquivo não versionado.
- [ ] `Jwt:Key` possui pelo menos 32 caracteres e não está commitada.
- [ ] `Jwt:Issuer` e `Jwt:Audience` conferem com API/Web/mobile.
- [ ] `ApiSettings:BaseUrl` ou `PlantaoProApi:BaseUrl` aponta para a API de homologação.
- [ ] Logs não registram senha, hash, token, segredo ou payload sensível.
- [ ] Arquivos `appsettings.*.example.json` permanecem apenas com placeholders.

## 3. Banco de dados

- [ ] Aplicar scripts incrementais de `backend/sql` na ordem cronológica.
- [ ] Aplicar migrations de `database/migrations` aplicáveis à rodada SaaS/LGPD/Jornada.
- [ ] Validar que constraints usam blocos `DO $$` quando necessário.
- [ ] Validar que não existe `ADD CONSTRAINT IF NOT EXISTS`.
- [ ] Validar índices críticos de clientes, assinaturas, faturas, auditoria, request logs, escalas e pagamentos.
- [ ] Executar seed apenas se o ambiente aceitar dados demonstrativos.

## 4. Publicação

- [ ] Executar `dotnet clean backend/PlantaoPro.Api/PlantaoPro.Api.csproj`.
- [ ] Executar `dotnet build backend/PlantaoPro.Api/PlantaoPro.Api.csproj`.
- [ ] Executar `dotnet clean backend/PlantaoPro.Web/PlantaoPro.Web.csproj`.
- [ ] Executar `dotnet build backend/PlantaoPro.Web/PlantaoPro.Web.csproj`.
- [ ] Executar `dotnet test` quando o SDK estiver disponível.
- [ ] Publicar API e Web em artefatos separados.
- [ ] Habilitar health/smoke checks para API, Web, Swagger autorizado e banco.

## 5. Smoke test pós-deploy

- [ ] Login admin funciona.
- [ ] Dashboard Web abre.
- [ ] Swagger/API responde.
- [ ] `/api/dashboard`, `/api/saas-dashboard/resumo` e `/api/mobile/home` respondem com autenticação válida.
- [ ] CRUD de clientes e planos abre.
- [ ] Geração de fatura SaaS não duplica competência.
- [ ] Bloqueio por limite de plano retorna mensagem amigável.
- [ ] Solicitação LGPD registra evento auditável.
- [ ] Observabilidade lista erros, acessos e performance com limite máximo aplicado.

## 6. Critérios para liberar homologação aos usuários

- [ ] Build e testes sem falhas de código.
- [ ] Pendências ambientais documentadas com responsável.
- [ ] Checklist de homologação final anexado à entrega.
- [ ] Plano de rollback definido.
- [ ] Backup de banco validado antes de testes destrutivos.
- [ ] Monitoramento de logs e auditoria acompanhado durante a janela inicial.
