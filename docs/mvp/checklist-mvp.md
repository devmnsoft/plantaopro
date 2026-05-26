# Checklist MVP Comercial — PlantãoPro

Data de referência: **26/05/2026**

## 1) Estabilidade técnica
- [ ] Build API verde em homologação.
- [ ] Build Web verde em homologação.
- [ ] Sem arquivos binários/versionados indevidos.
- [ ] JWT e login validados por perfil.
- [ ] Logs estruturados ativos.

## 2) Fluxo comercial
- [ ] Onboarding comercial completo (cliente → plano → assinatura → unidade → usuários).
- [ ] CNPJ duplicado bloqueado com mensagem amigável.
- [ ] E-mail admin duplicado bloqueado com mensagem amigável.
- [ ] Onboarding com retomada (progresso salvo).
- [ ] Conclusão com próximos passos.

## 3) SaaS (plano, assinatura, limite)
- [ ] Limites aplicados (médicos, hospitais, plantões/mês).
- [ ] Bloqueios com auditoria + toast.
- [ ] Status de assinatura respeitado (TESTE/ATIVA/SUSPENSA/VENCIDA/CANCELADA).
- [ ] Dashboard de consumo por cliente.

## 4) Faturamento SaaS
- [ ] Geração mensal sem duplicidade por competência.
- [ ] Marcação de pagamento com data/valor/forma.
- [ ] Cancelamento com justificativa.
- [ ] Alertas para faturas vencidas.

## 5) Customer Success
- [ ] Painel de saúde por cliente.
- [ ] Clientes em risco destacados.
- [ ] Registro de interação CS.
- [ ] Alertas de mudança de saúde (RISCO/CRÍTICO).

## 6) Operação
- [ ] Área do médico mobile-first validada.
- [ ] API mobile mínima validada no Swagger.
- [ ] Suporte (abertura, mensagens, resolução, cancelamento).
- [ ] Relatórios essenciais com filtros e exportação.

## 7) Segurança e governança
- [ ] Revisão de permissões por perfil.
- [ ] Auditoria em ações críticas.
- [ ] Acesso negado amigável.
- [ ] Sem exposição de dados sensíveis.

## 8) Venda e implantação
- [ ] Tela de demo comercial pronta.
- [ ] Manual de ajuda interno publicado.
- [ ] Documentação de deploy/homologação atualizada.
- [ ] Roteiro de apresentação comercial aprovado.

