# Checklist de Homologação — Release Candidate PlantãoPro

Data-base: 2026-06-09.

## Gate técnico

- [x] Branch de RC criada.
- [x] Varredura legado não PlantãoPro executada sem ocorrências.
- [x] Varredura de links/JS nativo proibido executada sem ocorrências críticas.
- [x] Menus críticos revisados.
- [x] Guard de rotas revisado para módulos de assinatura e treinamento.
- [ ] Build API executado em ambiente com SDK .NET.
- [ ] Build Web executado em ambiente com SDK .NET.
- [ ] `dotnet test` executado em ambiente com SDK .NET.

## Gate funcional por perfil

- [ ] ADMINISTRADOR_GLOBAL acessa Admin SaaS, planos, assinaturas, billing, white label, auditoria e observabilidade.
- [ ] ADMINISTRADOR_CLIENTE acessa apenas tenant, portal cliente, usuários, perfis, assinatura, uso e faturas.
- [ ] COORDENADOR acessa central de escala, plantões, convites e escalas, sem billing global.
- [ ] MEDICO acessa área própria, convites, agenda e pagamentos próprios.
- [ ] FINANCEIRO acessa pagamentos e faturas do tenant, sem Admin SaaS e sem white label.
- [ ] PARCEIRO acessa portal parceiro e tenants vinculados, sem dados clínicos sensíveis.
- [ ] AUDITOR acessa auditoria/relatórios em modo leitura.

## Gate SaaS

- [ ] White label exibe bloqueio por plano quando indisponível.
- [ ] White label aplica fallback de logo.
- [ ] Billing mostra plano atual, assinatura, uso, faturas e inadimplência.
- [ ] Bloqueios exibem plano atual, plano necessário, limite, uso e CTA.
- [ ] Marketplace não aparece indevidamente para tenant sem liberação.

## Gate operacional

- [ ] Criar hospital.
- [ ] Criar especialidade.
- [ ] Criar médico.
- [ ] Criar plantão.
- [ ] Publicar plantão.
- [ ] Enviar convite.
- [ ] Médico aceitar/recusar convite.
- [ ] Confirmar escala.
- [ ] Marcar plantão realizado.
- [ ] Gerar e confirmar pagamento.
- [ ] Exportar relatório.

## Gate segurança/LGPD

- [ ] POSTs Web possuem antiforgery.
- [ ] Acesso cruzado tenant bloqueado.
- [ ] Acesso negado mostra mensagem amigável.
- [ ] Logs não expõem senha/token/API key.
- [ ] Auditoria registra ações críticas.
- [ ] Solicitações LGPD são rastreáveis.
