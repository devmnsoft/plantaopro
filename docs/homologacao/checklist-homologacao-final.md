# Checklist de homologação final — PlantãoPro

Este checklist consolida a rodada final de aceite técnico, funcional, segurança e operação assistida do PlantãoPro. Marque cada item somente após validação real no ambiente de homologação com API, Web e banco PostgreSQL configurados.

## 1. Build, ambiente e configuração

- [ ] API inicia com `dotnet run --project backend/PlantaoPro.Api`.
- [ ] Web inicia com `dotnet run --project backend/PlantaoPro.Web`.
- [ ] Swagger abre em ambiente de desenvolvimento/homologação autorizado.
- [ ] `Jwt:Key`, `Issuer`, `Audience` e `ConnectionStrings:Default` estão configurados por variável segura ou arquivo não versionado.
- [ ] Não há referência ativa a produtos, domínios ou termos de outro projeto no código, nas views, nos scripts, nas configurações, no SQL ou na documentação.
- [ ] Migrations SQL foram aplicadas no schema `plantaopro`.

## 2. Login, perfis e multiempresa

- [ ] Login admin `admin@plantaopro.com` funciona no Web.
- [ ] Logout encerra cookie e sessão JWT.
- [ ] JWT é aceito pela API e recusado quando inválido/expirado.
- [ ] Admin global visualiza todos os clientes.
- [ ] Admin cliente, coordenação e financeiro visualizam apenas o próprio cliente.
- [ ] Médico visualiza apenas dados próprios no Web e no mobile.
- [ ] Acesso negado aparece de forma amigável e registra auditoria.

## 3. CRUDs operacionais

- [ ] Clientes: index, cadastro, edição, detalhes, filtros e status.
- [ ] Planos: cadastro, edição, recursos, inativação/reativação e limites.
- [ ] Assinaturas: criação, edição, troca de plano e alteração de status.
- [ ] Faturas SaaS: geração mensal, pagamento, cancelamento, contestação e inadimplência.
- [ ] Médicos, hospitais e especialidades: cadastro, edição, detalhes e inativação.
- [ ] Plantões: criação, edição, publicação, detalhes e bloqueio por limite.
- [ ] Escalas: listagem, detalhes, confirmação, recusa, cancelamento, substituição e realizado.
- [ ] Pagamentos: listagem, detalhes e confirmação operacional quando habilitada.
- [ ] Notificações e comunicação: listagem, detalhes e mensagens.

## 4. Jornada comercial e SaaS

- [ ] Lead é criado e convertido em oportunidade.
- [ ] Oportunidade recebe interações.
- [ ] Proposta válida pode ser aprovada.
- [ ] Proposta vencida não pode ser aprovada.
- [ ] Oportunidade ganha cria cliente e segue para jornada.
- [ ] Cliente convertido possui plano e assinatura antes de operar.
- [ ] Operação Assistida abre para cliente em implantação.
- [ ] Cliente em risco gera ação/recomendação de Customer Success.
- [ ] Toda mudança relevante gera auditoria e evento de sistema.

## 5. Limites, bloqueios e faturamento

- [ ] Cliente ativo com assinatura vigente opera normalmente.
- [ ] Cliente suspenso, cancelado ou inadimplente não publica plantão.
- [ ] Plano inativo não pode ser assinado.
- [ ] Limites de médicos, hospitais, usuários, plantões e convites bloqueiam cadastros/envios excedentes.
- [ ] Plano sem mobile bloqueia API mobile com mensagem segura.
- [ ] Plano sem BI ou relatório avançado bloqueia exportações avançadas.
- [ ] Bloqueio gera auditoria, alerta e registro de bloqueio do cliente.
- [ ] Fatura duplicada por competência é impedida.
- [ ] Inadimplência aparece no dashboard SaaS e influencia saúde do cliente.

## 6. Inteligência, Customer Success e manual

- [ ] Saúde do cliente calcula SAUDAVEL, ATENCAO, RISCO e CRITICO conforme cenário.
- [ ] Uso acima de 80% gera oportunidade/recomendação de upgrade.
- [ ] Cliente sem login recente, com chamados críticos ou implantação atrasada aparece em alerta.
- [ ] Recalcular saúde funciona e não duplica alertas indevidamente.
- [ ] Manual interativo abre pela topbar.
- [ ] Busca retorna artigos por perfil e mostra empty state quando não há resultado.
- [ ] Feedback "Foi útil?" salva e registra retorno ao usuário.

## 7. LGPD, auditoria e observabilidade

- [ ] Política atual abre.
- [ ] Consentimento pode ser registrado.
- [ ] Usuário solicita exportação dos próprios dados.
- [ ] Solicitação de anonimização respeita retenções legais.
- [ ] Médico vê apenas solicitações próprias.
- [ ] Admin cliente vê apenas solicitações do próprio contexto.
- [ ] Admin global vê todas as solicitações.
- [ ] Auditoria/Index e Auditoria/Details abrem.
- [ ] Observabilidade/Index, Erros, Acessos e Eventos abrem com limites de consulta.
- [ ] Senhas, tokens e segredos não aparecem em logs.

## 8. Mobile/API

- [ ] `/api/mobile/home` retorna dados do médico autenticado.
- [ ] Plantões disponíveis, convites, minhas escalas, pagamentos e notificações respeitam tenant e médico.
- [ ] Plano sem mobile retorna bloqueio amigável e auditado.
- [ ] Respostas seguem envelope `ApiResponse<T>` ou padrão equivalente documentado.

## 9. Resultado final

- [ ] Build API verde.
- [ ] Build Web verde.
- [ ] Testes automatizados verdes ou pendência ambiental documentada.
- [ ] QA manual executado com evidências.
- [ ] Sem binários, secrets, tokens ou patches de backup no commit.
- [ ] Documentação reflete apenas funcionalidades existentes ou pendências reais.
