# Checklist de pré-homologação — PlantãoPro

Use este checklist antes de liberar ambiente para cliente piloto ou demonstração executiva.

## 1. Técnico e estabilidade
- [ ] `git status` sem alterações inesperadas.
- [ ] Sem binários versionados (`dll`, `exe`, `pdb`, `zip`, `apk`, `aab`, bancos locais, `bin/`, `obj/`, `.vs/`).
- [ ] API compila sem erro.
- [ ] Web compila sem erro.
- [ ] Swagger abre e lista a tag `Mobile`.
- [ ] `/api/health` retorna sucesso.
- [ ] Login Web mantém Cookie Authentication.
- [ ] Login API/Mobile mantém JWT.
- [ ] Views MVC sem `@page`, `asp-page`, `@model dynamic`, `href="#"`, `alert()` ou `confirm()` nativo.

## 2. UX/UI e feedback
- [ ] Menus respeitam perfil logado.
- [ ] Telas principais têm título, subtítulo e ação principal clara.
- [ ] Listagens têm filtros, total, paginação/limite e EmptyState.
- [ ] Formulários têm validação, textos de ajuda, botão voltar/cancelar e loading no submit.
- [ ] Ações críticas usam modal global (`data-confirm="true"`).
- [ ] Feedback usa TempData padrão: `Success`, `Error`, `Warning`, `Info`.
- [ ] Sem stack trace ou SQL bruto para usuário final.
- [ ] Layout mobile verificado em Dashboard, Plantões, Escalas, Financeiro e Minha Agenda.

## 3. Segurança e isolamento
- [ ] `ADMINISTRADOR_GLOBAL` consegue visão ampla e observabilidade.
- [ ] `ADMINISTRADOR` vê apenas dados do seu cliente.
- [ ] `COORDENACAO` gerencia operação do cliente.
- [ ] `OPERADOR` executa ações operacionais permitidas.
- [ ] `FINANCEIRO` vê e altera apenas financeiro permitido.
- [ ] `MEDICO` vê apenas agenda, escalas, pagamentos e notificações próprias.
- [ ] `HOSPITAL` vê apenas dados da unidade/hospital associado.
- [ ] Acesso negado exibe tela amigável e registra auditoria.
- [ ] Logs não contêm senha, hash, token, segredo ou payload sensível.

## 4. Operação ponta a ponta
- [ ] Hospital criado.
- [ ] Especialidade criada.
- [ ] Médico criado e vinculado a usuário.
- [ ] Plantão criado como rascunho.
- [ ] Plantão publicado.
- [ ] Médico solicita plantão ou aceita convite.
- [ ] Coordenação confirma escala e vagas não ficam negativas.
- [ ] Escala marcada como realizada.
- [ ] Financeiro gera pagamento sem duplicidade.
- [ ] Financeiro confirma pagamento.
- [ ] Médico visualiza pagamento confirmado e notificação.
- [ ] Comunicação abre conversa relacionada.
- [ ] Agenda e Central de Escala refletem status atualizado.

## 5. Relatórios, auditoria e observabilidade
- [ ] Relatórios essenciais abrem com filtros.
- [ ] Exportação CSV respeita `cliente_id` e registra auditoria.
- [ ] Auditoria mostra ações críticas sem payload sensível.
- [ ] Observabilidade mostra erros do dia, endpoints lentos, últimos logins e acessos negados apenas para admin global.

## 6. API Mobile MVP
- [ ] `POST /api/mobile/auth/login` retorna JWT válido.
- [ ] Endpoint mobile sem token retorna `401`.
- [ ] Médico não acessa plantão de outro cliente.
- [ ] Solicitação mobile reutiliza regras de escala para bloquear duplicidade, conflito, médico inativo, especialidade incompatível e plantão sem vaga.
- [ ] Listagens mobile usam paginação e payload leve.
- [ ] Plano sem mobile retorna `403` amigável.
- [ ] Documentação em `docs/mobile/mobile-api-endpoints.md` revisada.

## Critérios de aprovação
- Build verde em ambiente com SDK .NET instalado.
- Fluxo operacional aprovado sem intervenção técnica.
- Nenhuma falha crítica de segurança ou isolamento multiempresa.
- Pendências conhecidas registradas com severidade e responsável.
