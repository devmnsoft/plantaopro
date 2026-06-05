# Checklist de Go-Live Beta Controlado

## Janela e responsáveis
- Responsável técnico: definir engenheiro de plantão.
- Responsável produto/CS: definir ponto focal com cliente piloto.
- Janela recomendada: baixa operação, com rollback em até 30 minutos.

## Antes do deploy
- [ ] Backup do banco PostgreSQL validado.
- [ ] Variáveis de ambiente revisadas sem segredos em repositório.
- [ ] Scripts SQL incrementais revisados e aplicados em homologação.
- [ ] `dotnet build` API/Web verde no agente de CI ou servidor.
- [ ] Swagger e `/api/health` validados.
- [ ] Usuários de teste e cliente piloto revisados.
- [ ] SMTP/push/webhooks desativados ou apontando para sandbox quando aplicável.

## Durante o deploy
- [ ] Publicar API.
- [ ] Publicar Web.
- [ ] Aplicar migrações SQL incrementais no schema `plantaopro`.
- [ ] Validar conexão com PostgreSQL.
- [ ] Validar login Web e JWT API.
- [ ] Validar assets estáticos, CSS, JS, toast e modal global.

## Smoke test obrigatório
- [ ] `/api/health` saudável.
- [ ] Swagger carregando e endpoint protegido retorna 401 sem token.
- [ ] Login admin global e dashboard executivo.
- [ ] Criar/publicar plantão de teste.
- [ ] Login médico e visualizar plantões disponíveis.
- [ ] Solicitar plantão, confirmar escala, marcar realizada e gerar pagamento.
- [ ] Gerar fatura SaaS e marcar como paga.
- [ ] Criar/resolver chamado de suporte.
- [ ] Baixar relatório CSV e validar auditoria.

## Rollback
- Reverter pacote API/Web anterior.
- Restaurar backup apenas se script SQL tiver causado inconsistência irreversível.
- Registrar ocorrência em observabilidade e Customer Success.

## Aprovação de go-live
- Build verde, smoke test concluído, cliente piloto comunicado e monitoramento ativo por 48 horas.

## Validação final da operação assistida

- Confirmar execução do SQL incremental `backend/sql/20260603_operacao_assistida_beta.sql` no schema `plantaopro`.
- Abrir `/OperacaoAssistida` com ADMINISTRADOR_GLOBAL.
- Validar isolamento acessando o mesmo módulo com ADMINISTRADOR do cliente.
- Conferir que ocorrências críticas aparecem em alertas operacionais e auditoria.

## Validação final da operação assistida

- Confirmar execução do SQL incremental `backend/sql/20260603_operacao_assistida_beta.sql` no schema `plantaopro`.
- Abrir `/OperacaoAssistida` com ADMINISTRADOR_GLOBAL.
- Validar isolamento acessando o mesmo módulo com ADMINISTRADOR do cliente.
- Conferir que ocorrências críticas aparecem em alertas operacionais e auditoria.
