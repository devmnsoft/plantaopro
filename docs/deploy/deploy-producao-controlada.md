# Deploy em Produção Controlada

## Estratégia
Produção controlada deve atender poucos clientes selecionados, com operação assistida, métricas acompanhadas diariamente e rollback documentado.

## Configuração mínima
- ASP.NET Core API e Web em processos separados.
- PostgreSQL com schema `plantaopro` e backup automatizado.
- HTTPS obrigatório.
- Segredos em variáveis/secret manager, nunca em `appsettings.json` versionado.
- Logs estruturados com retenção e correlação por request.

## Passos
1. Preparar servidor e variáveis de ambiente.
2. Publicar API e Web a partir da mesma versão/tag.
3. Aplicar scripts SQL incrementais em ordem cronológica.
4. Executar smoke test de API, Web e Mobile MVP.
5. Ativar cliente piloto em plano beta.
6. Monitorar observabilidade: erros, endpoints lentos, acessos negados, faturas vencidas e chamados críticos.

## Validação pós-deploy
- Login Web e JWT API.
- `/api/health` e Swagger.
- Fluxo operacional médico ponta a ponta.
- Fluxo SaaS básico.
- API Mobile com token válido, 401 sem token e 403 para plano sem mobile.
- Auditoria de ações críticas e exportações.

## Operação assistida
- Reunião diária rápida com cliente nos primeiros 5 dias úteis.
- Revisão de chamados críticos em até 2 horas úteis.
- Congelar mudanças de alto risco durante homologação real.
