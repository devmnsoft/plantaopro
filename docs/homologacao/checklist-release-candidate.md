# Checklist Release Candidate Operacional — PlantãoPro

## Objetivo
Consolidar uma validação objetiva para homologação real, demonstração comercial e piloto controlado do PlantãoPro.

## Critérios técnicos obrigatórios
- [ ] API sobe sem erro e mantém Swagger disponível em ambiente de desenvolvimento.
- [ ] `/api/health` responde com status operacional.
- [ ] Web sobe sem quebrar login por cookie.
- [ ] Login API mantém JWT válido e sem expor senha, hash, token em logs ou stack trace ao usuário.
- [ ] Build de API, Web e testes concluído sem erros.
- [ ] Views MVC não usam `@page`, `asp-page`, `@model dynamic`, `href="#"`, `alert()` ou `confirm()`.
- [ ] Nenhum arquivo `bin/`, `obj/`, `.vs/`, `.dll`, `.exe`, `.pdb`, `.zip`, `.apk` ou `.aab` está versionado.

## Fluxo ponta a ponta obrigatório
| Etapa | Perfil | Resultado esperado | Status |
|---|---|---|---|
| Criar hospital | ADMINISTRADOR_GLOBAL/COORDENACAO | Hospital ativo aparece em lookup e detalhes | [ ] |
| Criar especialidade | ADMINISTRADOR_GLOBAL/COORDENACAO | Especialidade ativa aparece em lookup | [ ] |
| Criar médico | ADMINISTRADOR_GLOBAL/COORDENACAO | Médico ativo, com especialidade compatível e usuário vinculado quando necessário | [ ] |
| Criar plantão | COORDENACAO | Plantão nasce `rascunho`; vagas disponíveis = vagas totais | [ ] |
| Publicar plantão | COORDENACAO | Plantão `aberto`, com auditoria e toast | [ ] |
| Solicitar plantão | MEDICO | Escala `solicitado`; bloqueia duplicidade/conflito | [ ] |
| Confirmar escala | COORDENACAO | Escala `confirmado`; vaga reduzida sem ficar negativa | [ ] |
| Realizar escala | COORDENACAO | Escala `realizado`; pagamento fica elegível | [ ] |
| Gerar pagamento | FINANCEIRO | Pagamento único para escala realizada | [ ] |
| Confirmar pagamento | FINANCEIRO | Valor/data/forma preenchidos, auditoria e notificação | [ ] |
| Consultar pagamento | MEDICO | Médico vê apenas seus pagamentos | [ ] |
| Conferir painéis | COORDENACAO/FINANCEIRO | Dashboard, Central de Escala e relatório financeiro refletem dados | [ ] |

## API Mobile MVP
- [ ] `POST /api/mobile/auth/login` funciona sem JWT e retorna contrato leve.
- [ ] Endpoints protegidos retornam `401` sem token.
- [ ] Endpoints mobile protegidos validam plano/permissão e retornam `403` amigável quando bloqueados.
- [ ] `GET /api/mobile/convites` lista apenas convites do médico autenticado, com paginação.
- [ ] `POST /api/mobile/convites/{id}/aceitar` revalida vaga, conflito, duplicidade e elegibilidade pelo serviço de escala.
- [ ] `POST /api/mobile/convites/{id}/recusar` exige motivo, atualiza status do convite e registra auditoria sem logar o texto sensível.
- [ ] `GET /api/mobile/meus-pagamentos` não retorna dados de outro médico.
- [ ] `GET /api/mobile/suporte/chamados/{id}` retorna apenas chamado próprio, com timeline leve e sem dados sensíveis.

## Segurança e multiempresa
- [ ] ADMINISTRADOR_GLOBAL visualiza todos os clientes.
- [ ] ADMINISTRADOR/COORDENACAO/OPERADOR/FINANCEIRO visualizam apenas dados do próprio cliente.
- [ ] MEDICO visualiza apenas dados próprios.
- [ ] HOSPITAL visualiza apenas dados da própria unidade quando aplicável.
- [ ] Acesso negado gera resposta amigável e auditoria.
- [ ] Central de Escala isolada por cliente para ADMINISTRADOR/COORDENACAO/FINANCEIRO, mantendo visão global apenas para ADMINISTRADOR_GLOBAL.

## Auditoria, observabilidade e relatórios
- [ ] Login sucesso/falha, publicação, escala, pagamento, convite e exportações registram auditoria.
- [ ] Observabilidade exibe erros recentes, acessos negados e endpoints lentos sem stack trace completa para usuário.
- [ ] Relatórios essenciais possuem filtros, resumo, tabela, CSV e isolamento por cliente.

## Pendências conhecidas para homologação
- Registrar evidências manuais do fluxo ponta a ponta com prints ou vídeo curto.
- Validar massa de dados realista em ambiente de homologação antes de piloto.
- Confirmar credenciais e URLs finais no checklist pós-deploy.

## Atualização desta rodada RC

- [x] Varredura obrigatória executada sem ocorrências para `@page`, `asp-page`, `@model dynamic`, `href="#"`, `alert()`, `confirm()` e collection expression em API/Web.
- [x] Verificação de binários versionados executada sem arquivos indevidos rastreados.
- [x] API Mobile MVP documentada com suporte a chamados (`GET/POST /api/mobile/suporte/chamados`).
- [x] Script SQL incremental criado para suporte mobile e mensagens de chamados.
- [ ] Build local não executado nesta estação porque o SDK `dotnet` não está instalado no ambiente do agente; executar no runner/ambiente de homologação antes do aceite final.

## Smoke test manual recomendado antes da demonstração

1. Autenticar no Swagger como médico demo.
2. Validar `/api/mobile/me`, `/dashboard`, `/plantoes-disponiveis`, `/convites`, `/minhas-escalas`, `/meus-pagamentos` e `/notificacoes`.
3. Criar chamado em `/api/mobile/suporte/chamados` e confirmar protocolo.
4. Listar chamados mobile e confirmar isolamento por usuário/cliente.
5. Repetir endpoints protegidos sem token e confirmar resposta 401 amigável.
6. Repetir detalhe de plantão de outro cliente e confirmar resposta 403 amigável com auditoria.
