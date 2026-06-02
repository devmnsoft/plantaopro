# Checklist pós-deploy

## Saúde técnica
- [ ] API responde `/api/health` com sucesso.
- [ ] Web abre tela de login.
- [ ] Swagger disponível apenas conforme política do ambiente.
- [ ] Connection string aponta para banco correto.
- [ ] Migrações SQL incrementais aplicadas no schema `plantaopro`.
- [ ] Variáveis de JWT, cookies e segredos configuradas fora do repositório.
- [ ] Logs gravando sem senha, hash, token ou payload sensível.

## Smoke test por perfil
- [ ] ADMINISTRADOR_GLOBAL abre dashboard, auditoria e observabilidade.
- [ ] ADMINISTRADOR vê apenas cliente próprio.
- [ ] COORDENACAO publica plantão e confirma escala.
- [ ] FINANCEIRO gera e confirma pagamento.
- [ ] MEDICO acessa Minha Agenda e só vê dados próprios.
- [ ] HOSPITAL acessa apenas dados da unidade.

## API Mobile
- [ ] Login mobile gera JWT.
- [ ] Endpoint sem token retorna `401`.
- [ ] Plano sem permissão mobile retorna `403` amigável.
- [ ] Listagens respondem paginadas.

## Operação e produto
- [ ] Toasts aparecem após ações relevantes.
- [ ] Modais aparecem em ações críticas.
- [ ] Relatórios exportam CSV e auditam.
- [ ] Observabilidade registra erro sintético controlado sem stack trace para usuário.
- [ ] Responsividade conferida em viewport mobile.

## Aprovação
- [ ] Responsável técnico aprovou.
- [ ] Responsável de produto aprovou.
- [ ] Pendências registradas com prioridade e dono.
