# Checklist do fluxo de produção

## P0 obrigatório
- [ ] `dotnet clean` (SDK ausente neste ambiente)
- [ ] `dotnet restore` (SDK ausente neste ambiente)
- [ ] `dotnet build` (SDK ausente neste ambiente)
- [ ] API inicia com PostgreSQL/schema reais
- [ ] Web inicia e alcança a API
- [x] Development possui chave JWT não produtiva com mais de 32 caracteres
- [x] produção permanece sem chave demo versionada e falha sem configuração externa
- [x] `/SystemHealth` possui destino real e compatibilidade com o contrato atual de health
- [ ] script canônico de instalação recuperado e validado duas vezes no mesmo banco
- [ ] bootstrap administrativo idempotente
- [ ] login válido, inválido, bloqueado, logout e auditoria

## Fluxo funcional subsequente
- [ ] Dashboard, organização/tenant, usuários, perfis, permissões e planos
- [ ] Fluxo operacional principal existente do PlantaoPro
- [ ] LGPD e acesso a dados sensíveis
- [ ] notificações e destinos reais
- [ ] governança e auditoria sem secrets
- [ ] relatórios/exportações somente quando reais
- [ ] integrações somente quando configuradas
- [ ] health, logs, backup/restore, publish e rollback
- [ ] rotas e BFFs inventariados via runtime sem 404

## Testes
Testes automatizados novos permanecem por último. Nenhuma classe/projeto de teste foi criado nesta execução.
